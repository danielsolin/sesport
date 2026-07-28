using Npgsql;

namespace SESport.Data.Repositories;

public sealed class AiAutomationRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<string>> GetEnabledJobIdsAsync(
      string eventId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select r.job_id
         from ai_automation_rules r
         join ai_jobs j on j.id = r.job_id
         where r.event_id = @event_id
           and r.enabled
           and j.enabled
         order by r.created_at
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("event_id", eventId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var jobIds = new List<string>();

      while(await reader.ReadAsync(cancellationToken))
      {
         jobIds.Add(reader.GetString(0));
      }

      return jobIds;
   }
}
