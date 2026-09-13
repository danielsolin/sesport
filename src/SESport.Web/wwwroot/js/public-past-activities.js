(() => {
   "use strict";

   const toggle = document.querySelector(
      "[data-activity-past-toggle]"
   );
   const agenda = toggle instanceof HTMLButtonElement
      ? toggle.closest("[data-activity-agenda]")
      : null;
   const hiddenClass = "activity-past-activities-hidden";
   const stateStorageKey =
      "sesport-public-past-activities-expanded";
   const notificationTargetClass =
      "activity-entry-notification-target";
   const collapsedLabel = toggle instanceof HTMLButtonElement
      ? toggle.dataset.collapsedLabel ?? "Visa Tidigare"
      : "Visa Tidigare";
   const expandedLabel = toggle instanceof HTMLButtonElement
      ? toggle.dataset.expandedLabel ?? "Dölj Tidigare"
      : "Dölj Tidigare";

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
      if(!(agenda instanceof HTMLElement))
      {
         return;
      }

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
      if(!(toggle instanceof HTMLButtonElement) ||
         !(agenda instanceof HTMLElement))
      {
         return;
      }

      const isHidden = agenda.classList.contains(hiddenClass);
      toggle.setAttribute("aria-expanded", String(!isHidden));
      toggle.textContent = isHidden
         ? collapsedLabel
         : expandedLabel;
   };

   const scrollTargetToViewport = target => {
      const viewportHeight = window.visualViewport?.height ??
         window.innerHeight;
      const targetRect = target.getBoundingClientRect();
      const targetTop = window.scrollY + targetRect.top;
      const centeredOffset = Math.max(
         0,
         (viewportHeight - targetRect.height) / 2
      );
      const maximumScrollTop = Math.max(
         0,
         document.documentElement.scrollHeight - viewportHeight
      );
      const scrollTop = Math.min(
         maximumScrollTop,
         Math.max(0, targetTop - centeredOffset)
      );
      const prefersReducedMotion = window.matchMedia(
         "(prefers-reduced-motion: reduce)"
      ).matches;

      window.scrollTo({
         top: scrollTop,
         behavior: prefersReducedMotion ? "auto" : "smooth"
      });
   };

   const highlightActivityTarget = target => {
      target.classList.remove(notificationTargetClass);
      void target.offsetWidth;
      target.classList.add(notificationTargetClass);
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
      if(targetIsHidden && agenda instanceof HTMLElement &&
         agenda.classList.contains(hiddenClass))
      {
         agenda.classList.remove(hiddenClass);
         updateToggle();
         saveExpandedState();
      }

      const scrollToTarget = () => {
         scrollTargetToViewport(target);
         highlightActivityTarget(target);
      };
      window.requestAnimationFrame(() => {
         window.requestAnimationFrame(scrollToTarget);
      });
   };

   if(toggle instanceof HTMLButtonElement &&
      agenda instanceof HTMLElement)
   {
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
   }

   scrollToActivityFromHash();
})();
