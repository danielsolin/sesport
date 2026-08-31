(() => {
   const isMember = document.body?.dataset.memberAuthenticated === "true";
   if(!isMember)
   {
      return;
   }

   const containerSelector = "[data-member-watch-search]";
   const formSelector = "[data-member-watch-search-form]";
   const inputSelector = "[data-member-watch-search-input]";
   const resultsSelector = "[data-member-watch-search-results]";
   const addFormSelector = "[data-member-watch-add-form]";
   const addRowSelector = "[data-member-watch-add-row]";
   const autoSubmitFormSelector =
      "[data-member-watch-auto-submit-form]";
   const pushStatusSelector = "[data-member-watch-push-status]";
   const minimumSearchLength = 2;
   const debounceMs = 300;

   const initializeSearch = root => {
      root.querySelectorAll(pushStatusSelector).forEach(
         initializePushStatus
      );
      root.querySelectorAll(containerSelector).forEach(initializeContainer);
      root.querySelectorAll(autoSubmitFormSelector).forEach(form => {
         if(!(form instanceof HTMLFormElement)
            || form.dataset.memberWatchInitialized === "true")
         {
            return;
         }

         form.dataset.memberWatchInitialized = "true";
         form.addEventListener("change", () => form.submit());
      });
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

   function initializePushStatus(status)
   {
      if(!(status instanceof HTMLElement)
         || status.dataset.memberWatchPushInitialized === "true")
      {
         return;
      }

      if(status.dataset.memberWatchNotificationsEnabled !== "true")
      {
         return;
      }

      const statusText = status.querySelector(
         "[data-member-watch-push-status-text]"
      );
      const activateButton = status.querySelector(
         "[data-member-watch-push-activate]"
      );
      const registrationForm = status.querySelector(
         "[data-member-watch-push-registration-form]"
      );
      const subscriptionInput = status.querySelector(
         "[data-member-watch-push-subscription]"
      );
      const pushConfigured =
         status.dataset.memberWatchPushConfigured === "true";
      const vapidPublicKey = (
         status.dataset.memberWatchVapidPublicKey ?? ""
      ).trim();

      if(!(statusText instanceof HTMLElement)
         || !(activateButton instanceof HTMLButtonElement)
         || !(registrationForm instanceof HTMLFormElement)
         || !(subscriptionInput instanceof HTMLInputElement))
      {
         return;
      }

      status.dataset.memberWatchPushInitialized = "true";
      const registrationPromise =
         pushConfigured && "serviceWorker" in navigator
            ? navigator.serviceWorker.register(
               "/service-worker.js",
               { scope: "/" }
            ).catch(() => null)
            : null;

      const setMessage = (message, state = "") => {
         statusText.textContent = message;
         status.classList.toggle("is-active", state === "active");
         status.classList.toggle("is-error", state === "error");
      };

      const createPushError = message => {
         const error = new Error(message);
         error.name = "MemberWatchError";
         return error;
      };

      const getRegistration = async () => {
         if(!pushConfigured || vapidPublicKey === "")
         {
            throw createPushError(
               "Notiser är inte tillgängliga just nu."
            );
         }

         if(!("serviceWorker" in navigator)
            || !("PushManager" in window)
            || !("Notification" in window)
            || typeof Notification.requestPermission !== "function")
         {
            throw createPushError(
               "Notiser stöds inte i den här webbläsaren."
            );
         }

         if(registrationPromise === null)
         {
            throw createPushError(
               "Kunde inte förbereda notiser i webbläsaren."
            );
         }

         const registration = await registrationPromise;
         if(registration === null)
         {
            throw createPushError(
               "Kunde inte förbereda notiser i webbläsaren."
            );
         }

         return registration;
      };

      const createApplicationServerKey = value => {
         const padding = "=".repeat((4 - value.length % 4) % 4);
         const base64 = (value + padding)
            .replace(/-/g, "+")
            .replace(/_/g, "/");
         const binary = window.atob(base64);
         return Uint8Array.from(
            binary,
            character => character.charCodeAt(0)
         );
      };

      const getExistingSubscription = async () => {
         const registration = await getRegistration();
         return registration.pushManager.getSubscription();
      };

      const requestNotificationPermission = async () => {
         if(!("Notification" in window)
            || typeof Notification.requestPermission !== "function")
         {
            throw createPushError(
               "Notiser stöds inte i den här webbläsaren."
            );
         }

         if(Notification.permission === "granted")
         {
            return;
         }

         if(Notification.permission === "denied")
         {
            throw createPushError(
               "Tillåt notiser för sesport i webbläsarens inställningar."
            );
         }

         let permission;
         try
         {
            permission = await Notification.requestPermission();
         }
         catch
         {
            throw createPushError(
               "Webbläsaren kunde inte fråga om notisbehörighet."
            );
         }

         if(permission !== "granted")
         {
            throw createPushError(
               "Tillåt notiser för sesport i webbläsarens inställningar."
            );
         }
      };

      const getPushSubscription = async () => {
         // Request permission before awaiting anything else. Browsers require
         // this call to happen as part of the activation button click.
         await requestNotificationPermission();
         const registration = await getRegistration();

         let subscription;
         try
         {
            subscription =
               await registration.pushManager.getSubscription();
            if(subscription === null)
            {
               subscription = await registration.pushManager.subscribe({
                  userVisibleOnly: true,
                  applicationServerKey:
                     createApplicationServerKey(vapidPublicKey)
               });
            }
         }
         catch(error)
         {
            const errorName = error !== null
               && typeof error === "object"
               && "name" in error
               ? String(error.name)
               : "";
            const errorMessage = error !== null
               && typeof error === "object"
               && "message" in error
               ? String(error.message).toLowerCase()
               : "";

            if(errorName === "NotAllowedError"
               || errorMessage.includes("permission denied"))
            {
               throw createPushError(
                  "Tillåt notiser för sesport i webbläsarens "
                  + "inställningar."
               );
            }

            if(errorMessage.includes("push service error"))
            {
               throw createPushError(
                  "Webbläsaren kunde inte ansluta till sin "
                  + "push-tjänst. Kontrollera nätverk och "
                  + "webbläsarinställningar."
               );
            }

            throw createPushError(
               "Webbläsaren kunde inte aktivera notiser."
            );
         }

         return subscription;
      };

      const getSubscriptionJson = subscription => {
         if(subscription === null
            || typeof subscription !== "object")
         {
            return null;
         }

         const serializedSubscription =
            typeof subscription.toJSON === "function"
               ? subscription.toJSON()
               : subscription;
         return serializedSubscription !== null
            && typeof serializedSubscription === "object"
            ? serializedSubscription
            : null;
      };

      const registerSubscription = async subscription => {
         const serializedSubscription = getSubscriptionJson(
            subscription
         );
         if(serializedSubscription === null)
         {
            throw createPushError(
               "Webbläsaren skickade en ogiltig notisprenumeration."
            );
         }

         subscriptionInput.value = JSON.stringify(
            serializedSubscription
         );
         const response = await fetch(registrationForm.action, {
            method: "POST",
            body: new FormData(registrationForm),
            headers: { Accept: "text/plain" }
         });
         if(!response.ok)
         {
            throw createPushError(
               "Kunde inte registrera notiser just nu."
            );
         }
      };

      const showError = error => {
         const message = error instanceof Error
            && error.name === "MemberWatchError"
            ? error.message
            : "Kunde inte aktivera notiser just nu.";
         setMessage(message, "error");
         activateButton.hidden = "Notification" in window
            && Notification.permission === "denied";
      };

      if("serviceWorker" in navigator)
      {
         navigator.serviceWorker.addEventListener(
            "message",
            event => {
               const data = event.data;
               if(data === null
                  || typeof data !== "object"
                  || data.type !==
                     "sesport-push-subscription-change")
               {
                  return;
               }

               if(data.subscription === null)
               {
                  setMessage(
                     "Notiser behöver aktiveras igen.",
                     "error"
                  );
                  activateButton.hidden = false;
                  return;
               }

               void registerSubscription(data.subscription)
                  .then(() => {
                     setMessage(
                        "Notiser är aktiva på den här enheten.",
                        "active"
                     );
                     activateButton.hidden = true;
                  })
                  .catch(showError);
            }
         );
      }

      const inspect = async () => {
         if(!pushConfigured || vapidPublicKey === "")
         {
            setMessage("Notiser är inte tillgängliga just nu.");
            activateButton.hidden = true;
            return;
         }

         if(!("serviceWorker" in navigator)
            || !("PushManager" in window)
            || !("Notification" in window)
            || typeof Notification.requestPermission !== "function")
         {
            setMessage(
               "Notiser stöds inte i den här webbläsaren.",
               "error"
            );
            activateButton.hidden = true;
            return;
         }

         if("Notification" in window
            && Notification.permission === "denied")
         {
            setMessage(
               "Notiser är blockerade i webbläsaren.",
               "error"
            );
            activateButton.hidden = true;
            return;
         }

         try
         {
            const subscription = await getExistingSubscription();
            if(subscription === null)
            {
               setMessage(
                  "Notiser är inte aktiva på den här enheten.",
                  "error"
               );
               activateButton.hidden = false;
               return;
            }

            await registerSubscription(subscription);
            setMessage(
               "Notiser är aktiva på den här enheten.",
               "active"
            );
            activateButton.hidden = true;
         }
         catch(error)
         {
            showError(error);
         }
      };

      activateButton.addEventListener("click", async () => {
         activateButton.disabled = true;
         activateButton.textContent = "AKTIVERAR...";
         setMessage("Aktiverar notiser...");

         try
         {
            const subscription = await getPushSubscription();
            await registerSubscription(subscription);
            setMessage(
               "Notiser är aktiva på den här enheten.",
               "active"
            );
            activateButton.hidden = true;
         }
         catch(error)
         {
            showError(error);
         }
         finally
         {
            activateButton.disabled = false;
            if(!activateButton.hidden)
            {
               activateButton.textContent = "AKTIVERA NOTISER";
            }
         }
      });

      void inspect();
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
      const notificationsEnabled =
         container.dataset.memberWatchNotificationsEnabled === "true";
      const pushConfigured =
         container.dataset.memberWatchPushConfigured === "true";
      const serviceWorkerRegistrationPromise =
         notificationsEnabled
            && pushConfigured
            && "serviceWorker" in navigator
            ? navigator.serviceWorker.register(
               "/service-worker.js",
               { scope: "/" }
            ).catch(() => null)
            : null;

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
         const status = results.querySelector(
            "[data-member-watch-search-status]"
         );

         results.querySelectorAll(":scope > *").forEach(child => {
            if(child !== status
               && !(status instanceof Node && child.contains(status)))
            {
               child.remove();
            }
         });

         if(status instanceof HTMLElement)
         {
            status.hidden = true;
            status.classList.remove("is-error");
            status.textContent = "";
         }

         results.hidden = true;
         results.removeAttribute("aria-busy");
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
         const status = results.querySelector(
            "[data-member-watch-search-status]"
         );

         if(!(status instanceof HTMLElement))
         {
            return;
         }

         status.classList.remove("is-error");
         status.textContent = text;
         status.hidden = false;
         results.hidden = false;
         setExpanded(true);
      };

      const showPushError = text => {
         const status = results.querySelector(
            "[data-member-watch-search-status]"
         );

         if(!(status instanceof HTMLElement))
         {
            return;
         }

         status.classList.add("is-error");
         status.textContent = text;
         status.hidden = false;
         results.hidden = false;
         setExpanded(true);
      };

      const renderResults = html => {
         window.replaceContentsWithPartialHtml(results, html);
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

         if(query.length < minimumSearchLength)
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

         if(input.value.trim().length < minimumSearchLength)
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
         if(input.value.trim().length >= minimumSearchLength)
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

      const submitAddForm = form => {
         if(!(form instanceof HTMLFormElement))
         {
            return;
         }

         const button = form.querySelector(
            ".member-watch-action-button"
         );
         if(!(button instanceof HTMLButtonElement) || button.disabled)
         {
            return;
         }

         form.requestSubmit(button);
      };

      results.addEventListener("click", event => {
         const target = event.target;
         if(!(target instanceof Element)
            || target.closest(
               "a, button, input, select, textarea, label, " +
               ".member-watch-image-container"
            ) !== null)
         {
            return;
         }

         const row = target.closest(addRowSelector);
         if(!(row instanceof HTMLElement))
         {
            return;
         }

         submitAddForm(row.querySelector(addFormSelector));
      });

      const getExistingPushSubscription = async () => {
         if(!notificationsEnabled
            || !pushConfigured
            || !("serviceWorker" in navigator)
            || !("PushManager" in window)
            || serviceWorkerRegistrationPromise === null)
         {
            return null;
         }

         try
         {
            const registration = await serviceWorkerRegistrationPromise;
            if(registration === null)
            {
               return null;
            }

            return await registration.pushManager.getSubscription();
         }
         catch
         {
            return null;
         }
      };

      results.addEventListener("submit", async event => {
         const target = event.target;
         if(!(target instanceof HTMLFormElement)
            || !target.matches(addFormSelector))
         {
            return;
         }

         event.preventDefault();
         const button = target.querySelector(
            ".member-watch-action-button"
         );
         if(!(button instanceof HTMLButtonElement))
         {
            return;
         }

         button.disabled = true;
         try
         {
            const subscription = await getExistingPushSubscription();
            const subscriptionInput = target.querySelector(
               "input[name='pushSubscription']"
            );
            if(subscription !== null)
            {
               let input = subscriptionInput;

               if(input instanceof HTMLInputElement)
               {
                  input.value = JSON.stringify(subscription.toJSON());
               }
            }

            if(subscription === null
               && subscriptionInput instanceof HTMLInputElement)
            {
               subscriptionInput.value = "";
            }

            const response = await fetch(target.action, {
               method: "POST",
               body: new FormData(target)
            });
            if(!response.ok)
            {
               const message = (await response.text()).trim();
               throw new Error(
                  message ||
                  "Det gick inte att lägga till bevakningen just nu."
               );
            }

            window.location.reload();
         }
         catch(error)
         {
            button.disabled = false;
            const message = error instanceof Error
               ? error.message
               : "Det gick inte att aktivera notiser.";
            showPushError(message);
         }
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
