(() => {
   const enhancedFormSelector =
      "form[data-ajax-success]:not([data-ajax-success=''])";
   const replacementFormSelector = "form[data-ajax-replace-target]";
   const checkboxToggleSelector = "[data-checkbox-toggle]";
   const checkboxVisibilitySelector = "[data-visible-when-checkbox-group]";
   const entityNameFilterSelector = "[data-entity-name-filter]";
   const generateTeaserSelector = "[data-generate-teaser]";
   const checkParticipationSelector =
      "[data-check-swedish-participation]";
   const checkParticipationRowSelector =
      "[data-check-swedish-participation-row]";
   const participationCellSelector = "[data-swedish-participation-cell]";
   const pendingParticipationIds = new Set();
   const getFormSelector = "form[method='get']";
   const exclusiveEmptySelectSelector = "select[data-empty-option='exclusive']";
   const dateSelectSelector = "#date-select-input";
   const exclusiveEmptySelectStates = new WeakMap();

   window.submitFilterForm = submitFilterForm;
   initializeExclusiveEmptySelects();
   initializeMultiSelectClearButtons();
   initializeCheckboxToggles();
   initializeCheckboxVisibility();
   initializeEntityNameFilters();
   initializeGetFormRestoration();
   initializeDateSelect();
   initializeTeaserGeneration();
   initializeBroadcastParticipationChecks();
   initializeBroadcastParticipationRowChecks();

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

            if(target)
            {
               target.remove();
            }
         }

         decrementCounter(form.dataset.ajaxDecrementTarget);
         refreshCheckboxControls();
      }
      catch
      {
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

   function initializeEntityNameFilters(root = document)
   {
      root.querySelectorAll(entityNameFilterSelector).forEach(field => {
         if(!(field instanceof HTMLInputElement)
            || field.dataset.entityNameFilterInitialized === "true")
         {
            return;
         }

         field.dataset.entityNameFilterInitialized = "true";

         const container = document.querySelector(
            "[data-entity-list-container]"
         );
         const rows = container?.querySelectorAll("[data-entity-row-name]");
         const emptyState = container?.querySelector(
            "[data-entity-empty-state]"
         );

         const update = () => {
            const query = field.value.trim().toLowerCase();
            let visibleCount = 0;

            rows?.forEach(row => {
               const rowName = (
                  row instanceof HTMLElement
                     ? row.dataset.entityRowName ?? ""
                     : ""
               ).toLowerCase();
               const rowRelated = (
                  row instanceof HTMLElement
                     ? row.dataset.entityRowRelated ?? ""
                     : ""
               ).toLowerCase();
               const matches =
                  query === "" ||
                  rowName.includes(query) ||
                  rowRelated.includes(query);

               row.hidden = !matches;

               if(matches)
               {
                  visibleCount++;
               }
            });

            if(emptyState instanceof HTMLElement)
            {
               emptyState.hidden = visibleCount > 0;
            }
         };

         field.addEventListener("input", update);
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
   }

   function initializeBroadcastParticipationChecks(root = document)
   {
      root.querySelectorAll(checkParticipationSelector).forEach(button => {
         if(!(button instanceof HTMLButtonElement)
            || button.dataset.checkParticipationInitialized === "true")
         {
            return;
         }

         button.dataset.checkParticipationInitialized = "true";
         button.addEventListener("click", async () => {
            await checkParticipationAsync(button);
         });
      });
   }

   function initializeBroadcastParticipationRowChecks(root = document)
   {
      root.querySelectorAll(checkParticipationRowSelector).forEach(button => {
         if(!(button instanceof HTMLButtonElement)
            || button.dataset.checkParticipationRowInitialized === "true")
         {
            return;
         }

         button.dataset.checkParticipationRowInitialized = "true";
         button.addEventListener("click", async () => {
            await checkParticipationRowAsync(button);
         });
      });
   }

   async function checkParticipationRowAsync(button)
   {
      const url = button.dataset.checkSwedishParticipationUrl;
      const broadcastId = button.dataset.broadcastId;
      const cell = button.closest(participationCellSelector);

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
            swedishParticipants: []
         });
      }
      finally
      {
         pendingParticipationIds.delete(broadcastId);
         button.disabled = false;
         button.textContent = originalLabel;
      }
   }

   async function checkParticipationAsync(button)
   {
      const url = button.dataset.checkSwedishParticipationUrl;
      const form = document.getElementById("generate-activity-form");
      const status = document.querySelector(
         "[data-check-swedish-participation-status]"
      );

      if(!url || !(form instanceof HTMLFormElement))
      {
         return;
      }

      const formData = new FormData(form);
      const selectedIds = formData
         .getAll("tvSportBroadcastIds")
         .map(value => String(value))
         .filter(value => value.trim() !== "")
         .filter(value => !pendingParticipationIds.has(value));

      if(selectedIds.length === 0)
      {
         const pendingCount = formData
            .getAll("tvSportBroadcastIds")
            .map(value => String(value))
            .filter(value => value.trim() !== "")
            .filter(value => pendingParticipationIds.has(value)).length;

         setParticipationStatus(
            status,
            pendingCount > 0
               ? "Selected broadcasts are already checking."
               : "Select at least one broadcast.",
            true
         );
         return;
      }

      setParticipationStatus(status, "Checking Swedish participation...");
      button.disabled = true;

      try
      {
         const payload = await postParticipationCheckAsync(
            url,
            selectedIds
         );
         const results = Array.isArray(payload.results)
            ? payload.results
            : [];

         results.forEach(result => {
            updateParticipationCellByResult(result);
         });

         const lines = results
            .map(formatParticipationResult)
            .filter(Boolean);

         setParticipationStatus(
            status,
            lines.length > 0
               ? lines.join("\n")
               : "No participation results returned."
         );
      }
      catch(error)
      {
         const message = error instanceof Error
            ? error.message
            : "Participation check failed.";

         setParticipationStatus(status, message, true);
      }
      finally
      {
         button.disabled = false;
      }
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
         formData.append("tvSportBroadcastIds", id);
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

   function formatParticipationResult(result)
   {
      if(!result || typeof result !== "object")
      {
         return "";
      }

      const label = [
         result.channelName,
         result.title
      ].filter(part => typeof part === "string" && part.trim() !== "")
         .join(" - ") || "Broadcast";

      if(typeof result.error === "string"
         && result.error.trim() !== "")
      {
         return `${label}: ${result.error}`;
      }

      const participation = typeof result.swedishParticipation === "string"
         && result.swedishParticipation.trim() !== ""
         ? result.swedishParticipation
         : "Unknown";
      const participants = Array.isArray(result.swedishParticipants)
         ? result.swedishParticipants
            .filter(participant =>
               typeof participant === "string" && participant.trim() !== "")
         : [];

      if(participation === "Yes" && participants.length > 0)
      {
         return `${label}: Yes (${participants.join(", ")})`;
      }

      return `${label}: ${participation}`;
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

   function updateParticipationCell(cell, result)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      cell.replaceChildren();

      if(!result || typeof result !== "object")
      {
         const fallback = document.createElement("span");
         fallback.className = "tv-sport-ai-check-empty";
         fallback.textContent = "Not checked yet";
         cell.append(fallback);
         return;
      }

      if(typeof result.error === "string" && result.error.trim() !== "")
      {
         cell.append(
            createParticipationErrorBlock(
               result.error,
               result.runId
            )
         );
         return;
      }

      const participation = typeof result.swedishParticipation === "string"
         && result.swedishParticipation.trim() !== ""
         ? result.swedishParticipation.trim()
         : "";
      const participants = Array.isArray(result.swedishParticipants)
         ? result.swedishParticipants
            .filter(participant =>
               typeof participant === "string" && participant.trim() !== "")
         : [];

      if(participation === "")
      {
         const fallback = document.createElement("span");
         fallback.className = "tv-sport-ai-check-empty";
         fallback.textContent = "Not checked yet";
         cell.append(fallback);
         return;
      }

      const wrapper = document.createElement("div");
      wrapper.className = "tv-sport-ai-check";
      wrapper.append(createParticipationSummaryLine(result));

      if(participants.length > 0)
      {
         const names = document.createElement("div");
         names.className = "tv-sport-ai-check-participants";
         names.textContent = participants.join(", ");
         wrapper.append(names);
      }

      cell.append(wrapper);
   }

   function setPendingParticipationCell(cell)
   {
      if(!(cell instanceof HTMLElement))
      {
         return;
      }

      cell.replaceChildren();

      const wrapper = document.createElement("div");
      wrapper.className = "tv-sport-ai-check";

      const pending = document.createElement("span");
      pending.className = "tv-sport-ai-check-pending";
      pending.textContent = "Checking...";
      wrapper.append(pending);

      cell.append(wrapper);
   }

   function createParticipationSummaryLine(result)
   {
      const line = document.createElement("div");
      line.className = "tv-sport-ai-check-line";

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

      const runLink = createParticipationRunLink(result.runId);

      if(runLink)
      {
         line.append(runLink);
      }

      return line;
   }

   function createParticipationErrorBlock(errorMessage, runId)
   {
      const wrapper = document.createElement("div");
      wrapper.className = "tv-sport-ai-check";

      const line = document.createElement("div");
      line.className = "tv-sport-ai-check-line";

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
      error.className = "tv-sport-ai-check-error";
      error.textContent = errorMessage;

      wrapper.append(line, error);
      return wrapper;
   }

   function createParticipationRunLink(runId)
   {
      if(typeof runId !== "string" || runId.trim() === "")
      {
         return null;
      }

      const link = document.createElement("a");
      link.href =
         `/Admin/Config/Ai/Runs/Details/${encodeURIComponent(runId)}`;
      link.textContent = "View run";
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

   function setParticipationStatus(status, message, isError = false)
   {
      if(!(status instanceof HTMLElement))
      {
         return;
      }

      status.textContent = message;
      status.classList.toggle("form-status-error", isError);
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

      setTeaserStatus(status, "Generating teaser...");
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

         output.value = payload.teaser || "";
         setTeaserStatus(status, "Teaser generated.");
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
         history.replaceState(null, "", url);
      }
      catch
      {
         HTMLFormElement.prototype.submit.call(form);
      }
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
})();
