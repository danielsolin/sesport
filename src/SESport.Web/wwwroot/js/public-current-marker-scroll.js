(() => {
   "use strict";

   const marker = document.querySelector(".activity-now-marker");

   if(!(marker instanceof HTMLElement))
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
            + marker.getBoundingClientRect().top
            - topMargin;

         window.scrollTo({
            top: Math.max(0, top),
            behavior: reduceMotion ? "auto" : "smooth"
         });
      });
   });
})();
