using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace Dalamud.ContextMenu;

internal class Service
{
    [PluginService]
    public static IGameGui GameGui { get; private set; } = null!;
    
    [PluginService]
    public static IClientState ClientState { get; private set; } = null!;
    
    [PluginService]
    public static IPluginLog Logger { get; private set; } = null!;
    
    [PluginService]
    public static ISigScanner SigScanner { get; private set; } = null!;
    
    [PluginService]
    public static IDataManager DataManager { get; private set; } = null!;
    
    [PluginService]
    public static ISigScanner Scanner { get; private set; } = null!;
}