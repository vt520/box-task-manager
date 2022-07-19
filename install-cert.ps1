If (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
{
  # Relaunch as an elevated process:
  Start-Process powershell.exe "-File",('"{0}"' -f $MyInvocation.MyCommand.Path) -Verb RunAs
  exit
}

Import-PfxCertificate -Password (ConvertTo-SecureString -String "Anvil@00Revolution" -AsPlainText -Force) -CertStoreLocation Cert:\LocalMachine\Root -FilePath '.\cces.pfx'