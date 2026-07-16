(() => {
   const broadcastInlineEditUrlSelector =
      "[data-broadcast-inline-edit-url]";
   const broadcastResultsSelector = "[data-broadcast-results]";
   const broadcastInlineEditFieldSelector =
      "[data-broadcast-inline-edit-field]";
   const broadcastInlineEditTitleField = "title";
   const broadcastInlineEditCategoriesField = "categories";
   const broadcastInlineEditOrganizationField = "organization";
   const broadcastInlineEditGroupField = "group";
   const broadcastInlineEditInputSelector =
      "[data-broadcast-inline-edit-input]";
   const broadcastInlineEditDisplaySelector =
      "[data-broadcast-inline-edit-display]";
   const broadcastTitleTextSelector = "[data-broadcast-title-text]";
   const broadcastTitleSearchLinkSelector =
      "[data-broadcast-title-search-link]";
   const broadcastCategoriesListSelector =
      "[data-broadcast-categories-list]";
   const broadcastGroupTextSelector = "[data-broadcast-group-text]";
   const broadcastOrganizationInputSelector = "[data-org-entity-input]";
   const broadcastOrganizationIdSelector = "[data-org-entity-id]";

   window.getBroadcastInlineEditUrl = getBroadcastInlineEditUrl;
   window.postBroadcastInlineEditAsync = postBroadcastInlineEditAsync;
   window.updateBroadcastInlineEditCell = updateBroadcastInlineEditCell;
   window.renderBroadcastCategories = renderBroadcastCategories;
   window.getBroadcastSearchUrlBase = getBroadcastSearchUrlBase;
   window.getAntiForgeryToken = getAntiForgeryToken;

   function getBroadcastInlineEditUrl()
   {
      const container = document.querySelector(broadcastInlineEditUrlSelector);

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.broadcastInlineEditUrl;

      return typeof url === "string" ? url.trim() : "";
   }

   async function postBroadcastInlineEditAsync(
      url,
      broadcastId,
      field,
      value
   )
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      formData.append("id", broadcastId);
      formData.append("field", field);
      formData.append("value", value);

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

   function updateBroadcastInlineEditCell(cell, payload)
   {
      if(!(cell instanceof HTMLElement) || !payload)
      {
         return;
      }

      const field = typeof payload.field === "string"
         ? payload.field.trim()
         : "";

      if(field === broadcastInlineEditTitleField)
      {
         const nextValue = typeof payload.value === "string"
            ? payload.value.trim()
            : "";

         if(nextValue === "")
         {
            return;
         }

         cell.dataset.broadcastInlineEditValue = nextValue;
         const input = cell.querySelector(
            broadcastInlineEditInputSelector
         );

         if(input instanceof HTMLInputElement)
         {
            input.value = nextValue;
            input.dataset.broadcastInlineEditOriginalValue = nextValue;
         }

         const titleText = cell.querySelector(broadcastTitleTextSelector);

         if(titleText instanceof HTMLElement)
         {
            titleText.textContent = nextValue;
         }

         const searchLink = cell.querySelector(
            broadcastTitleSearchLinkSelector
         );
         const searchUrlBase = getBroadcastSearchUrlBase();

         if(searchLink instanceof HTMLAnchorElement &&
            searchUrlBase !== "")
         {
            searchLink.href = `${searchUrlBase}${
               encodeURIComponent(nextValue)
            }`;
         }

         return;
      }

      if(field === broadcastInlineEditCategoriesField)
      {
         const categories = normalizeBroadcastCategories(payload.value);
         const list = cell.querySelector(broadcastCategoriesListSelector);
         const input = cell.querySelector(broadcastInlineEditInputSelector);

         cell.dataset.broadcastInlineEditValue = categories.join(", ");
         cell.dataset.broadcastCategoriesJson = JSON.stringify(categories);

         if(input instanceof HTMLInputElement)
         {
            input.value = categories.join(", ");
            input.dataset.broadcastInlineEditOriginalValue =
               input.value;
         }

         if(!(list instanceof HTMLElement))
         {
            return;
         }

         renderBroadcastCategories(list, categories);
         return;
      }

      if(field === broadcastInlineEditOrganizationField)
      {
         const nextValue = typeof payload.value === "string"
            ? payload.value.trim()
            : "";
         const input = cell.querySelector(broadcastOrganizationInputSelector);
         const hiddenId = cell.querySelector(broadcastOrganizationIdSelector);

         cell.dataset.broadcastInlineEditValue = nextValue;

         if(hiddenId instanceof HTMLInputElement)
         {
            hiddenId.value = nextValue;
            hiddenId.dataset.broadcastOrgOriginalValue = nextValue;
         }

         if(input instanceof HTMLInputElement)
         {
            input.dataset.broadcastOrgOriginalLabel = input.value.trim();
         }

         window.setBroadcastOrganizationLockState?.(cell, nextValue !== "");
         updateBroadcastGroupCell(cell, payload);
         return;
      }

      if(field === broadcastInlineEditGroupField)
      {
         updateBroadcastGroupCell(cell, payload);
      }
   }

   function updateBroadcastGroupCell(cell, payload)
   {
      if(!(cell instanceof HTMLElement) || !payload)
      {
         return;
      }

      const row = cell.closest("tr[data-broadcast-row='true']");
      const groupCell = row?.querySelector(broadcastGroupTextSelector);

      if(!(groupCell instanceof HTMLElement))
      {
         return;
      }

      const activityGroupId = typeof payload.activityGroupId === "string"
         ? payload.activityGroupId.trim()
         : "";
      const activityGroupTitle = typeof payload.activityGroupTitle === "string"
         ? payload.activityGroupTitle.trim()
         : "";
      const activityGroupDraftTitle =
         typeof payload.activityGroupDraftTitle === "string"
            ? payload.activityGroupDraftTitle.trim()
            : "";
      const activityGroupSourceKindId = typeof payload.activityGroupSourceKindId
         === "string"
         ? payload.activityGroupSourceKindId.trim()
         : "";
      const payloadValue = typeof payload.value === "string"
         ? payload.value.trim()
         : "";
      const groupValue = typeof payload.groupValue === "string"
         ? payload.groupValue.trim()
         : "";
      const groupText = typeof payload.groupText === "string"
         ? payload.groupText.trim()
         : "";
      const editableValue = groupValue
         || activityGroupTitle
         || activityGroupDraftTitle
         || payloadValue
         || groupText;
      const displayValue = groupText || (
         activityGroupSourceKindId !== ""
            ? (activityGroupId !== ""
               ? editableValue
               : `NEW: ${editableValue}`)
            : "-"
      );

      syncBroadcastGroupCell(
         groupCell,
         displayValue,
         editableValue,
         activityGroupId,
         activityGroupSourceKindId
      );

      if(activityGroupId === "")
      {
         return;
      }

      document
         .querySelectorAll(broadcastGroupTextSelector)
         .forEach(otherCell => {
            if(!(otherCell instanceof HTMLElement))
            {
               return;
            }

            if((otherCell.dataset.broadcastActivityGroupId ?? "").trim() !==
               activityGroupId)
            {
               return;
            }

            syncBroadcastGroupCell(
               otherCell,
               displayValue,
               editableValue,
               activityGroupId,
               activityGroupSourceKindId
            );
         });
   }

   function syncBroadcastGroupCell(
      cell,
      displayValue,
      editableValue,
      activityGroupId,
      activityGroupSourceKindId
   )
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const sourceKindId = typeof activityGroupSourceKindId === "string"
         ? activityGroupSourceKindId.trim()
         : "";
      const editable = sourceKindId !== "";
      const nextDisplayValue = typeof displayValue === "string"
         ? displayValue.trim()
         : "";
      const nextEditableValue = typeof editableValue === "string"
         ? editableValue.trim()
         : "";
      const nextActivityGroupId = typeof activityGroupId === "string"
         ? activityGroupId.trim()
         : "";

      cell.dataset.broadcastGroupText = nextDisplayValue;

      if(!editable)
      {
         cell.classList.remove("broadcast-inline-editable");
         delete cell.dataset.broadcastInlineEditField;
         delete cell.dataset.broadcastInlineEditValue;
         delete cell.dataset.broadcastActivityGroupId;
         delete cell.dataset.broadcastActivityGroupSourceKindId;
         cell.title = nextDisplayValue;
         cell.textContent = nextDisplayValue;
         return;
      }

      cell.classList.add("broadcast-inline-editable");
      const broadcastRow = cell.closest("tr[data-broadcast-row='true']");
      const broadcastId = (
         cell.dataset.broadcastId
         ?? broadcastRow?.dataset.broadcastId
         ?? ""
      ).trim();

      if(broadcastId !== "")
      {
         cell.dataset.broadcastId = broadcastId;
      }

      cell.dataset.broadcastInlineEditField = broadcastInlineEditGroupField;
      cell.dataset.broadcastInlineEditValue = nextEditableValue;
      cell.dataset.broadcastActivityGroupSourceKindId = sourceKindId;

      if(nextActivityGroupId !== "")
      {
         cell.dataset.broadcastActivityGroupId = nextActivityGroupId;
      }
      else
      {
         delete cell.dataset.broadcastActivityGroupId;
      }

      cell.title = "Double-click to edit";

      let display = cell.querySelector(broadcastInlineEditDisplaySelector);
      let input = cell.querySelector(broadcastInlineEditInputSelector);

      if(!(display instanceof HTMLElement) ||
         !(input instanceof HTMLInputElement))
      {
         cell.replaceChildren();

         display = document.createElement("div");
         display.dataset.broadcastInlineEditDisplay = "true";

         input = document.createElement("input");
         input.className = "broadcast-inline-edit-input";
         input.dataset.broadcastInlineEditInput = "true";
         input.type = "text";
         input.autocomplete = "off";
         input.spellcheck = false;
         input.setAttribute("aria-label", "Edit group title");
         input.hidden = true;
         input.tabIndex = -1;

         cell.append(display, input);
      }

      display.textContent = nextDisplayValue;
      display.hidden = false;
      input.value = nextEditableValue;
      input.dataset.broadcastInlineEditOriginalValue = nextEditableValue;
      input.hidden = true;
      input.disabled = false;

      window.initializeBroadcastInlineEditing?.(cell);
   }

   function getBroadcastSearchUrlBase()
   {
      const container = document.querySelector(broadcastResultsSelector);

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.searchUrlBase;

      return typeof url === "string" ? url.trim() : "";
   }

   function renderBroadcastCategories(list, categories)
   {
      if(!(list instanceof HTMLElement))
      {
         return;
      }

      const items = normalizeBroadcastCategories(categories);

      list.replaceChildren();

      items.forEach(category => {
         const span = document.createElement("span");
         span.textContent = category;
         list.append(span);
      });
   }

   function normalizeBroadcastCategories(categories)
   {
      if(Array.isArray(categories))
      {
         return categories
            .map(item => typeof item === "string" ? item.trim() : "")
            .filter(item => item !== "");
      }

      if(typeof categories === "string")
      {
         const trimmed = categories.trim();

         if(trimmed === "")
         {
            return [];
         }

         try
         {
            const parsed = JSON.parse(trimmed);

            if(Array.isArray(parsed))
            {
               return parsed
                  .map(item => typeof item === "string" ? item.trim() : "")
                  .filter(item => item !== "");
            }
         }
         catch
         {
            // Fall back to the legacy comma-separated representation.
         }

         return trimmed
            .split(",")
            .map(item => item.trim())
            .filter(item => item !== "");
      }

      return [];
   }

   function getAntiForgeryToken()
   {
      const tokenField = document.querySelector(
         "input[name='__RequestVerificationToken']"
      );

      if(tokenField instanceof HTMLInputElement)
      {
         return tokenField.value;
      }

      return "";
   }
})();
