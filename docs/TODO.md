# Saker att göra

På svenska eftersom detta inte är en del av produkten.

1. Kanalen "Horse & Country TV" / "Horse \u0026 Country TV" skal ignoreras i
   EPG-import, men den verkar komma med ändå. Eller, är det bara gammalt data
   jag ser? Alltså data som importerades innan ignore-regeln implementerades?
   Kanalen ligger importerad som "Horse \u0026 Country TV" i db, så tänker att
   det kanske är något problem med encodingen i ignore-filtret i importen.

   Viktigt! Som jag minns det skulle EPG-importen automatiskt rensa DB på redan
   existerande records som lagts till i ignore-listan efter det att dom
   importerades. Undersök detta också.