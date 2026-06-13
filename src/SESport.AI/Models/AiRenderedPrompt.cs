namespace SESport.AI.Models;

public sealed record AiRenderedPrompt(
   string? SystemPrompt,
   string UserPrompt
)
{
   public string ToPromptText()
   {
      return string.Join(
         Environment.NewLine + Environment.NewLine,
         new[]
         {
            SystemPrompt?.Trim(),
            UserPrompt.Trim()
         }
            .Where(value => !string.IsNullOrWhiteSpace(value))
      );
   }
}
