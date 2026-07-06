# Changelog

Todas as mudanças notáveis neste projeto serão documentadas aqui.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Unreleased]

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
