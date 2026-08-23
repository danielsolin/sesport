// Admin UI activity and AI controls.
// Loaded before site.js; the files intentionally share the classic-script scope.

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
