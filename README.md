# Starfield Tools - Load Order Editor

## Important Notes
- May conflict with mod managers like Vortex or MO2. Running Vortex after using this tool can disrupt your load order.
- Works with `.esm` and `.ba2` files but has limited support for loose file mods.

## Key Features

### Mod Management
- Reorder mods using drag-and-drop or hotkeys (`WASD`).
- Enable/disable mods without removing files.
- Multi-select mods (`Ctrl+Click` or `Shift+Click`).
- Install multiple mods by dragging and dropping `.zip`, `.7z` archives.
- Prevent unnecessary re-downloading of Creations mods.
- View LOOT groups if LOOT is installed.

### Profiles
- Create and switch between mod profiles for different save games.
- Ensures load order is restored after being affected by other apps.

### Backup & Restore
- Automatically backs up `Plugins.txt` on first run.
- Restore the original file via **File -> Restore Plugins.txt**.

### Dark Mode
- Switch between light, dark, or system themes via **View -> Theme** (restart required).

## Reset Options

### Delete Loose File Folders
- Cleans leftover mod files but does not affect SFSE files.
- Deletes the following folders:
  - `meshes`, `interface`, `scripts`, `sound`, `geometries`
  - Various `textures` subfolders (`actors`, `architecture`, `common`, etc.)
  - `materials` (contents deleted but folder preserved)

### Reset Everything
- Restores `Starfield.ini` and `StarfieldCustom.ini` to default.
- Disables loose files and resets changes made by Vortex.
- Deletes non-essential `.ba2` archives without `.esm` files.

## Creations Mods

### Mod Blocking
- Prevent Creations mods from being automatically downloaded.
- Enable the **Blocked** column under **View -> Columns -> Blocked**.
- Right-click a mod to block/unblock it.
- Blocked mods are saved in `%localappdata%\Starfield Tools\BlockedMods.txt`.

### Managing Mods
- Adjust load order, enable/disable mods outside of the game.
- Subscribe/unsubscribe/bookmark mods via the Creations website.
- Some Creations mods cannot be un-subscribed due to occasional Bethesda bugs.

### Using LOOT for Autosorting
- If LOOT is installed, press **Autosort** to organize your load order after making in-game changes.

## Game Version Switching

### Switching Between Steam & MS Store Versions
- Locate the directory with `Starfield.exe`:
  - **Steam:** `E:\SteamLibrary\steamapps\common\Starfield`
  - **MS Store:** `F:\XboxGames\Starfield\Content`
- Use **Game -> Game Version** to select the version to run.
- Set paths via **Tools -> Starfield Path** (only needed once per version).
- Mods for each version go into their respective game folders.

## Catalog Repair Tool

### Usage Instructions
- Run the tool before and after accessing the Creations menu to prevent lockups.
- Use **Backup** after entering Creations to save the catalog file.
- Use **Restore** to revert the catalog if necessary.
- **Check** and **Clean** functions ensure catalog integrity.

## Additional Information
- Works offline.
- Faster than the Creations menu and displays more of the load order at once.
- No `.ini` edits, folder junctions, or virtual folders required.
- The tool automatically quits after launching the game.

## Resources & Credits
- **7-Zip** - [7-Zip](https://www.7-zip.org)
- **SevenZipExtractor** - [SevenZipExtractor](https://github.com/adoconnection/SevenZipExtractor)
- **Narod's Steam Game Finder** - [Steam Game Finder](https://github.com/NarodGaming/steamgamefinder)
