(() => {
   function getHtmlRoot(html)
   {
      const template = document.createElement("template");
      template.innerHTML = html.trim();
      const elements = Array.from(template.content.children);

      if(elements.length !== 1)
      {
         throw new Error("A partial must contain exactly one root element.");
      }

      return elements[0];
   }

   async function loadPartialAsync(url, options = {})
   {
      const headers = new Headers(options.headers || {});
      headers.set("Accept", "text/html");

      const response = await fetch(url, {
         ...options,
         headers
      });
      const html = await response.text();

      if(!response.ok)
      {
         throw new Error(
            html.trim() || `Request failed with status ${response.status}`
         );
      }

      return html;
   }

   function replaceElementWithHtml(target, html)
   {
      if(!(target instanceof Element))
      {
         throw new Error("A partial replacement target is required.");
      }

      const replacement = getHtmlRoot(html);
      target.replaceWith(replacement);
      return replacement;
   }

   function replaceContentsWithHtml(target, html)
   {
      if(!(target instanceof Element))
      {
         throw new Error("A partial content target is required.");
      }

      const replacement = getHtmlRoot(html);
      target.replaceChildren(...replacement.childNodes);
      return target;
   }

   window.loadPartialAsync = loadPartialAsync;
   window.getPartialRootFromHtml = getHtmlRoot;
   window.replaceElementWithPartialHtml = replaceElementWithHtml;
   window.replaceContentsWithPartialHtml = replaceContentsWithHtml;
})();
