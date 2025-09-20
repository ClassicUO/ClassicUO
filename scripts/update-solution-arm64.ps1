# PowerShell script to update ClassicUO.sln with ARM64 support
# This script adds ARM64 and x64 platform configurations to all projects

param(
    [string]$SolutionPath = "..\ClassicUO.sln"
)

Write-Host "Updating ClassicUO.sln with ARM64 support..." -ForegroundColor Green

# Read the solution file
$content = Get-Content $SolutionPath -Raw

# Define the project configurations that need ARM64 support
$projectConfigs = @(
    @{Id="6F193271-70B3-48EC-9CE5-B8EE1A5CA89E"; Name="ManifestCreator"},
    @{Id="D7DD340F-1EE2-4F8A-AA3E-A8FC76098AD6"; Name="monokickstart"},
    @{Id="85972CEA-4AB1-45DC-922C-00C4E17764B5"; Name="ClassicUO.UnitTests"},
    @{Id="3F9AB6CE-3AD1-40B6-B2AA-C4D82F76258F"; Name="FNA.Core"},
    @{Id="D7EE32B0-ED1C-4263-B017-6C3EC74FD731"; Name="ClassicUO.Client"},
    @{Id="DDF690A2-7588-44BC-8E2E-9080C746A49C"; Name="ClassicUO.Assets"},
    @{Id="6B932930-D24C-43BB-8877-85B4F3F7A57B"; Name="ClassicUO.Utility"},
    @{Id="535BE739-1314-4F8D-B24B-06890DDD3B8D"; Name="ClassicUO.Renderer"},
    @{Id="69C1A629-F519-4F5A-85D5-4E0317CD267F"; Name="ClassicUO.IO"},
    @{Id="5AF00B6D-70C2-4CB0-A5D8-41F488F0DDF2"; Name="MP3Sharp"},
    @{Id="5ACA68DA-A282-434D-919F-B243A801D4C3"; Name="FontStashSharp.FNA.Core"}
)

# Generate ARM64 and x64 configurations for each project
$newConfigs = @()
foreach ($project in $projectConfigs) {
    $projectId = $project.Id
    $projectName = $project.Name
    
    # Add ARM64 configurations
    $newConfigs += "`t`t{$projectId}.Debug|ARM64.ActiveCfg = Debug|Any CPU"
    $newConfigs += "`t`t{$projectId}.Debug|ARM64.Build.0 = Debug|Any CPU"
    $newConfigs += "`t`t{$projectId}.Release|ARM64.ActiveCfg = Release|Any CPU"
    $newConfigs += "`t`t{$projectId}.Release|ARM64.Build.0 = Release|Any CPU"
    
    # Add x64 configurations
    $newConfigs += "`t`t{$projectId}.Debug|x64.ActiveCfg = Debug|Any CPU"
    $newConfigs += "`t`t{$projectId}.Debug|x64.Build.0 = Debug|Any CPU"
    $newConfigs += "`t`t{$projectId}.Release|x64.ActiveCfg = Release|Any CPU"
    $newConfigs += "`t`t{$projectId}.Release|x64.Build.0 = Release|Any CPU"
}

# Find the end of the ProjectConfigurationPlatforms section
$endPattern = "`tEndGlobalSection"
$endIndex = $content.IndexOf($endPattern, $content.IndexOf("ProjectConfigurationPlatforms"))

if ($endIndex -eq -1) {
    Write-Error "Could not find ProjectConfigurationPlatforms section end"
    exit 1
}

# Insert the new configurations before the EndGlobalSection
$beforeEnd = $content.Substring(0, $endIndex)
$afterEnd = $content.Substring($endIndex)

$newContent = $beforeEnd + ($newConfigs -join "`n") + "`n" + $afterEnd

# Write the updated content back to the file
Set-Content -Path $SolutionPath -Value $newContent -Encoding UTF8

Write-Host "Successfully updated ClassicUO.sln with ARM64 support!" -ForegroundColor Green
Write-Host "Added ARM64 and x64 configurations for $($projectConfigs.Count) projects" -ForegroundColor Yellow
