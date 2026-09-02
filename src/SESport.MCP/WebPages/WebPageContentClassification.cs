namespace SESport.AI.WebPages;

/// <summary>
/// Shared content-quality classification used by every transport. Only
/// <see cref="Usable"/> is a clean success.
/// </summary>
internal enum WebPageContentClassification
{
   Usable,
   Partial,
   NeedsRendering,
   Empty,
   Blocked,
   NotFound
}
