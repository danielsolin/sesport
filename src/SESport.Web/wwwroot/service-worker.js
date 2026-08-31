const defaultIcon = "/icon-192.png";
const subscriptionChangeMessageType =
   "sesport-push-subscription-change";

const readPayload = event => {
   if(!event.data)
   {
      return Promise.resolve({});
   }

   try
   {
      return Promise.resolve(event.data.json());
   }
   catch
   {
      return Promise.resolve({});
   }
};

const parseTimestamp = value => {
   if(typeof value !== "string")
   {
      return null;
   }

   const timestamp = Date.parse(value);
   return Number.isFinite(timestamp) ? timestamp : null;
};

self.addEventListener("push", event => {
   event.waitUntil(
      readPayload(event).then(payload => {
         const notification = payload &&
            typeof payload === "object"
            ? payload
            : {};

         const expiresAt = parseTimestamp(notification.expiresAt);
         if(expiresAt === null || expiresAt <= Date.now())
         {
            return;
         }

         const options = {
            body: notification.body || "",
            icon: notification.icon || defaultIcon,
            badge: notification.badge || defaultIcon,
            tag: notification.tag || "sesport",
            data: {
               url: notification.url || "/"
            }
         };
         const sentAt = parseTimestamp(notification.sentAt);
         if(sentAt !== null)
         {
            options.timestamp = sentAt;
         }

         return self.registration.showNotification(
            notification.title || "sesport",
            options
         );
      })
   );
});

self.addEventListener("pushsubscriptionchange", event => {
   event.waitUntil(
      self.clients.matchAll({
         type: "window",
         includeUncontrolled: true
      }).then(clients => {
         const subscription = event.newSubscription;
         const serializedSubscription = subscription === null ||
            subscription === undefined
            ? null
            : subscription.toJSON();

         for(const client of clients)
         {
            client.postMessage({
               type: subscriptionChangeMessageType,
               subscription: serializedSubscription
            });
         }
      })
   );
});

self.addEventListener("notificationclick", event => {
   event.notification.close();
   const targetUrl = new URL(
      event.notification.data?.url || "/",
      self.location.origin
   ).href;

   event.waitUntil(
      self.clients.matchAll({
         type: "window",
         includeUncontrolled: true
      }).then(async clients => {
         for(const client of clients)
         {
            if("navigate" in client)
            {
               await client.navigate(targetUrl);
            }
            if("focus" in client)
            {
               return client.focus();
            }
         }

         return self.clients.openWindow(targetUrl);
      })
   );
});
