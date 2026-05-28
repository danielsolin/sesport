namespace SESport.Sources.Iihf;

public sealed class IihfEventSourceImporter(
   IIihfScheduleClient scheduleClient
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

      var importRun = new ImportRun(
         new ImportRunId(CreateImportRunId(request)),
         Source,
         ImportRunStatus.Completed,
         request.StartsAfter,
         request.StartsAfter,
         importedEvents,
         []
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

   private static string CreateImportRunId(ImportRequest request)
   {
      return $"import-run:iihf:{request.StartsAfter:yyyyMMddHHmmss}";
   }
}
