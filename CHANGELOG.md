# Changelog

Todas as mudanças notáveis neste projeto serão documentadas aqui.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Unreleased]

### Added
- Estrutura inicial da Solution com 7 projetos: Core, NodeEditor, Nodes, Runtime, Recorder, Plugins, UI
- Modelos de domínio: `FlowNode`, `FlowConnection`, `FlowDocument`, `NodePin`, `NodeMetadata`
- Interface `IEventBus` com eventos de execução (`NodeStartedEvent`, `NodeCompletedEvent`, etc.)
- Interface `IExecutionContext` com acesso a variáveis, log e event bus
- `NodeCategory` enum com todas as categorias de blocos
- `Directory.Build.props` com versionamento centralizado e analyzers
- `.editorconfig` com padrão Allman style para C#
- `.gitignore` para projetos .NET/WPF
- GitHub Actions CI (`build-and-test.yml`)

---

## [0.1.0] - Em desenvolvimento

> MVP inicial — editor visual + execução básica de fluxos
