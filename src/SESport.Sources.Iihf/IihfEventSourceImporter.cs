namespace SESport.Sources.Iihf;

public sealed class IihfEventSourceImporter(
   IIihfScheduleClient scheduleClient,
   IihfCompetitionSource competitionSource
) : IEventSourceImporter
{
   public Source Source { get; } = new(
      new SourceId("source:iihf"),
      "IIHF"
   );

   public Task<ImportRun> ImportEventsAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      return ImportEventsCoreAsync(request, cancellationToken);
   }

   private async Task<ImportRun> ImportEventsCoreAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      var games = await scheduleClient.GetGamesAsync(
         request,
         cancellationToken
      );
      var importedEvents = games
         .Select(ToImportedEvent)
         .ToList();
      var issues = CreateIssues(importedEvents);

      var importRun = new ImportRun(
         new ImportRunId(CreateImportRunId(request)),
         Source,
         ImportRunStatus.Completed,
         request.StartsAfter,
         request.StartsAfter,
         importedEvents,
         issues
      );

      return importRun;
   }

   private ImportedEvent ToImportedEvent(IihfGame game)
   {
      var iceHockey = new ImportedSport(
         new ExternalEntityId("ice-hockey"),
         "Ice hockey"
      );
      var competition = new ImportedCompetition(
         new ExternalEntityId(game.CompetitionExternalId),
         game.CompetitionName,
         iceHockey
      );

      return new ImportedEvent(
         Source,
         new ExternalEntityId(game.ExternalId),
         $"{game.HomeTeam.CountryName} vs {game.AwayTeam.CountryName}",
         competition,
         game.StartsAt,
         game.Stage,
         [
            ToImportedParticipant(game.HomeTeam),
            ToImportedParticipant(game.AwayTeam)
         ]
      );
   }

   private static ImportedParticipant ToImportedParticipant(IihfTeam team)
   {
      return new ImportedParticipant(
         new ExternalEntityId(team.ExternalId),
         team.ParticipantName,
         ParticipantKind.NationalTeam,
         new ImportedCountry(
            new ExternalEntityId(team.CountryCode.ToLowerInvariant()),
            team.CountryCode,
            team.CountryName
         )
      );
   }

   private string CreateImportRunId(ImportRequest request)
   {
      var eventPath = competitionSource.EventPath.Replace("/", "-");

      return $"import-run:iihf:{eventPath}:" +
         $"{request.StartsAfter:yyyyMMddHHmmss}";
   }

   private IReadOnlyCollection<ImportIssue> CreateIssues(
      IReadOnlyCollection<ImportedEvent> importedEvents
   )
   {
      if (importedEvents.Count > 0)
      {
         return [];
      }

      return
      [
         new ImportIssue(
            ImportIssueKind.NoEventsFound,
            ImportIssueSeverity.Warning,
            null,
            $"No IIHF events were found for {competitionSource.EventPath}."
         )
      ];
   }
}
