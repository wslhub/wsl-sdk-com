$ErrorActionPreference = "Stop"
$obj = New-Object -ComObject 'WslSdk.WslService'
Write-Output 'A WslSdk.WslService object is created.'
Pause

# Get installed distro list
Write-Output 'Currently installed WSL distro list: '
$list = $obj.GetDistroNames()
Write-Output $list
Pause

# Generate Random Name
$RandomName = $obj.GenerateRandomName($false)
Write-Output "We will use $RandomName as a new distro"

# Download Alpine Linux RootFS Image
Write-Output 'Downloading alpine linux root file system image'
$TargetUrl = 'https://dl-cdn.alpinelinux.org/alpine/v3.14/releases/x86_64/alpine-minirootfs-3.14.0-x86_64.tar.gz'
$RootfsFilePath = "$env:TEMP\alpine.tar.gz"
$InstallPath = "C:\Distro\$RandomName"
Invoke-WebRequest -UseBasicParsing -Uri $TargetUrl -OutFile $RootfsFilePath
Pause

# Register Distro
Write-Output "Distro installation begins"
Write-Output " - Distro Name: $RandomName"
Write-Output " - Source RootFS File Path: $RootfsFilePath"
Write-Output " - Destination Install Path: $InstallPath"
$obj.RegisterDistro($RandomName, $RootfsFilePath, $InstallPath)
Pause

# Distro Register Check
$Result = $obj.IsDistroRegistered($RandomName)
Write-Output "Distro Name $RandomName Installed: $Result"
Pause

# Metadata Query
Write-Output "Querying $RandomName metadata..."
$o = $obj.QueryDistroInfo($RandomName)
Write-Output " - Distro ID: $($o.DistroId())"
Write-Output " - Distro Name: $($o.DistroName())"
Write-Output " - Environment Variabls: $($o.DefaultEnvironmentVariables())"
Write-Output " - Default Uid: $($o.DefaultUid())"
Write-Output " - Flags: $($o.DistroFlags())"
Write-Output " - Win32 Interop Enabled: $($o.EnableInterop())"
Write-Output " - Drive Mounting Enabled: $($o.EnableDriveMounting())"
Write-Output " - NT Path Append Enabled: $($o.AppendNtPath())"
Write-Output " - WSL Version: $($o.WslVersion())"
Pause

# Run WSL command
Write-Output "Installing vim..."
$res = $obj.RunWslCommand($o.DistroName(), "apk add vim")
Write-Output $res
Pause

# Revealing launcher executable
Write-Output "Revealing launcher executable file"
Start-Process -FilePath "$env:windir\explorer.exe" -ArgumentList "/select,$InstallPath\$RandomName.exe"
Pause

# Unregister Distro
Write-Output "Unregister $RandomName distro..."
$obj.UnregisterDistro($RandomName)
Pause

# Get installed distro list
Write-Output 'Currently installed WSL distro list: '
$list = $obj.GetDistroNames()
Write-Output $list
Pause

$obj = $null
