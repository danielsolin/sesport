namespace SESport.Core.Formatting;

public static class TimeTextFormatter
{
   public static string FormatTimeOnlyText(string? timeText)
   {
      if(string.IsNullOrEmpty(timeText))
      {
         return string.Empty;
      }

      if(!timeText.Contains(' '))
      {
         return timeText;
      }

      return timeText.Split(' ')[1];
   }
}
