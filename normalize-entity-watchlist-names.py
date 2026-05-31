import json
from pathlib import Path

path = Path("entity-watchlist.json")

replacements = {
    "Sweden men's football national team": "Herrlandslaget i fotboll",
    "Sweden women's football national team": "Damlandslaget i fotboll",
    "Sweden men's handball national team": "Herrlandslaget i handboll",
    "Sweden women's handball national team": "Damlandslaget i handboll",
    "Sweden men's basketball national team": "Herrlandslaget i basket",
    "Sweden women's basketball national team": "Damlandslaget i basket",
    "Sweden men's floorball national team": "Herrlandslaget i innebandy",
    "Sweden women's floorball national team": "Damlandslaget i innebandy",
    "Sweden men's beach volleyball national team": "Herrlandslaget i beachvolley",
    "Sweden cross-country skiing team": "Längdlandslaget",
    "Sweden biathlon team": "Skidskyttelandslaget",
    "Swedish alpine ski team": "Alpina landslaget",
    "Swedish Olympic Committee": "Svenska Olympiska Kommittén",
    "Swedish Football Association": "Svenska Fotbollförbundet",
    "Swedish Ice Hockey Association": "Svenska Ishockeyförbundet",
    "Swedish Ski Association": "Svenska Skidförbundet",
    "Swedish Biathlon Federation": "Svenska Skidskytteförbundet",
    "Swedish Athletics Federation": "Svensk Friidrott",
    "Swedish Handball Federation": "Svenska Handbollförbundet",
    "Swedish Basketball Federation": "Svenska Basketbollförbundet",
    "Swedish Volleyball Federation": "Svenska Volleybollförbundet",
    "Swedish Sailing Federation": "Svenska Seglarförbundet",
    "Rally Sweden": "Svenska Rallyt",
    "Sweden International Horse Show": "Sweden International Horse Show",
    "Viktor Gyökeres family/relationship cluster": "Viktor Gyökeres relationskluster",
    "Öberg sisters": "Systrarna Öberg",
    "Team Sweden showjumping core": "Svenska hopplandslagets kärngrupp",
    "Hammarby women in Europe cluster": "Hammarby damer i Europa",
    "BK Häcken women in Europe cluster": "BK Häcken damer i Europa",
    "Rebecca Peterson return watch": "Rebecca Peterson comebackbevakning",
    "Swedish women’s basketball depth cluster": "Svensk dambasket breddgrupp",
    "Fredrik Lindgren speedway cluster": "Fredrik Lindgren speedwaybevakning",
}

data = json.loads(path.read_text(encoding="utf-8"))

before_count = len(data["entities"])
changed = []

for entity in data["entities"]:
    old = entity.get("name")
    new = replacements.get(old)
    if new and new != old:
        entity["name"] = new
        changed.append((old, new))

after_count = len(data["entities"])

if before_count != after_count:
    raise RuntimeError(f"Entity count changed: {before_count} -> {after_count}")

path.write_text(
    json.dumps(data, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)

print(f"Entities: {before_count}")
print(f"Changed names: {len(changed)}")
for old, new in changed:
    print(f"- {old} -> {new}")
