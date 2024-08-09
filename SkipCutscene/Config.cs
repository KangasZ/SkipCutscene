using System.ComponentModel;
using Dalamud.Configuration;

namespace SkipCutscene
{
    public class Config : IPluginConfiguration
    {

        public int Version { get; set; }

        [DefaultValue(true)]
        public bool IsEnabled { get; set; }
    }
}
