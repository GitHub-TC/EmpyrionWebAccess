$ErrorActionPreference = "Stop"

$RepoRoot   = $PSScriptRoot
$OutLinux   = "$RepoRoot\publish\EWALoader\EWA"

Write-Host "=== Publish EmpyrionModWebHost net8.0 linux-x64 (self-contained) -> $OutLinux ==="

dotnet publish "$RepoRoot\EmpyrionModWebHost\EmpyrionModWebHost.csproj" `
    -c Release `
    -r linux-x64 `
    --self-contained true `
    -p:InvariantGlobalization=false `
    -p:SkipAngularBuild=true `
    -p:SkipLinuxPublish=true `
    -o "$OutLinux" `
    --no-restore

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Make the ELF executable bit is NOT preserved on Windows; user must chmod+x on the Linux server
# The EWAModClient code does this automatically via bash when Wine environment is detected.

Write-Host "Linux-Binary: EmpyrionModWebHost (ELF) published to $OutLinux"
Write-Host "Fertig."
