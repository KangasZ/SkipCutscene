using System;
using System.Diagnostics;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SkipCutscene;

public class SkipCutscene : IDalamudPlugin
{
    private readonly Config _config;

    private readonly decimal _base = uint.MaxValue;

    public SkipCutscene(IPluginLog PluginLog)
    {
        if (Interface.GetPluginConfig() is not Config configuration || configuration.Version == 0)
            configuration = new Config { IsEnabled = true, Version = 1 };

        _config = configuration;

        Address.Offset1 = SigScanner.ScanText("75 33 48 8B 0D ?? ?? ?? ?? BA ?? 00 00 00 48 83 C1 10 E8 ?? ?? ?? ?? 83 78");
        Address.Offset2 = SigScanner.ScanText("74 18 8B D7 48 8D 0D");
        PluginLog.Information(
            "Offset1: [\"ffxiv_dx11.exe\"+{0}]",
            (Address.Offset1.ToInt64() - Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64()).ToString("X")
        );
        PluginLog.Information(
            "Offset2: [\"ffxiv_dx11.exe\"+{0}]",
            (Address.Offset2.ToInt64() - Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64()).ToString("X")
        );

        if (Address.Offset1 != IntPtr.Zero && Address.Offset2 != IntPtr.Zero)
        {
            PluginLog.Information("Cutscene Offset Found.");
            if (_config.IsEnabled)
                SetEnabled(true);
        }
        else
        {
            PluginLog.Error("Cutscene Offset Not Found.");
            PluginLog.Warning("Plugin Disabling...");
            Dispose();
            return;
        }

        CommandManager.AddHandler("/sc", new CommandInfo(OnCommand)
        {
            HelpMessage = "/sc: skip cutscene enable/disable."
        });
    }

    public void Dispose()
    {
        SetEnabled(false);
        GC.SuppressFinalize(this);
    }

    public string Name => "SkipCutscene";

    [PluginService] public IDalamudPluginInterface Interface { get; private set; }

    [PluginService] public ISigScanner SigScanner { get; private set; }

    [PluginService] public ICommandManager CommandManager { get; private set; }

    [PluginService] public IChatGui ChatGui { get; private set; }

    public (nint Offset1, nint Offset2) Address = new(nint.Zero, nint.Zero);

    public void SetEnabled(bool isEnable)
    {
        if (Address.Offset1 == IntPtr.Zero || Address.Offset2 == IntPtr.Zero) return;
        if (isEnable)
        {
            SafeMemory.Write<short>(Address.Offset1, -28528);
            SafeMemory.Write<short>(Address.Offset2, -28528);
        }
        else
        {
            SafeMemory.Write<short>(Address.Offset1, 13173);
            SafeMemory.Write<short>(Address.Offset2, 6260);
        }
    }

    private void OnCommand(string command, string arguments)
    {
        if (command.ToLower() != "/sc") return;
        ChatGui.Print(_config.IsEnabled ? "Skip Cutscene: Disabled" : "Skip Cutscene: Enabled");
        _config.IsEnabled = !_config.IsEnabled;
        SetEnabled(_config.IsEnabled);
        Interface.SavePluginConfig(_config);
    }
}
