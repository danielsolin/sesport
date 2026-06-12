using SESport.Core.Broadcast;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Activities;

internal static class ActivityBroadcastPrefillBuilder
{
   internal static IReadOnlyList<Guid> NormalizeBroadcastIds(
      IEnumerable<Guid> ids
   )
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .Take(1)
         .ToList();
   }

   internal static IReadOnlyList<Guid> SelectLinkedEntityIds(
      IReadOnlyList<EntityOption> entities,
      BroadcastParticipationCheck? participationCheck
   )
   {
      if(participationCheck is null ||
         participationCheck.SwedishParticipants.Count == 0)
      {
         return [];
      }

      return ActivityEntityFilter.MatchPersonEntityIds(
         entities,
         participationCheck.SwedishParticipants
      );
   }

   internal static string CreateEvidenceComment(
      BroadcastActivitySource broadcast,
      BroadcastParticipationCheck? participationCheck
   )
   {
      var lines = new List<string>
      {
         CreateBroadcastSummary(broadcast)
      };

      if(participationCheck is null)
      {
         return string.Join(Environment.NewLine, lines);
      }

      lines.Add($"AI participation: {participationCheck.SummaryText}");

      if(participationCheck.SwedishParticipants.Count > 0)
      {
         lines.Add(
            "AI participants: " +
            string.Join(", ", participationCheck.SwedishParticipants)
         );
      }

      if(participationCheck.SourceUrls.Count > 0)
      {
         lines.Add("AI sources:");
         lines.AddRange(
            participationCheck.SourceUrls.Select(url => $"- {url}")
         );
      }

      return string.Join(Environment.NewLine, lines);
   }

   private static string CreateBroadcastSummary(BroadcastActivitySource broadcast)
   {
      var localStart = BroadcastRepository.ToLocal(broadcast.StartsAt);
      var localEnd = BroadcastRepository.ToLocal(broadcast.EndsAt);

      return string.Join(
         " ",
         [
            $"{localStart:yyyy-MM-dd HH:mm}-{localEnd:HH:mm}",
            broadcast.ChannelName,
            broadcast.Title,
            broadcast.Description ?? string.Empty
         ]
      ).Trim();
   }
}
