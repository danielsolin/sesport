using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace SESport.AI.WebPages;

internal static class PdfContentExtractor
{
   internal static string ExtractText(PdfDocument pdfDocument)
   {
      var pages = pdfDocument
         .GetPages()
         .Select(ExtractPdfPageText)
         .Where(text => !string.IsNullOrWhiteSpace(text))
         .Select(text => text.Trim());

      return string.Join(Environment.NewLine, pages);
   }

   private const double PdfColumnGapThreshold = 12d;

   private static string ExtractPdfPageText(Page page)
   {
      // PDF content order often follows draw order instead of visual layout.
      // Rebuild the text from positions so aligned table cells stay aligned.
      var words = page
         .GetWords()
         .Where(word => !string.IsNullOrWhiteSpace(word.Text))
         .OrderByDescending(word => word.BoundingBox.Top)
         .ThenBy(word => word.BoundingBox.Left)
         .ToList();

      if(words.Count == 0)
      {
         return ContentOrderTextExtractor.GetText(page, true).Trim();
      }

      var rows = GroupPdfWordsIntoRows(words);
      var lines = rows
         .Select(FormatPdfRow)
         .Where(line => !string.IsNullOrWhiteSpace(line))
         .ToList();

      if(lines.Count == 0)
      {
         return ContentOrderTextExtractor.GetText(page, true).Trim();
      }

      return string.Join(Environment.NewLine, lines);
   }

   private static IReadOnlyList<IReadOnlyList<Word>> GroupPdfWordsIntoRows(
      IReadOnlyList<Word> words
   )
   {
      var rows = new List<PdfTextRow>();
      var rowTolerance = GetPdfRowTolerance(words);

      foreach(var word in words)
      {
         if(rows.Count == 0 || !rows[^1].CanAccept(word, rowTolerance))
         {
            rows.Add(new PdfTextRow());
         }

         rows[^1].Add(word);
      }

      return rows.Select(row => row.GetWords()).ToList();
   }

   private static double GetPdfRowTolerance(IReadOnlyList<Word> words)
   {
      var heights = words
         .Select(word => word.BoundingBox.Height)
         .Where(height => height > 0d)
         .OrderBy(height => height)
         .ToList();

      if(heights.Count == 0)
      {
         return 3d;
      }

      return Math.Max(3d, GetMedian(heights) * 0.35d);
   }

   private static string FormatPdfRow(IReadOnlyList<Word> words)
   {
      var cells = SplitPdfRowIntoCells(words);

      return string.Join(" | ", cells);
   }

   private static IReadOnlyList<string> SplitPdfRowIntoCells(
      IReadOnlyList<Word> words
   )
   {
      var cells = new List<string>();
      var cellWords = new List<Word>();

      foreach(var word in words.OrderBy(word => word.BoundingBox.Left))
      {
         if(cellWords.Count > 0)
         {
            var gap = word.BoundingBox.Left -
               cellWords[^1].BoundingBox.Right;

            if(gap > PdfColumnGapThreshold)
            {
               cells.Add(JoinPdfWords(cellWords));
               cellWords.Clear();
            }
         }

         cellWords.Add(word);
      }

      if(cellWords.Count > 0)
      {
         cells.Add(JoinPdfWords(cellWords));
      }

      return cells;
   }

   private static string JoinPdfWords(IReadOnlyCollection<Word> words)
   {
      return string.Join(
         " ",
         words.Select(word => word.Text.Trim())
      ).Trim();
   }

   private static double GetMedian(IReadOnlyList<double> values)
   {
      if(values.Count == 0)
      {
         return 0d;
      }

      var middleIndex = values.Count / 2;

      if(values.Count % 2 == 1)
      {
         return values[middleIndex];
      }

      return (values[middleIndex - 1] + values[middleIndex]) / 2d;
   }

   private sealed class PdfTextRow
   {
      private readonly List<Word> words = [];

      public IReadOnlyList<Word> GetWords()
      {
         return words;
      }

      public void Add(Word word)
      {
         words.Add(word);
         CenterSum += GetVerticalCenter(word);
      }

      public bool CanAccept(Word word, double tolerance)
      {
         if(words.Count == 0)
         {
            return true;
         }

         var rowCenter = CenterSum / words.Count;
         return Math.Abs(GetVerticalCenter(word) - rowCenter) <= tolerance;
      }

      private static double GetVerticalCenter(Word word)
      {
         return (
            word.BoundingBox.Top +
            word.BoundingBox.Bottom
         ) / 2d;
      }

      private double CenterSum { get; set; }
   }

   internal static string ExtractTitle(
      PdfDocument pdfDocument,
      Uri absoluteUrl
   )
   {
      var title = pdfDocument.Information.Title?.Trim();

      if(!string.IsNullOrWhiteSpace(title))
      {
         return title;
      }

      var fileName = Path.GetFileNameWithoutExtension(
         absoluteUrl.AbsolutePath
      );

      if(!string.IsNullOrWhiteSpace(fileName))
      {
         return fileName;
      }

      return absoluteUrl.ToString();
   }
}
