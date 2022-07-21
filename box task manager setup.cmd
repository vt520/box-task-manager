@echo off

openfiles >NUL 2>&1 
if %ERRORLEVEL% EQU 0 goto Admin 
	echo Opening Administrative Session
	powershell Start-Process -FilePath '%0' -Verb RunAs
	exit /B
:Admin

echo Starting installation, this can take a while

if not exist "C:\Setup" mkdir "C:\Setup"
if not exist "C:\Setup\Box Task Manager" mkdir "C:\Setup\Box Task Manager"
pushd "C:\Setup\Box Task Manager"

echo Extracting installating script
call :heredoc test > installer.ps1 && goto test
	$app_name = 'box-task-manager';
	$app_host = 'apps.haxbits.org';
	$app_uri = "https://$app_host/downloads/$app_name"

	Write-Host  "${app_uri}.cer";
	Write-Host -NoNewline 'Checking for update server ' 
	Do { 
		$status = (Invoke-WebRequest -Method Head -Uri "${app_uri}").StatusCode;
		Write-Host -NoNewline "${status} ";
	} Until ( $status -eq 200); 
	Write-Host ' Found';

	Write-Output 'Fetching Trust Certificates';
	Invoke-WebRequest -Uri "${app_uri}/${app_name}.cer" -OutFile key.cer;

	Write-Output 'Adding Certificate to Windows'; 
	Import-Certificate -CertStoreLocation Cert:\LocalMachine\Root -FilePath key.cer; Write-Output 'Downloading AppInstaller'; 
	Invoke-WebRequest -Uri "${app_uri}/${app_name}.appinstaller" -OutFile setup.appinstaller;

	Write-Output 'Running AppInstaller'; Add-AppxPackage -AppInstallerFile setup.appinstaller; $package = (Get-AppxPackage 8f5cd344-e79b-4605-934a-61e4b335d041);
	Write-Output 'Starting Application';
	$app_id = (Select-Xml '//ns:Application' ($package.InstallLocation + '\appxmanifest.xml') -Namespace @{ ns='http://schemas.microsoft.com/appx/manifest/foundation/windows10' }).Node.Id;
	$package_fn = $package.PackageFamilyName;
	Write-Host "shell:AppsFolder\${package_fn}!exl!${app_id}";
	Start-Process "shell:AppsFolder\${package_fn}!exl!${app_id}"
:test

echo Running Setup Script
powershell -File installer.ps1
popd

pause

exit /B

:heredoc <uniqueIDX>
set exl=!
setlocal enabledelayedexpansion
set go=
for /f "delims=" %%A in ('findstr /n "^" "%~f0"') do (
    set "line=%%A" && set "line=!line:*:=!"
    if defined go (if #!line:~1!==#!go::=! (goto :EOF) else echo(!line!)
    if "!line:~0,13!"=="call :heredoc" (
        for /f "tokens=3 delims=>^ " %%i in ("!line!") do (
            if #%%i==#%1 (
                for /f "tokens=2 delims=&" %%I in ("!line!") do (
                    for /f "tokens=2" %%x in ("%%I") do set "go=%%x"
                )
            )
        )
    )
)
goto :EOF