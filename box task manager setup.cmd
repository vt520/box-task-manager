@echo off
openfiles >NUL 2>&1 
if NOT %ERRORLEVEL% EQU 0 goto NotAdmin 
goto Admin
:NotAdmin 
echo Opening Administrative Session
powershell Start-Process -FilePath '%0' -Verb RunAs
exit /B
:Admin

@echo off
echo Starting installation, this can take a while

if not exist "C:\Setup" mkdir "C:\Setup"
if not exist "C:\Setup\Box Task Manager" mkdir "C:\Setup\Box Task Manager"

pushd "C:\Setup\Box Task Manager"
powershell "Do { Write-Host 'Checking for update server' } Until (Test-Connection apps.haxbits.org -Quiet); Write-Output 'Fetching Trust Certificates'; Invoke-WebRequest -Uri https://apps.haxbits.org/downloads/box-task-manager/box-task-manager.cer -OutFile key.cer; Write-Output 'Adding Certificate to Windows'; Import-Certificate -CertStoreLocation Cert:\LocalMachine\Root -FilePath key.cer; Write-Output 'Downloading AppInstaller'; Invoke-WebRequest -Uri https://apps.haxbits.org/downloads/box-task-manager/box-task-manager.appinstaller -OutFile setup.appinstaller; Write-Output 'Running AppInstaller'; Add-AppxPackage -AppInstallerFile setup.appinstaller; $package = (Get-AppxPackage 8f5cd344-e79b-4605-934a-61e4b335d041);Write-Output 'Starting Application'; $app_id = (Select-Xml '//ns:Application' ($package.InstallLocation + '\appxmanifest.xml') -Namespace @{ ns='http://schemas.microsoft.com/appx/manifest/foundation/windows10' }).Node.Id; $package_fn = $package.PackageFamilyName; Start-Process ""shell:AppsFolder\$package_fn!$app_id"""
popd
