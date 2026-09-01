// Admin UI AI runs and run editing.
// Loaded before site.js; the files intentionally share the classic-script scope.

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
   catch(error)
   {
      console.error("Run status polling failed:", error);
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
