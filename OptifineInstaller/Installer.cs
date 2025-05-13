using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OptifineInstaller
{
    public static class Installer
    {
        private static FileInfo JarFile;
        private static ZipResourceProvider JarFileZrp;

        public async static Task<string> InstallOptifineAsync(string Minecraftpath, OptifineVersion version)
        {
            var url = await version.GetDirectDownloadUrlAsync();
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    string path = Path.Combine(Path.GetTempPath(), version.Version + ".jar");
                    using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }

                    DoInstall(new DirectoryInfo(Minecraftpath), new FileInfo(path), version);
                    return $"{version.MinecraftVersion}-OptiFine_{version.OptifineEdition}";
                }
            }
        }

        private static void DoInstall(DirectoryInfo dirMc, FileInfo jarFile, OptifineVersion version)
        {
            JarFile = jarFile;
            var diffStream = JarFile.OpenRead();
            var diffZip = new ZipArchive(diffStream, ZipArchiveMode.Read);
            JarFileZrp = new ZipResourceProvider(diffZip);

            string ofVer = version.Version;
            Debug.WriteLine($"OptiFine Version: {ofVer}");

            string mcVer = version.MinecraftVersion;
            Debug.WriteLine($"Minecraft Version: {mcVer}");

            string ofEd = version.OptifineEdition;
            Debug.WriteLine($"OptiFine Edition: {ofEd}");

            Debug.WriteLine($"Dir minecraft: {dirMc}");

            var dirMcLib = new DirectoryInfo(Path.Combine(dirMc.FullName, "libraries"));
            Debug.WriteLine($"Dir libraries: {dirMcLib}");

            var dirMcVers = new DirectoryInfo(Path.Combine(dirMc.FullName, "versions"));
            Debug.WriteLine($"Dir versions: {dirMcVers}");

            string mcVerOf = $"{mcVer}-OptiFine_{ofEd}";
            Debug.WriteLine($"Minecraft_OptiFine Version: {mcVerOf}");

            CopyMinecraftVersion(mcVer, mcVerOf, dirMcVers);
            InstallOptiFineLibrary(mcVer, ofEd, dirMcLib);
            InstallLaunchwrapperLibrary(dirMcLib);
            UpdateJson(dirMcVers, mcVerOf, mcVer, ofEd);
            JarFile = null;
            JarFileZrp = null;
            diffStream.Dispose();
            jarFile.Delete();
        }

        private static void UpdateJson(DirectoryInfo versionsDir, string versionWithOf, string baseVersion, string ofEdition)
        {
            var jsonPath = Path.Combine(versionsDir.FullName, versionWithOf, versionWithOf + ".json");
            string rawJson = File.ReadAllText(jsonPath, Encoding.UTF8);
            JObject original;
            try { original = JObject.Parse(rawJson); }
            catch (JsonException e) { throw new IOException($"JSON parsing failed: {jsonPath}", e); }

            var updated = new JObject
            {
                ["id"] = versionWithOf,
                ["inheritsFrom"] = baseVersion,
                ["time"] = FormatDate(DateTime.Now),
                ["releaseTime"] = FormatDate(DateTime.Now),
                ["type"] = "release",
                ["libraries"] = new JArray()
            };
            var libraries = (JArray)updated["libraries"];
            string mainClass = (string)original["mainClass"];
            if (string.IsNullOrEmpty(mainClass) || !mainClass.StartsWith("net.minecraft.launchwrapper."))
            {
                updated["mainClass"] = "net.minecraft.launchwrapper.Launch";
                if (original.TryGetValue("minecraftArguments", out var mcArgsToken))
                {
                    string mcArgs = mcArgsToken.ToString() + " --tweakClass optifine.OptiFineTweaker";
                    updated["minecraftArguments"] = mcArgs;
                }
                else
                {
                    updated["minimumLauncherVersion"] = "21";
                    var arguments = new JObject
                    {
                        ["game"] = new JArray("--tweakClass", "optifine.OptiFineTweaker")
                    };
                    updated["arguments"] = arguments;
                }
                libraries.Add(new JObject
                {
                    ["name"] = "optifine:launchwrapper-of:" + GetLaunchwrapperVersion()
                });
            }

            libraries.Insert(0, new JObject
            {
                ["name"] = $"optifine:OptiFine:{baseVersion}_{ofEdition}"
            });

            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath));
            var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            File.WriteAllText(jsonPath, updated.ToString().Replace("\r\n", "\n"), utf8WithoutBom);
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd'T'HH:mm:ssK");
        }

        private static bool InstallOptiFineLibrary(string mcVer, string ofEd, DirectoryInfo dirMcLib)
        {
            var dirDest = new DirectoryInfo(Path.Combine(dirMcLib.FullName, $"optifine/OptiFine/{mcVer}_{ofEd}"));
            var fileDest = new FileInfo(Path.Combine(dirDest.FullName, $"OptiFine-{mcVer}_{ofEd}.jar"));

            if (fileDest.FullName == JarFile.FullName)
            {
                Debug.WriteLine("Source and target file are the same. : Save");
                return false;
            }

            Debug.WriteLine($"Source: {JarFile}");
            Debug.WriteLine($"Dest: {fileDest}");

            var versionDir = new DirectoryInfo(Path.Combine(dirMcLib.Parent.FullName, $"versions/{mcVer}"));
            var baseJar = new FileInfo(Path.Combine(versionDir.FullName, $"{mcVer}.jar"));
            if (!baseJar.Exists) { ShowMessageVersionNotFound(mcVer); throw new Exception("QUIET"); }

            dirDest.Create();
            Patcher.Process(baseJar, JarFile, fileDest);
            return true;
        }

        private static void InstallLaunchwrapperLibrary(DirectoryInfo dirMcLib)
        {
            string ver = GetLaunchwrapperVersion();
            string fileName = $"launchwrapper-of-{ver}.jar";
            var dirDest = new DirectoryInfo(Path.Combine(dirMcLib.FullName, $"optifine/launchwrapper-of/{ver}"));
            var fileDest = new FileInfo(Path.Combine(dirDest.FullName, fileName));

            Debug.WriteLine($"Source: {fileName}");
            Debug.WriteLine($"Dest: {fileDest}");

            using (var stream = JarFileZrp.GetResourceStream(fileName) ?? throw new IOException($"File not found: {fileName}"))
            {
                dirDest.Create();
                using (var outStream = File.Create(fileDest.FullName))
                {
                    stream.CopyTo(outStream);
                }
            }
        }

        private static void CopyMinecraftVersion(string mcVer, string mcVerOf, DirectoryInfo dirMcVer)
        {
            var dirVerMc = new DirectoryInfo(Path.Combine(dirMcVer.FullName, mcVer));
            if (!dirVerMc.Exists) { ShowMessageVersionNotFound(mcVer); throw new Exception("Please Install Vanilla Version Minecraft First"); }

            var dirVerMcOf = new DirectoryInfo(Path.Combine(dirMcVer.FullName, mcVerOf));
            dirVerMcOf.Create();

            var fileJarMc = new FileInfo(Path.Combine(dirVerMc.FullName, $"{mcVer}.jar"));
            var fileJarMcOf = new FileInfo(Path.Combine(dirVerMcOf.FullName, $"{mcVerOf}.jar"));
            if (!fileJarMc.Exists) { ShowMessageVersionNotFound(mcVer); throw new Exception("QUIET"); }
            File.Copy(fileJarMc.FullName, fileJarMcOf.FullName);

            var fileJsonMc = new FileInfo(Path.Combine(dirVerMc.FullName, $"{mcVer}.json"));
            var fileJsonMcOf = new FileInfo(Path.Combine(dirVerMcOf.FullName, $"{mcVerOf}.json"));
            File.Copy(fileJsonMc.FullName, fileJsonMcOf.FullName);
        }

        private static void ShowMessageVersionNotFound(string mcVer)
        {
            Console.WriteLine($"Cannot find Minecraft {mcVer}.\nYou must download and start Minecraft {mcVer} once in the official launcher.");
        }

        private static string GetLaunchwrapperVersion()
        {
            using (var stream = JarFileZrp.GetResourceStream("launchwrapper-of.txt") ?? throw new IOException("File not found: launchwrapper-of.txt"))
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            {
                var str = reader.ReadToEnd().Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(str, "^[0-9\\.]+$"))
                    throw new IOException($"Invalid launchwrapper version: {str}");
                return str;
            }
        }
    }
}