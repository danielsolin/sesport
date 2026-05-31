using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

var options = ImportOptions.Parse(args);
var dataPath = Path.GetFullPath(options.DataPath);

if (!File.Exists(dataPath))
{
   Console.Error.WriteLine($"Entity data file not found: {dataPath}");
   return 1;
}

var document = await LoadDocumentAsync(dataPath);
var validationErrors = Validate(document);

if (validationErrors.Count > 0)
{
   foreach (var error in validationErrors)
   {
      Console.Error.WriteLine(error);
   }

   return 1;
}

try
{
   await using var dataSource = NpgsqlDataSource.Create(
      options.ConnectionString
   );
   await using var connection = await dataSource.OpenConnectionAsync();
   await using var transaction = await connection.BeginTransactionAsync();

   await UpsertSportsAsync(connection, transaction, document.Entities);

   var importedCount = 0;

   foreach (var entity in document.Entities)
   {
      await UpsertEntityAsync(connection, transaction, entity);
      importedCount++;
   }

   await transaction.CommitAsync();

   Console.WriteLine(
      $"Imported {importedCount} entities from {options.DataPath}."
   );
   return 0;
}
catch (PostgresException exception) when (
   exception.SqlState == PostgresErrorCodes.UndefinedColumn ||
   exception.SqlState == PostgresErrorCodes.UndefinedTable
)
{
   Console.Error.WriteLine(
      "Entity import could not run because the database schema does not " +
      "match the current migrations. Recreate the local dev database and " +
      "run migrations before importing entities."
   );
   Console.Error.WriteLine(exception.MessageText);
   return 2;
}

static async Task<EntityWatchlistDocument> LoadDocumentAsync(string dataPath)
{
   await using var stream = File.OpenRead(dataPath);
   var document =
      await JsonSerializer.DeserializeAsync<EntityWatchlistDocument>(
         stream,
         new JsonSerializerOptions
         {
            PropertyNameCaseInsensitive = true
         }
      );

   return document ?? throw new InvalidOperationException(
      "Entity data file was empty."
   );
}

static IReadOnlyList<string> Validate(EntityWatchlistDocument document)
{
   var errors = new List<string>();

   if (document.SchemaVersion != 1)
   {
      errors.Add($"Unsupported schemaVersion: {document.SchemaVersion}.");
   }

   var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

   foreach (var entity in document.Entities)
   {
      if (string.IsNullOrWhiteSpace(entity.Id))
      {
         errors.Add("Entity id is required.");
         continue;
      }

      if (!ids.Add(entity.Id))
      {
         errors.Add($"Duplicate entity id: {entity.Id}.");
      }

      AddRequiredError(errors, entity.Id, entity.Name, "name");
      AddRequiredError(errors, entity.Id, entity.Priority, "priority");
      AddRequiredError(errors, entity.Id, entity.Type, "type");
      AddRequiredError(errors, entity.Id, entity.Sport.Id, "sport.id");
      AddRequiredError(errors, entity.Id, entity.Sport.Name, "sport.name");
      AddRequiredError(
         errors,
         entity.Id,
         entity.SwedenConnection,
         "swedenConnection"
      );
      AddRequiredError(
         errors,
         entity.Id,
         entity.ExpectedStability,
         "expectedStability"
      );

      if (!SeedLookups.EntityTypeMap.ContainsKey(entity.Type))
      {
         errors.Add(
            $"Entity {entity.Id} has unsupported type '{entity.Type}'."
         );
      }

      if (!SeedLookups.AllowedPriorities.Contains(entity.Priority))
      {
         errors.Add(
            $"Entity {entity.Id} has unsupported priority '{entity.Priority}'."
         );
      }

      if (!SeedLookups.AllowedStabilities.Contains(entity.ExpectedStability))
      {
         errors.Add(
            $"Entity {entity.Id} has unsupported stability " +
            $"'{entity.ExpectedStability}'."
         );
      }
   }

   return errors;
}

static void AddRequiredError(
   List<string> errors,
   string entityId,
   string? value,
   string field
)
{
   if (string.IsNullOrWhiteSpace(value))
   {
      errors.Add($"Entity {entityId} is missing {field}.");
   }
}

static async Task UpsertSportsAsync(
   NpgsqlConnection connection,
   NpgsqlTransaction transaction,
   IReadOnlyCollection<EntitySeed> entities
)
{
   const string sql = """
      insert into sports (id, name)
      values (@id, @name)
      on conflict (id) do update
      set
         name = excluded.name,
         updated_at = now()
      """;

   foreach (var sport in entities
      .Select(entity => entity.Sport)
      .DistinctBy(sport => sport.Id))
   {
      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", sport.Id);
      command.Parameters.AddWithValue("name", NormalizeSportName(sport.Name));
      await command.ExecuteNonQueryAsync();
   }
}

