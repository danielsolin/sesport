namespace SESport.AI.WebPages;

/// <summary>
/// A Playwright launch strategy: which bundled browser or channel to use.
/// </summary>
internal sealed record BrowserStrategyDescriptor(
   string Id,
   BrowserEngine Engine,
   string? Channel,
   bool UseBrowserUserAgent
)
{
   internal static readonly BrowserStrategyDescriptor[] All =
   [
      new("chromium-bundled", BrowserEngine.Chromium, null, true),
      new("chromium-channel", BrowserEngine.Chromium, "chromium", false),
      new("chrome-channel", BrowserEngine.Chromium, "chrome", false),
      new("firefox-bundled", BrowserEngine.Firefox, null, false),
      new("webkit-bundled", BrowserEngine.Webkit, null, false)
   ];
}

internal enum BrowserEngine
{
   Chromium,
   Firefox,
   Webkit
}
