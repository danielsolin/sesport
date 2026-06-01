using System.Security.Cryptography;
using System.Text;

namespace SESport.Core.Identifiers;

public static class DeterministicGuid
{
   public static Guid Create(string value)
   {
      var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
      bytes[6] = (byte)((bytes[6] & 0x0f) | 0x30);
      bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
      return new Guid(bytes);
   }
}
