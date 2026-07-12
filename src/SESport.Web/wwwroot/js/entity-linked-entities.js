(() => {
   const pickerSelector = "[data-entity-linked-entities-picker]";
   const inputSelector = "[data-entity-linked-entities-input]";
   const suggestionsSelector =
      "[data-entity-linked-entities-suggestions]";
   const selectedSelector = "[data-entity-linked-entities-selected]";
   const chipSelector = "[data-entity-linked-entities-chip]";
   const chipRemoveSelector =
      ".entity-linked-entities-chip-remove," +
      " [data-entity-linked-entities-chip-remove]";
   const hiddenInputSelector =
      "[data-entity-linked-entities-hidden-id]";
   const debounceMs = 180;

   window.initializeEntityLinkedEntitiesPicker =
      initializeEntityLinkedEntitiesPicker;

   document.addEventListener("DOMContentLoaded", () => {
      initializeEntityLinkedEntitiesPicker();
   });

   function initializeEntityLinkedEntitiesPicker(root = document)
   {
      root.querySelectorAll(pickerSelector).forEach(picker => {
         initializePicker(picker);
      });
   }

   function initializePicker(picker)
   {
      if(!(picker instanceof HTMLElement)
         || picker.dataset.entityLinkedEntitiesInitialized === "true")
      {
         return;
      }

      const input = picker.querySelector(inputSelector);
      const suggestions = picker.querySelector(suggestionsSelector);
      const selected = picker.querySelector(selectedSelector);
      const searchUrl = (picker.dataset.entityLinkedEntitiesSearchUrl ?? "")
         .trim();
      const excludeEntityId = (picker.dataset.entityId ?? "").trim();
      const organizationOnly = (
         picker.dataset.organizationOnly ?? "true"
      ).trim().toLowerCase() !== "false";
      const maxResults = Number.parseInt(
         picker.dataset.maxResults ?? "",
         10
      );

      if(!(input instanceof HTMLInputElement)
         || !(suggestions instanceof HTMLElement)
         || !(selected instanceof HTMLElement)
         || searchUrl === "")
      {
         return;
      }

      picker.dataset.entityLinkedEntitiesInitialized = "true";

      const state = {
         timerId: null,
         requestId: 0,
         selectedIndex: -1,
         items: []
      };

      input.addEventListener("input", () => {
         scheduleSearch(
            state,
            picker,
            input,
            suggestions,
            searchUrl,
            excludeEntityId,
            organizationOnly,
            maxResults,
            selected
         );
      });

      input.addEventListener("focus", () => {
         if(input.value.trim() !== "")
         {
            scheduleSearch(
               state,
               picker,
               input,
               suggestions,
               searchUrl,
               excludeEntityId,
               organizationOnly,
               maxResults,
               selected
            );
         }
      });

      input.addEventListener("keydown", event => {
         handleKeyDown(
            event,
            state,
            picker,
            input,
            suggestions,
            selected,
            searchUrl,
            excludeEntityId,
            organizationOnly,
            maxResults
         );
      });

      input.addEventListener("blur", () => {
         window.setTimeout(() => {
            if(!picker.contains(document.activeElement))
            {
               closeSuggestions(state, suggestions);
            }
         }, 120);
      });

      picker.addEventListener("click", event => {
         const removeButton = event.target instanceof Element
            ? event.target.closest(chipRemoveSelector)
            : null;

         if(!(removeButton instanceof HTMLElement))
         {
            return;
         }

         event.preventDefault();
         event.stopPropagation();

         const chip = removeButton.closest(chipSelector);

         if(!(chip instanceof HTMLElement))
         {
            return;
         }

         chip.remove();

         if(input.value.trim() !== "")
         {
            scheduleSearch(
               state,
               picker,
               input,
               suggestions,
               searchUrl,
               excludeEntityId,
               organizationOnly,
               maxResults,
               selected
            );
         }
      });
   }

   function scheduleSearch(
      state,
      picker,
      input,
      suggestions,
      searchUrl,
      excludeEntityId,
      organizationOnly,
      maxResults,
      selected
   )
   {
      if(state.timerId !== null)
      {
         window.clearTimeout(state.timerId);
      }

      state.timerId = window.setTimeout(() => {
         state.timerId = null;
         void search(
            state,
            picker,
            input,
            suggestions,
            searchUrl,
            excludeEntityId,
            organizationOnly,
            maxResults,
            selected
         );
      }, debounceMs);
   }

   async function search(
      state,
      picker,
      input,
      suggestions,
      searchUrl,
      excludeEntityId,
      organizationOnly,
      maxResults,
      selected
   )
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
      url.searchParams.set(
         "organizationOnly",
         organizationOnly ? "true" : "false"
      );

      if(excludeEntityId !== "")
      {
         url.searchParams.set("excludeEntityId", excludeEntityId);
      }

      if(Number.isFinite(maxResults) && maxResults > 0)
      {
         url.searchParams.set("maxResults", String(maxResults));
      }

      try
      {
         const response = await fetch(url, {
            headers: {
               Accept: "application/json"
            }
         });

         const payload = await response.json();

         if(requestId !== state.requestId)
         {
            return;
         }

         if(!response.ok)
         {
            throw new Error("Entity search failed.");
         }

         const selectedIds = new Set(getSelectedEntityIds(selected));
         const results = Array.isArray(payload.results)
            ? payload.results
               .map(normalizeResult)
               .filter(item => item !== null)
               .filter(item => !selectedIds.has(item.id))
            : [];

         renderSuggestions(state, picker, suggestions, input, results);
      }
      catch
      {
         if(requestId === state.requestId)
         {
            closeSuggestions(state, suggestions);
         }
      }
   }

   function renderSuggestions(state, picker, suggestions, input, items)
   {
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
         option.dataset.entityId = item.id;
         option.textContent = formatItemText(item);

         option.addEventListener("click", event => {
            event.preventDefault();
            void selectSuggestion(item, picker, suggestions, state);
         });

         option.addEventListener("mousedown", event => {
            if(document.activeElement === input)
            {
               event.preventDefault();
            }
         });

         option.addEventListener("mouseenter", () => {
            setActiveSuggestion(state, suggestions, index);
         });

         suggestions.append(option);
      });

      suggestions.hidden = false;
   }

   function handleKeyDown(
      event,
      state,
      picker,
      input,
      suggestions,
      selected,
      searchUrl,
      excludeEntityId,
      organizationOnly,
      maxResults
   )
   {
      if(event.key === "Backspace"
         && input.value === ""
         && getSelectedEntityCount(selected) > 0)
      {
         event.preventDefault();
         removeLastChip(selected);
         if(input.value.trim() !== "")
         {
            scheduleSearch(
               state,
               picker,
               input,
               suggestions,
               searchUrl,
               excludeEntityId,
               organizationOnly,
               maxResults,
               selected
            );
         }
         return;
      }

      if(suggestions.hidden)
      {
         return;
      }

      if(event.key === "ArrowDown")
      {
         event.preventDefault();
         setActiveSuggestion(state, suggestions, state.selectedIndex + 1);
         return;
      }

      if(event.key === "ArrowUp")
      {
         event.preventDefault();
         setActiveSuggestion(state, suggestions, state.selectedIndex - 1);
         return;
      }

      if(event.key === "Escape")
      {
         event.preventDefault();
         input.value = "";
         closeSuggestions(state, suggestions);
         return;
      }

      if(event.key === "Enter")
      {
         event.preventDefault();
         const item = state.items[Math.max(state.selectedIndex, 0)];

         if(item)
         {
            void selectSuggestion(item, picker, suggestions, state);
         }
      }
   }

   function setActiveSuggestion(state, suggestions, index)
   {
      const options = Array.from(
         suggestions.querySelectorAll(".broadcast-org-entity-option")
      );

      if(options.length === 0)
      {
         return;
      }

      const nextIndex = Math.max(0, Math.min(index, options.length - 1));
      state.selectedIndex = nextIndex;

      options.forEach((option, optionIndex) => {
         option.classList.toggle("is-active", optionIndex === nextIndex);
      });
   }

   function selectSuggestion(item, picker, suggestions, state)
   {
      const selected = picker.querySelector(selectedSelector);
      const input = picker.querySelector(inputSelector);

      if(!(selected instanceof HTMLElement)
         || !(input instanceof HTMLInputElement))
      {
         return;
      }

      if(getSelectedEntityIds(selected).includes(item.id))
      {
         closeSuggestions(state, suggestions);
         input.value = "";
         input.focus();
         return;
      }

      const chip = document.createElement("span");
      chip.className = "entity-linked-entities-chip";
      chip.dataset.entityLinkedEntitiesChip = "true";
      chip.dataset.entityId = item.id;

      const hidden = document.createElement("input");
      hidden.type = "hidden";
      hidden.name = "Entity.LinkedEntityIds";
      hidden.value = item.id;
      hidden.dataset.entityLinkedEntitiesHiddenId = "true";

      const label = document.createElement("span");
      label.textContent = formatItemText(item);

      const removeButton = document.createElement("button");
      removeButton.type = "button";
      removeButton.className = "entity-linked-entities-chip-remove";
      removeButton.dataset.entityLinkedEntitiesChipRemove = "true";
      removeButton.setAttribute("aria-label", `Remove ${item.text}`);
      removeButton.textContent = "×";

      chip.append(hidden, label, removeButton);
      selected.append(chip);

      input.value = "";
      closeSuggestions(state, suggestions);
      input.focus();
   }

   function removeLastChip(selected)
   {
      const chips = Array.from(selected.querySelectorAll(chipSelector));
      const lastChip = chips[chips.length - 1];

      if(lastChip instanceof HTMLElement)
      {
         lastChip.remove();
      }
   }

   function getSelectedEntityCount(selected)
   {
      return getSelectedEntityIds(selected).length;
   }

   function getSelectedEntityIds(selected)
   {
      return Array.from(selected.querySelectorAll(hiddenInputSelector))
         .map(input => input instanceof HTMLInputElement ? input.value : "")
         .map(value => value.trim())
         .filter(value => value !== "");
   }

   function closeSuggestions(state, suggestions)
   {
      if(state.timerId !== null)
      {
         window.clearTimeout(state.timerId);
         state.timerId = null;
      }

      state.requestId += 1;
      state.items = [];
      state.selectedIndex = -1;
      suggestions.hidden = true;
      suggestions.replaceChildren();
   }

   function normalizeResult(item)
   {
      if(!(item && typeof item === "object"))
      {
         return null;
      }

      const id = normalizeString(item.id);
      const name = normalizeString(item.name);

      if(id === "" || name === "")
      {
         return null;
      }

      return {
         id,
         text: name,
         entityType: normalizeString(item.entityType),
         sport: normalizeString(item.sport)
      };
   }

   function formatItemText(item)
   {
      const parts = [];
      const entityType = normalizeString(item.entityType);
      const sport = normalizeString(item.sport);

      if(entityType !== "")
      {
         parts.push(entityType);
      }

      if(sport !== "")
      {
         parts.push(sport);
      }

      return parts.length === 0
         ? item.text
         : `${item.text} (${parts.join(", ")})`;
   }

   function normalizeString(value)
   {
      return typeof value === "string" ? value.trim() : "";
   }
})();
