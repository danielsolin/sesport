// Admin UI bootstrap.
// Feature implementations live in the neighboring admin-*.js files.

(() => {
   window.submitFilterForm = submitFilterForm;
   window.isTouchEditInteraction = isTouchEditInteraction;
   initializeExclusiveEmptySelects();
   initializeMultiSelectScrollRetention();
   initializeMultiSelectClearButtons();
   window.initializeEntitySearch?.(document);
   initializePersonGenderVisibility();
   initializeGetFormRestoration();
   initializeAdminDateSteppers();
   initializeEntityInlineEditing();
   window.initializeEntityInlineEditing = initializeEntityInlineEditing;
   window.initializeBroadcastInlineEditing =
      initializeBroadcastInlineEditing;
   window.initializeParticipationRunsAsync =
      initializeParticipationRunsAsync;
   initializeTeaserGeneration();
   initializeActivityStartChecks();
   initializeActivityResultChecks();
   initializeActivityFactsChecks();
   initializeParticipationRowChecks();
   initializeBroadcastParticipantClearing();
   void initializeParticipationRunsAsync();
   initializeBroadcastInlineEditing();
   if(typeof window.initializeBroadcastOrganizationAutocomplete === "function")
   {
      window.initializeBroadcastOrganizationAutocomplete();
   }
   initializeParticipationPolling();
   initializeRunPolling();
   initializeRunInlineEditing();
   initializeActivityAiResultInlineEditing();
})();
