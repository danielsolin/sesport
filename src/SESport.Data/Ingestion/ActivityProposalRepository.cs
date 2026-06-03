using Npgsql;

using SESport.Core.Domain;
using SESport.Core.Identifiers;
using SESport.Core.Ingestion;
using SESport.Core.Sources;

namespace SESport.Data.Ingestion;

public sealed class ActivityProposalRepository : IAsyncDisposable
{
   private readonly NpgsqlDataSource dataSource;
   private readonly bool ownsDataSource;

   public ActivityProposalRepository(NpgsqlDataSource dataSource)
   {
      this.dataSource = dataSource;
   }

   private ActivityProposalRepository(
      NpgsqlDataSource dataSource,
      bool ownsDataSource
   )
   {
      this.dataSource = dataSource;
      this.ownsDataSource = ownsDataSource;
   }

   public static ActivityProposalRepository Connect(string connectionString)
   {
      return new ActivityProposalRepository(
         NpgsqlDataSource.Create(connectionString),
         ownsDataSource: true
      );
   }

   public async ValueTask DisposeAsync()
   {
      if(ownsDataSource)
      {
         await dataSource.DisposeAsync();
      }
   }

   public async Task<int> SaveAsync(
      IReadOnlyCollection<ActivityProposal> proposals,
      CancellationToken cancellationToken
   )
   {
      if(proposals.Count == 0)
      {
         return 0;
      }

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      var sources = proposals
         .Select(proposal => proposal.Source)
         .Concat(proposals
            .SelectMany(proposal => proposal.Evidence)
            .Select(evidence => evidence.Source))
         .DistinctBy(source => source.Id.Value)
         .ToList();

      foreach(var source in sources)
      {
         await UpsertSourceAsync(
            connection,
            transaction,
            source,
            cancellationToken
         );
      }

      foreach(var proposal in proposals)
      {
         await UpsertProposalAsync(
            connection,
            transaction,
            proposal,
            cancellationToken
         );
         await ReplaceProposalLinksAsync(
            connection,
            transaction,
            proposal,
            cancellationToken
         );
         await ReplaceProposalEvidenceAsync(
            connection,
            transaction,
            proposal,
            cancellationToken
         );
      }

      await transaction.CommitAsync(cancellationToken);
      return proposals.Count;
   }

