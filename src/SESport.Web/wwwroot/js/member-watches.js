(() => {
   const containerSelector = "[data-member-watch-search]";
   const formSelector = "[data-member-watch-search-form]";
   const inputSelector = "[data-member-watch-search-input]";
   const resultsSelector = "[data-member-watch-search-results]";
   const debounceMs = 220;

   const initializeSearch = root => {
      root.querySelectorAll(containerSelector).forEach(initializeContainer);
   };

   if(document.readyState === "loading")
   {
      document.addEventListener("DOMContentLoaded", () => {
         initializeSearch(document);
      });
   }
   else
   {
      initializeSearch(document);
   }

   function initializeContainer(container)
   {
      if(!(container instanceof HTMLElement)
         || container.dataset.memberWatchInitialized === "true")
      {
         return;
      }

      const form = container.querySelector(formSelector);
      const input = container.querySelector(inputSelector);
      const results = container.querySelector(resultsSelector);
      const searchUrl = (
         container.dataset.memberWatchSearchUrl ?? ""
      ).trim();

      if(!(form instanceof HTMLFormElement)
         || !(input instanceof HTMLInputElement)
         || !(results instanceof HTMLElement)
         || searchUrl === "")
      {
         return;
      }

      container.dataset.memberWatchInitialized = "true";
      const state = {
         timerId: null,
         requestId: 0,
         controller: null
      };

      const setExpanded = value => {
         input.setAttribute("aria-expanded", value ? "true" : "false");
      };

      const hideResults = () => {
         results.hidden = true;
         results.removeAttribute("aria-busy");
         results.replaceChildren();
         setExpanded(false);
      };

      const cancelSearch = () => {
         state.requestId += 1;
         state.controller?.abort();
         state.controller = null;

         if(state.timerId !== null)
         {
            window.clearTimeout(state.timerId);
            state.timerId = null;
         }
      };

      const closeResults = () => {
         cancelSearch();
         hideResults();
      };

      const showStatus = text => {
         const status = document.createElement("p");
         status.className = "member-watches-search-status";
         status.textContent = text;
         results.replaceChildren(status);
         results.hidden = false;
         setExpanded(true);
      };

      const renderResults = html => {
         const template = document.createElement("template");
         template.innerHTML = html;
         results.replaceChildren(template.content.cloneNode(true));
         results.hidden = false;
         setExpanded(true);
      };

      const getActionButtons = () => Array.from(
         results.querySelectorAll(".member-watch-action-button")
      ).filter(button => button instanceof HTMLButtonElement);

      const focusActionButton = index => {
         const buttons = getActionButtons();
         if(buttons.length === 0)
         {
            return;
         }

         const normalizedIndex = (index + buttons.length)
            % buttons.length;
         buttons[normalizedIndex].focus();
      };

      const search = async () => {
         const query = input.value.trim();
         const requestId = ++state.requestId;

         if(query === "")
         {
            hideResults();
            return;
         }

         state.controller?.abort();
         const controller = new AbortController();
         state.controller = controller;
         results.setAttribute("aria-busy", "true");
         showStatus("Söker...");

         const url = new URL(searchUrl, window.location.origin);
         url.searchParams.set("q", query);

         try
         {
            const response = await fetch(url, {
               signal: controller.signal,
               headers: { Accept: "text/html" }
            });

            if(!response.ok)
            {
               throw new Error("Member search failed.");
            }

            const html = await response.text();
            if(requestId !== state.requestId)
            {
               return;
            }

            renderResults(html);
         }
         catch(error)
         {
            if(error instanceof DOMException
               && error.name === "AbortError")
            {
               return;
            }

            if(requestId === state.requestId)
            {
               hideResults();
            }
         }
         finally
         {
            if(requestId === state.requestId)
            {
               results.removeAttribute("aria-busy");
            }
         }
      };

      const scheduleSearch = () => {
         if(state.timerId !== null)
         {
            window.clearTimeout(state.timerId);
         }

         if(input.value.trim() === "")
         {
            closeResults();
            return;
         }

         state.timerId = window.setTimeout(() => {
            state.timerId = null;
            void search();
         }, debounceMs);
      };

      input.addEventListener("input", () => {
         cancelSearch();
         hideResults();
         scheduleSearch();
      });

      input.addEventListener("focus", () => {
         if(input.value.trim() !== "")
         {
            scheduleSearch();
         }
      });

      input.addEventListener("keydown", event => {
         if(event.key === "Escape")
         {
            event.preventDefault();
            closeResults();
         }
         else if(event.key === "ArrowDown"
            && !results.hidden)
         {
            if(getActionButtons().length > 0)
            {
               event.preventDefault();
               focusActionButton(0);
            }
         }
         else if(event.key === "ArrowUp"
            && !results.hidden)
         {
            if(getActionButtons().length > 0)
            {
               event.preventDefault();
               focusActionButton(-1);
            }
         }
      });

      form.addEventListener("submit", event => {
         event.preventDefault();
         scheduleSearch();
      });

      results.addEventListener("keydown", event => {
         const buttons = getActionButtons();
         const activeIndex = buttons.indexOf(document.activeElement);

         if(activeIndex < 0)
         {
            return;
         }

         if(event.key === "ArrowDown")
         {
            event.preventDefault();
            focusActionButton(activeIndex + 1);
         }
         else if(event.key === "ArrowUp")
         {
            event.preventDefault();
            if(activeIndex === 0)
            {
               input.focus();
            }
            else
            {
               focusActionButton(activeIndex - 1);
            }
         }
         else if(event.key === "Escape")
         {
            event.preventDefault();
            closeResults();
            input.focus();
         }
      });

      input.addEventListener("blur", () => {
         window.setTimeout(() => {
            if(!results.contains(document.activeElement))
            {
               closeResults();
            }
         }, 120);
      });
   }
})();
