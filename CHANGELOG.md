# Changelog

Todas as mudanças notáveis neste projeto serão documentadas aqui.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Unreleased]

## [0.1.5-dev] - 2026-07-06

### Added
- Centralized `NativeInput` helper with desktop/game fallbacks for mouse and keyboard
- `KeyboardMapper.GetKeyName` for full virtual-key coverage in the macro recorder
- `PinValueReader` for robust pin value parsing (including multi-monitor negative coordinates)

### Fixed
- Mouse clicks on multi-monitor setups (negative X coordinates no longer ignored)
- Held letter keys (e.g. W) recorded as `hold_key` instead of `press_key` / `type_text`
- Recorder import uses inline node delay instead of chaining `wait` blocks between clicks
- App starts with a single empty **New Automation** tab (no sample blocks)
- Project tab title updates when saving a `.mkproject` file
- Window focus uses `AttachThreadInput` for reliable foreground switching

### Improved
- Macro recorder monitors the full keyboard range (0x08–0xFE)
- JsonElement pin values normalized when loading projects

## [0.1.4-dev] - 2026-07-06

### Added
- `NodeCanvas`: dimensões de 50000×50000 para simular canvas infinito
- `ConnectionLine`: overlay transparente de 14px para facilitar clique/seleção nas conexões
- `NodeViewModel`: pin global de **Delay (ms)** em todos os nós (inline delay)
- `FlowExecutor`: executa o inline delay do nó antes do `StepDelayMs`
- Botão **Load Project** (📂) na toolbar do canvas
- Chave `TooltipLoadProject` nas traduções (pt-BR, en, es)

### Improved
- `TypeTextNode`: migrado de `keybd_event` para `SendInput` com Unicode (suporta acentos e caracteres especiais)
- `IfConditionExecutor`: avaliação real de condições (`var > 0`, `==`, `!=`, etc.)
- `RepeatLoopExecutor`: execução real do loop com contagem configurável
- `ForEachExecutor`: iteração real sobre lista de itens
- `MouseScrollExecutor`: scroll real via `mouse_event` (cima/baixo, quantidade configurável)
- `DoubleClickExecutor`: duplo clique real via `mouse_event`
- `HoldKeyExecutor`: segurar tecla por X ms via `keybd_event`
- `ComboKeyExecutor`: combinações de teclas reais (ex: Ctrl+C)

## [0.1.3-dev] - 2026-07-05

### Changed
- Reworked build output to `bin\Debug\net10.0\0.1.3-dev\`
- Switched the app logo to the new light/dark theme assets
- Removed the visible square background from the theme toggle

## [0.1.2-dev] - 2026-07-05

### Added
- Restored the global theme switch, language flags, page tabs, canvas inspector, and block editing flows
- Added a repository-specific `CLAUDE.md` for MacroKids
- Bumped the project version to `0.1.2-dev`
- Logger global configurado com NLog gravando em `logs/crash.log`
- Captura global de exceções da UI e AppDomain com janelas de diálogo críticas
- Tela principal (`MainWindow`) estilizada com 3 painéis (Catálogo, Editor, Propriedades)
- `MainWindowViewModel` com nós demo populados
- Correção de recursos dinâmicos em `NodeEditorControl.xaml` mesclando temas para evitar `XamlParseException`
- Atualização da arquitetura e roadmap nos documentos do projeto


> MVP inicial — editor visual + execução básica de fluxos
