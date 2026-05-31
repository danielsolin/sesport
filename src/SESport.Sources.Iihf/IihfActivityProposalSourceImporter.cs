namespace SESport.Sources.Iihf;

public sealed class IihfActivityProposalSourceImporter(
   IIihfScheduleClient scheduleClient,
   IihfActivityContextSource activityContextSource
) : IActivityProposalSourceImporter
{
   public Source Source { get; } = new(
      new SourceId("source:iihf"),
      "IIHF"
   );

   public Task<ImportRun> ImportActivityProposalsAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      return ImportActivityProposalsCoreAsync(request, cancellationToken);
   }

   private async Task<ImportRun> ImportActivityProposalsCoreAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   )
   {
      var games = await scheduleClient.GetGamesAsync(
         request,
         cancellationToken
      );
      var proposals = games
         .Select(ToActivityProposal)
         .ToList();
      var issues = CreateIssues(proposals);

      var importRun = new ImportRun(
         new ImportRunId(CreateImportRunId(request)),
         Source,
         ImportRunStatus.Completed,
         request.StartsAfter,
         request.StartsAfter,
         proposals,
         issues
      );

      return importRun;
   }

   private ActivityProposal ToActivityProposal(IihfGame game)
   {
      var eventName = CreateEventName(game);
      var iceHockey = new ImportedSport(
         new ExternalEntityId("ice-hockey"),
         "Ice hockey"
      );
      var entityLinks = new[]
      {
         game.HomeTeam,
         game.AwayTeam
      }
      .Where(team => team is not null)
      .Select(team => ToProposalEntityLink(team!))
      .ToList();

      return new ActivityProposal(
         new ActivityProposalId($"activity-proposal:iihf:{game.ExternalId}"),
         ActivityProposalProducerType.WebImport,
         Source,
         new ExternalEntityId(game.ExternalId),
         $"iihf:{game.ExternalId}",
         eventName,
         $"{game.CompetitionName}: {game.Stage}",
         null,
         ActivityType.Match,
         iceHockey,
         game.CompetitionName,
         ActivityTime.ExactStart(game.StartsAt),
         entityLinks,
         [
            new ActivityProposalEvidence(
               Source,
               activityContextSource.StatsUri,
               game.CompetitionName,
               DateTimeOffset.UtcNow,
               $"IIHF schedule entry for {eventName}.",
               null
            )
         ],
         Confidence: 1.0m,
         ActivityProposalStatus.Pending,
         null,
         null
      );
   }

   private static string CreateEventName(IihfGame game)
   {
      var homeName = game.HomeTeam?.CountryName ?? "TBD";
      var awayName = game.AwayTeam?.CountryName ?? "TBD";

      return $"{homeName} vs {awayName}";
   }

   private static ActivityProposalEntityLink ToProposalEntityLink(
      IihfTeam team
   )
   {
      return new ActivityProposalEntityLink(
         ToEntityId($"iihf:team:{team.ExternalId}"),
         ActivityEntityRole.CompetesIn,
         $"{team.TeamName} is listed as a team by IIHF.",
         team.TeamName,
         Confidence: 1.0m
      );
   }

   private static EntityId ToEntityId(string stableKey)
   {
      var bytes = System.Text.Encoding.UTF8.GetBytes(stableKey);
      var hash = System.Security.Cryptography.MD5.HashData(bytes);

      return new EntityId(new Guid(hash));
   }

   private string CreateImportRunId(ImportRequest request)
   {
      var eventPath = activityContextSource.EventPath.Replace("/", "-");

      return $"import-run:iihf:{eventPath}:" +
         $"{request.StartsAfter:yyyyMMddHHmmss}";
   }

   private IReadOnlyCollection<ImportIssue> CreateIssues(
      IReadOnlyCollection<ActivityProposal> proposals
   )
   {
      if (proposals.Count > 0)
      {
         return [];
      }

      return
      [
         new ImportIssue(
            ImportIssueKind.NoEventsFound,
            ImportIssueSeverity.Warning,
            null,
            $"No IIHF events were found for {activityContextSource.EventPath}."
         )
      ];
   }
}
