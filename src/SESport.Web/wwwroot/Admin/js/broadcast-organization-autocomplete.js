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
   let keyboardTabFocusActive = false;

   window.initializeBroadcastOrganizationAutocomplete =
      initializeBroadcastOrganizationAutocomplete;
   window.setBroadcastOrganizationLockState =
      setBroadcastOrganizationLockState;

   document.addEventListener("keydown", event => {
      if(event.key === "Tab")
      {
         keyboardTabFocusActive = true;
      }
   }, true);

   document.addEventListener("keyup", event => {
      if(event.key === "Tab")
      {
         window.requestAnimationFrame(() => {
            keyboardTabFocusActive = false;
         });
      }
   }, true);

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
      const originalLabel = (
         cell?.dataset.broadcastInlineEditLabel ?? ""
      ).trim();

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
      cell.dataset.broadcastInlineEditLabel =
         originalLabel || input.value.trim();
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

      suggestions.addEventListener("click", event => {
         const target = event.target;
         const option = target instanceof Element
            ? target.closest(".broadcast-org-entity-option")
            : null;

         if(!(option instanceof HTMLElement))
         {
            return;
         }

         event.preventDefault();
         event.stopPropagation();
         const item = state.items.find(candidate =>
            candidate.id === option.dataset.entityId
         );

         if(item)
         {
            void selectSuggestion(item);
         }
      });

      suggestions.addEventListener("mousedown", event => {
         const target = event.target;
         const option = target instanceof Element
            ? target.closest(".broadcast-org-entity-option")
            : null;

         if(option instanceof HTMLElement && document.activeElement === input)
         {
            event.preventDefault();
         }
      });

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

      const renderSuggestions = html => {
         window.replaceContentsWithPartialHtml(suggestions, html);
         state.items = Array.from(
            suggestions.querySelectorAll(".broadcast-org-entity-option")
         ).map(option => ({
            id: (option.dataset.entityId ?? "").trim(),
            text: (option.dataset.entityText ?? "").trim(),
            label: (option.dataset.entityLabel ?? "").trim()
         })).filter(item => item.id !== "" && item.text !== "");
         state.selectedIndex = -1;
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
         if(!item
            || typeof item.id !== "string"
            || typeof item.text !== "string")
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
         url.searchParams.set("format", "organization-suggestions");

         try
         {
            const html = await window.loadPartialAsync(url);

            if(requestId !== state.requestId)
            {
               return;
            }
            renderSuggestions(html);
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
         if(input.readOnly && keyboardTabFocusActive)
         {
            unlockForEdit();
            return;
         }

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

      const handleUnlockForEdit = event => {
         if(event.type === "click" && !window.isTouchEditInteraction?.())
         {
            return;
         }

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
      };

      container.addEventListener("dblclick", handleUnlockForEdit);
      container.addEventListener("click", handleUnlockForEdit);

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
      const sport = typeof item.sport === "string"
         ? item.sport.trim()
         : typeof item.Sport === "string"
            ? item.Sport.trim()
            : "";

      return id === "" || text === ""
         ? null
         : { id, text, sport };
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
         const rowHtml = await postBroadcastInlineEditAsync(
            url,
            broadcastId,
            broadcastInlineEditOrganizationField,
            currentValue
         );
         const rowContainer = cell.closest(
            "tbody[data-broadcast-container]"
         );

         if(!(rowContainer instanceof HTMLElement))
         {
            throw new Error("Broadcast container not found.");
         }

         const replacement = window.replaceElementWithPartialHtml(
            rowContainer,
            rowHtml
         );
         window.initializeBroadcastInlineEditing?.(replacement);
         window.initializeBroadcastOrganizationAutocomplete?.(replacement);
         window.initializeBroadcastActivityGroupAutocomplete?.(replacement);
         void window.initializeParticipationRunsAsync?.(replacement);
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
