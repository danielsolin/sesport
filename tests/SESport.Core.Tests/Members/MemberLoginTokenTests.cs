using SESport.Core.Members;

namespace SESport.Core.Tests.Members;

public sealed class MemberLoginTokenTests
{
   [Fact]
   public void GeneratedTokenIsUrlSafeAndHashIsDeterministic()
   {
      var token = MemberLoginToken.Generate();

      Assert.NotEmpty(token);
      Assert.DoesNotContain("+", token);
      Assert.DoesNotContain("/", token);
      Assert.DoesNotContain("=", token);
      Assert.Equal(
         MemberLoginToken.Hash(token),
         MemberLoginToken.Hash(token)
      );
      Assert.NotEqual(token, MemberLoginToken.Hash(token));
   }
}
