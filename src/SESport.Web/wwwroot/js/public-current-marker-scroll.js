(() => {
   "use strict";

   const autoReloadMarkerKey = "sesport-public-auto-reload";
   let loadedByAutoReload = false;

   try
   {
      loadedByAutoReload =
         window.sessionStorage.getItem(autoReloadMarkerKey) === "true";
      if(loadedByAutoReload)
      {
         window.sessionStorage.removeItem(autoReloadMarkerKey);
      }
   }
   catch
   {
      // Scroll on the initial load when session storage is unavailable.
   }

   if(loadedByAutoReload)
   {
      return;
   }

   const ongoingActivity = document.querySelector(
      ".activity-agenda-section.activity-is-ongoing"
   );

   if(!(ongoingActivity instanceof HTMLElement))
   {
      return;
   }

   const topMargin = 12;
   const reduceMotion = window.matchMedia(
      "(prefers-reduced-motion: reduce)"
   ).matches;

   window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => {
         const top = window.scrollY
            + ongoingActivity.getBoundingClientRect().top
            - topMargin;

         window.scrollTo({
            top: Math.max(0, top),
            behavior: reduceMotion ? "auto" : "smooth"
         });
      });
   });
})();
