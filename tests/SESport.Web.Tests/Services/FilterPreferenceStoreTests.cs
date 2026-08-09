using Microsoft.AspNetCore.Http;

namespace SESport.Core.Tests.Services;

public sealed class FilterPreferenceStoreTests
{
   [Fact]
   public void ResolveListUsesCookieWhenQueryKeyIsAbsent()
   {
      var context = CreateContext("filters=football|tennis");
      var store = new FilterPreferenceStore();

      var values = store.ResolveList(
         context,
         "selected",
         [],
         "filters"
      );

      Assert.Equal(["football", "tennis"], values);
   }

   [Fact]
   public void ResolveListUsesExplicitQueryValuesIncludingEmptySelection()
   {
      var context = CreateContext("filters=football");
      context.Request.QueryString = new QueryString("?selected=");
      var store = new FilterPreferenceStore();

      var values = store.ResolveList(
         context,
         "selected",
         [string.Empty],
         "filters"
      );

      Assert.Equal([string.Empty], values);
   }

   [Theory]
   [InlineData("?showHidden=false", false)]
   [InlineData("?showHidden=true&showHidden=false", true)]
   public void ResolveBooleanUsesExplicitQueryValues(
      string queryString,
      bool expected
   )
   {
      var context = CreateContext("show-hidden=true");
      context.Request.QueryString = new QueryString(queryString);
      var store = new FilterPreferenceStore();

      var value = store.ResolveBoolean(
         context,
         "showHidden",
         false,
         "show-hidden"
      );

      Assert.Equal(expected, value);
   }

   private static DefaultHttpContext CreateContext(string cookieHeader)
   {
      var context = new DefaultHttpContext();
      context.Request.Headers["Cookie"] = cookieHeader;
      return context;
   }
}
