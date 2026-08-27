using Npgsql;

using SESport.Core.Members;
using SESport.Core.Members.Interfaces;

namespace SESport.Data.Members;

public sealed class MemberRepository(NpgsqlDataSource dataSource)
   : IMemberRepository
{
   public async Task<bool> TryCreateLoginTokenAsync(
      string email,
      string normalizedEmail,
      string tokenHash,
      DateTimeOffset requestedAt,
      DateTimeOffset expiresAt,
      DateTimeOffset cooldownThreshold,
      DateTimeOffset windowStart,
      int maxRequestsPerWindow,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      var memberId = Guid.NewGuid();
      const string upsertMemberSql = """
         insert into members (
            id,
            email,
            email_normalized
         )
         values (
            @id,
            @email,
            @email_normalized
         )
         on conflict (email_normalized)
         do update set
            email = excluded.email,
            updated_at = now()
         returning id
         """;
      await using var upsertMemberCommand = connection.CreateCommand();
      upsertMemberCommand.Transaction = transaction;
      upsertMemberCommand.CommandText = upsertMemberSql;
      upsertMemberCommand.Parameters.AddWithValue("id", memberId);
      upsertMemberCommand.Parameters.AddWithValue("email", email);
      upsertMemberCommand.Parameters.AddWithValue(
         "email_normalized",
         normalizedEmail
      );

      var memberIdValue = await upsertMemberCommand.ExecuteScalarAsync(
         cancellationToken
      );
      memberId = (Guid)(memberIdValue ??
         throw new InvalidOperationException(
            "The member upsert did not return an id."
         ));

      const string cleanupSql = """
         delete from member_login_tokens
         where member_id = @member_id
            and (
               consumed_at is not null
               or expires_at <= @requested_at
            )
         """;
      await using var cleanupCommand = connection.CreateCommand();
      cleanupCommand.Transaction = transaction;
      cleanupCommand.CommandText = cleanupSql;
      cleanupCommand.Parameters.AddWithValue("member_id", memberId);
      cleanupCommand.Parameters.AddWithValue("requested_at", requestedAt);
      await cleanupCommand.ExecuteNonQueryAsync(cancellationToken);

      const string rateLimitSql = """
         select
            count(*)::int,
            max(requested_at)
         from member_login_tokens
         where member_id = @member_id
            and requested_at >= @window_start
         """;
      await using var rateLimitCommand = connection.CreateCommand();
      rateLimitCommand.Transaction = transaction;
      rateLimitCommand.CommandText = rateLimitSql;
      rateLimitCommand.Parameters.AddWithValue("member_id", memberId);
      rateLimitCommand.Parameters.AddWithValue("window_start", windowStart);

      await using var rateLimitReader =
         await rateLimitCommand.ExecuteReaderAsync(cancellationToken);
      await rateLimitReader.ReadAsync(cancellationToken);
      var requestCount = rateLimitReader.GetInt32(0);
      var lastRequestedAt = rateLimitReader.IsDBNull(1)
         ? (DateTimeOffset?)null
         : rateLimitReader.GetFieldValue<DateTimeOffset>(1);
      await rateLimitReader.CloseAsync();

      if(requestCount >= maxRequestsPerWindow ||
         lastRequestedAt is not null &&
         lastRequestedAt > cooldownThreshold)
      {
         await transaction.CommitAsync(cancellationToken);
         return false;
      }

      const string insertTokenSql = """
         insert into member_login_tokens (
            id,
            member_id,
            token_hash,
            requested_at,
            expires_at
         )
         values (
            @id,
            @member_id,
            @token_hash,
            @requested_at,
            @expires_at
         )
         """;
      await using var insertTokenCommand = connection.CreateCommand();
      insertTokenCommand.Transaction = transaction;
      insertTokenCommand.CommandText = insertTokenSql;
      insertTokenCommand.Parameters.AddWithValue("id", Guid.NewGuid());
      insertTokenCommand.Parameters.AddWithValue("member_id", memberId);
      insertTokenCommand.Parameters.AddWithValue("token_hash", tokenHash);
      insertTokenCommand.Parameters.AddWithValue(
         "requested_at",
         requestedAt
      );
      insertTokenCommand.Parameters.AddWithValue("expires_at", expiresAt);
      await insertTokenCommand.ExecuteNonQueryAsync(cancellationToken);

      await transaction.CommitAsync(cancellationToken);
      return true;
   }

   public async Task<Member?> ConsumeLoginTokenAsync(
      string tokenHash,
      DateTimeOffset consumedAt,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      const string selectSql = """
         select
            token.id,
            member.id,
            member.email,
            member.email_normalized,
            member.email_verified_at,
            member.created_at,
            member.updated_at,
            member.last_login_at
         from member_login_tokens token
         join members member on member.id = token.member_id
         where token.token_hash = @token_hash
            and token.consumed_at is null
            and token.expires_at > @consumed_at
         for update
         """;
      await using var selectCommand = connection.CreateCommand();
      selectCommand.Transaction = transaction;
      selectCommand.CommandText = selectSql;
      selectCommand.Parameters.AddWithValue("token_hash", tokenHash);
      selectCommand.Parameters.AddWithValue("consumed_at", consumedAt);

      await using var reader = await selectCommand.ExecuteReaderAsync(
         cancellationToken
      );
      if(!await reader.ReadAsync(cancellationToken))
      {
         await reader.CloseAsync();
         await transaction.CommitAsync(cancellationToken);
         return null;
      }

      var tokenId = reader.GetGuid(0);
      var member = new Member(
         reader.GetGuid(1),
         reader.GetString(2),
         reader.GetString(3),
         ReadNullableDateTimeOffset(reader, 4),
         reader.GetFieldValue<DateTimeOffset>(5),
         reader.GetFieldValue<DateTimeOffset>(6),
         ReadNullableDateTimeOffset(reader, 7)
      );
      await reader.CloseAsync();

      const string consumeSql = """
         update member_login_tokens
         set consumed_at = @consumed_at
         where id = @id
         """;
      await using var consumeCommand = connection.CreateCommand();
      consumeCommand.Transaction = transaction;
      consumeCommand.CommandText = consumeSql;
      consumeCommand.Parameters.AddWithValue("id", tokenId);
      consumeCommand.Parameters.AddWithValue("consumed_at", consumedAt);
      await consumeCommand.ExecuteNonQueryAsync(cancellationToken);

      const string updateMemberSql = """
         update members
         set
            email_verified_at = coalesce(email_verified_at, @logged_in_at),
            last_login_at = @logged_in_at,
            updated_at = @logged_in_at
         where id = @id
         """;
      await using var updateMemberCommand = connection.CreateCommand();
      updateMemberCommand.Transaction = transaction;
      updateMemberCommand.CommandText = updateMemberSql;
      updateMemberCommand.Parameters.AddWithValue("id", member.Id);
      updateMemberCommand.Parameters.AddWithValue(
         "logged_in_at",
         consumedAt
      );
      await updateMemberCommand.ExecuteNonQueryAsync(cancellationToken);

      await transaction.CommitAsync(cancellationToken);

      return member with
      {
         EmailVerifiedAt = member.EmailVerifiedAt ?? consumedAt,
         UpdatedAt = consumedAt,
         LastLoginAt = consumedAt
      };
   }

   public async Task InvalidateLoginTokenAsync(
      string tokenHash,
      DateTimeOffset invalidatedAt,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update member_login_tokens
         set consumed_at = @invalidated_at
         where token_hash = @token_hash
            and consumed_at is null
         """;
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("token_hash", tokenHash);
      command.Parameters.AddWithValue("invalidated_at", invalidatedAt);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static DateTimeOffset? ReadNullableDateTimeOffset(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateTimeOffset>(ordinal);
   }
}
