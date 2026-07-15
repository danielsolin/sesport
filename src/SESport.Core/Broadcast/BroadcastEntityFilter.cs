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
      var entityByName = CreateNameLookup(
         personEntities,
         entity => entity.Name,
         entity => entity.Id
      );
      var aliasByName = CreateNameLookup(
         personEntities.Where(entity =>
            !string.IsNullOrWhiteSpace(entity.AliasName)
         ),
         entity => entity.AliasName,
         entity => entity.Id
      );
      var matchedEntityIds = new List<Guid>();

      foreach(var participantName in participantNames)
      {
         var normalizedName = NormalizeParticipantName(participantName);

         if(string.IsNullOrWhiteSpace(normalizedName))
         {
            continue;
         }

         if(!entityByName.TryGetValue(normalizedName, out var entityId) &&
            !aliasByName.TryGetValue(normalizedName, out entityId))
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

   public static string NormalizeLooseName(string value)
   {
      var normalized = NormalizeName(value);
      var builder = new StringBuilder(normalized.Length);
      var previousLetter = '\0';

      foreach(var character in normalized)
      {
         if(char.IsLetter(character))
         {
            if(character == previousLetter)
            {
               continue;
            }

            previousLetter = character;
         }
         else
         {
            previousLetter = '\0';
         }

         builder.Append(character);
      }

      return builder.ToString();
   }

   public static string NormalizeParticipantName(string value)
   {
      return NormalizeName(BroadcastParticipantNameFormatter.Format(value));
   }

   public static IReadOnlyDictionary<string, Guid> CreateNameLookup<T>(
      IEnumerable<T> items,
      Func<T, string?> nameSelector,
      Func<T, Guid> idSelector
   )
   {
      var itemList = items.ToList();
      var lookup = new Dictionary<string, Guid>(
         StringComparer.OrdinalIgnoreCase
      );

      AddNameVariants(
         lookup,
         itemList,
         nameSelector,
         idSelector,
         NormalizeName
      );
      AddNameVariants(
         lookup,
         itemList,
         nameSelector,
         idSelector,
         NormalizeLooseName
      );

      return lookup;
   }

   private static void AddNameVariants<T>(
      IDictionary<string, Guid> lookup,
      IEnumerable<T> items,
      Func<T, string?> nameSelector,
      Func<T, Guid> idSelector,
      Func<string, string> normalizer
   )
   {
      foreach(var item in items)
      {
         var name = nameSelector(item);

         if(string.IsNullOrWhiteSpace(name))
         {
            continue;
         }

         var normalizedName = normalizer(name);

         if(string.IsNullOrWhiteSpace(normalizedName) ||
            lookup.ContainsKey(normalizedName))
         {
            continue;
         }

         lookup[normalizedName] = idSelector(item);
      }
   }
}
