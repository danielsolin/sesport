namespace SESport.Web.Configuration;

public sealed record WebStatsOptions
{
   public string ReportDirectory { get; init; } = string.Empty;
}
