# Saker att göra

(Detta dokument är på svenska eftersom detta inte är en del av produkten.)

- Gå igenom dom tre projekten i ./src och diskutera vad som ska
  flyttas/ändras/tas bort för att få en mer hållbar struktur. Som det är nu
  blir det ibland svårt att både hitta och förstå.
    - Kolla på \*Repository\*cs\* - ligger utspridda och behöver konsolideras
      på en plats.
    - Ska 'SESport.Web/Data' verkligen ligga där?
    - Det finns ett 'Ingestion'-namespace både i SESport.Core och SESport.Data.
      Så ska det kanske inte vara.

## Current structure notes

- `src/SESport.AI` is the active AI platform.
- The active AI code now lives physically under `src/SESport.AI/AI`.
- `src/SESport.Core/AIActivitySearch` and the `tools/SESport.AIActivitySearch*`
  projects are the legacy AI activity search path. Treat that path as
  deprecated and avoid extending it for new work.
- `src/SESport.Web/Data` mixes page-adjacent helpers with repositories. That
  split is the main place to simplify next.
