(() => {
   const currentPath = window.location.pathname.toLowerCase();
   const isRootPath = currentPath === "/";

   if(isRootPath)
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

})();
