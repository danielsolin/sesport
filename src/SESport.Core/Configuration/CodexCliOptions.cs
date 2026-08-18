namespace SESport.Core.Configuration;

public sealed class CodexCliOptions
{
   public const string SectionName = "CodexCli";

   public string ExecutablePath { get; set; } = "codex";

   public string? WorkingDirectory { get; set; }

   public int TimeoutSeconds { get; set; } = 1200;

   public bool WebToolsEnabled { get; set; } = true;

   public string WebToolsProjectPath { get; set; } =
      "tools/SESport.WebTools/SESport.WebTools.csproj";

   public int WebToolsTimeoutSeconds { get; set; } = 300;
}
