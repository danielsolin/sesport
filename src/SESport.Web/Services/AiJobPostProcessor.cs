using System.Text.Json;

using SESport.AI.Interfaces;
using SESport.AI.Jobs;
using SESport.Core.AI;
using SESport.Data;
using SESport.Core.Sources;

namespace SESport.Web.Services;

public sealed class AiJobPostProcessor(
   AiJobRunner inner,
   IAiJobRunRepository runRepository,
   ActivityRepository activityRepository,
   AdminRepository adminRepository,
   SourceReferenceRepository sourceRepository,
   ILogger<AiJobPostProcessor> logger
) : IAiJobProcessor
{
   public async Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      await inner.ProcessRunAsync(runId, cancellationToken);
      await SaveCompletedActivityTextAsync(runId, cancellationToken);
      await SaveCompletedPersonFactsAsync(runId, cancellationToken);
      await SaveCompletedTranslationAsync(runId, cancellationToken);
   }

   private async Task SaveCompletedActivityTextAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(runId, cancellationToken);

      if(run is null ||
         (run.JobId != AiJobIds.GenerateActivityTeaser &&
            run.JobId != AiJobIds.FindActivityFacts) ||
         !string.Equals(
            run.StatusId,
            AiJobRunStatusIds.Completed,
            StringComparison.Ordinal
         ))
      {
         return;
      }

      if(!Guid.TryParse(run.CorrelationId, out var activityId))
      {
         logger.LogWarning(
            "Activity text run {RunId} has invalid correlation id.",
            runId
         );
         return;
      }

      var output = run.JobId == AiJobIds.GenerateActivityTeaser
         ? ExtractGeneratedTeaser(run.OutputText ?? string.Empty)
         : ExtractGeneratedFacts(run.OutputText ?? string.Empty);

      if(string.IsNullOrWhiteSpace(output))
      {
         logger.LogWarning(
            "Activity text run {RunId} completed without output.",
            runId
         );
         return;
      }

      if(run.JobId == AiJobIds.GenerateActivityTeaser)
      {
         await activityRepository.UpdateTeaserAsync(
            activityId,
            output,
            cancellationToken
         );
      }
      else
      {
         await activityRepository.UpdateFactsAsync(
            activityId,
            output,
            cancellationToken
         );
      }
   }

   private async Task SaveCompletedPersonFactsAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(runId, cancellationToken);

      if(run is null ||
         run.JobId != AiJobIds.FindPersonFacts ||
         !string.Equals(
            run.StatusId,
            AiJobRunStatusIds.Completed,
            StringComparison.Ordinal
         ))
      {
         return;
      }

      if(!Guid.TryParse(run.CorrelationId, out var entityId))
      {
         logger.LogWarning(
            "Person facts run {RunId} has invalid correlation id.",
            runId
         );
         return;
      }

      var facts = ExtractGeneratedPersonFacts(
         run.OutputText ?? string.Empty
      );

      if(facts is null)
      {
         logger.LogWarning(
            "Person facts run {RunId} completed without valid output.",
            runId
         );
         return;
      }

      await adminRepository.UpdateEntityPersonFactsAsync(
         entityId,
         facts.Birthdate,
         facts.Height,
         facts.Weight,
         facts.FormativeClub,
         cancellationToken
      );

      await sourceRepository.DeleteByCorrelationAsync(
         SourceCorrelationTypes.Entity,
         entityId.ToString(),
         cancellationToken,
         SourceKinds.PersonFacts
      );

      foreach(var source in facts.Sources)
      {
         await sourceRepository.CreateAsync(
            SourceCorrelationTypes.Entity,
            entityId.ToString(),
            SourceKinds.PersonFacts,
            source.Url,
            source.Title,
            source.Excerpt,
            null,
            cancellationToken
         );
      }
   }

   private async Task SaveCompletedTranslationAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(runId, cancellationToken);

      if(run is null ||
         run.JobId != AiJobIds.TranslateText ||
         !string.Equals(
            run.StatusId,
            AiJobRunStatusIds.Completed,
            StringComparison.Ordinal
         ))
      {
         return;
      }

      if(!Guid.TryParse(run.CorrelationId, out var entityId))
      {
         logger.LogWarning(
            "Translation run {RunId} has invalid correlation id.",
            runId
         );
         return;
      }

      var translatedText = run.OutputText?.Trim();

      if(string.IsNullOrWhiteSpace(translatedText))
      {
         logger.LogWarning(
            "Translation run {RunId} completed without output.",
            runId
         );
         return;
      }

      await adminRepository.UpdateEntityBioAsync(
         entityId,
         translatedText,
         cancellationToken
      );
   }

   internal static string? ExtractGeneratedTeaser(string outputText)
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.TryGetProperty("teaser", out var teaser) &&
            teaser.ValueKind == JsonValueKind.String)
         {
            return teaser.GetString();
         }
      }
      catch(JsonException)
      {
      }

      return outputText;
   }

   internal static string? ExtractGeneratedFacts(string outputText)
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.TryGetProperty("facts", out var facts) &&
            facts.ValueKind == JsonValueKind.String)
         {
            return facts.GetString();
         }
      }
      catch(JsonException)
      {
      }

      return outputText;
   }

   internal static PersonFactsOutput? ExtractGeneratedPersonFacts(
      string outputText
   )
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object)
         {
            return null;
         }

         DateOnly? birthdate = null;
         if(root.TryGetProperty("birthdate", out var birthdateValue) &&
            birthdateValue.ValueKind == JsonValueKind.String &&
            DateOnly.TryParseExact(
               birthdateValue.GetString(),
               "yyyy-MM-dd",
               out var parsedBirthdate
            ))
         {
            birthdate = parsedBirthdate;
         }

         int? height = ReadNullableInt32(root, "height");
         int? weight = ReadNullableInt32(root, "weight");
         var formativeClub = ReadNullableString(
            root,
            "formative_club"
         );
         var sources = ReadPersonFactSources(root);

         return new PersonFactsOutput(
            birthdate,
            height,
            weight,
            formativeClub,
            sources
         );
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static int? ReadNullableInt32(
      JsonElement root,
      string propertyName
   )
   {
      return root.TryGetProperty(propertyName, out var value) &&
         value.ValueKind == JsonValueKind.Number &&
         value.TryGetInt32(out var result)
            ? result
            : null;
   }

   private static IReadOnlyList<PersonFactSource> ReadPersonFactSources(
      JsonElement root
   )
   {
      if(!root.TryGetProperty("sources", out var sources) ||
         sources.ValueKind != JsonValueKind.Array)
      {
         return [];
      }

      var result = new List<PersonFactSource>();
      var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(var source in sources.EnumerateArray())
      {
         if(source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty("url", out var urlValue) ||
            urlValue.ValueKind != JsonValueKind.String)
         {
            continue;
         }

         var url = urlValue.GetString()?.Trim();
         if(string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
               uri.Scheme != Uri.UriSchemeHttps) ||
            !urls.Add(url))
         {
            continue;
         }

         result.Add(
            new PersonFactSource(
               url,
               ReadNullableString(source, "title"),
               ReadNullableString(source, "excerpt")
            )
         );
      }

      return result;
   }

   private static string? ReadNullableString(
      JsonElement source,
      string propertyName
   )
   {
      return source.TryGetProperty(propertyName, out var value) &&
         value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;
   }

   internal sealed record PersonFactsOutput(
      DateOnly? Birthdate,
      int? Height,
      int? Weight,
      string? FormativeClub,
      IReadOnlyList<PersonFactSource> Sources
   );

   internal sealed record PersonFactSource(
      string Url,
      string? Title,
      string? Excerpt
   );

}
