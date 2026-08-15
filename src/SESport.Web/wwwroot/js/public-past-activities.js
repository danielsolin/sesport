(() => {
   "use strict";

   const toggle = document.querySelector(
      "[data-activity-past-toggle]"
   );
   if(!(toggle instanceof HTMLButtonElement))
   {
      return;
   }

   const agenda = toggle.closest("[data-activity-agenda]");
   if(!(agenda instanceof HTMLElement))
   {
      return;
   }

   const hiddenClass = "activity-past-activities-hidden";
   const collapsedLabel = toggle.dataset.collapsedLabel ??
      "Visa Tidigare";
   const expandedLabel = toggle.dataset.expandedLabel ??
      "Dölj Tidigare";

   const updateToggle = () => {
      const isHidden = agenda.classList.contains(hiddenClass);
      toggle.setAttribute("aria-expanded", String(!isHidden));
      toggle.textContent = isHidden
         ? collapsedLabel
         : expandedLabel;
   };

   toggle.addEventListener("click", event => {
      event.preventDefault();
      const scrollX = window.scrollX;
      const scrollY = window.scrollY;

      agenda.classList.toggle(hiddenClass);
      updateToggle();

      const restoreScrollPosition = () => {
         window.scrollTo({
            left: scrollX,
            top: scrollY,
            behavior: "auto"
         });
      };

      restoreScrollPosition();
      window.requestAnimationFrame(() => {
         window.requestAnimationFrame(restoreScrollPosition);
      });
   });

   updateToggle();
})();
