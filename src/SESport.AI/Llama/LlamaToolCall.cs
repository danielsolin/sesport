namespace SESport.AI.Llama;

internal sealed record LlamaToolCall(
   string Id,
   string Name,
   string Arguments
);
