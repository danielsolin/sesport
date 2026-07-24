namespace SESport.AI.WebPages;

internal static class WebPageNormalizationScript
{
   internal static string Build()
   {
      return """
         () => {
            document.querySelectorAll(
               'nav, footer, aside, [role="dialog"], ' +
               '[role="banner"], [aria-modal="true"], ' +
               '[class*="modal"], [class*="overlay"], ' +
               '[class*="consent"], [class*="privacy"], ' +
               '[class*="banner"]'
            ).forEach((element) => element.remove());

            function flattenTableElement(tableElement) {
               const rowSelector = 'tr, [role="row"]';
               const cellSelector =
                  'th, td, [role="cell"], [role="gridcell"], ' +
                  '[role="columnheader"], [role="rowheader"]';
               const rows = tableElement.querySelectorAll(rowSelector);
               const lines = [];

               rows.forEach((row) => {
                  const cells = row.querySelectorAll(cellSelector);
                  const parts = [];

                  cells.forEach((cell) => {
                     const cellHtml = cell.innerHTML.trim();

                     if(cellHtml !== '') {
                        parts.push(cellHtml);
                     }
                  });

                  const rowHtml = parts.join(' | ').trim();

                  if(rowHtml !== '') {
                     lines.push(rowHtml);
                  }
               });

               if(lines.length === 0) {
                  return;
               }

               const rowContainer = document.createElement('div');

               lines.forEach((line) => {
                  const rowElement = document.createElement('div');
                  rowElement.innerHTML = line;
                  rowContainer.appendChild(rowElement);
               });

               tableElement.replaceWith(rowContainer);
            }

            document.querySelectorAll(
               'table, [role="table"], [role="grid"], [role="treegrid"]'
            ).forEach((tableElement) => {
               if(!tableElement.isConnected) {
                  return;
               }

               if(tableElement.closest(
                  'table, [role="table"], [role="grid"], [role="treegrid"]'
               ) !== tableElement) {
                  return;
               }

               flattenTableElement(tableElement);
            });
         }
         """;
   }
}
