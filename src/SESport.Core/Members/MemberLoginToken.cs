using System.Security.Cryptography;
using System.Text;

namespace SESport.Core.Members;

public static class MemberLoginToken
{
   private const int TokenByteCount = 32;

   public static string Generate()
   {
      var bytes = RandomNumberGenerator.GetBytes(TokenByteCount);
      return Convert.ToBase64String(bytes)
         .TrimEnd('=')
         .Replace('+', '-')
         .Replace('/', '_');
   }

   public static string Hash(string token)
   {
      var bytes = Encoding.UTF8.GetBytes(token);
      return Convert.ToHexString(SHA256.HashData(bytes));
   }
}
