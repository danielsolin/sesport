using System.Diagnostics;
using System.Globalization;

using Microsoft.Extensions.Logging;

namespace SESport.AI.WebPages;

internal static class WebPageImageOcr
{
   internal static async Task<string> ExtractAsync(
      HttpClient httpClient,
      ILogger logger,
      IReadOnlyList<WebPageImageCandidate> images,
      CancellationToken cancellationToken
   )
   {
      var sections = new List<string>();

      foreach(var image in images
         .Take(WebPageFetchDefaults.ImageOcrMaximumCandidateCount))
      {
         var text = await ExtractImageAsync(
            httpClient,
            logger,
            image,
            cancellationToken
         );

         if(string.IsNullOrWhiteSpace(text))
         {
            continue;
         }

         sections.Add(
            $"Image text ({image.Url}):" +
            Environment.NewLine +
            text
         );
      }

      return string.Join(
         Environment.NewLine + Environment.NewLine,
         sections
      );
   }

   internal static string ParseTsv(string tsv)
   {
      var words = tsv
         .ReplaceLineEndings("\n")
         .Split('\n', StringSplitOptions.RemoveEmptyEntries)
         .Skip(1)
         .Select(ParseWord)
         .Where(word => word is not null)
         .Select(word => word!)
         .ToList();

      if(words.Count < WebPageFetchDefaults.ImageOcrMinimumWordCount)
      {
         return string.Empty;
      }

      var meanConfidence = words.Average(word => word.Confidence);

      if(meanConfidence <
         WebPageFetchDefaults.ImageOcrMinimumMeanConfidence)
      {
         return string.Empty;
      }

      var lines = words
         .GroupBy(word => new
         {
            word.Page,
            word.Block,
            word.Paragraph,
            word.Line
         })
         .Select(group => new OcrLine(
            group.Min(word => word.Top),
            group.Min(word => word.Left),
            FormatLine(group.OrderBy(word => word.Left).ToList())
         ))
         .Where(line => !string.IsNullOrWhiteSpace(line.Text))
         .OrderBy(line => line.Top)
         .ThenBy(line => line.Left)
         .Select(line => line.Text);

      return string.Join(Environment.NewLine, lines);
   }

