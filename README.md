# CopyPasta Native v0.3

A native Windows code snippet manager built with C# WPF and Material Design.

## 🚀 **What This Is**

CopyPasta Native is a personal code snippet manager that functions as a developer-friendly knowledge base. It allows you to store, edit, tag, and copy code snippets across various programming languages with advanced features for power users.

**CopyPasta is not a password manager.** Snippets are stored locally as plaintext JSON and are **not encrypted**. Do not store passwords, API keys, OAuth or session tokens, service-account credentials, client secrets, private keys, BitLocker recovery keys, connection strings containing credentials, or certificates containing private keys.

CopyPasta does not require network connectivity for normal operation.

## ✨ **Features**

### **Core Functionality**
- **Add, edit, and delete code snippets** with full CRUD operations
- **Support for 60+ programming languages** (HTML, CSS, JavaScript, Python, C#, Java, and many more)
- **Tag-based organization** for easy categorization and filtering
- **Search functionality** to find snippets quickly by title, language, or tags
- **Copy-to-clipboard** with one click (optional auto-clear, off by default)
- **Local JSON storage** - your data stays on your machine under `%APPDATA%\CopyPasta`
- **Modern Material Design UI** with beautiful, intuitive interface
- **About dialog** - version, storage path, and security notice

### **Advanced Features**
- **🔍 Search History** - Keeps track of your last 10 searches for quick access
- **🌟 Favorites System** - Mark frequently used snippets as favorites
- **📊 Statistics Panel** - View snippet count, favorites, languages, and tags
- **🔍 Duplicate Detection** - Automatically detects similar snippets before creation/editing
- **🎨 Dark/Light Themes** - Toggle between themes with full consistency
- **⌨️ Keyboard Shortcuts** - Power-user friendly keyboard navigation
- **📦 Export/Import** - Backup and restore your snippets as JSON
- **🎯 Multi-Select Mode** - Bulk operations for managing multiple snippets
- **💻 Syntax Highlighting** - Code editor with language-specific coloring (AvalonEdit)
- **🖱️ Smooth Scrolling** - Optimized mouse wheel scrolling through snippets
- **🎨 Theme-Aware Scrollbar** - Visible scrollbar that adapts to theme

### **Theme Management**
- **Dark Mode** - Easy on the eyes with consistent theming
- **Light Mode** - Clean, modern interface
- **Theme Persistence** - Maintains your preference across all operations
- **Visual Scrollbar** - Always visible, theme-aware scrollbar indicator

## 🆕 **What's New in v0.3**

Enterprise-hardened release. Existing v0.2 snippet features are unchanged; this version reduces attack surface and makes the app easier to evaluate on managed Windows endpoints.

### **Security and reliability**
- **.NET 10** Windows/WPF target (self-contained publish no longer ships the old .NET 8 runtime)
- **Unsafe BinaryFormatter compatibility disabled**
- **JSON import treated as untrusted input** — no arbitrary .NET types, file-size / snippet-count / field-length limits, invalid files rejected without touching the live database
- **Atomic saves** to `%APPDATA%\CopyPasta\snippets.json` with one previous backup (`snippets.json.bak`)
- **No silent save failures** — the user is told if persistence fails
- **Runs as a standard user** (`asInvoker`) — no administrator privileges
- **About dialog (ℹ)** with version `0.3.0`, data path, and a warning not to store secrets
- **Optional clipboard auto-clear** (off by default) — clears only if the clipboard still holds the exact text CopyPasta copied
- **`SECURITY.md`** documenting what CopyPasta does and does not do

### **Build and release**
- Local release script: `scripts/Release-CopyPasta.ps1` (clean, test, vulnerability audit, publish, SHA-256)
- Unit tests for import validation, atomic save/backup, and clipboard-clear policy
- Visual Studio is **not required** to produce a release `.exe`

v0.2 feature work (syntax highlighting, export/import, favorites, shortcuts, duplicate detection, statistics, multi-select) remains in this build. See Version History below.

## ⌨️ **Keyboard Shortcuts**

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Focus search box |
| `Ctrl+N` | Create new snippet |
| `Ctrl+C` | Copy selected snippet code |
| `Ctrl+A` | Select all snippets (multi-select mode) |
| `Ctrl+D` | Deselect all (multi-select mode) |
| `Delete` | Delete selected snippet(s) |
| `Enter` | Copy code and move to next snippet |
| `Esc` | Clear all filters |
| `↑` `↓` | Navigate through snippets |

## 🎯 **Multi-Select Mode**

Enable multi-select mode to perform bulk operations:
- **Select All** - Choose all filtered snippets at once
- **Deselect All** - Clear all selections
- **Bulk Delete** - Delete multiple snippets simultaneously
- **Visual Indicators** - Checkboxes and highlighting show selected state
- **Selection Counter** - See how many items are selected

## 🔍 **Search Features**

- **Real-time Search** - Debounced search as you type
- **Search History** - Quick access to last 10 searches
- **Tag Filtering** - Click tags to filter snippets
- **Favorites Filter** - Show only favorited snippets
- **Clear Filters** - Reset to show all snippets

## 🛠 **Tech Stack**

- **Framework**: .NET 10 WPF
- **UI**: Material Design for WPF
- **Code Editor**: ICSharpCode.AvalonEdit
- **Storage**: Local JSON files (stored in `%APPDATA%\CopyPasta\`)
- **Architecture**: MVVM-inspired with direct UI manipulation

## 📁 **Project Structure**

```
CopyPastaNative/
├── Models/
│   └── Snippet.cs                 # Data model for code snippets
├── Services/
│   ├── SnippetService.cs          # Persistence, CRUD, duplicate detection
│   ├── AppSettings.cs             # Local preference model
│   └── SettingsService.cs         # settings.json load/save
├── Security/
│   ├── SnippetJson.cs             # Safe JSON serializer settings
│   ├── SnippetLimits.cs           # Import/size bounds
│   ├── SnippetValidator.cs        # Untrusted import validation
│   └── ClipboardClearPolicy.cs    # Exact-match clipboard clear
├── Converters/
│   └── CountToVisibilityConverter.cs
├── CopyPastaNative.Tests/         # xUnit tests
├── scripts/
│   └── Release-CopyPasta.ps1      # Local Release publish + hashes
├── AboutWindow.xaml               # Version, storage path, security notice
├── MainWindow.xaml                # Main application window
├── SnippetDialog.xaml             # Add/edit snippet dialog
├── App.xaml                       # Application resources
├── app.manifest                   # asInvoker (no elevation)
├── SECURITY.md                    # Security policy
├── README.md
└── CopyPastaNative.csproj
```

## 🚀 **Getting Started**

### **Prerequisites**
- Windows 10/11
- A published **self-contained** release does **not** require Visual Studio or a separate .NET install
- Building from source requires the **.NET 10 SDK**

### **Installation**
1. Download the latest GitHub release zip
2. Verify the published SHA-256 hash if you are evaluating the build for a managed endpoint
3. Extract to your preferred location
4. Run `CopyPastaNative.exe` as a standard user (do not Run as administrator)

### **First Run**
- The app will create sample snippets to get you started
- Data is saved under `%APPDATA%\CopyPasta\`:
  - `snippets.json` — active snippet database
  - `snippets.json.bak` — previous valid database after a successful save
  - `settings.json` — local preferences (clipboard auto-clear)

### **Building a release from source**
From the repository root (no Visual Studio required):

```powershell
.\scripts\Release-CopyPasta.ps1
```

Output is written to `artifacts\CopyPasta_v0.3.0\` plus `CopyPasta_v0.3.0.zip` and `artifacts\RELEASE_HASHES.md`.

## 💡 **Usage Guide**

### **Basic Operations**
1. **Add Snippet**: Click the "+ New Snippet" button or press `Ctrl+N`
2. **Edit**: Click the pencil icon on any snippet to modify it
3. **Copy**: Click the copy icon to copy code to clipboard instantly
4. **Favorite**: Click the star icon to mark as favorite
5. **Delete**: Click the trash icon or press `Delete` key

### **Search & Filter**
1. **Search**: Type in the search box to find snippets by title, language, or content
2. **Tag Filter**: Click on tag buttons to filter by category
3. **Favorites Only**: Check "Show Favorites Only" to see starred snippets
4. **History**: Your last 10 searches appear in the Recent Searches panel

### **Multi-Select Mode**
1. Enable "Multi-Select Mode" checkbox
2. Left-click individual snippets to toggle selection
3. Use `Ctrl+A` to select all, `Ctrl+D` to deselect all
4. Use bulk delete to remove multiple snippets at once

### **Export/Import**
1. **Export**: Click "Export Snippets" to save your collection as JSON
2. **Import**: Click "Import Snippets" to load a backup (replace or add)
3. Imports are validated (size, count, field lengths). Malformed files are rejected and do not change your live database. Imported snippet text is never executed.

### **About and clipboard auto-clear**
- Click **ℹ** in the header for version, storage location, and the secret-storage warning
- Clipboard auto-clear is **off by default**. If enabled, CopyPasta clears the clipboard only when it still contains the exact snippet it copied

### **Theme Toggle**
- Click the moon/sun icon to switch between dark and light themes
- Theme preference is maintained across all operations in the current session

## 📝 **Data Format**

Snippets are stored with the following structure:
```json
{
  "id": "unique-guid",
  "title": "Snippet Title",
  "language": "csharp",
  "tags": ["tag1", "tag2"],
  "code": "// Your code here",
  "isFavorite": false,
  "createdAt": "2024-01-01T00:00:00",
  "updatedAt": "2024-01-01T00:00:00"
}
```

## 🔄 **Version History**

### **v0.3** (Current)
- Targeted .NET 10; disabled unsafe BinaryFormatter compatibility
- Hardened JSON import (bounds, no type-name deserialization)
- Atomic AppData saves with one local backup
- About dialog, secret-storage warning, `SECURITY.md`
- Optional clipboard auto-clear (off by default)
- Local release script and unit tests; Visual Studio not required to publish

### **v0.2**
- Added syntax highlighting with AvalonEdit
- Implemented export/import functionality
- Added favorites system with filtering
- Comprehensive keyboard shortcuts
- Duplicate detection algorithm
- Statistics panel with real-time analytics
- Multi-select and bulk operations
- Search history (last 10 searches)
- Enhanced scrolling with mouse wheel
- Visible, theme-aware scrollbar

### **v0.1.1**
- Fixed tag filtering system
- Resolved dark theme inconsistencies
- Eliminated false error dialogs
- Improved UI responsiveness
- Enhanced theme persistence

## Enterprise / Security Characteristics

- Runs entirely in the user's context
- Does not require administrator privileges
- Does not install a service
- Does not establish persistence
- Does not execute stored snippets
- Does not require network connectivity
- Does not contain telemetry
- Stores snippets locally under `%APPDATA%\CopyPasta`
- Snippet database and exports are plaintext
- Should not be used for storing credentials or secrets

See [SECURITY.md](SECURITY.md) for the full security policy, import behavior, and vulnerability reporting process.

## 🌟 **What's Next (Future Versions)**

v0.3 is an enterprise-hardening release. Features that increase attack surface (plugins, cloud sync, snippet execution, automatic updates) are intentionally out of scope.

## 📄 **License**

This project is open source and available under the MIT License.

## 🤝 **Contributing**

This is currently a personal project, but contributions are welcome! Feel free to submit issues or pull requests.

---

**Version**: 0.3.0  
**Release Date**: August 2026  
**Status**: Enterprise-Hardened local snippet manager
