# MacroKids

.NET 10 WPF visual automation editor for kids and beginners. The app is a block-based automation builder, not a game bot.

- **UI app**: `src/MacroKids.UI/` — primary editing target
- **Node editor**: `src/MacroKids.NodeEditor/` — canvas, node cards, connections, theming
- **Domain/runtime**: `src/MacroKids.Core/` and `src/MacroKids.Runtime/`
- **Built-in blocks**: `src/MacroKids.Nodes/`
- **Build**: `dotnet build` from repo root
- **Tests**: `dotnet test`

## Code rules

1. **MVVM first** — put state and commands in view models; keep code-behind only for view-only actions like fullscreen or focus.
2. **No silent failures** — if a command cannot run, surface the reason in the status bar or throw a real exception.
3. **Theme changes must be global** — theme dictionaries live in `App.xaml` and are swapped there.
4. **Localization is data-driven** — use `src/MacroKids.UI/Translations/*.json`; do not hardcode UI text when a translation key exists.
5. **Node colors come from metadata** — categories must map to stable colors for both sidebar and canvas nodes.
6. **Canvas state is authoritative** — selected page, node selection, pins, zoom, pan, and connections must stay synchronized with the live document.
7. **Undo/redo must rebuild UI state** — commands should mutate the live flow document, and the canvas view model must resync collections afterward.
8. **Pages are real sessions** — tabs represent separate canvas documents and should support create/select/close.
9. **No placeholder buttons** — if a toolbar action is shown, it should either work or be clearly marked as unfinished.
10. **Bump version deliberately** — update the app title and README badge when shipping visible progress.

## Working guidelines

- Prefer surgical edits over broad refactors unless a bug spans multiple layers.
- Reuse existing node registry, command history, and localization patterns.
- Keep connection rendering, selection, and inspector bindings in sync with the canvas VM.
- Before finishing, run a build and fix any XAML or binding regressions introduced by the change.

