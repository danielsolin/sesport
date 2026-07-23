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

   if(!(toggle instanceof HTMLButtonElement) ||
      !(menu instanceof HTMLElement))
   {
      return;
   }

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
})();
