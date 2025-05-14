using System;

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
    }
}