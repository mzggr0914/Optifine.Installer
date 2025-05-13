using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OptifineInstaller
{
    public class OptifineVersionLoader
    {
        private readonly HttpClient _httpClient;

        public OptifineVersionLoader(HttpClient client)
        {
            _httpClient = client;
        }

        public async Task<List<OptifineVersion>> GetOptifineVersionsAsync()
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
                {
                    MinecraftVersion = mcVer,
                    OptifineEdition = versionTr.SelectSingleNode(".//td[@class='colFile']").InnerText.Split(new[] { ' ' }, 2)[1].Replace(" ", "_"),
                    DownloadUrl = versionTr.SelectSingleNode(".//td[@class='colMirror']").SelectSingleNode("a").GetAttributeValue("href", null),
                    ForgeVersion = forgeVersion != "Forge N/A" ? forgeVersion : null,
                    IsPreviewVersion = versionTr.GetAttributeValue("class", "").Contains("downloadLinePreview"),
                    UploadedDate = DateTime.ParseExact(versionTr.SelectSingleNode(".//td[@class='colDate']").InnerText, "dd.MM.yyyy", CultureInfo.InvariantCulture)
                };
                versions.Add(version);
            }

            return versions;
        }
    }
}
