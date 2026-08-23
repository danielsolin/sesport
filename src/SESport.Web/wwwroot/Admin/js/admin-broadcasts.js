// Admin UI broadcasts and participation.
// Loaded before site.js; the files intentionally share the classic-script scope.

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
