# Dalamud.ContextMenu
[![Nuget](https://img.shields.io/nuget/v/Dalamud.ContextMenu)](https://www.nuget.org/packages/Dalamud.ContextMenu/)

This is a library to add context menus to dalamud plugins. The logic is all copied from a deprecated version of annaclemens' XIVCommon library. This excludes sub menus and support for setting menu item order.

This will be deprecated when any signatures break or when dalamud adds native context menu support.

```csharp
GameObjectContextMenuItem gameObjectContextMenuItem;
InventoryContextMenuItem inventoryContextMenuItem;
        
ContextMenu.OnGameObjectContextMenuOpened += OnOpenGameObjectContextMenu;
ContextMenu.OnInventoryContextMenuOpened += OnOpenInventoryContextMenu;

private void OnOpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
{
	PluginLog.Log("OnOpenGameObjectContextMenu");
	args.AddCustomItem(gameObjectContextMenuItem);
}

private void OnOpenGameObjectContextMenuItem(GameObjectContextMenuItemSelectedArgs args)
{
	PluginLog.Log("OnOpenGameObjectContextMenuItem");
	PluginLog.Log($"{args.ObjectId} / {args.ObjectWorld}");
}

private void OnOpenInventoryContextMenu(InventoryContextMenuOpenArgs args)
{
	PluginLog.Log("OnOpenInventoryContextMenu");
	args.AddCustomItem(inventoryContextMenuItem);
}

private void OnOpenInventoryContextMenuItem(InventoryContextMenuItemSelectedArgs args)
{
	PluginLog.Log("OnOpenInventoryContextMenuItem");
	PluginLog.Log($"{args.ItemId} / {args.ItemHq}");
}
```