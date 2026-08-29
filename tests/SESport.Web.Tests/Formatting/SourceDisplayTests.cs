using SESport.Core.Configuration;
using SESport.Core.Sources;
using SESport.Data.Models;
using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class SourceDisplayTests
{
   [Theory]
   [InlineData(SourceKinds.ActivityEvidence, "Aktivitet")]
   [InlineData(SourceKinds.StreamLink, "Stream")]
   [InlineData(SourceKinds.ParticipationEvidence, "Deltagande")]
   [InlineData(SourceKinds.ParticipantStartEvidence, "Starttid")]
   [InlineData(SourceKinds.ParticipantStarEvidence, "Stjärna")]
   public void FormatKindTranslatesKnownKinds(
      string kind,
      string expected
   )
   {
      Assert.Equal(expected, SourceDisplay.FormatKind(kind));
   }

   [Fact]
   public void FormatKindUsesOneWordFallbackForUnknownKinds()
   {
      Assert.Equal("Källa", SourceDisplay.FormatKind("Unknown"));
   }

   [Fact]
   public void FindStreamLinkForChannelMatchesProviderTitle()
   {
      var source = new ActivitySourceListItem(
         SourceKinds.StreamLink,
         "https://stream.example/activity",
         "Viaplay"
      );

      var result = SourceDisplay.FindStreamLinkForChannel(
         [source],
         "viaplay"
      );

      Assert.Same(source, result);
   }

   [Fact]
   public void FindChannelLinkUrlUsesDatabaseCatalogAsFallback()
   {
      var result = SourceDisplay.FindChannelLinkUrlForChannel(
         [],
         "SVT1",
         CreateCatalog()
      );

      Assert.Equal("svt1-link", result);
   }

   [Fact]
   public void FindChannelLinkUrlPrefersActivitySpecificStreamLink()
   {
      var source = new ActivitySourceListItem(
         SourceKinds.StreamLink,
         "https://stream.example/svt1-event",
         "SVT1"
      );

      var result = SourceDisplay.FindChannelLinkUrlForChannel(
         [source],
         "SVT1",
         new BroadcastChannelLinkCatalog([])
      );

      Assert.Equal("https://stream.example/svt1-event", result);
   }

   [Fact]
   public void FindChannelLinkUrlPrefersViaplayEventLinkOverFallback()
   {
      var source = new ActivitySourceListItem(
         SourceKinds.StreamLink,
         "https://stream.example/viaplay-event",
         "Viaplay"
      );

      var result = SourceDisplay.FindChannelLinkUrlForChannel(
         [source],
         "Viaplay",
         new BroadcastChannelLinkCatalog([])
      );

      Assert.Equal("https://stream.example/viaplay-event", result);
   }

   [Fact]
   public void FindChannelLinkUrlReturnsNullForUnmappedChannel()
   {
      Assert.Null(
         SourceDisplay.FindChannelLinkUrlForChannel(
            [],
            "Unknown Channel",
            new BroadcastChannelLinkCatalog([])
         )
      );
   }

   [Theory]
   [InlineData("https://blocked.example/event", false)]
   [InlineData("https://subdomain.blocked.example/event", false)]
   [InlineData("https://blocked.example.other/event", true)]
   [InlineData("https://allowed.example/event", true)]
   public void IsPubliclyVisibleSourceUrlMatchesConfiguredHost(
      string sourceUrl,
      bool expected
   )
   {
      var result = SourceDisplay.IsPubliclyVisibleSourceUrl(
         sourceUrl,
         ["blocked.example"]
      );

      Assert.Equal(expected, result);
   }

   [Fact]
   public void OrderDistinctByUrlKeepsOneRowAndSortsByTranslatedKind()
   {
      var sources = new[]
      {
         new ActivitySourceListItem(
            SourceKinds.ParticipationEvidence,
            "https://example.test/shared"
         ),
         new ActivitySourceListItem(
            SourceKinds.ActivityEvidence,
            "https://example.test/shared"
         ),
         new ActivitySourceListItem(
            SourceKinds.ParticipantStarEvidence,
            "https://example.test/star"
         ),
         new ActivitySourceListItem(
            SourceKinds.ParticipantStartEvidence,
            "https://example.test/start"
         )
      };

      var ordered = SourceDisplay.OrderDistinctByUrl(sources);

      Assert.Equal(3, ordered.Count);
      Assert.Equal(
         ["Aktivitet", "Starttid", "Stjärna"],
         ordered.Select(source => SourceDisplay.FormatKind(source.Kind))
      );
      Assert.Equal(
         "https://example.test/shared",
         ordered[0].Url
      );
      Assert.Equal(
         SourceKinds.ActivityEvidence,
         ordered[0].Kind
      );
   }

   private static BroadcastChannelLinkCatalog CreateCatalog()
   {
      return new BroadcastChannelLinkCatalog(
         [new("SVT1", "svt1-link", [])]
      );
   }
}
