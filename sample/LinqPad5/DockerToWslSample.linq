<Query Kind="Statements">
  <NuGetReference>Docker.DotNet</NuGetReference>
  <Namespace>Docker.DotNet</Namespace>
  <Namespace>Docker.DotNet.Models</Namespace>
  <Namespace>System.IO.Compression</Namespace>
</Query>

var localDockerUri = new Uri("npipe://./pipe/docker_engine");
var client = new DockerClientConfiguration(localDockerUri).CreateClient();
var callback = new Progress<JSONMessage>(jsonMessage => Console.Out.WriteLine(jsonMessage.Status));
var targetPath = Path.Combine(Path.GetTempPath(), $"wsl_{Guid.NewGuid().ToString("n")}.tar.gz");

var imagePullRequest = new ImagesCreateParameters()
{
	Repo = null,
	FromImage = "ubuntu",
	Tag = "latest",
};

var containerRunRequest = new CreateContainerParameters()
{
	Image = $"{imagePullRequest.FromImage}:{imagePullRequest.Tag}",
	Tty = false,
};

var containerRemoveRequest = new ContainerRemoveParameters()
{
	Force = true,
};

await client.Images.CreateImageAsync(imagePullRequest, null, callback);
var containerRunResult = await client.Containers.CreateContainerAsync(containerRunRequest);

using (var stream = await client.Containers.ExportContainerAsync(containerRunResult.ID))
using (var fileStream = new GZipStream(File.OpenWrite(targetPath), CompressionMode.Compress, false))
{
	await stream.CopyToAsync(fileStream);
}

await client.Containers.RemoveContainerAsync(containerRunResult.ID, containerRemoveRequest);

var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
dynamic wslService = Activator.CreateInstance(wslServiceType);

var randomName = wslService.GenerateRandomName(true);
var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

if (!Directory.Exists(tempDirectory))
	Directory.CreateDirectory(tempDirectory);

try
{
	wslService.RegisterDistro(randomName, targetPath, tempDirectory);
	var res = (string)wslService.RunWslCommand(randomName, "cat /etc/os-release");
	res.Dump();
}
finally
{
	wslService.UnregisterDistro(randomName);
	if (File.Exists(targetPath))
	{
		try { File.Delete(targetPath); }
		catch { }
	}
}
