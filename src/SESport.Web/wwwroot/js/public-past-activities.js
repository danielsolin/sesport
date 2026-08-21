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
   const stateStorageKey =
      "sesport-public-past-activities-expanded";
   const collapsedLabel = toggle.dataset.collapsedLabel ??
      "Visa Tidigare";
   const expandedLabel = toggle.dataset.expandedLabel ??
      "Dölj Tidigare";

   const hasStoredExpandedState = () => {
      try {
         return window.sessionStorage.getItem(stateStorageKey) ===
            window.location.href;
      }
      catch
      {
         return false;
      }
   };

   const saveExpandedState = () => {
      try
      {
         if(agenda.classList.contains(hiddenClass))
         {
            window.sessionStorage.removeItem(stateStorageKey);
            return;
         }

         window.sessionStorage.setItem(
            stateStorageKey,
            window.location.href
         );
      }
      catch
      {
         // The toggle still works when session storage is unavailable.
      }
   };

   const updateToggle = () => {
      const isHidden = agenda.classList.contains(hiddenClass);
      toggle.setAttribute("aria-expanded", String(!isHidden));
      toggle.textContent = isHidden
         ? collapsedLabel
         : expandedLabel;
   };

   const scrollToActivityFromHash = () => {
      const targetId = window.location.hash.slice(1);
      if(targetId === "")
      {
         return;
      }

      const target = document.getElementById(targetId);
      if(!(target instanceof HTMLElement))
      {
         return;
      }

      const targetIsHidden = target.closest(
         ".activity-past-activity-hidden"
      ) !== null;
      if(targetIsHidden && agenda.classList.contains(hiddenClass))
      {
         agenda.classList.remove(hiddenClass);
         updateToggle();
         saveExpandedState();
      }

      const scrollToTarget = () => {
         target.scrollIntoView({
            behavior: "smooth",
            block: "center"
         });
      };
      window.requestAnimationFrame(() => {
         window.requestAnimationFrame(scrollToTarget);
      });
   };

   toggle.addEventListener("click", event => {
      event.preventDefault();

      agenda.classList.toggle(hiddenClass);
      updateToggle();
      saveExpandedState();

      const scrollToToggle = () => {
         toggle.scrollIntoView({
            behavior: "smooth",
            block: "center"
         });
      };

      window.requestAnimationFrame(() => {
         window.requestAnimationFrame(scrollToToggle);
      });
   });

   if(hasStoredExpandedState())
   {
      agenda.classList.remove(hiddenClass);
   }

   updateToggle();
   scrollToActivityFromHash();
})();
