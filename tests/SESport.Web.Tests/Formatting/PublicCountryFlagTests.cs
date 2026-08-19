using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class PublicCountryFlagTests
{
   [Theory]
   [InlineData("de")]
   [InlineData("DE")]
   [InlineData("pl")]
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
   [InlineData("int")]
   [InlineData("unknown")]
   public void GetPathReturnsNullForUnknownCountry(string? countryId)
   {
      var path = PublicCountryFlag.GetPath(countryId);

      Assert.Null(path);
   }
}
