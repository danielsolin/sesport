using System.Text;

using SESport.Core.TvSport;

namespace SESport.Core.Tests.TvSport;

public class XmltvSportBroadcastParserTests
{
   [Fact]
   public async Task ParseAsyncImportsSportProgrammesWithCategoriesAndDescription()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <channel id="Eurosport1.se">
             <display-name>SE - Eurosport 1</display-name>
           </channel>
           <programme start="20260603180000 +0000" stop="20260603212500 +0000" channel="Eurosport1.se">
             <title lang="sv">Tennis Grand Slam Roland-Garros</title>
             <desc lang="sv">Kvartsfinal från Roland-Garros. (1/6-26).</desc>
             <category lang="sv">Tennis</category>
             <category lang="sv">Klubba och Bollspel</category>
             <category lang="sv">Sport</category>
             <category>Sports</category>
           </programme>
         </tv>
         """;

      var parser = new XmltvSportBroadcastParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.Equal("Eurosport1.se", broadcast.ChannelId);
      Assert.Equal("SE - Eurosport 1", broadcast.ChannelName);
      Assert.Equal("Tennis Grand Slam Roland-Garros", broadcast.Title);
      Assert.Equal("Kvartsfinal från Roland-Garros. (1/6-26).", broadcast.Description);
      Assert.Contains("Tennis", broadcast.Categories);
      Assert.DoesNotContain("Sport", broadcast.Categories);
      Assert.DoesNotContain("Sports", broadcast.Categories);
      Assert.DoesNotContain("Klubba och Bollspel", broadcast.Categories);
      Assert.True(broadcast.IsReplay);
      Assert.Equal(new DateOnly(2026, 6, 1), broadcast.OriginalAirDate);
      Assert.Contains("<desc lang=\"sv\">", broadcast.RawProgrammeXml);
      Assert.Equal(DateTimeOffset.Parse("2026-06-03T18:00:00+00:00"), broadcast.StartsAt);
   }

   [Fact]
   public async Task ParseAsyncNormalizesMotorSportCategory()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme start="20260603180000 +0000" stop="20260603190000 +0000" channel="VSportMotor.se">
             <title lang="sv">Formel 1</title>
             <category lang="sv">Motor sport</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new XmltvSportBroadcastParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.Contains("Motorsport", broadcast.Categories);
      Assert.DoesNotContain("Motor sport", broadcast.Categories);
   }

   [Fact]
   public async Task ParseAsyncParsesOriginalAirDateWithDateRange()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme start="20260603180000 +0000" stop="20260603190000 +0000" channel="VSportGolf.se">
             <title lang="sv">Golf</title>
             <desc lang="sv">Från Golfclub Kitzbühel-Schwarzsee-Reith. (28-31/5-26).</desc>
             <category lang="sv">Golf</category>
             <category lang="sv">Sport</category>
           </programme>
         </tv>
         """;

      var parser = new XmltvSportBroadcastParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);
      var broadcast = Assert.Single(broadcasts);

      Assert.True(broadcast.IsReplay);
      Assert.Equal(new DateOnly(2026, 5, 31), broadcast.OriginalAirDate);
   }

   [Fact]
   public async Task ParseAsyncSkipsNonSportProgrammes()
   {
      const string xml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <tv>
           <programme start="20260603180000 +0000" stop="20260603190000 +0000" channel="SVT1.se">
             <title lang="sv">Nyheterna</title>
             <category lang="sv">Nyheter</category>
           </programme>
         </tv>
         """;

      var parser = new XmltvSportBroadcastParser();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

      var broadcasts = await parser.ParseAsync(stream, CancellationToken.None);

      Assert.Empty(broadcasts);
   }
}
