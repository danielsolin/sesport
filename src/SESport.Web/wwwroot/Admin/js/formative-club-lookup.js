(() => {
   const pickerSelector = "[data-formative-club-picker]";
   const inputSelector = "[data-formative-club-input]";
   const hiddenSelector = "[data-formative-club-id]";
   const clearSelector = "[data-formative-club-clear]";
   const suggestionsSelector = "[data-formative-club-suggestions]";
   const optionSelector = ".broadcast-org-entity-option";
   const debounceMs = 180;

   document.addEventListener("DOMContentLoaded", () => {
      initializePickers();
   });

   function initializePickers()
   {
      document.querySelectorAll(pickerSelector).forEach(initializePicker);
   }

   function initializePicker(picker)
   {
      if(!(picker instanceof HTMLElement)
         || picker.dataset.formativeClubInitialized === "true")
      {
         return;
      }

      const input = picker.querySelector(inputSelector);
      const hidden = picker.querySelector(hiddenSelector);
      const clear = picker.querySelector(clearSelector);
      const suggestions = picker.querySelector(suggestionsSelector);
      const searchUrl = (
         picker.dataset.formativeClubSearchUrl ?? ""
      ).trim();

      if(!(input instanceof HTMLInputElement)
         || !(hidden instanceof HTMLInputElement)
         || !(clear instanceof HTMLButtonElement)
         || !(suggestions instanceof HTMLElement)
         || searchUrl === "")
      {
         return;
      }

      picker.dataset.formativeClubInitialized = "true";

      const state = {
         timerId: null,
         requestId: 0
      };

      input.addEventListener("input", () => {
         hidden.value = "";
         clear.hidden = true;
         scheduleSearch(state, input, suggestions, searchUrl);
      });

      input.addEventListener("focus", () => {
         if(input.value.trim() !== "")
         {
            scheduleSearch(state, input, suggestions, searchUrl);
         }
      });

      input.addEventListener("keydown", event => {
         if(event.key === "Escape")
         {
            event.preventDefault();
            closeSuggestions(state, suggestions);
         }
      });

      suggestions.addEventListener("click", event => {
         const option = event.target instanceof Element
            ? event.target.closest(optionSelector)
            : null;

         if(!(option instanceof HTMLElement))
         {
            return;
         }

         event.preventDefault();
         hidden.value = (option.dataset.entityId ?? "").trim();
         input.value = (
            option.dataset.entityText ?? option.textContent ?? ""
         ).trim();
         clear.hidden = hidden.value === "";
         closeSuggestions(state, suggestions);
      });

      clear.addEventListener("click", () => {
         hidden.value = "";
         input.value = "";
         clear.hidden = true;
         closeSuggestions(state, suggestions);
         input.focus();
      });

      input.addEventListener("blur", () => {
         window.setTimeout(() => {
            if(!picker.contains(document.activeElement))
            {
               closeSuggestions(state, suggestions);
            }
         }, 120);
      });
   }

   function scheduleSearch(state, input, suggestions, searchUrl)
   {
      if(state.timerId !== null)
      {
         window.clearTimeout(state.timerId);
      }

      state.timerId = window.setTimeout(() => {
         state.timerId = null;
         void search(state, input, suggestions, searchUrl);
      }, debounceMs);
   }

   async function search(state, input, suggestions, searchUrl)
   {
      const query = input.value.trim();

      if(query === "")
      {
         closeSuggestions(state, suggestions);
         return;
      }

      const requestId = ++state.requestId;
      const url = new URL(searchUrl, window.location.origin);
      url.searchParams.set("term", query);
      url.searchParams.set("format", "formative-club-suggestions");
      url.searchParams.set("entityTypeIds", "Club");
      url.searchParams.set("includeRelatedEntityNames", "false");
      url.searchParams.set("maxResults", "20");

      try
      {
         const response = await fetch(url, {
            headers: {
               Accept: "text/html"
            }
         });
         const html = await response.text();

         if(requestId !== state.requestId)
         {
            return;
         }

         if(!response.ok)
         {
            throw new Error("Formative club search failed.");
         }

         window.replaceContentsWithPartialHtml(suggestions, html);
         suggestions.hidden = false;
      }
      catch
      {
         if(requestId === state.requestId)
         {
            closeSuggestions(state, suggestions);
         }
      }
   }

   function closeSuggestions(state, suggestions)
   {
      if(state.timerId !== null)
      {
         window.clearTimeout(state.timerId);
         state.timerId = null;
      }

      state.requestId += 1;
      suggestions.hidden = true;
      suggestions.replaceChildren();
   }
})();
