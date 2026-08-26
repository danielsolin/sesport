using Microsoft.AspNetCore.Http;

namespace SESport.Core.Tests.Services;

public sealed class CachedImageResponseTests
{
   [Fact]
   public void CreateSetsPrivateCacheAndContentType()
   {
      var context = new DefaultHttpContext();
      var data = new byte[] { 1, 2, 3 };

      var result = CachedImageResponse.Create(
         context.Response,
         data,
         "image/jpeg",
         TimeSpan.FromHours(1)
      );

      Assert.Equal(
         "private, max-age=3600",
         context.Response.Headers.CacheControl.ToString()
      );
      Assert.Equal("image/jpeg", result.ContentType);
      Assert.Equal(data, result.FileContents);
      Assert.NotNull(result.EntityTag);
   }

   [Fact]
   public void CreateUsesStableAndContentBasedEntityTags()
   {
      var firstContext = new DefaultHttpContext();
      var secondContext = new DefaultHttpContext();
      var changedContext = new DefaultHttpContext();

      var first = CachedImageResponse.Create(
         firstContext.Response,
         new byte[] { 1, 2, 3 },
         "image/jpeg",
         TimeSpan.FromHours(1)
      );
      var same = CachedImageResponse.Create(
         secondContext.Response,
         new byte[] { 1, 2, 3 },
         "image/jpeg",
         TimeSpan.FromHours(1)
      );
      var changed = CachedImageResponse.Create(
         changedContext.Response,
         new byte[] { 1, 2, 4 },
         "image/jpeg",
         TimeSpan.FromHours(1)
      );

      Assert.Equal(first.EntityTag, same.EntityTag);
      Assert.NotEqual(first.EntityTag, changed.EntityTag);
   }
}
