<Query Kind="Statements" />

var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
dynamic wslService = Activator.CreateInstance(wslServiceType);

var randomName = wslService.GenerateRandomName(true);
var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

if (!Directory.Exists(tempDirectory))
	Directory.CreateDirectory(tempDirectory);

var busyboxRootfsFile = Path.Combine(tempDirectory, Path.Combine(tempDirectory, $"{randomName}.tar.gz"));

var alpineLinuxUrl = "https://dl-cdn.alpinelinux.org/alpine/v3.14/releases/x86_64/alpine-minirootfs-3.14.0-x86_64.tar.gz";
using (var webClient = new WebClient())
{
	await webClient.DownloadFileTaskAsync(alpineLinuxUrl, busyboxRootfsFile);
}

wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
try
{
	var res = (string)wslService.RunWslCommand(randomName, "ls /");
	res.Dump();
}
finally
{
	wslService.UnregisterDistro(randomName);
}
