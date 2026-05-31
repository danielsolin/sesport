param(
    [string]$WatchlistPath = "data/entity-watchlist.json",
    [string]$OutputPath = "data/entity-activity-probes.jsonl",
    [string]$Endpoint = "http://localhost:1234/v1/chat/completions",
    [string]$Model = "local-model",
    [int]$MaxEntities = 0,
    [int]$StartIndex = 0,
    [int]$DelayMs = 250,
    [switch]$Resume,
    [switch]$OnlyTier1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-JsonFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "File not found: $Path"
    }

    $raw = Get-Content -Raw -Encoding UTF8 $Path
    return $raw | ConvertFrom-Json -Depth 100
}

function ConvertTo-CompactJson {
    param([object]$Value)
    return ($Value | ConvertTo-Json -Depth 100 -Compress)
}

function Get-CompletedEntityIds {
    param([string]$Path)

    $ids = [System.Collections.Generic.HashSet[string]]::new()

    if (-not (Test-Path $Path)) {
        return $ids
    }

    foreach ($line in Get-Content -Encoding UTF8 $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        try {
            $obj = $line | ConvertFrom-Json -Depth 100
            if ($obj.entityId) {
                [void]$ids.Add([string]$obj.entityId)
            }
        }
        catch {
            Write-Warning "Skipping invalid JSONL line in $Path"
        }
    }

    return $ids
}

function New-EntityPrompt {
    param(
        [object]$Entity,
        [datetime]$Today,
        [datetime]$Until
    )

    $entityJson = ConvertTo-CompactJson $Entity
    $todayText = $Today.ToString("yyyy-MM-dd")
    $untilText = $Until.ToString("yyyy-MM-dd")

    return @"
Du är en svensk sportbevakningsassistent.

Uppgift:
Givet exakt en watchlist-entitet ska du avgöra om det finns någon PLANERAD aktivitet för entiteten under perioden $todayText till $untilText, inklusive båda datumen.

Viktigt:
- Hitta inte på.
- Om du inte säkert vet, returnera found=false.
- Använd bara information som finns i din kunskap eller i entitetens metadata.
- Aktivitet betyder t.ex. match, tävling, uttagning, turnering, mästerskap, planerat event, deadline, presskonferens eller annan konkret kalenderhändelse.
- Generella formuleringar som "kan vara aktuell", "spelar ofta", "brukar delta" räcker inte.
- Returnera ENDAST giltig JSON. Ingen markdown. Ingen förklaring runtom.

Entitet:
$entityJson

Returnera detta schema:

{
  "found": true eller false,
  "confidence": "high" | "medium" | "low",
  "entityId": "<id från entiteten>",
  "entityName": "<name från entiteten>",
  "activityTitle": "<kort titel eller null>",
  "activityType": "<match|competition|tournament|selection|event|other|null>",
  "startDate": "YYYY-MM-DD eller null",
  "endDate": "YYYY-MM-DD eller null",
  "location": "<plats eller null>",
  "sourceHint": "<kort beskrivning av källa/varför du tror detta, eller null>",
  "notes": "<kort kommentar eller null>"
}
"@
}

function Invoke-LmStudioChat {
    param(
        [string]$Endpoint,
        [string]$Model,
        [string]$Prompt
    )

    $body = @{
        model = $Model
        temperature = 0.0
        max_tokens = 800
        messages = @(
            @{
                role = "system"
                content = "Du returnerar endast strikt JSON. Ingen markdown, inga kodblock, ingen extra text."
            },
            @{
                role = "user"
                content = $Prompt
            }
        )
    }

    $jsonBody = $body | ConvertTo-Json -Depth 20

    $response = Invoke-RestMethod `
        -Uri $Endpoint `
        -Method Post `
        -ContentType "application/json; charset=utf-8" `
        -Body $jsonBody `
        -TimeoutSec 120

    if (-not $response.choices -or $response.choices.Count -eq 0) {
        throw "LM Studio returned no choices"
    }

    return [string]$response.choices[0].message.content
}

function ConvertFrom-ModelJson {
    param([string]$Text)

    $clean = $Text.Trim()

    if ($clean.StartsWith('```')) {
        $clean = $clean -replace '^```json\s*', ''
        $clean = $clean -replace '^```\s*', ''
        $clean = $clean -replace '\s*```$', ''
        $clean = $clean.Trim()
    }

    # Plocka ut första JSON-objektet om modellen råkar babbla runt det.
    $start = $clean.IndexOf('{')
    $end = $clean.LastIndexOf('}')

    if ($start -lt 0 -or $end -lt $start) {
        throw "Model response did not contain a JSON object. Raw response: $Text"
    }

    $json = $clean.Substring($start, $end - $start + 1)

    return $json | ConvertFrom-Json -Depth 50
}

