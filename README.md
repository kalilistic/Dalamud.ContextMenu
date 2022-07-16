# Dalamud.ContextMenu
[![Nuget](https://img.shields.io/nuget/v/Dalamud.ContextMenu)](https://www.nuget.org/packages/Dalamud.ContextMenu/)

This is a library to add context menus to dalamud plugins.

### Features
- Add context menu items to game objects (e.g. players).
- Add context menu items to inventory items.
- Add an dalamud indicator icon so players know a plugin is adding the menu item (optional).

### Limitations
- No sub menus.
- No random inserts - all custom items are added to the end of the menu.


### Credits
- The logic is all copied from a deprecated version of annaclemens' XIVCommon library.

### Example

```csharp
// create instance
DalamudContextMenu contextMenu = new DalamudContextMenu();

// create context menu item
GameObjectContextMenuItem contextMenuItem = new GameObjectContextMenuItem(
    new SeString(new TextPayload("My Menu Item"), // text
    MyMenuItemAction, // action to invoke
    true); // use dalamud indicator

// add event handler
contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;

// add custom item on game object menu open
private void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
{
    args.AddCustomItem(contextMenuItem);
}

// add action method when custom item is clicked
private void MyMenuItemAction(GameObjectContextMenuItemSelectedArgs args)
{
    // do something
}

// dispose
contextMenu.OnOpenGameObjectContextMenu -= OpenGameObjectContextMenu;
contextMenu.Dispose();
```