   private static async Task<string?> ExtractImageAsync(
      HttpClient httpClient,
      ILogger logger,
      WebPageImageCandidate image,
      CancellationToken cancellationToken
   )
   {
      if(!WebPageUrlPolicy.TryValidate(
         image.Url,
         out var imageUri,
         out _
      ))
      {
         return null;
      }

      var imageBytes = await DownloadImageAsync(
         httpClient,
         imageUri,
         cancellationToken
      );

      if(imageBytes is null)
      {
         return null;
      }

      var temporaryPath = Path.Combine(
         Path.GetTempPath(),
         $"sesport-ocr-{Guid.NewGuid():N}{GetExtension(imageUri)}"
      );

      try
      {
         await File.WriteAllBytesAsync(
            temporaryPath,
            imageBytes,
            cancellationToken
         );
         var tsv = await RunTesseractAsync(
            temporaryPath,
            cancellationToken
         );

         return ParseTsv(tsv);
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(Exception exception)
      {
         logger.LogWarning(
            exception,
            "Unable to run image OCR for {ImageUrl}.",
            image.Url
         );
         return null;
      }
      finally
      {
         TryDeleteTemporaryFile(temporaryPath);
      }
   }

   private static async Task<byte[]?> DownloadImageAsync(
      HttpClient httpClient,
      Uri imageUri,
      CancellationToken cancellationToken
   )
   {
      using var request = new HttpRequestMessage(HttpMethod.Get, imageUri);
      using var response = await httpClient.SendAsync(
         request,
         HttpCompletionOption.ResponseHeadersRead,
         cancellationToken
      );

      if(!response.IsSuccessStatusCode ||
         response.Content.Headers.ContentType?.MediaType is not
         { } mediaType ||
         !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
      {
         return null;
      }

      var contentLength = response.Content.Headers.ContentLength;

      if(contentLength >
         WebPageFetchDefaults.ImageOcrMaximumBytes)
      {
         return null;
      }

      await using var source = await response.Content.ReadAsStreamAsync(
         cancellationToken
      );
      await using var destination = new MemoryStream();
      var buffer = new byte[81920];

      while(true)
      {
         var bytesRead = await source.ReadAsync(
            buffer,
            cancellationToken
         );

         if(bytesRead == 0)
         {
            break;
         }

         if(destination.Length + bytesRead >
            WebPageFetchDefaults.ImageOcrMaximumBytes)
         {
            return null;
         }

         await destination.WriteAsync(
            buffer.AsMemory(0, bytesRead),
            cancellationToken
         );
      }

      return destination.ToArray();
   }

   private static async Task<string> RunTesseractAsync(
      string imagePath,
      CancellationToken cancellationToken
   )
   {
      using var timeoutSource =
         CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeoutSource.CancelAfter(WebPageFetchDefaults.ImageOcrTimeout);

      using var process = new Process
      {
         StartInfo = new ProcessStartInfo
         {
            FileName = WebPageFetchDefaults.ImageOcrExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
         }
      };
      process.StartInfo.ArgumentList.Add(imagePath);
      process.StartInfo.ArgumentList.Add("stdout");
      process.StartInfo.ArgumentList.Add("-l");
      process.StartInfo.ArgumentList.Add(
         WebPageFetchDefaults.ImageOcrLanguage
      );
      process.StartInfo.ArgumentList.Add("--psm");
      process.StartInfo.ArgumentList.Add(
         WebPageFetchDefaults.ImageOcrPageSegmentationMode.ToString(
            CultureInfo.InvariantCulture
         )
      );
      process.StartInfo.ArgumentList.Add("tsv");

      process.Start();
      var standardOutputTask = process.StandardOutput.ReadToEndAsync(
         timeoutSource.Token
      );
      var standardErrorTask = process.StandardError.ReadToEndAsync(
         timeoutSource.Token
      );

      try
      {
         await process.WaitForExitAsync(timeoutSource.Token);
      }
      catch(OperationCanceledException)
      {
         if(!process.HasExited)
         {
            process.Kill(entireProcessTree: true);
         }

         cancellationToken.ThrowIfCancellationRequested();
         throw new TimeoutException("Image OCR timed out.");
      }

      var standardOutput = await standardOutputTask;
      var standardError = await standardErrorTask;

      if(process.ExitCode != 0)
      {
         throw new InvalidOperationException(
            $"Tesseract failed: {standardError.Trim()}"
         );
      }

      return standardOutput;
   }

   private static OcrWord? ParseWord(string row)
   {
      var columns = row.Split('\t');

      if(columns.Length < 12 ||
         columns[0] != "5" ||
         !TryParseInt(columns[1], out var page) ||
         !TryParseInt(columns[2], out var block) ||
         !TryParseInt(columns[3], out var paragraph) ||
         !TryParseInt(columns[4], out var line) ||
         !TryParseInt(columns[6], out var left) ||
         !TryParseInt(columns[7], out var top) ||
         !TryParseInt(columns[8], out var width) ||
         !TryParseInt(columns[9], out var height) ||
         !double.TryParse(
            columns[10],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var confidence
         ))
      {
         return null;
      }

      var text = string.Join('\t', columns.Skip(11)).Trim();

      if(string.IsNullOrWhiteSpace(text) || confidence < 0d)
      {
         return null;
      }

      return new OcrWord(
         page,
         block,
         paragraph,
         line,
         left,
         top,
         width,
         height,
         confidence,
         text
      );
   }

   private static string FormatLine(IReadOnlyList<OcrWord> words)
   {
      if(words.Count == 0)
      {
         return string.Empty;
      }

      var medianHeight = words
         .Select(word => word.Height)
         .OrderBy(height => height)
         .ElementAt(words.Count / 2);
      var columnGap = Math.Max(24d, medianHeight * 2d);
      var parts = new List<string> { words[0].Text };

      for(var index = 1; index < words.Count; index++)
      {
         var previous = words[index - 1];
         var current = words[index];
         var gap = current.Left - (previous.Left + previous.Width);

         parts.Add(gap >= columnGap ? " | " : " ");
         parts.Add(current.Text);
      }

      return string.Concat(parts);
   }

   private static bool TryParseInt(string value, out int result)
   {
      return int.TryParse(
         value,
         NumberStyles.Integer,
         CultureInfo.InvariantCulture,
         out result
      );
   }

   private static string GetExtension(Uri imageUri)
   {
      var extension = Path.GetExtension(imageUri.AbsolutePath);

      return extension.Length is > 0 and <= 5
         ? extension
         : ".img";
   }

   private static void TryDeleteTemporaryFile(string path)
   {
      try
      {
         File.Delete(path);
      }
      catch(IOException)
      {
      }
      catch(UnauthorizedAccessException)
      {
      }
   }

   private sealed record OcrWord(
      int Page,
      int Block,
      int Paragraph,
      int Line,
      int Left,
      int Top,
      int Width,
      int Height,
      double Confidence,
      string Text
   );

   private sealed record OcrLine(int Top, int Left, string Text);
}
