namespace SESport.AI.WebPages;

internal static class WebPageFetchLogging
{
   internal static string SummarizeException(Exception exception)
   {
      var messages = new List<string>();
      Exception? current = exception;

      for(var depth = 0;
         current is not null && depth < 3;
         depth++)
      {
         var message = GetFirstLine(current.Message);
         messages.Add(
            string.IsNullOrWhiteSpace(message)
               ? current.GetType().Name
               : message
         );
         current = current.InnerException;
      }

      return string.Join(" | ", messages);
   }

   private static string GetFirstLine(string message)
   {
      var lineBreak = message.IndexOf('\n');
      return (lineBreak < 0 ? message : message[..lineBreak]).Trim();
   }
}
