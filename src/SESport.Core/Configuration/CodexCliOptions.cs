namespace SESport.Core.Configuration;

public sealed class CodexCliOptions
{
   public const string SectionName = "CodexCli";

   public string ExecutablePath { get; set; } = "codex";

   public string? WorkingDirectory { get; set; }

   public int TimeoutSeconds { get; set; } = 3600;
}
