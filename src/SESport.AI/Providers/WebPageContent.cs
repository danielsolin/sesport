namespace SESport.AI.Providers;

public sealed record WebPageContent(
   string Title,
   string Url,
   DateTimeOffset? PublishedAt,
   string MainText
);
