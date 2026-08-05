using SESport.Core.Members;

namespace SESport.Core.Tests.Members;

public sealed class MemberEmailNormalizerTests
{
   [Fact]
   public void NormalizeTrimsAndUsesCaseInsensitiveLookupValue()
   {
      var normalized = MemberEmailNormalizer.Normalize(
         "  Person@Example.COM "
      );

      Assert.Equal("person@example.com", normalized);
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("not-an-email")]
   public void NormalizeRejectsInvalidAddresses(string? email)
   {
      Assert.Null(MemberEmailNormalizer.Normalize(email));
   }
}
