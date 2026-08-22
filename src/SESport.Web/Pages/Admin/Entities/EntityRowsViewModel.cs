using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Entities;

public sealed record EntityRowsViewModel(
   IReadOnlyList<EntityListItem> Entities,
   string SearchUrlBase,
   string PersonFactsUrl,
   IReadOnlyList<ReferenceRow> WatchPriorities,
   string FemaleGenderId,
   string MaleGenderId
);
