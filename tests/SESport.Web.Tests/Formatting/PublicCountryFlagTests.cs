using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class PublicCountryFlagTests
{
   [Theory]
   [InlineData("al")]
   [InlineData("ba")]
   [InlineData("bg")]
   [InlineData("ch")]
   [InlineData("cy")]
   [InlineData("cz")]
   [InlineData("dk")]
   [InlineData("de")]
   [InlineData("DE")]
   [InlineData("es")]
   [InlineData("fi")]
   [InlineData("fr")]
   [InlineData("gr")]
   [InlineData("hr")]
   [InlineData("hu")]
   [InlineData("il")]
   [InlineData("is")]
   [InlineData("it")]
   [InlineData("jp")]
   [InlineData("lt")]
   [InlineData("nl")]
   [InlineData("no")]
   [InlineData("pl")]
   [InlineData("pt")]
   [InlineData("ro")]
   [InlineData("rs")]
   [InlineData("se")]
   [InlineData("sk")]
   [InlineData("tr")]
   [InlineData("uk")]
   [InlineData("us")]
   [InlineData("eu")]
   [InlineData("INT")]
   public void GetPathReturnsPathForKnownCountry(string countryId)
   {
      var path = PublicCountryFlag.GetPath(countryId);

      Assert.Equal(
         $"/images/flags/{countryId.ToLowerInvariant()}.svg",
         path
      );
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("xx")]
   [InlineData("unknown")]
   public void GetPathReturnsNullForUnknownCountry(string? countryId)
   {
      var path = PublicCountryFlag.GetPath(countryId);

      Assert.Null(path);
   }
}
