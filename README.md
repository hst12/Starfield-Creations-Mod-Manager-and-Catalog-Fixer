# Starfield Tools - Load Order Editor

## ⚠️ Important Notice
Use this tool cautiously if you're using other mod managers like Vortex or MO2, as they may conflict with it. Running Vortex after using this tool may disrupt your load order.

## 📌 Overview
Starfield Tools provides a load order editor for managing mods in Starfield. It automatically checks the `ContentCatalog.txt` file upon launch and includes options to repair the file.

---

## 🚀 Key Features

### 🛠️ Mod Management
- Reorder mods using **drag-and-drop** or **hotkeys** (`WASD`).
- Enable/disable mods **without removing files**.
- Multi-select mods using `Ctrl+Click` or `Shift+Click` for **batch actions**.
- Drag and drop **mod archives** (e.g., `.zip`, `.7z`) onto the grid to install multiple mods at once.
- **Mods -> Prepare for Creations Update**: Prevents re-downloading of updated Creations mods.
- **Block Mods**: Block specific mods from activation via the **right-click menu**.
  - Blocked mods are saved in `BlockedMods.txt` under `%localappdata%\Starfield Tools`.
  - You can manually edit this file via **File -> Edit Files -> Edit BlockedMods.txt**.
- View **LOOT groups** if LOOT is installed (**View -> Columns -> Group**).
- **Launch Starfield** directly from the tool (supports Steam, MS Store, or SFSE versions).

### 📂 Profiles
- Create and switch between **mod profiles** for different save games.
- Profiles ensure your **load order remains intact** even after being affected by other apps.

### 🔄 Backup & Restore
- **Automatically** backs up `Plugins.txt` on first run.
- Use **File -> Restore Plugins.txt** to revert to the original file.

### 🌙 Dark Mode
- Switch between **light, dark, or system themes** via **View -> Theme**.
- Restart the app after changing the theme for effects to take place.

---

## 📖 Usage Notes
- The tool works with `.esm` and `.ba2` files but **does not support loose files** or FOMOD installations.
- **Profiles and backups** are essential for managing load orders across **different game versions** (Steam/MS Store).
- The tool **automatically exits after launching the game**.

---

## 🔄 Reset Options

### 🗑️ Delete Loose File Folders (**File -> Reset/Delete Loose File Folders**)
- Deletes **non-vanilla game folders** (e.g., `meshes`, `textures`, `scripts`) while preserving `materials` (contents deleted).
- **SFSE files** and folders remain untouched.
- Use this option to clean up leftover files from **uninstalled mods**. Avoid using it if you wish to keep loose file mods.

### 🔄 Full Reset (**File -> Reset/Reset Everything**)
- Almost like a **clean game install**, but keeps `.esm` and `.ba2` formatted mods.
- Restores `Starfield.ini` and `StarfieldCustom.ini` to **default settings**.
- **Disables loose files**.
- Deletes commonly used **loose file folders**, but **preserves SFSE folders**.
- Optionally deletes **leftover `.ba2` archives** without corresponding `.esm` files.
- Resets **Starfield Documents folder** changes made by Vortex.
- Deletes `Starfield.ccc`.

---

## 📝 Notes
- For best results, back up your load order before making changes.
- Keep profiles updated to ensure load order consistency across versions.

---

Happy modding! 🚀✨

