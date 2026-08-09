using Npgsql;

using SESport.Core.Domain;
using SESport.Core.Sources;

namespace SESport.Data.Repositories;

public sealed class FactRepository(NpgsqlDataSource dataSource)
{
   public Task<FactRecord> CreateForActivityAsync(
      Guid activityId,
      string text,
      CancellationToken cancellationToken
   )
   {
      return CreateAsync(
         activityId,
         null,
         null,
         text,
         cancellationToken
      );
   }

   public Task<FactRecord> CreateForActivityGroupAsync(
      Guid activityGroupId,
      string text,
      CancellationToken cancellationToken
   )
   {
      return CreateAsync(
         null,
         activityGroupId,
         null,
         text,
         cancellationToken
      );
   }

   public Task<FactRecord> CreateForEntityAsync(
      Guid entityId,
      string text,
      CancellationToken cancellationToken
   )
   {
      return CreateAsync(
         null,
         null,
         entityId,
         text,
         cancellationToken
      );
   }

   public async Task<FactRecord?> GetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            activity_id,
            activity_group_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         from facts
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken)
         ? ReadFact(reader)
         : null;
   }

   public Task<IReadOnlyList<FactRecord>> GetForActivityAsync(
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      return GetForActivityIncludingGroupAsync(
         activityId,
         cancellationToken
      );
   }

   public Task<IReadOnlyList<FactRecord>> GetForActivityGroupAsync(
      Guid activityGroupId,
      CancellationToken cancellationToken
   )
   {
      return GetForSubjectAsync(
         "activity_group_id",
         activityGroupId,
         cancellationToken
      );
   }

   public Task<IReadOnlyList<FactRecord>> GetForEntityAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      return GetForSubjectAsync(
         "entity_id",
         entityId,
         cancellationToken
      );
   }

   public async Task<FactRecord?> UpdateAsync(
      Guid id,
      string text,
      CancellationToken cancellationToken
   )
   {
      var normalizedText = NormalizeText(text);
      const string sql = """
         update facts
         set
            fact_text = @fact_text,
            updated_at = now()
         where id = @id
         returning
            id,
            activity_id,
            activity_group_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("fact_text", normalizedText);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken)
         ? ReadFact(reader)
         : null;
   }

   public async Task<bool> DeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = "delete from facts where id = @id";
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task<bool> DeleteForActivityAsync(
      Guid id,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from facts
         where id = @id
            and (
               activity_id = @activity_id
               or activity_group_id = (
                  select activity_group_id
                  from activities
                  where id = @activity_id
               )
            )
         """;
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("activity_id", activityId);

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task<IReadOnlyList<SourceReference>> GetSourcesAsync(
      Guid factId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            s.id,
            s.correlation_type,
            s.correlation_id,
            s.kind,
            s.url,
            s.title,
            s.excerpt,
            s.observed_at,
            s.created_at
         from fact_source_links fsl
         join sources s on s.id = fsl.source_id
         where fsl.fact_id = @fact_id
         order by s.observed_at desc, s.created_at desc, s.id desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("fact_id", factId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new List<SourceReference>();

      while(await reader.ReadAsync(cancellationToken))
      {
         sources.Add(
            new SourceReference(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               ReadNullableString(reader, 5),
               ReadNullableString(reader, 6),
               reader.GetFieldValue<DateTimeOffset>(7),
               reader.GetFieldValue<DateTimeOffset>(8)
            )
         );
      }

      return sources;
   }

   public Task<IReadOnlyList<FactRecord>> AddForActivityAsync(
      Guid activityId,
      IReadOnlyList<FactDraft> drafts,
      CancellationToken cancellationToken
   )
   {
      return AddForSubjectAsync(
         activityId,
         null,
         SourceCorrelationTypes.Activity,
         drafts,
         cancellationToken
      );
   }

   public Task<IReadOnlyList<FactRecord>> AddForActivityGroupAsync(
      Guid activityGroupId,
      IReadOnlyList<FactDraft> drafts,
      CancellationToken cancellationToken
   )
   {
      return AddForSubjectAsync(
         null,
         activityGroupId,
         SourceCorrelationTypes.ActivityGroup,
         drafts,
         cancellationToken
      );
   }

   private async Task<IReadOnlyList<FactRecord>> AddForSubjectAsync(
      Guid? activityId,
      Guid? activityGroupId,
      string correlationType,
      IReadOnlyList<FactDraft> drafts,
      CancellationToken cancellationToken
   )
   {
      var normalizedDrafts = NormalizeDrafts(drafts);
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      var subjectId = activityId ?? activityGroupId;
      if(subjectId is null)
      {
         throw new InvalidOperationException("The fact has no subject.");
      }

      var subjectKey = subjectId.Value;

      var subjectExists = activityId is not null
         ? await ActivityExistsAsync(
            connection,
            transaction,
            activityId.Value,
            cancellationToken
         )
         : await ActivityGroupExistsAsync(
            connection,
            transaction,
            activityGroupId!.Value,
            cancellationToken
         );

      if(!subjectExists)
      {
         return [];
      }

      var sourceIds = await GetFactSourceIdsAsync(
         connection,
         transaction,
         correlationType,
         subjectKey,
         cancellationToken
      );
      var createdFacts = new List<FactRecord>();

      foreach(var draft in normalizedDrafts)
      {
         var fact = await InsertFactAsync(
            connection,
            transaction,
            activityId,
            activityGroupId,
            draft.Text,
            cancellationToken
         );
         createdFacts.Add(fact);
         var linkedSourceIds = new HashSet<Guid>();

         foreach(var source in draft.Sources)
         {
            if(!sourceIds.TryGetValue(source.Url, out var sourceId))
            {
               sourceId = await InsertFactSourceAsync(
                  connection,
                  transaction,
                  correlationType,
                  subjectKey,
                  source,
                  cancellationToken
               );
               sourceIds.Add(source.Url, sourceId);
            }

            if(linkedSourceIds.Add(sourceId))
            {
               await InsertFactSourceLinkAsync(
                  connection,
                  transaction,
                  fact.Id,
                  sourceId,
                  cancellationToken
               );
            }
         }
      }

      await transaction.CommitAsync(cancellationToken);
      return createdFacts;
   }

   private async Task<FactRecord> CreateAsync(
      Guid? activityId,
      Guid? activityGroupId,
      Guid? entityId,
      string text,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      var normalizedText = NormalizeText(text);
      const string sql = """
         insert into facts (
            id,
            activity_id,
            activity_group_id,
            entity_id,
            fact_text
         )
         values (
            @id,
            @activity_id,
            @activity_group_id,
            @entity_id,
            @fact_text
         )
         returning
            id,
            activity_id,
            activity_group_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "activity_id",
         (object?)activityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_id",
         (object?)activityGroupId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "entity_id",
         (object?)entityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue("fact_text", normalizedText);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         throw new InvalidOperationException("The fact was not created.");
      }

      return ReadFact(reader);
   }

   private async Task<IReadOnlyList<FactRecord>>
      GetForActivityIncludingGroupAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            f.id,
            f.activity_id,
            f.activity_group_id,
            f.entity_id,
            f.fact_text,
            f.created_at,
            f.updated_at,
            coalesce(
               array_agg(s.url order by fsl.created_at, s.id)
                  filter (where s.id is not null),
               array[]::text[]
            ) as source_urls
         from facts f
         left join fact_source_links fsl on fsl.fact_id = f.id
         left join sources s on s.id = fsl.source_id
         where f.activity_id = @activity_id
            or f.activity_group_id = (
               select activity_group_id
               from activities
               where id = @activity_id
            )
         group by
            f.id,
            f.activity_id,
            f.activity_group_id,
            f.entity_id,
            f.fact_text,
            f.created_at,
            f.updated_at
         order by f.created_at, f.id
         """;

      return await ReadFactsAsync(
         sql,
         command => command.Parameters.AddWithValue(
            "activity_id",
            activityId
         ),
         cancellationToken
      );
   }

   private async Task<IReadOnlyList<FactRecord>> GetForSubjectAsync(
      string subjectColumn,
      Guid subjectId,
      CancellationToken cancellationToken
   )
   {
      var sql = $"""
         select
            f.id,
            f.activity_id,
            f.activity_group_id,
            f.entity_id,
            f.fact_text,
            f.created_at,
            f.updated_at,
            coalesce(
               array_agg(s.url order by fsl.created_at, s.id)
                  filter (where s.id is not null),
               array[]::text[]
            ) as source_urls
         from facts f
         left join fact_source_links fsl on fsl.fact_id = f.id
         left join sources s on s.id = fsl.source_id
         where f.{subjectColumn} = @subject_id
         group by
            f.id,
            f.activity_id,
            f.activity_group_id,
            f.entity_id,
            f.fact_text,
            f.created_at,
            f.updated_at
         order by f.created_at, f.id
         """;

      return await ReadFactsAsync(
         sql,
         command => command.Parameters.AddWithValue(
            "subject_id",
            subjectId
         ),
         cancellationToken
      );
   }

   private async Task<IReadOnlyList<FactRecord>> ReadFactsAsync(
      string sql,
      Action<NpgsqlCommand> addParameters,
      CancellationToken cancellationToken
   )
   {
      await using var command = dataSource.CreateCommand(sql);
      addParameters(command);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var facts = new List<FactRecord>();

      while(await reader.ReadAsync(cancellationToken))
      {
         facts.Add(ReadFact(reader));
      }

      return facts;
   }

   private static FactRecord ReadFact(NpgsqlDataReader reader)
   {
      Guid? activityId = reader.IsDBNull(1) ? null : reader.GetGuid(1);
      Guid? activityGroupId = reader.IsDBNull(2)
         ? null
         : reader.GetGuid(2);
      Guid? entityId = reader.IsDBNull(3) ? null : reader.GetGuid(3);
      var subjectType = activityId is not null
         ? FactSubjectTypes.Activity
         : activityGroupId is not null
            ? FactSubjectTypes.ActivityGroup
            : FactSubjectTypes.Entity;

      return new FactRecord(
         reader.GetGuid(0),
         subjectType,
         activityId ?? activityGroupId ?? entityId ??
            throw new InvalidOperationException("The fact has no subject."),
         reader.GetString(4),
         reader.GetFieldValue<DateTimeOffset>(5),
         reader.GetFieldValue<DateTimeOffset>(6),
         reader.FieldCount > 7
            ? reader.GetFieldValue<string[]>(7)
            : []
      );
   }

   private static async Task<bool> ActivityExistsAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      await using var command = new NpgsqlCommand(
         "select exists(select 1 from activities where id = @id)",
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", activityId);

      return (bool)(await command.ExecuteScalarAsync(cancellationToken) ??
         false);
   }

   private static async Task<bool> ActivityGroupExistsAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityGroupId,
      CancellationToken cancellationToken
   )
   {
      await using var command = new NpgsqlCommand(
         "select exists(select 1 from activity_groups where id = @id)",
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", activityGroupId);

      return (bool)(await command.ExecuteScalarAsync(cancellationToken) ??
         false);
   }

   private static async Task<Dictionary<string, Guid>>
      GetFactSourceIdsAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string correlationType,
      Guid subjectId,
      CancellationToken cancellationToken
   )
   {
      await using var command = new NpgsqlCommand(
         """
         select id, url
         from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
            and kind = @kind
         order by created_at, id
         """,
         connection,
         transaction
      );
      command.Parameters.AddWithValue(
         "correlation_type",
         correlationType
      );
      command.Parameters.AddWithValue(
         "correlation_id",
         subjectId.ToString()
      );
      command.Parameters.AddWithValue(
         "kind",
         SourceKinds.ActivityEvidence
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sourceIds = new Dictionary<string, Guid>(
         StringComparer.OrdinalIgnoreCase
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         sourceIds.TryAdd(reader.GetString(1), reader.GetGuid(0));
      }

      return sourceIds;
   }

   private static async Task<FactRecord> InsertFactAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid? activityId,
      Guid? activityGroupId,
      string text,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      const string sql = """
         insert into facts (
            id,
            activity_id,
            activity_group_id,
            fact_text
         )
         values (
            @id,
            @activity_id,
            @activity_group_id,
            @fact_text
         )
         returning
            id,
            activity_id,
            activity_group_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         """;
      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "activity_id",
         (object?)activityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_id",
         (object?)activityGroupId ?? DBNull.Value
      );
      command.Parameters.AddWithValue("fact_text", text);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         throw new InvalidOperationException("The fact was not created.");
      }

      return ReadFact(reader);
   }

   private static async Task<Guid> InsertFactSourceAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string correlationType,
      Guid subjectId,
      FactSourceDraft source,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      const string sql = """
         insert into sources (
            id,
            correlation_type,
            correlation_id,
            kind,
            url,
            title,
            excerpt
         )
         values (
            @id,
            @correlation_type,
            @correlation_id,
            @kind,
            @url,
            @title,
            @excerpt
         )
         """;
      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "correlation_type",
         correlationType
      );
      command.Parameters.AddWithValue(
         "correlation_id",
         subjectId.ToString()
      );
      command.Parameters.AddWithValue(
         "kind",
         SourceKinds.ActivityEvidence
      );
      command.Parameters.AddWithValue("url", source.Url);
      command.Parameters.AddWithValue(
         "title",
         (object?)source.Title ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "excerpt",
         (object?)source.Excerpt ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync(cancellationToken);

      return id;
   }

   private static async Task InsertFactSourceLinkAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid factId,
      Guid sourceId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into fact_source_links (fact_id, source_id)
         values (@fact_id, @source_id)
         """;
      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("fact_id", factId);
      command.Parameters.AddWithValue("source_id", sourceId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static IReadOnlyList<FactDraft> NormalizeDrafts(
      IReadOnlyList<FactDraft> drafts
   )
   {
      if(drafts.Count == 0)
      {
         throw new ArgumentException(
            "At least one fact is required.",
            nameof(drafts)
         );
      }

      return drafts.Select(draft =>
      {
         if(draft.Sources.Count == 0)
         {
            throw new ArgumentException(
               "Every fact must have at least one source.",
               nameof(drafts)
            );
         }

         return new FactDraft(
            NormalizeText(draft.Text),
            draft.Sources.Select(NormalizeSource).ToList()
         );
      }).ToList();
   }

   private static FactSourceDraft NormalizeSource(FactSourceDraft source)
   {
      var url = source.Url.Trim();
      if(!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
         (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps))
      {
         throw new ArgumentException(
            "Fact sources must use absolute HTTP or HTTPS URLs.",
            nameof(source)
         );
      }

      return source with
      {
         Url = url,
         Title = NormalizeNullableText(source.Title),
         Excerpt = NormalizeNullableText(source.Excerpt)
      };
   }

   private static string? ReadNullableString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static string? NormalizeNullableText(string? text)
   {
      return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
   }

   private static string NormalizeText(string text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         throw new ArgumentException(
            "Fact text cannot be empty.",
            nameof(text)
         );
      }

      return text.Trim();
   }
}
