(() => {
   const pickerSelector = "[data-entity-linked-entities-picker]";
   const inputSelector = "[data-entity-linked-entities-input]";
   const suggestionsSelector =
      "[data-entity-linked-entities-suggestions]";
   const gridSelector = "[data-entity-linked-entities-grid]";
   const rowsSelector = "[data-entity-linked-entities-rows]";
   const rowSelector = "[data-entity-linked-entities-row]";
   const removeButtonSelector =
      "[data-entity-linked-entities-remove]";
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
      const grid = picker.querySelector(gridSelector);
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
         || !(grid instanceof HTMLElement)
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
         scheduleSearch(
            state,
            picker,
            input,
            suggestions,
            searchUrl,
            excludeEntityId,
            organizationOnly,
            maxResults,
            grid,
            updateUrl,
            isExistingEntity
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
               grid,
               updateUrl,
               isExistingEntity
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
            grid,
            searchUrl,
            excludeEntityId,
            organizationOnly,
            maxResults,
            updateUrl,
            isExistingEntity
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
            ? event.target.closest(removeButtonSelector)
            : null;

         if(!(removeButton instanceof HTMLElement))
         {
            return;
         }

         event.preventDefault();
         event.stopPropagation();

         const row = removeButton.closest(rowSelector);

         if(!(row instanceof HTMLElement))
         {
            return;
         }

         void removeRowAsync(
            state,
            row,
            picker,
            input,
            suggestions,
            searchUrl,
            excludeEntityId,
            organizationOnly,
            maxResults,
            grid,
            updateUrl,
            isExistingEntity
         );
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
      grid,
      updateUrl,
      isExistingEntity
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
            grid,
            updateUrl,
            isExistingEntity
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
      grid,
      updateUrl,
      isExistingEntity
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
      url.searchParams.set("includeRelatedEntityNames", "false");

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

         const selectedIds = new Set(getSelectedEntityIds(grid));
         state.pendingEntityIds.forEach(id => {
            selectedIds.add(id);
         });
         const results = Array.isArray(payload.results)
            ? payload.results
               .map(normalizeResult)
               .filter(item => item !== null)
               .filter(item => !selectedIds.has(item.id))
            : [];

         renderSuggestions(
            state,
            picker,
            suggestions,
            input,
            results,
            excludeEntityId,
            updateUrl,
            isExistingEntity
         );
      }
      catch
      {
         if(requestId === state.requestId)
         {
            closeSuggestions(state, suggestions);
         }
      }
   }

   function renderSuggestions(
      state,
      picker,
      suggestions,
      input,
      items,
      entityId,
      updateUrl,
      isExistingEntity
   )
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
            void selectSuggestionAsync(
               item,
               picker,
               suggestions,
               state,
               entityId,
               updateUrl,
               isExistingEntity
            );
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
      grid,
      searchUrl,
      excludeEntityId,
      organizationOnly,
      maxResults,
      updateUrl,
      isExistingEntity
   )
   {
      if(event.key === "Backspace"
         && input.value === ""
         && getSelectedEntityCount(grid) > 0)
      {
         event.preventDefault();
         const row = getLastRow(grid);

         if(row instanceof HTMLElement)
         {
            void removeRowAsync(
               state,
               row,
               picker,
               input,
               suggestions,
               searchUrl,
               excludeEntityId,
               organizationOnly,
               maxResults,
               grid,
               updateUrl,
               isExistingEntity
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
            void selectSuggestionAsync(
               item,
               picker,
               suggestions,
               state,
               excludeEntityId,
               updateUrl,
               isExistingEntity
            );
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

   async function selectSuggestionAsync(
      item,
      picker,
      suggestions,
      state,
      entityId,
      updateUrl,
      isExistingEntity
   )
   {
      const grid = picker.querySelector(gridSelector);
      const input = picker.querySelector(inputSelector);

      if(!(grid instanceof HTMLElement)
         || !(input instanceof HTMLInputElement))
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

      if(isExistingEntity && updateUrl !== "" && entityId !== "")
      {
         state.pendingEntityIds.add(item.id);

         try
         {
            await postEntityLinkAsync(updateUrl, entityId, "add", item.id);
         }
         catch(error)
         {
            window.alert(
               error instanceof Error
                  ? error.message
                  : "Linked entity update failed."
            );
            return;
         }
         finally
         {
            state.pendingEntityIds.delete(item.id);
         }
      }

      appendSelectedRow(grid, item);

      input.value = "";
      closeSuggestions(state, suggestions);
      input.focus();
   }

   function appendSelectedRow(grid, item)
   {
      if(!(grid instanceof HTMLElement))
      {
         return;
      }

      const rows = ensureEntityLinkedEntitiesTable(grid);

      if(!(rows instanceof HTMLElement))
      {
         return;
      }

      const row = document.createElement("tr");
      row.dataset.entityLinkedEntitiesRow = item.id;
      row.dataset.entityId = item.id;
      row.append(
         createCell(createEntityLink(item)),
         createCell(document.createTextNode(item.entityType)),
         createCell(document.createTextNode(item.sport)),
         createActionCell(item)
      );
      rows.append(row);
   }

   function ensureEntityLinkedEntitiesTable(grid)
   {
      const existingRows = grid.querySelector(rowsSelector);

      if(existingRows instanceof HTMLElement)
      {
         return existingRows;
      }

      const wrap = document.createElement("div");
      wrap.className = "admin-table-wrap";
      wrap.innerHTML = `
         <table class="admin-table admin-table-compact
                       entity-linked-entities-table">
            <thead>
               <tr>
                  <th>Name</th>
                  <th>Entity Type</th>
                  <th>Sport</th>
                  <th></th>
               </tr>
            </thead>
            <tbody data-entity-linked-entities-rows></tbody>
         </table>
      `;
      grid.replaceChildren(wrap);

      return grid.querySelector(rowsSelector);
   }

   function createCell(content)
   {
      const cell = document.createElement("td");
      cell.append(content);
      return cell;
   }

   function createActionCell(item)
   {
      const cell = document.createElement("td");
      cell.className = "table-actions";

      const hidden = document.createElement("input");
      hidden.type = "hidden";
      hidden.name = "Entity.LinkedEntityIds";
      hidden.value = item.id;
      hidden.dataset.entityLinkedEntitiesHiddenId = "true";

      const removeButton = document.createElement("button");
      removeButton.type = "button";
      removeButton.dataset.entityLinkedEntitiesRemove = "true";
      removeButton.setAttribute("aria-label", `Remove ${item.text}`);
      removeButton.textContent = "Delete";

      cell.append(hidden, removeButton);
      return cell;
   }

   function createEntityLink(item)
   {
      const link = document.createElement("a");
      link.href = `/Admin/Entities/Edit/${encodeURIComponent(item.id)}`;
      link.textContent = item.text;
      return link;
   }

   function getLastRow(grid)
   {
      const rows = Array.from(grid.querySelectorAll(rowSelector));

      return rows[rows.length - 1] ?? null;
   }

   function getSelectedEntityCount(grid)
   {
      return getSelectedEntityIds(grid).length;
   }

   function getSelectedEntityIds(grid)
   {
      return Array.from(grid.querySelectorAll(hiddenInputSelector))
         .map(input => input instanceof HTMLInputElement ? input.value : "")
         .map(value => value.trim())
         .filter(value => value !== "");
   }

   async function removeRowAsync(
      state,
      row,
      picker,
      input,
      suggestions,
      searchUrl,
      excludeEntityId,
      organizationOnly,
      maxResults,
      grid,
      updateUrl,
      isExistingEntity
   )
   {
      const entityId = (row.dataset.entityId ?? "").trim();

      if(entityId === "")
      {
         row.remove();
         renderEmptyGridIfNeeded(grid);
         return;
      }

      if(state.pendingEntityIds.has(entityId))
      {
         return;
      }

      if(isExistingEntity && updateUrl !== "" && excludeEntityId !== "")
      {
         state.pendingEntityIds.add(entityId);

         try
         {
            await postEntityLinkAsync(
               updateUrl,
               excludeEntityId,
               "remove",
               entityId
            );
         }
         catch(error)
         {
            window.alert(
               error instanceof Error
                  ? error.message
                  : "Linked entity update failed."
            );
            return;
         }
         finally
         {
            state.pendingEntityIds.delete(entityId);
         }
      }

      row.remove();
      renderEmptyGridIfNeeded(grid);

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
            grid,
            updateUrl,
            isExistingEntity
         );
      }
   }

   function renderEmptyGridIfNeeded(grid)
   {
      const rows = grid.querySelector(rowsSelector);

      if(!(rows instanceof HTMLElement) || rows.children.length === 0)
      {
         renderEmptyGrid(grid);
      }
   }

   function renderEmptyGrid(grid)
   {
      const notice = document.createElement("div");
      notice.className = "notice";
      notice.dataset.entityLinkedEntitiesEmpty = "true";
      notice.textContent = "No linked entities.";
      grid.replaceChildren(notice);
   }

   async function postEntityLinkAsync(
      url,
      entityId,
      action,
      linkedEntityId
   )
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      formData.append("id", entityId);
      formData.append("action", action);
      formData.append("linkedEntityId", linkedEntityId);

      const response = await fetch(url, {
         method: "post",
         body: formData,
         headers: {
            Accept: "application/json"
         }
      });
      const responseText = await response.text();
      const trimmedResponseText = responseText.trim();
      let payload = null;

      if(trimmedResponseText !== "")
      {
         try
         {
            payload = JSON.parse(trimmedResponseText);
         }
         catch
         {
            payload = null;
         }
      }

      if(!response.ok)
      {
         throw new Error(
            payload?.error ||
               trimmedResponseText ||
               `Request failed with status ${response.status}`
         );
      }

      return payload ?? {};
   }

   function getAntiForgeryToken()
   {
      const tokenInput = document.querySelector(
         "input[name='__RequestVerificationToken']"
      );

      if(!(tokenInput instanceof HTMLInputElement))
      {
         return "";
      }

      return tokenInput.value;
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
