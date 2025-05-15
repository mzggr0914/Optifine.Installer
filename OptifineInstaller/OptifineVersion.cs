using System;

namespace OptifineInstaller
{
    public class OptifineVersion
    {
        public string Version => $"OptiFine_{MinecraftVersion}_{OptifineEdition}";
        public string MinecraftVersion { get; }
        public string OptifineEdition { get; }
        public string ForgeVersion { get; }
        public string DownloadUrl => $"http://optifine.net/adloadx?f={Version}.jar";
        public bool IsPreviewVersion { get; }
        public DateTime UploadedDate { get; }

        public OptifineVersion(
            string minecraftVersion,
            string optifineEdition,
            string forgeVersion,
            bool isPreviewVersion,
            DateTime uploadedDate)
        {
            MinecraftVersion = minecraftVersion;
            OptifineEdition = optifineEdition;
            ForgeVersion = forgeVersion;
            IsPreviewVersion = isPreviewVersion;
            UploadedDate = uploadedDate;
        }
    }
}