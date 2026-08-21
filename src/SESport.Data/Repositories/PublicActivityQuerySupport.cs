using Npgsql;

namespace SESport.Data.Repositories;

internal static class PublicActivityQuerySupport
{
   internal const string TestActivityTitle = "Test Activity";

   internal const string TestActivitySlugPattern =
      "test-activity-%";

   internal const string ExclusionClause = """
      and not (
         (
            a.title = @test_activity_title
            or coalesce(a.slug, '') like @test_activity_slug_pattern
         )
         and a.published_at is null
      )
      """;

   internal static void AddExclusionParameters(NpgsqlCommand command)
   {
      command.Parameters.AddWithValue(
         "test_activity_title",
         TestActivityTitle
      );
      command.Parameters.AddWithValue(
         "test_activity_slug_pattern",
         TestActivitySlugPattern
      );
   }
}
