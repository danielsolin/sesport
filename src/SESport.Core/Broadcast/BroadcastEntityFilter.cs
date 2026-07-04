using System.Globalization;
using System.Text;

namespace SESport.Core.Broadcast;

public static class BroadcastEntityFilter
{
   private static readonly IReadOnlyList<string> NonOrganizationEntityTypeIds =
   [
      TrackedEntityTypeIds.Person,
      TrackedEntityTypeIds.Pair
   ];

   public static bool IsOrganizationEntityType(string entityTypeId)
   {
      return !NonOrganizationEntityTypeIds.Any(
         nonOrganizationEntityTypeId =>
            string.Equals(
               entityTypeId,
               nonOrganizationEntityTypeId,
               StringComparison.OrdinalIgnoreCase
            )
      );
   }

   public static string GetNonOrganizationEntityTypeSql()
   {
      return string.Join(
         ", ",
         NonOrganizationEntityTypeIds.Select(
            entityTypeId => $"'{entityTypeId}'"
         )
      );
   }

   public static string GetNonOrganizationEntityTypePredicateSql(
      string entityTypeSql
   )
   {
      return $"{entityTypeSql} not in ({GetNonOrganizationEntityTypeSql()})";
   }

   public static IReadOnlyList<BroadcastEntityOption> FilterSelectableEntities(
      IEnumerable<BroadcastEntityOption> entities
   )
   {
      return entities
         .Where(entity =>
            entity.Type == TrackedEntityTypeIds.Person)
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
            var aliasMatch = personEntities
               .Where(entity => !string.IsNullOrWhiteSpace(entity.AliasName))
               .FirstOrDefault(entity =>
                  string.Equals(
                     NormalizeName(entity.AliasName!),
                     normalizedName,
                     StringComparison.OrdinalIgnoreCase
                  )
               );

            if(aliasMatch is null)
            {
               continue;
            }

            entityId = aliasMatch.Id;
         }

         if(!matchedEntityIds.Contains(entityId))
         {
            matchedEntityIds.Add(entityId);
         }
      }

      return matchedEntityIds;
   }

   public static string NormalizeName(string value)
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
