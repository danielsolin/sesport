namespace SESport.AI.WebPages;

/// <summary>
/// Ordered, human-readable history of every transport stage attempted for
/// one fetch. Feeds the structured final failure message so a result like
/// "blocked" is not reported when curl later proved a 404.
/// </summary>
internal sealed class WebPageFetchLedger
{
   private readonly Uri _url;
   private readonly List<string> _entries = [];

   internal WebPageFetchLedger(Uri url)
   {
      _url = url;
   }

   internal void Add(string stage, string detail)
   {
      _entries.Add($"[{stage}] {detail}");
   }

   internal string BuildSummary()
   {
      return string.Join("; ", _entries);
   }
}
