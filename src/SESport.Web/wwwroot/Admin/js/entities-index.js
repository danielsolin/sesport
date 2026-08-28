(() => {
   const containerSelector = "[data-entity-list-container]";
   const filterSelector = "[data-entity-name-filter]";
   const typeFilterSelector = "[data-entity-type-filter]";
   const sportFilterSelector = "[data-entity-sport-filter]";
   const listBodySelector = "[data-entity-list-partial-body]";
   const emptyStateSelector = "[data-entity-empty-state]";
   const countSelector = "[data-entity-count]";
   const initializedFlag = "entitySearchInitialized";
   const debounceMs = 250;

   window.initializeEntitySearch = initializeEntitySearch;

   if(document.readyState === "loading")
   {
      document.addEventListener("DOMContentLoaded", () => {
         initializeEntitySearch();
      });
   }
   else
   {
      initializeEntitySearch();
   }

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
         const searchUrl = getDataValue(container, "entitySearchUrl");
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
            || searchUrl === "")
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

         applyTypeFilterCookie(
            initialTypeFilter,
            getCookie(typeCookieName)
         );
         applyTypeFilterCookie(
            initialSportFilter,
            getCookie(sportCookieName)
         );

         let debounceTimer = null;
         let activeController = null;
         let searchGeneration = 0;

         const currentQuery = () => field.value.trim();
         const getSelectedValues = select => select instanceof HTMLSelectElement
            ? Array.from(select.options)
               .filter(option =>
                  option.selected && option.value.trim() !== "")
               .map(option => option.value.trim())
            : [];
         const setEmptyState = hasMatches => {
            if(emptyState instanceof HTMLElement)
            {
               emptyState.hidden = hasMatches;
            }
         };
         const setEntityCount = value => {
            if(count instanceof HTMLElement)
            {
               count.textContent = value;
            }
         };
         const renderRows = html => {
            const rendered = window.getPartialRootFromHtml(html);
            window.replaceContentsWithPartialHtml(
               listBody,
               rendered.outerHTML
            );
            window.initializeEntityInlineEditing?.(listBody);
            initializePersonFactsTriggers(listBody);
            const rowCount = listBody.querySelectorAll(
               "[data-entity-row-id]"
            ).length;
            setEmptyState(rowCount > 0);
            setEntityCount(rowCount);
         };
         const clearRows = () => {
            listBody.replaceChildren();
            setEmptyState(false);
            setEntityCount(0);
         };
         const abortActiveSearch = () => {
            searchGeneration++;

            if(activeController instanceof AbortController)
            {
               activeController.abort();
               activeController = null;
            }
         };
         const fetchAndRenderAsync = async (
            query,
            typeIds,
            sportIds
         ) => {
            abortActiveSearch();

            const controller = new AbortController();
            const generation = searchGeneration;
            activeController = controller;

            try
            {
               const url = new URL(searchUrl, window.location.origin);

               if(query !== "")
               {
                  url.searchParams.set("term", query);
               }

               url.searchParams.set("includeAll", "true");
               url.searchParams.set("format", "entity-rows");

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
                     Accept: "text/html"
                  },
                  signal: controller.signal
               });
               const html = await response.text();

               if(controller.signal.aborted
                  || generation !== searchGeneration)
               {
                  return;
               }

               if(!response.ok)
               {
                  throw new Error(
                     `Request failed with status ${response.status}`
                  );
               }

               renderRows(html);
            }
            catch(error)
            {
               if(error instanceof DOMException
                  && error.name === "AbortError")
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
         const refreshSearch = () => {
            const query = currentQuery();
            const typeIds = getSelectedValues(getTypeFilter());
            const sportIds = getSelectedValues(getSportFilter());

            setCookie(cookieName, query);
            setCookie(typeCookieName, typeIds.join(","));
            setCookie(sportCookieName, sportIds.join(","));

            if(query === "" && typeIds.length === 0
               && sportIds.length === 0 && activityDate === "")
            {
               abortActiveSearch();
               clearRows();
               return;
            }

            void fetchAndRenderAsync(query, typeIds, sportIds);
         };
         const scheduleSearch = () => {
            window.clearTimeout(debounceTimer);
            debounceTimer = window.setTimeout(() => {
               refreshSearch();
            }, debounceMs);
         };

         field.addEventListener("input", scheduleSearch);
         field.addEventListener("change", scheduleSearch);
         initialTypeFilter?.addEventListener("change", scheduleSearch);
         initialSportFilter?.addEventListener("change", scheduleSearch);
         window.addEventListener("pagehide", abortActiveSearch);
         window.addEventListener("pageshow", event => {
            if(event.persisted)
            {
               refreshSearch();
            }
         });

         const initialQuery = currentQuery();
         const initialTypeIds = getSelectedValues(initialTypeFilter);
         const initialSportIds = getSelectedValues(initialSportFilter);

         if(initialQuery === "" && initialTypeIds.length === 0
            && initialSportIds.length === 0 && activityDate === "")
         {
            clearRows();
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
      if(!(select instanceof HTMLSelectElement)
         || typeof cookieValue !== "string"
         || cookieValue.trim() === "")
      {
         return;
      }

      const selectedValues = cookieValue
         .split(",")
         .map(value => value.trim())
         .filter(value => value !== "");
      const availableValues = new Set(
         Array.from(select.options).map(option => option.value)
      );
      const selectedSet = new Set(
         selectedValues.filter(value => availableValues.has(value))
      );

      Array.from(select.options).forEach(option => {
         option.selected = selectedSet.has(option.value);
      });

      if(selectedSet.size > 0
         && select._multiSelect
         && typeof select._multiSelect.setValues === "function")
      {
         select._multiSelect.setValues([...selectedSet]);
      }
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
            const status = form.querySelector("[data-person-facts-status]");
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
               status.textContent = error instanceof Error
                  ? error.message
                  : "Facts job failed.";
               status.classList.add("form-status-error");
            }
            finally
            {
               button.disabled = false;
            }
         });
      });
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

      document.cookie = `${encodeURIComponent(name.trim())}=` +
         `${encodeURIComponent(value ?? "")}; path=/; sameSite=lax; ` +
         "max-age=31536000";
   }
})();
