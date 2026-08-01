(() => {
   const pickerSelector = "[data-activity-group-picker]";
   const inputSelector = "[data-activity-group-input]";
   const idSelector = "[data-activity-group-id]";
   const titleSelector = "[data-activity-group-title]";
   const creationSelector =
      "[data-activity-group-creation-required]";
   const suggestionsSelector = "[data-activity-group-suggestions]";
   const sportSelector = "[name='Activity.SportId']";

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

      const selectItem = item => {
         if(!item || typeof item.text !== "string")
         {
            return;
         }

         const text = item.text.trim();
         if(text === "")
         {
            return;
         }

         input.value = text;
         hiddenTitle.value = text;

         if(item.kind === "create")
         {
            hiddenId.value = "";
            setCreationRequired(true);
         }
         else
         {
            hiddenId.value = item.id.trim();
            setCreationRequired(false);
         }

         closeSuggestions();
      };

      const setActiveSuggestion = index => {
         const options = Array.from(
            suggestions.querySelectorAll(
               ".broadcast-org-entity-option"
            )
         );

         if(options.length === 0)
         {
            return;
         }

         const nextIndex = Math.max(
            0,
            Math.min(index, options.length - 1)
         );
         state.selectedIndex = nextIndex;
         options.forEach((option, optionIndex) => {
            option.classList.toggle(
               "is-active",
               optionIndex === nextIndex
            );
         });
      };

      const renderSuggestions = items => {
         suggestions.replaceChildren();
         state.items = items;
         state.selectedIndex = -1;

         if(items.length === 0)
         {
            const empty = document.createElement("div");
            empty.className = "broadcast-org-entity-empty";
            empty.textContent = "No matches";
            suggestions.append(empty);
            suggestions.hidden = false;
            return;
         }

         items.forEach((item, index) => {
            const option = document.createElement("button");
            option.type = "button";
            option.className = "broadcast-org-entity-option";
            option.textContent = item.kind === "create"
               ? `Create new group: ${item.text}`
               : item.text;

            option.addEventListener("mousedown", event => {
               if(document.activeElement === input)
               {
                  event.preventDefault();
               }
            });
            option.addEventListener("mouseenter", () => {
               setActiveSuggestion(index);
            });
            option.addEventListener("click", event => {
               event.preventDefault();
               selectItem(item);
            });
            suggestions.append(option);
         });

         suggestions.hidden = false;
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

         try
         {
            const response = await fetch(url, {
               headers: { Accept: "application/json" }
            });
            const payload = await response.json();

            if(requestId !== state.requestId)
            {
               return;
            }

            if(!response.ok)
            {
               throw new Error("Group search failed.");
            }

            const items = Array.isArray(payload.results)
               ? payload.results
                  .map(normalizeResult)
                  .filter(item => item !== null)
               : [];

            if(term !== "")
            {
               items.push({ kind: "create", id: "", text: term });
            }
            renderSuggestions(items);
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
            const item = state.items[Math.max(state.selectedIndex, 0)];
            selectItem(item);
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
         picker.dataset.activityGroupSportId = sportInput.value;
         scheduleSearch();
      });
   }

   function normalizeResult(item)
   {
      if(!(item && typeof item === "object"))
      {
         return null;
      }

      const id = typeof item.id === "string" ? item.id.trim() : "";
      const text = typeof item.text === "string" ? item.text.trim() : "";

      return id === "" || text === ""
         ? null
         : { kind: "existing", id, text };
   }
})();
