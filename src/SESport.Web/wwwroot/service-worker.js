const defaultIcon = "/icon-192.png";

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

self.addEventListener("push", event => {
   event.waitUntil(
      readPayload(event).then(payload => {
         const notification = payload &&
            typeof payload === "object"
            ? payload
            : {};
         return self.registration.showNotification(
            notification.title || "sesport",
            {
               body: notification.body || "",
               icon: notification.icon || defaultIcon,
               badge: notification.badge || defaultIcon,
               tag: notification.tag || "sesport",
               data: {
                  url: notification.url || "/"
               }
            }
         );
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
