namespace SESport.AI.Providers;

internal static class WebPageNormalizationScript
{
   internal static string Build()
   {
      return """
         (countryNamesJson) => {
            const countryNames = JSON.parse(countryNamesJson);
            const flagClassPattern =
               /(?:^|\s)flag(?:--|-|_)?([a-z0-9_]+)?(?:\s|$)/i;
            const flagCodePattern = /^[a-z]{2,3}$/i;
            const flagNoisePatterns = [
               /^(?:flag|flags)(?:\s+(?:of|for|from))?\s+/i,
               /\s+(?:flag|flags)(?:\s+(?:icon|image|symbol))?$/i
            ];
            const flagLabelPatterns = [
               /(?:^|[^a-z0-9])flag(?:s)?(?:[-_\/#]*)([a-z]{2,3})/i,
               /(?:^|[^a-z0-9])([a-z]{2,3})(?:[-_\/#]*)(?:flag)(?:s)?/i,
               /(?:^|[^a-z0-9])Flag_of_([A-Za-z_]+)(?:[^a-z0-9]|$)/i
            ];
            const genericFlagLabels = new Set([
               'icon',
               'image',
               'symbol'
            ]);

            function normalizeFlagLabel(label) {
               if(typeof label !== 'string') {
                  return null;
               }

               let normalizedLabel = label.replace(/\s+/g, ' ').trim();

               if(normalizedLabel === '') {
                  return null;
               }

               for(const pattern of flagNoisePatterns) {
                  normalizedLabel = normalizedLabel.replace(pattern, '');
               }

               normalizedLabel = normalizedLabel.trim();

               if(genericFlagLabels.has(normalizedLabel.toLowerCase())) {
                  return null;
               }

               return normalizedLabel === '' ? null : normalizedLabel;
            }

            function getFlagLabelFromSourceCandidate(source) {
               if(typeof source !== 'string' || source === '') {
                  return null;
               }

               for(const pattern of flagLabelPatterns) {
                  const match = source.match(pattern);

                  if(match?.[1]) {
                     return normalizeFlagLabel(match[1].replaceAll('_', ' '));
                  }
               }

               return null;
            }

            function getFlagLabelFromSource(source) {
               if(typeof source !== 'string' || source === '') {
                  return null;
               }

               const sourceCandidates = source
                  .split(',')
                  .map(candidate => candidate.trim())
                  .filter(candidate => candidate !== '');

               for(const candidate of sourceCandidates) {
                  const urlCandidate = candidate.split(/\s+/)[0];
                  const label = getFlagLabelFromSourceCandidate(urlCandidate);

                  if(label) {
                     return label;
                  }
               }

               const nextImageMatch = source.match(
                  /\/_next\/image\?[^?#]*\burl=([^&\s,]+)/i
               );

               if(!nextImageMatch) {
                  return null;
               }

               let decodedSource = '';

               try {
                  decodedSource =
                     decodeURIComponent(nextImageMatch[1]);
               }
               catch {
                  return null;
               }

               return getFlagLabelFromSource(decodedSource);
            }

            function getClassFlagLabel(element) {
               const className = element.getAttribute('class') || '';
               const dataClass = element.getAttribute('data-class') || '';
               const classMatch = className.match(flagClassPattern);
               const dataClassMatch = dataClass.match(flagClassPattern);
               const classLabel =
                  normalizeFlagLabel(classMatch?.[1] || '');
               const dataClassLabel =
                  normalizeFlagLabel(dataClassMatch?.[1] || '');

               if(classLabel || dataClassLabel) {
                  return classLabel || dataClassLabel || null;
               }

               const tokens = [
                  ...className.split(/\s+/),
                  ...dataClass.split(/\s+/)
               ];
               const candidateCode = tokens
                  .flatMap(token => token.split(/[-_]/g))
                  .map(token => token.trim())
                  .find(token => flagCodePattern.test(token));

               return candidateCode || null;
            }

            function getAttributeFlagLabel(element, attributeName) {
               return normalizeFlagLabel(
                  element.getAttribute(attributeName) || ''
               );
            }

            function getSvgFlagLabel(element) {
               const svgElement =
                  element.tagName.toLowerCase() === 'svg'
                     ? element
                     : element.closest('svg');

               if(!svgElement) {
                  return null;
               }

               const useElements = svgElement.querySelectorAll(
                  'use[href], use[xlink\\:href]'
               );

               for(const useElement of useElements) {
                  const href =
                     useElement.getAttribute('href') ||
                     useElement.getAttribute('xlink:href') ||
                     '';
                  const label = getFlagLabelFromSource(href);

                  if(label) {
                     return label;
                  }
               }

               return null;
            }

            function getFlagTarget(element) {
               if(element.tagName.toLowerCase() === 'use') {
                  return element.closest('svg') || element;
               }

               return element;
            }

            function getFlagLabel(element) {
               const tagName = element.tagName.toLowerCase();

               if(tagName === 'img') {
                  return getFlagLabelFromSource(
                     element.getAttribute('src') || ''
                  ) ||
                     getFlagLabelFromSource(
                        element.getAttribute('srcset') || ''
                     ) ||
                     getAttributeFlagLabel(element, 'alt') ||
                     getAttributeFlagLabel(element, 'title') ||
                     getAttributeFlagLabel(element, 'aria-label');
               }

               const svgLabel = getSvgFlagLabel(element);

               if(svgLabel) {
                  return svgLabel;
               }

               return getClassFlagLabel(getFlagTarget(element));
            }

            const seenFlagTargets = new Set();
            document.querySelectorAll(
               'img, svg, use[href], use[xlink\\:href], ' +
               '[class*="flag"], [data-class*="flag"]'
            ).forEach((element) => {
               const targetElement = getFlagTarget(element);
               const targetKey = targetElement;

               if(seenFlagTargets.has(targetKey)) {
                  return;
               }

               const label = getFlagLabel(element);

               if(!label) {
                  return;
               }

               seenFlagTargets.add(targetKey);

               const normalizedLabel =
                  countryNames[label.toUpperCase()] ||
                  label;

               targetElement.replaceWith(
                  document.createTextNode(` ${normalizedLabel} `)
               );
            });

            document.querySelectorAll(
               'nav, footer, aside, [role="dialog"], ' +
               '[role="banner"], [aria-modal="true"], ' +
               '[class*="modal"], [class*="overlay"], ' +
               '[class*="consent"], [class*="privacy"], ' +
               '[class*="banner"]'
            ).forEach((element) => element.remove());

            function normalizeText(text) {
               return (text || '').replace(/\s+/g, ' ').trim();
            }

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
                     const cellText = normalizeText(
                        cell.textContent || ''
                     );

                     if(cellText !== '') {
                        parts.push(cellText);
                     }
                  });

                  const rowText = parts.join(' | ').trim();

                  if(rowText !== '') {
                     lines.push(rowText);
                  }
               });

               if(lines.length > 0) {
                  tableElement.replaceWith(
                     document.createTextNode(` ${lines.join('\n')} `)
                  );
               }
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
