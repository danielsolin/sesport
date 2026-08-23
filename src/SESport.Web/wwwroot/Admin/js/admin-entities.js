// Admin UI entities and entity editing.
// Loaded before site.js; the files intentionally share the classic-script scope.

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