function Write-JsonLine {
    param(
        [string]$Path,
        [object]$Object
    )

    $line = $Object | ConvertTo-Json -Depth 50 -Compress
    Add-Content -Path $Path -Value $line -Encoding UTF8
}

$today = (Get-Date).Date
$until = $today.AddDays(7)

Write-Host "Reading watchlist: $WatchlistPath"
$watchlist = Read-JsonFile $WatchlistPath

if (-not $watchlist.entities) {
    throw "No entities found in $WatchlistPath"
}

$entities = @($watchlist.entities)

if ($OnlyTier1) {
    $entities = @($entities | Where-Object { $_.priority -eq "tier_1" })
}

if ($StartIndex -gt 0) {
    $entities = @($entities | Select-Object -Skip $StartIndex)
}

if ($MaxEntities -gt 0) {
    $entities = @($entities | Select-Object -First $MaxEntities)
}

$completed = [System.Collections.Generic.HashSet[string]]::new()
if ($Resume) {
    $completed = Get-CompletedEntityIds $OutputPath
    Write-Host "Resume enabled. Completed entities in output: $($completed.Count)"
}

Write-Host "Entities to process: $($entities.Count)"
Write-Host "Period: $($today.ToString("yyyy-MM-dd")) -> $($until.ToString("yyyy-MM-dd"))"
Write-Host "Output: $OutputPath"
Write-Host ""

$index = 0
$success = 0
$fail = 0
$found = 0

foreach ($entity in $entities) {
    $index++

    $entityId = [string]$entity.id
    $entityName = [string]$entity.name

    if ($Resume -and $completed.Contains($entityId)) {
        Write-Host "[$index/$($entities.Count)] SKIP $entityName"
        continue
    }

    Write-Host "[$index/$($entities.Count)] Querying: $entityName"

    try {
        $prompt = New-EntityPrompt -Entity $entity -Today $today -Until $until
        $raw = Invoke-LmStudioChat -Endpoint $Endpoint -Model $Model -Prompt $prompt
        $parsed = ConvertFrom-ModelJson $raw

        $result = [ordered]@{
            checkedAt = (Get-Date).ToString("o")
            periodStart = $today.ToString("yyyy-MM-dd")
            periodEnd = $until.ToString("yyyy-MM-dd")
            entityId = $entityId
            entityName = $entityName
            entityType = $entity.type
            sportId = $entity.sport.id
            priority = $entity.priority
            model = $Model
            ok = $true
            found = [bool]$parsed.found
            confidence = $parsed.confidence
            activityTitle = $parsed.activityTitle
            activityType = $parsed.activityType
            startDate = $parsed.startDate
            endDate = $parsed.endDate
            location = $parsed.location
            sourceHint = $parsed.sourceHint
            notes = $parsed.notes
            rawModelText = $raw
        }

        if ([bool]$parsed.found) {
            $found++
            Write-Host "  FOUND: $($parsed.activityTitle) $($parsed.startDate)" -ForegroundColor Green
        }
        else {
            Write-Host "  none" -ForegroundColor DarkGray
        }

        Write-JsonLine -Path $OutputPath -Object $result
        $success++
    }
    catch {
        $fail++

        Write-Warning "Failed for ${entityName}: $($_.Exception.Message)"

        $errorResult = [ordered]@{
            checkedAt = (Get-Date).ToString("o")
            periodStart = $today.ToString("yyyy-MM-dd")
            periodEnd = $until.ToString("yyyy-MM-dd")
            entityId = $entityId
            entityName = $entityName
            entityType = $entity.type
            sportId = $entity.sport.id
            priority = $entity.priority
            model = $Model
            ok = $false
            found = $false
            confidence = "low"
            activityTitle = $null
            activityType = $null
            startDate = $null
            endDate = $null
            location = $null
            sourceHint = $null
            notes = "Error: $($_.Exception.Message)"
            rawModelText = $null
        }

        Write-JsonLine -Path $OutputPath -Object $errorResult
    }

    if ($DelayMs -gt 0) {
        Start-Sleep -Milliseconds $DelayMs
    }
}

Write-Host ""
Write-Host "Done."
Write-Host "Success: $success"
Write-Host "Failed:  $fail"
Write-Host "Found:   $found"
Write-Host "Output:  $OutputPath"
