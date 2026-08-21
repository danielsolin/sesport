using SESport.Core.Configuration;
using SESport.Data.Models;
using SESport.Web.Pages;

namespace SESport.Core.Tests.Pages;

public sealed class StatisticsModelTests
{
   [Fact]
   public void BuildMonthOptionsStartsAtConfiguredMonth()
   {
      var options = StatisticsModel.BuildMonthOptions(
         new DateOnly(2026, 6, 18),
         new DateOnly(2026, 8, 21),
         new DateOnly(2026, 8, 1)
      );

      Assert.Equal(
         ["2026-06", "2026-07", "2026-08"],
         options.Select(option => option.Value)
      );
      Assert.Equal(
         ["Juni 2026", "Juli 2026", "Augusti 2026"],
         options.Select(option => option.Label)
      );
      Assert.Equal(
         "2026-08",
         Assert.Single(options, option => option.IsSelected).Value
      );
   }

   [Theory]
   [InlineData("2026-07", "2026-07-01")]
   [InlineData("not-a-month", "2026-08-01")]
   [InlineData("2026-05", "2026-08-01")]
   [InlineData("2026-09", "2026-08-01")]
   public void NormalizeSelectedMonthKeepsOnlyAvailableMonths(
      string requestedMonth,
      string expectedMonth
   )
   {
      var result = StatisticsModel.NormalizeSelectedMonth(
         requestedMonth,
         new DateOnly(2026, 6, 1),
         new DateOnly(2026, 8, 21)
      );

      Assert.Equal(DateOnly.Parse(expectedMonth), result);
   }

   [Fact]
   public void ParseMonthRequiresYearAndMonthFormat()
   {
      Assert.Equal(
         new DateOnly(2026, 6, 1),
         StatisticsModel.ParseMonth("2026-06")
      );
      Assert.Null(StatisticsModel.ParseMonth("2026-6"));
      Assert.Null(StatisticsModel.ParseMonth("2026-06-01"));
   }

   [Fact]
   public void NormalizeSportFilterKeepsOnlyMonthlySportOptions()
   {
      var options = new PublicStatisticsSportOption[]
      {
         new("golf", "Golf", 2),
         new("tennis", "Tennis", 1)
      };

      Assert.Equal(
         "golf",
         StatisticsModel.NormalizeSportFilter(" GOLF ", options)
      );
      Assert.Null(
         StatisticsModel.NormalizeSportFilter("ski", options)
      );
      Assert.Null(
         StatisticsModel.NormalizeSportFilter(null, options)
      );
   }

   [Fact]
   public void PublicStatisticsOptionsUseJuneAsTheDefaultStart()
   {
      var options = new PublicStatisticsOptions();

      Assert.Equal(
         new DateOnly(2026, 6, 1),
         options.FirstAvailableMonth
      );
      Assert.Equal(10, options.TopParticipantLimit);
   }
}
