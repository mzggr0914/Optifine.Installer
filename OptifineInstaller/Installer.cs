using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OptifineInstaller
{
    public class Installer
    {
        private static FileInfo JarFile;

        private static ZipResourceProvider JarFileZrp;

        private readonly HttpClient _httpClient;

        public Installer(HttpClient client)
        {
            _httpClient = client;
        }

        public async Task<string> GetDirectDownloadUrlAsync(OptifineVersion version)
        {
            HttpClient client = new HttpClient();
            var res = await client.GetAsync(version.DownloadUrl);
            var html = await res.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//a[@onclick='onDownload()']");
            var href = node.GetAttributeValue("href", null);

            return $"https://optifine.net/{href}";
        }

        public async Task<IEnumerable<OptifineVersion>> GetOptifineVersionsAsync()
        {
            var response = await _httpClient.GetAsync("https://optifine.net/downloads");
            string html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//td[@class='content']").SelectSingleNode(".//span[@class='downloads']");

            List<OptifineVersion> versions = new List<OptifineVersion>();

            var versionTrList = node.SelectNodes(".//tr[contains(@class, 'downloadLine')]");

            foreach (var versionTr in versionTrList)
            {
                var mcVer = Regex.Replace(node.SelectNodes(".//h2")?.
                    Where(h2 => h2.StreamPosition < versionTr.StreamPosition).Last().InnerText.Split()[1], @"\.0$", "");

                var forgeVersion = versionTr.SelectSingleNode(".//td[@class='colForge']").InnerText.Replace("Forge ", "").Replace("#", "");

                var version = new OptifineVersion
                (
                    mcVer,
                    versionTr.SelectSingleNode(".//td[@class='colFile']").InnerText.Split(new[] { ' ' }, 2)[1].Replace(" ", "_"),
                    forgeVersion != "Forge N/A" ? forgeVersion : null,
                    versionTr.GetAttributeValue("class", "").Contains("downloadLinePreview"),
                    DateTime.ParseExact(versionTr.SelectSingleNode(".//td[@class='colDate']").InnerText, "dd.MM.yyyy", CultureInfo.InvariantCulture)
                );
                versions.Add(version);
            }

            return versions;
        }

        public async Task<string> InstallOptifineAsync(string Minecraftpath, OptifineVersion version)
        {
            var url = await GetDirectDownloadUrlAsync(version);

            using (HttpResponseMessage response = await _httpClient.GetAsync(url))
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
            InstallOptiFineLibrary(mcVer, ofEd, dirMcLib, Utils.IsLateVersion(version));
            if (!Utils.IsLegacyVersion(version))
            {
                InstallLaunchwrapperLibrary(dirMcLib);
                UpdateJson(dirMcVers, mcVerOf, mcVer, ofEd);
            }
            else
            {
                UpdateJsonLegacy(dirMcVers, mcVerOf, mcVer, ofEd, Utils.GetLaunchwrapperVersionLegacy(version));
            }
            JarFile = null;
            JarFileZrp = null;
            diffStream.Dispose();
            jarFile.Delete();
        }

        private static void UpdateJson(DirectoryInfo versionsDir, string versionWithOf, string baseVersion, string ofEdition)
        {
            var jsonPath = Path.Combine(versionsDir.FullName, versionWithOf, versionWithOf + ".json");
            string rawJson = File.ReadAllText(jsonPath, Encoding.UTF8);
            JObject original = JObject.Parse(rawJson);

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

        public static void UpdateJsonLegacy(DirectoryInfo dirMcVers, string mcVerOf, string mcVer, string ofEd, string launchwrapper)
        {
            var dirMcVersOf = new DirectoryInfo(Path.Combine(dirMcVers.FullName, mcVerOf));

            var fileJson = new FileInfo(Path.Combine(dirMcVersOf.FullName, mcVerOf + ".json"));

            string json;
            using (var reader = new StreamReader(fileJson.FullName, Encoding.UTF8))
            {
                json = reader.ReadToEnd();
            }

            JObject root = JObject.Parse(json);

            root["id"] = mcVerOf;
            root["inheritsFrom"] = mcVer;

            JArray libs = new JArray();
            root["libraries"] = libs;

            string mainClass = root.Value<string>("mainClass") ?? "";
            if (!mainClass.StartsWith("net.minecraft.launchwrapper.", StringComparison.Ordinal))
            {
                root["mainClass"] = "net.minecraft.launchwrapper.Launch";

                string args = root.Value<string>("minecraftArguments") ?? "";
                args += " --tweakClass optifine.OptiFineTweaker";
                root["minecraftArguments"] = args;

                var libLw = new JObject
                {
                    ["name"] = $"net.minecraft:launchwrapper:{launchwrapper}"
                };
                libs.Add(libLw);
            }

            var libOf = new JObject
            {
                ["name"] = $"optifine:OptiFine:{mcVer}_{ofEd}"
            };
            libs.Insert(0, libOf);

            using (var writer = new StreamWriter(fileJson.FullName, false, Encoding.UTF8))
            using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
            {
                root.WriteTo(jsonWriter);
            }
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd'T'HH:mm:ssK");
        }

        private static bool InstallOptiFineLibrary(string mcVer, string ofEd, DirectoryInfo dirMcLib, bool isOver117)
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

            dirDest.Create();
            Patcher.Process(baseJar, JarFile, fileDest, isOver117);
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

            using (var stream = JarFileZrp.GetResourceStream(fileName))
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
            if (!dirVerMc.Exists)
            {
                ShowMessageVersionNotFound(mcVer);
                throw new DirectoryNotFoundException($"Please Install {mcVer} Vanilla Version Minecraft First");
            }

            var dirVerMcOf = new DirectoryInfo(Path.Combine(dirMcVer.FullName, mcVerOf));
            dirVerMcOf.Create();

            var fileJarMc = new FileInfo(Path.Combine(dirVerMc.FullName, $"{mcVer}.jar"));
            var fileJarMcOf = new FileInfo(Path.Combine(dirVerMcOf.FullName, $"{mcVerOf}.jar"));
            if (!fileJarMc.Exists)
            {
                ShowMessageVersionNotFound(mcVer);
                throw new FileNotFoundException($"{mcVer}.jar file not found");
            }
            File.Copy(fileJarMc.FullName, fileJarMcOf.FullName);

            var fileJsonMc = new FileInfo(Path.Combine(dirVerMc.FullName, $"{mcVer}.json"));
            var fileJsonMcOf = new FileInfo(Path.Combine(dirVerMcOf.FullName, $"{mcVerOf}.json"));
            if (!fileJsonMc.Exists)
            {
                ShowMessageVersionNotFound(mcVer);
                throw new FileNotFoundException($"{mcVer}.json file not found");
            }
            File.Copy(fileJsonMc.FullName, fileJsonMcOf.FullName);
        }

        private static void ShowMessageVersionNotFound(string mcVer)
        {
            Console.WriteLine($"Cannot find Minecraft {mcVer}.\nYou must download and start Minecraft {mcVer} once in the official launcher.");
        }

        private static string GetLaunchwrapperVersion()
        {
            using (var stream = JarFileZrp.GetResourceStream("launchwrapper-of.txt")
                          ?? throw new FileNotFoundException("Resource not found: launchwrapper-of.txt"))
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            {
                var str = reader.ReadToEnd().Trim();

                if (!Regex.IsMatch(str, @"^[0-9\.]+$"))
                    throw new FormatException($"Invalid launchwrapper version format: '{str}'");

                return str;
            }
        }
    }
}