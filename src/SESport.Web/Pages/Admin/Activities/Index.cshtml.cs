using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Data.Repositories;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Activities;

public class IndexModel(
   ActivityRepository repository,
   ActivityIndexPageService indexService
) : PageModel
{
   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.Status)]
   public string? Status { get; set; } = ActivityListStatusIds.All;

   [BindProperty(SupportsGet = true)]
   public List<string> SelectedSports { get; set; } = [];

   [BindProperty(SupportsGet = true)]
   public string SortColumn { get; set; } = "Time";

   [BindProperty(SupportsGet = true)]
   public bool SortAsc { get; set; } = true;

   public string DateText => DateDisplay.Format(SelectedDate);

   public DateOnly SelectedDate { get; private set; }

   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public IReadOnlyList<SelectListItem> SportOptions { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      var viewModel = await indexService.BuildAsync(
         HttpContext,
         Date,
         Status,
         SelectedSports,
         SortColumn,
         SortAsc,
         cancellationToken
      );

      ApplyViewModel(viewModel);
   }

   public bool GetNextSortAsc(string sortColumn) =>
      string.Equals(SortColumn, sortColumn, StringComparison.Ordinal)
         ? !SortAsc
         : true;

   public string GetSortIndicator(string sortColumn)
   {
      if(!string.Equals(SortColumn, sortColumn, StringComparison.Ordinal))
      {
         return string.Empty;
      }

      return SortAsc ? "▲" : "▼";
   }

   public Dictionary<string, string?> GetSortRouteValues(string sortColumn)
   {
      var routeValues = AdminRouteValueBuilder.CreateSortRouteValues(
         SelectedDate,
         GetNextSortAsc(sortColumn),
         SelectedSports
      );
      routeValues[RouteKeys.Status] = Status ?? ActivityListStatusIds.All;
      routeValues[RouteKeys.SortColumn] = sortColumn;

      return routeValues;
   }

   public string GetReturnUrl()
   {
      var routeValues = AdminRouteValueBuilder.CreateSortRouteValues(
         SelectedDate,
         SortAsc,
         SelectedSports
      );
      routeValues[RouteKeys.Status] = Status ?? ActivityListStatusIds.All;
      routeValues[RouteKeys.SortColumn] = SortColumn;

      return Url.Page("./Index", routeValues) ?? "/Admin/Activities";
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      DateOnly? date,
      string? status,
      string? sortColumn,
      bool? sortAsc,
      List<string>? selectedSports,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteAsync(id, cancellationToken);

      var routeValues = AdminRouteValueBuilder
         .CreateActivityRedirectRouteValues(
            indexService.GetRouteDate(date, status),
            indexService.NormalizeStatusOrDefault(status),
            indexService.NormalizeSortColumnOrDefault(sortColumn),
            sortAsc ?? true,
            selectedSports ?? SelectedSports
         );

      return RedirectToPage("./Index", routeValues);
   }

   private void ApplyViewModel(ActivityIndexViewModel viewModel)
   {
      SelectedDate = viewModel.SelectedDate;
      Status = viewModel.Status;
      SortColumn = viewModel.SortColumn;
      SortAsc = viewModel.SortAsc;
      SelectedSports = viewModel.SelectedSports.ToList();
      Activities = viewModel.Activities;
      SportOptions = viewModel.SportOptions;
      LoadError = viewModel.LoadError;
   }
}
