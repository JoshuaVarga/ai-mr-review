[CmdletBinding()]
param(
  [Parameter(Position=0, Mandatory=$true)]
  [ValidateSet("Check", "UnitTest", "CompileAndPublish", "Run")]
  [string] $Target,

  [Parameter(Position=1)]
  [string] $Version,

  [Parameter(Position=2)]
  [string] $FileVersion
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Clean
{
  Write-Host "Cleaning bin/obj folders and build-artifacts..."
  Get-ChildItem -Path "src", "tests" -Recurse -Include bin,obj |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

  Remove-Item -Recurse -Force "build-artifacts" -ErrorAction SilentlyContinue | Out-Null
  New-Item -ItemType Directory -Path "build-artifacts" | Out-Null
  Write-Host "Cleaned bin/obj folders and build-artifacts!"
}

function Restore
{
  Write-Host "Restoring .NET solution..."
  dotnet restore TodoApp.slnx
  Write-Host "Restored .NET solution!"
}

function Check
{
  Write-Host "Checking NuGet packages for vulnerabilities..."
  dotnet list TodoApp.slnx package --vulnerable
  Write-Host "Checked NuGet for vulnerabilities"
}

function BuildSolution
{
  Write-Host "Building solution in Release mode..."
  dotnet build TodoApp.slnx --configuration Release
  Write-Host "Solution built!"
}

function TestSolution
{
  Write-Host "Running tests and collecting results..."
  $testResultsDir = "build-artifacts/test-results"
  New-Item -ItemType Directory -Path $testResultsDir -Force | Out-Null

  $failedTestAssemblies = @()
  $testProjects = Get-ChildItem -Recurse -Include *.Tests.csproj

  foreach ($testProj in $testProjects)
  {
    $projName = $testProj.BaseName
    Write-Host "Testing project $projName ..."

    dotnet test $testProj.FullName `
      --logger "trx" `
      --no-build `
      --results-directory "$testResultsDir" `
      --filter "TestCategory!=Interactive"

    if ($LASTEXITCODE -eq -1)
    {
      Write-Error "Invalid Args when testing $projName"
      exit 1
    } elseif ($LASTEXITCODE -gt 0)
    {
      $failedTestAssemblies += $projName
    }
  }

  if ($failedTestAssemblies.Count -gt 0)
  {
    Write-Error "Failing tests in $($failedTestAssemblies -join ', ')"
    exit 1
  }

  Write-Host "All tests passed successfully!"
}

function BuildTestProjects
{
  $testProjects = Get-ChildItem -Recurse -Include *.Tests.csproj
  foreach ($testProj in $testProjects)
  {
    Write-Host "Building test project $($testProj.FullName) ..."
    dotnet build $testProj.FullName
    if ($LASTEXITCODE -ne 0)
    {
      throw "Build failed for $($testProj.Name)"
    }
  }
}

function GenerateSemVerFile
{
  Write-Host "Generating Directory.Build.props with Version=$Version, FileVersion=$FileVersion"

  $propsContent = @"
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Product>TodoApp</Product>
    <Description>Avalonia + ReactiveUI Todo App</Description>
    <Version>$Version</Version>
    <FileVersion>$FileVersion</FileVersion>
  </PropertyGroup>
</Project>
"@

  $propsPath = "Directory.Build.props"
  Set-Content -Path $propsPath -Value $propsContent
  Write-Host "Wrote version info to $propsPath!"
}

function RunApp
{
  Write-Host "Running app..."
  dotnet run --project src\TodoApp.App\TodoApp.App.csproj
}

function PublishApp
{
  Write-Host "Publishing app..."
  dotnet publish src\TodoApp.App\TodoApp.App.csproj `
    -c Release `
    -o "build-artifacts/output"
  Write-Host "Published to build-artifacts/output!"
}

function PrepareScript
{
  $powerShellVersion = $PSVersionTable.PSVersion
  $gitVersion = & git --version
  $dotnetVersion = & dotnet --version
  $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

  Write-Host ""

  $banner = @'
  __  __ _                             _
 |  \/  (_)                           (_)
 | \  / |_  ___ _ __ _____      ____ _ ___   _____ 
 | |\/| | |/ __| '__/ _ \ \ /\ / / _` | \ \ / / _ \
 | |  | | | (__| | | (_) \ V  V / (_| | |\ V /  __/
 |_|  |_|_|\___|_|  \___/ \_/\_/ \__,_|_| \_/ \___|
'@

  $banner -split "`n" | ForEach-Object {
    Write-Host $_ -ForegroundColor Cyan
  }

  Write-Host ""
  Write-Host "                 ~~ Build and test without Nuke ~~ " -ForegroundColor Cyan
  Write-Host ""
  Write-Host "  Timestamp      : $timestamp" -ForegroundColor Cyan
  Write-Host "  PowerShell     : $powerShellVersion" -ForegroundColor Cyan
  Write-Host "  Git            : $gitVersion" -ForegroundColor Cyan
  Write-Host "  .NET SDK       : $dotnetVersion" -ForegroundColor Cyan
  Write-Host "  Version        : $Version" -ForegroundColor Cyan
  Write-Host "  File Version   : $FileVersion" -ForegroundColor Cyan
  Write-Host ""
}

if (-not $Version)
{ $Version = "0.0.0.0" 
}
if ($Version -match '^\.')
{ $Version = "0$Version" 
}
if (-not $FileVersion)
{ $FileVersion = $Version 
}

switch ($Target)
{
  "CompileAndPublish"
  {
    PrepareScript
    Clean
    GenerateSemVerFile
    BuildSolution
    PublishApp
    exit 0
  }
  "UnitTest"
  {
    PrepareScript
    Clean
    Restore
    GenerateSemVerFile
    BuildTestProjects
    TestSolution
    exit 0
  }
  "Run"
  {
    PrepareScript
    RunApp
    exit 0
  }
  "Check"
  {
    PrepareScript
    Restore
    Check
  }
  default
  {
    Write-Error "Unknown target '$Target'"
    exit 1
  }
}
