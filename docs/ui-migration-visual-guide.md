# Greenshot UI Migration - Visual Architecture

## Current Architecture (Windows Forms)

```
┌─────────────────────────────────────────────────────────────────┐
│                      Greenshot.exe                              │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                 MainForm (WinForms)                       │  │
│  │  • System Tray (NotifyIcon)                              │  │
│  │  • Context Menu (ContextMenuStrip)                       │  │
│  │  • Global Hotkeys                                        │  │
│  │  • Capture Coordination                                  │  │
│  └────────────┬──────────────────────────────────────────────┘  │
│               │                                                 │
│               ├─> AboutForm (WinForms)         389 lines       │
│               ├─> SettingsForm (WinForms)      861 lines       │
│               ├─> LanguageDialog (WinForms)    ~150 lines      │
│               ├─> BugReportForm (WinForms)     ~200 lines      │
│               ├─> CaptureForm (WinForms)       ~300 lines      │
│               ├─> PrintOptionsDialog (WinForms)                │
│               │                                                 │
│               └─> ImageEditorForm (WinForms)   2045 lines      │
│                   ├─> ColorDialog (WinForms)                   │
│                   ├─> ResizeForm (WinForms)                    │
│                   └─> EffectForms (WinForms)                   │
│                                                                 │
│  Total: ~13,785 lines across all forms                         │
└─────────────────────────────────────────────────────────────────┘

Translation: XML files (25+ languages)
├─ src/Greenshot/Languages/language-en-US.xml
├─ src/Greenshot/Languages/language-de-DE.xml
└─ ... (23+ more)

Configuration: INI files
└─ %AppData%/Greenshot/greenshot.ini
```

## Target Architecture (Modern WPF)

```
┌─────────────────────────────────────────────────────────────────┐
│                      Greenshot.exe                              │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │             Application (WPF)                            │  │
│  │  • TaskbarIcon (Hardcodet.NotifyIcon.Wpf)               │  │
│  │  • Theme Manager (Light/Dark/Auto)                       │  │
│  │  • Navigation Service                                    │  │
│  └────────────┬──────────────────────────────────────────────┘  │
│               │                                                 │
│  ┌────────────▼───────────── MVVM Pattern ──────────────────┐  │
│  │                                                           │  │
│  │  Views (XAML)          ViewModels (C#)                   │  │
│  │  ─────────────          ───────────────                  │  │
│  │  AboutView.xaml    <=>  AboutViewModel                   │  │
│  │  SettingsWindow    <=>  SettingsViewModel                │  │
│  │  LanguageView      <=>  LanguageViewModel                │  │
│  │  SystemTrayView    <=>  SystemTrayViewModel              │  │
│  │  ...                    ...                               │  │
│  │                                                           │  │
│  │  Services                                                 │  │
│  │  ────────────                                            │  │
│  │  • TranslationService (JSON-based)                       │  │
│  │  • ThemeService                                          │  │
│  │  • DialogService                                         │  │
│  │                                                           │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  Shared Business Logic (Unchanged)                             │
│  ├─ Capture Logic                                              │
│  ├─ Destinations                                               │
│  ├─ Plugins                                                    │
│  └─ Configuration                                              │
└─────────────────────────────────────────────────────────────────┘

Translation: JSON files (25+ languages) + Auto-generated RESX
├─ src/Greenshot.UI.Modern/Resources/Translations/en-US.json
├─ src/Greenshot.UI.Modern/Resources/Translations/de-DE.json
├─ ... (23+ more)
└─ Build generates: Resources.en-US.resx, Resources.de-DE.resx, ...

Configuration: INI files (backward compatible)
└─ %AppData%/Greenshot/greenshot.ini
   [Core]
   UseModernUI=true  # New flag for gradual migration
```

## Migration Strategy: Hybrid Approach (During Transition)

```
┌─────────────────────────────────────────────────────────────────┐
│                      Greenshot.exe                              │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │         ModernUIManager (Selector)                        │  │
│  │                                                           │  │
│  │  if (CoreConfiguration.UseModernUI)                      │  │
│  │      return new WPF View                                 │  │
│  │  else                                                     │  │
│  │      return new WinForms View                            │  │
│  │                                                           │  │
│  └────────┬───────────────────────┬──────────────────────────┘  │
│           │                       │                             │
│   ┌───────▼────────┐     ┌───────▼────────┐                   │
│   │   WPF Views    │     │ WinForms Views │                   │
│   │   (Modern)     │     │   (Legacy)     │                   │
│   │                │     │                │                   │
│   │ ✓ AboutView    │     │ AboutForm      │                   │
│   │ ✓ SettingsView │     │ SettingsForm   │                   │
│   │ ⧗ LanguageView │     │ LanguageDialog │                   │
│   │ ⧗ ...          │     │ ...            │                   │
│   └────────────────┘     └────────────────┘                   │
│           │                       │                             │
│           └───────────┬───────────┘                             │
│                       │                                         │
│  ┌────────────────────▼────────────────────┐                   │
│  │  Shared Business Logic                  │                   │
│  │  • Capture                              │                   │
│  │  • Destinations                         │                   │
│  │  • Plugins                              │                   │
│  │  • Configuration                        │                   │
│  └─────────────────────────────────────────┘                   │
└─────────────────────────────────────────────────────────────────┘

Legend:
  ✓ = Migrated to WPF
  ⧗ = In progress
  (blank) = Not yet started
```

