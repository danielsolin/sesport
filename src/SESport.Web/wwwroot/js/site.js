(() => {
   const currentPath = window.location.pathname.toLowerCase();
   const isRootPath = currentPath === "/";
   const isDesktopDevice = !isMobileDevice();

   if(isRootPath && !isDesktopDevice)
   {
      const autoReloadMarkerKey = "sesport-public-auto-reload";

      document.addEventListener("visibilitychange", () => {
         if(!document.hidden)
         {
            try
            {
               window.sessionStorage.setItem(
                  autoReloadMarkerKey,
                  "true"
               );
            }
            catch
            {
               // Reloading still works when session storage is unavailable.
            }

            window.location.reload();
         }
      });
   }

   const getFormSelector = "form[method='get']";

   const restoreGetForms = () => {
      document.querySelectorAll(getFormSelector).forEach(form => {
         if(form instanceof HTMLFormElement &&
            !form.hasAttribute("data-preserve-get-form-state"))
         {
            form.reset();
         }
      });
   };

   window.addEventListener("pageshow", restoreGetForms);
   restoreGetForms();

   function isMobileDevice()
   {
      const userAgentDataMobile =
         window.navigator.userAgentData?.mobile;

      if(typeof userAgentDataMobile === "boolean")
      {
         return userAgentDataMobile;
      }

      const userAgent = window.navigator.userAgent;
      return /android|iphone|ipad|ipod|mobile/i.test(userAgent) ||
         window.navigator.platform === "MacIntel" &&
         window.navigator.maxTouchPoints > 1;
   }
})();
