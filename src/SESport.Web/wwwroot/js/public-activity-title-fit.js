(() => {
   "use strict";

   const titleSelector = "[data-activity-title-fit]";
   const slotSelector = "[data-activity-slot-fit]";
   const isElement = value => value instanceof HTMLElement;
   const slotRows = Array.from(
      document.querySelectorAll(slotSelector)
   ).filter(isElement);
   const titleElements = Array.from(
      document.querySelectorAll(titleSelector)
   ).filter(title =>
      isElement(title) && title.closest(slotSelector) === null
   );

   if(titleElements.length === 0 && slotRows.length === 0)
   {
      return;
   }

   const minimumScale = 0.8;
   const narrowMinimumScale = 0.4;
   const baseFontSizes = new Map();

   const readFontSize = element => {
      const fontSize = Number.parseFloat(
         window.getComputedStyle(element).fontSize
      );
      return Number.isFinite(fontSize) ? fontSize : null;
   };

   const rememberFontSize = element => {
      const fontSize = readFontSize(element);

      if(fontSize !== null)
      {
         baseFontSizes.set(element, fontSize);
      }
   };

   titleElements.forEach(rememberFontSize);

   const slotElements = new Map();
   slotRows.forEach(row => {
      const elements = [
         row.querySelector("[data-activity-slot-time]"),
         row.querySelector(titleSelector)
      ].filter(isElement);

      elements.forEach(rememberFontSize);
      if(elements.length > 0)
      {
         slotElements.set(row, elements);
      }
   });

   const fits = element => element.scrollWidth <= element.clientWidth + 1;

   const getTextLineRects = element => {
      const range = document.createRange();
      range.selectNodeContents(element);
      return Array.from(range.getClientRects()).filter(rect =>
         rect.width > 0 && rect.height > 0
      );
   };

   const fitsAroundSportIcon = title => {
      const activityEntry = title.closest(".activity-entry");
      if(activityEntry === null)
      {
         return true;
      }

      const obstacles = Array.from(
         activityEntry.querySelectorAll(
            ".activity-entry-sport-icon, " +
               ".activity-entry-sport-country-tag"
         )
      ).filter(isElement);
      if(obstacles.length === 0)
      {
         return true;
      }

      const textLines = getTextLineRects(title);
      return obstacles.every(obstacle => {
         const obstacleRect = obstacle.getBoundingClientRect();
         return textLines.every(textLine =>
            textLine.bottom <= obstacleRect.top ||
            textLine.top >= obstacleRect.bottom ||
            textLine.right <= obstacleRect.left
         );
      });
   };

   const fitsTitle = title =>
      fits(title) && fitsAroundSportIcon(title);

   const setScale = (elements, scale) => {
      elements.forEach(element => {
         const baseFontSize = baseFontSizes.get(element);
         if(baseFontSize !== undefined)
         {
            element.style.fontSize = `${baseFontSize * scale}px`;
         }
      });
   };

   const fitElements = (
      elements,
      fitsAtCurrentSize,
      minimumScaleForElements,
      fallbackMinimumScale = null
   ) => {
      if(elements.some(element =>
            !baseFontSizes.has(element) || element.clientWidth === 0
         ))
      {
         return;
      }

      setScale(elements, 1);
      if(fitsAtCurrentSize())
      {
         return;
      }

      setScale(elements, minimumScaleForElements);
      if(!fitsAtCurrentSize())
      {
         if(fallbackMinimumScale === null)
         {
            return;
         }

         setScale(elements, fallbackMinimumScale);
         if(!fitsAtCurrentSize())
         {
            return;
         }

         minimumScaleForElements = fallbackMinimumScale;
      }

      let low = minimumScaleForElements;
      let high = 1;

      for(let attempt = 0; attempt < 12; attempt++)
      {
         const candidate = (low + high) / 2;
         setScale(elements, candidate);

         if(fitsAtCurrentSize())
         {
            low = candidate;
         }
         else
         {
            high = candidate;
         }
      }

      setScale(elements, low);
   };

   const fitTitle = title => {
      fitElements(
         [title],
         () => fitsTitle(title),
         minimumScale,
         narrowMinimumScale
      );
   };

   const fitSlot = elements => {
      fitElements(
         elements,
         () => elements.every(fits),
         minimumScale
      );
   };

   const fitAll = () => {
      slotElements.forEach(fitSlot);
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
         fitAll();
      });
   };

   if(typeof ResizeObserver === "function")
   {
      const observer = new ResizeObserver(scheduleFit);
      titleElements.forEach(title => observer.observe(title));
      slotRows.forEach(row => observer.observe(row));
   }

   window.addEventListener("resize", scheduleFit, { passive: true });
   fitAll();
   scheduleFit();

   if(document.fonts?.ready)
   {
      document.fonts.ready.then(scheduleFit);
   }
})();
