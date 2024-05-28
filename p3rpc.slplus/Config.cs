using p3rpc.commonmodutils;
using p3rpc.slplus.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;
using System.ComponentModel;

namespace p3rpc.slplus.Configuration
{
    public class Config : Configurable<Config>
    {
        [DisplayName("X")]
        [DefaultValue(0)]
        public int Xoffset { get; set; } = 0; // 284

        [DisplayName("Y")]
        [DefaultValue(0)]
        public int Yoffset { get; set; } = 0; // 106

        [DisplayName("DrawPoint")]
        [DefaultValue(0)]
        public uint DrawPoint { get; set; } = 0;

        [DisplayName("Description Debug")]
        [DefaultValue(false)]
        public bool ShowOriginalDescription { get; set; } = false;

        [DisplayName("Log Level")]
        [DefaultValue(LogLevel.Information)]
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
