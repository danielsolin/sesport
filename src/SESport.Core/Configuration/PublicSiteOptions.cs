namespace SESport.Core.Configuration;

public sealed record PublicSiteOptions
{
   public string CanonicalHomeUrl { get; init; } =
      "https://sesport.se/";

   public string PageDescription { get; init; } =
      "Svenskar i internationell sport på TV";

   public int MaxVisibleParticipants { get; init; } = 10;
}
