(() => {
   const containerSelector = "[data-entity-list-container]";
   const filterSelector = "[data-entity-name-filter]";
   const listBodySelector = "[data-entity-list-body]";
   const emptyStateSelector = "[data-entity-empty-state]";
   const watchPriorityTemplateSelector =
      "[data-entity-watch-priority-template]";
   const initializedFlag = "entitySearchInitialized";
   const debounceMs = 250;

   window.initializeEntitySearch = initializeEntitySearch;

   function initializeEntitySearch(root = document)
   {
      root.querySelectorAll(filterSelector).forEach(field => {
         if(!(field instanceof HTMLInputElement)
            || field.dataset[initializedFlag] === "true")
         {
            return;
         }

         const container = field.closest(containerSelector)
            || document.querySelector(containerSelector);
         const listBody = container?.querySelector(listBodySelector);
         const emptyState = container?.querySelector(emptyStateSelector);
         const template = container?.querySelector(
            watchPriorityTemplateSelector
         );
         const searchUrl = getDataValue(container, "entitySearchUrl");
         const editUrlBase = getDataValue(container, "entityEditUrl");
         const deleteUrlBase = getDataValue(container, "entityDeleteUrl");
         const searchUrlBase = getDataValue(
            container,
            "entitySearchUrlBase"
         );
         const cookieName = getDataValue(
            container,
            "entityFilterCookieName"
         );
         const sortColumn = getDataValue(container, "entitySortColumn")
            || "Name";
         const sortAsc = getDataValue(container, "entitySortAsc")
            .toLowerCase() === "true";

         if(!(container instanceof HTMLElement)
            || !(listBody instanceof HTMLTableSectionElement)
            || searchUrl === ""
            || editUrlBase === ""
            || deleteUrlBase === "")
         {
            return;
         }

         field.dataset[initializedFlag] = "true";

         const cookieValue = getCookie(cookieName);
         if(cookieValue !== "" && field.value.trim() === "")
         {
            field.value = cookieValue;
         }

         let debounceTimer = null;
         let activeController = null;

         const currentQuery = () => field.value.trim();

         const setEmptyState = hasMatches => {
            if(!(emptyState instanceof HTMLElement))
            {
               return;
            }

            emptyState.hidden = hasMatches;
         };

         const renderRows = entities => {
            const rowsHtml = entities
               .map(entity => renderEntityRowHtml(
                  entity,
                  searchUrlBase,
                  editUrlBase,
                  deleteUrlBase,
                  template
               ))
               .join("");

            listBody.innerHTML = rowsHtml;
            window.initializeEntityInlineEditing?.(listBody);
            setEmptyState(entities.length > 0);
         };

         const fetchAndRenderAsync = async query => {
            if(activeController instanceof AbortController)
            {
               activeController.abort();
            }

            const controller = new AbortController();
            activeController = controller;

            try
            {
               const url = new URL(searchUrl, window.location.origin);

               if(query !== "")
               {
                  url.searchParams.set("term", query);
               }

               url.searchParams.set("includeAll", "true");

               url.searchParams.set("sortColumn", sortColumn);
               url.searchParams.set("sortAsc", sortAsc ? "true" : "false");

               const response = await fetch(url, {
                  headers: {
                     Accept: "application/json"
                  },
                  signal: controller.signal
               });

               if(!response.ok)
               {
                  throw new Error(
                     `Request failed with status ${response.status}`
                  );
               }

               const payload = await response.json();
               const entities = Array.isArray(payload?.results)
                  ? payload.results
                  : [];
               renderRows(entities);
            }
            catch(error)
            {
               if(error instanceof DOMException &&
                  error.name === "AbortError")
               {
                  return;
               }

               console.error("Entity search failed:", error);
            }
            finally
            {
               if(activeController === controller)
               {
                  activeController = null;
               }
            }
         };

         const scheduleSearch = () => {
            window.clearTimeout(debounceTimer);
            debounceTimer = window.setTimeout(() => {
               const query = currentQuery();

               setCookie(cookieName, query);

               if(query === "")
               {
                  clearRows();
                  return;
               }

               void fetchAndRenderAsync(query);
            }, debounceMs);
         };

         field.addEventListener("input", scheduleSearch);

         const initialQuery = currentQuery();

         if(initialQuery === "")
         {
            listBody.replaceChildren();
            setEmptyState(false);
            return;
         }

         void fetchAndRenderAsync(initialQuery);
      });
   }

   function renderEntityRowHtml(
      entity,
      searchUrlBase,
      editUrlBase,
      deleteUrlBase,
      watchPriorityTemplate
   )
   {
      const token = getAntiForgeryToken();
      const entityId = escapeHtml(entity.id ?? "");
      const name = escapeHtml(entity.name ?? "");
      const relatedEntityNames = escapeHtml(entity.relatedEntityNames ?? "");
      const entityType = escapeHtml(entity.entityType ?? "");
      const sport = escapeHtml(entity.sport ?? "");
      const watchPriorityId = escapeHtml(entity.watchPriorityId ?? "");
      const watchPriority = escapeHtml(entity.watchPriority ?? "");
      const country = escapeHtml(entity.country ?? "");
      const searchQuery = encodeURIComponent(
         `${entity.name ?? ""} ${entity.sport ?? ""}`.trim()
      );
      const searchUrl = `${searchUrlBase}${searchQuery}`;
      const editUrl = new URL(editUrlBase, window.location.origin);
      const deleteUrl = new URL(deleteUrlBase, window.location.origin);
      const entityPath = `${editUrl.pathname.replace(/\/$/, "")}/` +
         `${entity.id ?? ""}`;

      editUrl.pathname = entityPath;
      deleteUrl.searchParams.set("handler", "Delete");
      deleteUrl.searchParams.set("id", entity.id ?? "");

      return `
         <tr data-entity-row-id="${entityId}"
             data-entity-row-name="${name}"
             data-entity-row-related="${relatedEntityNames}">
            <td>
               <a href="${editUrl.toString()}">
                  <strong>${name}</strong>
               </a>
               <a class="ses-entity-search-link"
                  href="${searchUrl}"
                  target="_blank"
                  rel="noreferrer">
                  <span class="ses-icon-search"></span>
               </a>
            </td>
            <td>${relatedEntityNames}</td>
            <td>${entityType}</td>
            <td>${sport}</td>
            <td class="entity-inline-editable"
                data-entity-inline-edit-field="watch-priority"
                data-entity-inline-edit-value="${watchPriorityId}"
                title="Double-click to edit">
               <div class="entity-inline-edit-display"
                    data-entity-inline-edit-display>
                  <span title="${watchPriority}">
                     ${watchPriority}
                  </span>
               </div>
               <select class="entity-inline-edit-input"
                       data-entity-inline-edit-input
                       hidden>
                  ${renderWatchPriorityOptions(
                     entity.watchPriorityId ?? "",
                     watchPriorityTemplate
                  )}
               </select>
            </td>
            <td>${country}</td>
            <td class="table-actions">
               <form method="post"
                     action="${deleteUrl.toString()}"
                     onsubmit='return confirm("Sure?");'>
                  ${renderAntiForgeryTokenInputHtml(token)}
                  <button type="submit">Delete</button>
               </form>
            </td>
         </tr>
      `;
   }

   function renderAntiForgeryTokenInputHtml(token)
   {
      if(typeof token !== "string" || token.trim() === "")
      {
         return "";
      }

      return `
         <input type="hidden"
                name="__RequestVerificationToken"
                value="${escapeHtml(token)}" />
      `;
   }

   function renderWatchPriorityOptions(selectedId, template)
   {
      if(!(template instanceof HTMLSelectElement))
      {
         return "";
      }

      return Array.from(template.options)
         .map(option => {
            const value = escapeHtml(option.value);
            const text = escapeHtml(option.textContent ?? "");
            const selected = option.value === selectedId
               ? " selected"
               : "";

            return `<option value="${value}"${selected}>${text}</option>`;
         })
         .join("");
   }

   function getDataValue(container, key)
   {
      if(!(container instanceof HTMLElement))
      {
         return "";
      }

      const value = container.dataset[key];
      return typeof value === "string" ? value.trim() : "";
   }

   function getAntiForgeryToken()
   {
      const tokenInput = document.querySelector(
         "input[name='__RequestVerificationToken']"
      );

      if(!(tokenInput instanceof HTMLInputElement))
      {
         return "";
      }

      return tokenInput.value;
   }

   function getCookie(name)
   {
      if(typeof name !== "string" || name.trim() === "")
      {
         return "";
      }

      const encodedName = `${encodeURIComponent(name)}=`;
      const cookies = document.cookie ? document.cookie.split(";") : [];

      for(const cookie of cookies)
      {
         const trimmedCookie = cookie.trim();

         if(trimmedCookie.startsWith(encodedName))
         {
            return decodeURIComponent(
               trimmedCookie.substring(encodedName.length)
            );
         }
      }

      return "";
   }

   function setCookie(name, value)
   {
      if(typeof name !== "string" || name.trim() === "")
      {
         return;
      }

      const encodedName = encodeURIComponent(name.trim());
      const encodedValue = encodeURIComponent(value ?? "");

      document.cookie = `${encodedName}=${encodedValue}; path=/; ` +
         "sameSite=lax; max-age=31536000";
   }

   function escapeHtml(value)
   {
      return String(value ?? "").replace(/[&<>'"]/g, char => {
         switch(char)
         {
            case "&":
               return "&amp;";
            case "<":
               return "&lt;";
            case ">":
               return "&gt;";
            case "'":
               return "&#39;";
            case '"':
               return "&quot;";
            default:
               return char;
         }
      });
   }
})();
