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

public sealed record PublicStatisticsSportOption(
   string SportId,
   string SportName,
   int ParticipantCount
);

public sealed record PublicStatisticsSportSnapshot(
   int ParticipantCount,
   IReadOnlyList<PublicStatisticsSportOption> Options
);
