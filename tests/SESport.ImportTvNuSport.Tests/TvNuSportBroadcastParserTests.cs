using System.Globalization;

using SESport.Tools.ImportTvNuSport;

namespace SESport.ImportTvNuSport.Tests;

public class TvNuSportBroadcastParserTests
{
   [Fact]
   public async Task ParseAsyncImportsTvNuSportBroadcasts()
   {
      var fixturePath = Path.GetFullPath(
         Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "2026-06-07.html"
         )
      );

      await using var stream = File.OpenRead(fixturePath);

      var parser = new TvNuSportBroadcastParser();
      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Equal(73, broadcasts.Count);

      var formula2Broadcast = Assert.Single(
         broadcasts,
         broadcast =>
            broadcast.Title == "Formel 2, Monaco GP - Race" &&
            broadcast.ChannelName == "Viaplay Sport"
      );

      Assert.Equal("tv-nu-sport", formula2Broadcast.SourceKey);
      Assert.Equal("Europe/Stockholm", formula2Broadcast.TimeZoneId);
      Assert.Contains("Motorsport", formula2Broadcast.Categories);
      Assert.Equal(
         DateTimeOffset.FromUnixTimeMilliseconds(1780816800000),
         formula2Broadcast.StartsAt
      );
      Assert.Equal(
         DateTimeOffset.FromUnixTimeMilliseconds(1780821600000),
         formula2Broadcast.EndsAt
      );

      var bommaritoBroadcast = Assert.Single(
         broadcasts,
         broadcast =>
            broadcast.Title ==
               "IndyCar Series, Bommarito Automotive Group 500 - Race" &&
            broadcast.ChannelName == "V Sport Motor"
      );

      Assert.Contains("Motorsport", bommaritoBroadcast.Categories);
      Assert.Equal(
         DateTimeOffset.Parse(
            "2026-06-08T01:00:00+00:00",
            null,
            DateTimeStyles.RoundtripKind
         ),
         bommaritoBroadcast.StartsAt
      );

      Assert.Contains(
         broadcasts,
         broadcast =>
            broadcast.Title ==
               "IndyCar Series, Bommarito Automotive Group 500 - Race" &&
            broadcast.ChannelName == "Viaplay"
      );

      var streamBroadcast = Assert.Single(
         broadcasts,
         broadcast =>
            broadcast.Title == "Formel 2, Monaco GP - Race" &&
            broadcast.ChannelName == "Viaplay"
      );

      Assert.Equal("stream", streamBroadcast.ExternalId.Split(':')[1]);
      Assert.Equal("Viaplay", streamBroadcast.ChannelName);
   }
}
