using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Ajax.Search;

public sealed record ActivityGroupSuggestionViewModel(
   IReadOnlyList<LookupOption> Results,
   string Term
);
