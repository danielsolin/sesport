(() => {
   "use strict";

   const host = document.querySelector("[data-run-tool-trace]");
   const pollIntervalMilliseconds = 5000;

   if(!(host instanceof HTMLElement))
   {
      return;
   }

   const url = host.dataset.url;

   if(!url)
   {
      return;
   }

   let pollTimer = null;
   let countdownTimer = null;
   let secondsUntilUpdate = 0;
   let stopped = false;

   const getOpenPanels = () => {
      const panels = Array.from(
         host.querySelectorAll("details[data-tool-trace-panel]")
      );

      return {
         hasPanels: panels.length > 0,
         names: panels
            .filter(panel => panel.open)
            .map(panel => panel.dataset.toolTracePanel)
      };
   };

   const restoreOpenPanels = openPanels => {
      if(!openPanels.hasPanels)
      {
         return;
      }

      for(const panel of host.querySelectorAll(
         "details[data-tool-trace-panel]"
      ))
      {
         panel.open = openPanels.names.includes(
            panel.dataset.toolTracePanel
         );
      }
   };

   const setStatus = text => {
      const status = host.querySelector(
         "[data-run-tool-trace-status]"
      );

      if(status)
      {
         status.textContent = text;
      }
   };

   const stopCountdown = () => {
      if(countdownTimer !== null)
      {
         window.clearInterval(countdownTimer);
         countdownTimer = null;
      }
   };

   const startCountdown = () => {
      stopCountdown();
      secondsUntilUpdate = pollIntervalMilliseconds / 1000;
      setStatus(`Next update in ${secondsUntilUpdate} seconds`);

      countdownTimer = window.setInterval(() => {
         secondsUntilUpdate -= 1;

         if(secondsUntilUpdate > 0)
         {
            setStatus(
               `Next update in ${secondsUntilUpdate} seconds`
            );
         }
      }, 1000);
   };

   const schedulePoll = () => {
      if(stopped || host.dataset.runStatus !== "running")
      {
         return;
      }

      startCountdown();
      pollTimer = window.setTimeout(
         updateToolTrace,
         pollIntervalMilliseconds
      );
   };

   const updateToolTrace = async () => {
      stopCountdown();
      setStatus("Updating tool trace…");
      host.classList.remove("is-stopped");
      host.classList.add("is-refreshing");

      try
      {
         const openPanels = getOpenPanels();
         const response = await fetch(url, {
            headers: {
               "X-Requested-With": "XMLHttpRequest"
            }
         });

         if(!response.ok)
         {
            throw new Error(`Request failed with ${response.status}.`);
         }

         host.innerHTML = await response.text();
         restoreOpenPanels(openPanels);

         const statusSource = host.querySelector("[data-run-status]");

         if(statusSource instanceof HTMLElement)
         {
            host.dataset.runStatus = statusSource.dataset.runStatus;
         }
      }
      catch(error)
      {
         setStatus("Update failed. Retrying in 5 seconds");
         console.error("Failed to update tool trace.", error);
      }
      finally
      {
         host.classList.remove("is-refreshing");

         if(host.dataset.runStatus === "running")
         {
            schedulePoll();
         }
         else
         {
            host.classList.add("is-stopped");
            setStatus("Tool trace loaded");
         }
      }
   };

   window.addEventListener("pagehide", () => {
      stopped = true;
      stopCountdown();

      if(pollTimer !== null)
      {
         window.clearTimeout(pollTimer);
      }
   });

   updateToolTrace();
})();
