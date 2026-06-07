# Saker att göra

(Detta dokument är på svenska eftersom detta inte är en del av produkten.)

- Gå igenom dom tre projekten i ./src och diskutera vad som ska
  flyttas/ändras/tas bort för att få en mer hållbar struktur. Som det är nu
  blir det svårt att både hitta och förstå.
    - Kolla på \*Repository\*cs\* - ligger utspridda och behöver konsolideras
      på en plats.
    - Ska 'SESport.Web/Data' verkligen ligga där?
    - Det finns ett 'Ingestion'-namespace både i SESport.Core och SESport.Data.
      Varför?