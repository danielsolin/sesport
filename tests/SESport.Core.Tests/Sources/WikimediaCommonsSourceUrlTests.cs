using SESport.Core.Sources;

namespace SESport.Core.Tests.Sources;

public sealed class WikimediaCommonsSourceUrlTests
{
   [Fact]
   public void TryParseReadsAndCanonicalizesFileRevisionUrl()
   {
      var isValid = WikimediaCommonsSourceUrl.TryParse(
         " https://commons.wikimedia.org/w/index.php?" +
         "title=File:Alexander_Isak_-_Sweden_-_Greece21_" +
         "%28cropped%29.jpg&oldid=1250198069 ",
         out var reference
      );

      Assert.True(isValid);
      Assert.Equal(
         "File:Alexander_Isak_-_Sweden_-_Greece21_(cropped).jpg",
         reference.FileTitle
      );
      Assert.Equal(1250198069, reference.RevisionId);
      Assert.Equal(
         "https://commons.wikimedia.org/w/index.php?" +
         "title=File:Alexander_Isak_-_Sweden_-_Greece21_" +
         "%28cropped%29.jpg&oldid=1250198069",
         reference.Url
      );
   }

   [Theory]
   [InlineData("https://commons.wikimedia.org/wiki/File:Example.jpg")]
   [InlineData(
      "https://commons.wikimedia.org/w/index.php?" +
      "title=File:Example.jpg"
   )]
   [InlineData(
      "https://commons.wikimedia.org/w/index.php?" +
      "title=File:Example.jpg&oldid=not-a-number"
   )]
   [InlineData(
      "https://en.wikipedia.org/w/index.php?" +
      "title=File:Example.jpg&oldid=123"
   )]
   [InlineData(
      "https://commons.wikimedia.org/w/index.php?" +
      "title=Category:Example&oldid=123"
   )]
   public void TryParseRejectsIncompatibleUrls(string sourceUrl)
   {
      Assert.False(
         WikimediaCommonsSourceUrl.TryParse(sourceUrl, out _)
      );
   }
}
