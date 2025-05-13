using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using OptifineInstaller;
using System;
using System.Net.Http;

var loader = new OptifineVersionLoader(new HttpClient());
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

var selectedVersion = versions.Find(x => x.Version == version);
var minecraftPath = new MinecraftPath();
var launcher = new MinecraftLauncher(minecraftPath);

await launcher.InstallAsync(selectedVersion.MinecraftVersion);

var versionName = await Installer.InstallOptifineAsync(minecraftPath.BasePath, selectedVersion);
var process = await launcher.InstallAndBuildProcessAsync(versionName, new MLaunchOption
{
    Session = CmlLib.Core.Auth.MSession.CreateLegacyOfflineSession("lunar123"),
    MaximumRamMb = 16384
});
process.Start();
await process.WaitForExitAsync();


/*
 * CONFIRMED WORKING VERSIONS
 * - OptiFine_1.17.1_HD_U_G9
 * - OptiFine_1.17_HD_U_G9_pre25
 * - OptiFine_1.16.5_HD_U_G8
 * - OptiFine_1.14.4_HD_U_G5
 * - OptiFine_1.14.3_HD_U_F2
 * - OptiFine_1.12.2_HD_U_G6_pre1
 * - OptiFine_1.8.9_HD_U_M6_pre2
 * 
 *  NOT WORKING VERSIONS
 * - OptiFine_1.17.1_HD_U_H1       // First version that fails to launch
 * - OptiFine_1.17.1_HD_U_H2_pre1
 */
