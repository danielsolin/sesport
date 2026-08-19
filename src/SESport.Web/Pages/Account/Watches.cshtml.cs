using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

using SESport.Core.Members;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Pages.Account;

[Authorize(AuthenticationSchemes = MemberAuthenticationDefaults.Scheme)]
public sealed class WatchesModel(
   MemberWatchRepository watchRepository,
   MemberPushRepository pushRepository,
   MemberPushOptions pushOptions
) : PageModel
{
   private const int MaxSearchResults = 5;

   private static readonly JsonSerializerOptions JsonOptions = new()
   {
      PropertyNameCaseInsensitive = true
   };

   public IReadOnlyList<MemberPersonListItem> WatchedEntities {
      get;
      private set;
   } = [];

   public int NotificationLeadTimeMinutes {
      get;
      private set;
   } = MemberNotificationLeadTimes.Normalize(
      null,
      MemberNotificationLeadTimes.TenMinutes
   );

   public bool PushNotificationsConfigured => pushOptions.IsConfigured;

   public string PushPublicKey => pushOptions.PublicKey;

   public IReadOnlyList<MemberNotificationLeadTimeOption>
      NotificationLeadTimeOptions { get; } =
      MemberNotificationLeadTimes.SupportedMinutes
         .Select(minutes => new MemberNotificationLeadTimeOption(
            minutes,
            FormatLeadTime(minutes)
         ))
         .ToArray();

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      var memberId = GetMemberId();
      WatchedEntities = await watchRepository.GetWatchedEntitiesAsync(
         memberId,
         cancellationToken
      );
      NotificationLeadTimeMinutes =
         MemberNotificationLeadTimes.Normalize(
            await pushRepository.GetNotificationLeadTimeMinutesAsync(
               memberId,
               cancellationToken
            ),
            pushOptions.DefaultNotificationLeadTimeMinutes
         );
   }

   public async Task<IActionResult> OnGetSearchAsync(
      string? q,
      CancellationToken cancellationToken
   )
   {
      var query = NormalizeQuery(q);
      var results = query is null
         ? Array.Empty<MemberPersonListItem>()
         : await watchRepository.SearchPeopleAsync(
            query,
            GetMemberId(),
            MaxSearchResults,
            cancellationToken
         );

      return Partial("_WatchSearchResults", results);
   }

   public async Task<IActionResult> OnPostAddAsync(
      Guid entityId,
      string? pushSubscription,
      CancellationToken cancellationToken
   )
   {
      if(!pushOptions.IsConfigured)
      {
         return BadRequest(
            "Pushnotiser är inte tillgängliga just nu."
         );
      }

      var memberId = GetMemberId();
      if(!string.IsNullOrWhiteSpace(pushSubscription))
      {
         if(!TryParsePushSubscription(
               pushSubscription,
               out var parsedSubscription
            ))
         {
            return BadRequest("Ogiltig push-prenumeration.");
         }

         await pushRepository.UpsertSubscriptionAsync(
            memberId,
            parsedSubscription,
            cancellationToken
         );
      }
      else if(!await pushRepository.HasActiveSubscriptionAsync(
            memberId,
            cancellationToken
         ))
      {
         return BadRequest(
            "Aktivera notiser på minst en enhet innan du lägger till " +
            "en bevakning."
         );
      }

      await watchRepository.TryAddEntityWatchAsync(
         memberId,
         entityId,
         cancellationToken
      );

      return RedirectToPage();
   }

   public async Task<IActionResult> OnPostRegisterPushAsync(
      string? pushSubscription,
      CancellationToken cancellationToken
   )
   {
      if(!pushOptions.IsConfigured ||
         !TryParsePushSubscription(
            pushSubscription,
            out var parsedSubscription
         ))
      {
         return BadRequest(
            "A valid push subscription is required."
         );
      }

      await pushRepository.UpsertSubscriptionAsync(
         GetMemberId(),
         parsedSubscription,
         cancellationToken
      );

      return new NoContentResult();
   }

   public async Task<IActionResult> OnPostSetNotificationLeadTimeAsync(
      int notificationLeadTimeMinutes,
      CancellationToken cancellationToken
   )
   {
      if(!MemberNotificationLeadTimes.IsSupported(
            notificationLeadTimeMinutes
         ))
      {
         return BadRequest("The notification lead time is not supported.");
      }

      await pushRepository.SetNotificationLeadTimeMinutesAsync(
         GetMemberId(),
         notificationLeadTimeMinutes,
         cancellationToken
      );
      return RedirectToPage();
   }

   public async Task<IActionResult> OnPostRemoveAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      await watchRepository.RemoveEntityWatchAsync(
         GetMemberId(),
         entityId,
         cancellationToken
      );

      return RedirectToPage();
   }

   private Guid GetMemberId()
   {
      var memberIdValue = User.FindFirstValue(
         MemberClaimTypes.MemberId
      );
      return Guid.TryParse(memberIdValue, out var memberId)
         ? memberId
         : throw new InvalidOperationException(
            "The member authentication claim is missing."
         );
   }

   private static string? NormalizeQuery(string? query)
   {
      var normalizedQuery = query?.Trim();
      return string.IsNullOrWhiteSpace(normalizedQuery)
         ? null
         : normalizedQuery;
   }

   private static bool TryParsePushSubscription(
      string? json,
      out MemberPushSubscriptionInput subscription
   )
   {
      subscription = null!;
      if(string.IsNullOrWhiteSpace(json))
      {
         return false;
      }

      PushSubscriptionRequest? request;
      try
      {
         request = JsonSerializer.Deserialize<
            PushSubscriptionRequest
         >(json, JsonOptions);
      }
      catch(JsonException)
      {
         return false;
      }

      if(request is null ||
         string.IsNullOrWhiteSpace(request.Endpoint) ||
         request.Keys is null ||
         !IsHttpsEndpoint(request.Endpoint) ||
         !IsBase64Url(request.Keys.P256dh) ||
         !IsBase64Url(request.Keys.Auth))
      {
         return false;
      }

      DateTimeOffset? expirationAt = null;
      if(request.ExpirationTime is not null)
      {
         try
         {
            expirationAt = DateTimeOffset.FromUnixTimeMilliseconds(
               request.ExpirationTime.Value
            );
         }
         catch(ArgumentOutOfRangeException)
         {
            return false;
         }
      }

      subscription = new MemberPushSubscriptionInput(
         request.Endpoint,
         request.Keys.P256dh!,
         request.Keys.Auth!,
         expirationAt
      );
      return true;
   }

   private static bool IsHttpsEndpoint(string endpoint)
   {
      return endpoint.Length <= 4096 &&
         Uri.TryCreate(
            endpoint,
            UriKind.Absolute,
            out var uri
         ) &&
         uri.Scheme == Uri.UriSchemeHttps;
   }

   private static bool IsBase64Url(string? value)
   {
      return !string.IsNullOrWhiteSpace(value) &&
         value.Length <= 512 &&
         value.All(character =>
            char.IsLetterOrDigit(character) ||
            character is '-' or '_'
         );
   }

   private static string FormatLeadTime(int minutes)
   {
      return minutes == MemberNotificationLeadTimes.OneHourMinutes
         ? "Skicka notis 1 timme före"
         : $"Skicka notis {minutes} minuter före";
   }

   private sealed record PushSubscriptionRequest(
      string? Endpoint,
      long? ExpirationTime,
      PushSubscriptionKeys? Keys
   );

   private sealed record PushSubscriptionKeys(
      string? P256dh,
      string? Auth
   );
}

public sealed record MemberNotificationLeadTimeOption(
   int Minutes,
   string Label
);
