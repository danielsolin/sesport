# Saker att göra

På svenska eftersom detta inte är en del av produkten.

1. Löst: Kanalen "Horse & Country TV" / "Horse \u0026 Country TV" låg kvar
   eftersom EPG-källan innehöll bokstavliga `\u0026` i kanalnamnet.
   Importen normaliserar nu den formen, och cleanup-steget rensar även gamla
   rows som sparats med den escaped varianten.
