(() => {
   const enhancedFormSelector =
      "form[data-ajax-success]:not([data-ajax-success=''])";
   const replacementFormSelector = "form[data-ajax-replace-target]";
   const checkboxToggleSelector = "[data-checkbox-toggle]";
   const checkboxVisibilitySelector = "[data-visible-when-checkbox-group]";
   const entityTypeSelectSelector = "[data-entity-type-select]";
   const personGenderFieldSelector = "[data-person-gender-field]";
   const entityInlineEditUrlSelector = "[data-entity-inline-edit-url]";
   const entityInlineEditCellSelector =
      "[data-entity-inline-edit-field]";
   const entityInlineEditDisplaySelector =
      "[data-entity-inline-edit-display]";
   const entityInlineEditInputSelector =
      "[data-entity-inline-edit-input]";
   const generateTeaserSelector = "[data-generate-teaser]";
   const findFactsSelector = "[data-find-facts]";
   const activityFactsCheckSelector =
      "[data-activity-facts-check]";
   const checkParticipationRowSelector =
      "[data-check-participation-row]";
   const participationRunsToggleSelector =
      "[data-participation-runs-toggle]";
   const participationCellSelector = "[data-participation-cell]";
   const participantCreateUrlSelector = "[data-create-participant-url]";
   const participationStatusUrlSelector =
      "[data-check-participation-status-url]";
   const runStatusesUrlSelector = "[data-run-statuses-url]";
   const runInlineEditUrlSelector = "[data-run-inline-edit-url]";
   const runRowSelector = "[data-ai-run-id]";
   const runStatusCellSelector = "[data-ai-run-status-cell]";
   const runStatusTextSelector = "[data-ai-run-status-text]";
   const runSummaryCellSelector = "[data-ai-run-summary-cell]";
   const activityFactsCheckStatusSelector =
      "[data-facts-check-status]";
   const runPayloadCellSelector = "[data-ai-run-payload-cell]";
   const runRoundsCellSelector = "[data-ai-run-rounds-cell]";
   const runDurationCellSelector = "[data-ai-run-duration-cell]";
   const runInlineEditCellSelector = "[data-run-inline-edit-field]";
   const runInlineEditDisplaySelector = "[data-run-inline-edit-display]";
   const runInlineEditInputSelector = "[data-run-inline-edit-input]";
   const runInlineEditField = "execution-environment";
   const currentMarkerSelector = "#activity-now-marker";
   const broadcastInlineEditCellSelector =
      "[data-broadcast-inline-edit-field]";
   const broadcastInlineEditUrlSelector =
      "[data-broadcast-inline-edit-url]";
   const broadcastCategoriesListSelector =
      "[data-broadcast-categories-list]";
   const broadcastResultsSelector = "[data-broadcast-results]";
   const broadcastRowSelector = "tr[data-broadcast-row='true']";
   const broadcastRunsRowSelector =
      ".broadcast-participation-runs-row";
   const broadcastInlineEditTitleField = "title";
   const broadcastInlineEditCategoriesField = "categories";
   const broadcastInlineEditOrganizationField = "organization";
   const broadcastInlineEditGroupField = "group";
   const getBroadcastInlineEditUrl =
      window.getBroadcastInlineEditUrl;
   const postBroadcastInlineEditAsync =
      window.postBroadcastInlineEditAsync;
   const updateBroadcastInlineEditCell =
      window.updateBroadcastInlineEditCell;
   const renderBroadcastCategories =
      window.renderBroadcastCategories;
   const getBroadcastSearchUrlBase =
      window.getBroadcastSearchUrlBase;
   const getAntiForgeryToken = window.getAntiForgeryToken;
   const pendingParticipationIds = new Set();
   const pendingRunIds = new Set();
   let participationPollingTimer = null;
   let participationPollingInFlight = false;
   let runPollingTimer = null;
   let runPollingInFlight = false;
   const getFormSelector = "form[method='get']";
   const exclusiveEmptySelectSelector = "select[data-empty-option='exclusive']";
   const dateSelectSelector = "#date-select-input";
   const exclusiveEmptySelectStates = new WeakMap();
   const multiSelectScrollPositions = new WeakMap();
   window.submitFilterForm = submitFilterForm;
   window.isTouchEditInteraction = isTouchEditInteraction;
   initializeExclusiveEmptySelects();
   initializeMultiSelectScrollRetention();
   initializeMultiSelectClearButtons();
   initializeCheckboxToggles();
   initializeCheckboxVisibility();
   window.initializeEntitySearch?.(document);
   initializePersonGenderVisibility();
   initializeGetFormRestoration();
   initializeDateSelect();
   initializeEntityInlineEditing();
   window.initializeEntityInlineEditing = initializeEntityInlineEditing;
   window.initializeBroadcastInlineEditing =
      initializeBroadcastInlineEditing;
   initializeTeaserGeneration();
   initializeActivityFactsChecks();
   initializeParticipationRowChecks();
   void initializeParticipationRunsAsync();
   initializeBroadcastInlineEditing();
   if(typeof window.initializeBroadcastOrganizationAutocomplete === "function")
   {
      window.initializeBroadcastOrganizationAutocomplete();
   }
   initializeParticipationPolling();
   initializeRunPolling();
   initializeRunInlineEditing();
   initializeCurrentMarkerScroll();

   document.addEventListener("submit", async event => {
      const form = event.target;

      if (!(form instanceof HTMLFormElement)
         || !form.matches(enhancedFormSelector))
      {
         return;
      }

      event.preventDefault();

      const submitButton = form.querySelector("[type='submit']");

      if(submitButton instanceof HTMLButtonElement)
      {
         submitButton.disabled = true;
      }

      try
      {
         const response = await fetch(form.action, {
            method: form.method || "post",
            body: new FormData(form),
            headers: {
               Accept: "application/json"
            }
         });

         if(!response.ok)
         {
            throw new Error(`Request failed with status ${response.status}`);
         }

         if(form.dataset.ajaxSuccess === "remove")
         {
            const targetSelector = form.dataset.ajaxRemoveTarget || "tr";
            const target = form.closest(targetSelector);
            const preserveScroll =
               form.dataset.ajaxPreserveScroll === "true";
            const scrollX = preserveScroll ? window.scrollX : 0;
            const scrollY = preserveScroll ? window.scrollY : 0;

            if(target)
            {
              removeBroadcastRunRowIfNeeded(target);
              target.remove();

               if(preserveScroll)
               {
                  window.requestAnimationFrame(() => {
                     window.scrollTo(scrollX, scrollY);
                  });
               }
            }
         }
         else if(form.dataset.ajaxSuccess === "toggle-visibility")
         {
            await updateBroadcastVisibilityAsync(form, response);
         }
         else if(form.dataset.ajaxSuccess === "reload")
         {
            window.location.reload();
            return;
         }
         else if(form.dataset.ajaxSuccess === "replace")
         {
            await replaceParticipantCreateFormAsync(form, response);
         }
         else if(form.dataset.ajaxSuccess === "replace-target")
         {
            await replaceTargetFromFormAsync(form);
         }
         else if(form.dataset.ajaxSuccess === "update-participation")
         {
            await updateParticipationFromResponseAsync(response);
         }

         decrementCounter(form.dataset.ajaxDecrementTarget);
         refreshCheckboxControls();
      }
      catch(error)
      {
         if(form.dataset.ajaxSuccess === "update-participation")
         {
            console.error(error);
            return;
         }

         HTMLFormElement.prototype.submit.call(form);
      }
      finally
      {
         if(submitButton instanceof HTMLButtonElement)
         {
            submitButton.disabled = false;
         }
      }
   });

   document.addEventListener("submit", async event => {
      const form = event.target;

      if(!(form instanceof HTMLFormElement)
         || !form.matches(replacementFormSelector))
      {
         return;
      }

      event.preventDefault();
      await replaceFromFormAsync(form);
   });

   async function updateParticipationFromResponseAsync(response)
   {
      if(!(response instanceof Response))
      {
         return;
      }

      let payload = null;

      try
      {
         payload = await response.clone().json();
      }
      catch
      {
         return;
      }

      if(Array.isArray(payload?.results))
      {
         payload.results.forEach(updateParticipationCellByResult);
         return;
      }

      if(payload?.result)
      {
         updateParticipationCellByResult(payload.result);
      }
   }

   function decrementCounter(selector)
   {
      if(!selector)
      {
         return;
      }

      const counter = document.querySelector(selector);
      const currentValue = Number.parseInt(counter?.textContent ?? "", 10);

      if(!counter || Number.isNaN(currentValue))
      {
         return;
      }

      counter.textContent = Math.max(0, currentValue - 1).toString();
   }

   function removeBroadcastRunRowIfNeeded(target)
   {
      if(!(target instanceof HTMLElement))
      {
         return;
      }

      const broadcastId = typeof target.dataset.broadcastId === "string"
         ? target.dataset.broadcastId.trim()
         : "";

      if(broadcastId === "" ||
         !target.matches(".broadcast-participation-main-row"))
      {
         return;
      }

      const nextRow = target.nextElementSibling;

      if(!(nextRow instanceof HTMLElement)
         || !nextRow.matches(".broadcast-participation-runs-row")
         || nextRow.dataset.broadcastId !== broadcastId)
      {
         return;
      }

      nextRow.remove();
   }

   async function updateBroadcastVisibilityAsync(form, response)
   {
      if(!(form instanceof HTMLFormElement)
         || !(response instanceof Response))
      {
         return;
      }

      let payload = null;

      try
      {
         payload = await response.clone().json();
      }
      catch
      {
         return;
      }

      const hidden = typeof payload?.hidden === "boolean"
         ? payload.hidden
         : null;

      if(hidden === null)
      {
         return;
      }

      const target = form.closest(broadcastRowSelector);
      const showHidden = isBroadcastShowHiddenEnabled(form);
      const preserveScroll = form.dataset.ajaxPreserveScroll === "true";
      const scrollX = preserveScroll ? window.scrollX : 0;
      const scrollY = preserveScroll ? window.scrollY : 0;
      const rowPayload = await fetchBroadcastRowAsync(form);

      if(hidden && !showHidden)
      {
         if(target instanceof HTMLElement)
         {
            removeBroadcastRowPair(target);
         }

         if(preserveScroll)
         {
            window.requestAnimationFrame(() => {
               window.scrollTo(scrollX, scrollY);
            });
         }

         return;
      }

      if(!rowPayload)
      {
         return;
      }

      const nextRows = createBroadcastRows(rowPayload, form, target);
      const nextMainRow = nextRows?.firstElementChild;
      const nextRunsRow = nextMainRow?.nextElementSibling;

      if(!(nextRows instanceof DocumentFragment)
         || !(nextMainRow instanceof HTMLElement)
         || !(nextRunsRow instanceof HTMLElement))
      {
         return;
      }

      replaceBroadcastRowPair(target, nextMainRow, nextRunsRow);

      initializeBroadcastInlineEditing(nextMainRow);
      window.initializeBroadcastOrganizationAutocomplete?.(nextMainRow);
      initializeParticipationRunsAsync(nextRunsRow);
      initializeParticipationPolling(nextRunsRow);

      if(preserveScroll)
      {
         window.requestAnimationFrame(() => {
            window.scrollTo(scrollX, scrollY);
         });
      }
   }

   async function fetchBroadcastRowAsync(form)
   {
      const url = getBroadcastRowUrl();

      if(!(form instanceof HTMLFormElement) || url === "")
      {
         return null;
      }

      try
      {
         const response = await fetch(url, {
            method: "post",
            body: new FormData(form),
            headers: {
               Accept: "application/json"
            }
         });

         if(!response.ok)
         {
            throw new Error(`Request failed with status ${response.status}`);
         }

         const payload = await response.json();
         return payload?.broadcast ?? null;
      }
      catch
      {
         return null;
      }
   }

   function getBroadcastRowUrl()
   {
      const container = document.querySelector(broadcastResultsSelector);

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.broadcastRowUrl;

      return typeof url === "string" ? url.trim() : "";
   }

   function isBroadcastShowHiddenEnabled(form)
   {
      if(!(form instanceof HTMLFormElement))
      {
         return false;
      }

      const input = form.querySelector("input[name='ShowHidden']");

      return input instanceof HTMLInputElement && input.checked;
   }

   function getBroadcastVisibilityLabels()
   {
      const container = document.querySelector(broadcastResultsSelector);

      if(!(container instanceof HTMLElement))
      {
         return {
            show: "",
            hide: "",
            check: ""
         };
      }

      return {
         show: container.dataset.broadcastShowLabel || "",
         hide: container.dataset.broadcastHideLabel || "",
         check: container.dataset.broadcastCheckLabel || ""
      };
   }

   function removeBroadcastRowPair(target)
   {
      if(!(target instanceof HTMLElement))
      {
         return;
      }

      const nextRow = getBroadcastRunsRow(target);

      if(nextRow instanceof HTMLElement)
      {
         nextRow.remove();
      }

      const broadcastId = (target.dataset.broadcastId ?? "").trim();
      target.remove();

      if(broadcastId !== "")
      {
         pendingParticipationIds.delete(broadcastId);

         if(pendingParticipationIds.size === 0)
         {
            stopParticipationPolling();
         }
      }
   }

   function replaceBroadcastRowPair(target, nextMainRow, nextRunsRow)
   {
      if(!(target instanceof HTMLElement)
         || !(nextMainRow instanceof HTMLElement)
         || !(nextRunsRow instanceof HTMLElement))
      {
         return;
      }

      const currentRunsRow = getBroadcastRunsRow(target);

      target.before(nextMainRow, nextRunsRow);

      if(currentRunsRow instanceof HTMLElement)
      {
         currentRunsRow.remove();
      }

      target.remove();
   }

   function getBroadcastRunsRow(target)
   {
      if(!(target instanceof HTMLElement))
      {
         return null;
      }

      const broadcastId = (target.dataset.broadcastId ?? "").trim();
      const nextRow = target.nextElementSibling;

      if(broadcastId === "" || !(nextRow instanceof HTMLElement))
      {
         return null;
      }

      if(!nextRow.matches(broadcastRunsRowSelector)
         || (nextRow.dataset.broadcastId ?? "").trim() !== broadcastId)
      {
         return null;
      }

      return nextRow;
   }

   function createBroadcastRows(broadcast, form, target)
   {
      if(!(broadcast && typeof broadcast === "object"))
      {
         return null;
      }

      const fragment = document.createDocumentFragment();
      const labels = getBroadcastVisibilityLabels();
      const sourceForm = form instanceof HTMLFormElement ? form : null;
      const sourceRunsRow = getBroadcastRunsRow(target);
      const sourceRunsCell =
         sourceRunsRow?.querySelector(participationCellSelector) ?? null;
      const searchUrlBase = getBroadcastSearchUrlBase();
      const showHidden = isBroadcastShowHiddenEnabled(form);
      const broadcastId = normalizeString(broadcast.id);
      const title = normalizeString(broadcast.title);
      const timeOnlyText = normalizeString(broadcast.timeOnlyText);
      const channelName = normalizeString(broadcast.channelName);
      const description = normalizeNullableString(broadcast.description);
      const categories = normalizeBroadcastCategories(broadcast.categories);
      const originalAirDate = normalizeNullableString(
         broadcast.originalAirDate
      );
      const organizationEntityId = normalizeNullableString(
         broadcast.organizationEntityId
      );
      const organizationEntityName = normalizeNullableString(
         broadcast.organizationEntityName
      );
      const activityGroupId = normalizeNullableString(
         broadcast.activityGroupId
      );
      const activityGroupTitle = normalizeNullableString(
         broadcast.activityGroupTitle
      );
      const activityGroupDraftTitle = normalizeNullableString(
         broadcast.activityGroupDraftTitle
      );
      const activityGroupSourceKindId = normalizeNullableString(
         broadcast.activityGroupSourceKindId
      );
      const groupValue = normalizeNullableString(broadcast.groupValue)
         || activityGroupTitle
         || activityGroupDraftTitle
         || title;
      const groupText = normalizeNullableString(broadcast.groupText) || "-";
      const participationStatusId = normalizeNullableString(
         broadcast.participationStatusId
      );
      const isHidden = Boolean(broadcast.isHidden);
      const isReplay = Boolean(broadcast.isReplay);
      const activityUrlBase = sourceRunsCell instanceof HTMLElement
         ? (sourceRunsCell.dataset.activityUrlBase ?? "").trim()
         : "";
      const checkParticipationUrl = sourceRunsCell instanceof HTMLElement
         ? (sourceRunsCell.dataset.checkParticipationUrl ?? "").trim()
         : "";

      if(broadcastId === "" || title === "" || timeOnlyText === "")
      {
         return null;
      }

      const mainRow = document.createElement("tr");
      mainRow.className = "broadcast-participation-main-row";
      mainRow.dataset.broadcastRow = "true";
      mainRow.dataset.broadcastId = broadcastId;
      mainRow.dataset.participationStatus = participationStatusId || "";

      const channelCell = document.createElement("td");
      channelCell.className = "broadcasts-col-channel";
      const channelDiv = document.createElement("div");
      channelDiv.className = "ses-nowrap";
      channelDiv.textContent = channelName;
      channelCell.append(channelDiv);

      const timeCell = document.createElement("td");
      timeCell.className = "broadcasts-col-time";
      const timeStrong = document.createElement("strong");
      timeStrong.className = "ses-nowrap";
      timeStrong.textContent = timeOnlyText;
      timeCell.append(timeStrong);

      const titleCell = document.createElement("td");
      titleCell.className =
         "broadcasts-col-broadcast broadcast-inline-editable";
      titleCell.dataset.broadcastId = broadcastId;
      titleCell.dataset.broadcastInlineEditField = "title";
      titleCell.dataset.broadcastInlineEditValue = title;
      titleCell.title = "Double-click to edit";
      const titleDisplay = document.createElement("div");
      titleDisplay.dataset.broadcastInlineEditDisplay = "true";
      const titleStrong = document.createElement("strong");
      titleStrong.dataset.broadcastTitleText = "true";
      titleStrong.textContent = title;
      titleDisplay.append(titleStrong);

      const searchLink = document.createElement("a");
      searchLink.className = "ses-entity-search-link";
      searchLink.dataset.broadcastTitleSearchLink = "true";
      searchLink.target = "_blank";
      searchLink.href = searchUrlBase === ""
         ? ""
         : `${searchUrlBase}${encodeURIComponent(title)}`;
      searchLink.tabIndex = -1;
      const searchIcon = document.createElement("span");
      searchIcon.className = "ses-icon-search";
      searchLink.append(searchIcon);
      titleDisplay.append(searchLink);

      if(description !== "")
      {
         const descriptionSpan = document.createElement("span");
         descriptionSpan.textContent = description;
         titleDisplay.append(descriptionSpan);
      }

      if(isReplay && originalAirDate !== "")
      {
         const replaySpan = document.createElement("span");
         replaySpan.textContent = `Repris från ${originalAirDate}`;
         titleDisplay.append(replaySpan);
      }

      const titleInput = document.createElement("input");
      titleInput.className = "broadcast-inline-edit-input";
      titleInput.dataset.broadcastInlineEditInput = "true";
      titleInput.type = "text";
      titleInput.value = title;
      titleInput.autocomplete = "off";
      titleInput.spellcheck = false;
      titleInput.setAttribute("aria-label", "Edit broadcast title");
      titleInput.hidden = true;
      titleInput.tabIndex = -1;

      titleCell.append(titleDisplay, titleInput);

      const organizationCell = document.createElement("td");
      organizationCell.className =
         "broadcasts-col-organization broadcast-inline-editable";
      organizationCell.dataset.broadcastId = broadcastId;
      organizationCell.dataset.broadcastInlineEditField = "organization";
      organizationCell.dataset.broadcastInlineEditValue =
         organizationEntityId || "";
      organizationCell.dataset.broadcastInlineEditLabel =
         organizationEntityName || "";
      organizationCell.title = "Double-click to edit";
      const organizationWrap = document.createElement("div");
      organizationWrap.className = "broadcast-org-autocomplete is-locked";
      organizationWrap.dataset.orgEntityAutocomplete = "true";
      const organizationInput = document.createElement("input");
      organizationInput.className = "broadcast-org-entity-input";
      organizationInput.type = "text";
      organizationInput.setAttribute("aria-label", "Organization entity");
      organizationInput.dataset.orgEntityInput = "true";
      organizationInput.autocomplete = "off";
      organizationInput.spellcheck = false;
      organizationInput.readOnly = true;
      organizationInput.value = organizationEntityName || "";
      organizationInput.tabIndex = 0;
      const organizationHidden = document.createElement("input");
      organizationHidden.type = "hidden";
      organizationHidden.dataset.orgEntityId = "true";
      organizationHidden.value = organizationEntityId || "";
      const organizationSuggestions = document.createElement("div");
      organizationSuggestions.className =
         "broadcast-org-entity-suggestions";
      organizationSuggestions.dataset.orgEntitySuggestions = "true";
      organizationSuggestions.hidden = true;
      organizationWrap.append(
         organizationInput,
         organizationHidden,
         organizationSuggestions
      );
      organizationCell.append(organizationWrap);

      const groupCell = document.createElement("td");
      const groupEditable = activityGroupSourceKindId !== "";

      groupCell.className = groupEditable
         ? "broadcasts-col-group broadcast-inline-editable"
         : "broadcasts-col-group";
      groupCell.dataset.broadcastGroupText = groupText;

      if(groupEditable)
      {
         groupCell.dataset.broadcastId = broadcastId;
         groupCell.dataset.broadcastInlineEditField =
            broadcastInlineEditGroupField;
         groupCell.dataset.broadcastInlineEditValue = groupValue;
         groupCell.dataset.broadcastActivityGroupSourceKindId =
            activityGroupSourceKindId;
         groupCell.dataset.broadcastActivityGroupId = activityGroupId;
         groupCell.title = "Double-click to edit";

         const groupDisplay = document.createElement("div");
         groupDisplay.dataset.broadcastInlineEditDisplay = "true";
         groupDisplay.textContent = groupText;

         const groupInput = document.createElement("input");
         groupInput.className = "broadcast-inline-edit-input";
         groupInput.dataset.broadcastInlineEditInput = "true";
         groupInput.type = "text";
         groupInput.value = groupValue;
         groupInput.autocomplete = "off";
         groupInput.spellcheck = false;
         groupInput.setAttribute("aria-label", "Edit group title");
         groupInput.hidden = true;
         groupInput.tabIndex = -1;

         groupCell.append(groupDisplay, groupInput);
      }
      else
      {
         groupCell.title = groupText;
         groupCell.textContent = groupText;
      }

      const categoriesCell = document.createElement("td");
      categoriesCell.className =
         "broadcasts-col-categories broadcast-inline-editable";
      categoriesCell.dataset.broadcastId = broadcastId;
      categoriesCell.dataset.broadcastInlineEditField = "categories";
      categoriesCell.dataset.broadcastInlineEditValue =
         categories.join(", ");
      categoriesCell.dataset.broadcastCategoriesJson =
         JSON.stringify(categories);
      categoriesCell.title = "Double-click to edit";
      const categoriesDisplay = document.createElement("div");
      categoriesDisplay.dataset.broadcastInlineEditDisplay = "true";
      const categoriesList = document.createElement("div");
      categoriesList.className = "broadcast-categories-list";
      categoriesList.dataset.broadcastCategoriesList = "true";
      if(typeof renderBroadcastCategories === "function")
      {
         renderBroadcastCategories(categoriesList, categories);
      }
      categoriesDisplay.append(categoriesList);
      const categoriesInput = document.createElement("input");
      categoriesInput.className = "broadcast-inline-edit-input";
      categoriesInput.dataset.broadcastInlineEditInput = "true";
      categoriesInput.type = "text";
      categoriesInput.value = categories.join(", ");
      categoriesInput.autocomplete = "off";
      categoriesInput.spellcheck = false;
      categoriesInput.setAttribute("aria-label", "Edit categories");
      categoriesInput.hidden = true;
      categoriesInput.tabIndex = -1;
      categoriesCell.append(categoriesDisplay, categoriesInput);

      const actionsCell = document.createElement("td");
      actionsCell.className = "broadcasts-col-actions table-actions";
      const actionsStack = document.createElement("div");
      actionsStack.className = "table-actions-stack";

      if((participationStatusId || "").toLowerCase() !== "running"
         && checkParticipationUrl !== "")
      {
         const checkButton = document.createElement("button");
         checkButton.type = "button";
         checkButton.className = "broadcast-participation-check-link";
         checkButton.dataset.checkParticipationRow = "true";
         checkButton.dataset.checkParticipationUrl = checkParticipationUrl;
         checkButton.dataset.broadcastId = broadcastId;
         checkButton.textContent = labels.check;
         checkButton.tabIndex = -1;
         actionsStack.append(checkButton);
      }

      const visibilityForm = document.createElement("form");
      visibilityForm.method = "post";
      visibilityForm.action = sourceForm?.action ?? "";
      visibilityForm.dataset.ajaxSuccess = "toggle-visibility";
      visibilityForm.dataset.ajaxRemoveTarget = "tr";
      visibilityForm.dataset.ajaxPreserveScroll = "true";
      appendHiddenBroadcastVisibilityFields(
         visibilityForm,
         sourceForm,
         broadcastId,
         isHidden,
         showHidden
      );

      const visibilityButton = document.createElement("button");
      visibilityButton.type = "submit";
      visibilityButton.textContent = isHidden
         ? labels.show
         : labels.hide;
      visibilityButton.tabIndex = -1;
      visibilityForm.append(visibilityButton);
      actionsStack.append(visibilityForm);
      actionsCell.append(actionsStack);

      setBroadcastRowTabOrder(mainRow);

      mainRow.append(
         channelCell,
         timeCell,
         titleCell,
         organizationCell,
         groupCell,
         categoriesCell,
         actionsCell
      );

      const runsRow = document.createElement("tr");
      runsRow.className = "broadcast-participation-runs-row";
      runsRow.dataset.broadcastId = broadcastId;
      runsRow.dataset.participationStatus = participationStatusId || "";

      const spacerCell = document.createElement("td");
      spacerCell.className = "broadcast-participation-spacer";

      const runsCell = document.createElement("td");
      runsCell.className = "broadcast-participation-runs-cell";
      runsCell.colSpan = 6;
      runsCell.dataset.participationCell = "true";
      runsCell.dataset.broadcastId = broadcastId;
      runsCell.dataset.participationRunId = "";
      runsCell.dataset.participationStatus = participationStatusId || "";
      runsCell.dataset.checkParticipationUrl = checkParticipationUrl;
      runsCell.dataset.activityUrlBase = activityUrlBase;
      runsRow.append(spacerCell, runsCell);

      fragment.append(mainRow, runsRow);
      return fragment;
   }

   function setBroadcastRowTabOrder(row)
   {
      if(!(row instanceof HTMLElement))
      {
         return;
      }

      row.querySelectorAll(
         "a,button,input,select,textarea,[tabindex]"
      ).forEach(element => {
         if(!(element instanceof HTMLElement))
         {
            return;
         }

         if(element.matches(".broadcast-org-entity-input"))
         {
            element.tabIndex = 0;
            return;
         }

         element.tabIndex = -1;
      });
   }

   function appendHiddenBroadcastVisibilityFields(
      form,
      sourceForm,
      broadcastId,
      isHidden,
      showHidden
   )
   {
      if(!(form instanceof HTMLFormElement))
      {
         return;
      }

      if(sourceForm instanceof HTMLFormElement)
      {
         sourceForm.querySelectorAll("input").forEach(input => {
            if(!(input instanceof HTMLInputElement) || input.type !== "hidden")
            {
               return;
            }

            if(["id", "isHidden", "ShowHidden"].includes(input.name))
            {
               return;
            }

            form.append(input.cloneNode(true));
         });
      }

      ensureHiddenInput(form, "id", broadcastId);
      ensureHiddenInput(form, "isHidden", String(isHidden));
      ensureHiddenInput(form, "ShowHidden", String(showHidden));
   }

   function ensureHiddenInput(form, name, value)
   {
      if(!(form instanceof HTMLFormElement))
      {
         return;
      }

      const selector = `input[name='${name}']`;
      let input = form.querySelector(selector);

      if(!(input instanceof HTMLInputElement))
      {
         input = document.createElement("input");
         input.type = "hidden";
         input.name = name;
         form.append(input);
      }

      input.value = value;
   }

   function normalizeString(value)
   {
      if(typeof value !== "string")
      {
         return "";
      }

      return value.trim();
   }

   function normalizeNullableString(value)
   {
      if(value === null || typeof value === "undefined")
      {
         return "";
      }

      if(typeof value !== "string")
      {
         return String(value).trim();
      }

      return value.trim();
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

   function getBroadcastCategoriesFromCell(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return [];
      }

      return normalizeBroadcastCategories(
         cell.dataset.broadcastCategoriesJson
            ?? cell.dataset.broadcastInlineEditValue
            ?? ""
      );
   }

   function initializeCheckboxToggles(root = document)
   {
      root.querySelectorAll(checkboxToggleSelector).forEach(toggle => {
         if(!(toggle instanceof HTMLButtonElement))
         {
            return;
         }

         if(toggle.dataset.checkboxToggleInitialized === "true")
         {
            return;
         }

         toggle.dataset.checkboxToggleInitialized = "true";
         updateCheckboxToggle(toggle);

         toggle.addEventListener("click", () => {
            const checkboxes = getCheckboxGroup(toggle);
            const shouldSelect = checkboxes.some(checkbox => !checkbox.checked);

            checkboxes.forEach(checkbox => {
               if(checkbox.checked === shouldSelect)
               {
                  return;
               }

               checkbox.checked = shouldSelect;
               checkbox.dispatchEvent(new Event("change", { bubbles: true }));
            });

            updateCheckboxToggle(toggle);
         });

         getCheckboxGroup(toggle).forEach(checkbox => {
            checkbox.addEventListener("change", () => {
               updateCheckboxToggle(toggle);
            });
         });
      });
   }

   function initializeCheckboxVisibility(root = document)
   {
      root.querySelectorAll(checkboxVisibilitySelector).forEach(target => {
         if(target.dataset.checkboxVisibilityInitialized === "true")
         {
            return;
         }

         target.dataset.checkboxVisibilityInitialized = "true";
         updateCheckboxVisibility(target);

         getCheckboxesForGroup(
            target.dataset.visibleWhenCheckboxGroup
         ).forEach(checkbox => {
            checkbox.addEventListener("change", () => {
               updateCheckboxVisibility(target);
            });
         });
      });
   }

   function initializePersonGenderVisibility(root = document)
   {
      root.querySelectorAll(entityTypeSelectSelector).forEach(select => {
         if(!(select instanceof HTMLSelectElement)
            || select.dataset.personGenderVisibilityInitialized === "true")
         {
            return;
         }

         const form = select.closest("form");
         const genderField = form?.querySelector(personGenderFieldSelector);

         if(!(genderField instanceof HTMLElement))
         {
            return;
         }

         select.dataset.personGenderVisibilityInitialized = "true";

         const update = () => {
            genderField.style.display =
               select.value.trim().toLowerCase() === "person"
                  ? ""
                  : "none";
         };

         select.addEventListener("change", update);
         update();
      });
   }

   function initializeDateSelect(root = document)
   {
      const select = root.querySelector(dateSelectSelector);

      if(!(select instanceof HTMLSelectElement)
         || select.dataset.dateSelectInitialized === "true")
      {
         return;
      }

      select.dataset.dateSelectInitialized = "true";

      const sync = () => {
         const url = new URL(window.location.href);
         const selectedDate = url.searchParams.get("date");

         if(selectedDate && select.value !== selectedDate)
         {
            select.value = selectedDate;
         }
      };

      window.addEventListener("pageshow", sync);
      sync();
   }

   function initializeGetFormRestoration()
   {
      const restore = () => {
         document.querySelectorAll(getFormSelector).forEach(form => {
            if(form instanceof HTMLFormElement)
            {
               form.reset();
            }
         });
      };

      window.addEventListener("pageshow", restore);
      restore();
   }

   function initializeTeaserGeneration(root = document)
   {
      root.querySelectorAll(generateTeaserSelector).forEach(button => {
         if(!(button instanceof HTMLButtonElement)
            || button.dataset.generateTeaserInitialized === "true")
         {
            return;
         }

         button.dataset.generateTeaserInitialized = "true";
         button.addEventListener("click", async () => {
            await generateTeaserAsync(button);
         });
      });

      root.querySelectorAll(findFactsSelector).forEach(button => {
         if(!(button instanceof HTMLButtonElement)
            || button.dataset.findFactsInitialized === "true")
         {
            return;
         }

         button.dataset.findFactsInitialized = "true";
         button.addEventListener("click", async () => {
            await findFactsAsync(button);
         });
      });
   }

   function initializeActivityFactsChecks()
   {
      if(document.documentElement.dataset.activityFactsChecksInitialized
         === "true")
      {
         return;
      }

      document.documentElement.dataset.activityFactsChecksInitialized =
         "true";

      document.addEventListener("submit", async event => {
         const form = event.target;

         if(!(form instanceof HTMLFormElement)
            || !form.matches(activityFactsCheckSelector))
         {
            return;
         }

         event.preventDefault();

         const button = form.querySelector("button[type='submit']");
         const status = form.querySelector("[data-facts-check-status]");
         const url = button instanceof HTMLButtonElement
            ? button.dataset.factsUrl
            : "";

         if(!(button instanceof HTMLButtonElement)
            || !url
            || !(status instanceof HTMLElement))
         {
            return;
         }

         button.disabled = true;
         status.textContent = "Queueing...";
         status.classList.remove("form-status-error");

         try
         {
            const response = await fetch(url, {
               method: "post",
               body: new FormData(form),
               headers: {
                  Accept: "application/json"
               }
            });
            const payload = await response.json();

            if(!response.ok)
            {
               throw new Error(payload.error || "Facts check failed.");
            }

            const runId = typeof payload.runId === "string"
               ? payload.runId.trim()
               : "";

            if(runId !== "")
            {
               const row = form.closest("tr");

               if(row instanceof HTMLElement)
               {
                  row.dataset.aiRunId = runId;
                  row.dataset.aiRunStatus = "pending";
                  pendingRunIds.add(runId);
                  startRunPolling();
               }
            }

            status.textContent = "Queued";
         }
         catch(error)
         {
            status.textContent = error instanceof Error
               ? error.message
               : "Facts check failed.";
            status.classList.add("form-status-error");
         }
         finally
         {
            button.disabled = false;
         }
      });
   }

   function initializeParticipationRowChecks(root = document)
   {
      if(root !== document
         || document.documentElement.dataset.broadcastChecksInitialized
            === "true")
      {
         return;
      }

      document.documentElement.dataset.broadcastChecksInitialized = "true";

      document.addEventListener("click", async event => {
         const target = event.target;

         if(!(target instanceof Element))
         {
            return;
         }

         const button = target.closest(
            `${checkParticipationRowSelector},`
               + participationRunsToggleSelector
         );

         if(!(button instanceof HTMLButtonElement))
         {
            return;
         }

         event.preventDefault();
         if(button.hasAttribute("data-participation-runs-toggle"))
         {
            toggleParticipationRuns(button);
            return;
         }

         await checkParticipationRowAsync(button);
      });
   }

   function initializeBroadcastInlineEditing(root = document)
   {
      if(root === document
         && document.documentElement.dataset.broadcastInlineEditingInitialized
            === "true")
      {
         return;
      }

      if(root === document)
      {
         document.documentElement.dataset
            .broadcastInlineEditingInitialized = "true";

         const handleInlineEditActivation = event => {
            if(event.type === "click" && !isTouchEditInteraction())
            {
               return;
            }

            const target = event.target;

            if(!(target instanceof Element))
            {
               return;
            }

            if(target.closest("a,button,input,textarea,select,label"))
            {
               return;
            }

            const broadcastCell = target.closest(
               broadcastInlineEditCellSelector
            );

            if(broadcastCell instanceof HTMLElement)
            {
               event.preventDefault();
               openBroadcastInlineEditCell(broadcastCell);
               return;
            }

            const runCell = target.closest(runInlineEditCellSelector);

            if(!(runCell instanceof HTMLElement))
            {
               return;
            }

            event.preventDefault();
            openRunInlineEditCell(runCell);
         };

         document.addEventListener("dblclick", handleInlineEditActivation);
         document.addEventListener("click", handleInlineEditActivation);
      }

      root.querySelectorAll("[data-broadcast-inline-edit-input]").forEach(
         input => {
            initializeBroadcastInlineEditInput(input);
         }
      );

      root.querySelectorAll(broadcastCategoriesListSelector).forEach(list => {
         const cell = list.closest(broadcastInlineEditCellSelector);
         const value = cell instanceof HTMLElement
            ? getBroadcastCategoriesFromCell(cell)
            : [];

         if(typeof renderBroadcastCategories === "function")
         {
            renderBroadcastCategories(list, value);
         }
      });

      root.querySelectorAll(broadcastRowSelector).forEach(row => {
         setBroadcastRowTabOrder(row);
      });
   }

   function initializeRunInlineEditing(root = document)
   {
      if(root === document
         && document.documentElement.dataset.runInlineEditingInitialized
            === "true")
      {
         return;
      }

      if(root === document)
      {
         document.documentElement.dataset
            .runInlineEditingInitialized = "true";
      }

      root.querySelectorAll(runInlineEditInputSelector).forEach(input => {
         initializeRunInlineEditInput(input);
      });
   }

   function initializeRunInlineEditInput(input)
   {
      if(!(input instanceof HTMLSelectElement)
         || input.dataset.runInlineEditInitialized === "true")
      {
         return;
      }

      input.dataset.runInlineEditInitialized = "true";

      input.addEventListener("change", () => {
         void saveRunInlineEditAsync(input);
      });

      input.addEventListener("blur", () => {
         void saveRunInlineEditAsync(input);
      });

      input.addEventListener("keydown", event => {
         if(event.key === "Escape")
         {
            event.preventDefault();
            cancelRunInlineEdit(input);
         }
      });
   }

   async function initializeParticipationRunsAsync(root = document)
   {
      if(root === document
         && document.documentElement.dataset.broadcastParticipationRunsLoaded
            === "true")
      {
         return;
      }

      if(root === document)
      {
         document.documentElement.dataset
            .broadcastParticipationRunsLoaded = "true";
      }

      const cells = Array.from(root.querySelectorAll(participationCellSelector))
         .filter(cell => cell instanceof HTMLElement);

      if(cells.length === 0)
      {
         return;
      }

      const broadcastIds = [];

      cells.forEach(cell => {
         const broadcastId = (cell.dataset.broadcastId ?? "").trim();

         if(broadcastId !== "" && !broadcastIds.includes(broadcastId))
         {
            broadcastIds.push(broadcastId);
         }
      });

      if(broadcastIds.length === 0)
      {
         return;
      }

      const url = getParticipationStatusUrl();

      if(url === "")
      {
         return;
      }

      try
      {
         const payload = await postParticipationStatusAsync(url, broadcastIds);
         const results = Array.isArray(payload?.results)
            ? payload.results
            : [];
         const resultsByBroadcastId = new Map();

         results.forEach(result => {
            if(!result || typeof result !== "object")
            {
               return;
            }

            const broadcastId = typeof result.id === "string"
               ? result.id.trim()
               : "";

            if(broadcastId !== "")
            {
               resultsByBroadcastId.set(broadcastId, result);
            }
         });

         cells.forEach(cell => {
            const broadcastId = (cell.dataset.broadcastId ?? "").trim();
            const result = broadcastIds.includes(broadcastId)
               ? resultsByBroadcastId.get(broadcastId)
               : null;

            if(result)
            {
               updateParticipationCell(cell, result);
            }
            else
            {
               setNoParticipationHistoryCell(cell);
            }
         });

         initializeParticipationPolling(root);
      }
      catch(error)
      {
         console.error("Participation runs load failed:", error);
      }
   }

   function openRunInlineEditCell(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const row = cell.closest("tr");
      const statusId = (row?.dataset.aiRunStatus ?? "").trim().toLowerCase();
      const input = cell.querySelector(runInlineEditInputSelector);
      const display = cell.querySelector(runInlineEditDisplaySelector);

      if(statusId !== "pending"
         || !(input instanceof HTMLSelectElement)
         || !(display instanceof HTMLElement)
         || input.hidden === false)
      {
         return;
      }

      if(input.dataset.runInlineEditSaving === "true")
      {
         return;
      }

      input.dataset.runInlineEditOriginalValue = input.value;
      cell.dataset.runInlineEditing = "true";
      display.hidden = true;
      input.hidden = false;

      window.requestAnimationFrame(() => {
         input.focus();
      });
   }

   async function saveRunInlineEditAsync(input)
   {
      if(!(input instanceof HTMLSelectElement)
         || input.hidden
         || input.dataset.runInlineEditSaving === "true")
      {
         return;
      }

      const cell = input.closest(runInlineEditCellSelector);
      const url = getRunInlineEditUrl();
      const runId = (cell?.closest("tr")?.dataset.aiRunId ?? "").trim();
      const field = (cell?.dataset.runInlineEditField ?? "").trim();
      const currentValue = input.value.trim();
      const originalValue = (
         input.dataset.runInlineEditOriginalValue ?? ""
      ).trim();

      if(!(cell instanceof HTMLElement)
         || url === ""
         || runId === ""
         || field === "")
      {
         return;
      }

      if(currentValue === originalValue)
      {
         restoreRunInlineEditInput(input);
         return;
      }

      input.dataset.runInlineEditSaving = "true";
      input.disabled = true;

      try
      {
         const payload = await postRunInlineEditAsync(
            url,
            runId,
            field,
            currentValue
         );

         updateRunInlineEditCell(cell, payload);
         restoreRunInlineEditInput(input);
      }
      catch(error)
      {
         window.alert(
            error instanceof Error
               ? error.message
               : "Run update failed."
         );
         input.hidden = false;
         window.requestAnimationFrame(() => {
            input.focus();
         });
      }
      finally
      {
         input.disabled = false;
         delete input.dataset.runInlineEditSaving;
      }
   }

   function cancelRunInlineEdit(input)
   {
      if(!(input instanceof HTMLSelectElement))
      {
         return;
      }

      const originalValue = (
         input.dataset.runInlineEditOriginalValue ?? input.value
      ).trim();

      input.value = originalValue;
      restoreRunInlineEditInput(input);
   }

   function restoreRunInlineEditInput(input)
   {
      if(!(input instanceof HTMLSelectElement))
      {
         return;
      }

      const cell = input.closest(runInlineEditCellSelector);
      const display = cell?.querySelector(runInlineEditDisplaySelector);

      if(display instanceof HTMLElement)
      {
         display.hidden = false;
      }

      input.hidden = true;

      if(cell instanceof HTMLElement)
      {
         delete cell.dataset.runInlineEditing;
      }
   }

   function initializeBroadcastInlineEditInput(input)
   {
      if(!(input instanceof HTMLInputElement)
         || input.dataset.broadcastInlineEditInitialized === "true")
      {
         return;
      }

      input.dataset.broadcastInlineEditInitialized = "true";

      input.addEventListener("blur", () => {
         void saveBroadcastInlineEditAsync(input);
      });

      input.addEventListener("keydown", event => {
         if(event.key === "Enter")
         {
            event.preventDefault();
            input.blur();
         }
         else if(event.key === "Escape")
         {
            event.preventDefault();
            cancelBroadcastInlineEdit(input);
         }
      });
   }

   function openBroadcastInlineEditCell(cell)
   {
      const input = cell.querySelector(
         "[data-broadcast-inline-edit-input]"
      );
      const display = cell.querySelector(
         "[data-broadcast-inline-edit-display]"
      );

      if(!(input instanceof HTMLInputElement)
         || !(display instanceof HTMLElement)
         || input.hidden === false)
      {
         return;
      }

      if(input.dataset.broadcastInlineEditSaving === "true")
      {
         return;
      }

      input.dataset.broadcastInlineEditOriginalValue = input.value;
      cell.dataset.broadcastInlineEditing = "true";
      display.hidden = true;
      input.hidden = false;

      window.requestAnimationFrame(() => {
         input.focus();
         input.select();
      });
   }

   async function saveBroadcastInlineEditAsync(input)
   {
      if(!(input instanceof HTMLInputElement)
         || input.hidden
         || input.dataset.broadcastInlineEditSaving === "true")
      {
         return;
      }

      const cell = input.closest(broadcastInlineEditCellSelector);
      const url = getBroadcastInlineEditUrl();
      const broadcastId = (cell?.dataset.broadcastId ?? "").trim();
      const field = (cell?.dataset.broadcastInlineEditField ?? "").trim();
      const currentValue = input.value.trim();
      const originalValue = (
         input.dataset.broadcastInlineEditOriginalValue ?? ""
      ).trim();

      if(!(cell instanceof HTMLElement)
         || url === ""
         || broadcastId === ""
         || field === "")
      {
         return;
      }

      if(field === broadcastInlineEditTitleField && currentValue === "")
      {
         window.alert("Title cannot be empty.");
         restoreBroadcastInlineEditInput(input);
         return;
      }

      if(currentValue === originalValue)
      {
         restoreBroadcastInlineEditInput(input);
         return;
      }

      input.dataset.broadcastInlineEditSaving = "true";
      input.disabled = true;

      try
      {
         const payload = await postBroadcastInlineEditAsync(
            url,
            broadcastId,
            field,
            currentValue
         );

         updateBroadcastInlineEditCell(cell, payload);
         restoreBroadcastInlineEditInput(input);
      }
      catch(error)
      {
         window.alert(
            error instanceof Error
               ? error.message
               : "Broadcast update failed."
         );
         input.hidden = false;
         window.requestAnimationFrame(() => {
            input.focus();
            input.select();
         });
      }
      finally
      {
         input.disabled = false;
         delete input.dataset.broadcastInlineEditSaving;
      }
   }

   function cancelBroadcastInlineEdit(input)
   {
      if(!(input instanceof HTMLInputElement))
      {
         return;
      }

      const originalValue = (
         input.dataset.broadcastInlineEditOriginalValue ?? input.value
      ).trim();

      input.value = originalValue;
      restoreBroadcastInlineEditInput(input);
   }

   function restoreBroadcastInlineEditInput(input)
   {
      if(!(input instanceof HTMLInputElement))
      {
         return;
      }

      const cell = input.closest(broadcastInlineEditCellSelector);
      const display = cell?.querySelector(
         "[data-broadcast-inline-edit-display]"
      );

      if(display instanceof HTMLElement)
      {
         display.hidden = false;
      }

      input.hidden = true;

      if(cell instanceof HTMLElement)
      {
         delete cell.dataset.broadcastInlineEditing;
      }
   }

   function isTouchEditInteraction()
   {
      const mediaQuery = window.matchMedia?.(
         "(hover: none) and (pointer: coarse)"
      );

      return mediaQuery?.matches ?? false;
   }

   function initializeCurrentMarkerScroll()
   {
      const marker = document.querySelector(currentMarkerSelector);
      const storageKey = `sesport.currentMarkerScrolled:${
         window.location.pathname
      }`;

      if(!(marker instanceof HTMLElement)
         || window.sessionStorage.getItem(storageKey) === "true")
      {
         return;
      }

      const scroll = () => {
         marker.scrollIntoView({
            behavior: window.matchMedia(
               "(prefers-reduced-motion: reduce)"
            ).matches
               ? "auto"
               : "smooth",
            block: "center",
            inline: "nearest"
         });

         window.sessionStorage.setItem(storageKey, "true");
      };

      window.requestAnimationFrame(() => {
         window.requestAnimationFrame(scroll);
      });
   }

   async function checkParticipationRowAsync(button)
   {
      const url = button.dataset.checkParticipationUrl;
      const broadcastId = button.dataset.broadcastId;
      const cell = getParticipationCellForButton(button);
      const previousRunId = getParticipationRunId(cell);
      let keepPolling = false;

      if(!url || !broadcastId || !(cell instanceof HTMLElement))
      {
         return;
      }

      if(pendingParticipationIds.has(broadcastId))
      {
         return;
      }

      pendingParticipationIds.add(broadcastId);
      const originalLabel = button.textContent ?? "Check";
      button.disabled = true;
      button.textContent = "Checking...";
      setPendingParticipationCell(cell);

      try
      {
         const payload = await postParticipationCheckAsync(
            url,
            [broadcastId]
         );

         if(payload && payload.queued === true)
         {
            keepPolling = true;
            if(previousRunId !== "")
            {
               cell.dataset.participationQueuedFromRunId = previousRunId;
            }
            setQueuedParticipationCell(cell);
            startParticipationPolling();
            return;
         }

         const result = Array.isArray(payload.results)
            ? payload.results[0]
            : null;

         if(!result)
         {
            throw new Error("No participation result returned.");
         }

         updateParticipationCell(cell, result);
      }
      catch(error)
      {
         const message = error instanceof Error
            ? error.message
            : "Participation check failed.";

         updateParticipationCell(cell, {
            error: message,
            runId: null,
            swedishParticipation: null,
            participants: [],
            sourceUrls: []
         });
      }
      finally
      {
         if(!keepPolling)
         {
            pendingParticipationIds.delete(broadcastId);
         }
         button.disabled = false;
         button.textContent = originalLabel;
      }
   }

   function getParticipationCellForButton(button)
   {
      if(!(button instanceof HTMLButtonElement))
      {
         return null;
      }

      const directCell = button.closest(participationCellSelector);

      if(directCell instanceof HTMLElement)
      {
         return directCell;
      }

      const broadcastId = (button.dataset.broadcastId ?? "").trim();

      if(broadcastId === "")
      {
         return null;
      }

      const mainRow = button.closest("tr[data-broadcast-row='true']");

      if(!(mainRow instanceof HTMLElement))
      {
         return null;
      }

      const runsRow = mainRow.nextElementSibling;

      if(!(runsRow instanceof HTMLElement)
         || !runsRow.matches(".broadcast-participation-runs-row")
         || runsRow.dataset.broadcastId !== broadcastId)
      {
         return null;
      }

      return runsRow.querySelector(participationCellSelector);
   }

   async function postParticipationCheckAsync(url, selectedIds)
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      selectedIds.forEach(id => {
         formData.append("broadcastIds", id);
      });

      const response = await fetch(url, {
         method: "post",
         body: formData,
         keepalive: true,
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
         throw new Error(createParticipationErrorMessage(
            response.status,
            payload?.error,
            trimmedResponseText
         ));
      }

      return payload ?? {};
   }

   function setQueuedParticipationCell(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      cell.replaceChildren();
      const pendingCheck = normalizeParticipationCheckResult({
         statusId: "pending"
      });
      const { wrapper, body } = createParticipationRunsShell(
         cell,
         [pendingCheck]
      );
      body.append(createParticipationRunBlock(cell, pendingCheck));

      cell.dataset.participationStatus = "pending";
      updateParticipationRowStatus(cell, "pending");
      cell.append(wrapper);
   }

   function initializeParticipationPolling(root = document)
   {
      root.querySelectorAll(participationCellSelector).forEach(cell => {
         if(!(cell instanceof HTMLElement))
         {
            return;
         }

         const statusId = (cell.dataset.participationStatus ?? "").trim();
         const broadcastId = (cell.dataset.broadcastId ?? "").trim();

         if(!broadcastId)
         {
            return;
         }

         if(statusId === "running" || statusId === "pending")
         {
            pendingParticipationIds.add(broadcastId);
         }
      });

      if(pendingParticipationIds.size > 0)
      {
         startParticipationPolling();
      }
   }

   function startParticipationPolling()
   {
      if(participationPollingTimer !== null)
      {
         return;
      }

      participationPollingTimer = window.setInterval(() => {
         void pollParticipationStatusesAsync();
      }, 4000);

      void pollParticipationStatusesAsync();
   }

   function stopParticipationPolling()
   {
      if(participationPollingTimer === null)
      {
         return;
      }

      window.clearInterval(participationPollingTimer);
      participationPollingTimer = null;
   }

   function initializeRunPolling(root = document)
   {
      root.querySelectorAll(runRowSelector).forEach(row => {
         if(!(row instanceof HTMLElement))
         {
            return;
         }

         const runId = (row.dataset.aiRunId ?? "").trim();
         const statusId = (row.dataset.aiRunStatus ?? "").trim();

         if(!runId)
         {
            return;
         }

         if(statusId === "running" || statusId === "pending")
         {
            pendingRunIds.add(runId);
         }
      });

      if(pendingRunIds.size > 0)
      {
         startRunPolling();
      }
   }

   function startRunPolling()
   {
      if(runPollingTimer !== null)
      {
         return;
      }

      runPollingTimer = window.setInterval(() => {
         void pollRunStatusesAsync();
      }, 4000);

      void pollRunStatusesAsync();
   }

   function stopRunPolling()
   {
      if(runPollingTimer === null)
      {
         return;
      }

      window.clearInterval(runPollingTimer);
      runPollingTimer = null;
   }

   async function pollParticipationStatusesAsync()
   {
      if(pendingParticipationIds.size === 0)
      {
         stopParticipationPolling();
         return;
      }

      if(participationPollingInFlight)
      {
         return;
      }

      participationPollingInFlight = true;

      try
      {
         const url = getParticipationStatusUrl();

         if(!url)
         {
            return;
         }

         const payload = await postParticipationStatusAsync(
            url,
            [...pendingParticipationIds]
         );

         if(!payload || !Array.isArray(payload.results))
         {
            return;
         }

         payload.results.forEach(result => {
            if(!result || typeof result !== "object")
            {
               return;
            }

            const broadcastId = typeof result.id === "string"
               ? result.id
               : "";
            const cell = getParticipationCellByBroadcastId(broadcastId);
            const statusId = typeof result.statusId === "string"
               ? result.statusId.trim()
               : "";
            const resultRunId = typeof result.runId === "string"
               ? result.runId.trim()
               : "";
            const participation = getParticipationValue(result);
            const isFinal =
               (typeof result.error === "string"
                  && result.error.trim() !== "") ||
               participation !== "" ||
               statusId === "completed" ||
               statusId === "failed";
            const queuedFromRunId = cell instanceof HTMLElement
               ? (cell.dataset.participationQueuedFromRunId ?? "").trim()
               : "";
            const isStaleQueuedResult =
               isFinal &&
               queuedFromRunId !== "" &&
               resultRunId !== "" &&
               resultRunId === queuedFromRunId;

            if(isStaleQueuedResult)
            {
               return;
            }

            updateParticipationCellByResult(result);

            if(broadcastId && isFinal)
            {
               pendingParticipationIds.delete(broadcastId);
            }
         });

         if(pendingParticipationIds.size === 0)
         {
            stopParticipationPolling();
         }
      }
      catch
      {
      }
      finally
      {
         participationPollingInFlight = false;
      }
   }

   function getParticipationStatusUrl()
   {
      const container = document.querySelector(
         participationStatusUrlSelector
      );

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.checkParticipationStatusUrl;

      return typeof url === "string" && url.trim() !== ""
         ? url.trim()
         : "";
   }

   async function postParticipationStatusAsync(url, selectedIds)
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      selectedIds.forEach(id => {
         formData.append("broadcastIds", id);
      });

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
         throw new Error(createParticipationErrorMessage(
            response.status,
            payload?.error,
            trimmedResponseText
         ));
      }

      return payload ?? {};
   }

   async function pollRunStatusesAsync()
   {
      if(pendingRunIds.size === 0)
      {
         stopRunPolling();
         return;
      }

      if(runPollingInFlight)
      {
         return;
      }

      runPollingInFlight = true;

      try
      {
         const url = getRunStatusesUrl();

         if(!url)
         {
            return;
         }

         const payload = await postRunStatusesAsync(url, [...pendingRunIds]);

         if(!payload || !Array.isArray(payload.results))
         {
            return;
         }

         payload.results.forEach(result => {
            if(!result || typeof result !== "object")
            {
               return;
            }

            const runId = typeof result.id === "string"
               ? result.id.trim()
               : "";

            if(!runId)
            {
               return;
            }

            const row = getRunRowById(runId);
            const statusId = typeof result.statusId === "string"
               ? result.statusId.trim()
               : "";
            const isFinal =
               statusId !== "running" && statusId !== "pending";

            updateRunRow(row, result);

            if(isFinal)
            {
               pendingRunIds.delete(runId);
            }
         });

         if(pendingRunIds.size === 0)
         {
            stopRunPolling();
         }
      }
      catch
      {
      }
      finally
      {
         runPollingInFlight = false;
      }
   }

   function getRunStatusesUrl()
   {
      const container = document.querySelector(runStatusesUrlSelector);

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.runStatusesUrl;

      return typeof url === "string" && url.trim() !== ""
         ? url.trim()
         : "";
   }

   function getRunInlineEditUrl()
   {
      const container = document.querySelector(runInlineEditUrlSelector);

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.runInlineEditUrl;

      return typeof url === "string" && url.trim() !== ""
         ? url.trim()
         : "";
   }

   function getEntityInlineEditUrl()
   {
      const container = document.querySelector(entityInlineEditUrlSelector);

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.entityInlineEditUrl;

      return typeof url === "string" && url.trim() !== ""
         ? url.trim()
         : "";
   }

   async function postRunStatusesAsync(url, runIds)
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      runIds.forEach(id => {
         formData.append("runIds", id);
      });

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

   async function postRunInlineEditAsync(url, runId, field, value)
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      formData.append("id", runId);
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

   async function postEntityInlineEditAsync(url, entityId, field, value)
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      formData.append("id", entityId);
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

   function updateParticipationCellByResult(result)
   {
      if(!result || typeof result !== "object")
      {
         return;
      }

      const broadcastId = typeof result.id === "string"
         ? result.id
         : "";

      if(broadcastId === "")
      {
         return;
      }

      const cell = document.querySelector(
         `${participationCellSelector}[data-broadcast-id='${broadcastId}']`
      );

      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      updateParticipationCell(cell, result);
   }

   function getParticipationCellByBroadcastId(broadcastId)
   {
      if(typeof broadcastId !== "string" || broadcastId.trim() === "")
      {
         return null;
      }

      return document.querySelector(
         `${participationCellSelector}[data-broadcast-id='${broadcastId}']`
      );
   }

   function getRunRowById(runId)
   {
      if(typeof runId !== "string" || runId.trim() === "")
      {
         return null;
      }

      return document.querySelector(
         `${runRowSelector}[data-ai-run-id='${runId}']`
      );
   }

   function updateRunRow(row, result)
   {
      if(!(row instanceof HTMLElement) || !result || typeof result !== "object")
      {
         return;
      }

      const statusId = typeof result.statusId === "string"
         ? result.statusId.trim()
         : "";
      const rounds = typeof result.rounds === "number"
         ? result.rounds.toString()
         : "";
      const maxPayloadChars = typeof result.maxPayloadChars === "number"
         ? result.maxPayloadChars.toString()
         : "";
      const duration = typeof result.duration === "string"
         ? result.duration.trim()
         : "";
      const summary = typeof result.resultSummary === "string"
         ? result.resultSummary.trim()
         : "";

      row.dataset.aiRunStatus = statusId;
      updateRunStatusRow(row, statusId);
      updateRunStatusCell(row, statusId);
      updateActivityFactsCheckStatus(row, statusId);

      const payloadCell = row.querySelector(runPayloadCellSelector);

      if(payloadCell instanceof HTMLElement && maxPayloadChars !== "")
      {
         payloadCell.textContent = maxPayloadChars;
      }

      const roundsCell = row.querySelector(runRoundsCellSelector);

      if(roundsCell instanceof HTMLElement && rounds !== "")
      {
         roundsCell.textContent = rounds;
      }

      const durationCell = row.querySelector(runDurationCellSelector);

      if(durationCell instanceof HTMLElement && duration !== "")
      {
         durationCell.textContent = duration;
      }

      const summaryCell = row.querySelector(runSummaryCellSelector);

      if(summaryCell instanceof HTMLElement)
      {
         summaryCell.textContent = summary !== "" ? summary : "-";
      }
   }

   function updateActivityFactsCheckStatus(row, statusId)
   {
      if(!(row instanceof HTMLElement))
      {
         return;
      }

      const status = row.querySelector(
         activityFactsCheckStatusSelector
      );

      if(!(status instanceof HTMLElement))
      {
         return;
      }

      const normalizedStatusId = typeof statusId === "string"
         ? statusId.trim().toLowerCase()
         : "";

      status.textContent = normalizedStatusId === "running"
         ? "Running"
         : normalizedStatusId === "pending"
            ? "Queued"
            : "";
   }

   function updateRunInlineEditCell(cell, payload)
   {
      if(!(cell instanceof HTMLElement) || !payload)
      {
         return;
      }

      const field = typeof payload.field === "string"
         ? payload.field.trim()
         : "";

      if(field !== runInlineEditField)
      {
         return;
      }

      const nextValue = typeof payload.value === "string"
         ? payload.value.trim()
         : "";
      const displayValue = typeof payload.displayValue === "string"
         ? payload.displayValue.trim()
         : nextValue;
      const display = cell.querySelector(runInlineEditDisplaySelector);
      const input = cell.querySelector(runInlineEditInputSelector);

      cell.dataset.runInlineEditValue = nextValue;

      if(display instanceof HTMLElement)
      {
         display.hidden = false;

         const environment = display.querySelector(".ai-runs-environment");

         if(environment instanceof HTMLElement)
         {
            environment.textContent = displayValue || "-";
            environment.title = nextValue;
         }
      }

      if(input instanceof HTMLSelectElement)
      {
         input.value = nextValue;
         input.dataset.runInlineEditOriginalValue = nextValue;
      }
   }

   function updateEntityInlineEditCell(cell, payload)
   {
      if(!(cell instanceof HTMLElement) || !payload)
      {
         return;
      }

      const field = typeof payload.field === "string"
         ? payload.field.trim()
         : "";

      if(field !== "watch-priority")
      {
         return;
      }

      const nextValue = typeof payload.value === "string"
         ? payload.value.trim()
         : "";
      const displayValue = typeof payload.displayValue === "string"
         ? payload.displayValue.trim()
         : nextValue;
      const display = cell.querySelector(entityInlineEditDisplaySelector);
      const input = cell.querySelector(entityInlineEditInputSelector);

      cell.dataset.entityInlineEditValue = nextValue;

      if(display instanceof HTMLElement)
      {
         display.hidden = false;

         const valueText = display.querySelector("span");

         if(valueText instanceof HTMLElement)
         {
            valueText.textContent = displayValue || "-";
            valueText.title = nextValue;
         }
         else
         {
            display.textContent = displayValue || "-";
         }
      }

      if(input instanceof HTMLSelectElement)
      {
         input.value = nextValue;
         input.dataset.entityInlineEditOriginalValue = nextValue;
      }
   }

   function updateRunStatusCell(row, statusId)
   {
      if(!(row instanceof HTMLElement))
      {
         return;
      }

      const statusCell = row.querySelector(runStatusCellSelector);
      const statusText = row.querySelector(runStatusTextSelector);

      if(statusText instanceof HTMLElement)
      {
         statusText.textContent = statusId;
         return;
      }

      if(statusCell instanceof HTMLElement)
      {
         statusCell.textContent = statusId;
      }
   }

   function updateRunStatusRow(row, statusId)
   {
      if(!(row instanceof HTMLElement))
      {
         return;
      }

      const normalizedStatusId = typeof statusId === "string"
         ? statusId.trim().toLowerCase()
         : "";

      if(normalizedStatusId === "running"
         || normalizedStatusId === "pending")
      {
         const runId = typeof row.dataset.aiRunId === "string"
            ? row.dataset.aiRunId.trim()
            : "";

         row.dataset.aiRunStatus = normalizedStatusId;

         if(runId)
         {
            pendingRunIds.add(runId);
         }
      }
      else
      {
         delete row.dataset.aiRunStatus;
         const runId = typeof row.dataset.aiRunId === "string"
            ? row.dataset.aiRunId.trim()
            : "";

         if(runId)
         {
            pendingRunIds.delete(runId);
         }
      }
   }

   function updateParticipationCell(cell, result)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const isOpen = isParticipationRunsOpen(cell);
      cell.replaceChildren();
      const checks = normalizeParticipationChecks(result);

      if(checks.length === 0)
      {
         const { wrapper, body } = createParticipationRunsShell(
            cell,
            checks,
            isOpen
         );
         cell.append(wrapper);
         updateParticipationRunId(cell, "");
         updateParticipationRowStatus(cell, "");
         body.append(createParticipationNoHistoryRunBlock(cell));
         return;
      }

      const { wrapper, body } = createParticipationRunsShell(
         cell,
         checks,
         isOpen
      );
      cell.append(wrapper);

      const latestCheck = checks[0];

      const statusId = typeof latestCheck.statusId === "string"
         ? latestCheck.statusId.trim()
         : "";
      updateParticipationRunId(cell, latestCheck.runId);
      if(statusId !== "")
      {
         cell.dataset.participationStatus = statusId;
         updateParticipationRowStatus(cell, statusId);
      }
      else
      {
         updateParticipationRowStatus(cell, "");
      }

      const latestWrapper = createParticipationRunBlock(cell, latestCheck);

      body.append(latestWrapper);

      checks.slice(1).forEach(check => {
         body.append(createParticipationRunBlock(cell, check));
      });

      initializeParticipationRowChecks(cell);
   }

   function setNoParticipationHistoryCell(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const isOpen = isParticipationRunsOpen(cell);
      cell.replaceChildren();
      const { wrapper, body } = createParticipationRunsShell(
         cell,
         [],
         isOpen
      );
      body.append(createParticipationNoHistoryRunBlock(cell));

      updateParticipationRunId(cell, "");
      updateParticipationRowStatus(cell, "");
      cell.append(wrapper);
   }

   function normalizeParticipationChecks(result)
   {
      if(!result || typeof result !== "object")
      {
         return [];
      }

      if(Array.isArray(result.checks))
      {
         return result.checks
            .map(check => normalizeParticipationCheckResult(check))
            .filter(check => check !== null);
      }

      return [
         normalizeParticipationCheckResult(result)
      ].filter(check => check !== null);
   }

   function normalizeParticipationCheckResult(check)
   {
      if(!check || typeof check !== "object")
      {
         return null;
      }

      const statusId = typeof check.statusId === "string"
         ? check.statusId.trim()
         : "";
      const runId = typeof check.runId === "string"
         ? check.runId.trim()
         : "";
      const error = typeof check.errorMessage === "string"
         ? check.errorMessage.trim()
         : typeof check.error === "string"
            ? check.error.trim()
            : "";

      const participants = Array.isArray(check.participants)
         ? check.participants
         : Array.isArray(check.Participants)
            ? check.Participants
            : [];

      return {
         runId,
         statusId,
         toolRoundCount: typeof check.toolRoundCount === "number"
            ? check.toolRoundCount
            : 0,
         swedishParticipation: getParticipationValue(check),
         participants,
         sourceUrls: Array.isArray(check.sourceUrls)
            ? check.sourceUrls
               .filter(url => typeof url === "string" && url.trim() !== "")
            : [],
         error,
         summaryText: typeof check.summaryText === "string"
            && check.summaryText.trim() !== ""
            ? check.summaryText.trim()
               : getParticipationValue(check) === ""
                  ? formatParticipationStatus(statusId)
                  : statusId
      };
   }

   function getParticipationValue(check)
   {
      if(!check || typeof check !== "object")
      {
         return "";
      }

      if(typeof check.swedishParticipation === "string")
      {
         return check.swedishParticipation.trim();
      }

      if(typeof check.participation === "string")
      {
         return check.participation.trim();
      }

      return typeof check.Participation === "string"
         ? check.Participation.trim()
         : "";
   }

   function createParticipationRunsShell(
      cell,
      checks,
      isOpen = false
   )
   {
      const wrapper = document.createElement("div");
      wrapper.className = "broadcast-ai-check-runs";
      wrapper.dataset.participationRunsOpen = String(isOpen);

      const table = document.createElement("table");
      table.className = "broadcast-ai-check-runs-table";
      table.dataset.participationRunsOpen = String(isOpen);

      const head = document.createElement("thead");
      head.className = "broadcast-ai-check-runs-head";
      const headRow = document.createElement("tr");
      const headCell = document.createElement("th");
      headCell.colSpan = 4;

      const headerBar = document.createElement("div");
      headerBar.className = "broadcast-ai-check-runs-summary-bar";

      const summaryCheck = selectParticipationSummaryCheck(checks);
      const summaryText = document.createElement("span");
      summaryText.className = [
         "broadcast-ai-check-runs-summary-text",
         getParticipationSummaryBadgeClass(summaryCheck)
      ].filter(value => value !== "").join(" ");
      summaryText.textContent = formatParticipationRunsSummaryText(
         summaryCheck
      );
      headerBar.append(summaryText);

      const actions = document.createElement("div");
      actions.className = "broadcast-ai-check-runs-summary-actions";

      const toggleButton = document.createElement("button");
      toggleButton.className = "button broadcast-ai-check-toggle";
      toggleButton.type = "button";
      toggleButton.dataset.participationRunsToggle = "true";
      toggleButton.setAttribute("aria-expanded", String(isOpen));
      toggleButton.setAttribute(
         "aria-label",
         isOpen ? "Hide participation runs" : "Show participation runs"
      );
      toggleButton.textContent = isOpen ? "−" : "+";
      toggleButton.tabIndex = -1;
      actions.append(toggleButton);

      headerBar.append(actions);
      headCell.append(headerBar);
      headRow.append(headCell);
      head.append(headRow);

      const body = document.createElement("tbody");
      body.className = "broadcast-ai-check-runs-body";
      body.hidden = !isOpen;

      table.append(head, body);
      wrapper.append(table);

      return { wrapper, body };
   }

   function formatParticipationRunsSummaryText(check)
   {
      if(!check || typeof check !== "object")
      {
         return "Not checked yet";
      }

      const participation = typeof check.swedishParticipation === "string"
         ? check.swedishParticipation.trim()
         : "";

      if(participation.toLowerCase() === "yes")
      {
         const participantCount = Array.isArray(check.participants)
            ? check.participants.length
            : 0;

         return `YES: ${participantCount}`;
      }

      return typeof check.summaryText === "string" &&
         check.summaryText.trim() !== ""
         ? check.summaryText.trim()
         : "Not checked yet";
   }

   function toggleParticipationRuns(toggleButton)
   {
      if(!(toggleButton instanceof HTMLButtonElement))
      {
         return;
      }

      const table = toggleButton.closest(".broadcast-ai-check-runs-table");

      if(!(table instanceof HTMLTableElement))
      {
         return;
      }

      const body = table.querySelector(".broadcast-ai-check-runs-body");

      if(!(body instanceof HTMLElement))
      {
         return;
      }

      body.hidden = !body.hidden;
      const isOpen = !body.hidden;
      table.dataset.participationRunsOpen = String(isOpen);
      toggleButton.setAttribute("aria-expanded", String(isOpen));
      toggleButton.setAttribute(
         "aria-label",
         isOpen ? "Hide participation runs" : "Show participation runs"
      );
      toggleButton.textContent = isOpen ? "−" : "+";
   }

   function getParticipationSummaryBadgeClass(check)
   {
      if(!check || typeof check !== "object")
      {
         return "";
      }

      const participation = typeof check.swedishParticipation === "string"
         ? check.swedishParticipation.trim().toLowerCase()
         : "";

      switch(participation)
      {
         case "yes":
            return "tool-trace-badge tool-trace-badge-result";
         case "no":
            return "tool-trace-badge tool-trace-badge-temperature";
         case "unknown":
            return "tool-trace-badge tool-trace-badge-count";
         default:
            return "";
      }
   }

   function selectParticipationSummaryCheck(checks)
   {
      if(!Array.isArray(checks) || checks.length === 0)
      {
         return null;
      }

      const normalizedChecks = checks.filter(check =>
         check && typeof check === "object");

      return normalizedChecks.find(check =>
            getParticipationSummaryPriority(check) === 0)
         ?? normalizedChecks.find(check =>
            getParticipationSummaryPriority(check) === 1)
         ?? normalizedChecks.find(check =>
            getParticipationSummaryPriority(check) === 2)
         ?? normalizedChecks[0]
         ?? null;
   }

   function getParticipationSummaryPriority(check)
   {
      if(!check || typeof check !== "object")
      {
         return Number.POSITIVE_INFINITY;
      }

      const participation = typeof check.swedishParticipation === "string"
         ? check.swedishParticipation.trim().toLowerCase()
         : "";

      switch(participation)
      {
         case "yes":
            return 0;
         case "no":
            return 1;
         case "unknown":
            return 2;
         default:
            return Number.POSITIVE_INFINITY;
      }
   }

   function isParticipationRunsOpen(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return false;
      }

      const body = cell.querySelector(".broadcast-ai-check-runs-body");

      return body instanceof HTMLElement ? !body.hidden : false;
   }

   function createParticipationEmptyRunBlock(cell)
   {
      const row = document.createElement("tr");
      row.className = "broadcast-ai-check-row";

      const summaryCell = document.createElement("td");
      summaryCell.className = "broadcast-ai-check-summary-cell";

      const participantsCell = document.createElement("td");
      participantsCell.className = "broadcast-ai-check-participants-cell";

      const sourcesCell = document.createElement("td");
      sourcesCell.className = "broadcast-ai-check-sources-cell";

      const activityCell = document.createElement("td");
      activityCell.className = "broadcast-ai-check-activity-cell";

      const fallback = document.createElement("span");
      fallback.className = "broadcast-ai-check-empty";
      fallback.textContent = "Not checked yet";
      summaryCell.append(fallback);

      row.append(
         summaryCell,
         participantsCell,
         sourcesCell,
         activityCell
      );

      return row;
   }

   function createParticipationNoHistoryRunBlock(cell)
   {
      const row = createParticipationEmptyRunBlock(cell);
      const activityCell = row.querySelector(
         ".broadcast-ai-check-activity-cell"
      );
      const activityLink = createParticipationActivityLink(cell, "");

      if(activityCell instanceof HTMLElement && activityLink)
      {
         activityCell.append(activityLink);
      }

      return row;
   }

   function createParticipationRunBlock(cell, check)
   {
      const row = document.createElement("tr");
      row.className = "broadcast-ai-check-row";

      const summaryCell = document.createElement("td");
      summaryCell.className = "broadcast-ai-check-summary-cell";

      const participantsCell = document.createElement("td");
      participantsCell.className = "broadcast-ai-check-participants-cell";

      const sourcesCell = document.createElement("td");
      sourcesCell.className = "broadcast-ai-check-sources-cell";

      const activityCell = document.createElement("td");
      activityCell.className = "broadcast-ai-check-activity-cell";

      if(check.error !== "")
      {
         const line = document.createElement("div");
         line.className = "broadcast-ai-check-line";

         const pill = document.createElement("span");
         pill.className = "status-pill status-pill-warning";
         pill.textContent = "Error";
         line.append(pill);

         const error = document.createElement("span");
         error.className = "broadcast-ai-check-error";
         error.textContent = check.error;
         summaryCell.append(line, error);

         if(check.participants.length > 0)
         {
            participantsCell.append(
               createParticipationParticipantsBlock(check.participants)
            );
         }

         const sources =
            createParticipationSourcesBlock(check.sourceUrls, check.runId);

         if(sources)
         {
            sourcesCell.append(sources);
         }

         const activityLink = createParticipationActivityLink(
            cell,
            check.runId
         );

         if(activityLink)
         {
            activityCell.append(activityLink);
         }

         const archiveForm = createParticipationArchiveForm(cell, check);

         if(archiveForm)
         {
            activityCell.append(archiveForm);
         }

         row.append(
            summaryCell,
            participantsCell,
            sourcesCell,
            activityCell
         );
         return row;
      }

      if(check.swedishParticipation === "")
      {
         const line = document.createElement("div");
         line.className = "broadcast-ai-check-line";

         const pending = document.createElement("span");
         pending.className = "broadcast-ai-check-pending";
         pending.textContent = formatParticipationStatus(check.statusId);
         line.append(pending);

         if(check.statusId === "running")
         {
            const rounds = createParticipationRoundsLabel(
               check.toolRoundCount,
               true
            );

            if(rounds)
            {
               line.append(rounds);
            }
         }

         summaryCell.append(line);

         if(check.participants.length > 0)
         {
            participantsCell.append(
               createParticipationParticipantsBlock(check.participants)
            );
         }

         const activityLink = createParticipationActivityLink(
            cell,
            check.runId
         );

         if(activityLink)
         {
            activityCell.append(activityLink);
         }

         const archiveForm = createParticipationArchiveForm(cell, check);

         if(archiveForm)
         {
            activityCell.append(archiveForm);
         }

         row.append(
            summaryCell,
            participantsCell,
            sourcesCell,
            activityCell
         );
         return row;
      }

      const result = {
         runId: check.runId,
         statusId: check.statusId,
         toolRoundCount: check.toolRoundCount,
         swedishParticipation: check.swedishParticipation,
         participants: check.participants,
         sourceUrls: check.sourceUrls
      };

      summaryCell.append(createParticipationSummaryLine(cell, result));

      const sources =
         createParticipationSourcesBlock(check.sourceUrls, check.runId);

      if(sources)
      {
         sourcesCell.append(sources);
      }

      const activityLink = createParticipationActivityLink(
         cell,
         check.runId
      );

      if(activityLink)
      {
         activityCell.append(activityLink);
      }

      const archiveForm = createParticipationArchiveForm(cell, check);

      if(archiveForm)
      {
         activityCell.append(archiveForm);
      }

      if(check.participants.length > 0)
      {
         participantsCell.append(
            createParticipationParticipantsBlock(check.participants)
         );
      }

      row.append(
         summaryCell,
         participantsCell,
         sourcesCell,
         activityCell
      );
      return row;
   }

   function createParticipationArchiveForm(cell, check)
   {
      if(!(cell instanceof HTMLElement) ||
         !check ||
         typeof check.runId !== "string" ||
         check.runId.trim() === "" ||
         (typeof check.statusId === "string" &&
            check.statusId.trim().toLowerCase() === "archived"))
      {
         return null;
      }

      const urlContainer = document.querySelector(runInlineEditUrlSelector);

      if(!(urlContainer instanceof HTMLElement) ||
         typeof urlContainer.dataset.runInlineEditUrl !== "string" ||
         urlContainer.dataset.runInlineEditUrl.trim() === "")
      {
         return null;
      }

      const form = document.createElement("form");
      form.className = "broadcast-ai-check-archive-form";
      form.method = "post";
      form.action = urlContainer.dataset.runInlineEditUrl.trim();
      form.dataset.ajaxSuccess = "update-participation";

      const token = getAntiForgeryToken();

      if(token)
      {
         const tokenInput = document.createElement("input");
         tokenInput.type = "hidden";
         tokenInput.name = "__RequestVerificationToken";
         tokenInput.value = token;
         form.append(tokenInput);
      }

      const idInput = document.createElement("input");
      idInput.type = "hidden";
      idInput.name = "id";
      idInput.value = check.runId.trim();

      const fieldInput = document.createElement("input");
      fieldInput.type = "hidden";
      fieldInput.name = "field";
      fieldInput.value = "archive";

      const button = document.createElement("button");
      button.type = "submit";
      button.className = "broadcast-ai-check-archive-link";
      button.textContent = "Archive Run";

      form.append(idInput, fieldInput, button);

      return form;
   }

   function updateParticipationRunId(cell, runId)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      if(typeof runId === "string" && runId.trim() !== "")
      {
         cell.dataset.participationRunId = runId.trim();
      }
      else
      {
         delete cell.dataset.participationRunId;
      }

      delete cell.dataset.participationQueuedFromRunId;
   }

   function getParticipationRunId(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return "";
      }

      return typeof cell.dataset.participationRunId === "string"
         ? cell.dataset.participationRunId.trim()
         : "";
   }

   function updateParticipationRowStatus(cell, statusId)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const row = cell.closest("tr");

      if(!(row instanceof HTMLElement))
      {
         return;
      }

      const normalizedStatusId = typeof statusId === "string"
         ? statusId.trim().toLowerCase()
         : "";
      const broadcastId = typeof cell.dataset.broadcastId === "string"
         ? cell.dataset.broadcastId.trim()
         : "";
      const mainRow = broadcastId === ""
         ? null
         : document.querySelector(
            `tr[data-broadcast-row='true'][data-broadcast-id='${broadcastId}']`
         );

      if(normalizedStatusId === "running"
         || normalizedStatusId === "pending")
      {
         row.dataset.participationStatus = normalizedStatusId;
         if(mainRow instanceof HTMLElement && mainRow !== row)
         {
            mainRow.dataset.participationStatus = normalizedStatusId;
            syncParticipationCheckButton(mainRow, normalizedStatusId);
         }
      }
      else
      {
         delete row.dataset.participationStatus;
         if(mainRow instanceof HTMLElement && mainRow !== row)
         {
            delete mainRow.dataset.participationStatus;
            syncParticipationCheckButton(mainRow, "");
         }
      }
   }

   function syncParticipationCheckButton(mainRow, statusId)
   {
      if(!(mainRow instanceof HTMLElement))
      {
         return;
      }

      const checkButton = mainRow.querySelector(
         "[data-check-participation-row]"
      );

      if(!(checkButton instanceof HTMLButtonElement))
      {
         return;
      }

      const normalizedStatusId = typeof statusId === "string"
         ? statusId.trim().toLowerCase()
         : "";

      checkButton.hidden = normalizedStatusId === "running";
   }

   function formatParticipationStatus(statusId)
   {
      if(typeof statusId !== "string" || statusId.trim() === "")
      {
         return "Not checked yet";
      }

      switch(statusId.trim())
      {
         case "running":
            return "Running";
         case "pending":
            return "Queued";
         case "completed":
            return "Completed";
         case "failed":
            return "Failed";
         default:
            return statusId.trim();
      }
   }

   function setPendingParticipationCell(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const isOpen = isParticipationRunsOpen(cell);
      cell.replaceChildren();
      const pendingCheck = normalizeParticipationCheckResult({
         statusId: "pending"
      });
      const { wrapper, body } = createParticipationRunsShell(
         cell,
         [pendingCheck],
         isOpen
      );
      body.append(createParticipationRunBlock(cell, pendingCheck));

      updateParticipationRowStatus(cell, "pending");
      cell.append(wrapper);
   }

   function createParticipationSummaryLine(cell, result)
   {
      const line = document.createElement("div");
      line.className = "broadcast-ai-check-line";

      const participation = typeof result.swedishParticipation === "string"
         && result.swedishParticipation.trim() !== ""
         ? result.swedishParticipation.trim()
         : "Unknown";
      const pill = document.createElement("span");
      const isPositive = participation.toLowerCase() === "yes";

      pill.className = [
         "status-pill",
         isPositive ? "status-pill-positive" : "status-pill-neutral"
      ].join(" ");
      pill.textContent = participation;
      line.append(pill);

      return line;
   }

   function createParticipationActionButton(
      cell,
      text,
      baseClass = "broadcast-ai-check-action",
      extraClass = ""
   )
   {
      if(!(cell instanceof HTMLElement))
      {
         return null;
      }

      const url = cell.dataset.checkParticipationUrl;
      const broadcastId = cell.dataset.broadcastId;

      if(!url || !broadcastId)
      {
         return null;
      }

      const button = document.createElement("button");
      button.className = ["button", baseClass, extraClass]
         .filter(value => value !== "")
         .join(" ");
      button.type = "button";
      button.textContent = text;
      button.dataset.checkParticipationRow = "true";
      button.dataset.checkParticipationUrl = url;
      button.dataset.broadcastId = broadcastId;

      return button;
   }

   function createParticipationActivityLink(cell, runId = "")
   {
      if(!(cell instanceof HTMLElement))
      {
         return null;
      }

      const activityUrlBase = typeof cell.dataset.activityUrlBase === "string"
         ? cell.dataset.activityUrlBase.trim()
         : "";

      if(activityUrlBase === "")
      {
         return null;
      }

      const url = new URL(activityUrlBase, window.location.origin);
      const normalizedRunId = typeof runId === "string"
         ? runId.trim()
         : "";

      if(normalizedRunId !== "")
      {
         url.searchParams.set(
            "participationRunId",
            normalizedRunId
         );
      }

      const link = document.createElement("a");
      link.href = `${url.pathname}${url.search}${url.hash}`;
      link.className = "ses-nowrap";
      link.textContent = "Create Activity";

      const runLink = createParticipationRunLink(runId);

      const wrapper = document.createElement("div");
      wrapper.className = "broadcast-ai-check-activity-links";
      wrapper.append(link);

      if(runLink)
      {
         wrapper.append(runLink);
      }

      return wrapper;
   }

   function createParticipationSourcesBlock(sourceUrls, runId)
   {
      if(!Array.isArray(sourceUrls))
      {
         return null;
      }

      const urls = [];
      const seen = new Set();

      sourceUrls.forEach(url => {
         if(typeof url !== "string")
         {
            return;
         }

         const trimmed = url.trim();

         if(trimmed === "" || seen.has(trimmed))
         {
            return;
         }

         seen.add(trimmed);
         urls.push(trimmed);
      });

      if(urls.length === 0)
      {
         return null;
      }

      const wrapper = document.createElement("div");
      wrapper.className = "broadcast-ai-check-sources";

      const list = document.createElement("div");
      list.className = "broadcast-ai-check-sources-list";

      urls.forEach(url => {
         const link = document.createElement("a");
         link.href = url;
         link.target = "_blank";
         link.rel = "noreferrer noopener";
         link.title = url;
         link.textContent = url;
         list.append(link);
      });

      wrapper.append(list);

      return wrapper;
   }

   function createParticipationRoundsLabel(
      toolRoundCount,
      includeZero = false
   )
   {
      if(typeof toolRoundCount !== "number"
         || !Number.isFinite(toolRoundCount)
         || toolRoundCount < 0
         || (toolRoundCount === 0 && !includeZero))
      {
         return null;
      }

      const label = document.createElement("span");
      label.className = "broadcast-ai-check-rounds";
      label.textContent = `Rounds: ${toolRoundCount}`;
      return label;
   }

   function createParticipationParticipantsBlock(participants)
   {
      if(!Array.isArray(participants) || participants.length === 0)
      {
         return null;
      }

      const names = participants
         .map(participant =>
            typeof participant === "string"
               ? {
                  name: formatParticipantName(participant),
                  editUrl: null,
                  templateEntityId: null
               }
               : normalizeParticipantItem(participant))
         .filter(participant => participant !== null);

      if(names.length === 0)
      {
         return null;
      }

      const wrapper = document.createElement("div");
      wrapper.className = "broadcast-ai-check-participants";

      names.forEach(participant => {
         wrapper.append(createParticipantRow(participant));
      });

      return wrapper;
   }

   function createParticipationErrorBlock(
      cell,
      errorMessage,
      runId,
      sourceUrls
   )
   {
      const wrapper = document.createElement("div");
      wrapper.className = "broadcast-ai-check";

      const line = document.createElement("div");
      line.className = "broadcast-ai-check-line";

      const pill = document.createElement("span");
      pill.className = "status-pill status-pill-warning";
      pill.textContent = "Error";
      line.append(pill);

      const runLink = createParticipationRunLink(runId);

      if(runLink)
      {
         line.append(runLink);
      }

      const error = document.createElement("span");
      error.className = "broadcast-ai-check-error";
      error.textContent = errorMessage;

      wrapper.append(line, error);

      const sources = createParticipationSourcesBlock(sourceUrls, runId);

      if(sources)
      {
         wrapper.append(sources);
      }

      return wrapper;
   }

   function createParticipationRunLink(runId)
   {
      if(typeof runId !== "string" || runId.trim() === "")
      {
         return null;
      }

      const link = document.createElement("a");
      link.href = `/Admin/Runs/Details/${encodeURIComponent(runId)}`;
      link.target = "_blank";
      link.rel = "noreferrer noopener";
      link.textContent = "View Run";
      return link;
   }

   function createParticipationErrorMessage(
      statusCode,
      payloadError,
      responseText
   )
   {
      const parts = [
         `Participation check failed (HTTP ${statusCode}).`
      ];

      if(typeof payloadError === "string" && payloadError.trim() !== "")
      {
         parts.push(payloadError.trim());
      }

      const preview = createResponsePreview(responseText);

      if(preview !== "")
      {
         parts.push(`Response: ${preview}`);
      }

      return parts.join(" ");
   }

   function initializeEntityInlineEditing(root = document)
   {
      if(root === document
         && document.documentElement.dataset.entityInlineEditingInitialized
            === "true")
      {
         return;
      }

      if(root === document)
      {
         document.documentElement.dataset
            .entityInlineEditingInitialized = "true";

         const handleInlineEditActivation = event => {
            if(event.type === "click" && !isTouchEditInteraction())
            {
               return;
            }

            const target = event.target;

            if(!(target instanceof Element))
            {
               return;
            }

            if(target.closest("a,button,input,textarea,select,label"))
            {
               return;
            }

            const entityCell = target.closest(entityInlineEditCellSelector);

            if(entityCell instanceof HTMLElement)
            {
               event.preventDefault();
               openEntityInlineEditCell(entityCell);
            }
         };

         document.addEventListener("dblclick", handleInlineEditActivation);
         document.addEventListener("click", handleInlineEditActivation);
      }

      root.querySelectorAll(entityInlineEditInputSelector).forEach(input => {
         initializeEntityInlineEditInput(input);
      });
   }

   function initializeEntityInlineEditInput(input)
   {
      if(!(input instanceof HTMLSelectElement)
         || input.dataset.entityInlineEditInitialized === "true")
      {
         return;
      }

      input.dataset.entityInlineEditInitialized = "true";

      input.addEventListener("change", () => {
         void saveEntityInlineEditAsync(input);
      });

      input.addEventListener("blur", () => {
         void saveEntityInlineEditAsync(input);
      });

      input.addEventListener("keydown", event => {
         if(event.key === "Escape")
         {
            event.preventDefault();
            cancelEntityInlineEdit(input);
         }
      });
   }

   function openEntityInlineEditCell(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const input = cell.querySelector(entityInlineEditInputSelector);
      const display = cell.querySelector(entityInlineEditDisplaySelector);

      if(!(input instanceof HTMLSelectElement)
         || !(display instanceof HTMLElement)
         || input.hidden === false)
      {
         return;
      }

      if(input.dataset.entityInlineEditSaving === "true")
      {
         return;
      }

      input.dataset.entityInlineEditOriginalValue = input.value;
      cell.dataset.entityInlineEditing = "true";
      display.hidden = true;
      input.hidden = false;

      window.requestAnimationFrame(() => {
         input.focus();
      });
   }

   async function saveEntityInlineEditAsync(input)
   {
      if(!(input instanceof HTMLSelectElement)
         || input.hidden
         || input.dataset.entityInlineEditSaving === "true")
      {
         return;
      }

      const cell = input.closest(entityInlineEditCellSelector);
      const url = getEntityInlineEditUrl();
      const entityId = (cell?.closest("tr")?.dataset.entityRowId ?? "").trim();
      const field = (cell?.dataset.entityInlineEditField ?? "").trim();
      const currentValue = input.value.trim();
      const originalValue = (
         input.dataset.entityInlineEditOriginalValue ?? ""
      ).trim();

      if(!(cell instanceof HTMLElement)
         || url === ""
         || entityId === ""
         || field === "")
      {
         return;
      }

      if(currentValue === originalValue)
      {
         restoreEntityInlineEditInput(input);
         return;
      }

      input.dataset.entityInlineEditSaving = "true";
      input.disabled = true;

      try
      {
         const payload = await postEntityInlineEditAsync(
            url,
            entityId,
            field,
            currentValue
         );

         updateEntityInlineEditCell(cell, payload);
         restoreEntityInlineEditInput(input);
      }
      catch(error)
      {
         window.alert(
            error instanceof Error
               ? error.message
               : "Entity update failed."
         );
         input.hidden = false;
         window.requestAnimationFrame(() => {
            input.focus();
         });
      }
      finally
      {
         input.disabled = false;
         delete input.dataset.entityInlineEditSaving;
      }
   }

   function cancelEntityInlineEdit(input)
   {
      if(!(input instanceof HTMLSelectElement))
      {
         return;
      }

      const originalValue = (
         input.dataset.entityInlineEditOriginalValue ?? input.value
      ).trim();

      input.value = originalValue;
      restoreEntityInlineEditInput(input);
   }

   function restoreEntityInlineEditInput(input)
   {
      if(!(input instanceof HTMLSelectElement))
      {
         return;
      }

      const cell = input.closest(entityInlineEditCellSelector);
      const display = cell?.querySelector(entityInlineEditDisplaySelector);

      if(display instanceof HTMLElement)
      {
         display.hidden = false;
      }

      input.hidden = true;

      if(cell instanceof HTMLElement)
      {
         delete cell.dataset.entityInlineEditing;
      }
   }


   function createParticipantRow(participant)
   {
      const item = normalizeParticipantItem(participant);

      if(item === null)
      {
         return document.createElement("div");
      }

      const row = document.createElement("div");
      row.className = "broadcast-ai-check-participant-row";
      row.append(createParticipantInlineNode(item));

      const createForm = createParticipantCreateForm(item);

      if(createForm)
      {
         row.append(createForm);
      }

      return row;
   }

   function createParticipantInlineNode(participant)
   {
      const item = normalizeParticipantItem(participant);

      if(item === null)
      {
         const span = document.createElement("span");
         span.textContent = "";
         return span;
      }

      if(item.editUrl !== null)
      {
         const anchor = document.createElement("a");
         anchor.href = item.editUrl;
         anchor.textContent = item.name;
         anchor.className = "broadcast-ai-check-participant-link";
         anchor.title = "Edit entity";
         anchor.target = "_blank";
         anchor.rel = "noreferrer noopener";
         return anchor;
      }

      const span = document.createElement("span");
      span.textContent = item.name;
      return span;
   }

   function createParticipantCreateForm(participant)
   {
      const item = normalizeParticipantItem(participant);

      if(item === null ||
         item.editUrl !== null ||
         item.templateEntityId === null)
      {
         return null;
      }

      const form = document.createElement("form");
      form.method = "post";
      form.action = getParticipantCreateUrl();
      form.dataset.ajaxSuccess = "replace";
      form.className = "broadcast-ai-check-participant-create-form";

      const token = getAntiForgeryToken();

      if(token !== "")
      {
         const tokenInput = document.createElement("input");
         tokenInput.type = "hidden";
         tokenInput.name = "__RequestVerificationToken";
         tokenInput.value = token;
         form.append(tokenInput);
      }

      const nameInput = document.createElement("input");
      nameInput.type = "hidden";
      nameInput.name = "participantName";
      nameInput.value = item.name;

      const templateInput = document.createElement("input");
      templateInput.type = "hidden";
      templateInput.name = "templateEntityId";
      templateInput.value = item.templateEntityId;

      const button = document.createElement("button");
      button.type = "submit";
      button.className = "broadcast-ai-check-participant-create-button";
      button.textContent = "+";
      button.title = "Create entity";
      button.setAttribute("aria-label", `Create entity for ${item.name}`);
      button.tabIndex = -1;

      form.append(nameInput, templateInput, button);
      return form;
   }

   function normalizeParticipantItem(participant)
   {
      if(!(participant && typeof participant === "object"))
      {
         return null;
      }

      const name = typeof participant.Name === "string"
         ? formatParticipantName(participant.Name)
         : typeof participant.name === "string"
            ? formatParticipantName(participant.name)
            : "";
      const editUrl = typeof participant.EditUrl === "string"
         && participant.EditUrl.trim() !== ""
         ? participant.EditUrl.trim()
         : typeof participant.editUrl === "string"
            && participant.editUrl.trim() !== ""
            ? participant.editUrl.trim()
            : null;
      const templateEntityId = typeof participant.TemplateEntityId === "string"
         && participant.TemplateEntityId.trim() !== ""
         ? participant.TemplateEntityId.trim()
         : typeof participant.templateEntityId === "string"
            && participant.templateEntityId.trim() !== ""
            ? participant.templateEntityId.trim()
            : null;

      return name === ""
         ? null
         : { name, editUrl, templateEntityId };
   }

   function formatParticipantName(value)
   {
      if(typeof value !== "string")
      {
         return "";
      }

      return value.trim()
         .replace(/\s+/gu, " ")
         .replace(/\p{L}+/gu, word => isShoutedParticipantWord(word)
            ? word.toLocaleLowerCase("en-US")
               .replace(/^\p{L}/u, first => first.toLocaleUpperCase("en-US"))
            : word);
   }

   function isShoutedParticipantWord(value)
   {
      const letters = Array.from(value)
         .filter(character => /\p{L}/u.test(character));

      return letters.length >= 2
         && letters.every(character =>
            character === character.toLocaleUpperCase("en-US"));
   }

   function isValidParticipantItem(participant)
   {
      return normalizeParticipantItem(participant) !== null;
   }

   function getParticipantCreateUrl()
   {
      const container = document.querySelector(
         participantCreateUrlSelector
      );

      if(!(container instanceof HTMLElement))
      {
         return window.location.href;
      }

      const url = container.dataset.createParticipantUrl;

      return typeof url === "string" && url.trim() !== ""
         ? url.trim()
         : window.location.href;
   }

   function createResponsePreview(responseText)
   {
      const preview = responseText
         .replace(/\s+/g, " ")
         .trim();

      if(preview === "")
      {
         return "";
      }

      if(preview.length <= 220)
      {
         return preview;
      }

      return `${preview.slice(0, 220)}...`;
   }

   async function generateTeaserAsync(button)
   {
      const form = button.form;
      const url = button.dataset.teaserUrl;
      const output = form?.querySelector("[data-teaser-output]");
      const status = form?.querySelector("[data-teaser-status]");

      if(!form || !url || !(output instanceof HTMLTextAreaElement))
      {
         return;
      }

      setTeaserStatus(status, "Queueing teaser job...");
      button.disabled = true;

      try
      {
         const response = await fetch(url, {
            method: "post",
            body: new FormData(form),
            headers: {
               Accept: "application/json"
            }
         });
         const payload = await response.json();

         if(!response.ok)
         {
            throw new Error(payload.error || "Teaser generation failed.");
         }

         const runId = typeof payload.runId === "string"
            ? payload.runId
            : "";
         const message = runId === ""
            ? "Teaser job queued."
            : `Teaser job queued: ${runId}`;

         setTeaserStatus(status, message);
      }
      catch(error)
      {
         const message = error instanceof Error
            ? error.message
            : "Teaser generation failed.";

         setTeaserStatus(status, message, true);
      }
      finally
      {
         button.disabled = false;
      }
   }

   async function findFactsAsync(button)
   {
      const form = button.form;
      const url = button.dataset.factsUrl;
      const output = form?.querySelector("[data-facts-output]");
      const status = form?.querySelector("[data-facts-status]");

      if(!form || !url || !(output instanceof HTMLTextAreaElement))
      {
         return;
      }

      setTeaserStatus(status, "Queueing facts job...");
      button.disabled = true;

      try
      {
         const response = await fetch(url, {
            method: "post",
            body: new FormData(form),
            headers: {
               Accept: "application/json"
            }
         });
         const payload = await response.json();

         if(!response.ok)
         {
            throw new Error(payload.error || "Finding facts failed.");
         }

         const runId = typeof payload.runId === "string"
            ? payload.runId
            : "";
         const message = runId === ""
            ? "Facts job queued."
            : `Facts job queued: ${runId}`;

         setTeaserStatus(status, message);
      }
      catch(error)
      {
         const message = error instanceof Error
            ? error.message
            : "Finding facts failed.";

         setTeaserStatus(status, message, true);
      }
      finally
      {
         button.disabled = false;
      }
   }

   function setTeaserStatus(status, message, isError = false)
   {
      if(!(status instanceof HTMLElement))
      {
         return;
      }

      status.textContent = message;
      status.classList.toggle("form-status-error", isError);
   }

   function updateCheckboxVisibility(target)
   {
      const checkboxes = getCheckboxesForGroup(
         target.dataset.visibleWhenCheckboxGroup
      );
      const hasSelection = checkboxes.some(checkbox => checkbox.checked);

      target.hidden = !hasSelection;
   }

   function getCheckboxGroup(toggle)
   {
      const groupName = toggle.dataset.checkboxToggle;

      return getCheckboxesForGroup(groupName);
   }

   function getCheckboxesForGroup(groupName)
   {
      if(!groupName)
      {
         return [];
      }

      return Array
         .from(document.querySelectorAll("[data-checkbox-group]"))
         .filter(checkbox => checkbox instanceof HTMLInputElement)
         .filter(checkbox => checkbox.type === "checkbox")
         .filter(checkbox => checkbox.dataset.checkboxGroup === groupName)
         .filter(checkbox => !checkbox.disabled);
   }

   function updateCheckboxToggle(toggle)
   {
      const checkboxes = getCheckboxGroup(toggle);
      const allSelected = checkboxes.length > 0
         && checkboxes.every(checkbox => checkbox.checked);
      const label = allSelected
         ? toggle.dataset.unselectLabel
         : toggle.dataset.selectLabel;

      toggle.textContent = label
         || (allSelected ? "Unselect all" : "Select all");
      toggle.disabled = checkboxes.length === 0;
   }

   function refreshCheckboxControls(root = document)
   {
      root.querySelectorAll(checkboxToggleSelector).forEach(toggle => {
         updateCheckboxToggle(toggle);
      });

      root.querySelectorAll(checkboxVisibilitySelector).forEach(target => {
         updateCheckboxVisibility(target);
      });
   }

   function submitFilterForm(field)
   {
      normalizeExclusiveEmptyOption(field);
      field.form?.requestSubmit();
   }

   async function replaceFromFormAsync(form)
   {
      const targetSelector = form.dataset.ajaxReplaceTarget;

      if(!targetSelector)
      {
         HTMLFormElement.prototype.submit.call(form);
         return;
      }

      const target = document.querySelector(targetSelector);

      if(!target)
      {
         HTMLFormElement.prototype.submit.call(form);
         return;
      }

      try
      {
         const url = getFormUrl(form);
         const response = await fetch(url, {
            headers: {
               Accept: "text/html"
            }
         });

         if(!response.ok)
         {
            throw new Error(`Request failed with status ${response.status}`);
         }

         const documentText = await response.text();
         const parser = new DOMParser();
         const nextDocument = parser.parseFromString(
            documentText,
            "text/html"
         );
         const nextTarget = nextDocument.querySelector(targetSelector);

         if(!nextTarget)
         {
            throw new Error("Replacement target was not found.");
         }

         target.replaceWith(nextTarget);
         initializeCheckboxToggles(nextTarget);
         initializeCheckboxVisibility(nextTarget);
         initializeTeaserGeneration(nextTarget);
         initializeParticipationRowChecks(nextTarget);
         void initializeParticipationRunsAsync(nextTarget);
         initializeBroadcastInlineEditing(nextTarget);
         window.initializeBroadcastOrganizationAutocomplete?.(nextTarget);
         initializeParticipationPolling(nextTarget);
         history.replaceState(null, "", url);
      }
      catch
      {
         HTMLFormElement.prototype.submit.call(form);
      }
   }

   async function replaceTargetFromFormAsync(form)
   {
      const targetSelector = (form.dataset.ajaxReplaceTarget ?? "").trim();

      if(targetSelector === "")
      {
         HTMLFormElement.prototype.submit.call(form);
         return;
      }

      const target = document.querySelector(targetSelector);

      if(!(target instanceof HTMLElement))
      {
         HTMLFormElement.prototype.submit.call(form);
         return;
      }

      const openBroadcastIds = captureOpenBroadcastIds(target);

      try
      {
         const response = await fetch(form.action, {
            method: form.method || "post",
            body: new FormData(form),
            headers: {
               Accept: "text/html"
            }
         });

         if(!response.ok)
         {
            throw new Error(`Request failed with status ${response.status}`);
         }

         const documentText = await response.text();
         const parser = new DOMParser();
         const nextDocument = parser.parseFromString(
            documentText,
            "text/html"
         );
         const nextTarget = nextDocument.querySelector(targetSelector);

         if(!(nextTarget instanceof HTMLElement))
         {
            throw new Error("Replacement target was not found.");
         }

         target.replaceWith(nextTarget);
         initializeCheckboxToggles(nextTarget);
         initializeCheckboxVisibility(nextTarget);
         initializeTeaserGeneration(nextTarget);
         initializeParticipationRowChecks(nextTarget);
         void initializeParticipationRunsAsync(nextTarget);
         initializeBroadcastInlineEditing(nextTarget);
         window.initializeBroadcastOrganizationAutocomplete?.(nextTarget);
         initializeParticipationPolling(nextTarget);
         restoreExpandedBroadcastRows(nextTarget, openBroadcastIds);
      }
      catch
      {
         HTMLFormElement.prototype.submit.call(form);
      }
   }

   function captureOpenBroadcastIds(root)
   {
      if(!(root instanceof HTMLElement))
      {
         return [];
      }

      const ids = [];

      root.querySelectorAll(".broadcast-participation-runs-row").forEach(
         row => {
            if(!(row instanceof HTMLElement))
            {
               return;
            }

            const table = row.querySelector(
               ".broadcast-ai-check-runs-table"
            );
            const broadcastId = (row.dataset.broadcastId ?? "").trim();

            if(!(table instanceof HTMLTableElement)
               || table.dataset.participationRunsOpen !== "true"
               || broadcastId === "")
            {
               return;
            }

            ids.push(broadcastId);
         }
      );

      return ids;
   }

   function restoreExpandedBroadcastRows(root, broadcastIds)
   {
      if(!(root instanceof HTMLElement) || broadcastIds.length === 0)
      {
         return;
      }

      root.querySelectorAll(".broadcast-participation-runs-row").forEach(
         row => {
            if(!(row instanceof HTMLElement))
            {
               return;
            }

            const broadcastId = (row.dataset.broadcastId ?? "").trim();

            if(!broadcastIds.includes(broadcastId))
            {
               return;
            }

            const toggleButton = row.querySelector(
               "[data-participation-runs-toggle]"
            );

            if(toggleButton instanceof HTMLButtonElement &&
               toggleButton.getAttribute("aria-expanded") !== "true")
            {
               toggleParticipationRuns(toggleButton);
            }
         }
      );
   }

   async function replaceParticipantCreateFormAsync(form, response)
   {
      if(!(form instanceof HTMLFormElement))
      {
         return;
      }

      if(!(response instanceof Response))
      {
         return;
      }

      let payload = null;

      try
      {
         payload = await response.clone().json();
      }
      catch
      {
         return;
      }

      const editUrl = typeof payload.editUrl === "string"
         ? payload.editUrl.trim()
         : "";
      const canonicalName = typeof payload.canonicalName === "string"
         ? payload.canonicalName.trim()
         : "";

      if(editUrl === "" || canonicalName === "")
      {
         return;
      }

      const row = form.closest(".broadcast-ai-check-participant-row");

      if(!(row instanceof HTMLElement))
      {
         return;
      }

      const link = document.createElement("a");
      link.className = "broadcast-ai-check-participant-link";
      link.href = editUrl;
      link.target = "_blank";
      link.rel = "noreferrer noopener";
      link.title = "Edit entity";
      link.textContent = canonicalName;

      const nameNode = row.firstElementChild;

      if(nameNode instanceof Node)
      {
         row.replaceChild(link, nameNode);
      }
      else
      {
         row.prepend(link);
      }

      form.remove();
   }

   function getFormUrl(form)
   {
      const url = new URL(form.action || window.location.href);

      if((form.method || "get").toLowerCase() !== "get")
      {
         return url;
      }

      url.search = new URLSearchParams(new FormData(form)).toString();
      return url;
   }

   function initializeExclusiveEmptySelects(root = document)
   {
      root
         .querySelectorAll(exclusiveEmptySelectSelector)
         .forEach(field => {
            if(field instanceof HTMLSelectElement)
            {
               rememberExclusiveEmptySelection(field);
            }
         });
   }

   function normalizeExclusiveEmptyOption(field)
   {
      if(!(field instanceof HTMLSelectElement)
         || field.dataset.emptyOption !== "exclusive")
      {
         return;
      }

      const options = Array.from(field.options);
      const emptyOption = options.find(option => option.value === "");

      if(!emptyOption)
      {
         return;
      }

      const previousSelection = exclusiveEmptySelectStates.get(field) ?? [];
      const hadEmptySelection = previousSelection.includes("");
      const specificOptions = options.filter(option => option.value !== "");
      const selectedSpecificOptions = specificOptions.filter(option =>
         option.selected
      );

      if(emptyOption.selected
         && selectedSpecificOptions.length > 0
         && !hadEmptySelection)
      {
         selectedSpecificOptions.forEach(option => {
            option.selected = false;
         });
      }
      else if(selectedSpecificOptions.length > 0)
      {
         emptyOption.selected = false;
      }
      else
      {
         emptyOption.selected = true;
      }

      rememberExclusiveEmptySelection(field);
   }

   function rememberExclusiveEmptySelection(field)
   {
      exclusiveEmptySelectStates.set(
         field,
         Array
            .from(field.selectedOptions)
            .map(option => option.value)
      );
   }

   function initializeMultiSelectClearButtons(root = document)
   {
      root.querySelectorAll("[data-multi-select-clear]").forEach(button => {
         if(!(button instanceof HTMLButtonElement)
            || button.dataset.multiSelectClearInitialized === "true")
         {
            return;
         }

         const container = button.closest("label, .multi-select-row");
         const select = container?.querySelector("select[data-multi-select]");

         if(!(select instanceof HTMLSelectElement))
         {
            return;
         }

         button.dataset.multiSelectClearInitialized = "true";

         const update = () => {
            button.disabled = select.selectedOptions.length === 0;
         };

         button.addEventListener("click", () => {
            select._multiSelect?.deselectAll();
            update();
         });

         select.addEventListener("change", update);
         update();
      });
   }

   function initializeMultiSelectScrollRetention()
   {
      if(document.documentElement.dataset
         .multiSelectScrollRetentionInitialized === "true")
      {
         return;
      }

      document.documentElement.dataset
         .multiSelectScrollRetentionInitialized = "true";

      document.addEventListener(
         "pointerdown",
         rememberMultiSelectScroll,
         true
      );
      document.addEventListener(
         "click",
         preserveMultiSelectScroll,
         true
      );
      document.addEventListener(
         "focusin",
         preserveMultiSelectScroll,
         true
      );
      document.addEventListener(
         "change",
         preserveMultiSelectScroll,
         true
      );
   }

   function rememberMultiSelectScroll(event)
   {
      const options = getMultiSelectOptionsForEvent(event);

      if(options instanceof HTMLElement)
      {
         multiSelectScrollPositions.set(options, options.scrollTop);
      }
   }

   function preserveMultiSelectScroll(event)
   {
      const options = getMultiSelectOptionsForEvent(event);

      if(!(options instanceof HTMLElement))
      {
         return;
      }

      const scrollTop = multiSelectScrollPositions.get(options)
         ?? options.scrollTop;

      if(scrollTop <= 0)
      {
         return;
      }

      const restore = () => {
         if(options.isConnected)
         {
            options.scrollTop = scrollTop;
         }
      };

      queueMicrotask(restore);
      window.requestAnimationFrame(restore);
      window.setTimeout(restore, 0);
      window.setTimeout(restore, 25);
      window.setTimeout(restore, 100);
   }

   function getMultiSelectOptionsForEvent(event)
   {
      const target = event.target;

      if(!(target instanceof Element))
      {
         return null;
      }

      const directOptions = target.closest(".multi-select-options");

      if(directOptions instanceof HTMLElement)
      {
         return directOptions;
      }

      const multiSelect = target.closest(".multi-select");
      const options = multiSelect?.querySelector(".multi-select-options");

      return options instanceof HTMLElement ? options : null;
   }
})();
