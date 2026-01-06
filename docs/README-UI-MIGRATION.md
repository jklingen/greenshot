# UI Migration Documentation

This folder contains comprehensive documentation for migrating Greenshot's UI from Windows Forms to modern WPF.

## Documents

### 1. [UI Migration Concept](ui-migration-concept.md)
**Primary planning document** - Read this first!

- Executive summary of the migration strategy
- Current state analysis (WinForms, XML translations, INI config)
- Technology evaluation (WPF, Avalonia, MAUI, Blazor)
- **Recommendation: WPF** (already proven in Confluence plugin)
- Translation system modernization (XML → JSON)
- Step-by-step migration plan (8 phases, ~27 weeks)
- Configuration strategy (gradual opt-in via `UseModernUI` flag)
- Risk mitigation strategies
- Success metrics

**Key Decisions**:
- ✅ Use **WPF** (Windows Presentation Foundation) for modern UI
- ✅ Use **JSON** for human-readable translations
- ✅ **Gradual migration** with both UIs running side-by-side
- ✅ **Opt-in during development** via greenshot.ini config flag
- ✅ Start with simple components (About dialog) then progress to complex (Settings, Editor)

### 2. [UI Migration Technical Reference](ui-migration-technical-reference.md)
**Implementation guide** - Reference during development

- Architecture diagrams (current, hybrid, target)
- JSON translation system implementation
- MVVM pattern reference code
- Translation service with markup extensions
- WPF conversion examples (before/after)
- Build system integration (MSBuild tasks)
- Testing strategy (unit tests, UI automation)
- Complete file structure

**Includes Ready-to-Use Code**:
- `TranslationService.cs` - JSON-based translation loader
- `TranslateExtension.cs` - XAML markup extension `{local:Translate key}`
- `ViewModelBase.cs` - Base class for MVVM
- `RelayCommand.cs` - ICommand implementation
- Example ViewModels and Views
- MSBuild task for JSON → RESX conversion

## Migration Phases Overview

| Phase | Duration | Component | Status |
|-------|----------|-----------|--------|
| **0** | 2 weeks | Infrastructure Setup | 📋 Planned |
| **1** | 2 weeks | About Dialog (Pilot) | 📋 Planned |
| **2** | 3 weeks | System Tray Context Menu | 📋 Planned |
| **3** | 5 weeks | Settings Dialog | 📋 Planned |
| **4** | 1 week | Language Selection Dialog | 📋 Planned |
| **5** | 2 weeks | Capture-Related Dialogs | 📋 Planned |
| **6** | 3 weeks | Plugin Settings Forms | 📋 Planned |
| **7** | 7 weeks | Image Editor | 📋 Planned |
| **8** | 2+ weeks | Final Cutover & Testing | 📋 Planned |

**Total**: ~27 weeks (6-7 months) with one part-time developer

## Quick Start for Contributors

### For Reviewers
1. Read the [concept document](ui-migration-concept.md) sections:
   - Executive Summary
   - Technology Recommendations (why WPF)
   - Step-by-Step Migration Plan
   - Configuration Strategy

### For Developers (when implementation starts)
1. Review [technical reference](ui-migration-technical-reference.md)
2. Set up development environment:
   - Visual Studio 2019+ with WPF workload
   - Install .NET Framework 4.7.2 SDK
3. Study the Confluence plugin (`src/Greenshot.Plugin.Confluence/Forms/*.xaml`)
   - This already uses WPF successfully!
4. Start with Phase 0 infrastructure setup

### For Translators (future)
- Translations will move from XML to JSON format
- JSON files will be on Crowdin for easy contribution
- More human-readable, better tooling support

## Key Benefits

**For Users**:
- ✅ Better DPI scaling (no more blurry text on high-DPI displays)
- ✅ Modern look and feel
- ✅ Better support for long translations (no more cut-off text)
- ✅ Dark/light theme support
- ✅ Better accessibility (screen readers)

**For Developers**:
- ✅ Easier UI maintenance (XAML separation)
- ✅ Better tooling (Visual Studio designer, XAML Hot Reload)
- ✅ Testable ViewModels (MVVM pattern)
- ✅ Reusable styles and components
- ✅ Already proven in codebase (Confluence plugin)

**For Translators**:
- ✅ Human-readable JSON instead of XML
- ✅ Crowdin/Weblate integration
- ✅ Machine translation pre-fill
- ✅ Context screenshots for each string

## Timeline

- **Phase 0**: Infrastructure (Week 1-2)
- **Phase 1**: About Dialog Pilot (Week 3-4) ← First visible results
- **Phase 2**: System Tray (Week 5-7) ← High user impact
- **Phase 3**: Settings (Week 8-12) ← Most complex dialog
- **Phases 4-6**: Other dialogs (Week 13-18)
- **Phase 7**: Image Editor (Week 19-25) ← Most complex, consider hybrid approach
- **Phase 8**: Final cutover (Week 26+)

## Important Learnings from Current Codebase

### Already Using WPF ✓
The **Confluence plugin** (`src/Greenshot.Plugin.Confluence/Forms/`) already uses WPF successfully:
- `ConfluenceConfigurationForm.xaml` - Settings dialog
- Custom translation markup extension: `{support:Translate key}`
- Proof that WPF works in Greenshot architecture

### Current Translation System
- XML files: `src/Greenshot/Languages/language-{IETF}.xml`
- 25+ languages supported
- Custom loader: `src/Greenshot.Base/Core/Language.cs`
- Usage: `Language.GetString(LangKey.about_title)`

### Current Configuration System
- INI files with custom parser: `src/Greenshot.Base/IniFile/`
- Main config: `CoreConfiguration.cs` with `[IniProperty]` attributes
- File: `greenshot.ini` in user profile

## Configuration Flag

During migration, users can opt-in to modern UI:

```ini
[Core]
# Enable modern WPF UI (default: false during migration)
# Set to true to test new UI components as they become available
UseModernUI=false
```

## Questions?

- **Technical questions**: See [technical reference](ui-migration-technical-reference.md)
- **Strategy questions**: See [concept document](ui-migration-concept.md)
- **Implementation timeline**: See Phase descriptions in concept document

## Status

📋 **Current Status**: Concept and planning phase  
🎯 **Next Step**: Review and approval of concept documents  
📅 **Updated**: January 2026

---

**Note**: This is a living document. As the migration progresses, update the phase statuses and add lessons learned.
