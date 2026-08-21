namespace SESport.Data.Models;

public sealed record PublicStatisticsSnapshot(
   int ParticipantCount,
   IReadOnlyList<PublicStatisticsLeader> Leaders
);

public sealed record PublicStatisticsLeader(
   int Rank,
   string Name,
   string SportNames,
   int Points
);
