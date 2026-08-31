(() => {
   "use strict";

   const dropdown = document.querySelector("[data-date-dropdown]");
   if(!(dropdown instanceof HTMLElement))
   {
      return;
   }

   const toggle = dropdown.querySelector("[data-date-dropdown-toggle]");
   const menu = dropdown.querySelector("[data-date-dropdown-menu]");
   const options = Array.from(
      dropdown.querySelectorAll(".date-dropdown-option")
   );
   const fitTargets = Array.from(
      dropdown.querySelectorAll("[data-date-option-fit]")
   ).filter(target => target instanceof HTMLElement);
   const chevron = dropdown.querySelector(".date-dropdown-chevron");

   if(!(toggle instanceof HTMLButtonElement) ||
      !(menu instanceof HTMLElement))
   {
      return;
   }

   const minimumScale = 0.5;
   const baseFontSizes = new Map();
   fitTargets.forEach(target => {
      const fontSize = Number.parseFloat(
         window.getComputedStyle(target).fontSize
      );
      if(Number.isFinite(fontSize))
      {
         baseFontSizes.set(target, fontSize);
      }
   });

   const measuredElements = target => [
      target.querySelector(".date-option-label"),
      ...target.querySelectorAll(
         ".date-option-day, .date-option-date"
      ),
      target.querySelector(".date-option-participant-count")
   ].filter(element => element instanceof HTMLElement);

   const fits = target => {
      const label = target.querySelector(".date-option-label");
      if(label instanceof HTMLElement &&
         (label.clientWidth === 0 ||
          label.scrollWidth > label.clientWidth))
      {
         return false;
      }

      const count = target.querySelector(
         ".date-option-participant-count"
      );
      if(count instanceof HTMLElement &&
         (count.clientWidth === 0 ||
          count.scrollWidth > count.clientWidth))
      {
         return false;
      }

      if(label instanceof HTMLElement)
      {
         return true;
      }

      const elements = measuredElements(target);
      if(elements.length === 0)
      {
         return target.clientWidth > 0 &&
            target.scrollWidth <= target.clientWidth + 1;
      }

      const targetRect = target.getBoundingClientRect();
      const chevronRect = chevron instanceof HTMLElement
         ? chevron.getBoundingClientRect()
         : null;
      const availableRight = chevronRect === null
         ? targetRect.right
         : Math.min(targetRect.right, chevronRect.left - 4);
      return elements.every(element => {
         const rect = element.getBoundingClientRect();
         return rect.left >= targetRect.left - 1 &&
            rect.right <= availableRight;
      });
   };

   const fitDateText = target => {
      const baseFontSize = baseFontSizes.get(target);
      if(baseFontSize === undefined || target.clientWidth === 0)
      {
         return;
      }

      target.style.fontSize = `${baseFontSize}px`;
      const fitsAtBaseSize = fits(target);
      if(fitsAtBaseSize)
      {
         return;
      }

      target.style.fontSize = `${baseFontSize * minimumScale}px`;
      if(!fits(target))
      {
         return;
      }

      let low = minimumScale;
      let high = 1;

      for(let attempt = 0; attempt < 12; attempt++)
      {
         const candidate = (low + high) / 2;
         target.style.fontSize = `${baseFontSize * candidate}px`;

         if(fits(target))
         {
            low = candidate;
         }
         else
         {
            high = candidate;
         }
      }

      target.style.fontSize = `${baseFontSize * low}px`;
   };

   const fitAllDateText = () => {
      fitTargets.forEach(fitDateText);
   };

   let fitFrame = 0;
   const scheduleFit = () => {
      if(fitFrame !== 0)
      {
         return;
      }

      fitFrame = window.requestAnimationFrame(() => {
         fitFrame = 0;
         fitAllDateText();
      });
   };

   const close = (restoreFocus = false) => {
      menu.hidden = true;
      toggle.setAttribute("aria-expanded", "false");
      dropdown.classList.remove("is-open");

      if(restoreFocus)
      {
         toggle.focus();
      }
   };

   const open = () => {
      menu.hidden = false;
      toggle.setAttribute("aria-expanded", "true");
      dropdown.classList.add("is-open");
      scheduleFit();

      const selected = options.find(option =>
         option.getAttribute("aria-selected") === "true"
      );
      (selected ?? options[0])?.focus();
   };

   toggle.addEventListener("click", () => {
      if(menu.hidden)
      {
         open();
      }
      else
      {
         close();
      }
   });

   toggle.addEventListener("keydown", event => {
      if(event.key === "ArrowDown" || event.key === "ArrowUp")
      {
         event.preventDefault();
         open();
      }
   });

   menu.addEventListener("keydown", event => {
      const currentIndex = options.indexOf(document.activeElement);

      if(event.key === "Escape")
      {
         event.preventDefault();
         close(true);
         return;
      }

      if(event.key !== "ArrowDown" && event.key !== "ArrowUp")
      {
         return;
      }

      event.preventDefault();
      const direction = event.key === "ArrowDown" ? 1 : -1;
      const nextIndex = (
         currentIndex + direction + options.length
      ) % options.length;
      options[nextIndex]?.focus();
   });

   document.addEventListener("click", event => {
      if(event.target instanceof Node && !dropdown.contains(event.target))
      {
         close();
      }
   });

   if(fitTargets.length > 0)
   {
      if(typeof ResizeObserver === "function")
      {
         const observer = new ResizeObserver(scheduleFit);
         fitTargets.forEach(target => observer.observe(target));
      }

      window.addEventListener("resize", scheduleFit, { passive: true });
      fitAllDateText();
      scheduleFit();

      if(document.fonts?.ready)
      {
         document.fonts.ready.then(scheduleFit);
      }
   }
})();
