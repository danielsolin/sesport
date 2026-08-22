(() => {
   const pickerSelector = "[data-activity-participant-picker]";
   const inputSelector = "[data-activity-participant-input]";
   const idSelector = "[data-activity-participant-id]";
   const suggestionsSelector = "[data-activity-participant-suggestions]";
   const hiddenIdSelector = "[data-activity-participant-hidden-id]";
   const selectionSelector = "[data-activity-participant-selection]";
   const removeButtonSelector = "[data-activity-participant-remove]";
   const addFormSelector = "#add-participant-form";
   const addEntityIdSelector = "[data-add-participant-entity-id]";
   const optionSelector = ".broadcast-org-entity-option";

   document.addEventListener("DOMContentLoaded", () => {
      document.addEventListener("click", handleRemoveParticipantClick);
      document.querySelectorAll(pickerSelector).forEach(initializePicker);
   });

   function initializePicker(picker)
   {
      if(!(picker instanceof HTMLElement)
         || picker.dataset.activityParticipantInitialized === "true")
      {
         return;
      }

      const input = picker.querySelector(inputSelector);
      const hiddenId = picker.querySelector(idSelector);
      const suggestions = picker.querySelector(suggestionsSelector);
      const searchUrl = (picker.dataset.activityParticipantSearchUrl ?? "")
         .trim();
      const organizationEntityId = (
         picker.dataset.organizationEntityId ?? ""
      ).trim();

      if(!(input instanceof HTMLInputElement)
         || !(hiddenId instanceof HTMLInputElement)
         || !(suggestions instanceof HTMLElement)
         || searchUrl === "")
      {
         return;
      }

      picker.dataset.activityParticipantInitialized = "true";

      if(organizationEntityId === "")
      {
         input.disabled = true;
         input.placeholder = "No related organization";
         return;
      }

      const state = {
         timerId: null,
         requestId: 0,
         selectedIndex: -1,
         items: []
      };

      input.addEventListener("input", () => {
         hiddenId.value = "";
         scheduleSearch(
            state,
            input,
            suggestions,
            searchUrl,
            organizationEntityId
         );
      });

      input.addEventListener("focus", () => {
         scheduleSearch(
            state,
            input,
            suggestions,
            searchUrl,
            organizationEntityId
         );
      });

      input.addEventListener("keydown", event => {
         handleKeyDown(
            event,
            state,
            input,
            hiddenId,
            suggestions,
            searchUrl,
            organizationEntityId
         );
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
            selectSuggestion(
               item,
               input,
               suggestions,
               state,
               searchUrl,
               organizationEntityId
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

      input.addEventListener("blur", () => {
         window.setTimeout(() => {
            if(!picker.contains(document.activeElement))
            {
               closeSuggestions(state, suggestions);
            }
         }, 120);
      });
   }

   function scheduleSearch(
      state,
      input,
      suggestions,
      searchUrl,
      organizationEntityId
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
            input,
            suggestions,
            searchUrl,
            organizationEntityId
         );
      }, 180);
   }

   async function search(
      state,
      input,
      suggestions,
      searchUrl,
      organizationEntityId
   )
   {
      const requestId = ++state.requestId;
      const url = new URL(searchUrl, window.location.origin);
      url.searchParams.set("term", input.value.trim());
      url.searchParams.set("organizationEntityId", organizationEntityId);
      url.searchParams.set("format", "participant-suggestions");

      getSelectedEntityIds().forEach(entityId => {
         url.searchParams.append("excludedEntityIds", entityId);
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
            throw new Error("Participant search failed.");
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

   function handleKeyDown(
      event,
      state,
      input,
      hiddenId,
      suggestions,
      searchUrl,
      organizationEntityId
   )
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
         hiddenId.value = "";
         closeSuggestions(state, suggestions);
         return;
      }

      if(event.key === "Enter")
      {
         event.preventDefault();
         const item = state.items[Math.max(state.selectedIndex, 0)];

         if(item)
         {
            selectSuggestion(
               item,
               input,
               suggestions,
               state,
               searchUrl,
               organizationEntityId
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

   function selectSuggestion(
      item,
      input,
      suggestions,
      state,
      searchUrl,
      organizationEntityId
   )
   {
      input.value = "";
      closeSuggestions(state, suggestions);

      const activityId = getActivityId();

      if(activityId === "")
      {
         void loadParticipantSelectionAsync(
            searchUrl,
            organizationEntityId,
            [...getSelectedEntityIds(), item.id]
         );
         return;
      }

      const form = document.querySelector(addFormSelector);
      const entityId = form?.querySelector(addEntityIdSelector);

      if(!(form instanceof HTMLFormElement)
         || !(entityId instanceof HTMLInputElement))
      {
         return;
      }

      entityId.value = item.id;
      form.submit();
   }

   async function loadParticipantSelectionAsync(
      searchUrl,
      organizationEntityId,
      selectedEntityIds
   )
   {
      const selection = document.querySelector(selectionSelector);

      if(!(selection instanceof HTMLElement))
      {
         return;
      }

      const url = new URL(searchUrl, window.location.origin);
      url.searchParams.set("format", "participant-selection");
      url.searchParams.set("organizationEntityId", organizationEntityId);
      selectedEntityIds.forEach(entityId => {
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

         if(!response.ok)
         {
            throw new Error("Participant selection update failed.");
         }

         window.replaceElementWithPartialHtml(selection, html);
      }
      catch(error)
      {
         console.error(error);
      }
   }

   function getActivityId()
   {
      const picker = document.querySelector(pickerSelector);

      return picker instanceof HTMLElement
         ? (picker.dataset.activityId ?? "").trim()
         : "";
   }

   function getSelectedEntityIds()
   {
      return Array.from(document.querySelectorAll(hiddenIdSelector))
         .map(input => input instanceof HTMLInputElement ? input.value : "")
         .map(value => value.trim())
         .filter(value => value !== "");
   }

   function handleRemoveParticipantClick(event)
   {
      if(!(event.target instanceof HTMLElement))
      {
         return;
      }

      const button = event.target.closest(removeButtonSelector);

      if(!(button instanceof HTMLButtonElement))
      {
         return;
      }

      const row = button.closest("[data-activity-participant-row]");
      const picker = document.querySelector(pickerSelector);
      const organizationEntityId = picker instanceof HTMLElement
         ? (picker.dataset.organizationEntityId ?? "").trim()
         : "";
      const searchUrl = picker instanceof HTMLElement
         ? (picker.dataset.activityParticipantSearchUrl ?? "").trim()
         : "";

      if(!(row instanceof HTMLElement)
         || !(picker instanceof HTMLElement)
         || searchUrl === "")
      {
         return;
      }

      const entityId = (row.dataset.activityParticipantRow ?? "").trim();

      if(entityId === "")
      {
         return;
      }

      void loadParticipantSelectionAsync(
         searchUrl,
         organizationEntityId,
         getSelectedEntityIds().filter(id => id !== entityId)
      );
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
            relatedOrganizations: (
               option.dataset.relatedOrganizations ?? ""
            ).trim(),
            watchPriority: (option.dataset.watchPriority ?? "").trim(),
            gender: (option.dataset.gender ?? "").trim(),
            alias: (option.dataset.alias ?? "").trim()
         };
   }
})();