   private static async Task UpsertSourceAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Source source,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into sources (id, name)
         values (@id, @name)
         on conflict (id) do update
         set
            name = excluded.name,
            updated_at = now()
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", source.Id.Value);
      command.Parameters.AddWithValue("name", source.Name);

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task UpsertProposalAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      ActivityProposal proposal,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_proposals (
            id, producer_type_id, producer, source_id, external_id,
            fingerprint, title, description, raw_content, activity_type_id,
            sport_id, context, activity_date, local_start_time, starts_at,
            time_zone_id, confidence, status_id, reject_reason_id,
            reject_comment, activity_id, prompt
         )
         values (
            @id, @producer_type_id, @producer, @source_id, @external_id,
            @fingerprint, @title, @description, @raw_content,
            @activity_type_id, @sport_id, @context, @activity_date,
            @local_start_time, @starts_at, @time_zone_id, @confidence,
            @status_id, @reject_reason_id, @reject_comment, @activity_id,
            @prompt
         )
         on conflict (id) do update
         set
            producer_type_id = excluded.producer_type_id,
            producer = excluded.producer,
            source_id = excluded.source_id,
            external_id = excluded.external_id,
            fingerprint = excluded.fingerprint,
            title = excluded.title,
            description = excluded.description,
            raw_content = excluded.raw_content,
            activity_type_id = excluded.activity_type_id,
            sport_id = excluded.sport_id,
            context = excluded.context,
            activity_date = excluded.activity_date,
            local_start_time = excluded.local_start_time,
            starts_at = excluded.starts_at,
            time_zone_id = excluded.time_zone_id,
            confidence = excluded.confidence,
            status_id = excluded.status_id,
            reject_reason_id = excluded.reject_reason_id,
            reject_comment = excluded.reject_comment,
            activity_id = excluded.activity_id,
            prompt = excluded.prompt,
            updated_at = now()
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      AddProposalParameters(command, proposal);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static void AddProposalParameters(
      NpgsqlCommand cmd,
      ActivityProposal ap
   )
   {
      cmd.Parameters.AddWithValue("id", ap.Id.Value);
      cmd.Parameters.AddWithValue(
         "producer_type_id",
         ap.ProducerType.ToString()
      );
      cmd.Parameters.AddWithValue(
         "producer",
         (object?)ap.Producer ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue("source_id", ap.Source.Id.Value);
      cmd.Parameters.AddWithValue(
         "external_id",
         (object?)ap.ExternalId?.Value ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue("fingerprint", ap.Fingerprint);
      cmd.Parameters.AddWithValue("title", ap.Title);
      cmd.Parameters.AddWithValue(
         "description",
         (object?)ap.Description ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue(
         "raw_content",
         (object?)ap.RawContent ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue(
         "activity_type_id",
         ap.Type.ToString()
      );
      cmd.Parameters.AddWithValue("sport_id", ap.Sport.ExternalId.Value);
      cmd.Parameters.AddWithValue(
         "context",
         (object?)ap.Context ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue("activity_date", ap.Time.ActivityDate);
      cmd.Parameters.AddWithValue(
         "local_start_time",
         (object?)ap.Time.LocalStartTime ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue(
         "starts_at",
         (object?)ToUtc(GetStartsAt(ap.Time)) ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue("time_zone_id", ap.Time.TimeZoneId);
      cmd.Parameters.AddWithValue(
         "confidence",
         (object?)ap.Confidence ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue("status_id", ap.Status.ToString());
      cmd.Parameters.AddWithValue(
         "reject_reason_id",
         (object?)ap.RejectReason?.ToString() ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue(
         "reject_comment",
         (object?)ap.RejectComment ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue(
         "activity_id",
         (object?)ap.ActivityId?.Value ?? DBNull.Value
      );
      cmd.Parameters.AddWithValue(
         "prompt",
         ap.ProducerType == ActivityProposalProducerType.AiSearch
            ? (object?)ap.Prompt ?? DBNull.Value
            : DBNull.Value
      );
   }

   private static async Task ReplaceProposalLinksAsync(
      NpgsqlConnection con,
      NpgsqlTransaction tran,
      ActivityProposal ap,
      CancellationToken ct
   )
   {
      await using(var deleteCommand = new NpgsqlCommand(
         "delete from activity_proposal_entity_links where proposal_id = @id",
         con,
         tran
      ))
      {
         deleteCommand.Parameters.AddWithValue("id", ap.Id.Value);
         await deleteCommand.ExecuteNonQueryAsync(ct);
      }

      const string sql = """
         insert into activity_proposal_entity_links (
            id, proposal_id, entity_id, proposed_role_id, explanation,
            context_name, confidence
         )
         values (
            @id, @proposal_id, @entity_id, @proposed_role_id, @explanation,
            @context_name, @confidence
         )
         """;

      foreach(var link in ap.EntityLinks)
      {
         await using var command = new NpgsqlCommand(sql, con, tran);
         command.Parameters.AddWithValue(
            "id",
            CreateGuid($"proposal-link:{ap.Id.Value}:{link.EntityId.Value}")
         );
         command.Parameters.AddWithValue("proposal_id", ap.Id.Value);
         command.Parameters.AddWithValue("entity_id", link.EntityId.Value);
         command.Parameters.AddWithValue(
            "proposed_role_id",
            link.ProposedRole.ToString()
         );
         command.Parameters.AddWithValue("explanation", link.Explanation);
         command.Parameters.AddWithValue(
            "context_name",
            (object?)link.ContextName ?? DBNull.Value
         );
         command.Parameters.AddWithValue(
            "confidence",
            (object?)link.Confidence ?? DBNull.Value
         );
         await command.ExecuteNonQueryAsync(ct);
      }
   }

   private static async Task ReplaceProposalEvidenceAsync(
      NpgsqlConnection con,
      NpgsqlTransaction tran,
      ActivityProposal ap,
      CancellationToken ct
   )
   {
      await using(var deleteCommand = new NpgsqlCommand(
         "delete from activity_proposal_evidence where proposal_id = @id",
         con,
         tran
      ))
      {
         deleteCommand.Parameters.AddWithValue("id", ap.Id.Value);
         await deleteCommand.ExecuteNonQueryAsync(ct);
      }

      const string sql = """
         insert into activity_proposal_evidence (
            id, proposal_id, source_id, uri, title, observed_at, summary,
            raw_excerpt
         )
         values (
            @id, @proposal_id, @source_id, @uri, @title, @observed_at,
            @summary, @raw_excerpt
         )
         """;

      var index = 0;
      foreach(var pe in ap.Evidence)
      {
         await using var command = new NpgsqlCommand(sql, con, tran);
         command.Parameters.AddWithValue(
            "id",
            CreateGuid($"proposal-evidence:{ap.Id.Value}:{index}")
         );
         command.Parameters.AddWithValue("proposal_id", ap.Id.Value);
         command.Parameters.AddWithValue("source_id", pe.Source.Id.Value);
         command.Parameters.AddWithValue(
            "uri",
            (object?)pe.Uri?.ToString() ?? DBNull.Value
         );
         command.Parameters.AddWithValue(
            "title",
            (object?)pe.Title ?? DBNull.Value
         );
         command.Parameters.AddWithValue(
            "observed_at",
            pe.ObservedAt.ToUniversalTime()
         );
         command.Parameters.AddWithValue("summary", pe.Summary);
         command.Parameters.AddWithValue(
            "raw_excerpt",
            (object?)pe.RawExcerpt ?? DBNull.Value
         );
         await command.ExecuteNonQueryAsync(ct);
         index++;
      }
   }

   private static DateTimeOffset? GetStartsAt(ActivityTime time)
   {
      if(time.StartsAt is not null)
      {
         return time.StartsAt;
      }

      if(time.LocalStartTime is null)
      {
         return null;
      }

      var localDateTime = time.ActivityDate.ToDateTime(
         time.LocalStartTime.Value,
         DateTimeKind.Unspecified
      );
      var timeZone = ResolveTimeZone(time.TimeZoneId);
      var offset = timeZone.GetUtcOffset(localDateTime);
      return new DateTimeOffset(localDateTime, offset);
   }

   private static DateTimeOffset? ToUtc(DateTimeOffset? value)
   {
      return value?.ToUniversalTime();
   }

   private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
   {
      try
      {
         return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      }
      catch(TimeZoneNotFoundException)
      {
         if(
            TimeZoneInfo.TryConvertIanaIdToWindowsId(
               timeZoneId,
               out var windowsId
            )
         )
         {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
         }

         return TimeZoneInfo.Utc;
      }
   }

   private static Guid CreateGuid(string value)
   {
      return DeterministicGuid.Create(value);
   }
}