static async Task UpsertEntityAsync(
   NpgsqlConnection connection,
   NpgsqlTransaction transaction,
   EntitySeed entity
)
{
   const string sql = """
      insert into tracked_entities (
         id,
         canonical_name,
         entity_type_id,
         sport_id,
         country_id,
         country_code,
         country_name,
         country_relevance_kind_id,
         country_relevance_reason,
         watch_priority_id,
         expected_stability_id
      )
      values (
         @id,
         @canonical_name,
         @entity_type_id,
         @sport_id,
         @country_id,
         @country_code,
         @country_name,
         @country_relevance_kind_id,
         @country_relevance_reason,
         @watch_priority_id,
         @expected_stability_id
      )
      on conflict (id) do update
      set
         canonical_name = excluded.canonical_name,
         entity_type_id = excluded.entity_type_id,
         sport_id = excluded.sport_id,
         country_relevance_kind_id = excluded.country_relevance_kind_id,
         country_relevance_reason = excluded.country_relevance_reason,
         watch_priority_id = excluded.watch_priority_id,
         expected_stability_id = excluded.expected_stability_id,
         updated_at = now()
      """;

   await using var command = new NpgsqlCommand(sql, connection, transaction);
   command.Parameters.AddWithValue("id", CreateGuid($"entity:{entity.Id}"));
   command.Parameters.AddWithValue("canonical_name", entity.Name);
   command.Parameters.AddWithValue(
      "entity_type_id",
      SeedLookups.EntityTypeMap[entity.Type]
   );
   command.Parameters.AddWithValue("sport_id", entity.Sport.Id);
   command.Parameters.AddWithValue("country_id", "country:se");
   command.Parameters.AddWithValue("country_code", "SE");
   command.Parameters.AddWithValue("country_name", "Sweden");
   command.Parameters.AddWithValue(
      "country_relevance_kind_id",
      GetCountryRelevanceKind(entity)
   );
   command.Parameters.AddWithValue(
      "country_relevance_reason",
      entity.SwedenConnection
   );
   command.Parameters.AddWithValue("watch_priority_id", entity.Priority);
   command.Parameters.AddWithValue(
      "expected_stability_id",
      entity.ExpectedStability
   );
   await command.ExecuteNonQueryAsync();
}

static string GetCountryRelevanceKind(EntitySeed entity)
{
   if (entity.Type == "national_team")
   {
      return "NationalTeamRepresentation";
   }

   if (entity.Type == "recurring_event")
   {
      return "RecurringEventOriginOrInterest";
   }

   var connection = entity.SwedenConnection.ToLowerInvariant();

   if (
      connection.Contains("athlete", StringComparison.Ordinal) ||
      connection.Contains("player", StringComparison.Ordinal) ||
      connection.Contains("driver", StringComparison.Ordinal) ||
      connection.Contains("rider", StringComparison.Ordinal) ||
      connection.Contains("sailor", StringComparison.Ordinal)
   )
   {
      return "NationalityOrSportingIdentity";
   }

   if (
      connection.Contains("sweden-based", StringComparison.Ordinal) ||
      connection.Contains("swedish club", StringComparison.Ordinal) ||
      connection.Contains("governing body", StringComparison.Ordinal) ||
      connection.Contains("swedish national", StringComparison.Ordinal)
   )
   {
      return "BasedInCountry";
   }

   return "Manual";
}

static string NormalizeSportName(string value)
{
   var words = value
      .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .Select(word => word is "/" ? word : char.ToUpperInvariant(word[0]) +
         word[1..]);

   return string.Join(' ', words);
}

static Guid CreateGuid(string value)
{
   var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
   bytes[6] = (byte)((bytes[6] & 0x0f) | 0x30);
   bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
   return new Guid(bytes);
}

sealed record ImportOptions(
   string DataPath,
   string ConnectionString
)
{
   public static ImportOptions Parse(string[] args)
   {
      var dataPath = "data/entity-watchlist.json";
      var connectionString = SeedLookups.DefaultConnectionString();

      for (var index = 0; index < args.Length; index++)
      {
         switch (args[index])
         {
            case "--data":
               dataPath = ReadArgumentValue(args, ref index, "--data");
               break;
            case "--connection-string":
               connectionString = ReadArgumentValue(
                  args,
                  ref index,
                  "--connection-string"
               );
               break;
            default:
               throw new ArgumentException(
                  $"Unknown argument '{args[index]}'."
               );
         }
      }

      return new ImportOptions(dataPath, connectionString);
   }

   private static string ReadArgumentValue(
      string[] args,
      ref int index,
      string argumentName
   )
   {
      if (index + 1 >= args.Length)
      {
         throw new ArgumentException(
            $"{argumentName} requires a value."
         );
      }

      index++;
      return args[index];
   }
}

sealed record EntityWatchlistDocument(
   int SchemaVersion,
   string SourceDocument,
   string GeneratedFrom,
   IReadOnlyList<EntitySeed> Entities
);

sealed record EntitySeed(
   string Id,
   string Name,
   string Priority,
   string Type,
   SportSeed Sport,
   string SwedenConnection,
   string StableReason,
   string CurrentRelationshipOrStatus,
   string CurrentRelationshipOrStatusConfidence,
   string ExpectedStability,
   IReadOnlyList<string> LikelyActivityTypes,
   string SuggestedEvidenceSources,
   string Notes
);

sealed record SportSeed(string Id, string Name);

static class SeedLookups
{
   public static readonly Dictionary<string, string> EntityTypeMap = new(
      StringComparer.OrdinalIgnoreCase
   )
   {
      ["person"] = "Person",
      ["national_team"] = "NationalTeam",
      ["club"] = "Club",
      ["recurring_event"] = "RecurringEvent",
      ["organization"] = "Organization",
      ["family_or_group"] = "Pair",
      ["other"] = "Other"
   };

   public static readonly HashSet<string> AllowedPriorities = new(
      ["tier_1", "tier_2", "tier_3", "review"],
      StringComparer.OrdinalIgnoreCase
   );

   public static readonly HashSet<string> AllowedStabilities = new(
      ["long_term", "medium_term", "short_term"],
      StringComparer.OrdinalIgnoreCase
   );

   public static string DefaultConnectionString()
   {
      return Environment.GetEnvironmentVariable("ConnectionStrings__SESport") ??
         Environment.GetEnvironmentVariable("SESPORT_CONNECTION_STRING") ??
         "Host=localhost;Port=5432;Database=sesport;Username=sesport;" +
         "Password=sesport";
   }
}
