namespace SESport.Core.Tests.Ingestion;

public class CollectionRefreshPlannerTests
{
   [Fact]
   public void PlannerReturnsNoRefreshWhenEventsHaveExpectedParticipants()
   {
      var planner = CreateIceHockeyPlanner();
      var importRun = CreateImportRun(
         CreateEvent(
            new DateTimeOffset(2026, 5, 30, 15, 20, 0, TimeSpan.Zero),
            participantCount: 2
         )
      );

      var nextRefreshAt = planner.GetNextUsefulRefreshAt(importRun);

      Assert.Null(nextRefreshAt);
   }

   [Fact]
   public void PlannerReturnsEarliestUsefulRefreshForUnresolvedEvents()
   {
      var planner = CreateIceHockeyPlanner();
      var startsAt = new DateTimeOffset(
         2026,
         5,
         30,
         15,
         20,
         0,
         TimeSpan.Zero
      );
      var importRun = CreateImportRun(
         CreateEvent(startsAt, participantCount: 1)
      );

      var nextRefreshAt = planner.GetNextUsefulRefreshAt(importRun);

      Assert.Equal(
         startsAt.AddMinutes(165),
         nextRefreshAt
      );
   }

   private static CollectionRefreshPlanner CreateIceHockeyPlanner()
   {
      return new CollectionRefreshPlanner(
         new SportCollectionProfile(
            new ExternalEntityId("ice-hockey"),
            TimeSpan.FromMinutes(150),
            TimeSpan.FromMinutes(15),
            ExpectedParticipantCount: 2
         )
      );
   }

   private static ImportRun CreateImportRun(params ImportedEvent[] events)
   {
      return new ImportRun(
         new ImportRunId("import-run:test"),
         new Source(new SourceId("source:test"), "Test"),
         ImportRunStatus.Completed,
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow,
         events,
         []
      );
   }

   private static ImportedEvent CreateEvent(
      DateTimeOffset startsAt,
      int participantCount
   )
   {
      var participants = Enumerable
         .Range(1, participantCount)
         .Select(index => new ImportedParticipant(
            new ExternalEntityId($"participant:{index}"),
            $"Participant {index}",
            ParticipantKind.NationalTeam,
            null
         ))
         .ToList();

      return new ImportedEvent(
         new Source(new SourceId("source:test"), "Test"),
         new ExternalEntityId($"event:{startsAt:yyyyMMddHHmmss}"),
         "Test event",
         new ImportedCompetition(
            new ExternalEntityId("competition:test"),
            "Test competition",
            new ImportedSport(
               new ExternalEntityId("ice-hockey"),
               "Ice hockey"
            )
         ),
         startsAt,
         "Scheduled",
         participants
      );
   }
}
