(() => {
   "use strict";

   const host = document.querySelector("[data-run-tool-trace]");

   if(!(host instanceof HTMLElement))
   {
      return;
   }

   const url = host.dataset.url;

   if(!url)
   {
      return;
   }

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

   const updateToolTrace = async () => {
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

         window.replaceContentsWithPartialHtml(
            host,
            await response.text()
         );
         restoreOpenPanels(openPanels);
      }
      catch(error)
      {
         setStatus("Failed to update tool trace.");
         console.error("Failed to update tool trace.", error);
      }
      finally
      {
         host.classList.remove("is-refreshing");
         host.classList.add("is-stopped");
      }
   };

   updateToolTrace();
})();
