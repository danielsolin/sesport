(() => {
   const menus = document.querySelectorAll(
      "[data-public-header-menu]"
   );
   if(menus.length === 0)
   {
      return;
   }

   const portraitQuery = window.matchMedia(
      "(orientation: portrait)"
   );

   const syncMenuState = () => {
      menus.forEach(menu => {
         if(portraitQuery.matches)
         {
            menu.removeAttribute("open");
         }
         else
         {
            menu.setAttribute("open", "");
         }
      });
   };

   const closeMenusWhenClickedOutside = event => {
      const target = event.target;
      if(!(target instanceof Node))
      {
         return;
      }

      menus.forEach(menu => {
         if(menu.open && !menu.contains(target))
         {
            menu.removeAttribute("open");
         }
      });
   };

   syncMenuState();
   document.addEventListener(
      "pointerdown",
      closeMenusWhenClickedOutside
   );
   if(typeof portraitQuery.addEventListener === "function")
   {
      portraitQuery.addEventListener("change", syncMenuState);
   }
   else
   {
      portraitQuery.addListener(syncMenuState);
   }
})();
