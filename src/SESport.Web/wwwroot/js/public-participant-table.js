(() => {
   const tableSelector = "[data-participant-table]";
   const sortSelector = "[data-participant-sort]";
   const collator = new Intl.Collator("sv", {
      numeric: true,
      sensitivity: "base"
   });

   document.querySelectorAll(tableSelector).forEach(table => {
      if(!(table instanceof HTMLTableElement))
      {
         return;
      }

      table.querySelectorAll(sortSelector).forEach(button => {
         button.addEventListener("click", () => {
            const key = button.dataset.participantSort ?? "";
            const currentKey = table.dataset.participantSortKey ?? "";
            const currentDirection =
               table.dataset.participantSortDirection ?? "ascending";
            const direction = currentKey === key &&
               currentDirection === "ascending"
               ? "descending"
               : "ascending";

            sortTable(table, key, direction);
         });
      });

      if(table.classList.contains("activity-participant-table-has-start-time"))
      {
         table.dataset.participantSortKey = "start-time";
         table.dataset.participantSortDirection = "ascending";
         updateSortHeaders(table, "start-time", "ascending");
      }

      const collapseButton = getCollapseButton(table);

      if(collapseButton !== null)
      {
         updateCollapseButton(table, collapseButton);
         collapseButton.addEventListener("click", () => {
            table.classList.toggle("activity-participant-table-collapsed");
            updateCollapseButton(table, collapseButton);
         });
      }
   });

   function sortTable(table, key, direction)
   {
      const body = table.tBodies[0];

      if(body === undefined)
      {
         return;
      }

      const rows = Array.from(body.rows).map((row, index) => ({
         row,
         index,
         value: getSortValue(row, key)
      }));
      const multiplier = direction === "descending" ? -1 : 1;

      rows.sort((left, right) => {
         if(left.value === null && right.value === null)
         {
            return left.index - right.index;
         }

         if(left.value === null)
         {
            return 1;
         }

         if(right.value === null)
         {
            return -1;
         }

         const result = typeof left.value === "number"
            ? left.value - right.value
            : collator.compare(left.value, right.value);

         return result === 0
            ? left.index - right.index
            : result * multiplier;
      });

      body.append(...rows.map(item => item.row));
      table.dataset.participantSortKey = key;
      table.dataset.participantSortDirection = direction;
      updateSortHeaders(table, key, direction);
   }

   function getSortValue(row, key)
   {
      const value = getRawSortValue(row, key).trim();

      if(value === "")
      {
         return null;
      }

      if(key === "age")
      {
         const number = Number(value);
         return Number.isFinite(number) ? number : null;
      }

      if(key === "start-time")
      {
         return parseStartTime(value);
      }

      return value;
   }

   function getRawSortValue(row, key)
   {
      switch(key)
      {
         case "name":
            return row.dataset.participantName ?? "";
         case "age":
            return row.dataset.participantAge ?? "";
         case "start-time":
            return row.dataset.participantStartTime ?? "";
         case "club":
            return row.dataset.participantClub ?? "";
         default:
            return "";
      }
   }

   function updateSortHeaders(table, key, direction)
   {
      table.querySelectorAll(sortSelector).forEach(button => {
         const header = button.closest("th");

         if(!(header instanceof HTMLTableCellElement))
         {
            return;
         }

         header.setAttribute(
            "aria-sort",
            button.dataset.participantSort === key
               ? direction
               : "none"
         );
      });
   }

   function getCollapseButton(table)
   {
      const wrap = table.closest(".activity-participant-table-wrap");

      if(!(wrap instanceof HTMLElement))
      {
         return null;
      }

      const button = wrap.querySelector("[data-participant-toggle]");

      return button instanceof HTMLButtonElement ? button : null;
   }

   function updateCollapseButton(table, button)
   {
      const collapsed = table.classList.contains(
         "activity-participant-table-collapsed"
      );
      const label = collapsed
         ? button.dataset.collapsedLabel ?? "Visa alla"
         : button.dataset.expandedLabel ?? "Visa färre";

      button.textContent = label;
      button.setAttribute("aria-expanded", (!collapsed).toString());
   }

   function parseStartTime(value)
   {
      const match = /^(\d{1,2})[:.](\d{2})$/.exec(value);

      if(match === null)
      {
         return null;
      }

      const hours = Number(match[1]);
      const minutes = Number(match[2]);

      if(!Number.isFinite(hours) || !Number.isFinite(minutes))
      {
         return null;
      }

      return hours * 60 + minutes;
   }
})();
