(() => {
   const enhancedFormSelector =
      "form[data-ajax-success]:not([data-ajax-success=''])";
   const replacementFormSelector = "form[data-ajax-replace-target]";
   const checkboxToggleSelector = "[data-checkbox-toggle]";
   const checkboxVisibilitySelector = "[data-visible-when-checkbox-group]";
   const entityTypeSelectSelector = "[data-entity-type-select]";
   const personGenderFieldSelector = "[data-person-gender-field]";
   const personBirthdateFieldSelector =
      "[data-person-birthdate-field]";
   const personHeightFieldSelector = "[data-person-height-field]";
   const personWeightFieldSelector = "[data-person-weight-field]";
   const personFormativeClubFieldSelector =
      "[data-person-formative-club-field]";
   const entityInlineEditUrlSelector = "[data-entity-inline-edit-url]";
   const entityInlineEditCellSelector =
      "[data-entity-inline-edit-field]";
   const entityInlineEditDisplaySelector =
      "[data-entity-inline-edit-display]";
   const entityInlineEditInputSelector =
      "[data-entity-inline-edit-input]";
   const generateTeaserSelector = "[data-generate-teaser]";
   const findFactsSelector = "[data-find-facts]";
   const activityStartCheckSelector =
      "[data-activity-start-check]";
   const activityResultCheckSelector =
      "[data-activity-result-check]";
   const activityFactsCheckSelector =
      "[data-activity-facts-check]";
   const checkParticipationRowSelector =
      "[data-check-participation-row]";
   const participationRunsToggleSelector =
      "[data-participation-runs-toggle]";
   const participationCellSelector = "[data-participation-cell]";
   const participationStatusUrlSelector =
      "[data-check-participation-status-url]";
   const adminDateInputSelector = "input[type='date']";
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
   const activityAiResultInlineEditUrlSelector =
      "[data-ai-result-edit-url]";
   const activityAiResultInlineEditCellSelector =
      "[data-ai-result-edit-field]";
   const activityAiResultInlineEditDisplaySelector =
      "[data-ai-result-edit-display]";
   const activityAiResultInlineEditInputSelector =
      "[data-ai-result-edit-input]";
   const activityAiResultInlineEditField = "value";
   const activityAiResultInlineEditDefaultPlaceholder = "Add value..";
   const broadcastInlineEditCellSelector =
      "[data-broadcast-inline-edit-field]";
   const broadcastInlineEditUrlSelector =
      "[data-broadcast-inline-edit-url]";
   const broadcastResultsSelector = "[data-broadcast-results]";
   const broadcastRowSelector = "tr[data-broadcast-row='true']";
   const broadcastRunsRowSelector =
      ".broadcast-participation-runs-row";
   const broadcastGroupParticipantsClearSelector =
      "[data-broadcast-group-participants-clear]";
   const broadcastActivityLinkSelector =
      "[data-broadcast-activity-link]";
   const clearParticipantsQueryKey = "clearParticipants";
   const broadcastInlineEditTitleField = "title";
   const getBroadcastInlineEditUrl =
      window.getBroadcastInlineEditUrl;
   const postBroadcastInlineEditAsync =
      window.postBroadcastInlineEditAsync;
   const getAntiForgeryToken = window.getAntiForgeryToken;
   const pendingParticipationIds = new Set();
   const queuingParticipationIds = new Set();
   const pendingRunIds = new Set();
   let participationPollingTimer = null;
   let participationPollingInFlight = false;
   let runPollingTimer = null;
   let runPollingInFlight = false;
   const getFormSelector = "form[method='get']";
   const exclusiveEmptySelectSelector = "select[data-empty-option='exclusive']";
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
   initializeAdminDateSteppers();
   initializeEntityInlineEditing();
   window.initializeEntityInlineEditing = initializeEntityInlineEditing;
   window.initializeBroadcastInlineEditing =
      initializeBroadcastInlineEditing;
   window.initializeParticipationRunsAsync =
      initializeParticipationRunsAsync;
   initializeTeaserGeneration();
   initializeActivityStartChecks();
   initializeActivityResultChecks();
   initializeActivityFactsChecks();
   initializeParticipationRowChecks();
   initializeBroadcastParticipantClearing();
   void initializeParticipationRunsAsync();
   initializeBroadcastInlineEditing();
   if(typeof window.initializeBroadcastOrganizationAutocomplete === "function")
   {
      window.initializeBroadcastOrganizationAutocomplete();
   }
   initializeParticipationPolling();
   initializeRunPolling();
   initializeRunInlineEditing();
   initializeActivityAiResultInlineEditing();

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
         const expectsHtml = form.dataset.ajaxSuccess ===
            "update-participation"
            || form.dataset.ajaxSuccess === "replace";
         const response = await fetch(form.action, {
            method: form.method || "post",
            body: new FormData(form),
            headers: {
               Accept: expectsHtml ? "text/html" : "application/json"
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

   document.addEventListener("change", event => {
      const select = event.target;

      if(!(select instanceof HTMLSelectElement)
         || !select.matches("[data-broadcast-participant-template-select]"))
      {
         return;
      }

      const participants = select.closest(
         ".broadcast-ai-check-participants"
      );

      if(!(participants instanceof HTMLElement))
      {
         return;
      }

      participants.querySelectorAll(
         ".broadcast-ai-check-participant-template-input"
      ).forEach(input => {
         if(input instanceof HTMLInputElement)
         {
            input.value = select.value;
         }
      });
   });

   async function updateParticipationFromResponseAsync(response)
   {
      if(!(response instanceof Response))
      {
         return;
      }

      const html = await response.text();
      replaceParticipationCellsFromHtml(html);
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
      const container = target?.closest("tbody[data-broadcast-container]");
      const showHidden = isBroadcastShowHiddenEnabled(form);
      const preserveScroll = form.dataset.ajaxPreserveScroll === "true";
      const scrollX = preserveScroll ? window.scrollX : 0;
      const scrollY = preserveScroll ? window.scrollY : 0;
      const rowHtml = await fetchBroadcastRowAsync(form);

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

      if(!(container instanceof HTMLElement) || !rowHtml)
      {
         return;
      }

      let nextContainer;

      try
      {
         nextContainer = window.replaceElementWithPartialHtml(
            container,
            rowHtml
         );
      }
      catch
      {
         return;
      }

      const nextMainRow = nextContainer.querySelector(broadcastRowSelector);

      initializeBroadcastInlineEditing(nextContainer);
      window.initializeBroadcastOrganizationAutocomplete?.(nextContainer);
      initializeParticipationRunsAsync(nextContainer);
      initializeParticipationPolling(nextContainer);

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
               Accept: "text/html"
            }
         });

         if(!response.ok)
         {
            throw new Error(`Request failed with status ${response.status}`);
         }

         return await response.text();
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

   function removeBroadcastRowPair(target)
   {
      if(!(target instanceof HTMLElement))
      {
         return;
      }

      const container = target.closest("tbody[data-broadcast-container]");

      if(container instanceof HTMLElement)
      {
         const broadcastId = (target.dataset.broadcastId ?? "").trim();
         container.remove();

         if(broadcastId !== "")
         {
            pendingParticipationIds.delete(broadcastId);
         }

         if(pendingParticipationIds.size === 0)
         {
            stopParticipationPolling();
         }

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
         const birthdateField = form?.querySelector(
            personBirthdateFieldSelector
         );
         const heightField = form?.querySelector(personHeightFieldSelector);
         const weightField = form?.querySelector(personWeightFieldSelector);
         const formativeClubField = form?.querySelector(
            personFormativeClubFieldSelector
         );

         if(!(genderField instanceof HTMLElement)
            || !(birthdateField instanceof HTMLElement)
            || !(heightField instanceof HTMLElement)
            || !(weightField instanceof HTMLElement)
            || !(formativeClubField instanceof HTMLElement))
         {
            return;
         }

         select.dataset.personGenderVisibilityInitialized = "true";

         const update = () => {
            const isPerson = select.value.trim().toLowerCase() === "person";
            genderField.hidden = !isPerson;
            birthdateField.hidden = !isPerson;
            heightField.hidden = !isPerson;
            weightField.hidden = !isPerson;
            formativeClubField.hidden = !isPerson;
         };

         select.addEventListener("change", update);
         update();
      });
   }

   function initializeGetFormRestoration()
   {
      const restore = () => {
         document.querySelectorAll(getFormSelector).forEach(form => {
            if(form instanceof HTMLFormElement &&
               !form.hasAttribute("data-preserve-get-form-state"))
            {
               form.reset();
            }
         });
      };

      window.addEventListener("pageshow", restore);
      restore();
   }

   function initializeAdminDateSteppers(root = document)
   {
      root.querySelectorAll(adminDateInputSelector).forEach(input => {
         if(!(input instanceof HTMLInputElement)
            || input.dataset.adminDateStepperInitialized === "true"
            || input.readOnly
            || input.disabled)
         {
            return;
         }

         const label = input.closest("label");

         if(label instanceof HTMLLabelElement)
         {
            label.classList.add("admin-date-field");
         }

         const stepper = input.nextElementSibling;
         const previousButton = stepper?.querySelector(
            "[data-admin-date-step='-1']"
         );
         const clearButton = stepper?.querySelector(
            "[data-admin-date-clear]"
         );
         const nextButton = stepper?.querySelector(
            "[data-admin-date-step='1']"
         );

         if(!(stepper instanceof HTMLElement)
            || !(previousButton instanceof HTMLButtonElement)
            || !(clearButton instanceof HTMLButtonElement)
            || !(nextButton instanceof HTMLButtonElement))
         {
            return;
         }

         input.dataset.adminDateStepperInitialized = "true";

         previousButton.addEventListener("click", () => {
            shiftAdminDateInput(input, -1);
         });
         clearButton.addEventListener("click", () => {
            clearAdminDateInput(input);
         });
         nextButton.addEventListener("click", () => {
            shiftAdminDateInput(input, 1);
         });

      });
   }

   function clearAdminDateInput(input)
   {
      input.value = "";
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
   }

   function shiftAdminDateInput(input, dayOffset)
   {
      const nextDate = getAdminDateOffset(input.value, dayOffset);

      if(nextDate === null)
      {
         return;
      }

      input.value = nextDate;
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
   }

   function getAdminDateOffset(value, dayOffset)
   {
      const parsedDate = parseAdminDateValue(value);

      if(parsedDate instanceof Date)
      {
         parsedDate.setDate(parsedDate.getDate() + dayOffset);
         return formatAdminDateValue(parsedDate);
      }

      return formatAdminDateValue(getAdminDateRelativeToToday(dayOffset));
   }

   function parseAdminDateValue(value)
   {
      const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(
         value.trim()
      );

      if(match === null)
      {
         return null;
      }

      const year = Number.parseInt(match[1], 10);
      const month = Number.parseInt(match[2], 10);
      const day = Number.parseInt(match[3], 10);
      const parsedDate = new Date(year, month - 1, day, 12);

      if(parsedDate.getFullYear() !== year
         || parsedDate.getMonth() !== month - 1
         || parsedDate.getDate() !== day)
      {
         return null;
      }

      return parsedDate;
   }

   function getAdminDateRelativeToToday(dayOffset)
   {
      const today = new Date();

      today.setHours(12, 0, 0, 0);
      today.setDate(today.getDate() + dayOffset);

      return today;
   }

   function formatAdminDateValue(date)
   {
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, "0");
      const day = String(date.getDate()).padStart(2, "0");

      return `${year}-${month}-${day}`;
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

   function initializeActivityStartChecks()
   {
      if(document.documentElement.dataset.activityStartChecksInitialized
         === "true")
      {
         return;
      }

      document.documentElement.dataset.activityStartChecksInitialized =
         "true";

      document.addEventListener("submit", async event => {
         const form = event.target;

         if(!(form instanceof HTMLFormElement)
            || !form.matches(activityStartCheckSelector))
         {
            return;
         }

         event.preventDefault();

         const button = form.querySelector("button[type='submit']");
         const status = form.querySelector("[data-start-check-status]");
         const url = button instanceof HTMLButtonElement
            ? button.dataset.startUrl
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
               throw new Error(
                  payload.error || "Start time check failed."
               );
            }

            const runId = typeof payload.runId === "string"
               ? payload.runId.trim()
               : "";

            status.textContent = runId === ""
               ? "Queued"
               : `Queued: ${runId}`;
         }
         catch(error)
         {
            status.textContent = error instanceof Error
               ? error.message
               : "Start time check failed.";
            status.classList.add("form-status-error");
         }
         finally
         {
            button.disabled = false;
         }
      });
   }

   function initializeActivityResultChecks()
   {
      if(document.documentElement.dataset.activityResultChecksInitialized
         === "true")
      {
         return;
      }

      document.documentElement.dataset.activityResultChecksInitialized =
         "true";

      document.addEventListener("submit", async event => {
         const form = event.target;

         if(!(form instanceof HTMLFormElement)
            || !form.matches(activityResultCheckSelector))
         {
            return;
         }

         event.preventDefault();

         const button = form.querySelector("button[type='submit']");
         const status = form.querySelector("[data-result-check-status]");
         const url = button instanceof HTMLButtonElement
            ? button.dataset.resultUrl
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
               throw new Error(
                  payload.error || "Result check failed."
               );
            }

            const runId = typeof payload.runId === "string"
               ? payload.runId.trim()
               : "";

            status.textContent = runId === ""
               ? "Queued"
               : `Queued: ${runId}`;
         }
         catch(error)
         {
            status.textContent = error instanceof Error
               ? error.message
               : "Result check failed.";
            status.classList.add("form-status-error");
         }
         finally
         {
            button.disabled = false;
         }
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

   function initializeBroadcastParticipantClearing(root = document)
   {
      if(root !== document
         || document.documentElement.dataset
            .broadcastParticipantClearingInitialized === "true")
      {
         return;
      }

      document.documentElement.dataset
         .broadcastParticipantClearingInitialized = "true";

      document.addEventListener("click", event => {
         const target = event.target;

         if(!(target instanceof Element))
         {
            return;
         }

         const button = target.closest(
            broadcastGroupParticipantsClearSelector
         );

         if(!(button instanceof HTMLButtonElement))
         {
            return;
         }

         event.preventDefault();
         clearBroadcastParticipants(button);
      });
   }

   function clearBroadcastParticipants(button)
   {
      const broadcastId = (button.dataset.broadcastId ?? "").trim();

      if(broadcastId === "")
      {
         return;
      }

      const mainRow = Array.from(
         document.querySelectorAll(broadcastRowSelector)
      ).find(row => row instanceof HTMLElement &&
         row.dataset.broadcastId === broadcastId);
      const activityLink = mainRow?.querySelector(
         broadcastActivityLinkSelector
      );

      if(!(activityLink instanceof HTMLAnchorElement))
      {
         return;
      }

      const activityUrl = new URL(
         activityLink.href,
         window.location.origin
      );
      activityUrl.searchParams.set(clearParticipantsQueryKey, "true");
      activityLink.href = `${activityUrl.pathname}${activityUrl.search}` +
         `${activityUrl.hash}`;

      const participantList = button.closest("td")?.querySelector(
         "[data-broadcast-group-participants]"
      );

      if(participantList instanceof HTMLElement)
      {
         participantList.remove();
      }

      const runsRow = mainRow instanceof HTMLElement
         ? getBroadcastRunsRow(mainRow)
         : null;
      const participationCell = runsRow?.querySelector(
         participationCellSelector
      );

      if(participationCell instanceof HTMLElement)
      {
         participationCell.dataset.clearParticipants = "true";
      }

      button.disabled = true;
      button.textContent = "Cleared";
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
               const activityAiResultCell = target.closest(
                  activityAiResultInlineEditCellSelector
               );

               if(activityAiResultCell instanceof HTMLElement)
               {
                  event.preventDefault();
                  openActivityAiResultInlineEditCell(
                     activityAiResultCell
                  );
               }

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

   function initializeActivityAiResultInlineEditing(root = document)
   {
      root.querySelectorAll(
         activityAiResultInlineEditInputSelector
      ).forEach(input => {
         initializeActivityAiResultInlineEditInput(input);
      });
   }

   function initializeActivityAiResultInlineEditInput(input)
   {
      if(!(input instanceof HTMLInputElement)
         || input.dataset.activityAiResultInlineEditInitialized
            === "true")
      {
         return;
      }

      input.dataset.activityAiResultInlineEditInitialized = "true";

      input.addEventListener("blur", () => {
         void saveActivityAiResultInlineEditAsync(input);
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
            cancelActivityAiResultInlineEdit(input);
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
         const html = await postParticipationStatusAsync(url, broadcastIds);
         replaceParticipationCellsFromHtml(html, cells);

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

   function openActivityAiResultInlineEditCell(cell)
   {
      const input = cell.querySelector(
         activityAiResultInlineEditInputSelector
      );
      const display = cell.querySelector(
         activityAiResultInlineEditDisplaySelector
      );

      if(!(input instanceof HTMLInputElement)
         || !(display instanceof HTMLElement)
         || input.hidden === false
         || input.dataset.activityAiResultInlineEditSaving === "true")
      {
         return;
      }

      input.dataset.activityAiResultInlineEditOriginalValue = input.value;
      cell.dataset.activityAiResultInlineEditing = "true";
      display.hidden = true;
      input.hidden = false;

      window.requestAnimationFrame(() => {
         input.focus();
         input.select();
      });
   }

   async function saveActivityAiResultInlineEditAsync(input)
   {
      if(!(input instanceof HTMLInputElement)
         || input.hidden
         || input.dataset.activityAiResultInlineEditSaving === "true")
      {
         return;
      }

      const cell = input.closest(
         activityAiResultInlineEditCellSelector
      );
      const url = getActivityAiResultInlineEditUrl();
      const resultId = (
         cell?.dataset.aiResultValueId ?? ""
      ).trim();
      const field = (cell?.dataset.aiResultEditField ?? "").trim();
      const currentValue = input.value.trim();
      const originalValue = (
         input.dataset.activityAiResultInlineEditOriginalValue ?? ""
      ).trim();

      if(!(cell instanceof HTMLElement)
         || url === ""
         || resultId === ""
         || field === "")
      {
         return;
      }

      if(currentValue === originalValue)
      {
         restoreActivityAiResultInlineEditInput(input);
         return;
      }

      input.dataset.activityAiResultInlineEditSaving = "true";
      input.disabled = true;

      try
      {
         const payload = await postActivityAiResultInlineEditAsync(
            url,
            resultId,
            field,
            currentValue
         );

         updateActivityAiResultInlineEditCell(cell, payload);
         restoreActivityAiResultInlineEditInput(input);
      }
      catch(error)
      {
         window.alert(
            error instanceof Error
               ? error.message
               : "AI result update failed."
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
         delete input.dataset.activityAiResultInlineEditSaving;
      }
   }

   function cancelActivityAiResultInlineEdit(input)
   {
      if(!(input instanceof HTMLInputElement))
      {
         return;
      }

      input.value = (
         input.dataset.activityAiResultInlineEditOriginalValue ??
            input.value
      ).trim();
      restoreActivityAiResultInlineEditInput(input);
   }

   function restoreActivityAiResultInlineEditInput(input)
   {
      if(!(input instanceof HTMLInputElement))
      {
         return;
      }

      const cell = input.closest(
         activityAiResultInlineEditCellSelector
      );
      const display = cell?.querySelector(
         activityAiResultInlineEditDisplaySelector
      );

      if(display instanceof HTMLElement)
      {
         display.hidden = false;
      }

      input.hidden = true;

      if(cell instanceof HTMLElement)
      {
         delete cell.dataset.activityAiResultInlineEditing;
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
         const rowHtml = await postBroadcastInlineEditAsync(
            url,
            broadcastId,
            field,
            currentValue
         );
         const container = cell.closest(
            "tbody[data-broadcast-container]"
         );

         if(!(container instanceof HTMLElement))
         {
            throw new Error("Broadcast container not found.");
         }

         const replacement = window.replaceElementWithPartialHtml(
            container,
            rowHtml
         );
         initializeBroadcastInlineEditing(replacement);
         window.initializeBroadcastOrganizationAutocomplete?.(replacement);
         window.initializeBroadcastActivityGroupAutocomplete?.(replacement);
         void initializeParticipationRunsAsync(replacement);
         initializeParticipationPolling(replacement);
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

   async function checkParticipationRowAsync(button)
   {
      const url = button.dataset.checkParticipationUrl;
      const broadcastId = button.dataset.broadcastId;
      const cell = getParticipationCellForButton(button);
      const previousRunId = getParticipationRunId(cell);

      if(!url || !broadcastId || !(cell instanceof HTMLElement))
      {
         return;
      }

      if(queuingParticipationIds.has(broadcastId))
      {
         return;
      }

      queuingParticipationIds.add(broadcastId);
      const originalLabel = button.textContent ?? "Check";
      button.disabled = true;
      button.textContent = "Checking...";

      try
      {
         const payload = await postParticipationCheckAsync(
            url,
            [broadcastId]
         );

         if(payload && payload.queued === true)
         {
            pendingParticipationIds.add(broadcastId);
            if(previousRunId !== "")
            {
               cell.dataset.participationQueuedFromRunId = previousRunId;
            }
            const pendingHtml = await postParticipationStatusAsync(
               getParticipationStatusUrl(),
               [broadcastId],
               true
            );
            replaceParticipationCellsFromHtml(pendingHtml);
            startParticipationPolling();
            return;
         }
      }
      catch(error)
      {
         console.error(error);
      }
      finally
      {
         queuingParticipationIds.delete(broadcastId);
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

         const html = await postParticipationStatusAsync(
            url,
            [...pendingParticipationIds]
         );
         const finalIds = replaceParticipationCellsFromHtml(html);
         finalIds.forEach(broadcastId => {
            pendingParticipationIds.delete(broadcastId);
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

   async function postParticipationStatusAsync(
      url,
      selectedIds,
      pending = false
   )
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

      if(pending)
      {
         formData.append("pending", "true");
      }

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

   function replaceParticipationCellsFromHtml(html, requestedCells = null)
   {
      const finalIds = new Set();
      const partialRoot = window.getPartialRootFromHtml(html);
      const cells = requestedCells ?? Array.from(
         document.querySelectorAll(participationCellSelector)
      );
      const cellsById = new Map(
         cells
            .filter(cell => cell instanceof HTMLElement)
            .map(cell => [cell.dataset.broadcastId ?? "", cell])
      );

      partialRoot.querySelectorAll("[data-participation-partial]")
         .forEach(partial => {
            if(!(partial instanceof HTMLElement))
            {
               return;
            }

            const broadcastId = (partial.dataset.broadcastId ?? "").trim();
            const cell = cellsById.get(broadcastId)
               ?? getParticipationCellByBroadcastId(broadcastId);
            const rendered = partial.firstElementChild;

            if(!(cell instanceof HTMLElement)
               || !(rendered instanceof HTMLElement))
            {
               return;
            }

            const queuedFromRunId = (
               cell.dataset.participationQueuedFromRunId ?? ""
            ).trim();
            const renderedRunId = (
               rendered.dataset.participationRunId ?? ""
            ).trim();
            const isFinal = rendered.dataset.participationFinal === "true";

            if(isFinal && queuedFromRunId !== ""
               && renderedRunId === queuedFromRunId)
            {
               return;
            }

            const isOpen = cell.querySelector(
               ".broadcast-ai-check-runs-body"
            )?.hidden === false;

            window.replaceContentsWithPartialHtml(
               cell,
               rendered.outerHTML
            );

            if(rendered.dataset.participationRunId)
            {
               cell.dataset.participationRunId =
                  rendered.dataset.participationRunId;
            }
            else
            {
               delete cell.dataset.participationRunId;
            }

            if(rendered.dataset.participationStatus)
            {
               cell.dataset.participationStatus =
                  rendered.dataset.participationStatus;
            }
            else
            {
               delete cell.dataset.participationStatus;
            }

            restoreParticipationRunsOpen(cell, isOpen);

            if(isFinal)
            {
               finalIds.add(broadcastId);
            }
         });

      return finalIds;
   }

   function restoreParticipationRunsOpen(cell, isOpen)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      const table = cell.querySelector(".broadcast-ai-check-runs-table");
      const body = cell.querySelector(".broadcast-ai-check-runs-body");
      const toggle = cell.querySelector(
         "[data-participation-runs-toggle]"
      );

      if(body instanceof HTMLElement)
      {
         body.hidden = !isOpen;
      }

      if(table instanceof HTMLElement)
      {
         table.dataset.participationRunsOpen = String(isOpen);
      }

      if(toggle instanceof HTMLButtonElement)
      {
         toggle.setAttribute("aria-expanded", String(isOpen));
         toggle.setAttribute(
            "aria-label",
            isOpen ? "Hide participation runs" : "Show participation runs"
         );
         toggle.textContent = isOpen ? "−" : "+";
      }
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

   function getActivityAiResultInlineEditUrl()
   {
      const container = document.querySelector(
         activityAiResultInlineEditUrlSelector
      );

      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const url = container.dataset.aiResultEditUrl;

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

   async function postActivityAiResultInlineEditAsync(
      url,
      resultId,
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

      formData.append("id", resultId);
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

   function updateActivityAiResultInlineEditCell(cell, payload)
   {
      if(!(cell instanceof HTMLElement) || !payload)
      {
         return;
      }

      const field = typeof payload.field === "string"
         ? payload.field.trim()
         : "";

      if(field !== activityAiResultInlineEditField)
      {
         return;
      }

      const nextValue = typeof payload.value === "string"
         ? payload.value.trim()
         : "";
      const displayValue = typeof payload.displayValue === "string"
         ? payload.displayValue.trim()
         : nextValue;
      const placeholder = (
         cell.dataset.aiResultPlaceholder
            ?? activityAiResultInlineEditDefaultPlaceholder
      ).trim() || activityAiResultInlineEditDefaultPlaceholder;
      const hasValue = nextValue !== "";
      const display = cell.querySelector(
         activityAiResultInlineEditDisplaySelector
      );
      const input = cell.querySelector(
         activityAiResultInlineEditInputSelector
      );

      if(display instanceof HTMLElement)
      {
         display.textContent = hasValue
            ? displayValue || nextValue
            : placeholder;
         display.classList.toggle("inline-edit-placeholder", !hasValue);
      }

      if(input instanceof HTMLInputElement)
      {
         input.value = nextValue;
         input.dataset.activityAiResultInlineEditOriginalValue =
            nextValue;
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
      const placeholder = (
         cell.dataset.entityInlineEditPlaceholder
            ?? "Add watch priority.."
      ).trim() || "Add watch priority..";
      const hasDisplayValue = displayValue !== "";
      const display = cell.querySelector(entityInlineEditDisplaySelector);
      const input = cell.querySelector(entityInlineEditInputSelector);

      cell.dataset.entityInlineEditValue = nextValue;

      if(display instanceof HTMLElement)
      {
         display.hidden = false;

         const valueText = display.querySelector("span");

         if(valueText instanceof HTMLElement)
         {
            valueText.textContent = hasDisplayValue
               ? displayValue
               : placeholder;
            valueText.title = nextValue;
            valueText.classList.toggle(
               "inline-edit-placeholder",
               !hasDisplayValue
            );
         }
         else
         {
            display.textContent = hasDisplayValue
               ? displayValue
               : placeholder;
            display.classList.toggle(
               "inline-edit-placeholder",
               !hasDisplayValue
            );
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
      const status = form?.querySelector("[data-facts-status]");

      if(!form || !url)
      {
         return;
      }

      setTeaserStatus(status, "Queueing group facts job...");
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
         initializeAdminDateSteppers(nextTarget);
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
         syncReplacementCount(form, nextTarget);
         initializeAdminDateSteppers(nextTarget);
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

   function syncReplacementCount(form, nextTarget)
   {
      if(!(form instanceof HTMLFormElement) ||
         !(nextTarget instanceof HTMLElement))
      {
         return;
      }

      const countSelector = (form.dataset.ajaxCountTarget ?? "").trim();
      const countValue = (nextTarget.dataset.ajaxCountValue ?? "").trim();

      if(countSelector === "" || countValue === "")
      {
         return;
      }

      const count = document.querySelector(countSelector);

      if(!(count instanceof HTMLElement))
      {
         return;
      }

      count.textContent = countValue;
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

      const row = form.closest(".broadcast-ai-check-participant-row");

      if(!(row instanceof HTMLElement))
      {
         return;
      }

      const html = await response.text();
      window.replaceElementWithPartialHtml(row, html);
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
