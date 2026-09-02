namespace SESport.AI.WebPages;

internal sealed record WebPageAssessment(
   WebPageContentClassification Classification,
   string? Reason,
   string? BlockSignature,
   string? SoftNotFoundSignature
)
{
   internal static readonly WebPageAssessment Usable = new(
      WebPageContentClassification.Usable,
      null,
      null,
      null
   );

   internal bool IsSuccess =>
      Classification == WebPageContentClassification.Usable;
}
