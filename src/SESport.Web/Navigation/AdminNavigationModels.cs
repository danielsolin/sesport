namespace SESport.Web.Navigation;

public sealed record AdminNavItem(string Title, string Href);

public sealed record AdminNavGroup(
   string Title,
   IReadOnlyList<AdminNavItem> Items
);
