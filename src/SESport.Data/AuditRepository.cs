using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SESport.Core.Formatting;

namespace SESport.Data;

public sealed class AuditRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<ActivityProposalAuditItem>>
      GetProposalsAsync(
         CancellationToken cancellationToken,
         string proposalStatus = "Pending"
      )
   {
      const string sql = """
         select
            p.id,
            p.title,
            coalesce(nullif(p.producer, ''), pt.label),
            s.name,
            ps.label,
            prr.label,
            p.reject_comment,
            at.label,
            sp.name,
            p.activity_date,
            p.local_start_time,
            p.confidence,
            p.activity_id,
            count(distinct l.id) as entity_link_count,
            count(distinct e.id) as evidence_count,
            p.created_at
         from activity_proposals p
         join producer_types pt on pt.id = p.producer_type_id
         join sources s on s.id = p.source_id
         join proposal_statuses ps on ps.id = p.status_id
         left join proposal_reject_reasons prr on prr.id = p.reject_reason_id
         join activity_types at on at.id = p.activity_type_id
         join sports sp on sp.id = p.sport_id
         left join activity_proposal_entity_links l on l.proposal_id = p.id
         left join activity_proposal_evidence e on e.proposal_id = p.id
         where ps.id = @proposal_status
         group by p.id, pt.label, s.name, ps.label, prr.label, at.label, sp.name
         order by p.activity_date, p.local_start_time nulls last, p.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("proposal_status", proposalStatus);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var proposals = new List<ActivityProposalAuditItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         proposals.Add(
            new ActivityProposalAuditItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               ReadString(reader, 5),
               ReadString(reader, 6),
               reader.GetString(7),
               reader.GetString(8),
               FormatTime(reader, 9, 10),
               ReadDecimal(reader, 11),
               ReadGuid(reader, 12),
               reader.GetInt32(13),
               reader.GetInt32(14),
               reader.GetDateTime(15)
            )
         );
      }

      return proposals;
   }

   public async Task<IReadOnlyList<ActivityProposalLinkAuditItem>>
      GetProposalLinksAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select
            p.id,
            p.title,
            te.canonical_name,
            r.label,
            l.explanation,
            l.context_name,
            l.confidence
         from activity_proposal_entity_links l
         join activity_proposals p on p.id = l.proposal_id
         join entities te on te.id = l.entity_id
         join activity_entity_link_roles r on r.id = l.proposed_role_id
         order by p.activity_date, p.title, te.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var links = new List<ActivityProposalLinkAuditItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         links.Add(
            new ActivityProposalLinkAuditItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               ReadString(reader, 5),
               ReadDecimal(reader, 6)
            )
         );
      }

      return links;
   }

   public async Task<ActivityProposalDetail?> GetProposalAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            p.id,
            p.title,
            p.description,
            p.context,
            p.producer_type_id,
            coalesce(nullif(p.producer, ''), pt.label),
            s.name,
            ps.label,
            prr.label,
            p.reject_comment,
            at.label,
            p.activity_type_id,
            sp.name,
             p.sport_id,
            p.activity_date,
            p.local_start_time,
            p.time_zone_id,
            p.confidence,
            p.activity_id,
            p.prompt
         from activity_proposals p
         join producer_types pt on pt.id = p.producer_type_id
         join sources s on s.id = p.source_id
         join proposal_statuses ps on ps.id = p.status_id
         left join proposal_reject_reasons prr on prr.id = p.reject_reason_id
         join activity_types at on at.id = p.activity_type_id
         join sports sp on sp.id = p.sport_id
         where p.id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      var activityDate = reader.GetFieldValue<DateOnly>(14);
      var localStartTime = ReadTimeOnly(reader, 15);

      return new ActivityProposalDetail(
         reader.GetString(0),
         reader.GetString(1),
         ReadString(reader, 2),
         ReadString(reader, 3),
         reader.GetString(4),
         reader.GetString(5),
         reader.GetString(6),
         reader.GetString(7),
         ReadString(reader, 8),
         ReadString(reader, 9),
         reader.GetString(10),
         reader.GetString(11),
         reader.GetString(12),
         reader.GetString(13),
         FormatTime(activityDate, localStartTime),
         activityDate,
         localStartTime,
         reader.GetString(16),
         ReadDecimal(reader, 17),
         ReadGuid(reader, 18),
         ReadString(reader, 19)
      );
   }

   public async Task<IReadOnlyList<ActivityProposalLinkAuditItem>>
      GetProposalLinksAsync(
         string proposalId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            p.id,
            p.title,
            te.canonical_name,
            r.label,
            l.explanation,
            l.context_name,
            l.confidence
         from activity_proposal_entity_links l
         join activity_proposals p on p.id = l.proposal_id
         join entities te on te.id = l.entity_id
         join activity_entity_link_roles r on r.id = l.proposed_role_id
         where p.id = @proposal_id
         order by te.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("proposal_id", proposalId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var links = new List<ActivityProposalLinkAuditItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         links.Add(
            new ActivityProposalLinkAuditItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               ReadString(reader, 5),
               ReadDecimal(reader, 6)
            )
         );
      }

      return links;
   }

   public async Task<IReadOnlyList<ActivityProposalEvidenceAuditItem>>
      GetProposalEvidenceAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select
            p.id,
            p.title,
            s.name,
            e.uri,
            e.title,
            e.observed_at,
            e.summary,
            e.raw_excerpt
         from activity_proposal_evidence e
         join activity_proposals p on p.id = e.proposal_id
         join sources s on s.id = e.source_id
         order by p.activity_date, p.title, e.observed_at desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var evidence = new List<ActivityProposalEvidenceAuditItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         evidence.Add(
            new ActivityProposalEvidenceAuditItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               ReadString(reader, 3),
               ReadString(reader, 4),
               reader.GetFieldValue<DateTimeOffset>(5),
               reader.GetString(6),
               ReadString(reader, 7)
            )
         );
      }

      return evidence;
   }

   public async Task<IReadOnlyList<ActivityProposalEvidenceAuditItem>>
      GetProposalEvidenceAsync(
         string proposalId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            p.id,
            p.title,
            s.name,
            e.uri,
            e.title,
            e.observed_at,
            e.summary,
            e.raw_excerpt
         from activity_proposal_evidence e
         join activity_proposals p on p.id = e.proposal_id
         join sources s on s.id = e.source_id
         where p.id = @proposal_id
         order by e.observed_at desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("proposal_id", proposalId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var evidence = new List<ActivityProposalEvidenceAuditItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         evidence.Add(
            new ActivityProposalEvidenceAuditItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               ReadString(reader, 3),
               ReadString(reader, 4),
               reader.GetFieldValue<DateTimeOffset>(5),
               reader.GetString(6),
               ReadString(reader, 7)
            )
         );
      }

      return evidence;
   }

   public async Task<IReadOnlyList<RejectReasonOption>>
      GetRejectReasonsAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select id, label
         from proposal_reject_reasons
         order by sort_order, label
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var reasons = new List<RejectReasonOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         reasons.Add(new RejectReasonOption(
            reader.GetString(0),
            reader.GetString(1)
         ));
      }

      return reasons;
   }

   public async Task<Guid> AcceptProposalAsync(
      string proposalId,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      var activityId = Guid.NewGuid();

      await InsertActivityFromProposalAsync(
         connection,
         transaction,
         proposalId,
         activityId,
         cancellationToken
      );
      await InsertActivityLinksFromProposalAsync(
         connection,
         transaction,
         proposalId,
         activityId,
         cancellationToken
      );
      await InsertActivityEvidenceFromProposalAsync(
         connection,
         transaction,
         proposalId,
         activityId,
         cancellationToken
      );
      await MarkProposalApprovedAsync(
         connection,
         transaction,
         proposalId,
         activityId,
         cancellationToken
      );

      await transaction.CommitAsync(cancellationToken);
      return activityId;
   }

   public async Task RejectProposalAsync(
      string proposalId,
      string rejectReasonId,
      string? rejectComment,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activity_proposals
         set
            status_id = 'Rejected',
            reject_reason_id = @reject_reason_id,
            reject_comment = @reject_comment,
            activity_id = null,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", proposalId);
      command.Parameters.AddWithValue("reject_reason_id", rejectReasonId);
      command.Parameters.AddWithValue(
         "reject_comment",
         string.IsNullOrWhiteSpace(rejectComment)
            ? DBNull.Value
            : rejectComment.Trim()
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task InsertActivityFromProposalAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string proposalId,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activities (
            id,
            title,
            description,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            time_zone_id,
            publication_status_id,
            slug,
            published_at
         )
         select
            @activity_id,
            title,
            description,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            time_zone_id,
            'Draft',
            @slug,
            null
         from activity_proposals
         where id = @proposal_id
         """;

      var proposal = await ReadProposalActivitySeedAsync(
         connection,
         transaction,
         proposalId,
         cancellationToken
      );

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("proposal_id", proposalId);
      command.Parameters.AddWithValue(
         "slug",
         CreateActivitySlug(proposal.Title, proposal.ActivityDate, proposalId)
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task InsertActivityLinksFromProposalAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string proposalId,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_entity_links (id, activity_id, entity_id)
         select md5(@activity_id::text || entity_id::text)::uuid,
            @activity_id,
            entity_id
         from activity_proposal_entity_links
         where proposal_id = @proposal_id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("proposal_id", proposalId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task InsertActivityEvidenceFromProposalAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string proposalId,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_evidence (
            id,
            activity_id,
            proposal_id,
            source_id,
            uri,
            title,
            observed_at,
            comment
         )
         select
            md5(
               @activity_id::text ||
               id::text ||
               coalesce(uri, '') ||
               observed_at::text
            )::uuid,
            @activity_id,
            proposal_id,
            source_id,
            uri,
            title,
            observed_at,
            summary
         from activity_proposal_evidence
         where proposal_id = @proposal_id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("proposal_id", proposalId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task MarkProposalApprovedAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string proposalId,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activity_proposals
         set
            status_id = 'Approved',
            activity_id = @activity_id,
            reject_reason_id = null,
            reject_comment = null,
            updated_at = now()
         where id = @proposal_id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("proposal_id", proposalId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task<ProposalActivitySeed>
      ReadProposalActivitySeedAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string proposalId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select title, activity_date
         from activity_proposals
         where id = @proposal_id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("proposal_id", proposalId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         throw new InvalidOperationException(
            $"Activity proposal was not found: {proposalId}."
         );
      }

      return new ProposalActivitySeed(
         reader.GetString(0),
         reader.GetFieldValue<DateOnly>(1)
      );
   }

   private static string FormatTime(
      NpgsqlDataReader reader,
      int dateOrdinal,
      int timeOrdinal
   )
   {
      var activityDate = reader.GetFieldValue<DateOnly>(dateOrdinal);
      TimeOnly? localStartTime = reader.IsDBNull(timeOrdinal)
         ? null
         : reader.GetFieldValue<TimeOnly>(timeOrdinal);

      return localStartTime is null
         ? FormatTime(activityDate, null)
         : FormatTime(activityDate, localStartTime);
   }

   private static string FormatTime(
      DateOnly activityDate,
      TimeOnly? localStartTime
   )
   {
      return localStartTime is null
         ? DateDisplay.Format(activityDate)
         : $"{DateDisplay.Format(activityDate)} {localStartTime:HH:mm}";
   }

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static Guid? ReadGuid(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
   }

   private static decimal? ReadDecimal(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
   }

   private static TimeOnly? ReadTimeOnly(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<TimeOnly>(ordinal);
   }

   private static string CreateActivitySlug(
      string title,
      DateOnly activityDate,
      string proposalId
   )
   {
      var normalized = Regex.Replace(
         title.ToLowerInvariant(),
         "[^a-z0-9]+",
         "-"
      ).Trim('-');

      if(string.IsNullOrWhiteSpace(normalized))
      {
         normalized = "activity";
      }

      var suffix = Convert.ToHexString(
         SHA256.HashData(Encoding.UTF8.GetBytes(proposalId))
      )[..8].ToLowerInvariant();

      return $"{DateDisplay.Format(activityDate)}-{normalized}-{suffix}";
   }

   private sealed record ProposalActivitySeed(
      string Title,
      DateOnly ActivityDate
   );
}
