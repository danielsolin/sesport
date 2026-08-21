namespace SESport.Core.Configuration;

public sealed record PublicStatisticsOptions
{
   public DateOnly FirstAvailableMonth { get; init; } =
      new(2026, 6, 1);

   public int TopParticipantLimit { get; init; } = 10;
}
