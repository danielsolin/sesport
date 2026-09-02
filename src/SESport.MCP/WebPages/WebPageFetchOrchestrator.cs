using UglyToad.PdfPig;

namespace SESport.AI.WebPages;

/// <summary>
/// Central owner of the page fetch decision tree. Transports return
/// structured evidence; this type decides what happens next, keeps the
/// attempt ledger, enforces the stage budget, and produces the final
/// result.
/// </summary>
internal sealed class WebPageFetchOrchestrator
{
   private readonly HttpClient _httpClient;
   private readonly ILogger _logger;
   private readonly Func<Task<string>> _browserUserAgentFetcher;
   private readonly Func<Uri, IReadOnlyList<BrowserStrategyDescriptor>,
      CancellationToken, Task<WebPageBrowserOutcome>> _browserFetcher;
   private readonly Func<Uri, int, CancellationToken,
      Task<WebPageHttpResponse>> _curlTransport;
   private readonly Func<IReadOnlyList<WebPageImageCandidate>,
      CancellationToken, Task<string>> _imageTextFetcher;
   private readonly BrowserStrategyPolicy _browserStrategyPolicy = new();

   internal WebPageFetchOrchestrator(
      HttpClient httpClient,
      ILogger logger,
      Func<Task<string>> browserUserAgentFetcher,
      Func<Uri, IReadOnlyList<BrowserStrategyDescriptor>,
         CancellationToken, Task<WebPageBrowserOutcome>> browserFetcher,
      Func<Uri, int, CancellationToken,
         Task<WebPageHttpResponse>> curlTransport,
      Func<IReadOnlyList<WebPageImageCandidate>,
         CancellationToken, Task<string>> imageTextFetcher
   )
   {
      _httpClient = httpClient;
      _logger = logger;
      _browserUserAgentFetcher = browserUserAgentFetcher;
      _browserFetcher = browserFetcher;
      _curlTransport = curlTransport;
      _imageTextFetcher = imageTextFetcher;
   }

   internal async Task<WebPageContent?> FetchAsync(
      Uri url,
      CancellationToken cancellationToken
   )
   {
      using var budget = new WebPageFetchBudget(
         WebPageFetchDefaults.TotalFetchTimeout,
         cancellationToken
      );

      try
      {
         var result = await RunAsync(url, budget, budget.DeadlineToken);
         return await AppendImageTextAsync(
            result,
            budget,
            budget.DeadlineToken
         );
      }
      catch(OperationCanceledException)
         when(budget.CallerCanceled)
      {
         throw;
      }
      catch(OperationCanceledException)
      {
         _logger.LogWarning(
            "Page fetch for {Url} timed out.",
            url
         );

         return WebPageContentFetchSupport.BuildFailureContent(
            url,
            null,
            WebPageFetchErrorKind.Timeout,
            "Web page fetch exceeded its configured total timeout.",
            "timeout"
         );
      }
   }

