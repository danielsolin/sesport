using System.Globalization;
using System.Text;
using SESport.Core.Domain;

namespace SESport.Core.Broadcast;

public static class BroadcastEntityFilter
{
   public static IReadOnlyList<BroadcastEntityOption> FilterSelectableEntities(
      IEnumerable<BroadcastEntityOption> entities
   )
   {
      return entities
         .Where(entity =>
            entity.Type == TrackedEntityTypeIds.Person ||
            entity.Type == TrackedEntityTypeIds.NationalTeam)
         .ToList();
   }

   public static IReadOnlyList<Guid> MatchPersonEntityIds(
      IEnumerable<BroadcastEntityOption> entities,
      IReadOnlyCollection<string> participantNames
   )
   {
      var personEntities = FilterSelectableEntities(entities)
         .Where(entity => entity.Type == TrackedEntityTypeIds.Person)
         .ToList();
      var entityByName = personEntities
         .Where(entity => !string.IsNullOrWhiteSpace(entity.Name))
         .GroupBy(entity => NormalizeName(entity.Name))
         .Where(group => !string.IsNullOrWhiteSpace(group.Key))
         .ToDictionary(group => group.Key, group => group.First().Id);
      var matchedEntityIds = new List<Guid>();

      foreach(var participantName in participantNames)
      {
         var normalizedName = NormalizeName(participantName);

         if(string.IsNullOrWhiteSpace(normalizedName))
         {
            continue;
         }

         if(!entityByName.TryGetValue(normalizedName, out var entityId))
         {
            continue;
         }

         if(!matchedEntityIds.Contains(entityId))
         {
            matchedEntityIds.Add(entityId);
         }
      }

      return matchedEntityIds;
   }

   private static string NormalizeName(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder(normalized.Length);

      foreach(var character in normalized)
      {
         if(CharUnicodeInfo.GetUnicodeCategory(character) ==
            UnicodeCategory.NonSpacingMark)
         {
            continue;
         }

         if(char.IsLetterOrDigit(character))
         {
            builder.Append(char.ToUpperInvariant(character));
         }
         else if(char.IsWhiteSpace(character))
         {
            builder.Append(' ');
         }
      }

      return string.Join(
         " ",
         builder
            .ToString()
            .Split(
               ' ',
               StringSplitOptions.RemoveEmptyEntries
                  | StringSplitOptions.TrimEntries
            )
      );
   }
}
