namespace SESport.AI.Llama;

internal sealed record LlamaPageTarget(
   string ReferenceLabel,
   string ReferenceValue,
   string Url,
   string Title,
   string? SearchSnippet
);
