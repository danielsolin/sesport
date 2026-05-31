using Npgsql;

namespace SESport.Web.Data;

public sealed class AuditRepository(NpgsqlDataSource dataSource)
{
   public IReadOnlyList<AuditArea> GetAuditAreas()
   {
      return
      [
         new AuditArea(
            "Activity proposals",
            "Review imported or manually produced proposal records.",
            "/Admin/Audit/Proposals"
         ),
         new AuditArea(
            "Activity audit",
            "Inspect canonical activity entity links and evidence.",
            "/Admin/Audit/Activities"
         ),
         new AuditArea(
            "Proposal groups",
            "Inspect dedupe groups that can connect proposals to activities.",
            "/Admin/Audit/Groups"
         )
      ];
   }

   public async Task<IReadOnlyList<ActivityProposalAuditItem>>
      GetProposalsAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select
            p.id,
            p.title,
            pt.label,
            s.name,
            ps.label,
            at.label,
            sp.name,
            p.activity_date,
            p.local_start_time,
            p.confidence,
            p.group_id,
            p.activity_id,
            count(distinct l.id) as entity_link_count,
            count(distinct e.id) as evidence_count
         from activity_proposals p
         join producer_types pt on pt.id = p.producer_type_id
         join sources s on s.id = p.source_id
         join proposal_statuses ps on ps.id = p.status_id
         join activity_types at on at.id = p.activity_type_id
         join sports sp on sp.id = p.sport_id
         left join activity_proposal_entity_links l on l.proposal_id = p.id
         left join activity_proposal_evidence e on e.proposal_id = p.id
         group by p.id, pt.label, s.name, ps.label, at.label, sp.name
         order by p.activity_date, p.local_start_time nulls last, p.title
         """;

      await using var command = dataSource.CreateCommand(sql);
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
               reader.GetString(5),
               reader.GetString(6),
               FormatTime(reader, 7, 8),
               ReadDecimal(reader, 9),
               ReadString(reader, 10),
               ReadGuid(reader, 11),
               reader.GetInt32(12),
               reader.GetInt32(13)
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
         join tracked_entities te on te.id = l.entity_id
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

   public async Task<IReadOnlyList<ActivityLinkAuditItem>>
      GetActivityLinksAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select
            a.id,
            a.title,
            a.activity_date,
            a.local_start_time,
            te.canonical_name,
            et.label
         from activity_entity_links l
         join activities a on a.id = l.activity_id
         join tracked_entities te on te.id = l.entity_id
         join entity_types et on et.id = te.entity_type_id
         order by a.activity_date, a.title, te.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var links = new List<ActivityLinkAuditItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         links.Add(
            new ActivityLinkAuditItem(
               reader.GetGuid(0),
               reader.GetString(1),
               FormatTime(reader, 2, 3),
               reader.GetString(4),
               reader.GetString(5)
            )
         );
      }

      return links;
   }

   public async Task<IReadOnlyList<ActivityEvidenceAuditItem>>
      GetActivityEvidenceAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select
            a.id,
            a.title,
            a.activity_date,
            a.local_start_time,
            s.name,
            e.uri,
            e.title,
            e.observed_at,
            e.comment,
            e.proposal_id
         from activity_evidence e
         join activities a on a.id = e.activity_id
         join sources s on s.id = e.source_id
         order by a.activity_date, a.title, e.observed_at desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var evidence = new List<ActivityEvidenceAuditItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         evidence.Add(
            new ActivityEvidenceAuditItem(
               reader.GetGuid(0),
               reader.GetString(1),
               FormatTime(reader, 2, 3),
               reader.GetString(4),
               ReadString(reader, 5),
               ReadString(reader, 6),
               reader.GetFieldValue<DateTimeOffset>(7),
               ReadString(reader, 8),
               ReadString(reader, 9)
            )
         );
      }

      return evidence;
   }

   public async Task<IReadOnlyList<ProposalGroupAuditItem>>
      GetProposalGroupsAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select
            g.id,
            g.fingerprint,
            g.activity_id,
            count(p.id) as proposal_count,
            g.updated_at
         from activity_proposal_groups g
         left join activity_proposals p on p.group_id = g.id
         group by g.id
         order by g.updated_at desc, g.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var groups = new List<ProposalGroupAuditItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         groups.Add(
            new ProposalGroupAuditItem(
               reader.GetString(0),
               reader.GetString(1),
               ReadGuid(reader, 2),
               reader.GetInt32(3),
               reader.GetFieldValue<DateTimeOffset>(4)
            )
         );
      }

      return groups;
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
         ? $"{activityDate:yyyy-MM-dd}"
         : $"{activityDate:yyyy-MM-dd} {localStartTime:HH:mm}";
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
}
