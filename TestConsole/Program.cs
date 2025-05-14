using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using OptifineInstaller;
using System;
using System.Linq;
using System.Net.Http;

var loader = new Installer(new HttpClient());
var versions = await loader.GetOptifineVersionsAsync();

Console.WriteLine($"{"Version",-30} {"Forge Ver",-10} {"Preview",-8} {"Uploaded",-12}");
Console.WriteLine(new string('-', 61));

foreach (var v in versions)
{
    Console.WriteLine($"{v.Version,-30} {v.ForgeVersion,-10} {(v.IsPreviewVersion ? "Yes" : "No"),-8} {v.UploadedDate:yyyy-MM-dd}");
}
Console.WriteLine(new string('-', 61));

Console.Write("Version: ");
var version = Console.ReadLine();

Console.WriteLine("selected version: " + version);

var selectedVersion = versions.FirstOrDefault(x => x.Version == version);
if (selectedVersion is null)
{
    Console.WriteLine("version not found");
    return;
}
var minecraftPath = new MinecraftPath();
var launcher = new MinecraftLauncher(minecraftPath);

await launcher.InstallAsync(selectedVersion.MinecraftVersion);
Console.WriteLine("done installing vanilla version");

var versionName = await loader.InstallOptifineAsync(minecraftPath.BasePath, selectedVersion);
Console.WriteLine("done installing optifine");

var process = await launcher.InstallAndBuildProcessAsync(versionName, new MLaunchOption
{
    Session = CmlLib.Core.Auth.MSession.CreateLegacyOfflineSession("lunar123"),
    MaximumRamMb = 16384
});
process.Start();
await process.WaitForExitAsync();


/*
 * CONFIRMED WORKING VERSIONS
 * - OptiFine_1.21.4_HD_U_J4_pre2
 * - OptiFine_1.19.4_HD_U_I4
 * - OptiFine_1.17.1_HD_U_H1
 * - OptiFine_1.17.1_HD_U_G9
 * - OptiFine_1.17_HD_U_G9_pre25
 * - OptiFine_1.16.5_HD_U_G8
 * - OptiFine_1.14.4_HD_U_G5
 * - OptiFine_1.14.3_HD_U_F2
 * - OptiFine_1.12.2_HD_U_G6_pre1
 * - OptiFine_1.8.9_HD_U_M6_pre2
 * - OptiFine_1.8.9_HD_U_M5
 * - OptiFine_1.8.9_HD_U_L5
 * - OptiFine_1.8.9_HD_U_I7
 * - OptiFine_1.8.9_HD_U_H7
 * - OptiFine_1.8.9_HD_U_H5
 * - OptiFine_1.8.8_HD_U_I7
 * - OptiFine_1.7.2_HD_U_E3
 * 
 * KNOWN NON-WORKING VERSIONS
 * - not yet
 * 
 * 
 */