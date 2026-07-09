(() => {
   const pickerSelector = "[data-activity-participant-picker]";
   const inputSelector = "[data-activity-participant-input]";
   const idSelector = "[data-activity-participant-id]";
   const suggestionsSelector = "[data-activity-participant-suggestions]";
   const hiddenInputsSelector = "[data-activity-participant-hidden-inputs]";
   const hiddenIdSelector = "[data-activity-participant-hidden-id]";
   const gridSelector = "[data-activity-participant-grid]";
   const rowsSelector = "[data-activity-participant-rows]";
   const addFormSelector = "#add-participant-form";
   const addEntityIdSelector = "[data-add-participant-entity-id]";

   document.addEventListener("DOMContentLoaded", () => {
      document.querySelectorAll(pickerSelector).forEach(initializePicker);
   });

   function initializePicker(picker)
   {
      if(!(picker instanceof HTMLElement))
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
         handleKeyDown(event, state, input, hiddenId, suggestions);
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
      const query = input.value.trim();

      const requestId = ++state.requestId;
      const url = new URL(searchUrl, window.location.origin);
      url.searchParams.set("term", query);
      url.searchParams.set("organizationEntityId", organizationEntityId);

      getSelectedEntityIds().forEach(entityId => {
         url.searchParams.append("excludedEntityIds", entityId);
      });

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
            throw new Error("Participant search failed.");
         }

         const results = Array.isArray(payload.results)
            ? payload.results.map(normalizeResult).filter(item => item !== null)
            : [];

         renderSuggestions(state, suggestions, input, results);
      }
      catch
      {
         if(requestId === state.requestId)
         {
            closeSuggestions(state, suggestions);
         }
      }
   }

   function renderSuggestions(state, suggestions, input, items)
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
         option.textContent = item.text;

         option.addEventListener("click", event => {
            event.preventDefault();
            selectSuggestion(item, input, suggestions, state);
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

   function handleKeyDown(event, state, input, hiddenId, suggestions)
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
            selectSuggestion(item, input, suggestions, state);
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

   function selectSuggestion(item, input, suggestions, state)
   {
      input.value = "";
      closeSuggestions(state, suggestions);

      const activityId = getActivityId();

      if(activityId === "")
      {
         addUnsavedParticipant(item);
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

   function addUnsavedParticipant(item)
   {
      if(getSelectedEntityIds().includes(item.id))
      {
         return;
      }

      const hiddenInputs = document.querySelector(hiddenInputsSelector);

      if(hiddenInputs instanceof HTMLElement)
      {
         const input = document.createElement("input");
         input.type = "hidden";
         input.name = "Activity.LinkedEntityIds";
         input.value = item.id;
         input.dataset.activityParticipantHiddenId = "true";
         hiddenInputs.append(input);
      }

      renderUnsavedParticipantRow(item);
   }

   function renderUnsavedParticipantRow(item)
   {
      const grid = document.querySelector(gridSelector);

      if(!(grid instanceof HTMLElement))
      {
         return;
      }

      ensureUnsavedTable(grid);
      const rows = grid.querySelector(rowsSelector);

      if(!(rows instanceof HTMLElement))
      {
         return;
      }

      const row = document.createElement("tr");
      row.dataset.activityParticipantRow = item.id;
      row.append(
         createCell(createEntityLink(item)),
         createCell(document.createTextNode(item.relatedOrganizations)),
         createCell(document.createTextNode(item.watchPriority)),
         createCell(document.createTextNode(item.gender)),
         createCell(document.createTextNode(item.alias)),
         createCell(document.createTextNode(""))
      );
      rows.append(row);
   }

   function ensureUnsavedTable(grid)
   {
      if(grid.querySelector(rowsSelector))
      {
         return;
      }

      const wrap = document.createElement("div");
      wrap.className = "admin-table-wrap";
      wrap.innerHTML = `
         <table class="admin-table admin-table-compact">
            <thead>
               <tr>
                  <th>Name</th>
                  <th>Related (orgs)</th>
                  <th>Watch Priority</th>
                  <th>Gender</th>
                  <th>Alias</th>
                  <th></th>
               </tr>
            </thead>
            <tbody data-activity-participant-rows></tbody>
         </table>
      `;
      grid.replaceChildren(wrap);
   }

   function createCell(content)
   {
      const cell = document.createElement("td");
      cell.append(content);
      return cell;
   }

   function createEntityLink(item)
   {
      const link = document.createElement("a");
      link.href = `/Admin/Entities/Edit/${encodeURIComponent(item.id)}`;
      link.textContent = item.text;
      return link;
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
      const text = normalizeString(item.text);

      if(id === "" || text === "")
      {
         return null;
      }

      return {
         id,
         text,
         relatedOrganizations: normalizeString(item.relatedOrganizations),
         watchPriority: normalizeString(item.watchPriority),
         gender: normalizeString(item.gender),
         alias: normalizeString(item.alias)
      };
   }

   function normalizeString(value)
   {
      return typeof value === "string" ? value.trim() : "";
   }
})();
