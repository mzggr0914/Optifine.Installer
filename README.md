# OptifineInstaller
Minecraft OptiFine Installer.

## Features

* Fetch available OptiFine versions from the official source.
* Display version details including Forge compatibility, preview status, and upload date.
* Install Nearly All OptiFine Versions

## Sample Code With [CmlLib.Core](https://github.com/CmlLib/CmlLib.Core)

```csharp
using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using OptifineInstaller;
using System;
using System.Linq;
using System.Net.Http;


var loader = new Installer(new HttpClient());
var versions = await loader.GetOptifineVersionsAsync();

foreach (var v in versions)
{
    Console.WriteLine(v.Version);
}

Console.Write("Version: ");
var version = Console.ReadLine();

Console.WriteLine("Selected version: " + version);

var selectedVersion = versions.FirstOrDefault(x => x.Version == version);
if (selectedVersion is null)
{
    Console.WriteLine("Version not found");
    return;
}

var minecraftPath = new MinecraftPath();
var launcher = new MinecraftLauncher(minecraftPath);

await launcher.InstallAsync(selectedVersion.MinecraftVersion);
Console.WriteLine("Done installing vanilla version");

var versionName = await loader.InstallOptifineAsync(minecraftPath.BasePath, selectedVersion);
Console.WriteLine("Done installing OptiFine");

var process = await launcher.InstallAndBuildProcessAsync(
    versionName,
    new MLaunchOption
    {
        Session = CmlLib.Core.Auth.MSession.CreateLegacyOfflineSession("lunar123"),
        MaximumRamMb = 16384
    }
);

process.Start();
await process.WaitForExitAsync();

```

---

## Supported OptiFine Versions

### Confirmed Working Versions

* OptiFine\_1.21.4\_HD\_U\_J4\_pre2
* OptiFine\_1.19.4\_HD\_U\_I4
* OptiFine\_1.17.1\_HD\_U\_H1
* OptiFine\_1.17.1\_HD\_U\_G9
* OptiFine\_1.17\_HD\_U\_G9\_pre25
* OptiFine\_1.16.5\_HD\_U\_G8
* OptiFine\_1.14.4\_HD\_U\_G5
* OptiFine\_1.14.3\_HD\_U\_F2
* OptiFine\_1.12.2\_HD\_U\_G6\_pre1
* OptiFine\_1.8.9\_HD\_U\_M6\_pre2
* OptiFine\_1.8.9\_HD\_U\_M5
* OptiFine\_1.8.9\_HD\_U\_L5
* OptiFine\_1.8.9\_HD\_U\_I7
* OptiFine\_1.8.9\_HD\_U\_H7
* OptiFine\_1.8.9\_HD\_U\_H5
* OptiFine\_1.8.8\_HD\_U\_I7
* OptiFine\_1.7.2\_HD\_U\_E3

### Known Non-Working Versions

* (None identified yet.)
