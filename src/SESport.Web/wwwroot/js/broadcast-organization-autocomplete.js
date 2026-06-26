(() => {
   const broadcastInlineEditCellSelector =
      "[data-broadcast-inline-edit-field]";
   const broadcastInlineEditOrganizationField = "organization";
   const broadcastOrganizationAutocompleteSelector =
      "[data-org-entity-autocomplete]";
   const broadcastOrganizationInputSelector = "[data-org-entity-input]";
   const broadcastOrganizationIdSelector = "[data-org-entity-id]";
   const broadcastOrganizationSuggestionsSelector =
      "[data-org-entity-suggestions]";
   const broadcastOrganizationSearchUrlSelector =
      "[data-org-entity-search-url]";
   const getBroadcastInlineEditUrl =
      window.getBroadcastInlineEditUrl;
   const postBroadcastInlineEditAsync =
      window.postBroadcastInlineEditAsync;
   const updateBroadcastInlineEditCell =
      window.updateBroadcastInlineEditCell;

   window.initializeBroadcastOrganizationAutocomplete =
      initializeBroadcastOrganizationAutocomplete;
   window.setBroadcastOrganizationLockState =
      setBroadcastOrganizationLockState;

   function initializeBroadcastOrganizationAutocomplete(root = document)
   {
      root.querySelectorAll(broadcastOrganizationAutocompleteSelector).forEach(
         container => {
            initializeBroadcastOrganizationAutocompleteContainer(container);
         }
      );
   }

   function initializeBroadcastOrganizationAutocompleteContainer(container)
   {
      if(!(container instanceof HTMLElement)
         || container.dataset.broadcastOrgAutocompleteInitialized === "true")
      {
         return;
      }

      const cell = container.closest(broadcastInlineEditCellSelector);
      const input = container.querySelector(
         broadcastOrganizationInputSelector
      );
      const hiddenId = container.querySelector(
         broadcastOrganizationIdSelector
      );
      const suggestions = container.querySelector(
         broadcastOrganizationSuggestionsSelector
      );
      const searchUrl = getBroadcastOrganizationSearchUrl();
      const broadcastId = (cell?.dataset.broadcastId ?? "").trim();
      const originalId = (cell?.dataset.broadcastInlineEditValue ?? "").trim();
      const originalLabel = (cell?.dataset.broadcastInlineEditLabel ?? "").trim();

      if(!(cell instanceof HTMLElement)
         || !(input instanceof HTMLInputElement)
         || !(hiddenId instanceof HTMLInputElement)
         || !(suggestions instanceof HTMLElement)
         || searchUrl === ""
         || broadcastId === "")
      {
         return;
      }

      container.dataset.broadcastOrgAutocompleteInitialized = "true";
      cell.dataset.broadcastInlineEditValue = originalId;
      cell.dataset.broadcastInlineEditLabel = originalLabel || input.value.trim();
      input.dataset.broadcastOrgOriginalLabel =
         cell.dataset.broadcastInlineEditLabel ?? "";
      hiddenId.dataset.broadcastOrgOriginalValue = originalId;

      const state = {
         timerId: null,
         requestId: 0,
         selectedIndex: -1,
         items: [],
         isOpen: false
      };

      const closeSuggestions = () => {
         if(state.timerId !== null)
         {
            window.clearTimeout(state.timerId);
            state.timerId = null;
         }

         state.requestId += 1;
         suggestions.hidden = true;
         suggestions.replaceChildren();
         state.items = [];
         state.selectedIndex = -1;
         state.isOpen = false;
      };

      const setLockedState = isLocked => {
         setBroadcastOrganizationLockState(container, isLocked);
      };

      const unlockForEdit = () => {
         closeSuggestions();
         setLockedState(false);
         window.requestAnimationFrame(() => {
            input.focus();
            input.select();
         });
      };

      setLockedState(true);

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
            state.isOpen = true;
            return;
         }

         items.forEach((item, index) => {
            const option = document.createElement("button");
            option.type = "button";
            option.className = "broadcast-org-entity-option";
            option.dataset.entityId = item.id;
            option.dataset.entityLabel = item.text;
            option.textContent = item.text;

            option.addEventListener("click", event => {
               event.preventDefault();
               event.stopPropagation();
               void selectSuggestion(item);
            });

            option.addEventListener("mousedown", event => {
               if(document.activeElement === input)
               {
                  event.preventDefault();
               }
            });

            option.addEventListener("mouseenter", () => {
               setActiveSuggestion(index);
            });

            suggestions.append(option);
         });

         suggestions.hidden = false;
         state.isOpen = true;
      };

      const setActiveSuggestion = index => {
         const options = Array.from(
            suggestions.querySelectorAll(".broadcast-org-entity-option")
         );

         if(options.length === 0)
         {
            return;
         }

         const normalizedIndex = Math.max(
            0,
            Math.min(index, options.length - 1)
         );
         state.selectedIndex = normalizedIndex;

         options.forEach((option, optionIndex) => {
            option.classList.toggle(
               "is-active",
               optionIndex === normalizedIndex
            );
         });
      };

      const selectSuggestion = async item => {
         if(!item || typeof item.id !== "string" || typeof item.text !== "string")
         {
            return;
         }

         const nextId = item.id.trim();
         const nextLabel = item.text.trim();

         if(nextId === "")
         {
            return;
         }

         input.value = nextLabel;
         hiddenId.value = nextId;
         closeSuggestions();

         await saveBroadcastOrganizationAsync(container);
      };

      const search = async query => {
         const trimmedQuery = query.trim();

         if(trimmedQuery === "")
         {
            closeSuggestions();
            return;
         }

         const requestId = ++state.requestId;
         const url = new URL(searchUrl, window.location.origin);
         url.searchParams.set("term", trimmedQuery);
         url.searchParams.set("organizationOnly", "true");

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
               throw new Error(
                  payload?.error ||
                     `Request failed with status ${response.status}`
               );
            }

            const results = Array.isArray(payload.results)
               ? payload.results
                  .map(item => normalizeOrgSearchResult(item))
                  .filter(item => item !== null)
               : [];

            renderSuggestions(results);
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
            void search(input.value);
         }, 180);
      };

      const revertToOriginal = () => {
         const originalValue = (
            hiddenId.dataset.broadcastOrgOriginalValue ?? originalId
         ).trim();
         const originalLabel = (
            input.dataset.broadcastOrgOriginalLabel ??
               cell.dataset.broadcastInlineEditLabel ??
               input.value
         ).trim();

         hiddenId.value = originalValue;
         input.value = originalLabel;
         cell.dataset.broadcastInlineEditValue = originalValue;
         cell.dataset.broadcastInlineEditLabel = originalLabel;
         setLockedState(true);
         closeSuggestions();
      };

      input.addEventListener("input", () => {
         if(input.readOnly)
         {
            return;
         }

         hiddenId.value = "";
         scheduleSearch();
      });

      input.addEventListener("focus", () => {
         if(!input.readOnly && input.value.trim() !== "")
         {
            scheduleSearch();
         }
      });

      input.addEventListener("keydown", event => {
         if(!state.isOpen)
         {
            return;
         }

         if(event.key === "ArrowDown")
         {
            event.preventDefault();
            setActiveSuggestion(state.selectedIndex + 1);
            return;
         }

         if(event.key === "ArrowUp")
         {
            event.preventDefault();
            setActiveSuggestion(state.selectedIndex - 1);
            return;
         }

         if(event.key === "Escape")
         {
            event.preventDefault();
            revertToOriginal();
            return;
         }

         if(event.key === "Enter")
         {
            event.preventDefault();
            const option = suggestions.querySelector(
               ".broadcast-org-entity-option.is-active"
            ) || suggestions.querySelector(".broadcast-org-entity-option");

            if(option instanceof HTMLElement)
            {
               const item = state.items.find(candidate =>
                  candidate.id === option.dataset.entityId
               );

               if(item)
               {
                  void selectSuggestion(item);
               }
            }
         }
      });

      input.addEventListener("blur", () => {
         window.setTimeout(() => {
            if(!container.contains(document.activeElement))
            {
               const currentValue = hiddenId.value.trim();
               const currentLabel = input.value.trim();

               if(currentValue === "")
               {
                  if(currentLabel === "")
                  {
                     closeSuggestions();

                     if(
                        (hiddenId.dataset.broadcastOrgOriginalValue ?? "")
                           .trim() !== ""
                     )
                     {
                        void saveBroadcastOrganizationAsync(container);
                     }
                     else
                     {
                        setLockedState(true);
                     }
                  }
                  else
                  {
                     revertToOriginal();
                  }
               }
               else
               {
                  closeSuggestions();
                  setLockedState(true);
               }
            }
         }, 120);
      });

      container.addEventListener("dblclick", event => {
         if(!(event.target instanceof Element))
         {
            return;
         }

         if(!event.target.closest(broadcastOrganizationInputSelector))
         {
            return;
         }

         if(!input.readOnly)
         {
            return;
         }

         event.preventDefault();
         event.stopPropagation();
         unlockForEdit();
      });

      container.addEventListener("mousedown", event => {
         if(!(event.target instanceof Element))
         {
            return;
         }

         const option = event.target.closest(".broadcast-org-entity-option");

         if(option instanceof HTMLElement && document.activeElement === input)
         {
            event.preventDefault();
         }
      });
   }

   function normalizeOrgSearchResult(item)
   {
      if(!(item && typeof item === "object"))
      {
         return null;
      }

      const id = typeof item.id === "string"
         ? item.id.trim()
         : typeof item.Id === "string"
            ? item.Id.trim()
            : "";
      const text = typeof item.text === "string"
         ? item.text.trim()
         : typeof item.name === "string"
            ? item.name.trim()
            : typeof item.Name === "string"
               ? item.Name.trim()
               : "";

      return id === "" || text === ""
         ? null
         : { id, text };
   }

   function getBroadcastOrganizationSearchUrl()
   {
      const container = document.querySelector(
         broadcastOrganizationSearchUrlSelector
      );

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.orgEntitySearchUrl;

      return typeof url === "string" ? url.trim() : "";
   }

   function setBroadcastOrganizationLockState(container, isLocked)
   {
      if(!(container instanceof HTMLElement))
      {
         return;
      }

      const input = container.querySelector(broadcastOrganizationInputSelector);
      const nextLocked = Boolean(isLocked);

      container.classList.toggle("is-locked", nextLocked);

      if(input instanceof HTMLInputElement)
      {
         input.readOnly = nextLocked;
         input.classList.toggle("is-locked", nextLocked);
         input.setAttribute("aria-disabled", String(nextLocked));
      }
   }

   async function saveBroadcastOrganizationAsync(container)
   {
      if(!(container instanceof HTMLElement))
      {
         return;
      }

      const cell = container.closest(broadcastInlineEditCellSelector);
      const input = container.querySelector(broadcastOrganizationInputSelector);
      const hiddenId = container.querySelector(broadcastOrganizationIdSelector);
      const url = getBroadcastInlineEditUrl();
      const broadcastId = (cell?.dataset.broadcastId ?? "").trim();
      const currentValue = (
         hiddenId instanceof HTMLInputElement ? hiddenId.value : ""
      ).trim();
      const originalValue = (
         hiddenId instanceof HTMLInputElement
            ? hiddenId.dataset.broadcastOrgOriginalValue
            : ""
      )?.trim() ?? "";

      if(!(cell instanceof HTMLElement)
         || !(input instanceof HTMLInputElement)
         || !(hiddenId instanceof HTMLInputElement)
         || url === ""
         || broadcastId === "")
      {
         return;
      }

      if(currentValue === originalValue)
      {
         return;
      }

      try
      {
         const payload = await postBroadcastInlineEditAsync(
            url,
            broadcastId,
            broadcastInlineEditOrganizationField,
            currentValue
         );

         updateBroadcastInlineEditCell(cell, payload);
         setBroadcastOrganizationLockState(container, true);
         hiddenId.dataset.broadcastOrgOriginalValue = currentValue;
         input.dataset.broadcastOrgOriginalLabel = input.value.trim();
         if(document.activeElement === input)
         {
            input.blur();
         }
      }
      catch(error)
      {
         window.alert(
            error instanceof Error
               ? error.message
               : "Broadcast update failed."
         );

         const originalLabel = (
            input.dataset.broadcastOrgOriginalLabel ??
               cell.dataset.broadcastInlineEditLabel ??
               ""
         ).trim();
         const originalId = (
            hiddenId.dataset.broadcastOrgOriginalValue ?? ""
         ).trim();

         input.value = originalLabel;
         hiddenId.value = originalId;
         cell.dataset.broadcastInlineEditValue = originalId;
         cell.dataset.broadcastInlineEditLabel = originalLabel;
         setBroadcastOrganizationLockState(container, true);
      }
   }

})();
