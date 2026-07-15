using System.Text.Json;

using SESport.Core.Domain;

namespace SESport.Core.Broadcast;

public static class BroadcastParticipationCheckParser
{
   public static BroadcastParticipationCheck Parse(
      Guid runId,
      string statusId,
      int toolRoundCount,
      string? outputText,
      string? rawResponseText,
      string? errorMessage
   )
   {
      var sourceUrls = ParticipationSourceUrlExtractor.ExtractFromOutput(
         outputText
      );
      var resolvedSourceUrls = sourceUrls.Count > 0
         ? sourceUrls
         : ParticipationSourceUrlExtractor.Extract(rawResponseText);

      if(string.IsNullOrWhiteSpace(outputText))
      {
         return new BroadcastParticipationCheck(
            runId,
            statusId,
            toolRoundCount,
            null,
            [],
            resolvedSourceUrls,
            errorMessage
         );
      }

      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object)
         {
            throw new JsonException("Expected a JSON object.");
         }

         if(!TryGetStringProperty(
            root,
            "Participation",
            PrimaryCountry.LanguageName + "Participation",
            out var participation
         ))
         {
            throw new JsonException("Missing Participation property.");
         }

         var participants = new List<string>();

         if(TryGetArrayProperty(
            root,
            "Participants",
            PrimaryCountry.LanguageName + "Participants",
            out var participantsArray
         ))
         {
            foreach(var participant in participantsArray.EnumerateArray())
            {
               var participantName = ReadParticipantName(participant);

               if(!string.IsNullOrWhiteSpace(participantName))
               {
                  participants.Add(participantName);
               }
            }
         }

         return new BroadcastParticipationCheck(
            runId,
            statusId,
            toolRoundCount,
            participation,
            participants,
            resolvedSourceUrls,
            errorMessage
         );
      }
      catch(JsonException)
      {
         return new BroadcastParticipationCheck(
            runId,
            statusId,
            toolRoundCount,
            null,
            [],
            resolvedSourceUrls,
            errorMessage ?? "The model returned invalid JSON."
         );
      }
   }

   private static string? ReadParticipantName(JsonElement participant)
   {
      if(participant.ValueKind == JsonValueKind.String)
      {
         return participant.GetString();
      }

      if(participant.ValueKind == JsonValueKind.Object &&
         participant.TryGetProperty("Name", out var name) &&
         name.ValueKind == JsonValueKind.String)
      {
         return name.GetString();
      }

      return null;
   }

   private static bool TryGetStringProperty(
      JsonElement root,
      string propertyName,
      string legacyPropertyName,
      out string? value
   )
   {
      if(root.TryGetProperty(propertyName, out var property) &&
         property.ValueKind == JsonValueKind.String)
      {
         value = property.GetString();
         return true;
      }

      if(root.TryGetProperty(legacyPropertyName, out var legacyProperty) &&
         legacyProperty.ValueKind == JsonValueKind.String)
      {
         value = legacyProperty.GetString();
         return true;
      }

      value = null;
      return false;
   }

   private static bool TryGetArrayProperty(
      JsonElement root,
      string propertyName,
      string legacyPropertyName,
      out JsonElement value
   )
   {
      if(root.TryGetProperty(propertyName, out value) &&
         value.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      if(root.TryGetProperty(legacyPropertyName, out value) &&
         value.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      value = default;
      return false;
   }
}
