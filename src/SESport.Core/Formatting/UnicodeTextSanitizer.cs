using System.Text;

namespace SESport.Core.Formatting;

public static class UnicodeTextSanitizer
{
   public static string Sanitize(string value)
   {
      var sanitized = new StringBuilder(value.Length);

      for(var index = 0; index < value.Length; index++)
      {
         var character = value[index];

         if(character == '\0')
         {
            continue;
         }

         if(char.IsHighSurrogate(character))
         {
            if(index + 1 < value.Length &&
               char.IsLowSurrogate(value[index + 1]))
            {
               sanitized.Append(character);
               sanitized.Append(value[++index]);
            }
            else
            {
               sanitized.Append('\uFFFD');
            }

            continue;
         }

         sanitized.Append(
            char.IsLowSurrogate(character) ? '\uFFFD' : character
         );
      }

      return sanitized.ToString();
   }
}
