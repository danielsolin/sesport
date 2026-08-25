(() => {
   "use strict";

   const titleElements = Array.from(
      document.querySelectorAll("[data-activity-title-fit]")
   ).filter(title => title instanceof HTMLElement);

   if(titleElements.length === 0)
   {
      return;
   }

   const baseFontSizes = new Map();
   const minimumScale = 0.68;

   titleElements.forEach(title => {
      const fontSize = Number.parseFloat(
         window.getComputedStyle(title).fontSize
      );

      if(Number.isFinite(fontSize))
      {
         baseFontSizes.set(title, fontSize);
      }
   });

   const fits = title => title.scrollWidth <= title.clientWidth + 1;

   const fitTitle = title => {
      const baseFontSize = baseFontSizes.get(title);

      if(baseFontSize === undefined || title.clientWidth === 0)
      {
         return;
      }

      title.style.fontSize = `${baseFontSize}px`;

      if(fits(title))
      {
         return;
      }

      const minimumFontSize = baseFontSize * minimumScale;
      title.style.fontSize = `${minimumFontSize}px`;

      if(!fits(title))
      {
         return;
      }

      let low = minimumFontSize;
      let high = baseFontSize;

      for(let attempt = 0; attempt < 12; attempt++)
      {
         const candidate = (low + high) / 2;
         title.style.fontSize = `${candidate}px`;

         if(fits(title))
         {
            high = candidate;
         }
         else
         {
            low = candidate;
         }
      }

      title.style.fontSize = `${high}px`;
   };

   const fitTitles = () => {
      titleElements.forEach(fitTitle);
   };

   let frameId = 0;
   const scheduleFit = () => {
      if(frameId !== 0)
      {
         return;
      }

      frameId = window.requestAnimationFrame(() => {
         frameId = 0;
         fitTitles();
      });
   };

   if(typeof ResizeObserver === "function")
   {
      const observer = new ResizeObserver(scheduleFit);
      titleElements.forEach(title => observer.observe(title));
   }

   window.addEventListener("resize", scheduleFit, { passive: true });
   scheduleFit();

   if(document.fonts?.ready)
   {
      document.fonts.ready.then(scheduleFit);
   }
})();
