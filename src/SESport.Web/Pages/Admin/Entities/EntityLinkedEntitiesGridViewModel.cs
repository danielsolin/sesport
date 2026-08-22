using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Entities;

public sealed record EntityLinkedEntitiesGridViewModel(
   IReadOnlyList<EntityLinkOption> Options,
   Guid? EntityId
);
