(() => {
   const pickerSelector = "[data-activity-group-picker]";
   const inputSelector = "[data-activity-group-input]";
   const idSelector = "[data-activity-group-id]";
   const titleSelector = "[data-activity-group-title]";
   const creationSelector =
      "[data-activity-group-creation-required]";
   const suggestionsSelector = "[data-activity-group-suggestions]";
   const sportSelector = "[name='Activity.SportId']";
   const optionSelector = ".broadcast-org-entity-option";

   const initializePickers = () => {
      document.querySelectorAll(pickerSelector).forEach(initializePicker);
   };

   if(document.readyState === "loading")
   {
      document.addEventListener("DOMContentLoaded", initializePickers);
   }
   else
   {
      initializePickers();
   }

   function initializePicker(picker)
   {
      if(!(picker instanceof HTMLElement)
         || picker.dataset.activityGroupInitialized === "true")
      {
         return;
      }

      const input = picker.querySelector(inputSelector);
      const hiddenId = picker.querySelector(idSelector);
      const hiddenTitle = picker.querySelector(titleSelector);
      const creationRequired = picker.querySelector(creationSelector);
      const suggestions = picker.querySelector(suggestionsSelector);
      const searchUrl = (
         picker.dataset.activityGroupSearchUrl ?? ""
      ).trim();

      if(!(input instanceof HTMLInputElement)
         || !(hiddenId instanceof HTMLInputElement)
         || !(hiddenTitle instanceof HTMLInputElement)
         || !(creationRequired instanceof HTMLInputElement)
         || !(suggestions instanceof HTMLElement)
         || searchUrl === "")
      {
         return;
      }

      picker.dataset.activityGroupInitialized = "true";
      const state = {
         timerId: null,
         requestId: 0,
         selectedIndex: -1,
         items: []
      };
      const original = {
         id: hiddenId.value.trim(),
         title: hiddenTitle.value.trim(),
         creationRequired: creationRequired.value === "true"
      };

      const closeSuggestions = () => {
         if(state.timerId !== null)
         {
            window.clearTimeout(state.timerId);
            state.timerId = null;
         }

         state.requestId += 1;
         state.selectedIndex = -1;
         state.items = [];
         suggestions.hidden = true;
         suggestions.replaceChildren();
      };
      const setCreationRequired = value => {
         creationRequired.value = value ? "true" : "false";
      };
      const restoreOriginal = () => {
         input.value = original.title;
         hiddenId.value = original.id;
         hiddenTitle.value = original.title;
         setCreationRequired(original.creationRequired);
         closeSuggestions();
      };
      const setActiveSuggestion = index => {
         const options = Array.from(
            suggestions.querySelectorAll(optionSelector)
         );

         if(options.length === 0)
         {
            return;
         }

         state.selectedIndex = Math.max(
            0,
            Math.min(index, options.length - 1)
         );
         options.forEach((option, optionIndex) => {
            option.classList.toggle(
               "is-active",
               optionIndex === state.selectedIndex
            );
         });
      };
      const renderSuggestions = html => {
         window.replaceContentsWithPartialHtml(suggestions, html);
         state.items = Array.from(
            suggestions.querySelectorAll(optionSelector)
         ).map(option => ({
            kind: option.dataset.suggestionKind === "create"
               ? "create"
               : "existing",
            id: (option.dataset.suggestionId ?? "").trim(),
            text: (option.dataset.suggestionText ?? "").trim()
         }));
         state.selectedIndex = -1;
         suggestions.hidden = false;
      };
      const selectItem = item => {
         if(!item || item.text === "")
         {
            return;
         }

         input.value = item.text;
         hiddenTitle.value = item.text;

         if(item.kind === "create")
         {
            hiddenId.value = "";
            setCreationRequired(true);
         }
         else
         {
            hiddenId.value = item.id;
            setCreationRequired(false);
         }

         closeSuggestions();
      };
      const search = async () => {
         const term = input.value.trim();
         const requestId = ++state.requestId;
         const url = new URL(searchUrl, window.location.origin);
         url.searchParams.set("term", term);
         url.searchParams.set(
            "sportId",
            picker.dataset.activityGroupSportId ?? ""
         );
         url.searchParams.set("format", "activity-suggestions");

         try
         {
            const html = await window.loadPartialAsync(url);

            if(requestId === state.requestId)
            {
               renderSuggestions(html);
            }
         }
         catch
         {
            if(requestId === state.requestId)
            {
               closeSuggestions();
            }
         }
      };
      const scheduleSearch = () => {
         if(state.timerId !== null)
         {
            window.clearTimeout(state.timerId);
         }

         state.timerId = window.setTimeout(() => {
            state.timerId = null;
            void search();
         }, 180);
      };

      suggestions.addEventListener("click", event => {
         const option = event.target instanceof Element
            ? event.target.closest(optionSelector)
            : null;
         const index = option instanceof HTMLElement
            ? Array.from(suggestions.querySelectorAll(optionSelector))
               .indexOf(option)
            : -1;

         if(index >= 0)
         {
            event.preventDefault();
            selectItem(state.items[index]);
         }
      });
      suggestions.addEventListener("mousedown", event => {
         const option = event.target instanceof Element
            ? event.target.closest(optionSelector)
            : null;

         if(option instanceof HTMLElement && document.activeElement === input)
         {
            event.preventDefault();
         }
      });
      suggestions.addEventListener("mouseover", event => {
         const option = event.target instanceof Element
            ? event.target.closest(optionSelector)
            : null;
         const index = option instanceof HTMLElement
            ? Array.from(suggestions.querySelectorAll(optionSelector))
               .indexOf(option)
            : -1;

         if(index >= 0)
         {
            setActiveSuggestion(index);
         }
      });

      input.addEventListener("input", () => {
         hiddenId.value = "";
         hiddenTitle.value = input.value.trim();
         setCreationRequired(false);
         scheduleSearch();
      });
      input.addEventListener("focus", scheduleSearch);
      input.addEventListener("keydown", event => {
         if(suggestions.hidden)
         {
            return;
         }

         if(event.key === "ArrowDown")
         {
            event.preventDefault();
            setActiveSuggestion(state.selectedIndex + 1);
         }
         else if(event.key === "ArrowUp")
         {
            event.preventDefault();
            setActiveSuggestion(state.selectedIndex - 1);
         }
         else if(event.key === "Escape")
         {
            event.preventDefault();
            restoreOriginal();
         }
         else if(event.key === "Enter")
         {
            event.preventDefault();
            selectItem(state.items[Math.max(state.selectedIndex, 0)]);
         }
      });
      input.addEventListener("blur", () => {
         window.setTimeout(() => {
            if(picker.contains(document.activeElement))
            {
               return;
            }

            if(creationRequired.value !== "true"
               && hiddenId.value.trim() === "")
            {
               restoreOriginal();
            }
            else
            {
               closeSuggestions();
            }
         }, 120);
      });

      const sportInput = document.querySelector(sportSelector);
      sportInput?.addEventListener("change", () => {
         if(sportInput instanceof HTMLSelectElement)
         {
            picker.dataset.activityGroupSportId = sportInput.value;
            scheduleSearch();
         }
      });
   }
})();
