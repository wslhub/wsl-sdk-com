$obj = New-Object -ComObject 'WslSdk.WslService'

Write-Output 'A WslSdk.WslService object is created'

# Distro Register Check
$distroName = 'Ubuntu-20.04'
Write-Output $obj.IsDistroRegistered($distroName)

# Metadata Query
$o = $obj.GetDefaultDistro()
Write-Output "Distro ID: $($o.DistroId())"
Write-Output "Distro Name: $($o.DistroName())"

$list = $obj.GetDistroList()
Write-Output $list

# Run WSL command
$res = $obj.RunWslCommand($o.DistroName(), "cat /etc/os-release")
Write-Output $res

$obj = $null