namespace SESport.MCP.Models;

public sealed class ActivityDatabaseToolOptions
{
   public const string SectionName = "Mcp:ActivityDatabase";

   public int DefaultSearchLimit { get; init; } = 10;

   public int MaximumSearchLimit { get; init; } = 20;

   public int MaximumSearchOffset { get; init; } = 100;
}