## Migration Timeline Visualization

```
Phase 0: Infrastructure (Week 1-2)
┌─────────────────────────────────────────────────┐
│ • Create Greenshot.UI.Modern project            │
│ • Set up JSON translation system                │
│ • Create TranslationService                     │
│ • Add UseModernUI config flag                   │
│ • Create MVVM base classes                      │
│ • Set up build tasks (JSON → RESX)              │
└─────────────────────────────────────────────────┘
                    ↓
Phase 1: About Dialog - PILOT (Week 3-4)
┌─────────────────────────────────────────────────┐
│ AboutForm (WinForms) → AboutView (WPF)          │
│                                                 │
│ Why first: Simple, self-contained, good visual │
│ Success criteria: Both UIs work side-by-side   │
│ Learning: XAML, MVVM, translations, styling    │
└─────────────────────────────────────────────────┘
                    ↓
Phase 2: System Tray Context Menu (Week 5-7)
┌─────────────────────────────────────────────────┐
│ NotifyIcon (WinForms) → TaskbarIcon (WPF)       │
│                                                 │
│ Why second: High visibility, good user impact  │
│ Challenge: Need Hardcodet.NotifyIcon.Wpf lib   │
└─────────────────────────────────────────────────┘
                    ↓
Phase 3: Settings Dialog (Week 8-12)
┌─────────────────────────────────────────────────┐
│ SettingsForm (WinForms) → SettingsWindow (WPF) │
│                                                 │
│ Why third: Most complex, tests everything      │
│ Challenge: 6 tabs, many controls, validation   │
│ Benefit: Modular design (one VM per tab)       │
└─────────────────────────────────────────────────┘
                    ↓
Phase 4-6: Other Dialogs (Week 13-18)
┌─────────────────────────────────────────────────┐
│ • Language Selection Dialog                    │
│ • Capture Preview Dialog                       │
│ • Print Options Dialog                         │
│ • Plugin Settings Forms                        │
└─────────────────────────────────────────────────┘
                    ↓
Phase 7: Image Editor (Week 19-25) - COMPLEX
┌─────────────────────────────────────────────────┐
│ ImageEditorForm (WinForms) → ? (Hybrid?)        │
│                                                 │
│ Why last: Most complex (2045 lines)            │
│ Consider: Hybrid approach (WPF chrome + WinForms│
│           editor control for performance)       │
│ Defer?: Could postpone to future major version  │
└─────────────────────────────────────────────────┘
                    ↓
Phase 8: Final Cutover (Week 26+)
┌─────────────────────────────────────────────────┐
│ • Change default: UseModernUI=true              │
│ • Comprehensive testing                         │
│ • Update documentation                          │
│ • Plan to remove old UI in next release        │
└─────────────────────────────────────────────────┘
```

## Translation System Migration

### Current (XML-based)

```
language-en-US.xml                  Language.cs
┌────────────────────┐              ┌──────────────────┐
│ <language>         │              │ static class     │
│   <resources>      │──Parse XML──>│ Language {       │
│     <resource      │              │   GetString()    │
│      name="about_  │              │   GetFormatted() │
│      title">       │              │ }                │
│      About         │              └──────────────────┘
│      Greenshot     │                       ▲
│     </resource>    │                       │
│   </resources>     │                  Usage in code
│ </language>        │                       │
└────────────────────┘              ┌──────────────────┐
                                    │ Language.GetString│
                                    │  (LangKey.        │
                                    │   about_title)    │
                                    └──────────────────┘
```

### Target (JSON-based with XAML Support)

```
en-US.json                  TranslationService.cs
┌────────────────────┐      ┌──────────────────────┐
│ {                  │      │ class Translation-   │
│   "about": {       │─Load─>│ Service {            │
│     "title":       │      │   Translate(key)     │
│     "About         │      │   SetLanguage()      │
│     Greenshot"     │      │ }                    │
│   }                │      └──────────────────────┘
│ }                  │              │      │
└────────────────────┘              │      │
         │                          │      │
         │                          ▼      ▼
         │              ┌─────────────────────────┐
         │              │ Usage in Code & XAML    │
         │              │                         │
    Build Time          │ C#:                     │
         │              │  _service.Translate(    │
         ▼              │    "about.title")       │
┌────────────────────┐  │                         │
│ Resources.en-US.   │  │ XAML:                   │
│ resx (generated)   │  │  Text="{local:Translate │
│                    │  │    about.title}"        │
│ For WPF binding    │  │                         │
│ and compile-time   │  └─────────────────────────┘
│ checking           │
└────────────────────┘

Benefits:
• Human-readable JSON source
• Auto-generated RESX for WPF bindings
• Crowdin/Weblate integration
• Machine translation friendly
• Better version control (readable diffs)
```

