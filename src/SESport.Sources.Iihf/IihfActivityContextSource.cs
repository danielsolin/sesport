namespace SESport.Sources.Iihf;

public sealed record IihfActivityContextSource(
   string ActivityContext,
   string EventPath,
   Uri StatsUri
);
