﻿using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace PleaseWork.Settings
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        public virtual bool splitPPVals { get; set; } = false;
    }
}
