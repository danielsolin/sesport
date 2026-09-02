// Admin UI forms and controls.
// Loaded before site.js; the files intentionally share the classic-script scope.

const filterSubmitDebounceMs = 250;
const filterSubmitTimers = new WeakMap();

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
      const participationStatusField = form?.querySelector(
         personParticipationStatusFieldSelector
      );
      const participationReasonField = form?.querySelector(
         personParticipationReasonFieldSelector
      );

      if(!(genderField instanceof HTMLElement)
         || !(birthdateField instanceof HTMLElement)
         || !(heightField instanceof HTMLElement)
         || !(weightField instanceof HTMLElement)
         || !(formativeClubField instanceof HTMLElement)
         || !(participationStatusField instanceof HTMLElement)
         || !(participationReasonField instanceof HTMLElement))
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
         participationStatusField.hidden = !isPerson;
         participationReasonField.hidden = !isPerson;
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

function submitFilterForm(field)
{
   normalizeExclusiveEmptyOption(field);
   const form = field.form;

   if(!(form instanceof HTMLFormElement))
   {
      return;
   }

   const pendingTimer = filterSubmitTimers.get(form);
   if(pendingTimer !== undefined)
   {
      window.clearTimeout(pendingTimer);
      filterSubmitTimers.delete(form);
   }

   if(field instanceof HTMLInputElement
      && (field.type === "search" || field.type === "text"))
   {
      const timer = window.setTimeout(() => {
         filterSubmitTimers.delete(form);
         form.requestSubmit();
      }, filterSubmitDebounceMs);
      filterSubmitTimers.set(form, timer);
      return;
   }

   form.requestSubmit();
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
