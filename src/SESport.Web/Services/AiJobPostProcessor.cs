using System.Text.Json;

using SESport.AI.Jobs;
using SESport.Core.AI;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;

namespace SESport.Web.Services;

public sealed class AiJobPostProcessor(
   AiJobRunner inner,
   IAiJobRunRepository runRepository,
   ActivityRepository activityRepository,
   FactRepository factRepository,
   AdminRepository adminRepository,
   SourceReferenceRepository sourceRepository,
   ActivityParticipantAiResultService activityParticipantAiResultService,
   ILogger<AiJobPostProcessor> logger
) : IAiJobProcessor
{
   public async Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      await inner.ProcessRunAsync(runId, cancellationToken);
      await activityParticipantAiResultService.TryApplyRunAsync(
         runId,
         cancellationToken
      );
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
            run.JobId != AiJobIds.FindActivityGroupFacts) ||
         !string.Equals(
            run.StatusId,
            AiJobRunStatusIds.Completed,
            StringComparison.Ordinal
         ))
      {
         return;
      }

      if(!Guid.TryParse(run.CorrelationId, out var targetId))
      {
         logger.LogWarning(
            "Activity text run {RunId} has invalid correlation id.",
            runId
         );
         return;
      }

      var activityFacts = run.JobId == AiJobIds.FindActivityGroupFacts
         ? ExtractGeneratedActivityFacts(run.OutputText ?? string.Empty)
         : null;
      var output = run.JobId == AiJobIds.GenerateActivityTeaser
         ? ExtractGeneratedTeaser(run.OutputText ?? string.Empty)
         : null;

      if(run.JobId == AiJobIds.GenerateActivityTeaser &&
         string.IsNullOrWhiteSpace(output))
      {
         logger.LogWarning(
            "Activity text run {RunId} completed without output.",
            runId
         );
         return;
      }

      if(run.JobId == AiJobIds.FindActivityGroupFacts &&
         activityFacts is null)
      {
         logger.LogWarning(
            "Activity facts run {RunId} completed without valid facts.",
            runId
         );
         return;
      }

      bool wasApplied;

      if(run.JobId == AiJobIds.GenerateActivityTeaser)
      {
         wasApplied = await activityRepository.UpdateTeaserAsync(
            targetId,
            output!,
            cancellationToken
         );
      }
      else
      {
         var createdFacts = await factRepository.AddForActivityGroupAsync(
            targetId,
            activityFacts!.Facts,
            cancellationToken
         );
         wasApplied = createdFacts.Count > 0;
      }

      if(!wasApplied)
      {
         return;
      }

      await runRepository.RecordApplicationAsync(
         runId,
         run.JobId == AiJobIds.FindActivityGroupFacts
            ? AiJobRunApplicationTargetTypes.ActivityGroup
            : AiJobRunApplicationTargetTypes.Activity,
         targetId.ToString(),
         cancellationToken
      );
   }

   private async Task SaveCompletedPersonFactsAsync(
      Guid runId,
      CancellationToken cancellationToken
   )
   {
      var run = await runRepository.GetRunAsync(runId, cancellationToken);

      if(run is null ||
         run.JobId != AiJobIds.FindPersonData ||
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

      var wasApplied = await adminRepository.UpdateEntityPersonFactsAsync(
         entityId,
         facts.Birthdate,
         facts.Height,
         facts.Weight,
         facts.FormativeClub,
         cancellationToken
      );
      if(!wasApplied)
      {
         return;
      }

      await runRepository.RecordApplicationAsync(
         runId,
         AiJobRunApplicationTargetTypes.Entity,
         entityId.ToString(),
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

      var wasApplied = await adminRepository.UpdateEntityBioAsync(
         entityId,
         translatedText,
         cancellationToken
      );
      if(!wasApplied)
      {
         return;
      }

      await runRepository.RecordApplicationAsync(
         runId,
         AiJobRunApplicationTargetTypes.Entity,
         entityId.ToString(),
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

   internal static ActivityFactsOutput? ExtractGeneratedActivityFacts(
      string outputText
   )
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(!root.TryGetProperty("facts", out var facts) ||
            facts.ValueKind != JsonValueKind.Array)
         {
            return null;
         }

         var result = new List<FactDraft>();
         foreach(var fact in facts.EnumerateArray())
         {
            if(fact.ValueKind != JsonValueKind.Object ||
               !fact.TryGetProperty("text", out var textValue) ||
               textValue.ValueKind != JsonValueKind.String)
            {
               continue;
            }

            var text = textValue.GetString()?.Trim();
            var sources = ReadActivityFactSources(fact);

            if(!string.IsNullOrWhiteSpace(text) && sources.Count > 0)
            {
               result.Add(new FactDraft(text, sources));
            }
         }

         return result.Count == 3 ? new ActivityFactsOutput(result) : null;
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static IReadOnlyList<FactSourceDraft> ReadActivityFactSources(
      JsonElement root
   )
   {
      if(!root.TryGetProperty("sources", out var sources) ||
         sources.ValueKind != JsonValueKind.Array)
      {
         return [];
      }

      var result = new List<FactSourceDraft>();
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
            new FactSourceDraft(
               url,
               ReadNullableString(source, "title"),
               ReadNullableString(source, "excerpt")
            )
         );
      }

      return result;
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
               DateDisplay.DateOnlyFormat,
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
      if(!source.TryGetProperty(propertyName, out var value) ||
         value.ValueKind != JsonValueKind.String)
      {
         return null;
      }

      var text = value.GetString()?.Trim();
      return string.IsNullOrWhiteSpace(text) ? null : text;
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

   internal sealed record ActivityFactsOutput(
      IReadOnlyList<FactDraft> Facts
   );

}
