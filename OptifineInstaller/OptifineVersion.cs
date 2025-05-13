using HtmlAgilityPack;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OptifineInstaller
{
    public class OptifineVersion
    {
        public string Version => $"OptiFine_{MinecraftVersion}_{OptifineEdition}";
        public string MinecraftVersion;
        public string OptifineEdition;
        public string ForgeVersion;
        public string DownloadUrl;
        public bool IsPreviewVersion;
        public DateTime UploadedDate;

        public async Task<string> GetDirectDownloadUrlAsync()
        {
            HttpClient client = new HttpClient();
            var res = await client.GetAsync(DownloadUrl);
            var html = await res.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//a[@onclick='onDownload()']");
            var href = node.GetAttributeValue("href", null);

            return $"https://optifine.net/{href}";
        }
    }
}
