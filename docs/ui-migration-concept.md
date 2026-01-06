# Greenshot UI Migration Concept

## Executive Summary

This document outlines a comprehensive plan to migrate Greenshot's legacy Windows Forms UI to a modern, scalable UI framework. The migration will be gradual, opt-in during development, and maintain backward compatibility until completion.

## Current State Analysis

### Existing UI Technology Stack

**Primary Framework**: Windows Forms (.NET Framework 4.7.2)
- **Main Application**: `src/Greenshot/Forms/` - 13 form files
- **Image Editor**: `src/Greenshot.Editor/Forms/` - 11 form files  
- **Plugins**: Each plugin has 1-4 forms
- **Total Lines**: ~13,785 lines across all forms

**Key UI Components**:
1. **System Tray Icon & Context Menu** (`MainForm.cs`)
   - Capture options (area, window, fullscreen, last region, IE)
   - File operations (open from clipboard/file)
   - Quick settings and preferences
   - Help, donate, about, exit

2. **Main Forms**:
   - `AboutForm.cs` (389 lines) - Custom animated "G" logo
   - `SettingsForm.cs` (861 lines) - Complex preferences with tabs
   - `MainForm.cs` (2000+ lines) - Application shell and orchestration
   - `LanguageDialog.cs` - Language selection
   - `BugReportForm.cs` - Error reporting
   - `CaptureForm.cs` - Capture preview/selection
   - `PrintOptionsDialog.cs` - Print settings

3. **Image Editor** (`Greenshot.Editor/Forms/`):
   - `ImageEditorForm.cs` (2045 lines) - Main editor window
   - `ColorDialog.cs` - Color picker
   - `ResizeSettingsForm.cs` - Image resize options
   - `DropShadowSettingsForm.cs` - Drop shadow effects
   - `TornEdgeSettingsForm.cs` - Torn edge effects
   - `MovableShowColorForm.cs` - Color preview widget

### Translation System

**Current Implementation**: Custom XML-based system
- **Location**: `src/Greenshot/Languages/` and plugin-specific `Languages/` folders
- **Format**: XML files named `language-{IETF}.xml` (e.g., `language-en-US.xml`)
- **Loader**: `src/Greenshot.Base/Core/Language.cs` - Custom parser
- **Structure**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<language description="English" ietf="en-US" version="1.0.4" languagegroup="1">
  <resources>
    <resource name="about_title">About Greenshot</resource>
    <resource name="contextmenu_settings">Preferences...</resource>
    <!-- 300+ resources per language -->
  </resources>
</language>
```

**Supported Languages**: 25+ languages including:
- English (en-US), German (de-DE), French (fr-FR), Spanish (es-ES)
- Chinese (zh-CN, zh-TW), Japanese (ja-JP), Korean (ko-KR)
- Russian (ru-RU), Polish (pl-PL), Czech (cs-CZ)
- And many more...

**Usage in Code**:
```csharp
Language.GetString(LangKey.about_title)
Language.GetFormattedString(LangKey.error_openlink, link)
```

**Issues**:
- Not human-readable (XML overhead)
- No IDE/tooling support for translations
- Difficult integration with modern translation platforms
- No machine translation compatibility
- Hard to maintain consistency across 25+ languages

### Configuration System

**Implementation**: Custom INI file system
- **File**: `greenshot.ini` (created at runtime in user profile)
- **Library**: `src/Greenshot.Base/IniFile/` - Custom INI parser with attributes
- **Main Config**: `CoreConfiguration.cs` with `[IniProperty]` attributes

**Example Configuration**:
```csharp
[IniSection("Core", Description = "Greenshot core configuration")]
public class CoreConfiguration : IniSection
{
    [IniProperty("Language", Description = "The language in IETF format")]
    public string Language { get; set; }
    