   private async Task<WebPageContent?> AppendImageTextAsync(
      WebPageContent? page,
      WebPageFetchBudget budget,
      CancellationToken token
   )
   {
      if(page?.RelevantImages is not { Count: > 0 } images)
      {
         return page;
      }

      // OCR only runs while the text is still insufficient. A page that
      // already has rich text does not need its images read.
      if(page.HasBodyText &&
         page.MainTextFull.Length >=
            WebPageFetchDefaults.RichContentMinimumCharacters)
      {
         return page;
      }

      if(budget.Remaining <= TimeSpan.Zero)
      {
         return page;
      }

      string imageText;
      try
      {
         imageText = await _imageTextFetcher(images, token);
      }
      catch(OperationCanceledException)
         when(!budget.CallerCanceled)
      {
         return page;
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(Exception exception)
      {
         _logger.LogWarning(
            "Image OCR failed for {Url}: {Reason}",
            page.Url,
            WebPageFetchLogging.SummarizeException(exception)
         );
         return page;
      }

      if(string.IsNullOrWhiteSpace(imageText))
      {
         return page;
      }

      var fullText = string.IsNullOrWhiteSpace(page.MainTextFull)
         ? imageText
         : page.MainTextFull.TrimEnd() +
            Environment.NewLine +
            Environment.NewLine +
            imageText;

      var updated = page with
      {
         MainTextFull = fullText,
         MainText = WebPageContentFetchSupport.ApplyResponseCutoff(
            fullText
         ),
         HasBodyText = true
      };

      // OCR success may clear a stale error when the resulting candidate
      // is intentionally selected as usable.
      if(updated.FetchErrorMessage is not null &&
         fullText.Length >=
            WebPageFetchDefaults.RichContentMinimumCharacters)
      {
         updated = updated with
         {
            FetchErrorMessage = null,
            FetchErrorKind = null
         };
      }

      return updated;
   }

   private async Task<WebPageContent?> RunAsync(
      Uri url,
      WebPageFetchBudget budget,
      CancellationToken token
   )
   {
      var ledger = new WebPageFetchLedger(url);
      var browserUserAgent = await GetBrowserUserAgentAsync(
         budget,
         token
      );

      // ---- Direct HTTP stage (with bounded transient retries) ----
      var httpResponse = await SendDirectHttpAsync(
         url,
         browserUserAgent,
         budget,
         token,
         ledger
      );

      if(httpResponse?.RedirectPolicyError is { } redirectError)
      {
         ledger.Add("http", $"Redirect blocked: {redirectError}");
         return WebPageContentFetchSupport.BuildFailureContent(
            url,
            null,
            WebPageFetchErrorKind.HttpError,
            $"Redirect target rejected: {redirectError}",
            "http"
         );
      }

      if(httpResponse?.ErrorKind ==
         WebPageFetchErrorKind.ResponseTooLarge)
      {
         ledger.Add(
            "decision",
            "Direct HTTP response exceeded the configured byte limit."
         );
         return BuildResponseTooLargeFailure(url, "http");
      }

      if(IsPdfResponse(httpResponse))
      {
         return await HandlePdfAsync(
            url,
            httpResponse,
            budget,
            token,
            ledger,
            stage: "http"
         );
      }

      var htmlEvidence = ClassifyHtml(httpResponse, url, "http");
      WebPageHtmlCandidate? bestCandidate = null;
      WebPageAssessment? bestAssessment = null;
      WebPageAssessment? blockedAssessment = null;

      var directNotFoundEvidence =
         httpResponse is { StatusCode: 404 or 410 } &&
         htmlEvidence.Assessment?.Classification ==
            WebPageContentClassification.NotFound;

      if(ShouldFailNotFound(httpResponse, htmlEvidence.Assessment) &&
         !directNotFoundEvidence)
      {
         ledger.Add("decision", "Direct HTTP proved not-found.");
         return BuildNotFoundFailure(url, htmlEvidence.Assessment);
      }

      if(htmlEvidence.Assessment?.Classification ==
         WebPageContentClassification.Blocked)
      {
         blockedAssessment = htmlEvidence.Assessment;
      }

      if(httpResponse is { StatusCode: >= 200 and < 300 } &&
         htmlEvidence.CleanSuccess is not null)
      {
         return htmlEvidence.CleanSuccess;
      }

      var browserEligible = ShouldTryBrowser(
         httpResponse,
         htmlEvidence.Assessment
      );
      var curlEligible = ShouldTryCurl(
         httpResponse,
         htmlEvidence.Assessment
      );

      if(httpResponse is { StatusCode: >= 200 and < 300 } &&
         htmlEvidence.Candidate is not null &&
         IsPartial(htmlEvidence.Assessment))
      {
         bestCandidate = htmlEvidence.Candidate;
         bestAssessment = htmlEvidence.Assessment;
      }

      // ---- Browser stage ----
      if(browserEligible &&
         budget.Remaining >= WebPageFetchDefaults.MinBrowserStageBudget)
      {
         var browserEvidence = await RunBrowserStageAsync(
            url,
            budget,
            token,
            ledger
         );

         if(browserEvidence.Assessment is not null &&
            IsBetterCandidate(
               bestCandidate,
               browserEvidence.Candidate
            ))
         {
            bestCandidate = browserEvidence.Candidate;
            bestAssessment = browserEvidence.Assessment;
         }

         if(browserEvidence.CleanSuccess is not null)
         {
            return browserEvidence.CleanSuccess;
         }

         if(browserEvidence.Assessment?.Classification ==
            WebPageContentClassification.Blocked)
         {
            blockedAssessment = browserEvidence.Assessment;
         }
      }
      else if(browserEligible)
      {
         ledger.Add(
            "browser",
            "Skipped: not enough budget remains."
         );
      }

      // ---- Curl stage ----
      if(curlEligible &&
         budget.Remaining >= WebPageFetchDefaults.MinCurlStageBudget)
      {
         var curlResponse = await _curlTransport(
            url,
            CurlBudgetSeconds(budget),
            token
         );
         ledger.Add("curl", DescribeResponse(curlResponse));

         if(curlResponse.RedirectPolicyError is { } curlRedirectError)
         {
            ledger.Add(
               "curl",
               $"Redirect blocked: {curlRedirectError}"
            );
         }
         else if(curlResponse.ErrorKind ==
            WebPageFetchErrorKind.ResponseTooLarge)
         {
            ledger.Add(
               "decision",
               "Curl response exceeded the configured byte limit."
            );
            return BuildResponseTooLargeFailure(url, "curl");
         }
         else if(IsPdfResponse(curlResponse))
         {
            var pdfResult = await HandlePdfAsync(
               url,
               curlResponse,
               budget,
               token,
               ledger,
               allowCurlFallback: false,
               stage: "curl"
            );

            if(pdfResult is not null)
            {
               return pdfResult;
            }
         }
         else
         {
            var curlEvidence = ClassifyHtml(
               curlResponse,
               url,
               "curl"
            );

            if(curlEvidence.Assessment?.Classification ==
               WebPageContentClassification.Blocked)
            {
               blockedAssessment = curlEvidence.Assessment;
            }

            if(curlEvidence.CleanSuccess is not null)
            {
               return curlEvidence.CleanSuccess;
            }

            if(ShouldFailNotFound(curlResponse, curlEvidence.Assessment))
            {
               ledger.Add(
                  "decision",
                  "Curl proved not-found."
               );
               if(directNotFoundEvidence ||
                  httpResponse is { StatusCode: 404 or 410 })
               {
                  return BuildNotFoundFailure(
                     url,
                     curlEvidence.Assessment
                  );
               }
            }

            if(httpResponse is { StatusCode: 404 or 410 } &&
               curlResponse is { StatusCode: 404 or 410 })
            {
               ledger.Add(
                  "decision",
                  "Curl confirmed the direct HTTP not-found status."
               );
               return BuildNotFoundFailure(url, curlEvidence.Assessment);
            }

            if(curlResponse is { StatusCode: >= 200 and < 300 } &&
               curlEvidence.Candidate is not null &&
               IsPartial(curlEvidence.Assessment) &&
               IsBetterCandidate(
                  bestCandidate,
                  curlEvidence.Candidate
               ))
            {
               bestCandidate = curlEvidence.Candidate;
               bestAssessment = curlEvidence.Assessment;
            }
         }
      }
      else if(curlEligible)
      {
         ledger.Add(
            "curl",
            "Skipped: not enough budget remains."
         );
      }

      // ---- Final selection ----
      return SelectFinalResult(
         url,
         httpResponse,
         bestCandidate,
         bestAssessment,
         blockedAssessment,
         ledger,
         budget,
         directNotFoundEvidence
      );
   }

   private async Task<string> GetBrowserUserAgentAsync(
      WebPageFetchBudget budget,
      CancellationToken token
   )
   {
      try
      {
         return await _browserUserAgentFetcher().WaitAsync(token);
      }
      catch(OperationCanceledException)
         when(budget.CallerCanceled)
      {
         throw;
      }
      catch(Exception exception)
      {
         _logger.LogWarning(
            "Browser user agent fetch failed: {Reason}. Using fallback.",
            WebPageFetchLogging.SummarizeException(exception)
         );
         return WebPageFetchDefaults.BrowserUserAgentFallback;
      }
   }

   private async Task<WebPageHttpResponse?> SendDirectHttpAsync(
      Uri url,
      string browserUserAgent,
      WebPageFetchBudget budget,
      CancellationToken token,
      WebPageFetchLedger ledger
   )
   {
      WebPageHttpResponse? response = null;

      for(var attempt = 0; ; attempt++)
      {
         response = await WebPageHttpTransport.SendAsync(
            _httpClient,
            url,
            browserUserAgent,
            token
         );
         ledger.Add("http", DescribeResponse(response));

         if(!IsTransientHttpFailure(response) ||
            attempt >= WebPageFetchDefaults.MaxTransientHttpRetries)
         {
            return response;
         }

         var delay = WebPageFetchDefaults.TransientRetryDelays[attempt];
         if(budget.Remaining < delay)
         {
            ledger.Add(
               "http",
               "Retry skipped: not enough budget remains."
            );
            return response;
         }

         _logger.LogInformation(
            "Direct HTTP attempt {Attempt} for {Url} was transient; " +
            "retrying in {Delay}.",
            attempt + 1,
            url,
            delay
         );

         await Task.Delay(delay, token);
      }
   }

   private async Task<WebPageContent?> HandlePdfAsync(
      Uri url,
      WebPageHttpResponse? httpResponse,
      WebPageFetchBudget budget,
      CancellationToken token,
      WebPageFetchLedger ledger,
      bool allowCurlFallback = true,
      string stage = "http"
   )
   {
      string? pdfError = null;
      var httpEffectiveUrl = httpResponse?.EffectiveUrl ?? url;

      if(httpResponse?.Body is { Length: > 0 } body)
      {
         var extraction = ExtractPdfText(body, httpEffectiveUrl);
         if(extraction.Success)
         {
            ledger.Add(stage, "PDF text extracted successfully.");
            return BuildPdfSuccess(
               httpEffectiveUrl,
               stage,
               extraction
            );
         }

         pdfError = extraction.Error ?? "Unknown PDF error.";
         ledger.Add(stage, $"PDF extraction failed: {pdfError}");
      }

      if(httpResponse?.ErrorKind ==
         WebPageFetchErrorKind.ResponseTooLarge)
      {
         return BuildResponseTooLargeFailure(url, stage);
      }

      if(!allowCurlFallback ||
         budget.Remaining < WebPageFetchDefaults.MinCurlStageBudget)
      {
         return BuildPdfFailure(url, pdfError);
      }

      var curlResponse = await _curlTransport(
         url,
         CurlBudgetSeconds(budget),
         token
      );
      ledger.Add("curl", DescribeResponse(curlResponse));

      if(curlResponse.ErrorKind ==
         WebPageFetchErrorKind.ResponseTooLarge)
      {
         return BuildResponseTooLargeFailure(url, "curl");
      }

      if(curlResponse.Body is { Length: > 0 } curlBody &&
         IsPdfResponse(curlResponse))
      {
         var extraction = ExtractPdfText(
            curlBody,
            curlResponse.EffectiveUrl
         );
         if(extraction.Success)
         {
            ledger.Add("curl", "PDF text extracted successfully.");
            return BuildPdfSuccess(
               curlResponse.EffectiveUrl,
               "curl",
               extraction
            );
         }

         pdfError = extraction.Error ?? pdfError;
      }

      return BuildPdfFailure(url, pdfError);
   }

   private static PdfExtractionResult ExtractPdfText(
      byte[] body,
      Uri url
   )
   {
      try
      {
         using var pdfStream = new MemoryStream(body);
         using var pdfDocument = PdfDocument.Open(pdfStream);
         var text = WebPageContentFetchSupport.ExtractPdfText(
            pdfDocument
         );

         if(string.IsNullOrWhiteSpace(text))
         {
            return PdfExtractionResult.Failed(
               "PDF response produced no text."
            );
         }

         return PdfExtractionResult.Succeeded(
            text,
            WebPageContentFetchSupport.ExtractPdfTitle(pdfDocument, url)
         );
      }
      catch(Exception exception)
      {
         return PdfExtractionResult.Failed(
            $"Unable to extract PDF response: " +
               WebPageFetchLogging.SummarizeException(exception)
         );
      }
   }

   private async Task<BrowserStageEvidence> RunBrowserStageAsync(
      Uri url,
      WebPageFetchBudget budget,
      CancellationToken token,
      WebPageFetchLedger ledger
   )
   {
      var strategies = _browserStrategyPolicy.GetStrategies(url);

      WebPageBrowserOutcome outcome;
      try
      {
         outcome = await _browserFetcher(url, strategies, token);
      }
      catch(OperationCanceledException)
         when(budget.CallerCanceled)
      {
         throw;
      }
      catch(Exception exception)
      {
         ledger.Add(
            "browser",
            "Stage failed: " +
               WebPageFetchLogging.SummarizeException(exception)
         );
         return BrowserStageEvidence.None;
      }

      foreach(var attempt in outcome.Attempts)
      {
         ledger.Add(
            $"browser:{attempt.StrategyId}",
            attempt.FailureSummary is { } failure
               ? failure
               : attempt.Rendered
                  ? $"Rendered (status {attempt.NavigationStatus})."
                  : "Launched without render."
         );

         if(!attempt.Launched)
         {
            _browserStrategyPolicy.ReportLaunchFailure(
               attempt.StrategyId
            );
         }
      }

      if(outcome.Render is not { } render)
      {
         return BrowserStageEvidence.None;
      }

      var renderUrl = url;
      if(render.EffectiveUrl is { } effectiveUrl)
      {
         if(!WebPageUrlPolicy.TryValidate(
            effectiveUrl.ToString(),
            out var validatedUrl,
            out _
         ))
         {
            ledger.Add(
               "browser",
               "Final browser URL was rejected by the URL policy."
            );
            return BrowserStageEvidence.None;
         }

         renderUrl = validatedUrl;
      }

      var candidate = WebPageHtmlCandidate.FromRendered(
         render.FullHtml,
         render.BodyHtml,
         render.Title,
         render.RelevantImages,
         renderUrl
      );
      var assessment = candidate.Assess(WebPageBlockSource.Browser);

      var cleanStatus = render.NavigationStatus is
         null or >= 200 and < 300;

      if(assessment.IsSuccess && cleanStatus)
      {
         var successfulAttempt = outcome.Attempts.LastOrDefault(
            attempt => attempt.StrategyId == render.StrategyId
         );

         if(!render.ContentTruncated && successfulAttempt is
            { Launched: true, Rendered: true, ErrorKind: null })
         {
            _browserStrategyPolicy.ReportSuccess(
               url,
               render.StrategyId
            );
         }

         return new BrowserStageEvidence(
            candidate,
            assessment,
            BuildSuccess(candidate, "playwright", render)
         );
      }

      if(IsPartial(assessment) &&
         candidate.TextContent.Length > 0 &&
         cleanStatus)
      {
         return new BrowserStageEvidence(candidate, assessment, null);
      }

      if(assessment.Classification ==
         WebPageContentClassification.Blocked)
      {
         // Blocked evidence is kept for the final failure message;
         // the blocked body itself is never returned as content.
         return new BrowserStageEvidence(
            null,
            assessment,
            null
         );
      }

      return BrowserStageEvidence.None;
   }

   private static WebPageContent? SelectFinalResult(
      Uri url,
      WebPageHttpResponse? httpResponse,
      WebPageHtmlCandidate? bestCandidate,
      WebPageAssessment? bestAssessment,
      WebPageAssessment? blockedAssessment,
      WebPageFetchLedger ledger,
      WebPageFetchBudget budget,
      bool directNotFoundEvidence
   )
   {
      if(bestCandidate is { TextContent.Length: > 0 } candidate &&
         bestAssessment is { IsSuccess: false } assessment)
      {
         var warning = $"Content may be incomplete: {assessment.Reason}.";
         var content = BuildSuccess(candidate, "partial", warning: warning);

         return content;
      }

      var summary = ledger.BuildSummary();

      if(bestAssessment?.Classification ==
         WebPageContentClassification.NotFound)
      {
         return WebPageContentFetchSupport.BuildFailureContent(
            url,
            null,
            WebPageFetchErrorKind.HttpError,
            $"The page was not found. {summary}",
            "http"
         );
      }

      if(blockedAssessment is { } blocked &&
         blockedAssessment.BlockSignature is { } marker)
      {
         return WebPageContentFetchSupport.BuildFailureContent(
            url,
            null,
            WebPageFetchErrorKind.BrowserBlocked,
            $"The page is behind a block or challenge " +
               $"(marker: {marker}). {summary}",
            "http"
         );
      }

      if(blockedAssessment is not null)
      {
         return WebPageContentFetchSupport.BuildFailureContent(
            url,
            null,
            WebPageFetchErrorKind.BrowserBlocked,
            $"The page is behind a block or challenge. {summary}",
            "http"
         );
      }

      if(directNotFoundEvidence)
      {
         return WebPageContentFetchSupport.BuildFailureContent(
            url,
            null,
            WebPageFetchErrorKind.HttpError,
            "Direct HTTP reported that the page was not found, but " +
               "independent confirmation was unavailable. " + summary,
            "http"
         );
      }

      if(budget.Remaining <= TimeSpan.Zero)
      {
         return WebPageContentFetchSupport.BuildFailureContent(
            url,
            null,
            WebPageFetchErrorKind.Timeout,
            $"No usable content was retrieved. {summary}",
            "timeout"
         );
      }

      return WebPageContentFetchSupport.BuildFailureContent(
         url,
         null,
         WebPageFetchErrorKind.HttpError,
         $"No usable content was retrieved. {summary}",
         "http"
      );
   }

   private static WebPageContent BuildNotFoundFailure(
      Uri url,
      WebPageAssessment? assessment
   )
   {
      var reason = assessment?.Reason is { } reasonText
         ? $" ({reasonText})"
         : "";

      return WebPageContentFetchSupport.BuildFailureContent(
         url,
         null,
         WebPageFetchErrorKind.HttpError,
         $"The page was not found.{reason}",
         "http"
      );
   }

   private static WebPageContent BuildPdfFailure(
      Uri url,
      string? pdfError
   )
   {
      return WebPageContentFetchSupport.BuildFailureContent(
         url,
         null,
         null,
         pdfError ?? "PDF response produced no text.",
         "pdf"
      );
   }

   private static WebPageContent BuildResponseTooLargeFailure(
      Uri url,
      string fetcher
   )
   {
      return WebPageContentFetchSupport.BuildFailureContent(
         url,
         null,
         WebPageFetchErrorKind.ResponseTooLarge,
         "The response exceeded the configured maximum of " +
            $"{WebPageFetchDefaults.MaximumResponseBytes} bytes.",
         fetcher
      );
   }

   private static WebPageContent BuildPdfSuccess(
      Uri url,
      string fetcher,
      PdfExtractionResult extraction
   )
   {
      var text = extraction.Text ?? string.Empty;

      return new WebPageContent(
         extraction.Title ?? url.ToString(),
         url.ToString(),
         null,
         [],
         WebPageContentFetchSupport.ApplyResponseCutoff(text),
         true,
         text,
         Fetcher: fetcher
      );
   }

   private static WebPageContent BuildSuccess(
      WebPageHtmlCandidate candidate,
      string fetcher,
      WebPageBrowserRenderResult? render = null,
      string? warning = null
   )
   {
      var renderWarning = warning ?? candidate.RenderWarning;
      if(render?.ContentTruncated == true)
      {
         renderWarning = string.IsNullOrWhiteSpace(renderWarning)
            ? "Rendered content was truncated at the response limit."
            : renderWarning +
               " Rendered content was truncated at the response limit.";
      }

      return new WebPageContent(
         candidate.Title is { Length: > 0 }
            ? candidate.Title
            : candidate.Url.ToString(),
         candidate.Url.ToString(),
         candidate.PublishedAt,
         candidate.Headings,
         WebPageContentFetchSupport.ApplyResponseCutoff(
            candidate.TextContent
         ),
         true,
         candidate.TextContent,
         Fetcher: fetcher,
         BrowserStrategy: render?.StrategyId,
         RelevantLinks: candidate.RelevantLinks,
         RelevantImages: candidate.RelevantImages,
         RenderWarning: renderWarning
      );
   }

   private static bool IsPdfResponse(WebPageHttpResponse? response)
   {
      if(response is null || response.StatusCode is not >= 200 and < 300)
      {
         return false;
      }

      var contentType = response.ContentType;

      if(string.Equals(
         contentType,
         "application/pdf",
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         contentType,
         "application/x-pdf",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return true;
      }

      var body = response.Body;
      return body is not null && body.Length >= 4 &&
         body[0] == (byte)'%' &&
         body[1] == (byte)'P' &&
         body[2] == (byte)'D' &&
         body[3] == (byte)'F';
   }

   private static bool IsTransientHttpFailure(
      WebPageHttpResponse? response
   )
   {
      if(response is null)
      {
         return true;
      }

      if(response.ErrorKind is not null)
      {
         return false;
      }

      if(response.TransportError is not null)
      {
         return true;
      }

      return response.StatusCode is 408 or 425 or 429 or >= 500;
   }

   private static bool IsPartial(WebPageAssessment? assessment)
   {
      return assessment?.Classification ==
         WebPageContentClassification.Partial;
   }

   private static bool IsBetterCandidate(
      WebPageHtmlCandidate? current,
      WebPageHtmlCandidate? proposed
   )
   {
      // A new candidate is only worth adopting when it is strictly
      // longer than what we already hold; a short rendered shell must
      // not displace a longer static body.
      if(current is null)
      {
         return proposed is not null;
      }

      return proposed is not null &&
         proposed.TextContent.Length > current.TextContent.Length;
   }

   private static bool ShouldFailNotFound(
      WebPageHttpResponse? response,
      WebPageAssessment? assessment
   )
   {
      if(response is null || assessment is null)
      {
         return false;
      }

      if(response.StatusCode is 404 or 410)
      {
         return assessment.Classification ==
            WebPageContentClassification.NotFound;
      }

      // A 2xx page whose visible text says "not found" is treated the
      // same way: the site told us the page does not exist.
      return response.StatusCode is >= 200 and < 300 &&
         assessment.Classification ==
            WebPageContentClassification.NotFound;
   }

   private static bool ShouldTryBrowser(
      WebPageHttpResponse? response,
      WebPageAssessment? assessment
   )
   {
      if(response is null || response.TransportError is not null)
      {
         return true;
      }

      var status = response.StatusCode;

      if(status is 401 or 403 or 429 or >= 500)
      {
         return true;
      }

      if(status is 404 or 410)
      {
         return assessment?.Classification ==
            WebPageContentClassification.Blocked;
      }

      if(status is >= 200 and < 300)
      {
         return assessment is not null && !assessment.IsSuccess &&
            assessment.Classification !=
               WebPageContentClassification.NotFound;
      }

      return false;
   }

   private static bool ShouldTryCurl(
      WebPageHttpResponse? response,
      WebPageAssessment? assessment
   )
   {
      if(response is null || response.TransportError is not null)
      {
         return true;
      }

      var status = response.StatusCode;

      if(status is 401 or 403 or 429 or >= 500)
      {
         return true;
      }

      if(status is 404 or 410)
      {
         return assessment?.Classification is
            WebPageContentClassification.NotFound or
            WebPageContentClassification.Empty or
            WebPageContentClassification.Blocked;
      }

      if(status is >= 200 and < 300)
      {
         return assessment is not null &&
            assessment.Classification is
               WebPageContentClassification.Empty or
               WebPageContentClassification.Blocked or
               WebPageContentClassification.NeedsRendering;
      }

      return false;
   }

   private static int CurlBudgetSeconds(WebPageFetchBudget budget)
   {
      var seconds = (int)Math.Min(
         budget.Remaining.TotalSeconds,
         WebPageFetchDefaults.CurlMaxTimeSeconds
      );
      return Math.Max(1, seconds);
   }

   private static string DescribeResponse(WebPageHttpResponse? response)
   {
      if(response is null)
      {
         return "No response.";
      }

      if(response.RedirectPolicyError is { } redirectError)
      {
         return $"Redirect rejected: {redirectError}";
      }

      if(response.ErrorKind is { } errorKind)
      {
         return errorKind == WebPageFetchErrorKind.ResponseTooLarge
            ? "Response exceeded the configured byte limit."
            : errorKind.ToString();
      }

      if(response.TransportError is { } transportError)
      {
         return transportError;
      }

      return $"Status {response.StatusCode}." +
         (response.Redirected ? " (redirected)" : "");
   }

   private static ClassifyResult ClassifyHtml(
      WebPageHttpResponse? response,
      Uri requestedUrl,
      string fetcher
   )
   {
      if(response is null || response.Body is null)
      {
         return new ClassifyResult(null, null, null);
      }

      // Error statuses are assessed for evidence (block pages,
      // not-found markers), but their bodies are never returned as
      // clean content.
      var candidate = CreateCandidate(response, requestedUrl);
      var assessment = candidate.Assess(WebPageBlockSource.HtmlFallback);

      WebPageContent? cleanSuccess = null;
      if(response.StatusCode is >= 200 and < 300 &&
         assessment.IsSuccess)
      {
         cleanSuccess = BuildSuccess(candidate, fetcher);
      }

      return new ClassifyResult(candidate, assessment, cleanSuccess);
   }

   private static WebPageHtmlCandidate CreateCandidate(
      WebPageHttpResponse response,
      Uri requestedUrl
   )
   {
      var html = System.Text.Encoding.UTF8.GetString(response.Body);
      return WebPageHtmlCandidate.FromHtml(html, response.EffectiveUrl);
   }

   private sealed record ClassifyResult(
      WebPageHtmlCandidate? Candidate,
      WebPageAssessment? Assessment,
      WebPageContent? CleanSuccess
   );

   private sealed record BrowserStageEvidence(
      WebPageHtmlCandidate? Candidate,
      WebPageAssessment? Assessment,
      WebPageContent? CleanSuccess
   )
   {
      internal static readonly BrowserStageEvidence None = new(
         null,
         null,
         null
      );
   }
}
