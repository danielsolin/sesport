(() => {
   const pickerSelector = "[data-broadcast-activity-group-picker]";
   const inputSelector = "[data-broadcast-activity-group-input]";
   const idSelector = "[data-broadcast-activity-group-id-input]";
   const suggestionsSelector =
      "[data-broadcast-activity-group-suggestions]";
   const cellSelector = "[data-broadcast-inline-edit-field='group']";
   const optionSelector = ".broadcast-org-entity-option";

   window.initializeBroadcastActivityGroupAutocomplete =
      initializePickers;

   if(document.readyState === "loading")
   {
      document.addEventListener("DOMContentLoaded", initializePickers);
   }
   else
   {
      initializePickers();
   }

   document.addEventListener("focusin", event => {
      initializePickerFromEvent(event);
   });
   document.addEventListener("input", event => {
      initializePickerFromEvent(event);
   });

   function initializePickerFromEvent(event)
   {
      if(!(event.target instanceof Element))
      {
         return;
      }

      const picker = event.target.closest(pickerSelector);

      if(picker instanceof HTMLElement)
      {
         initializePicker(picker);
      }
   }

   function initializePickers(root = document)
   {
      const pickers = root instanceof Element &&
         root.matches(pickerSelector)
         ? [root]
         : root.querySelectorAll(pickerSelector);

      pickers.forEach(initializePicker);
   }

   function initializePicker(picker)
   {
      if(!(picker instanceof HTMLElement) ||
         picker.dataset.broadcastActivityGroupInitialized === "true")
      {
         return;
      }

      const input = picker.querySelector(inputSelector);
      const hiddenId = picker.querySelector(idSelector);
      const suggestions = picker.querySelector(suggestionsSelector);
      const cell = picker.closest(cellSelector);
      const row = picker.closest("tr[data-broadcast-row='true']");
      const searchUrl = (
         picker.dataset.broadcastActivityGroupSearchUrl ?? ""
      ).trim();

      if(!(input instanceof HTMLInputElement) ||
         !(hiddenId instanceof HTMLInputElement) ||
         !(suggestions instanceof HTMLElement) ||
         !(cell instanceof HTMLElement) ||
         !(row instanceof HTMLElement) ||
         searchUrl === "")
      {
         return;
      }

      picker.dataset.broadcastActivityGroupInitialized = "true";
      suggestions.classList.add(
         "broadcast-activity-group-suggestions-fixed"
      );
      const state = {
         timerId: null,
         requestId: 0,
         selectedIndex: -1,
         items: []
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
            showSuggestions();
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
               void selectItem(item);
            });
            suggestions.append(option);
         });

         showSuggestions();
      };

      const positionSuggestions = () => {
         const inputRect = input.getBoundingClientRect();
         suggestions.style.left = `${inputRect.left}px`;
         suggestions.style.top = `${inputRect.bottom + 2}px`;
         suggestions.style.width = `${inputRect.width}px`;
      };

      const showSuggestions = () => {
         positionSuggestions();
         suggestions.hidden = false;
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

      const saveSelectedGroup = async item => {
         const url = window.getBroadcastInlineEditUrl?.() ?? "";
         const broadcastId = (cell.dataset.broadcastId ?? "").trim();

         if(url === "" || broadcastId === "" || item.id === "")
         {
            return;
         }

         input.dataset.broadcastInlineEditSaving = "true";
         input.disabled = true;

         try
         {
            const payload =
               await window.postBroadcastInlineEditAsync(
                  url,
                  broadcastId,
                  "group",
                  item.text,
                  item.id
               );
            window.updateBroadcastInlineEditCell?.(cell, payload);
         }
         catch(error)
         {
            window.alert(
               error instanceof Error
                  ? error.message
                  : "Broadcast group update failed."
            );
            hiddenId.value = "";
            delete cell.dataset.broadcastActivityGroupId;
         }
         finally
         {
            input.disabled = false;
            delete input.dataset.broadcastInlineEditSaving;
         }
      };

      const selectItem = async item => {
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
         closeSuggestions();

         if(item.kind === "create")
         {
            hiddenId.value = "";
            delete cell.dataset.broadcastActivityGroupId;
            input.blur();
            return;
         }

         hiddenId.value = item.id;
         cell.dataset.broadcastActivityGroupId = item.id;
         await saveSelectedGroup(item);
      };

      const search = async () => {
         const organizationInput = row.querySelector(
            "[data-org-entity-id]"
         );
         const organizationId = organizationInput instanceof HTMLInputElement
            ? organizationInput.value.trim()
            : (
               cell.dataset.broadcastOrganizationEntityId ?? ""
            ).trim();

         if(organizationId === "")
         {
            closeSuggestions();
            return;
         }

         const term = input.value.trim();
         const requestId = ++state.requestId;
         const url = new URL(searchUrl, window.location.origin);
         url.searchParams.set("term", term);
         url.searchParams.set("organizationEntityId", organizationId);

         try
         {
            const response = await fetch(url, {
               headers: { Accept: "application/json" }
            });
            const payload = await response.json();

            if(requestId !== state.requestId || !response.ok)
            {
               return;
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
         delete cell.dataset.broadcastActivityGroupId;
         scheduleSearch();
      });
      input.addEventListener("focus", () => {
         scheduleSearch();
      });
      input.addEventListener("keydown", event => {
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
         else if(event.key === "Enter" && state.selectedIndex >= 0)
         {
            event.preventDefault();
            void selectItem(state.items[state.selectedIndex]);
         }
         else if(event.key === "Escape")
         {
            closeSuggestions();
         }
      });
      input.addEventListener("blur", () => {
         window.setTimeout(closeSuggestions, 120);
      });
   }

   function normalizeResult(item)
   {
      if(!item || typeof item !== "object")
      {
         return null;
      }

      const id = typeof item.id === "string" ? item.id.trim() : "";
      const text = typeof item.text === "string"
         ? item.text.trim()
         : "";

      return id !== "" && text !== ""
         ? { kind: "existing", id, text }
         : null;
   }
})();
