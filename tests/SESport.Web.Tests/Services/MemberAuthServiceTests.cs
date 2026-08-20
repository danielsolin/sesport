using Microsoft.AspNetCore.Http;
using SESport.Core.Configuration;
using SESport.Core.Members;
using SESport.Core.Members.Interfaces;

namespace SESport.Core.Tests.Services;

public sealed class MemberAuthServiceTests
{
   [Theory]
   [InlineData("https://sesport.se")]
   [InlineData("https://dev.sesport.se")]
   [InlineData("http://localhost:5109")]
   public async Task RequestLoginLinkBuildsUrlFromRequestOrigin(
      string requestOrigin
   )
   {
      var repository = new FakeMemberRepository();
      var sender = new FakeMemberEmailSender();
      var service = new MemberAuthService(
         repository,
         sender,
         CreateOptions()
      );

      await service.RequestLoginLinkAsync(
         " Person@Example.COM ",
         "/activities?date=2199-12-01",
         CreateRequest(requestOrigin),
         CancellationToken.None
      );

      Assert.Equal("person@example.com", repository.NormalizedEmail);
      Assert.Equal("Person@Example.COM", repository.Email);
      Assert.Equal("person@example.com", sender.RecipientEmail);
      Assert.NotNull(sender.LoginLink);
      Assert.StartsWith(
         requestOrigin + "/Account/Verify?token=",
         sender.LoginLink,
         StringComparison.Ordinal
      );
      Assert.Contains(
         "returnUrl=%2Factivities%3Fdate%3D2199-12-01",
         sender.LoginLink,
         StringComparison.Ordinal
      );
   }

   [Fact]
   public async Task RateLimitedRequestDoesNotSendAnotherEmail()
   {
      var repository = new FakeMemberRepository
      {
         ShouldCreateLoginToken = false
      };
      var sender = new FakeMemberEmailSender();
      var service = new MemberAuthService(
         repository,
         sender,
         CreateOptions()
      );

      await service.RequestLoginLinkAsync(
         "person@example.com",
         null,
         CreateRequest("https://sesport.test"),
         CancellationToken.None
      );

      Assert.Null(sender.LoginLink);
   }

   private static MemberAuthOptions CreateOptions()
   {
      return new MemberAuthOptions
      {
         LoginTokenLifetime = TimeSpan.FromMinutes(15),
         LoginRequestCooldown = TimeSpan.FromMinutes(1),
         LoginRequestWindow = TimeSpan.FromHours(1),
         MaxLoginRequestsPerWindow = 5
      };
   }

   private static HttpRequest CreateRequest(string requestOrigin)
   {
      var uri = new Uri(requestOrigin);
      var context = new DefaultHttpContext();
      context.Request.Scheme = uri.Scheme;
      context.Request.Host = uri.IsDefaultPort
         ? new HostString(uri.Host)
         : new HostString(uri.Host, uri.Port);
      return context.Request;
   }

   private sealed class FakeMemberRepository : IMemberRepository
   {
      public bool ShouldCreateLoginToken { get; init; } = true;

      public string? Email { get; private set; }

      public string? NormalizedEmail { get; private set; }

      public Task<bool> TryCreateLoginTokenAsync(
         string email,
         string normalizedEmail,
         string tokenHash,
         DateTimeOffset requestedAt,
         DateTimeOffset expiresAt,
         DateTimeOffset cooldownThreshold,
         DateTimeOffset windowStart,
         int maxRequestsPerWindow,
         CancellationToken cancellationToken
      )
      {
         Email = email;
         NormalizedEmail = normalizedEmail;
         return Task.FromResult(ShouldCreateLoginToken);
      }

      public Task<Member?> ConsumeLoginTokenAsync(
         string tokenHash,
         DateTimeOffset consumedAt,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<Member?>(null);
      }

      public Task InvalidateLoginTokenAsync(
         string tokenHash,
         DateTimeOffset invalidatedAt,
         CancellationToken cancellationToken
      )
      {
         return Task.CompletedTask;
      }
   }

   private sealed class FakeMemberEmailSender : IMemberEmailSender
   {
      public string? RecipientEmail { get; private set; }

      public string? LoginLink { get; private set; }

      public Task SendLoginLinkAsync(
         string recipientEmail,
         string loginLink,
         TimeSpan tokenLifetime,
         CancellationToken cancellationToken
      )
      {
         RecipientEmail = recipientEmail;
         LoginLink = loginLink;
         return Task.CompletedTask;
      }
   }
}
