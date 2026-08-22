(() => {
   const broadcastInlineEditUrlSelector =
      "[data-broadcast-inline-edit-url]";
   const broadcastResultsSelector = "[data-broadcast-results]";
   const placeholders = Object.freeze({
      title: "Add title..",
      channel: "Add channel..",
      "start-time": "Add start time..",
      "end-time": "Add end time..",
      description: "Add description..",
      categories: "Add categories..",
      organization: "Add organization..",
      group: "Add group.."
   });

   window.getBroadcastInlineEditUrl = getBroadcastInlineEditUrl;
   window.postBroadcastInlineEditAsync = postBroadcastInlineEditAsync;
   window.getBroadcastInlineEditPlaceholder = getBroadcastInlineEditPlaceholder;
   window.getBroadcastSearchUrlBase = getBroadcastSearchUrlBase;
   window.getAntiForgeryToken = getAntiForgeryToken;

   function getBroadcastInlineEditUrl()
   {
      const container = document.querySelector(
         broadcastInlineEditUrlSelector
      );

      return container instanceof HTMLElement
         ? (container.dataset.broadcastInlineEditUrl ?? "").trim()
         : "";
   }

   async function postBroadcastInlineEditAsync(
      url,
      broadcastId,
      field,
      value,
      activityGroupId = ""
   )
   {
      const formData = new URLSearchParams();
      const token = getAntiForgeryToken();

      if(token)
      {
         formData.append("__RequestVerificationToken", token);
      }

      formData.append("id", broadcastId);
      formData.append("field", field);
      formData.append("value", value);

      if(activityGroupId !== "")
      {
         formData.append("activityGroupId", activityGroupId);
      }

      appendBroadcastContext(formData);

      return window.loadPartialAsync(url, {
         method: "post",
         body: formData
      });
   }

   function appendBroadcastContext(formData)
   {
      const container = document.querySelector(broadcastResultsSelector);

      if(!(container instanceof HTMLElement))
      {
         return;
      }

      appendDataValue(formData, "date", container.dataset.broadcastDate);
      appendDataValue(
         formData,
         "sortColumn",
         container.dataset.broadcastSortColumn
      );
      appendDataValue(
         formData,
         "sortAsc",
         container.dataset.broadcastSortAsc
      );
      appendDataValue(
         formData,
         "showHidden",
         container.dataset.broadcastShowHidden
      );
      appendDataValue(
         formData,
         "hideReplays",
         container.dataset.broadcastHideReplays
      );

      let selectedSports = [];

      try
      {
         const parsed = JSON.parse(
            container.dataset.broadcastSelectedSports ?? "[]"
         );
         selectedSports = Array.isArray(parsed) ? parsed : [];
      }
      catch
      {
         selectedSports = [];
      }

      selectedSports
         .filter(sport => typeof sport === "string" && sport.trim() !== "")
         .forEach(sport => formData.append("SelectedSports", sport));
   }

   function appendDataValue(formData, name, value)
   {
      if(typeof value === "string" && value.trim() !== "")
      {
         formData.append(name, value);
      }
   }

   function getBroadcastInlineEditPlaceholder(field)
   {
      const normalizedField = typeof field === "string"
         ? field.trim()
         : "";

      return placeholders[normalizedField] ?? "Add value..";
   }

   function getBroadcastSearchUrlBase()
   {
      const container = document.querySelector(broadcastResultsSelector);

      return container instanceof HTMLElement
         ? (container.dataset.searchUrlBase ?? "").trim()
         : "";
   }

   function getAntiForgeryToken()
   {
      const tokenField = document.querySelector(
         "input[name='__RequestVerificationToken']"
      );

      return tokenField instanceof HTMLInputElement
         ? tokenField.value
         : "";
   }
})();
