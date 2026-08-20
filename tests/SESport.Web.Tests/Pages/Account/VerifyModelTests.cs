using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using SESport.Core.Configuration;
using SESport.Core.Members;
using SESport.Core.Members.Interfaces;
using SESport.Web.Pages.Account;

namespace SESport.Core.Tests.Pages.Account;

public sealed class VerifyModelTests
{
   [Fact]
   public async Task OnGetAsyncRedirectsToWatchesAfterSuccessfulVerification()
   {
      var now = DateTimeOffset.UtcNow;
      var member = new Member(
         Guid.NewGuid(),
         "person@example.com",
         "person@example.com",
         now,
         now,
         now,
         now
      );
      var service = new MemberAuthService(
         new FakeMemberRepository(member),
         new FakeMemberEmailSender(),
         new MemberAuthOptions()
      );
      var services = new ServiceCollection();
      services.AddLogging();
      services.AddAuthentication(MemberAuthenticationDefaults.Scheme)
         .AddCookie(MemberAuthenticationDefaults.Scheme);
      using var serviceProvider = services.BuildServiceProvider();
      var model = new VerifyModel(
         service,
         NullLogger<VerifyModel>.Instance
      )
      {
         Token = "test-token",
         PageContext = new PageContext
         {
            HttpContext = new DefaultHttpContext
            {
               RequestServices = serviceProvider
            }
         }
      };

      var result = await model.OnGetAsync(CancellationToken.None);

      var redirect = Assert.IsType<LocalRedirectResult>(result);
      Assert.Equal("/bevakningar", redirect.Url);
   }

   private sealed class FakeMemberRepository(Member member)
      : IMemberRepository
   {
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
         return Task.FromResult(false);
      }

      public Task<Member?> ConsumeLoginTokenAsync(
         string tokenHash,
         DateTimeOffset consumedAt,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult<Member?>(member);
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
      public Task SendLoginLinkAsync(
         string recipientEmail,
         string loginLink,
         TimeSpan tokenLifetime,
         CancellationToken cancellationToken
      )
      {
         return Task.CompletedTask;
      }
   }
}
