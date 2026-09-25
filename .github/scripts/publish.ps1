param (
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing RunAsAdminPolMan (GUI)..."
dotnet publish src/RunAsAdminPolMan/RunAsAdminPolMan.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:Version=$Version -o ./publish

Write-Host "Publishing RunAsAdminPolMan.CLI (CLI)..."
dotnet publish src/RunAsAdminPolMan.CLI/RunAsAdminPolMan.CLI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:Version=$Version -o ./publish

Write-Host "Publishing RunAsAdminPolMan.Bootstrapper (Proxy)..."
dotnet publish src/RunAsAdminPolMan.Bootstrapper/RunAsAdminPolMan.Bootstrapper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:Version=$Version -o ./publish

Write-Host "Cleaning up extraneous files..."
Remove-Item -Path "./publish/*.pdb" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "./publish/*.xml" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "./publish/*.json" -Force -ErrorAction SilentlyContinue

Write-Host "Compressing artifacts into RunAsAdminPolMan.zip..."
Compress-Archive -Path "./publish/*" -DestinationPath "./RunAsAdminPolMan.zip" -Force

Write-Host "Publish completed successfully!"
