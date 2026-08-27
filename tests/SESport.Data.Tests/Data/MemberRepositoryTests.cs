using Npgsql;

using SESport.Core.Members;

namespace SESport.Core.Tests.Data;

public sealed class MemberRepositoryTests
{
   [Fact]
   public async Task LoginTokenCreatesVerifiedMemberAndCanOnlyBeConsumedOnce()
   {
      var email = $"member-{Guid.NewGuid():N}@example.test";
      var normalizedEmail = email.ToLowerInvariant();
      var rawToken = MemberLoginToken.Generate();
      var requestedAt = DateTimeOffset.UtcNow;
      await using var dataSource = CreateDataSource();
      var repository = new MemberRepository(dataSource);

      try
      {
         Assert.True(
            await repository.TryCreateLoginTokenAsync(
               email,
               normalizedEmail,
               MemberLoginToken.Hash(rawToken),
               requestedAt,
               requestedAt.AddMinutes(15),
               requestedAt.AddMinutes(-1),
               requestedAt.AddHours(-1),
               5,
               CancellationToken.None
            )
         );

         var member = await repository.ConsumeLoginTokenAsync(
            MemberLoginToken.Hash(rawToken),
            requestedAt.AddMinutes(1),
            CancellationToken.None
         );

         Assert.NotNull(member);
         Assert.Equal(normalizedEmail, member!.NormalizedEmail);
         Assert.NotNull(member.EmailVerifiedAt);
         Assert.NotNull(member.LastLoginAt);
         Assert.Null(
            await repository.ConsumeLoginTokenAsync(
               MemberLoginToken.Hash(rawToken),
               requestedAt.AddMinutes(2),
               CancellationToken.None
            )
         );
      }
      finally
      {
         await DeleteMemberAsync(dataSource, normalizedEmail);
      }
   }

   [Fact]
   public async Task LoginTokenRequestsAreRateLimitedPerMember()
   {
      var email = $"member-{Guid.NewGuid():N}@example.test";
      var normalizedEmail = email.ToLowerInvariant();
      var requestedAt = DateTimeOffset.UtcNow;
      await using var dataSource = CreateDataSource();
      var repository = new MemberRepository(dataSource);

      try
      {
         Assert.True(
            await repository.TryCreateLoginTokenAsync(
               email,
               normalizedEmail,
               MemberLoginToken.Hash(MemberLoginToken.Generate()),
               requestedAt,
               requestedAt.AddMinutes(15),
               requestedAt.AddMinutes(-1),
               requestedAt.AddHours(-1),
               1,
               CancellationToken.None
            )
         );
         Assert.False(
            await repository.TryCreateLoginTokenAsync(
               email,
               normalizedEmail,
               MemberLoginToken.Hash(MemberLoginToken.Generate()),
               requestedAt.AddSeconds(2),
               requestedAt.AddMinutes(15),
               requestedAt,
               requestedAt.AddHours(-1),
               1,
               CancellationToken.None
            )
         );
      }
      finally
      {
         await DeleteMemberAsync(dataSource, normalizedEmail);
      }
   }

   private static async Task DeleteMemberAsync(
      NpgsqlDataSource dataSource,
      string normalizedEmail
   )
   {
      const string sql = """
         delete from members
         where email_normalized = @email_normalized
         """;
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("email_normalized", normalizedEmail);
      await command.ExecuteNonQueryAsync();
   }
}
