(() => {
   const control = document.querySelector(
      "[data-entity-image-source-control]"
   );

   if(!control) {
      return;
   }

   const input = control.querySelector(
      "[data-entity-image-source-input]"
   );
   const replaceButton = control.querySelector(
      "[data-entity-image-replace-button]"
   );

   if(!input || !replaceButton) {
      return;
   }

   const unlock = () => {
      input.readOnly = false;
      input.removeAttribute("readonly");
      input.classList.add("is-unlocked");
      replaceButton.hidden = false;
      input.focus();
      input.select();
   };

   input.addEventListener("dblclick", unlock);
   input.addEventListener("input", () => {
      if(!input.readOnly) {
         replaceButton.hidden = false;
      }
   });
})();
