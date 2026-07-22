(() => {
   const containerSelector = "[data-entity-list-container]";
   const filterSelector = "[data-entity-name-filter]";
   const typeFilterSelector = "[data-entity-type-filter]";
   const sportFilterSelector = "[data-entity-sport-filter]";
   const listBodySelector = "[data-entity-list-body]";
   const emptyStateSelector = "[data-entity-empty-state]";
   const countSelector = "[data-entity-count]";
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
         const filterForm = field.closest(".entity-filter-grid");
         const listBody = container?.querySelector(listBodySelector);
         const emptyState = container?.querySelector(emptyStateSelector);
         const count = filterForm?.querySelector(countSelector);
         const template = container?.querySelector(
            watchPriorityTemplateSelector
         );
         const searchUrl = getDataValue(container, "entitySearchUrl");
         const editUrlBase = getDataValue(container, "entityEditUrl");
         const deleteUrlBase = getDataValue(container, "entityDeleteUrl");
         const personFactsUrl = getDataValue(container, "personFactsUrl");
         const searchUrlBase = getDataValue(
            container,
            "entitySearchUrlBase"
         );
         const activityDate = getDataValue(container, "entityDate");
         const cookieName = getDataValue(
            container,
            "entityFilterCookieName"
         );
         const typeCookieName = getDataValue(
            container,
            "entityTypeFilterCookieName"
         );
         const sportCookieName = getDataValue(
            container,
            "entitySportFilterCookieName"
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

         const getTypeFilter = () => {
            const filter = document.querySelector(typeFilterSelector);
            return filter instanceof HTMLSelectElement ? filter : null;
         };

         const getSportFilter = () => {
            const filter = document.querySelector(sportFilterSelector);
            return filter instanceof HTMLSelectElement ? filter : null;
         };

         const initialTypeFilter = getTypeFilter();
         const initialSportFilter = getSportFilter();

         if(initialTypeFilter instanceof HTMLSelectElement)
         {
            applyTypeFilterCookie(
               initialTypeFilter,
               getCookie(typeCookieName)
            );
         }

         if(initialSportFilter instanceof HTMLSelectElement)
         {
            applyTypeFilterCookie(
               initialSportFilter,
               getCookie(sportCookieName)
            );
         }

         let debounceTimer = null;
         let activeController = null;

         const currentQuery = () => field.value.trim();
         const getSelectedTypeIds = () => {
            const typeFilter = getTypeFilter();

            return typeFilter instanceof HTMLSelectElement
               ? Array.from(typeFilter.options)
                  .filter(option =>
                     option.selected && option.value.trim() !== "")
                  .map(option => option.value.trim())
               : [];
         };

         const getSelectedSportIds = () => {
            const sportFilter = getSportFilter();

            return sportFilter instanceof HTMLSelectElement
               ? Array.from(sportFilter.options)
                  .filter(option =>
                     option.selected && option.value.trim() !== ""
                  )
                  .map(option => option.value.trim())
               : [];
         };

         const setEmptyState = hasMatches => {
            if(!(emptyState instanceof HTMLElement))
            {
               return;
            }

            emptyState.hidden = hasMatches;
         };

         const setEntityCount = value => {
            if(count instanceof HTMLElement)
            {
               count.textContent = value;
            }
         };

         const renderRows = entities => {
            const rowsHtml = entities
               .map(entity => renderEntityRowHtml(
                  entity,
                  searchUrlBase,
                  editUrlBase,
                  deleteUrlBase,
                  template,
                  personFactsUrl
               ))
               .join("");

            listBody.innerHTML = rowsHtml;
            window.initializeEntityInlineEditing?.(listBody);
            initializePersonFactsTriggers(listBody);
            setEmptyState(entities.length > 0);
            setEntityCount(entities.length);
         };

         const clearRows = () => {
            listBody.replaceChildren();
            setEmptyState(false);
            setEntityCount(0);
         };

         const fetchAndRenderAsync = async (
            query,
            typeIds,
            sportIds
         ) => {
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

               if(activityDate !== "")
               {
                  url.searchParams.set("date", activityDate);
               }

               typeIds.forEach(typeId => {
                  url.searchParams.append("entityTypeIds", typeId);
               });

               sportIds.forEach(sportId => {
                  url.searchParams.append("sportIds", sportId);
               });

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
               const typeIds = getSelectedTypeIds();
               const sportIds = getSelectedSportIds();

               setCookie(cookieName, query);
               setCookie(typeCookieName, typeIds.join(","));
               setCookie(sportCookieName, sportIds.join(","));

               if(query === "" && typeIds.length === 0 &&
                  sportIds.length === 0 &&
                  activityDate === "")
               {
                  clearRows();
                  return;
               }

               void fetchAndRenderAsync(query, typeIds, sportIds);
            }, debounceMs);
         };

         field.addEventListener("input", scheduleSearch);
         field.addEventListener("change", scheduleSearch);
         initialTypeFilter?.addEventListener("change", scheduleSearch);
         initialSportFilter?.addEventListener("change", scheduleSearch);

         const initialQuery = currentQuery();
         const initialTypeIds = getSelectedTypeIds();
         const initialSportIds = getSelectedSportIds();

         if(initialQuery === "" && initialTypeIds.length === 0 &&
            initialSportIds.length === 0 &&
            activityDate === "")
         {
            listBody.replaceChildren();
            setEmptyState(false);
            return;
         }

         void fetchAndRenderAsync(
            initialQuery,
            initialTypeIds,
            initialSportIds
         );
      });
   }

   function applyTypeFilterCookie(select, cookieValue)
   {
      if(!(select instanceof HTMLSelectElement) ||
         typeof cookieValue !== "string" ||
         cookieValue.trim() === "")
      {
         return;
      }

      const selectedValues = cookieValue
         .split(",")
         .map(value => value.trim())
         .filter(value => value !== "");

      if(selectedValues.length === 0)
      {
         return;
      }

      const availableValues = new Set(
         Array.from(select.options)
            .map(option => option.value)
      );
      const selectedSet = new Set(
         selectedValues.filter(value => availableValues.has(value))
      );

      Array.from(select.options).forEach(option => {
         option.selected = selectedSet.has(option.value);
      });

      if(selectedSet.size === 0)
      {
         return;
      }

      const selectedOptions = Array.from(selectedSet);
      const multiSelect = select._multiSelect;

      if(multiSelect && typeof multiSelect.setValues === "function")
      {
         multiSelect.setValues(selectedOptions);
      }
   }

   function renderEntityRowHtml(
      entity,
      searchUrlBase,
      editUrlBase,
      deleteUrlBase,
      watchPriorityTemplate,
      personFactsUrl
   )
   {
      const token = getAntiForgeryToken();
      const entityId = escapeHtml(entity.id ?? "");
      const name = escapeHtml(entity.name ?? "");
      const relatedEntityNames = escapeHtml(entity.relatedEntityNames ?? "");
      const relatedPersonCount = Number(entity.relatedPersonCount ?? 0);
      const relatedDisplay = [
         relatedEntityNames,
         relatedPersonCount > 0 ? `P:${relatedPersonCount}` : ""
      ].filter(value => value !== "").join(", ");
      const entityType = escapeHtml(entity.entityType ?? "");
      const sport = escapeHtml(entity.sport ?? "");
      const gender = formatGender(entity.personGenderId);
      const ageValue = formatAge(entity.birthdate);
      const ageSearchQuery = encodeURIComponent(
         `${entity.name ?? ""} ${entity.sport ?? ""} ålder`
      );
      const ageSearchUrl = `${searchUrlBase}${ageSearchQuery}`;
      const age = ageValue !== ""
         ? ageValue
         : `
               <a class="ses-entity-search-link"
                  href="${ageSearchUrl}"
                  target="_blank"
                  rel="noreferrer">
                  <span class="ses-icon-search"></span>
               </a>
            `;
      const heightValue = formatMeasurement(entity.height, "cm");
      const heightSearchQuery = encodeURIComponent(
         `${entity.name ?? ""} ${entity.sport ?? ""} längd`
      );
      const heightSearchUrl = `${searchUrlBase}${heightSearchQuery}`;
      const height = heightValue !== ""
         ? heightValue
         : `
               <a class="ses-entity-search-link"
                  href="${heightSearchUrl}"
                  target="_blank"
                  rel="noreferrer">
                  <span class="ses-icon-search"></span>
               </a>
            `;
      const weightValue = formatMeasurement(entity.weight, "kg");
      const weightSearchQuery = encodeURIComponent(
         `${entity.name ?? ""} ${entity.sport ?? ""} vikt`
      );
      const weightSearchUrl = `${searchUrlBase}${weightSearchQuery}`;
      const weight = weightValue !== ""
         ? weightValue
         : `
               <a class="ses-entity-search-link"
                  href="${weightSearchUrl}"
                  target="_blank"
                  rel="noreferrer">
                  <span class="ses-icon-search"></span>
               </a>
            `;
      const formativeClub = escapeHtml(entity.formativeClub ?? "");
      const firstClubSearchQuery = encodeURIComponent(
         `${entity.name ?? ""} ${entity.sport ?? ""} moderklubb`
      );
      const firstClubSearchUrl =
         `${searchUrlBase}${firstClubSearchQuery}`;
      const firstClub = formativeClub !== ""
         ? formativeClub
         : `
               <a class="ses-entity-search-link"
                  href="${firstClubSearchUrl}"
                  target="_blank"
                  rel="noreferrer">
                  <span class="ses-icon-search"></span>
               </a>
            `;
      const watchPriorityId = escapeHtml(entity.watchPriorityId ?? "");
      const watchPriority = escapeHtml(entity.watchPriority ?? "");
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

      const isPerson = (entity.entityType ?? "")
         .trim()
         .toLowerCase() === "person";
      const factsButton = isPerson && personFactsUrl !== ""
         ? `
               <form method="post" data-person-facts-form>
                  ${renderAntiForgeryTokenInputHtml(token)}
                  <input type="hidden" name="id" value="${entityId}" />
                  <button class="broadcast-participation-check-link"
                          type="submit"
                          data-person-facts-url="${escapeHtml(
                             personFactsUrl
                          )}">
                     Facts
                  </button>
                  <span class="form-status"
                        data-person-facts-status></span>
               </form>
            `
         : "";

      return `
         <tr data-entity-row-id="${entityId}"
             data-entity-row-name="${name}"
             data-entity-row-related="${relatedEntityNames}">
            <td>
               <a href="${editUrl.toString()}">
                  <span class="entity-list-type">${entityType}</span>
                  <strong>${name}</strong>
               </a>
               <a class="ses-entity-search-link"
                  href="${searchUrl}"
                  target="_blank"
                  rel="noreferrer">
                  <span class="ses-icon-search"></span>
               </a>
            </td>
            <td>${relatedDisplay}</td>
            <td>${sport}</td>
            <td>${gender}</td>
            <td>${age}</td>
            <td>${height}</td>
            <td>${weight}</td>
            <td>${firstClub}</td>
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
            <td class="table-actions">
               ${factsButton}
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

   function formatGender(personGenderId)
   {
      if(personGenderId === "female")
      {
         return "F";
      }

      if(personGenderId === "male")
      {
         return "M";
      }

      return "";
   }

   function formatAge(birthdate)
   {
      const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(birthdate ?? "");

      if(match === null)
      {
         return "";
      }

      const birthYear = Number(match[1]);
      const birthMonth = Number(match[2]);
      const birthDay = Number(match[3]);
      const today = new Date();
      let age = today.getFullYear() - birthYear;

      if(today.getMonth() + 1 < birthMonth ||
         (today.getMonth() + 1 === birthMonth &&
            today.getDate() < birthDay))
      {
         age--;
      }

      return `${age} (${escapeHtml(birthdate)})`;
   }

   function formatMeasurement(value, unit)
   {
      if(value === null || value === undefined || value === "")
      {
         return "";
      }

      const measurement = Number(value);

      return Number.isFinite(measurement)
         ? `${measurement}${unit}`
         : "";
   }

   function initializePersonFactsTriggers(root)
   {
      root.querySelectorAll("[data-person-facts-form]").forEach(form => {
         if(!(form instanceof HTMLFormElement)
            || form.dataset.personFactsInitialized === "true")
         {
            return;
         }

         form.dataset.personFactsInitialized = "true";
         form.addEventListener("submit", async event => {
            event.preventDefault();

            const button = form.querySelector("button[type='submit']");
            const status = form.querySelector(
               "[data-person-facts-status]"
            );
            const url = button instanceof HTMLButtonElement
               ? button.dataset.personFactsUrl
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
                  throw new Error(payload.error || "Facts job failed.");
               }

               status.textContent = "Queued";
            }
            catch(error)
            {
               const message = error instanceof Error
                  ? error.message
                  : "Facts job failed.";
               status.textContent = message;
               status.classList.add("form-status-error");
            }
            finally
            {
               button.disabled = false;
            }
         });
      });
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
