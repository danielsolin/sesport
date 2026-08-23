// Admin UI shared state and selectors.
// Loaded before site.js; the files intentionally share the classic-script scope.

const enhancedFormSelector =
   "form[data-ajax-success]:not([data-ajax-success=''])";
const replacementFormSelector = "form[data-ajax-replace-target]";
const checkboxToggleSelector = "[data-checkbox-toggle]";
const checkboxVisibilitySelector = "[data-visible-when-checkbox-group]";
const entityTypeSelectSelector = "[data-entity-type-select]";
const personGenderFieldSelector = "[data-person-gender-field]";
const personBirthdateFieldSelector =
   "[data-person-birthdate-field]";
const personHeightFieldSelector = "[data-person-height-field]";
const personWeightFieldSelector = "[data-person-weight-field]";
const personFormativeClubFieldSelector =
   "[data-person-formative-club-field]";
const entityInlineEditUrlSelector = "[data-entity-inline-edit-url]";
const entityInlineEditCellSelector =
   "[data-entity-inline-edit-field]";
const entityInlineEditDisplaySelector =
   "[data-entity-inline-edit-display]";
const entityInlineEditInputSelector =
   "[data-entity-inline-edit-input]";
const generateTeaserSelector = "[data-generate-teaser]";
const findFactsSelector = "[data-find-facts]";
const activityStartCheckSelector =
   "[data-activity-start-check]";
const activityResultCheckSelector =
   "[data-activity-result-check]";
const activityFactsCheckSelector =
   "[data-activity-facts-check]";
const checkParticipationRowSelector =
   "[data-check-participation-row]";
const participationRunsToggleSelector =
   "[data-participation-runs-toggle]";
const participationCellSelector = "[data-participation-cell]";
const participationStatusUrlSelector =
   "[data-check-participation-status-url]";
const adminDateInputSelector = "input[type='date']";
const runStatusesUrlSelector = "[data-run-statuses-url]";
const runInlineEditUrlSelector = "[data-run-inline-edit-url]";
const runRowSelector = "[data-ai-run-id]";
const runStatusCellSelector = "[data-ai-run-status-cell]";
const runStatusTextSelector = "[data-ai-run-status-text]";
const runSummaryCellSelector = "[data-ai-run-summary-cell]";
const activityFactsCheckStatusSelector =
   "[data-facts-check-status]";
const runPayloadCellSelector = "[data-ai-run-payload-cell]";
const runRoundsCellSelector = "[data-ai-run-rounds-cell]";
const runDurationCellSelector = "[data-ai-run-duration-cell]";
const runInlineEditCellSelector = "[data-run-inline-edit-field]";
const runInlineEditDisplaySelector = "[data-run-inline-edit-display]";
const runInlineEditInputSelector = "[data-run-inline-edit-input]";
const runInlineEditField = "execution-environment";
const activityAiResultInlineEditUrlSelector =
   "[data-ai-result-edit-url]";
const activityAiResultInlineEditCellSelector =
   "[data-ai-result-edit-field]";
const activityAiResultInlineEditDisplaySelector =
   "[data-ai-result-edit-display]";
const activityAiResultInlineEditInputSelector =
   "[data-ai-result-edit-input]";
const activityAiResultInlineEditField = "value";
const activityAiResultInlineEditDefaultPlaceholder = "Add value..";
const broadcastInlineEditCellSelector =
   "[data-broadcast-inline-edit-field]";
const broadcastInlineEditUrlSelector =
   "[data-broadcast-inline-edit-url]";
const broadcastResultsSelector = "[data-broadcast-results]";
const broadcastRowSelector = "tr[data-broadcast-row='true']";
const broadcastRunsRowSelector =
   ".broadcast-participation-runs-row";
const broadcastGroupParticipantsClearSelector =
   "[data-broadcast-group-participants-clear]";
const broadcastActivityLinkSelector =
   "[data-broadcast-activity-link]";
const clearParticipantsQueryKey = "clearParticipants";
const broadcastInlineEditTitleField = "title";
const getBroadcastInlineEditUrl =
   window.getBroadcastInlineEditUrl;
const postBroadcastInlineEditAsync =
   window.postBroadcastInlineEditAsync;
const getAntiForgeryToken = window.getAntiForgeryToken;
const pendingParticipationIds = new Set();
const queuingParticipationIds = new Set();
const pendingRunIds = new Set();
let participationPollingTimer = null;
let participationPollingInFlight = false;
let runPollingTimer = null;
let runPollingInFlight = false;
const getFormSelector = "form[method='get']";
const exclusiveEmptySelectSelector = "select[data-empty-option='exclusive']";
const exclusiveEmptySelectStates = new WeakMap();
const multiSelectScrollPositions = new WeakMap();
function normalizeString(value)
{
   if(typeof value !== "string")
   {
      return "";
   }

   return value.trim();
}

function normalizeNullableString(value)
{
   if(value === null || typeof value === "undefined")
   {
      return "";
   }

   if(typeof value !== "string")
   {
      return String(value).trim();
   }

   return value.trim();
}

function isTouchEditInteraction()
{
   const mediaQuery = window.matchMedia?.(
      "(hover: none) and (pointer: coarse)"
   );

   return mediaQuery?.matches ?? false;
}
