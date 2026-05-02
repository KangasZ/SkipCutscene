using System;
using System.Diagnostics;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SkipCutscene;

public class SkipCutscene : IDalamudPlugin
{
    private readonly Config _config;
    private readonly uint[] _msqTerritoryTypeIds = [1043,1044,1048];

    private readonly decimal _base = uint.MaxValue;

    public SkipCutscene(IPluginLog PluginLog)
    {
        if (Interface.GetPluginConfig() is not Config configuration || configuration.Version == 0)
            configuration = new Config { IsEnabled = true, Version = 1 };

        _config = configuration;

        Address.Offset1 = SigScanner.ScanText("75 ?? 48 8b 0d ?? ?? ?? ?? ba ?? 00 00 00 48 83 c1 10 e8 ?? ?? ?? ?? 83 78 ?? ?? 74");
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
        ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose()
    {
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        SetEnabled(false);
        GC.SuppressFinalize(this);
    }

    public string Name => "SkipCutscene";

    [PluginService] public IDalamudPluginInterface Interface { get; private set; }

    [PluginService] public ISigScanner SigScanner { get; private set; }

    [PluginService] public ICommandManager CommandManager { get; private set; }

    [PluginService] public IClientState ClientState { get; private set; }

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
            SafeMemory.Write<short>(Address.Offset1, 14709);
            SafeMemory.Write<short>(Address.Offset2, 6260);
        }
    }

    private void OnTerritoryChanged(uint territoryType)
    {
        if (_msqTerritoryTypeIds.Contains(territoryType) && _config.IsEnabled)
            SetEnabled(true);
        else
            SetEnabled(false);
    }

    private void OnCommand(string command, string arguments)
    {
        if (command.ToLower() != "/sc") return;
        ChatGui.Print(_config.IsEnabled ? "Skip Cutscene: Disabled" : "Skip Cutscene: Enabled");
        _config.IsEnabled = !_config.IsEnabled;
        Interface.SavePluginConfig(_config);
    }
}
