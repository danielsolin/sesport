using Npgsql;

using SESport.Core.Facts;
using SESport.Core.Sources;

namespace SESport.Data;

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
      return GetForSubjectAsync(
         "activity_id",
         activityId,
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

   public async Task<IReadOnlyList<FactRecord>> ReplaceForActivityAsync(
      Guid activityId,
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

      if(!await ActivityExistsAsync(
         connection,
         transaction,
         activityId,
         cancellationToken
      ))
      {
         return [];
      }

      await DeleteActivityFactsAndSourcesAsync(
         connection,
         transaction,
         activityId,
         cancellationToken
      );

      var createdFacts = new List<FactRecord>();
      var sourceIds = new Dictionary<string, Guid>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(var draft in normalizedDrafts)
      {
         var fact = await InsertActivityFactAsync(
            connection,
            transaction,
            activityId,
            draft.Text,
            cancellationToken
         );
         createdFacts.Add(fact);
         var linkedSourceIds = new HashSet<Guid>();

         foreach(var source in draft.Sources)
         {
            if(!sourceIds.TryGetValue(source.Url, out var sourceId))
            {
               sourceId = await InsertActivityFactSourceAsync(
                  connection,
                  transaction,
                  activityId,
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
            entity_id,
            fact_text
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            @fact_text
         )
         returning
            id,
            activity_id,
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

   private async Task<IReadOnlyList<FactRecord>> GetForSubjectAsync(
      string subjectColumn,
      Guid subjectId,
      CancellationToken cancellationToken
   )
   {
      var sql = $"""
         select
            id,
            activity_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         from facts
         where {subjectColumn} = @subject_id
         order by created_at, id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("subject_id", subjectId);
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
      Guid? entityId = reader.IsDBNull(2) ? null : reader.GetGuid(2);

      return new FactRecord(
         reader.GetGuid(0),
         activityId is not null
            ? FactSubjectTypes.Activity
            : FactSubjectTypes.Entity,
         activityId ?? entityId ?? throw new InvalidOperationException(
            "The fact has no subject."
         ),
         reader.GetString(3),
         reader.GetFieldValue<DateTimeOffset>(4),
         reader.GetFieldValue<DateTimeOffset>(5)
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

   private static async Task DeleteActivityFactsAndSourcesAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      await using(var factCommand = new NpgsqlCommand(
         "delete from facts where activity_id = @activity_id",
         connection,
         transaction
      ))
      {
         factCommand.Parameters.AddWithValue("activity_id", activityId);
         await factCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await using var sourceCommand = new NpgsqlCommand(
         """
         delete from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
            and kind = @kind
         """,
         connection,
         transaction
      );
      sourceCommand.Parameters.AddWithValue(
         "correlation_type",
         SourceCorrelationTypes.Activity
      );
      sourceCommand.Parameters.AddWithValue(
         "correlation_id",
         activityId.ToString()
      );
      sourceCommand.Parameters.AddWithValue(
         "kind",
         SourceKinds.ActivityEvidence
      );
      await sourceCommand.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task<FactRecord> InsertActivityFactAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string text,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      const string sql = """
         insert into facts (id, activity_id, fact_text)
         values (@id, @activity_id, @fact_text)
         returning
            id,
            activity_id,
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
      command.Parameters.AddWithValue("activity_id", activityId);
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

   private static async Task<Guid> InsertActivityFactSourceAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
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
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue(
         "correlation_id",
         activityId.ToString()
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
