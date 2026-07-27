namespace SESport.Core.Configuration;

public sealed record WebStatsOptions
{
   public string ReportDirectory { get; init; } = string.Empty;
}
