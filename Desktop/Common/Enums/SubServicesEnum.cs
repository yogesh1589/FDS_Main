using System.ComponentModel;

namespace Desktop.Common.Enums
{
    public enum SubServicesEnum
    {
        [Description("Web Cache Cleaning")]
        WebCacheCleaning,
        [Description("Web History Cleaning")]
        WebHistoryCleaning,
        [Description("Web Cookie Cleaning")]
        WebCookieCleaning,
        [Description("DNS Flushing")]
        DNSFlushing,
        [Description("Windows Registry Cleaning")]
        WindowsRegistryCleaning,
        [Description("Recycle Bin Cleaning")]
        RecycleBinCleaning,
        [Description("Memory Cleaning")]
        MemoryCleaning
    }
}
