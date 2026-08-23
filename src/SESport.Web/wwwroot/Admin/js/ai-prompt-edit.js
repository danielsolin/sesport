(() => {
   const jobSelect = document.querySelector("#Prompt_JobId");
   const field = document.querySelector(
      "[data-codex-reasoning-field]"
   );
   const reasoningSelect = document.querySelector(
      "[data-codex-reasoning-select]"
   );

   if(!(jobSelect instanceof HTMLSelectElement)
      || !(field instanceof HTMLElement)
      || !(reasoningSelect instanceof HTMLSelectElement))
   {
      return;
   }

   const codexProviderKinds = (field.dataset.codexProviderKinds ?? "")
      .split(",")
      .map(kind => kind.trim())
      .filter(Boolean);
   const defaultEffort = field.dataset.defaultEffort ?? "";

   const syncReasoningField = () => {
      const selectedOption = jobSelect.selectedOptions[0];
      const isCodex = codexProviderKinds.includes(
         selectedOption?.dataset.providerKind ?? ""
      );

      field.hidden = !isCodex;
      reasoningSelect.disabled = !isCodex;

      if(isCodex && !reasoningSelect.value)
      {
         reasoningSelect.value = defaultEffort;
      }
   };

   jobSelect.addEventListener("change", syncReasoningField);
   syncReasoningField();
})();
