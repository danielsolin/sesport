using Npgsql;

using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class AdminMemberRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<AdminMemberListItem>> GetMembersAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            member.id,
            member.email,
            member.created_at,
            member.last_login_at,
            coalesce(watch_counts.watch_count, 0)::int,
            coalesce(push_counts.sent_count, 0)::int,
            coalesce(login_token_counts.created_count, 0)::int,
            coalesce(login_token_counts.consumed_count, 0)::int
         from members member
         left join (
            select
               member_id,
               count(*)::int as watch_count
            from member_entity_watches
            group by member_id
         ) watch_counts
            on watch_counts.member_id = member.id
         left join (
            select
               member_id,
               count(*)::int as sent_count
            from member_activity_push_notifications
            where sent_at is not null
            group by member_id
         ) push_counts
            on push_counts.member_id = member.id
         left join (
            select
               member_id,
               count(*)::int as created_count,
               count(*) filter (
                  where consumed_at is not null
               )::int as consumed_count
            from member_login_tokens
            group by member_id
         ) login_token_counts
            on login_token_counts.member_id = member.id
         order by member.email_normalized, member.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var members = new List<AdminMemberListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         members.Add(
            new AdminMemberListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetFieldValue<DateTimeOffset>(2),
               reader.IsDBNull(3)
                  ? null
                  : reader.GetFieldValue<DateTimeOffset>(3),
               reader.GetInt32(4),
               reader.GetInt32(5),
               reader.GetInt32(6),
               reader.GetInt32(7)
            )
         );
      }

      return members;
   }
}