    [IniProperty("IsFirstLaunch", DefaultValue = "true")]
    public bool IsFirstLaunch { get; set; }
}
```

**Access Pattern**:
```csharp
var conf = IniConfig.GetIniSection<CoreConfiguration>();
conf.Language = "de-DE";
```

### Existing Modern UI Components

**WPF Already in Use**: The Confluence plugin already uses WPF!
- **Location**: `src/Greenshot.Plugin.Confluence/Forms/*.xaml`
- **Technology**: WPF with XAML
- **Custom Markup Extension**: `{support:Translate}` for localization
- **Files**:
  - `ConfluenceConfigurationForm.xaml` - Settings dialog
  - `ConfluencePagePicker.xaml` - Page selection
  - `ConfluenceSearch.xaml` - Search interface
  - `ConfluenceTreePicker.xaml` - Tree navigation
  - `ConfluenceUpload.xaml` - Upload dialog

**Project Configuration**: 
- `Microsoft.NET.Sdk.WindowsDesktop` SDK
- `UseWPF` and `UseWindowsForms` both enabled
- Target Framework: .NET Framework 4.7.2

**This is significant**: We already have proof that WPF works in the Greenshot architecture!

### Known Issues with Current UI

1. **Scaling Problems**: Windows Forms doesn't handle DPI scaling well
2. **Translation Overflow**: Text longer in translation causes layout issues
3. **Dated Appearance**: 2000s-era look and feel
4. **Limited Flexibility**: Hard to theme or customize
5. **Accessibility**: Limited support for screen readers and accessibility features
6. **Cross-platform**: Windows Forms is Windows-only

## Technology Recommendations - Comprehensive Analysis

This section provides an objective comparison of modern UI frameworks for Greenshot's migration, evaluating each option against key criteria: cross-platform support, migration effort, maturity, and long-term viability.

### Evaluation Criteria

Before diving into specific technologies, let's establish the evaluation criteria:

1. **Cross-Platform Support** - Can it run on Windows, Linux, macOS?
2. **Migration Complexity** - Effort required to migrate from WinForms
3. **Framework Requirements** - Does it require .NET upgrade?
4. **Maturity & Stability** - Production-ready? Large ecosystem?
5. **Performance** - Suitable for desktop screenshot tool?
6. **Developer Experience** - Tooling, debugging, documentation
7. **Future-Proofing** - Active development, long-term support
8. **Gradual Migration** - Can it coexist with WinForms during migration?

### Option 1: Avalonia UI

**Overview**: Modern, cross-platform XAML-based UI framework inspired by WPF, supporting Windows, Linux, macOS, iOS, Android, and WebAssembly.

**Pros**:
- ✅ **True Cross-Platform** - Native apps on Windows, Linux (X11/Wayland), macOS
- ✅ **XAML/MVVM** - Familiar pattern for .NET developers, similar to WPF
- ✅ **Modern & Actively Developed** - Regular releases, growing community
- ✅ **Excellent DPI Scaling** - Built-in support across all platforms
- ✅ **Flexible Styling** - FluentTheme, Material Design, custom themes
- ✅ **Future-Proof** - .NET 6/7/8 support, modern C# features
- ✅ **Good Documentation** - Comprehensive guides and samples
- ✅ **Open Source** - MIT license, community-driven
- ✅ **VS Code & Rider Support** - XAML previewer, hot reload
- ✅ **Native Look** - Can adapt to platform conventions (Windows 11, macOS)

**Cons**:
- ❌ **Requires .NET Upgrade** - Best with .NET 6+, though .NET Framework 4.6.1+ supported
- ❌ **Smaller Ecosystem** - Fewer third-party controls vs WPF (but growing)
- ❌ **Migration Effort** - All UI code needs rewriting (though XAML is similar to WPF)
- ❌ **No Interop with WinForms** - Cannot mix Avalonia and WinForms in same app
- ❌ **Learning Curve** - Platform differences (Linux/macOS) require testing
- ❌ **Designer Maturity** - Visual Studio designer not as mature as WPF's
- ❌ **Some WPF Features Missing** - Not 100% WPF-compatible (e.g., 3D, some controls)

**Migration Complexity**: **HIGH**
- Requires .NET version upgrade (4.7.2 → .NET 6+)
- Complete UI rewrite - cannot gradually migrate
- Must handle platform-specific behaviors (file dialogs, system tray on Linux/macOS)
- Testing required on all target platforms

**Cross-Platform Considerations**:
- **Linux**: Needs X11/Wayland, testing on various distros (Ubuntu, Fedora, etc.)
- **macOS**: Requires macOS-specific builds, notarization for distribution
- **System Tray**: Works differently on each platform (need abstraction)
- **Keyboard Shortcuts**: Platform conventions differ (Ctrl vs Cmd)

**Timeline Impact**: 
- Infrastructure: +2-3 weeks (setup .NET 6+, CI/CD for multi-platform)
- Per Component: +30-50% time (cross-platform testing, platform-specific code)
- Total: ~35-40 weeks vs 27 weeks for WPF

**Best For**: Projects ready to commit to cross-platform support NOW and accept the .NET upgrade

### Option 2: WPF (Windows Presentation Foundation)

**Overview**: Microsoft's mature, Windows-only UI framework with XAML and extensive tooling support.

**Pros**:
- ✅ **Already Proven in Greenshot** - Confluence plugin uses WPF successfully
- ✅ **No .NET Upgrade** - Works with current .NET Framework 4.7.2
- ✅ **Mature & Stable** - 15+ years of production use, extensive ecosystem
- ✅ **Excellent Tooling** - Visual Studio designer, XAML IntelliSense, hot reload
- ✅ **Rich Control Library** - Thousands of third-party controls (DevExpress, Telerik, etc.)
- ✅ **Gradual Migration** - Can run side-by-side with WinForms
- ✅ **Excellent DPI Scaling** - Per-monitor DPI awareness built-in
- ✅ **Large Community** - Massive StackOverflow presence, tutorials, samples
- ✅ **Performance** - Hardware-accelerated rendering
- ✅ **Accessibility** - Full UI Automation support
- ✅ **XAML Skills Transferable** - Similar to Avalonia, UWP, MAUI

**Cons**:
- ❌ **Windows-Only** - No Linux or macOS support
- ❌ **Legacy Technology** - Microsoft focus on newer frameworks (MAUI, WinUI 3)
- ❌ **No Future .NET Core Support** - Stays on .NET Framework (Windows-only)
- ❌ **Themes Look Dated** - Default controls have Windows 7/8 appearance
- ❌ **No Built-in Modern Controls** - Need third-party libs for hamburger menus, etc.

**Migration Complexity**: **LOW**
- No framework upgrade required
- Gradual component-by-component migration possible
- WinForms and WPF can coexist in same application
- Existing Confluence plugin serves as reference implementation

**Timeline Impact**:
- Follows planned 27-week timeline
- Infrastructure: 2 weeks
- Components: As planned (About: 2w, Settings: 5w, etc.)

**Future Cross-Platform Path**:
- WPF XAML knowledge transfers well to Avalonia
- UI structure/MVVM patterns reusable
- Future migration to Avalonia would be easier than WinForms → Avalonia

**Best For**: Projects prioritizing quick migration, low risk, Windows-only for now

### Option 3: .NET MAUI (Multi-platform App UI)

**Overview**: Microsoft's latest cross-platform framework, successor to Xamarin.Forms, targeting mobile and desktop.

**Pros**:
- ✅ **Official Microsoft** - Long-term support guaranteed
- ✅ **Cross-Platform** - Windows, macOS, iOS, Android, Samsung Tizen
- ✅ **Modern .NET** - .NET 6/7/8, latest C# features
- ✅ **XAML-Based** - Familiar to WPF developers
- ✅ **Hot Reload** - Fast development iteration
- ✅ **Single Project** - Share code and UI across platforms

**Cons**:
- ❌ **Requires .NET 6+** - Complete framework upgrade
- ❌ **Desktop Support Immature** - Primarily designed for mobile apps
- ❌ **Not Ideal for Desktop Tools** - Better for consumer apps than productivity tools
- ❌ **Limited Desktop Controls** - Focused on mobile-style UI patterns
- ❌ **No WinForms Interop** - Cannot gradual migrate
- ❌ **Linux Not Officially Supported** - Community effort only
- ❌ **Different XAML** - Not compatible with WPF/Avalonia XAML
- ❌ **System Tray Limited** - Not a first-class scenario
- ❌ **Massive Migration** - Complete application rewrite

**Migration Complexity**: **VERY HIGH**
- Requires .NET 6+ upgrade
- Complete UI and architecture rewrite
- Mobile-first patterns don't fit screenshot tool
- System tray, global hotkeys need platform-specific code

**Timeline Impact**: ~50-60 weeks (double the WPF timeline)

**Best For**: Mobile-first apps or apps targeting mobile + desktop equally. Not recommended for Greenshot.

### Option 4: Uno Platform

**Overview**: Cross-platform UI framework that runs on Windows, Linux, macOS, iOS, Android, and WebAssembly using WinUI/UWP XAML.

**Pros**:
- ✅ **True Cross-Platform** - Most platforms of any framework (incl. WebAssembly)
- ✅ **WinUI XAML** - Microsoft's latest XAML flavor
- ✅ **Pixel-Perfect** - Same UI on all platforms
- ✅ **Good Documentation** - Extensive guides
- ✅ **Active Development** - Frequent releases

**Cons**:
- ❌ **Requires .NET 6+** - Framework upgrade needed
- ❌ **WinUI XAML** - Different from WPF XAML (less transferable)
- ❌ **Smaller Community** - Less mature than Avalonia or WPF
- ❌ **Desktop Not Primary Focus** - More emphasis on mobile/web
- ❌ **No WinForms Interop** - No gradual migration
- ❌ **Complex Build Setup** - Multiple platform targets

**Migration Complexity**: **VERY HIGH**

**Best For**: Apps needing WebAssembly support or pixel-perfect UI across all platforms. Not recommended for Greenshot.

### Option 5: Electron.NET

**Overview**: Use web technologies (HTML/CSS/JavaScript or Blazor) with .NET backend in cross-platform desktop app.

**Pros**:
- ✅ **Cross-Platform** - Windows, Linux, macOS
- ✅ **Web Technologies** - Familiar to web developers
- ✅ **Rich Ecosystem** - npm packages available

**Cons**:
- ❌ **Large Footprint** - Bundles Chromium (~100MB+ base size)
- ❌ **Memory Intensive** - Not suitable for lightweight tools
- ❌ **Startup Performance** - Slower than native
- ❌ **Not Native Feel** - Web-based UI
- ❌ **Wrong Paradigm** - Screenshot tool needs native integration
- ❌ **Massive Rewrite** - Complete change in architecture

**Migration Complexity**: **VERY HIGH**

**Best For**: Web apps that need desktop packaging. Not recommended for Greenshot.

### Comparison Matrix

| Criteria | Avalonia | WPF | MAUI | Uno Platform | Electron.NET |
|----------|----------|-----|------|--------------|--------------|
| **Cross-Platform** | ⭐⭐⭐⭐⭐ | ❌ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Migration Complexity** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐ | ⭐ | ⭐ |
| **No .NET Upgrade** | ❌ | ✅ | ❌ | ❌ | ❌ |
| **Maturity** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| **Tooling** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Gradual Migration** | ❌ | ✅ | ❌ | ❌ | ❌ |
| **Desktop-Focused** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| **Timeline (weeks)** | 35-40 | 27 | 50-60 | 50-60 | 60+ |
| **Community Size** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |

### Decision Framework

**Choose WPF if:**
- ✅ Windows-only support acceptable for now
- ✅ Want fastest migration with lowest risk
- ✅ Need to stay on .NET Framework 4.7.2
- ✅ Want gradual, component-by-component migration
- ✅ Cross-platform is a future consideration, not immediate need

**Choose Avalonia if:**
- ✅ Cross-platform support is essential NOW
- ✅ Ready to upgrade to .NET 6+ (accept the migration cost)
- ✅ Can commit to full UI rewrite (no gradual migration)
- ✅ Want modern, actively developed framework
- ✅ Can accept longer timeline (~35-40 weeks)
- ✅ Have resources to test on Linux/macOS

**Don't Choose MAUI/Uno/Electron.NET** - Not suitable for desktop screenshot tools

### Recommended Approach: Two-Phase Strategy

Given the tension between quick migration and cross-platform future, consider this pragmatic approach:

#### **Phase A: WPF Migration (Now - 27 weeks)**
1. Migrate to WPF using documented 8-phase plan
2. Solve immediate issues (DPI scaling, modern look, translations)
3. Gain XAML/MVVM experience with low risk
4. Deliver value to users quickly
5. Stay on .NET Framework 4.7.2

#### **Phase B: Avalonia Migration (Future - When Ready)**
1. Upgrade to .NET 6+ (done as separate effort)
2. Migrate from WPF to Avalonia (much easier than WinForms → Avalonia)
3. XAML and MVVM patterns transfer directly
4. UI structure already modern and well-organized
5. Enable Linux/macOS support

**Why This Works**:
- ✅ Solves current problems immediately with low risk
- ✅ Doesn't block future cross-platform (makes it easier, actually)
- ✅ WPF → Avalonia is much easier than WinForms → Avalonia
- ✅ XAML skills are 80% transferable between WPF and Avalonia
- ✅ Spreads migration cost over time
- ✅ Allows .NET upgrade to be planned separately
- ✅ Validates MVVM architecture before committing to Avalonia

### Alternative: Direct to Avalonia

**If cross-platform is critical now**, skip WPF and go directly to Avalonia:

**Updated Timeline**: ~35-40 weeks
- Phase 0: Infrastructure + .NET upgrade (4 weeks)
- Phase 1: About Dialog (3 weeks - includes cross-platform testing)
- Phase 2: System Tray (5 weeks - Linux/macOS tray differs significantly)
- Phase 3: Settings (7 weeks - platform-specific file dialogs, etc.)
- Phases 4-6: Other dialogs (6 weeks)
- Phase 7: Image Editor (10 weeks - complex cross-platform testing)
- Phase 8: Cutover (3 weeks)

**Additional Requirements**:
- CI/CD for Linux and macOS builds
- Test infrastructure for all platforms
- Handle platform differences (system tray, file dialogs, keyboard shortcuts)
- macOS code signing and notarization

**Risk Level**: MEDIUM-HIGH (vs LOW for WPF)

### Final Recommendation

**For Greenshot Today**: Start with **WPF**
- Lowest risk, fastest delivery
- Solves immediate problems
- Proven in existing codebase
- Enables future Avalonia migration

**For Greenshot Future**: Plan migration to **Avalonia** when:
- .NET 6+ upgrade is feasible
- Cross-platform demand is validated
- WPF migration is complete and stable
- Resources available for multi-platform testing

This two-phase approach balances immediate needs with long-term vision while minimizing risk and maximizing value delivery.

## Modern Translation System Recommendations

### Current System Issues
- XML overhead makes files less human-readable
- No standard tooling support
- Poor integration with translation platforms (Crowdin, Transifex, Weblate)
- Difficult for machine translation
- Custom parser maintenance burden

### Translation Format Options - Comprehensive Analysis

#### Option 1: JSON (JavaScript Object Notation)

**Overview**: Human-readable, hierarchical key-value format widely used in modern web and desktop apps.

**Pros**:
- ✅ **Highly Human-Readable** - Clean, minimal syntax
- ✅ **Universal Platform Support** - Crowdin, Weblate, Transifex, Lokalise all support JSON
- ✅ **Machine Translation Ready** - Easy to parse for DeepL, Google Translate, ChatGPT
- ✅ **Version Control Friendly** - Clear diffs, easy merge conflict resolution
- ✅ **Industry Standard** - Used by React, Angular, Vue, Electron apps
- ✅ **Excellent Tooling** - IDE support, validation, linters
- ✅ **Flexible Structure** - Nested hierarchies for organization
- ✅ **No Dependencies** - Built into .NET with System.Text.Json
- ✅ **Fast Parsing** - Lightweight and performant

**Cons**:
- ❌ **No Built-in WPF Integration** - Needs custom markup extension (but easy to implement)
- ❌ **Runtime Loading** - Not compiled into assemblies (can be mitigated with build-time RESX generation)
- ❌ **No Pluralization** - Simple JSON doesn't handle plural forms (need library like i18next.net)

**Libraries**:
- **i18next.net** - Feature-rich with pluralization, interpolation, namespacing
- **Custom Parser** - Simple, lightweight if only basic translation needed
- **Hybrid Approach** - JSON source → build-time RESX generation for WPF

**Example**:
```json
{
  "$schema": "./translation-schema.json",
  "language": {
    "code": "en-US",
    "name": "English (United States)"
  },
  "about": {
    "title": "About Greenshot",
    "version": "Version {0}",
    "bugs": "Please report bugs to"
  },
  "contextmenu": {
    "settings": "Preferences...",
    "capturearea": "Capture region"
  }
}
```

**Best For**: Maximum human readability, platform integration, modern tooling

#### Option 2: RESX (Resource Files)

**Overview**: Microsoft's native .NET resource format with Visual Studio integration.

**Pros**:
- ✅ **Native .NET Integration** - Built into Visual Studio and MSBuild
- ✅ **Strongly Typed** - Auto-generated C# classes with compile-time checking
- ✅ **Excellent WPF Support** - Direct binding in XAML via x:Static
- ✅ **Compiled Resources** - Embedded in assemblies for performance
- ✅ **Mature Tooling** - Visual Studio Resource Editor, ResX Manager
- ✅ **Localization Preview** - Can preview different languages in VS
- ✅ **No Custom Code** - Works out of the box

**Cons**:
- ❌ **Not Human-Readable** - XML-heavy format designed for tools, not humans
- ❌ **Poor Version Control** - Large XML diffs, merge conflicts difficult
- ❌ **Limited Platform Support** - Most translation platforms require conversion
- ❌ **Machine Translation Difficult** - Must convert to/from RESX
- ❌ **Editing Experience** - Grid editor in VS is clunky for large translations

**Example**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="about_title" xml:space="preserve">
    <value>About Greenshot</value>
  </data>
  <data name="about_version" xml:space="preserve">
    <value>Version {0}</value>
  </data>
</root>
```

**Usage in WPF**:
```xaml
<TextBlock Text="{x:Static properties:Resources.about_title}"/>
```

**Best For**: WPF projects that don't need external translation platforms, .NET-only workflow

#### Option 3: PO/POT Files (Gettext)

**Overview**: Traditional Unix/Linux localization format used by thousands of open source projects.

**Pros**:
- ✅ **Industry Standard** - 30+ years of proven use
- ✅ **Human-Readable** - Plain text format
- ✅ **Best Translation Tools** - Poedit, Lokalize, GTranslator (desktop apps for translators)
- ✅ **Platform Support** - Supported by all major translation platforms
- ✅ **Pluralization Built-in** - Native support for plural forms
- ✅ **Context Support** - Can add translator comments and context
- ✅ **Large Ecosystem** - Many tools and workflows available

**Cons**:
- ❌ **Not .NET Native** - Requires NGettext or similar library
- ❌ **File Management** - Separate .po/.pot/.mo files per language
- ❌ **Learning Curve** - Unfamiliar to .NET developers
- ❌ **Build Complexity** - Need tooling to compile .po → .mo files

**Libraries**:
- **NGettext** - Mature .NET Gettext implementation
- **Karambolo.PO** - Modern, .NET Standard library

**Example**:
```po
# Translation for About dialog
msgid "about_title"
msgstr "About Greenshot"

# Supports context
msgctxt "menu"
msgid "settings"
msgstr "Preferences..."

# Plural forms
msgid "file_count_one"
msgid_plural "file_count_many"
msgstr[0] "{0} file"
msgstr[1] "{0} files"
```

**Best For**: Cross-platform projects, teams familiar with Linux/Unix localization

#### Option 4: YAML

**Overview**: Human-readable data serialization format, popular in configuration and i18n.

**Pros**:
- ✅ **Very Human-Readable** - Minimal syntax, whitespace-based
- ✅ **Platform Support** - Crowdin, Weblate support YAML
- ✅ **Flexible** - Comments, multi-line strings, references
- ✅ **Good Tooling** - YAML linters, validators available

**Cons**:
- ❌ **Whitespace Sensitive** - Indentation errors can break parsing
- ❌ **Slower Parsing** - More complex than JSON
- ❌ **Less Common** - Not as widespread as JSON for i18n
- ❌ **Fewer .NET Libraries** - YamlDotNet is main option

**Example**:
```yaml
language:
  code: en-US
  name: English (United States)

about:
  title: About Greenshot
  version: "Version {0}"
  bugs: Please report bugs to

contextmenu:
  settings: Preferences...
  capturearea: Capture region
```

**Best For**: Teams already using YAML heavily, preference for minimal syntax

#### Option 5: XLIFF (XML Localization Interchange File Format)

**Overview**: Industry-standard XML format for translation exchange between tools.

**Pros**:
- ✅ **Translation Industry Standard** - Used by professional translation tools
- ✅ **Full Platform Support** - All translation platforms support XLIFF
- ✅ **Rich Metadata** - Translation units, context, notes, state
- ✅ **Tool Interoperability** - Exchange between CAT tools (SDL Trados, MemoQ, etc.)
- ✅ **Quality Assurance** - Built-in support for translation memory, validation

**Cons**:
- ❌ **Not Human-Readable** - Verbose XML format
- ❌ **Overkill for Simple Projects** - Designed for enterprise translation workflows
- ❌ **Complex** - Learning curve for developers
- ❌ **Limited .NET Support** - Fewer libraries compared to JSON/RESX

**Example**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
  <file source-language="en-US" target-language="de-DE" datatype="plaintext">
    <body>
      <trans-unit id="about_title">
        <source>About Greenshot</source>
        <target>Über Greenshot</target>
      </trans-unit>
    </body>
  </file>
</xliff>
```

**Best For**: Enterprise translation workflows, professional translation agencies

### Comparison Matrix: Translation Formats

| Criteria | JSON | RESX | PO/POT | YAML | XLIFF |
|----------|------|------|--------|------|-------|
| **Human Readability** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐ |
| **Platform Support** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Machine Translation** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Version Control** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **WPF Integration** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Tooling** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Setup Complexity** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| **Pluralization** | ⭐⭐⭐* | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐* | ⭐⭐⭐⭐⭐ |
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |

*With library support (i18next.net, etc.)

### Recommended Approach: Hybrid JSON + RESX

**Strategy**: Use JSON as source of truth, auto-generate RESX for WPF integration

**How It Works**:
1. **Source Files**: JSON files for all languages (human-editable, platform-friendly)
2. **Build Process**: MSBuild task converts JSON → RESX at build time
3. **XAML Usage**: Standard WPF bindings using generated RESX
4. **C# Usage**: Either RESX classes or JSON-based TranslationService

**Benefits**:
- ✅ Human-readable JSON for developers and translators
- ✅ Upload to Crowdin/Weblate easily
- ✅ Machine translation compatible
- ✅ Clean version control diffs
- ✅ Strong typing in WPF via RESX
- ✅ Best of both worlds

**Implementation**:
```
Source:           Build Time:           Runtime:
en-US.json  -->   Resources.en-US.resx  -->  WPF Binding
de-DE.json  -->   Resources.de-DE.resx  -->  or C# Code
fr-FR.json  -->   Resources.fr-FR.resx
```

**MSBuild Integration**:
```xml
<Target Name="GenerateResxFromJson" BeforeTargets="CoreCompile">
  <JsonToResxTask SourceFiles="@(JsonTranslations)" />
</Target>
```

### Alternative: Pure JSON with Custom Markup Extension

**For projects wanting zero RESX**:

Custom WPF Markup Extension:
```xaml
<!-- XAML Usage -->
<TextBlock Text="{local:Translate about.title}"/>
```

C# Implementation:
```csharp
[MarkupExtensionReturnType(typeof(string))]
public class TranslateExtension : MarkupExtension
{
    public string Key { get; set; }
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return TranslationService.Instance.Translate(Key);
    }
}
```

**Note**: Confluence plugin already uses this pattern!

### Recommendation Summary

**Primary Recommendation**: **JSON with Build-Time RESX Generation**
- Source: JSON (human-readable, platform-compatible)
- Build: Auto-generate RESX for WPF strong typing
- Platform: Upload JSON to Crowdin/Weblate
- Machine Translation: Feed JSON to DeepL/ChatGPT
- Best balance of all factors

**Alternative for WPF-Only Teams**: **Pure RESX**
- If never using translation platforms
- If prefer Visual Studio-only workflow
- Simpler setup but less flexible

**Alternative for Linux/Cross-Platform Focus**: **PO Files with NGettext**
- If targeting Linux users heavily
- If translators familiar with Poedit
- Traditional open source approach

## Step-by-Step Migration Plan

### Phase 0: Infrastructure Setup (Week 1-2)

**Tasks**:
1. ✅ Create migration concept document (this document)
2. Create new project structure:
   ```
   src/Greenshot.UI.Modern/
     Greenshot.UI.Modern.csproj
     Core/
       ModernUIManager.cs       - Manages UI mode selection
       ViewModelBase.cs         - Base class for ViewModels
     Views/
       (Empty - will be populated per phase)
     Resources/
       Translations/
         en-US.json
     Converters/
       (WPF value converters)
   ```
3. Add configuration flag to `CoreConfiguration.cs`:
   ```csharp
   [IniProperty("UseModernUI", Description = "Enable modern WPF UI (experimental)", 
                DefaultValue = "false")]
   public bool UseModernUI { get; set; }
   ```
4. Implement translation system:
   - Create JSON translation files for all existing languages
   - Build migration tool: `Tools/XmlToJsonTranslation/`
   - Create MSBuild task to generate RESX from JSON
   - Create WPF markup extension for translations
5. Create UI factory pattern:
   ```csharp
   public interface IUIFactory
   {
       IAboutDialog CreateAboutDialog();
       ISettingsDialog CreateSettingsDialog();
       // ... etc
   }
   
   public class WinFormsUIFactory : IUIFactory { }
   public class ModernUIFactory : IUIFactory { }
   ```

**Deliverables**:
- `Greenshot.UI.Modern` project created
- Translation JSON files for all 25+ languages
- Build process for RESX generation
- Configuration flag in greenshot.ini
- UI factory infrastructure

### Phase 1: About Dialog (Week 3-4) - PILOT

**Why Start Here**:
- ✅ Simple, self-contained dialog
- ✅ Good visual impact (can showcase modern styling)
- ✅ Low risk - not critical to functionality
- ✅ Has custom graphics (animated G logo) - good test of WPF capabilities

**Implementation**:

1. Create `src/Greenshot.UI.Modern/Views/AboutView.xaml`:
```xaml
<Window x:Class="Greenshot.UI.Modern.Views.AboutView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:Greenshot.UI.Modern"
        Title="{local:Translate about.title}" 
        SizeToContent="WidthAndHeight"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen"
        Background="#FF3D3D3D">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Logo (can reuse existing animation logic in code-behind) -->
        <Image Grid.Row="0" x:Name="LogoImage" Width="90" Height="90" 
               Margin="0,0,0,20"/>
        
        <!-- Version Info -->
        <TextBlock Grid.Row="1" x:Name="VersionText" 
                   FontSize="16" FontWeight="Bold"
                   Foreground="White" TextAlignment="Center"
                   Margin="0,0,0,10"/>
        
        <!-- Links -->
        <StackPanel Grid.Row="2" Margin="0,10">
            <TextBlock Foreground="LightGray">
                <Hyperlink NavigateUri="https://getgreenshot.org/" 
                           RequestNavigate="Hyperlink_RequestNavigate"
                           Foreground="#FF8AFF00">
                    <Run Text="{local:Translate about.host}"/>
                </Hyperlink>
            </TextBlock>
            <!-- More links... -->
        </StackPanel>
        
        <!-- Close Button -->
        <Button Grid.Row="3" Content="{local:Translate OK}" 
                IsDefault="True" Click="CloseButton_Click"
                Padding="20,5" HorizontalAlignment="Center"
                Margin="0,20,0,0"/>
    </Grid>
</Window>
```

2. Create ViewModel `AboutViewModel.cs`
3. Update `MainForm.cs` to check configuration and instantiate correct dialog:
```csharp
private void ShowAboutDialog()
{
    var conf = IniConfig.GetIniSection<CoreConfiguration>();
    if (conf.UseModernUI)
    {
        var modernAbout = new Greenshot.UI.Modern.Views.AboutView();
        modernAbout.ShowDialog();
    }
    else
    {
        var classicAbout = new AboutForm();
        classicAbout.ShowDialog();
    }
}
```

**Testing**:
- Manual testing with `UseModernUI=false` (default, old UI)
- Manual testing with `UseModernUI=true` (new UI)
- Test with multiple languages
- Test DPI scaling at 100%, 125%, 150%, 200%
- Visual comparison screenshots

**Success Criteria**:
- ✅ Both UIs work correctly
- ✅ Switching via config flag works
- ✅ Modern UI looks better and scales properly
- ✅ Translations work in both UIs
- ✅ No impact on users who don't enable flag

### Phase 2: System Tray Context Menu (Week 5-7)

**Why Second**:
- ✅ High-visibility component (used constantly)
- ✅ Good impact on user experience
- ✅ Relatively straightforward
- ✅ Tests the pattern for menus

**Challenges**:
- WPF doesn't have native NotifyIcon support
- Need third-party library or custom implementation

**Solution**: Use **Hardcodet.NotifyIcon.Wpf** library
- Mature, well-maintained NuGet package
- Designed specifically for WPF system tray icons
- XAML-based context menu definition

**Implementation**:

1. Add NuGet package: `Hardcodet.NotifyIcon.Wpf`
2. Create `SystemTrayView.xaml`:
```xaml
<tb:TaskbarIcon x:Class="Greenshot.UI.Modern.Views.SystemTrayView"
                xmlns:tb="http://www.hardcodet.net/taskbar"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:local="clr-namespace:Greenshot.UI.Modern"
                IconSource="/Greenshot;component/Resources/icon.ico"
                ToolTipText="Greenshot">
    
    <tb:TaskbarIcon.ContextMenu>
        <ContextMenu>
            <MenuItem Header="{local:Translate contextmenu.capturearea}"
                      Command="{Binding CaptureAreaCommand}">
                <MenuItem.Icon>
                    <Image Source="/Resources/region.png"/>
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header="{local:Translate contextmenu.capturelastregion}"
                      Command="{Binding CaptureLastRegionCommand}"/>
            <!-- ... more menu items ... -->
            <Separator/>
            <MenuItem Header="{local:Translate contextmenu.settings}"
                      Command="{Binding ShowSettingsCommand}"/>
            <MenuItem Header="{local:Translate contextmenu.about}"
                      Command="{Binding ShowAboutCommand}"/>
            <Separator/>
            <MenuItem Header="{local:Translate contextmenu.exit}"
                      Command="{Binding ExitCommand}"/>
        </ContextMenu>
    </tb:TaskbarIcon.ContextMenu>
</tb:TaskbarIcon>
```

3. Create `SystemTrayViewModel.cs` with ICommand implementations
4. Update `MainForm.cs` to conditionally create WPF or WinForms tray icon

**Testing**:
- All menu items work correctly
- Icons display properly
- Keyboard shortcuts work
- Multi-monitor scenarios
- High DPI displays

### Phase 3: Settings Dialog (Week 8-12)

**Why Third**:
- ✅ Most complex dialog - good stress test
- ✅ High user impact
- ✅ Currently has many layout issues with translations

**Challenges**:
- Large, complex form with multiple tabs
- Many controls and validation logic
- Plugin settings integration

**Implementation Strategy**:

1. **Analyze current structure**:
   - `SettingsForm.cs` has ~861 lines
   - Uses tab control with 6 tabs:
     - General
     - Capture
     - Output
     - Printer
     - Plugins
     - Expert

2. **Create modular WPF structure**:
```
Views/
  Settings/
    SettingsWindow.xaml          (main window with tab control)
    GeneralTab.xaml               (general settings)
    CaptureTab.xaml               (capture settings)
    OutputTab.xaml                (output settings)
    PrinterTab.xaml               (printer settings)
    PluginsTab.xaml               (plugin settings)
    ExpertTab.xaml                (expert settings)
ViewModels/
  Settings/
    SettingsViewModel.cs          (main VM, coordinates tabs)
    GeneralTabViewModel.cs
    CaptureTabViewModel.cs
    OutputTabViewModel.cs
    PrinterTabViewModel.cs
    PluginsTabViewModel.cs
    ExpertTabViewModel.cs
```

3. **Use WPF data binding**:
```xaml
<!-- Example: General Tab -->
<UserControl x:Class="Greenshot.UI.Modern.Views.Settings.GeneralTab">
    <StackPanel Margin="10">
        <!-- Language Selection -->
        <Label Content="{local:Translate settings.language}"/>
        <ComboBox ItemsSource="{Binding AvailableLanguages}"
                  SelectedItem="{Binding SelectedLanguage}"
                  DisplayMemberPath="Description"/>
        
        <!-- Startup -->
        <CheckBox Content="{local:Translate settings.autostartshortcut}"
                  IsChecked="{Binding StartupEnabled}"/>
        
        <!-- ... more controls ... -->
    </StackPanel>
</UserControl>
```

4. **Validation with IDataErrorInfo**:
```csharp
public class CaptureTabViewModel : ViewModelBase, IDataErrorInfo
{
    private int _captureDelay;
    
    public int CaptureDelay
    {
        get => _captureDelay;
        set
        {
            if (_captureDelay != value)
            {
                _captureDelay = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string this[string columnName]
    {
        get
        {
            if (columnName == nameof(CaptureDelay))
            {
                if (CaptureDelay < 0 || CaptureDelay > 5000)
                    return "Delay must be between 0 and 5000ms";
            }
            return null;
        }
    }
}
```

**Testing**:
- All settings save/load correctly
- Validation works
- Plugin settings integration
- Multi-language testing
- DPI scaling
- Tab navigation and keyboard shortcuts

### Phase 4: Language Selection Dialog (Week 13)

**Why Fourth**:
- ✅ Simple dialog
- ✅ Quick win after complex settings dialog
- ✅ Tests combo box/list box patterns

**Implementation**:
- Single window with language list
- Filter/search capability
- Preview of selected language

### Phase 5: Capture-Related Dialogs (Week 14-15)

**Dialogs to Migrate**:
- `CaptureForm.cs` - Capture preview/selection
- `PrintOptionsDialog.cs` - Print settings

**Implementation**:
- Follow established patterns from previous phases
- Focus on maintaining existing functionality
- Improve layouts for better scaling

### Phase 6: Plugin Settings Forms (Week 16-18)

**Scope**:
- Migrate plugin settings dialogs to WPF
- Each plugin has 1-2 settings forms
- ~10 plugins to update

**Strategy**:
- Create base plugin settings view/viewmodel
- Each plugin extends base classes
- Maintain plugin API compatibility

**Note**: Some plugins like Confluence already use WPF - document as reference!

### Phase 7: Image Editor (Week 19-25) - COMPLEX

**Why Last**:
- ❌ Most complex component (2045 lines in `ImageEditorForm.cs`)
- ❌ Custom drawing code
- ❌ Many child dialogs (color picker, resize, effects)
- ❌ Performance-critical
- ❌ Highest risk

**Strategy**:
- Evaluate if full migration is necessary
- Consider hybrid approach (WPF shell, WinForms editor control)
- Potentially defer to future major version
- May use Windows Forms for editor, WPF for toolbars/chrome

**Sub-dialogs to Consider**:
- `ColorDialog.cs` - Custom color picker
- `ResizeSettingsForm.cs` - Resize options
- `DropShadowSettingsForm.cs` - Effect settings
- `TornEdgeSettingsForm.cs` - Effect settings

### Phase 8: Final Cutover (Week 26+)

**Tasks**:
1. Comprehensive testing of all migrated components
2. Update documentation
3. Create migration guide for contributors
4. **Change default**: `UseModernUI=true` by default
5. Announce in release notes
6. Keep old UI available for one more release (safety net)
7. Plan to remove old UI in subsequent release

**Testing Checklist**:
- [ ] All UI components work in modern mode
- [ ] All translations display correctly
- [ ] DPI scaling works at 100%, 125%, 150%, 200%, 300%
- [ ] Keyboard navigation and shortcuts work
- [ ] Screen reader accessibility tested
- [ ] Multi-monitor scenarios
- [ ] Theme consistency
- [ ] Performance benchmarks (startup time, memory usage)
- [ ] Plugin compatibility
- [ ] Upgrade path from old config to new config

## Configuration Strategy

### greenshot.ini Configuration

Add new configuration section and properties:

```ini
[Core]
; Enable modern WPF UI (false = classic Windows Forms UI)
; During migration phases 1-7, this defaults to false
; After Phase 8, this defaults to true
; Old UI will be removed in a future release
UseModernUI=false

; UI Theme for modern UI (light, dark, auto)
; Only applies when UseModernUI=true
UITheme=auto

; Translation format (xml, json)
; xml = legacy format (deprecated)
; json = new format (recommended)
; During migration, both are supported
TranslationFormat=json
```

### Implementation in CoreConfiguration.cs

```csharp
[IniSection("Core", Description = "Greenshot core configuration")]
public class CoreConfiguration : IniSection
{
    // ... existing properties ...
    
    [IniProperty("UseModernUI", 
        Description = "Enable modern WPF UI. false = Windows Forms (classic), true = WPF (modern)",
        DefaultValue = "false")]  // Will change to "true" in Phase 8
    public bool UseModernUI { get; set; }
    
    [IniProperty("UITheme",
        Description = "UI theme for modern UI: light, dark, auto (follows system)",
        DefaultValue = "auto")]
    public UITheme Theme { get; set; }
    
    [IniProperty("TranslationFormat",
        Description = "Translation file format: xml (legacy) or json (modern)",
        DefaultValue = "json")]
    public TranslationFormat TranslationFormat { get; set; }
}

public enum UITheme
{
    Light,
    Dark,
    Auto
}

public enum TranslationFormat
{
    Xml,
    Json
}
```

### UI Mode Selection Logic

**Location**: `src/Greenshot.UI.Modern/Core/ModernUIManager.cs`

```csharp
public static class ModernUIManager
{
    private static CoreConfiguration _config;
    
    public static void Initialize()
    {
        _config = IniConfig.GetIniSection<CoreConfiguration>();
    }
    
    public static bool IsModernUIEnabled => _config?.UseModernUI ?? false;
    
    public static void ShowAboutDialog()
    {
        if (IsModernUIEnabled)
        {
            var dialog = new Greenshot.UI.Modern.Views.AboutView();
            dialog.ShowDialog();
        }
        else
        {
            var dialog = new Greenshot.Forms.AboutForm();
            dialog.ShowDialog();
        }
    }
    
    public static void ShowSettingsDialog()
    {
        if (IsModernUIEnabled)
        {
            var dialog = new Greenshot.UI.Modern.Views.Settings.SettingsWindow();
            dialog.ShowDialog();
        }
        else
        {
            var dialog = new Greenshot.Forms.SettingsForm();
            dialog.ShowDialog();
        }
    }
    
    // ... other dialog factory methods ...
}
```

### Migration Path for Users

**Phase 1-7 (Development/Testing)**:
```ini
; Default for all users - old UI
UseModernUI=false

; Power users / testers can opt-in
UseModernUI=true
```

**Phase 8 (Soft Launch)**:
```ini
; New installs default to modern UI
UseModernUI=true

; Existing users keep their setting (false if upgrading)
; They can manually change to true when ready
```

**Future Release (Complete Migration)**:
```ini
; Remove UseModernUI flag entirely
; Remove all WinForms UI code
; WPF is the only UI
```

## Translation Platform Integration

### Recommended Platform: **Crowdin** (or Weblate)

**Why Crowdin**:
- ✅ Free for open source projects
- ✅ Excellent JSON support
- ✅ Machine translation integration (DeepL, Google Translate)
- ✅ Translation memory
- ✅ Context screenshots
- ✅ GitHub integration (automatic PR creation)
- ✅ Proofreading workflow
- ✅ Used by many open source projects

**Setup Process**:
1. Create Crowdin project for Greenshot
2. Upload `en-US.json` as source language
3. Configure 25+ target languages
4. Invite community translators
5. Set up GitHub integration for automated sync
6. Configure machine translation pre-fill (DeepL recommended)

**Workflow**:
```
Developer adds new string → en-US.json → Git commit → 
Crowdin auto-sync → Translators notified → 
Translations completed → Crowdin creates PR → 
Review and merge → Translations in next build
```

### File Structure for Translation Platform

```
src/Greenshot.UI.Modern/Resources/Translations/
  en-US.json          (source language)
  de-DE.json          (auto-synced from Crowdin)
  fr-FR.json
  es-ES.json
  ... (25+ languages)
  
  _metadata/
    context-screenshots/    (UI screenshots for translators)
    translation-notes.md    (context for translators)
```

### Migration from XML to JSON

**Tool**: `Tools/XmlToJsonTranslation/Program.cs`

```csharp
// Converts language-en-US.xml → en-US.json
// Preserves all translations
// Handles special characters
// Validates structure

public class XmlToJsonConverter
{
    public void ConvertAll(string sourcePath, string targetPath)
    {
        // For each language-*.xml file
        // Parse XML structure
        // Convert to JSON hierarchy
        // Preserve formatting
        // Write to output
    }
}
```

**Usage**:
```bash
cd Tools/XmlToJsonTranslation
dotnet run -- --source ../../src/Greenshot/Languages --target ../../src/Greenshot.UI.Modern/Resources/Translations
```

## Benefits of Migration

### For Users

1. **Better DPI Scaling**: Text and UI elements scale properly on high-DPI displays
2. **Modern Appearance**: Updated look and feel, more professional
3. **Better Translations**: More room for longer translations, no more cut-off text
4. **Accessibility**: Better screen reader support, keyboard navigation
5. **Theming**: Light and dark theme support
6. **Smoother Animations**: WPF's hardware-accelerated rendering

### For Developers

1. **Easier Maintenance**: XAML separation of UI from logic
2. **Better Tooling**: Visual Studio XAML designer, XAML Hot Reload
3. **Modern Patterns**: MVVM, data binding reduces boilerplate
4. **Type Safety**: Compile-time checking of bindings (with compiled bindings)
5. **Testability**: ViewModels easier to unit test than WinForms
6. **Reusability**: Styles, templates, custom controls

### For Translators

1. **Human-Readable Format**: JSON instead of XML
2. **Platform Support**: Crowdin/Weblate integration
3. **Machine Translation**: Pre-fill with DeepL/Google Translate
4. **Context**: Screenshots and notes for each string
5. **Translation Memory**: Reuse of similar translations
6. **Easier Contributions**: Lower barrier to entry

## Risks and Mitigations

### Risk 1: Breaking Existing Functionality

**Mitigation**:
- ✅ Gradual migration, one component at a time
- ✅ Both UIs run side-by-side during migration
- ✅ Comprehensive testing checklist for each phase
- ✅ Old UI remains available as fallback
- ✅ Config flag allows instant switch back

### Risk 2: Performance Regression

**Mitigation**:
- ✅ Benchmark before and after each phase
- ✅ WPF is hardware-accelerated (often faster than WinForms)
- ✅ Performance testing on low-end hardware
- ✅ Monitor memory usage

### Risk 3: Learning Curve for Team

**Mitigation**:
- ✅ Confluence plugin already uses WPF (reference code)
- ✅ Extensive documentation in this concept doc
- ✅ Simple pilot (About dialog) to learn pattern
- ✅ Code reviews to share knowledge

### Risk 4: Translation Quality During Migration

**Mitigation**:
- ✅ Automated conversion from XML to JSON preserves all translations
- ✅ Both formats supported during migration
- ✅ Validate conversion with diff tools
- ✅ Community review before cutover

### Risk 5: Plugin Compatibility

**Mitigation**:
- ✅ Plugin API remains unchanged
- ✅ Plugins can use either WinForms or WPF
- ✅ Confluence plugin proves WPF plugins work
- ✅ Gradual plugin migration, not forced

### Risk 6: Cross-Platform Expectations

**Mitigation**:
- ✅ Clear communication: This is WPF, Windows-only
- ✅ Future migration to Avalonia is possible (similar XAML)
- ✅ Focus on solving current problems first
- ✅ Document cross-platform as future consideration

## Timeline Summary

| Phase | Duration | Component | Complexity |
|-------|----------|-----------|------------|
| 0 | 2 weeks | Infrastructure Setup | Medium |
| 1 | 2 weeks | About Dialog | Low |
| 2 | 3 weeks | System Tray Context Menu | Medium |
| 3 | 5 weeks | Settings Dialog | High |
| 4 | 1 week | Language Selection Dialog | Low |
| 5 | 2 weeks | Capture-Related Dialogs | Medium |
| 6 | 3 weeks | Plugin Settings Forms | Medium |
| 7 | 7 weeks | Image Editor | Very High |
| 8 | 2+ weeks | Final Cutover & Testing | High |
| **Total** | **27+ weeks** | **Complete Migration** | **~6-7 months** |

**Note**: Timeline assumes one developer working part-time. Can be accelerated with multiple contributors.

## Success Metrics

### Quantitative Metrics

1. **DPI Scaling**: UI renders correctly at 100%, 125%, 150%, 200%, 300% without clipping
2. **Translation Coverage**: All 25+ languages migrated successfully
3. **Performance**: Startup time < 2 seconds (same or better than WinForms)
4. **Memory**: Memory usage < 100MB baseline (same or better)
5. **Bug Reports**: < 10 critical bugs reported in first month after launch

### Qualitative Metrics

1. **User Feedback**: Positive sentiment on modern look and feel
2. **Contributor Feedback**: Developers find WPF easier to work with
3. **Translator Feedback**: JSON format easier to work with than XML
4. **Accessibility**: Screen reader users report improved experience

## Conclusion

This migration plan provides a **low-risk, incremental path** to modernize Greenshot's UI while maintaining full backward compatibility. By leveraging WPF (already proven in the Confluence plugin), we can deliver significant improvements to users without requiring a .NET version upgrade or massive architectural changes.

The gradual, opt-in approach ensures that:
- ✅ Users are not disrupted during migration
- ✅ Each component can be thoroughly tested before release
- ✅ Team can learn WPF incrementally
- ✅ Risk is minimized at each step
- ✅ Value is delivered early (About dialog, context menu)

**Recommendation**: Approve and begin with Phase 0 (Infrastructure Setup) and Phase 1 (About Dialog) as a pilot to validate the approach.

---

**Document Version**: 1.0  
**Date**: January 2026  
**Author**: GitHub Copilot  
**Status**: Proposal - Awaiting Review
