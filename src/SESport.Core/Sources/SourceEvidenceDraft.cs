using System.Text.Json.Serialization;

namespace SESport.Core.Sources;

public sealed record SourceEvidenceDraft(
   [property: JsonPropertyName("url")] string Url,
   [property: JsonPropertyName("title")] string? Title,
   [property: JsonPropertyName("excerpt")] string? Excerpt
);
