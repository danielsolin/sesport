$ErrorActionPreference = "Stop"

$database = $env:SESPORT_POSTGRES_DB
$user = $env:SESPORT_POSTGRES_USER

if ([string]::IsNullOrWhiteSpace($database))
{
   $database = "sesport"
}

if ([string]::IsNullOrWhiteSpace($user))
{
   $user = "sesport"
}

$migrations = Get-ChildItem `
   -Path "database/postgres/migrations" `
   -Filter "*.sql" |
   Sort-Object Name

foreach ($migration in $migrations)
{
   $containerPath = "/migrations/$($migration.Name)"

   Write-Host "Running $($migration.Name)"

   docker compose exec -T postgres `
      psql `
      -U $user `
      -d $database `
      -v ON_ERROR_STOP=1 `
      -f $containerPath
}
