(() => {
   "use strict";

   const promotion = document.querySelector(
      "[data-public-install-promotion]"
   );
   const message = promotion?.querySelector(
      "[data-public-install-message]"
   );
   const installButton = promotion?.querySelector(
      "[data-public-install-button]"
   );
   const dismissButton = promotion?.querySelector(
      "[data-public-install-dismiss]"
   );

   if(!(promotion instanceof HTMLElement) ||
      !(message instanceof HTMLElement) ||
      !(installButton instanceof HTMLButtonElement) ||
      !(dismissButton instanceof HTMLButtonElement))
   {
      return;
   }

   const dismissalKey = "sesport-install-promotion-dismissed";
   const isStandalone =
      window.matchMedia("(display-mode: standalone)").matches ||
      window.navigator.standalone === true;
   const isMobile =
      window.navigator.userAgentData?.mobile === true ||
      /android|iphone|ipad|ipod/i.test(
         window.navigator.userAgent
      );
   const isIos =
      /iphone|ipad|ipod/i.test(window.navigator.userAgent) ||
      (
         window.navigator.platform === "MacIntel" &&
         window.navigator.maxTouchPoints > 1
      );
   let installPrompt = null;

   if("serviceWorker" in navigator)
   {
      navigator.serviceWorker.register(
         "/service-worker.js",
         { scope: "/" }
      ).catch(() => null);
   }

   const wasDismissed = () => {
      try
      {
         return window.sessionStorage.getItem(dismissalKey) === "true";
      }
      catch
      {
         return false;
      }
   };

   const hidePromotion = () => {
      promotion.hidden = true;
   };

   const showPromotion = (text, canPrompt) => {
      if(isStandalone || wasDismissed())
      {
         return;
      }

      message.textContent = text;
      installButton.hidden = !canPrompt;
      promotion.hidden = false;
   };

   dismissButton.addEventListener("click", () => {
      try
      {
         window.sessionStorage.setItem(dismissalKey, "true");
      }
      catch
      {
         // The promotion can still be dismissed for this page view.
      }

      hidePromotion();
   });

   installButton.addEventListener("click", async () => {
      if(installPrompt === null)
      {
         return;
      }

      installButton.disabled = true;
      installPrompt.prompt();
      await installPrompt.userChoice;
      installPrompt = null;
      hidePromotion();
   });

   window.addEventListener("beforeinstallprompt", event => {
      event.preventDefault();

      if(!isMobile)
      {
         return;
      }

      installPrompt = event;
      showPromotion(
         "Installera sesport för snabb åtkomst från hemskärmen.",
         true
      );
   });

   window.addEventListener("appinstalled", () => {
      installPrompt = null;
      hidePromotion();
   });

   if(isIos && !isStandalone)
   {
      showPromotion(
         "Installera sesport: öppna sidan i Safari, tryck på Dela och " +
         "välj Lägg till på hemskärmen.",
         false
      );
   }
})();
