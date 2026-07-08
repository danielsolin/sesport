using Npgsql;

using SESport.Core.Identifiers;

namespace SESport.Data.AI;

public sealed class AiActivitySearchRunRepository : IAsyncDisposable
{
   private readonly NpgsqlDataSource dataSource;
   private readonly bool ownsDataSource;

   public AiActivitySearchRunRepository(NpgsqlDataSource dataSource)
   {
      this.dataSource = dataSource;
   }

   private AiActivitySearchRunRepository(
      NpgsqlDataSource dataSource,
      bool ownsDataSource
   )
   {
      this.dataSource = dataSource;
      this.ownsDataSource = ownsDataSource;
   }

   public static AiActivitySearchRunRepository Connect(string connectionString)
   {
      return new AiActivitySearchRunRepository(
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

   public async Task StartAsync(
      AiActivitySearchRunRecord run,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into ai_activity_search_runs (
            id, started_at, status_id, client_mode, base_address,
            requested_model, api_key_source, allow_web_search,
            web_search_tool_type, plugin_id, search_date,
            window_start, window_end, max_proposals, write_to_database,
            run_directory, output_path, total_entity_count
         )
         values (
            @id, @started_at, @status_id, @client_mode, @base_address,
            @requested_model, @api_key_source, @allow_web_search,
            @web_search_tool_type, @plugin_id, @search_date,
            @window_start, @window_end, @max_proposals, @write_to_database,
            @run_directory, @output_path, @total_entity_count
         )
         on conflict (id) do update
         set
            started_at = excluded.started_at,
            completed_at = null,
            status_id = excluded.status_id,
            client_mode = excluded.client_mode,
            base_address = excluded.base_address,
            requested_model = excluded.requested_model,
            api_key_source = excluded.api_key_source,
            allow_web_search = excluded.allow_web_search,
            web_search_tool_type = excluded.web_search_tool_type,
            plugin_id = excluded.plugin_id,
            search_date = excluded.search_date,
            window_start = excluded.window_start,
            window_end = excluded.window_end,
            max_proposals = excluded.max_proposals,
            write_to_database = excluded.write_to_database,
            run_directory = excluded.run_directory,
            output_path = excluded.output_path,
            total_entity_count = excluded.total_entity_count,
            completed_item_count = 0,
            failed_item_count = 0,
            proposal_count = 0,
            persisted_proposal_count = 0,
            error_message = null,
            updated_at = now()
         """;

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var command = new NpgsqlCommand(sql, connection);
      AddRunParameters(command, run);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task RecordItemAsync(
      string runId,
      AiActivitySearchRunItemRecord item,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into ai_activity_search_run_items (
            id, run_id, entity_id, entity_key, entity_name, status_id,
            proposal_count, persisted_proposal_count, result_path,
            failure_path, error_type, error_message, started_at,
            completed_at, duration_seconds
         )
         values (
            @id, @run_id, @entity_id, @entity_key, @entity_name, @status_id,
            @proposal_count, @persisted_proposal_count, @result_path,
            @failure_path, @error_type, @error_message, @started_at,
            @completed_at, @duration_seconds
         )
         """;

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var command = new NpgsqlCommand(sql, connection);
      command.Parameters.AddWithValue(
         "id",
         DeterministicGuid.Create(
            $"ai-activity-search-run-item:{runId}:{item.EntityKey}:" +
            $"{item.StartedAt:O}"
         )
      );
      command.Parameters.AddWithValue("run_id", runId);
      command.Parameters.AddWithValue(
         "entity_id",
         TryParseGuid(item.EntityKey, out var entityId)
            ? entityId
            : DBNull.Value
      );
      command.Parameters.AddWithValue("entity_key", item.EntityKey);
      command.Parameters.AddWithValue("entity_name", item.EntityName);
      command.Parameters.AddWithValue("status_id", item.Status);
      command.Parameters.AddWithValue(
         "proposal_count",
         (object?)item.ProposalCount ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "persisted_proposal_count",
         (object?)item.PersistedProposalCount ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "result_path",
         (object?)item.ResultPath ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "failure_path",
         (object?)item.FailurePath ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "error_type",
         (object?)item.ErrorType ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "error_message",
         (object?)item.ErrorMessage ?? DBNull.Value
      );
      command.Parameters.AddWithValue("started_at", item.StartedAt);
      command.Parameters.AddWithValue("completed_at", item.CompletedAt);
      command.Parameters.AddWithValue(
         "duration_seconds",
         item.DurationSeconds
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task CompleteAsync(
      string runId,
      string status,
      string? errorMessage,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_activity_search_runs r
         set
            completed_at = now(),
            status_id = @status_id,
            completed_item_count = stats.completed_count,
            failed_item_count = stats.failed_count,
            proposal_count = stats.proposal_count,
            persisted_proposal_count = stats.persisted_proposal_count,
            error_message = @error_message,
            updated_at = now()
         from (
            select
               count(*) filter (where status_id = 'completed')::integer
                  as completed_count,
               count(*) filter (where status_id = 'failed')::integer
                  as failed_count,
               coalesce(sum(proposal_count), 0)::integer as proposal_count,
               coalesce(sum(persisted_proposal_count), 0)::integer
                  as persisted_proposal_count
            from ai_activity_search_run_items
            where run_id = @id
         ) stats
         where r.id = @id
         """;

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var command = new NpgsqlCommand(sql, connection);
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue("status_id", status);
      command.Parameters.AddWithValue(
         "error_message",
         (object?)errorMessage ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static void AddRunParameters(
      NpgsqlCommand command,
      AiActivitySearchRunRecord run
   )
   {
      command.Parameters.AddWithValue("id", run.Id);
      command.Parameters.AddWithValue("started_at", run.StartedAt);
      command.Parameters.AddWithValue("status_id", run.Status);
      command.Parameters.AddWithValue("client_mode", run.ClientMode);
      command.Parameters.AddWithValue("base_address", run.BaseAddress);
      command.Parameters.AddWithValue("requested_model", run.RequestedModel);
      command.Parameters.AddWithValue("api_key_source", run.ApiKeySource);
      command.Parameters.AddWithValue("allow_web_search", run.AllowWebSearch);
      command.Parameters.AddWithValue(
         "web_search_tool_type",
         run.WebSearchToolType
      );
      command.Parameters.AddWithValue(
         "plugin_id",
         (object?)run.PluginId ?? DBNull.Value
      );
      command.Parameters.AddWithValue("search_date", run.SearchDate);
      command.Parameters.AddWithValue("window_start", run.WindowStart);
      command.Parameters.AddWithValue("window_end", run.WindowEnd);
      command.Parameters.AddWithValue("max_proposals", run.MaxProposals);
      command.Parameters.AddWithValue(
         "write_to_database",
         run.WriteToDatabase
      );
      command.Parameters.AddWithValue(
         "run_directory",
         (object?)run.RunDirectory ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "output_path",
         (object?)run.OutputPath ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "total_entity_count",
         run.TotalEntityCount
      );
   }

   private static bool TryParseGuid(string value, out object guid)
   {
      if(Guid.TryParse(value, out var parsed))
      {
         guid = parsed;

         return true;
      }

      guid = DBNull.Value;

      return false;
   }
}

public sealed record AiActivitySearchRunRecord(
   string Id,
   DateTimeOffset StartedAt,
   string Status,
   string ClientMode,
   string BaseAddress,
   string RequestedModel,
   string ApiKeySource,
   bool AllowWebSearch,
   string WebSearchToolType,
   string? PluginId,
   DateOnly SearchDate,
   DateOnly WindowStart,
   DateOnly WindowEnd,
   int MaxProposals,
   bool WriteToDatabase,
   string? RunDirectory,
   string? OutputPath,
   int TotalEntityCount
);

public sealed record AiActivitySearchRunItemRecord(
   string EntityKey,
   string EntityName,
   string Status,
   int? ProposalCount,
   int? PersistedProposalCount,
   string? ResultPath,
   string? FailurePath,
   string? ErrorType,
   string? ErrorMessage,
   DateTimeOffset StartedAt,
   DateTimeOffset CompletedAt,
   decimal DurationSeconds
);
