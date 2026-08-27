using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;
using System.Net;
using System.Text.Json;

namespace SESport.Web.Services;

public sealed class MemberPushNotificationSender(
   PushServiceClient pushServiceClient,
   MemberPushRepository pushRepository,
   MemberPushOptions options,
   ILogger<MemberPushNotificationSender> logger
)
{
   private const string ActivityAnchorPrefix = "activity-";

   private static readonly JsonSerializerOptions JsonOptions = new()
   {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
   };

   public async Task<MemberPushDeliveryResult> SendAsync(
      MemberActivityPushNotification notification,
      DateTimeOffset now,
      CancellationToken cancellationToken
   )
   {
      ValidateOptions();
      var authentication = new VapidAuthentication(
         options.PublicKey,
         options.PrivateKey
      )
      {
         Subject = options.Subject
      };
      var payload = CreatePayloadWithPersonLimit(
         notification,
         options.MaxVisiblePersonNames
      );
      var timeToLive = GetTimeToLiveSeconds(notification, now);
      var successfulDeliveries = 0;
      var permanentFailures = 0;
      var transientFailures = 0;

      using(authentication)
      {
         foreach(var subscription in notification.Subscriptions)
         {
            var pushSubscription = new PushSubscription
            {
               Endpoint = subscription.Endpoint
            };
            pushSubscription.SetKey(
               PushEncryptionKeyName.P256DH,
               subscription.P256dh
            );
            pushSubscription.SetKey(
               PushEncryptionKeyName.Auth,
               subscription.Auth
            );

            var message = new PushMessage(payload)
            {
               TimeToLive = timeToLive,
               Urgency = PushMessageUrgency.Normal
            };

            try
            {
               await pushServiceClient.RequestPushMessageDeliveryAsync(
                  pushSubscription,
                  message,
                  authentication,
                  cancellationToken
               );
               successfulDeliveries++;
            }
            catch(PushServiceClientException exception)
               when(exception.StatusCode is
                  HttpStatusCode.BadRequest or
                  HttpStatusCode.Gone or
                  HttpStatusCode.NotFound)
            {
               permanentFailures++;
               await pushRepository.DeleteSubscriptionAsync(
                  notification.MemberId,
                  subscription.Id,
                  CancellationToken.None
               );
               logger.LogInformation(
                  "Removed expired push subscription {SubscriptionId}.",
                  subscription.Id
               );
            }
            catch(Exception exception)
               when(!cancellationToken.IsCancellationRequested)
            {
               transientFailures++;
               logger.LogWarning(
                  exception,
                  "Could not send push notification for activity {ActivityId}",
                  notification.ActivityId
               );
            }
         }
      }

      return new MemberPushDeliveryResult(
         successfulDeliveries,
         permanentFailures,
         transientFailures
      );
   }

   private static string CreatePayload(
      MemberActivityPushNotification notification
   )
   {
      return CreatePayloadWithPersonLimit(
         notification,
         MemberPushOptions.DefaultMaxVisiblePersonNames
      );
   }

   private static string CreatePayloadWithPersonLimit(
      MemberActivityPushNotification notification,
      int maxVisiblePersonNames
   )
   {
      var displayDate = ActivityDisplayDateResolver.Resolve(
         notification.StartsAt,
         notification.PublicDateMode
      );
      var leadTime = FormatLeadTime(notification.LeadTimeMinutes);
      var activityAnchor = ActivityAnchorPrefix +
         notification.ActivityId.ToString("N");
      var personNames = FormatPersonNames(
         notification.PersonNames,
         maxVisiblePersonNames
      );
      var body = "Om " + leadTime + ": " +
         personNames +
         " deltar i " + notification.ActivityTitle + ".";
      var channelNames = FormatChannelNames(notification.TvChannelName);
      if(channelNames.Length > 0)
      {
         body += " Visas på " + channelNames + ".";
      }

      return JsonSerializer.Serialize(
         new
         {
            title = "sesport",
            body,
            url = "/?date=" + DateDisplay.Format(displayDate) +
               "#" + activityAnchor,
            icon = "/icon-192.png",
            badge = "/icon-192.png",
            tag = activityAnchor
         },
         JsonOptions
      );
   }

   private static string FormatPersonNames(
      string personNames,
      int maxVisiblePersonNames
   )
   {
      var names = personNames.Split(
         ',',
         StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries
      );
      var visibleNames = names.Take(
         Math.Max(1, maxVisiblePersonNames)
      );
      var formattedNames = string.Join(", ", visibleNames);

      return names.Length > maxVisiblePersonNames
         ? formattedNames + " med flera"
         : formattedNames;
   }

   private static string FormatLeadTime(int minutes)
   {
      return minutes == MemberNotificationLeadTimes.OneHourMinutes
         ? "en timme"
         : $"{minutes} minuter";
   }

   private static string FormatChannelNames(string? tvChannelName)
   {
      return string.Join(
         ", ",
         (tvChannelName ?? string.Empty)
            .Split(
               ',',
               StringSplitOptions.TrimEntries |
                  StringSplitOptions.RemoveEmptyEntries
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
      );
   }

   private static int GetTimeToLiveSeconds(
      MemberActivityPushNotification notification,
      DateTimeOffset now
   )
   {
      var seconds = (int)Math.Ceiling(
         (notification.StartsAt - now).TotalSeconds
      );
      return Math.Clamp(seconds, 60, 3600);
   }

   private void ValidateOptions()
   {
      if(!options.IsConfigured ||
         !Uri.TryCreate(
            options.Subject,
            UriKind.Absolute,
            out var subject
         ) ||
         subject.Scheme is not ("http" or "https" or "mailto"))
      {
         throw new InvalidOperationException(
            "MemberPush requires a valid Subject, PublicKey, " +
            "and PrivateKey."
         );
      }
   }
}

public sealed record MemberPushDeliveryResult(
   int SuccessfulDeliveries,
   int PermanentFailures,
   int TransientFailures
)
{
   public bool HasDelivery => SuccessfulDeliveries > 0;

   public bool HasTransientFailure => TransientFailures > 0;
}
