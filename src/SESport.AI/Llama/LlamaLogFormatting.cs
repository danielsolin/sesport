namespace SESport.AI.Llama;

internal static class LlamaLogFormatting
{
   public static string Truncate(string value, int maxLength)
   {
      if(value.Length <= maxLength)
      {
         return value;
      }

      return value[..maxLength] + "...";
   }
}
