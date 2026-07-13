using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class ProposalsModel(AuditRepository repository) : PageModel
{
   public const string CreatedSortColumn = "Created";
   public const string TimeSortColumn = "Time";
   public const string ActivitySortColumn = "Activity";
   public const string ProducerSortColumn = "Producer";
   public const string StatusSortColumn = "Status";
   public const string TypeSortColumn = "Type";
   public const string SportSortColumn = "Sport";

   public IReadOnlyList<ActivityProposalAuditItem> Proposals
   {
      get;
      private set;
   } = [];

   public string SortColumn { get; private set; } = CreatedSortColumn;

   public bool SortAsc { get; private set; }

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(
      string? sortColumn,
      bool sortAsc = false,
      CancellationToken cancellationToken = default
   )
   {
      SortColumn = NormalizeSortColumn(sortColumn);
      SortAsc = sortAsc;

      try
      {
         var proposals = await repository.GetProposalsAsync(cancellationToken);
         Proposals = SortProposals(proposals, SortColumn, SortAsc);
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   public bool GetNextSortAsc(string sortColumn) =>
      string.Equals(SortColumn, sortColumn, StringComparison.Ordinal)
         ? !SortAsc
         : GetDefaultSortAsc(sortColumn);

   public string GetSortIndicator(string sortColumn)
   {
      if(!string.Equals(SortColumn, sortColumn, StringComparison.Ordinal))
      {
         return string.Empty;
      }

      return SortAsc ? "▲" : "▼";
   }

   private static string NormalizeSortColumn(string? sortColumn) =>
      sortColumn switch
      {
         TimeSortColumn => TimeSortColumn,
         ActivitySortColumn => ActivitySortColumn,
         ProducerSortColumn => ProducerSortColumn,
         StatusSortColumn => StatusSortColumn,
         TypeSortColumn => TypeSortColumn,
         SportSortColumn => SportSortColumn,
         _ => CreatedSortColumn
      };

   private static bool GetDefaultSortAsc(string sortColumn) =>
      !string.Equals(sortColumn, CreatedSortColumn, StringComparison.Ordinal);

   private static IReadOnlyList<ActivityProposalAuditItem> SortProposals(
      IEnumerable<ActivityProposalAuditItem> proposals,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         TimeSortColumn => OrderByDirection(
            proposals,
            proposal => proposal.TimeText,
            sortAsc
         ),
         ActivitySortColumn => OrderByDirection(
            proposals,
            proposal => proposal.Title,
            sortAsc
         ),
         ProducerSortColumn => OrderByDirection(
            proposals,
            proposal => proposal.Producer,
            sortAsc
         ),
         StatusSortColumn => OrderByDirection(
            proposals,
            proposal => proposal.Status,
            sortAsc
         ),
         TypeSortColumn => OrderByDirection(
            proposals,
            proposal => proposal.ActivityType,
            sortAsc
         ),
         SportSortColumn => OrderByDirection(
            proposals,
            proposal => proposal.Sport,
            sortAsc
         ),
         _ => OrderByDirection(proposals, proposal => proposal.CreatedOn, sortAsc)
      };
   }

   private static IReadOnlyList<ActivityProposalAuditItem> OrderByDirection(
      IEnumerable<ActivityProposalAuditItem> proposals,
      Func<ActivityProposalAuditItem, string> keySelector,
      bool sortAsc
   )
   {
      var sortedProposals = sortAsc
         ? proposals.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase)
         : proposals.OrderByDescending(
            keySelector,
            StringComparer.OrdinalIgnoreCase
         );

      return sortedProposals.ThenByDescending(proposal => proposal.CreatedOn)
         .ToList();
   }

   private static IReadOnlyList<ActivityProposalAuditItem> OrderByDirection(
      IEnumerable<ActivityProposalAuditItem> proposals,
      Func<ActivityProposalAuditItem, DateTime> keySelector,
      bool sortAsc
   )
   {
      var sortedProposals = sortAsc
         ? proposals.OrderBy(keySelector)
         : proposals.OrderByDescending(keySelector);

      return sortedProposals.ThenBy(
            proposal => proposal.Title,
            StringComparer.OrdinalIgnoreCase
         )
         .ToList();
   }
}