## Configuration Flag Usage

```
greenshot.ini
┌─────────────────────────────┐
│ [Core]                      │
│ UseModernUI=false           │ ──┐
│                             │   │
│ # During migration:         │   │
│ # false = WinForms (safe)   │   │
│ # true = WPF (opt-in test)  │   │
└─────────────────────────────┘   │
                                  │
                Load Config       │
                                  ▼
              ┌─────────────────────────────┐
              │ ModernUIManager             │
              │                             │
              │ if (UseModernUI) {          │
              │   return new AboutView()    │
              │ } else {                    │
              │   return new AboutForm()    │
              │ }                           │
              └─────────────────────────────┘
                  │              │
           Modern │              │ Legacy
                  ▼              ▼
          ┌──────────┐    ┌─────────────┐
          │ WPF View │    │ WinForms    │
          │          │    │ View        │
          │ • Modern │    │ • Old style │
          │ • Scales │    │ • Works     │
          │ • Themes │    │ • Proven    │
          └──────────┘    └─────────────┘
```

## Risk Mitigation Strategy

```
┌──────────────────────────────────────────────────────────┐
│                    RISK MITIGATION                        │
├──────────────────────────────────────────────────────────┤
│                                                           │
│  Risk: Breaking existing functionality                   │
│  ──────────────────────────────────────                  │
│  ✓ Gradual migration (one component at a time)           │
│  ✓ Both UIs run side-by-side                            │
│  ✓ Default to old UI (UseModernUI=false)                │
│  ✓ Can switch back instantly via config                  │
│                                                           │
│  Risk: Performance regression                            │
│  ─────────────────────────────                           │
│  ✓ Benchmark before/after each phase                     │
│  ✓ WPF is hardware-accelerated (often faster)            │
│  ✓ Test on low-end hardware                             │
│                                                           │
│  Risk: Team learning curve                               │
│  ──────────────────────────                              │
│  ✓ Confluence plugin already uses WPF (reference)        │
│  ✓ Start simple (About dialog)                           │
│  ✓ Comprehensive documentation                           │
│                                                           │
│  Risk: Translation quality                               │
│  ──────────────────────────                              │
│  ✓ Automated XML → JSON conversion                       │
│  ✓ Both formats supported during migration               │
│  ✓ Validation before cutover                             │
│                                                           │
│  Risk: Plugin compatibility                              │
│  ───────────────────────────                             │
│  ✓ Plugin API unchanged                                  │
│  ✓ Plugins can use WinForms or WPF                       │
│  ✓ Gradual plugin migration                              │
│                                                           │
└──────────────────────────────────────────────────────────┘
```

## Success Metrics Dashboard

```
┌─────────────────────────────────────────────────────────┐
│              MIGRATION SUCCESS METRICS                   │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Quantitative Metrics                                   │
│  ────────────────────                                   │
│  ☐ DPI Scaling: Renders correctly at 100%-300%         │
│  ☐ Translation: All 25+ languages migrated              │
│  ☐ Performance: Startup < 2s (same or better)           │
│  ☐ Memory: Usage < 100MB baseline                       │
│  ☐ Bugs: < 10 critical in first month                   │
│                                                          │
│  Qualitative Metrics                                    │
│  ───────────────────                                    │
│  ☐ User Feedback: Positive sentiment                    │
│  ☐ Developer Feedback: WPF easier to work with          │
│  ☐ Translator Feedback: JSON easier than XML            │
│  ☐ Accessibility: Screen reader improvements            │
│                                                          │
│  Milestone Completion                                   │
│  ─────────────────────                                  │
│  ☐ Phase 0: Infrastructure .............. [ 0%]         │
│  ☐ Phase 1: About Dialog ................ [ 0%]         │
│  ☐ Phase 2: System Tray ................. [ 0%]         │
│  ☐ Phase 3: Settings .................... [ 0%]         │
│  ☐ Phase 4: Language Dialog ............. [ 0%]         │
│  ☐ Phase 5: Capture Dialogs ............. [ 0%]         │
│  ☐ Phase 6: Plugin Forms ................ [ 0%]         │
│  ☐ Phase 7: Image Editor ................ [ 0%]         │
│  ☐ Phase 8: Final Cutover ............... [ 0%]         │
│                                                          │
│  Overall Progress: [░░░░░░░░░░░░░░░░░░░░] 0%            │
└─────────────────────────────────────────────────────────┘
```

---

**Document Version**: 1.0  
**Part of**: Greenshot UI Migration Documentation  
**Date**: January 2026
