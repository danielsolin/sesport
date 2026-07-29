using System.Text;

using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Broadcast;

public class BroadcastXmlParserTests
{
   [Fact]
   public async Task ParseAsyncImportsSportProgramme()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <channel id="Eurosport1.se">
             <display-name>SE - Eurosport 1</display-name>
           </channel>
           <programme
             start="20260603180000 +0000"
             stop="20260603212500 +0000"
             channel="Eurosport1.se">
             <title lang="sv">Tennis Grand Slam Roland-Garros</title>
             <desc lang="sv">Kvartsfinal från Roland-Garros. (1/6-26).</desc>
            <category lang="sv">Tennis</category>
            <category lang="sv">Klubba och Bollspel</category>
            <category lang="sv">Sport</category>
            <category>Sports</category>
          </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.Equal("Eurosport1.se", broadcast.ChannelId);
      Assert.Equal("Eurosport 1", broadcast.ChannelName);
      Assert.Equal("Tennis Grand Slam Roland-Garros", broadcast.Title);
      Assert.Equal(
         "Kvartsfinal från Roland-Garros. (1/6-26).",
         broadcast.Description
      );
      Assert.Contains("Tennis", broadcast.Categories);
      Assert.DoesNotContain("Sport", broadcast.Categories);
      Assert.DoesNotContain("Sports", broadcast.Categories);
      Assert.DoesNotContain("Klubba och Bollspel", broadcast.Categories);
      Assert.False(broadcast.IsReplay);
      Assert.Null(broadcast.OriginalAirDate);
      Assert.Contains("<desc lang=\"sv\">", broadcast.RawProgrammeXml);
      Assert.Equal(
         DateTimeOffset.Parse("2026-06-03T18:00:00+00:00"),
         broadcast.StartsAt
      );
   }

   [Fact]
   public async Task ParseAsyncNormalizesMotorSportCategory()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="VSportMotor.se">
             <title lang="sv">Formel 1</title>
             <category lang="sv">Motor sport</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.Contains("Motorsport", broadcast.Categories);
      Assert.DoesNotContain("Motor sport", broadcast.Categories);
   }

   [Fact]
   public async Task ParseAsyncRemovesSwedishChannelPrefix()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <channel id="GINXeSportsTV.se">
             <display-name>SE - GINX eSports TV</display-name>
           </channel>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="GINXeSportsTV.se">
             <title lang="sv">Esport</title>
             <category lang="sv">E-sport</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.Equal("GINX eSports TV", broadcast.ChannelName);
   }

   [Fact]
   public async Task ParseAsyncRemovesSwedishChannelSuffix()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <channel id="AppleTV.se">
             <display-name>Apple TV (SE)</display-name>
           </channel>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="AppleTV.se">
             <title lang="sv">Film</title>
             <category lang="sv">Film</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.Equal("Apple TV", broadcast.ChannelName);
   }

   [Fact]
   public async Task ParseAsyncUnescapesUnicodeAmpersandsInChannelNames()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <channel id="Horse\u0026CountryTV.se">
             <display-name>SE - Horse \u0026 Country TV</display-name>
           </channel>
           <programme
             start="20260606113000 +0000"
             stop="20260606120000 +0000"
             channel="Horse\u0026CountryTV.se">
             <title lang="sv">The Slam Show</title>
             <category lang="sv">Ridsport</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.Equal("Horse & Country TV", broadcast.ChannelName);
   }

   [Fact]
   public async Task ParseAsyncDoesNotMarkDateRangeAsReplay()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="VSportGolf.se">
             <title lang="sv">Golf</title>
             <desc lang="sv">
               Från Golfclub Kitzbühel-Schwarzsee-Reith. (28-31/5-26).
             </desc>
             <category lang="sv">Golf</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.False(broadcast.IsReplay);
      Assert.Null(broadcast.OriginalAirDate);
   }

   [Fact]
   public async Task ParseAsyncDoesNotMarkDateInNewSentenceAsReplay()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603143000 +0000"
             stop="20260603163000 +0000"
             channel="ViaplaySport3.se">
             <title lang="sv">Fotboll: Landskamp</title>
             <desc lang="sv">
               Viaplay Sport 3/6 16:30. Norge - Sverige.
               Kommentatorer: Anders Bjuhr &amp; Fredrik Ljungberg.
               (1/6-26) Producerat år 2026.
             </desc>
             <category lang="sv">Fotboll</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.False(broadcast.IsReplay);
      Assert.Null(broadcast.OriginalAirDate);
   }

   [Fact]
   public async Task ParseAsyncDoesNotMarkDateInSentenceAsReplay()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603143000 +0000"
             stop="20260603163000 +0000"
             channel="ViaplaySport3.se">
             <title lang="sv">Fotboll: Landskamp</title>
             <desc lang="sv">
               Norge - Sverige (1/6-26) med svensk kommentering.
             </desc>
             <category lang="sv">Fotboll</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.False(broadcast.IsReplay);
      Assert.Null(broadcast.OriginalAirDate);
   }

   [Fact]
   public async Task ParseAsyncDoesNotMarkDateAtStartAsReplay()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603143000 +0000"
             stop="20260603163000 +0000"
             channel="ViaplaySport3.se">
             <title lang="sv">Fotboll: Landskamp</title>
             <desc lang="sv">(1/6-26). Norge - Sverige.</desc>
             <category lang="sv">Fotboll</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.False(broadcast.IsReplay);
      Assert.Null(broadcast.OriginalAirDate);
   }

   [Fact]
   public async Task ParseAsyncDoesNotMarkProducedYearAsReplay()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603143000 +0000"
             stop="20260603163000 +0000"
             channel="ViaplaySport3.se">
             <title lang="sv">Fotboll: Landskamp</title>
             <desc lang="sv">Norge - Sverige. Producerat år 2026.</desc>
             <category lang="sv">Fotboll</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.False(broadcast.IsReplay);
      Assert.Null(broadcast.OriginalAirDate);
   }

   [Fact]
   public async Task ParseAsyncSkipsNonSportProgrammes()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="SVT1.se">
             <title lang="sv">Nyheterna</title>
             <category lang="sv">Nyheter</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Empty(broadcasts);
   }

   [Fact]
   public async Task ParseAsyncSkipsProgrammesWithoutSpecificSportCategory()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="TV4Sportkanalen.se">
             <title lang="sv">Sändningsuppehåll</title>
             <category lang="sv">Sport</category>
             <category>Sports</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Empty(broadcasts);
   }

   [Fact]
   public async Task ParseAsyncSkipsSportMagazineProgrammes()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="Eurosport1.se">
             <title lang="sv">Tennis Magasin</title>
             <category lang="sv">Tennis</category>
             <category lang="sv">Sportmagasin</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Empty(broadcasts);
   }

   [Fact]
   public async Task ParseAsyncSkipsDocumentaryProgrammes()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="Eurosport1.se">
             <title lang="sv">Tennis Grand Slam Roland-Garros</title>
             <category lang="sv">Tennis</category>
             <category lang="sv">Dokumentär</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Empty(broadcasts);
   }

   [Fact]
   public async Task ParseAsyncSkipsProgrammesWithHighlightsInDescription()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="Eurosport1.se">
             <title lang="sv">Tennis Grand Slam Roland-Garros</title>
             <desc lang="sv">Höjdpunkter från dagens matcher.</desc>
             <category lang="sv">Tennis</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Empty(broadcasts);
   }

   [Fact]
   public async Task ParseAsyncSkipsEnglishHighlights()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme
             start="20260603180000 +0000"
             stop="20260603190000 +0000"
             channel="Eurosport1.se">
             <title lang="sv">Tennis Grand Slam Roland-Garros</title>
             <desc lang="sv">Match highlights from Roland-Garros.</desc>
             <category lang="sv">Tennis</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new BroadcastXmlParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Empty(broadcasts);
   }
}
