using UglyToad.PdfPig;

namespace SESport.AI.WebPages;

internal static class WebPagePdfPageFetcher
{
   internal static async Task<WebPageContent?> FetchAsync(
      HttpResponseMessage response,
      Uri absoluteUrl,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var absoluteUrlString = absoluteUrl.ToString();
         var pdfBytes = await response.Content.ReadAsByteArrayAsync(
            cancellationToken
         );

         if(pdfBytes.Length == 0)
         {
            return WebPageContentFetchSupport.BuildFailureContent(
               absoluteUrl,
               null,
               null,
               "PDF response had no body.",
               "pdf"
            );
         }

         using var pdfStream = new MemoryStream(pdfBytes);
         using var pdfDocument = PdfDocument.Open(pdfStream);
         var text = WebPageContentFetchSupport.ExtractPdfText(pdfDocument);

         if(string.IsNullOrWhiteSpace(text))
         {
            return WebPageContentFetchSupport.BuildFailureContent(
               absoluteUrl,
               title: WebPageContentFetchSupport.ExtractPdfTitle(
                  pdfDocument,
                  absoluteUrl
               ),
               fetchErrorKind: null,
               fetchErrorMessage: "PDF response produced no text.",
               fetcher: "pdf"
            );
         }

         var title =
            WebPageContentFetchSupport.ExtractPdfTitle(
               pdfDocument,
               absoluteUrl
            );
         return new WebPageContent(
            title,
            absoluteUrlString,
            null,
            [],
            WebPageContentFetchSupport.ApplyResponseCutoff(text),
            true,
            text,
            Fetcher: "pdf"
         );
      }
      catch(OperationCanceledException)
      {
         throw;
      }
      catch(Exception)
      {
         return WebPageContentFetchSupport.BuildFailureContent(
            absoluteUrl,
            null,
            null,
            "Unable to extract PDF response.",
            "pdf"
         );
      }
   }
}
