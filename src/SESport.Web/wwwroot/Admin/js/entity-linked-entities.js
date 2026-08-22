(() => {
   const pickerSelector = "[data-entity-linked-entities-picker]";
   const inputSelector = "[data-entity-linked-entities-input]";
   const suggestionsSelector =
      "[data-entity-linked-entities-suggestions]";
   const gridSelector = "[data-entity-linked-entities-grid]";
   const rowSelector = "[data-entity-linked-entities-row]";
   const removeButtonSelector =
      "[data-entity-linked-entities-remove]";
   const hiddenInputSelector =
      "[data-entity-linked-entities-hidden-id]";
   const optionSelector = ".broadcast-org-entity-option";
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
      const searchUrl = (picker.dataset.entityLinkedEntitiesSearchUrl ?? "")
         .trim();
      const updateUrl = (picker.dataset.entityLinkedEntitiesUpdateUrl ?? "")
         .trim();
      const excludeEntityId = (picker.dataset.entityId ?? "").trim();
      const isExistingEntity = excludeEntityId !== "";
      const organizationOnly = (
         picker.dataset.organizationOnly ?? "true"
      ).trim().toLowerCase() !== "false";
      const maxResults = Number.parseInt(
         picker.dataset.maxResults ?? "",
         10
      );

      if(!(input instanceof HTMLInputElement)
         || !(suggestions instanceof HTMLElement)
         || searchUrl === "")
      {
         return;
      }

      picker.dataset.entityLinkedEntitiesInitialized = "true";

      const state = {
         timerId: null,
         requestId: 0,
         selectedIndex: -1,
         items: [],
         pendingEntityIds: new Set()
      };

      input.addEventListener("input", () => {
         scheduleSearch(state, picker, input, suggestions, searchUrl);
      });

      input.addEventListener("focus", () => {
         if(input.value.trim() !== "")
         {
            scheduleSearch(state, picker, input, suggestions, searchUrl);
         }
      });

      input.addEventListener("keydown", event => {
         handleKeyDown(event, state, picker, input, suggestions);
      });

      suggestions.addEventListener("click", event => {
         const option = event.target instanceof Element
            ? event.target.closest(optionSelector)
            : null;
         const item = option instanceof HTMLElement
            ? getItemFromOption(option)
            : null;

         if(item)
         {
            event.preventDefault();
            void selectSuggestionAsync(
               item,
               state,
               picker,
               input,
               suggestions,
               searchUrl,
               excludeEntityId,
               isExistingEntity,
               updateUrl
            );
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
         const options = Array.from(
            suggestions.querySelectorAll(optionSelector)
         );
         const index = options.indexOf(option);

         if(index >= 0)
         {
            setActiveSuggestion(state, suggestions, index);
         }
      });

      picker.addEventListener("click", event => {
         const removeButton = event.target instanceof Element
            ? event.target.closest(removeButtonSelector)
            : null;

         if(!(removeButton instanceof HTMLElement))
         {
            return;
         }

         event.preventDefault();
         event.stopPropagation();
         void removeRowAsync(
            state,
            picker,
            input,
            suggestions,
            searchUrl,
            excludeEntityId,
            isExistingEntity,
            updateUrl,
            removeButton.closest(rowSelector),
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
   }

   function scheduleSearch(state, picker, input, suggestions, searchUrl)
   {
      if(state.timerId !== null)
      {
         window.clearTimeout(state.timerId);
      }

      state.timerId = window.setTimeout(() => {
         state.timerId = null;
         void search(state, picker, input, suggestions, searchUrl);
      }, debounceMs);
   }

   async function search(state, picker, input, suggestions, searchUrl)
   {
      const query = input.value.trim();

      if(query === "")
      {
         closeSuggestions(state, suggestions);
         return;
      }

      const requestId = ++state.requestId;
      const url = new URL(searchUrl, window.location.origin);
      const organizationOnly = (
         picker.dataset.organizationOnly ?? "true"
      ).trim().toLowerCase() !== "false";
      const maxResults = Number.parseInt(
         picker.dataset.maxResults ?? "",
         10
      );
      const excludeEntityId = (picker.dataset.entityId ?? "").trim();
      const grid = picker.querySelector(gridSelector);

      url.searchParams.set("term", query);
      url.searchParams.set("format", "linked-entity-suggestions");
      url.searchParams.set(
         "organizationOnly",
         organizationOnly ? "true" : "false"
      );
      url.searchParams.set("includeRelatedEntityNames", "false");

      if(excludeEntityId !== "")
      {
         url.searchParams.set("excludeEntityId", excludeEntityId);
      }

      if(Number.isFinite(maxResults) && maxResults > 0)
      {
         url.searchParams.set("maxResults", String(maxResults));
      }

      getSelectedEntityIds(grid).forEach(entityId => {
         url.searchParams.append("selectedEntityIds", entityId);
      });

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
            throw new Error("Entity search failed.");
         }

         renderSuggestions(state, suggestions, html);
      }
      catch
      {
         if(requestId === state.requestId)
         {
            closeSuggestions(state, suggestions);
         }
      }
   }

   function renderSuggestions(state, suggestions, html)
   {
      window.replaceContentsWithPartialHtml(suggestions, html);
      state.items = Array.from(
         suggestions.querySelectorAll(optionSelector)
      ).map(getItemFromOption).filter(item => item !== null);
      state.selectedIndex = -1;
      suggestions.hidden = false;
   }

   function handleKeyDown(event, state, picker, input, suggestions)
   {
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
            const updateUrl = (
               picker.dataset.entityLinkedEntitiesUpdateUrl ?? ""
            ).trim();
            const excludeEntityId = (picker.dataset.entityId ?? "").trim();
            void selectSuggestionAsync(
               item,
               state,
               picker,
               input,
               suggestions,
               picker.dataset.entityLinkedEntitiesSearchUrl ?? "",
               excludeEntityId,
               excludeEntityId !== "",
               updateUrl
            );
         }
      }
   }

   function setActiveSuggestion(state, suggestions, index)
   {
      const options = Array.from(
         suggestions.querySelectorAll(optionSelector)
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

   async function selectSuggestionAsync(
      item,
      state,
      picker,
      input,
      suggestions,
      searchUrl,
      excludeEntityId,
      isExistingEntity,
      updateUrl
   )
   {
      const grid = picker.querySelector(gridSelector);

      if(!(grid instanceof HTMLElement))
      {
         return;
      }

      if(getSelectedEntityIds(grid).includes(item.id)
         || state.pendingEntityIds.has(item.id))
      {
         closeSuggestions(state, suggestions);
         input.value = "";
         input.focus();
         return;
      }

      state.pendingEntityIds.add(item.id);

      try
      {
         if(isExistingEntity && updateUrl !== "" && excludeEntityId !== "")
         {
            const html = await postEntityLinkAsync(
               updateUrl,
               excludeEntityId,
               "add",
               item.id
            );
            replaceGrid(grid, html);
         }
         else
         {
            const selectedIds = [...getSelectedEntityIds(grid), item.id];
            const html = await loadGridAsync(
               searchUrl,
               excludeEntityId,
               selectedIds
            );
            replaceGrid(grid, html);
         }
      }
      catch(error)
      {
         window.alert(
            error instanceof Error
               ? error.message
               : "Linked entity update failed."
         );
      }
      finally
      {
         state.pendingEntityIds.delete(item.id);
         input.value = "";
         closeSuggestions(state, suggestions);
         input.focus();
      }
   }

   async function removeRowAsync(
      state,
      picker,
      input,
      suggestions,
      searchUrl,
      excludeEntityId,
      isExistingEntity,
      updateUrl,
      row,
      organizationOnly,
      maxResults
   )
   {
      if(!(row instanceof HTMLElement))
      {
         return;
      }

      const entityId = (row.dataset.entityId ?? "").trim();
      const grid = picker.querySelector(gridSelector);

      if(entityId === ""
         || !(grid instanceof HTMLElement)
         || state.pendingEntityIds.has(entityId))
      {
         return;
      }

      state.pendingEntityIds.add(entityId);

      try
      {
         let html;

         if(isExistingEntity && updateUrl !== "" && excludeEntityId !== "")
         {
            html = await postEntityLinkAsync(
               updateUrl,
               excludeEntityId,
               "remove",
               entityId
            );
         }
         else
         {
            html = await loadGridAsync(
               searchUrl,
               excludeEntityId,
               getSelectedEntityIds(grid).filter(id => id !== entityId),
               organizationOnly,
               maxResults
            );
         }

         replaceGrid(grid, html);
      }
      catch(error)
      {
         window.alert(
            error instanceof Error
               ? error.message
               : "Linked entity update failed."
         );
      }
      finally
      {
         state.pendingEntityIds.delete(entityId);
      }
   }

   async function loadGridAsync(
      searchUrl,
      excludeEntityId,
      selectedIds
   )
   {
      const url = new URL(searchUrl, window.location.origin);
      url.searchParams.set("format", "linked-entity-grid");

      if(excludeEntityId !== "")
      {
         url.searchParams.set("excludeEntityId", excludeEntityId);
      }

      selectedIds.forEach(entityId => {
         url.searchParams.append("selectedEntityIds", entityId);
      });

      const response = await fetch(url, {
         headers: {
            Accept: "text/html"
         }
      });
      const responseText = await response.text();

      if(!response.ok)
      {
         throw new Error(
            responseText.trim() ||
               `Request failed with status ${response.status}`
         );
      }

      return responseText;
   }

   function replaceGrid(grid, html)
   {
      window.replaceElementWithPartialHtml(grid, html);
   }

   async function postEntityLinkAsync(
      url,
      entityId,
      action,
      linkedEntityId
   )
   {
      const formData = new URLSearchParams();
      const tokenInput = document.querySelector(
         "input[name='__RequestVerificationToken']"
      );

      if(tokenInput instanceof HTMLInputElement)
      {
         formData.append("__RequestVerificationToken", tokenInput.value);
      }

      formData.append("id", entityId);
      formData.append("action", action);
      formData.append("linkedEntityId", linkedEntityId);

      const response = await fetch(url, {
         method: "post",
         body: formData,
         headers: {
            Accept: "text/html"
         }
      });
      const responseText = await response.text();

      if(!response.ok)
      {
         throw new Error(
            responseText.trim() ||
               `Request failed with status ${response.status}`
         );
      }

      return responseText;
   }

   function getSelectedEntityIds(grid)
   {
      if(!(grid instanceof HTMLElement))
      {
         return [];
      }

      return Array.from(grid.querySelectorAll(hiddenInputSelector))
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

   function getItemFromOption(option)
   {
      if(!(option instanceof HTMLElement))
      {
         return null;
      }

      const id = (option.dataset.entityId ?? "").trim();
      const text = (option.dataset.entityText ?? option.textContent ?? "")
         .trim();

      return id === "" || text === ""
         ? null
         : {
            id,
            text,
            entityType: (option.dataset.entityType ?? "").trim(),
            sport: (option.dataset.entitySport ?? "").trim()
         };
   }
})();
