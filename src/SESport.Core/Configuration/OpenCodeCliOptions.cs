namespace SESport.Core.Configuration;

public sealed class OpenCodeCliOptions
{
   public const string SectionName = "OpenCodeCli";

   public string ExecutablePath { get; set; } = "opencode";

   public string? WorkingDirectory { get; set; }

   public int TimeoutSeconds { get; set; } = 3600;
}
