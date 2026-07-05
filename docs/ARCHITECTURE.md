# MacroKids — Documentação Técnica

> Versão do documento: 0.1 | Atualizado: Julho 2026

---

## Índice

1. [Visão Geral](#1-visão-geral)
2. [Arquitetura](#2-arquitetura)
3. [Projetos & Responsabilidades](#3-projetos--responsabilidades)
4. [Conceitos Fundamentais](#4-conceitos-fundamentais)
5. [MacroKids.Core — Domínio](#5-macrokidscore--domínio)
6. [Event Bus](#6-event-bus)
7. [Sistema de Nodes](#7-sistema-de-nodes)
8. [Engine de Execução](#8-engine-de-execução)
9. [Formato de Arquivo .mkproject](#9-formato-de-arquivo-mkproject)
10. [Sistema de Plugins](#10-sistema-de-plugins)
11. [Node Editor (WPF)](#11-node-editor-wpf)
12. [Padrões de Código](#12-padrões-de-código)
13. [Testes](#13-testes)
14. [CI/CD & Qualidade](#14-cicd--qualidade)
15. [Versionamento & Release](#15-versionamento--release)
16. [Roadmap Mobile](#16-roadmap-mobile)
17. [Glossário](#17-glossário)

---

## 1. Visão Geral

**MacroKids** é uma plataforma educacional de automação visual para Windows. A criança monta fluxos de automação conectando blocos coloridos — sem escrever código — mas aprendendo os conceitos reais de programação: eventos, condições, loops e variáveis.

### Público-alvo
- **Primário:** Crianças a partir de 8 anos
- **Secundário:** Educadores, pais, iniciantes em programação e criadores

### Inspirações de UX
- **Scratch** — blocos coloridos, linguagem visual
- **Unreal Engine Blueprint** — canvas de nós com conexões
- **Node-RED** — simplicidade de fluxo
- **Figma** — qualidade de editor visual

---

## 2. Arquitetura

### Princípio Central
> **A UI nunca conhece tipos concretos de nós.**

A camada `UI` não importa `PressKeyNode`, `MoveMouseNode` etc. Ela apenas lê `NodeMetadata` e renderiza qualquer bloco automaticamente. Isso permite:
- Adicionar 100 novos blocos sem alterar o editor
- Terceiros publicarem plugins com novos blocos
- Blocos mobile no futuro (MAUI) reusando o mesmo `Core`

### Diagrama de Dependências

```
┌──────────────────────────────────────────────────────────┐
│                   MacroKids.Core                          │
│         net10.0 — C# puro, sem Win32 ou WPF              │
│   Models · Interfaces · Events · Commands · Serialização  │
└──────┬──────────────┬────────────────┬───────────────────┘
       │              │                │
  ┌────▼─────┐  ┌─────▼──────┐  ┌─────▼──────────────┐
  │ .Plugins │  │ .NodeEditor │  │      .Runtime       │
  │ (net10.0)│  │  (WPF ctrl) │  │  (net10.0-windows)  │
  └────┬─────┘  └─────┬───────┘  └─────┬───────────────┘
       │              │                │
       └──────────────▼────────────────▼
                  ┌──────────┐
                  │  .Nodes  │
                  │(win-only)│
                  └────┬─────┘
                       │
                 ┌─────▼──────┐
                 │    .UI     │
                 │ (WPF App)  │
                 └────────────┘
```

### Separação de responsabilidades

| Camada | O que sabe | O que NÃO sabe |
|---|---|---|
| `Core` | Modelos, contratos, eventos | WPF, Win32, nodes concretos |
| `Runtime` | Como executar fluxos, Windows API | WPF, layout visual |
| `NodeEditor` | Como renderizar canvas WPF | Lógica de execução |
| `Nodes` | O que cada bloco faz | Como o canvas funciona |
| `UI` | Como montar a janela | Detalhes de execução |

---

## 3. Projetos & Responsabilidades

### `MacroKids.Core` — `net10.0`
O coração do sistema. **Zero dependências de plataforma.** Pode rodar em Windows, Linux, macOS, MAUI.

```
Models/
  FlowNode.cs          → instância de um nó no canvas
  FlowConnection.cs    → wire entre dois pins
  FlowDocument.cs      → documento completo (grafo + viewport + versão)
  NodePin.cs           → porta de entrada ou saída de um nó
  NodeMetadata.cs      → descrição completa de um tipo de nó
  NodeCategory.cs      → enum de categorias (Keyboard, Mouse, etc.)

Interfaces/
  IEventBus.cs         → publish/subscribe de eventos de execução
  IExecutionContext.cs → contexto passado a cada nó durante execução
  INodeExecutor.cs     → contrato de execução de um nó
  INodeRegistry.cs     → registro de tipos de nó disponíveis
  INodePlugin.cs       → contrato para plugins externos

Events/
  ExecutionEvents.cs   → todos os eventos da engine (records)

Commands/
  IEditorCommand.cs    → base com Execute() / Undo()
  CommandHistory.cs    → pilha de undo/redo
  CreateNodeCommand.cs
  DeleteNodeCommand.cs
  MoveNodeCommand.cs
  ConnectPinsCommand.cs

Serialization/
  FlowSerializer.cs    → JSON ↔ FlowDocument
  ProjectPackager.cs   → ZIP ↔ .mkproject
```

### `MacroKids.Runtime` — `net10.0-windows`
Engine de execução. Usa Windows APIs para simular input.

```
EventBus.cs          → implementação de IEventBus
ExecutionContext.cs   → implementação de IExecutionContext
FlowExecutor.cs      → percorre grafo, executa nós em ordem
NodeRegistry.cs      → TypeId → (NodeMetadata, INodeExecutor)
InputSimulator.cs    → wrapper sobre SendInput() / mouse_event()
```

### `MacroKids.NodeEditor` — `net10.0-windows` + WPF
Controle WPF isolado. Pode ser usado em qualquer projeto WPF.

```
NodeCanvas.cs        → canvas infinito com pan/zoom/grid
NodeView.xaml        → visual de um bloco (via NodeMetadata)
PinView.xaml         → ponto de conexão
ConnectionView.cs    → curva Bézier animada
NodeEditorViewModel  → lógica de seleção, drag, conexões
```

### `MacroKids.Nodes` — `net10.0-windows`
Biblioteca de blocos nativos. Cada bloco = `INodeExecutor` + `NodeMetadata`.

### `MacroKids.Recorder` — `net10.0-windows`
Grava ações do usuário via hooks globais Win32 e converte em blocos.

### `MacroKids.Plugins` — `net10.0`
SDK público para plugins de terceiros. Referencia apenas `Core`.

### `MacroKids.UI` — `net10.0-windows` + WPF
Aplicação principal. Orquestra tudo: layout, temas, projetos, execução.

---

## 4. Conceitos Fundamentais

### FlowNode vs NodeMetadata vs INodeExecutor

```
NodeMetadata          FlowNode               INodeExecutor
─────────────         ──────────────         ─────────────────
"O tipo"              "A instância"          "A lógica"

TypeId: "key.press"   InstanceId: Guid()     ExecuteAsync(ctx)
Name: "Press Key"     TypeId: "key.press"
Category: Keyboard    X: 200, Y: 150
Pins: [key, delay]    PinValues: {key: "A"}
```

**Analogia:** `NodeMetadata` é a *classe*, `FlowNode` é o *objeto*, `INodeExecutor` é o *método*.

### Fluxo de dados entre nós

```
[EventNode] ──output:trigger──► [WaitNode] ──output:completed──► [PressKeyNode]
                                  input:ms = 500                   input:key = "A"
```

Os valores fluem pelos outputs para os inputs conectados. O `FlowExecutor` resolve os valores antes de chamar `ExecuteAsync()`.

---

## 5. MacroKids.Core — Domínio

### NodeMetadata
```csharp
// Exemplo de metadado de um nó — definido no projeto Nodes, lido pela UI
var metadata = new NodeMetadata
{
    TypeId      = "keyboard.press_key",
    Name        = "Press Key",
    Description = "Pressiona uma tecla do teclado",
    Category    = NodeCategory.Keyboard,
    IconKey     = "keyboard",
    NodeVersion = new Version(1, 0, 0),
    Pins =
    [
        new NodePin { Id = "key",   Label = "Tecla",  Direction = PinDirection.Input,
                      DataType = typeof(string), DefaultValue = "A", IsRequired = true },
        new NodePin { Id = "hold",  Label = "Segurar (ms)", Direction = PinDirection.Input,
                      DataType = typeof(int),    DefaultValue = 0 },
        new NodePin { Id = "done",  Label = "Concluído", Direction = PinDirection.Output,
                      DataType = typeof(bool) },
    ]
};
```

### FlowDocument — Versionamento
```csharp
var doc = new FlowDocument
{
    Id                   = Guid.NewGuid(),
    Name                 = "Meu Primeiro Fluxo",
    CreatedAt            = DateTime.UtcNow,
    UpdatedAt            = DateTime.UtcNow,
    SchemaVersion        = 1,          // versão do formato JSON
    EngineVersion        = "0.1.0",    // versão que criou o doc
    MinimumEngineVersion = "0.1.0",    // versão mínima para abrir
    Nodes                = [...],
    Connections          = [...]
};
```

---

## 6. Event Bus

### Como usar — publicar
```csharp
// No FlowExecutor, ao iniciar um nó:
_context.EventBus.Publish(new NodeStartedEvent(
    FlowId:         _document.Id,
    NodeInstanceId: node.InstanceId,
    TypeId:         node.TypeId
));
```

### Como usar — subscrever (na UI)
```csharp
// No ViewModel, subscreve para iluminar o nó durante execução:
_subscriptions.Add(
    _eventBus.Subscribe<NodeStartedEvent>(e =>
    {
        // Atualiza o visual do nó no canvas
        _canvasVm.SetNodeExecuting(e.NodeInstanceId, true);
    })
);
```

### Eventos disponíveis
| Evento | Quando é publicado |
|---|---|
| `ExecutionStartedEvent` | Ao pressionar Run |
| `ExecutionCompletedEvent` | Fluxo terminou normalmente |
| `ExecutionStoppedByUserEvent` | Usuário pressionou Stop |
| `ExecutionFailedEvent` | Erro não tratado |
| `NodeStartedEvent` | Um nó começa a executar |
| `NodeCompletedEvent` | Um nó terminou (com outputs) |
| `NodeSkippedEvent` | Nó desabilitado ou condição falsa |
| `NodeErrorEvent` | Um nó lançou exceção |
| `VariableChangedEvent` | Uma variável foi alterada |
| `ExecutionPausedEvent` | Step-through pausou no nó |
| `LogMessageEvent` | Mensagem de log |

---

## 7. Sistema de Nodes

### Criando um novo bloco

**Passo 1:** Implementar `INodeExecutor`
```csharp
public class PressKeyExecutor : INodeExecutor
{
    private readonly InputSimulator _input;

    public PressKeyExecutor(InputSimulator input) => _input = input;

    public async Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context)
    {
        var key  = context.GetInputValue<string>(node, "key");
        var hold = context.GetInputValue<int>(node, "hold");

        _input.Keyboard.KeyPress(key);

        if (hold > 0)
            await Task.Delay(hold, context.CancellationToken);

        return NodeExecutionResult.Success(outputs: new() { ["done"] = true });
    }
}
```

**Passo 2:** Registrar no `NodeRegistry`
```csharp
registry.Register(
    PressKeyMetadata.Instance,  // NodeMetadata estático
    new PressKeyExecutor(inputSim)
);
```

**Passo 3:** A UI descobre o bloco automaticamente — zero alterações no editor.

---

## 8. Engine de Execução

### FlowExecutor — Ciclo de vida

```
Run()
  │
  ├─ Publica ExecutionStartedEvent
  │
  ├─ Ordena nós topologicamente (DFS a partir dos EventNodes)
  │
  └─ Para cada nó:
       ├─ Publica NodeStartedEvent         ← UI ilumina o nó
       ├─ Resolve inputs (valores estáticos + outputs de nós anteriores)
       ├─ Chama INodeExecutor.ExecuteAsync()
       ├─ Publica NodeCompletedEvent       ← UI atualiza o nó
       └─ Passa outputs para nós seguintes

Stop()  → CancellationToken.Cancel()
Pause() → SemaphoreSlim para pausar o loop
Step()  → Avança um nó por vez (debug visual)
```

### Controle de velocidade
```csharp
// Slider na UI de 0ms (rápido) a 2000ms (devagar — bom para crianças aprenderem)
executor.StepDelayMs = 500;
```

---

## 9. Formato de Arquivo .mkproject

Arquivo ZIP com extensão renomeada — idêntico a `.docx`, `.unitypackage`.

```
meufluxo.mkproject (ZIP)
├── project.json     ← FlowDocument serializado em JSON
├── preview.png      ← thumbnail 400×225 gerado automaticamente ao salvar
└── assets/
    ├── images/      ← imagens usadas por nós de reconhecimento de tela
    └── sounds/      ← sons usados por nós de áudio (futuro)
```

### Versionamento do schema
O `SchemaVersion` em `project.json` permite migrações:
```csharp
// ProjectPackager abre o zip, lê SchemaVersion,
// e aplica migrações em sequência antes de deserializar
if (doc.SchemaVersion < CurrentSchemaVersion)
    doc = MigrationRunner.Migrate(doc);
```

---

## 10. Sistema de Plugins

### Criando um plugin externo

1. Instale o NuGet `MacroKids.Plugins` (futuramente no nuget.org)
2. Crie uma DLL que implementa `INodePlugin`:

```csharp
public class MinhaExtensao : INodePlugin
{
    public PluginManifest Manifest => new()
    {
        Id          = "com.meusite.minhaextensao",
        Name        = "Minha Extensão",
        Version     = new Version(1, 0, 0),
        Author      = "Meu Nome",
        Description = "Adiciona blocos de integração com Discord"
    };

    public IEnumerable<(NodeMetadata, INodeExecutor)> GetNodes()
    {
        yield return (DiscordSendMessageMetadata.Instance,
                      new DiscordSendMessageExecutor());
    }
}
```

3. Compile e coloque a DLL em `%AppData%\MacroKids\plugins\`
4. MacroKids detecta e carrega na inicialização — blocos aparecem na sidebar com badge 🔌

---

## 11. Node Editor (WPF)

### NodeCanvas
Canvas WPF customizado com:
- **Pan:** Espaço + arrastar ou botão do meio do mouse
- **Zoom:** Scroll do mouse (10% a 500%)
- **Grid:** Pontos de fundo estilo Blueprint (renderizado no `OnRender`)
- **Seleção múltipla:** Retângulo de seleção com arrastar
- **Snap to grid:** Opcional, 20px por padrão

### NodeView
Renderizado dinamicamente a partir de `NodeMetadata`:
```
┌─[Ícone] Press Key ────────────────[×]─┐
│  ● Tecla    ────────── [A    ]         │
│  ● Segurar  ────────── [0    ] ms      │
│                                ● Done  │
└────────────────────────────────────────┘
```

Cada `NodePin` com `Direction = Input` gera uma linha com label + campo editável.
Cada `NodePin` com `Direction = Output` gera uma linha com label + ponto de conexão.

### ConnectionView
Curva Bézier com animação de fluxo durante execução:
```csharp
// Ponto de controle para curva natural
var cp1 = new Point(start.X + distance * 0.5, start.Y);
var cp2 = new Point(end.X   - distance * 0.5, end.Y);
```

---

## 12. Padrões de Código

### Convenções gerais
- **Allman style:** chaves sempre na nova linha
- **4 espaços** para `.cs`, **2 espaços** para `.xaml`/`.json`
- `var` quando o tipo é óbvio
- **Nullable** habilitado em todos os projetos
- **Records** para eventos e DTOs imutáveis
- **Init-only properties** para modelos (`required ... { get; init; }`)

### MVVM (CommunityToolkit)
```csharp
// ViewModel com source generators — sem boilerplate
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _projectName = string.Empty;

    [RelayCommand]
    private async Task RunFlowAsync() { ... }
}
```

### Tratamento de erros
```csharp
// Sempre logar com contexto útil
catch (Exception ex)
{
    _context.EventBus.Publish(new NodeErrorEvent(
        FlowId:         _flowId,
        NodeInstanceId: node.InstanceId,
        TypeId:         node.TypeId,
        Error:          ex
    ));
    // Re-throw ou retornar NodeExecutionResult.Failure(ex)
}
```

---

## 13. Testes

### Estrutura
```
tests/
├── MacroKids.Core.Tests/       → testes de modelos, serialização, commands
└── MacroKids.Runtime.Tests/    → testes da engine, event bus, executor
```

### Exemplo — testando o Event Bus
```csharp
[Fact]
public void EventBus_Subscribe_ReceivesPublishedEvent()
{
    var bus = new EventBus();
    NodeStartedEvent? received = null;

    using var _ = bus.Subscribe<NodeStartedEvent>(e => received = e);

    var expected = new NodeStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "test.node");
    bus.Publish(expected);

    Assert.Equal(expected, received);
}
```

### Rodando os testes
```bash
dotnet test
dotnet test --verbosity normal
dotnet test --filter "Category=Unit"
```

---

## 14. CI/CD & Qualidade

### GitHub Actions (`.github/workflows/build-and-test.yml`)
Roda automaticamente em cada push para `main` e em pull requests:

1. **Checkout** do código
2. **Setup .NET 10**
3. **Restore** dependências
4. **Format check** (`dotnet format --verify-no-changes`)
5. **Build** Release
6. **Testes** com saída verbosa
7. **Auditoria** de pacotes vulneráveis

### Ferramentas locais
```bash
# Formatar todo o código
dotnet format

# Verificar formatação sem alterar
dotnet format --verify-no-changes

# Checar pacotes vulneráveis
dotnet list package --vulnerable

# Checar pacotes desatualizados
dotnet list package --outdated
```

---

## 15. Versionamento & Release

### Fonte única da versão
Edite **apenas** `Directory.Build.props`:
```xml
<Version>0.2.0</Version>
```
Todos os assemblies herdam automaticamente.

### Fluxo de release
```bash
# 1. Editar CHANGELOG.md: mover [Unreleased] → [0.2.0] com data
# 2. Editar Directory.Build.props: <Version>0.2.0</Version>
# 3. Build e testes
dotnet build && dotnet test
# 4. Commit
git add -A && git commit -m "release: v0.2.0"
# 5. Tag
git tag v0.2.0
# 6. Push
git push && git push origin v0.2.0
```

### SemVer
| Tipo | Quando | Exemplo |
|---|---|---|
| `PATCH` | Correção de bug | `0.1.0` → `0.1.1` |
| `MINOR` | Nova funcionalidade compatível | `0.1.0` → `0.2.0` |
| `MAJOR` | Quebra de compatibilidade | `0.x.x` → `1.0.0` |

---

## 16. Roadmap Mobile

O `MacroKids.Core` é `net10.0` puro — roda em qualquer plataforma.

**Quando chegar a hora:**
1. Criar `MacroKids.Mobile` com **.NET MAUI**
2. Referencia `MacroKids.Core` e `MacroKids.Plugins` sem mudanças
3. `MacroKids.Runtime` não roda no mobile (P/Invoke Win32), mas a interface `INodeExecutor` sim
4. No mobile os nós podem ter implementações diferentes:
   - **Modo Educacional:** simula o fluxo visualmente (sem executar de verdade)
   - **Modo Remoto:** envia comandos ao PC via WebSocket/SignalR

---

## 17. Glossário

| Termo | Significado |
|---|---|
| **Nó (Node)** | Um bloco visual que representa uma ação ou lógica |
| **Pin** | Porta de entrada ou saída de um nó |
| **Conexão (Wire)** | A linha que conecta o output de um nó ao input de outro |
| **Fluxo (Flow)** | O grafo completo de nós e conexões |
| **Canvas** | A área de edição infinita onde os nós são posicionados |
| **TypeId** | Identificador único de um *tipo* de nó (ex: `"keyboard.press_key"`) |
| **InstanceId** | GUID único de uma *instância* de nó no canvas |
| **Event Bus** | Sistema de mensagens interno que conecta engine ↔ UI ↔ plugins |
| **NodeMetadata** | Descriptor imutável que descreve um tipo de nó para a UI |
| **INodeExecutor** | Interface que contém a lógica de execução de um nó |
| **FlowDocument** | O documento salvo de um projeto (grafo + viewport + versão) |
| **`.mkproject`** | Formato de arquivo do MacroKids (ZIP com JSON + assets) |
| **SchemaVersion** | Versão do formato JSON do `project.json` |
| **Command Pattern** | Padrão que encapsula operações do canvas para Undo/Redo |
