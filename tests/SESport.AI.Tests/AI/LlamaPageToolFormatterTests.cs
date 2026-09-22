using SESport.AI.Llama;
using SESport.AI.WebPages;

namespace SESport.Core.Tests.AI;

public class LlamaPageToolFormatterTests
{
   [Fact]
   public void FormatPageContentTextOmitsNonPdfRelevantLinks()
   {
      var output = LlamaPageToolFormatter.FormatPageContentText(
         "Page URL",
         "https://example.test/article",
         "Title",
         "https://example.test/article",
         null,
         null,
         [],
         [
            new WebPageRelevantLink(
               "Entry list",
               "https://example.test/entries"
            )
         ],
         null,
         null,
         "Page body text."
      );

      Assert.DoesNotContain("Relevant links:", output);
      Assert.DoesNotContain("https://example.test/entries", output);
      Assert.Contains("Page text:", output);
   }

   [Fact]
   public void FormatPageContentTextPlacesPdfLinksBeforePageText()
   {
      var output = LlamaPageToolFormatter.FormatPageContentText(
         "Page URL",
         "https://example.test/article",
         "Title",
         "https://example.test/article",
         null,
         null,
         [],
         [
            new WebPageRelevantLink(
               "Pole Vault- men",
               "https://example.test/files/men-pole-vault.pdf"
            )
         ],
         null,
         null,
         "Page body text."
      );

      Assert.Contains("PDF links:", output);
      Assert.Contains(
         "- Pole Vault- men: https://example.test/files/men-pole-vault.pdf",
         output
      );
      Assert.True(
         output.IndexOf("PDF links:", StringComparison.Ordinal) <
         output.IndexOf("Page text:", StringComparison.Ordinal)
      );
   }
}
