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

   private static readonly IReadOnlyList<string>
      NonBroadcastOrganizationEntityTypeIds =
   [
      TrackedEntityTypeIds.Person,
      TrackedEntityTypeIds.Pair,
      TrackedEntityTypeIds.Team
   ];

   public static bool IsBroadcastOrganizationEntityType(
      string entityTypeId
   )
   {
      return !NonBroadcastOrganizationEntityTypeIds.Any(
         nonOrganizationEntityTypeId =>
            string.Equals(
               entityTypeId,
               nonOrganizationEntityTypeId,
               StringComparison.OrdinalIgnoreCase
            )
      );
   }

   public static string GetBroadcastOrganizationEntityTypeSql()
   {
      return string.Join(
         ", ",
         NonBroadcastOrganizationEntityTypeIds.Select(
            entityTypeId => $"'{entityTypeId}'"
         )
      );
   }

   public static string GetBroadcastOrganizationEntityTypePredicateSql(
      string entityTypeSql
   )
   {
      return $"{entityTypeSql} not in (" +
         $"{GetBroadcastOrganizationEntityTypeSql()})";
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

         Guid entityId;
         if(TryGetNormalizedNameMatch(
               entityByName,
               normalizedName,
               out entityId
            ) ||
            TryGetNormalizedNameMatch(
               aliasByName,
               normalizedName,
               out entityId
            ) ||
            TryGetNormalizedFuzzyNameMatch(
               entityByName.Concat(aliasByName),
               normalizedName,
               out entityId
            ))
         {
            if(!matchedEntityIds.Contains(entityId))
            {
               matchedEntityIds.Add(entityId);
            }
         }
      }

      return matchedEntityIds;
   }

   public static bool TryGetNameMatch(
      IReadOnlyDictionary<string, Guid> entityIdsByName,
      string value,
      out Guid entityId
   )
   {
      var normalizedName = NormalizeParticipantName(value);

      return TryGetNormalizedNameMatch(
         entityIdsByName,
         normalizedName,
         out entityId
      );
   }

   public static bool TryGetFuzzyNameMatch(
      IEnumerable<KeyValuePair<string, Guid>> entityIdsByName,
      string value,
      out Guid entityId
   )
   {
      var normalizedName = NormalizeParticipantName(value);

      return TryGetNormalizedFuzzyNameMatch(
         entityIdsByName,
         normalizedName,
         out entityId
      );
   }

   private static bool TryGetNormalizedNameMatch(
      IReadOnlyDictionary<string, Guid> entityIdsByName,
      string normalizedName,
      out Guid entityId
   )
   {
      if(string.IsNullOrWhiteSpace(normalizedName))
      {
         entityId = default;
         return false;
      }

      return entityIdsByName.TryGetValue(normalizedName, out entityId);
   }

   private static bool TryGetNormalizedFuzzyNameMatch(
      IEnumerable<KeyValuePair<string, Guid>> entityIdsByName,
      string normalizedName,
      out Guid entityId
   )
   {
      if(string.IsNullOrWhiteSpace(normalizedName))
      {
         entityId = default;
         return false;
      }

      Guid? matchedEntityId = null;

      foreach(var (candidateName, candidateEntityId) in entityIdsByName)
      {
         if(!IsOneEditAway(normalizedName, candidateName))
         {
            continue;
         }

         if(matchedEntityId is null)
         {
            matchedEntityId = candidateEntityId;
            continue;
         }

         if(matchedEntityId.Value != candidateEntityId)
         {
            entityId = default;
            return false;
         }
      }

      if(matchedEntityId is null)
      {
         entityId = default;
         return false;
      }

      entityId = matchedEntityId.Value;
      return true;
   }

   private static bool IsOneEditAway(string left, string right)
   {
      var lengthDifference = Math.Abs(left.Length - right.Length);

      if(lengthDifference > 1)
      {
         return false;
      }

      if(left.Length == right.Length)
      {
         var firstMismatchIndex = -1;
         var secondMismatchIndex = -1;

         for(var i = 0; i < left.Length; i++)
         {
            if(left[i] == right[i])
            {
               continue;
            }

            if(firstMismatchIndex >= 0)
            {
               secondMismatchIndex = i;
               break;
            }

            firstMismatchIndex = i;
         }

         if(firstMismatchIndex < 0)
         {
            return false;
         }

         if(secondMismatchIndex < 0)
         {
            return true;
         }

         return secondMismatchIndex == firstMismatchIndex + 1 &&
            left[firstMismatchIndex] == right[secondMismatchIndex] &&
            left[secondMismatchIndex] == right[firstMismatchIndex];
      }

      var longer = left.Length > right.Length ? left : right;
      var shorter = left.Length > right.Length ? right : left;
      var longerIndex = 0;
      var shorterIndex = 0;
      var skipped = false;

      while(longerIndex < longer.Length && shorterIndex < shorter.Length)
      {
         if(longer[longerIndex] == shorter[shorterIndex])
         {
            longerIndex++;
            shorterIndex++;
            continue;
         }

         if(skipped)
         {
            return false;
         }

         skipped = true;
         longerIndex++;
      }

      return true;
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
