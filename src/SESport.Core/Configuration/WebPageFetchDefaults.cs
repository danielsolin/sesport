namespace SESport.Core.Configuration;

public static class WebPageFetchDefaults
{
   public const string BrowserUserAgentPrefix =
      "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
      "(KHTML, like Gecko) Chrome/";
   public const string BrowserUserAgentSuffix =
      ".0.0.0 Safari/537.36";
   public const int BrowserUserAgentFallbackMajorVersion = 125;
   public static readonly string BrowserUserAgentFallback =
      BrowserUserAgentPrefix +
      BrowserUserAgentFallbackMajorVersion +
      BrowserUserAgentSuffix;

   public const string BrowserAcceptHeader =
      "text/html,application/xhtml+xml,application/xml;q=0.9," +
      "image/avif,image/webp,*/*;q=0.8";
   public const string BrowserAcceptLanguageHeader = "en-US,en;q=0.9";
   public const string BrowserLocale = "en-US";
   public const string BrowserPlatform = "Linux";

   public const int MaximumUrlLength = 2048;
   public const string ImageOcrExecutable = "tesseract";
   public const string ImageOcrLanguage = "eng";
   public const int ImageOcrPageSegmentationMode = 3;

   public const string CutoffMarker = "[CUTOFF]";
   public const int MaxResponseCharacters = 50000;
   public const int IncompleteContentMinimumMarkerCount = 3;
   public const int RichContentMinimumCharacters = 1000;
   public const int MaxRelevantLinkCount = 20;
   public const int MaxTransientRetryAttempts = 3;
   public const int BrowserNavigationRetryAttempts = 2;
   public static readonly TimeSpan BrowserNavigationRetryDelay =
      TimeSpan.FromSeconds(2);
   public const int CurlMaxTimeSeconds = 30;
   public static readonly IReadOnlyList<TimeSpan> TransientRetryDelays =
   [
      TimeSpan.FromSeconds(2),
      TimeSpan.FromSeconds(5),
      TimeSpan.FromSeconds(10)
   ];

   public static readonly TimeSpan BrowserNavigationTimeout =
      TimeSpan.FromSeconds(30);
   public static readonly TimeSpan BrowserLoadStateTimeout =
      TimeSpan.FromSeconds(30);
   public static readonly TimeSpan BrowserContentStabilityTimeout =
      TimeSpan.FromSeconds(15);
   public static readonly TimeSpan BrowserContentStabilityInterval =
      TimeSpan.FromMilliseconds(500);
   public const int BrowserStableContentSampleCount = 3;
   public static readonly TimeSpan BrowserScrollTimeout =
      TimeSpan.FromSeconds(15);
   public static readonly TimeSpan BrowserScrollInterval =
      TimeSpan.FromMilliseconds(500);
   public const int BrowserScrollMaxSteps = 20;
   public const int BrowserStableScrollSampleCount = 2;
   public const int BrowserViewportWidth = 1440;
   public const int BrowserViewportHeight = 2400;
   public const int ImageOcrMinimumWidth = 600;
   public const int ImageOcrMinimumHeight = 400;
   public const int ImageOcrMinimumArea = 300000;
   public const int ImageOcrMaximumCandidateCount = 3;
   public const int ImageOcrMaximumBytes = 10 * 1024 * 1024;
   public const int ImageOcrMinimumWordCount = 6;
   public const double ImageOcrMinimumMeanConfidence = 60d;
   public static readonly TimeSpan ImageOcrTimeout =
      TimeSpan.FromSeconds(30);
}
