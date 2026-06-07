using System.Globalization;

namespace SESport.Core.Tests.Domain;

public sealed class SportDayTests
{
   [Fact]
   public void GetLocalDateUsesStockholmTime()
   {
      var instant = DateTimeOffset.Parse(
         "2026-06-05T22:30:00+00:00",
         CultureInfo.InvariantCulture
      );

      var localDate = SportDay.GetLocalDate(instant);

      Assert.Equal(new DateOnly(2026, 6, 6), localDate);
   }

   [Fact]
   public void GetSportDateUsesCutoff()
   {
      var instant = DateTimeOffset.Parse(
         "2026-06-05T22:30:00+00:00",
         CultureInfo.InvariantCulture
      );

      var sportDate = SportDay.GetSportDate(instant);

      Assert.Equal(new DateOnly(2026, 6, 5), sportDate);
   }

   [Fact]
   public void TodayAndTomorrowWindowsOverlapAtCutoff()
   {
      var instant = DateTimeOffset.Parse(
         "2026-06-05T22:30:00+00:00",
         CultureInfo.InvariantCulture
      );

      var today = SportDay.Today(instant);
      var tomorrow = SportDay.Tomorrow(instant);

      Assert.Equal(new DateOnly(2026, 6, 5), today.StartDate);
      Assert.Equal(new DateOnly(2026, 6, 6), today.EndDateExclusive);
      Assert.Equal(new DateOnly(2026, 6, 6), tomorrow.StartDate);
      Assert.Equal(new DateOnly(2026, 6, 7), tomorrow.EndDateExclusive);
      Assert.Equal(new TimeOnly(4, 0), today.Cutoff);
      Assert.Equal(today.Cutoff, tomorrow.Cutoff);
   }

   [Fact]
   public void ForDateCreatesExpectedWindow()
   {
      var window = SportDay.ForDate(new DateOnly(2026, 6, 7));

      Assert.Equal(new DateOnly(2026, 6, 7), window.StartDate);
      Assert.Equal(new DateOnly(2026, 6, 8), window.EndDateExclusive);
      Assert.Equal(new TimeOnly(4, 0), window.Cutoff);
   }
}
