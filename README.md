<div align="center">

<img src="Assets/MacroKids-Logo.png" alt="MacroKids" width="480"/>

### Create. Automate. Imagine.

Plataforma educacional de automação visual para crianças e criadores.
Monte fluxos poderosos com blocos coloridos — sem escrever uma linha de código.

---

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-22C55E?style=for-the-badge)](LICENSE)
[![CI](https://img.shields.io/github/actions/workflow/status/luizsauer/MacroKids/build-and-test.yml?style=for-the-badge&label=CI&logo=githubactions&logoColor=white)](https://github.com/luizsauer/MacroKids/actions)
[![Version](https://img.shields.io/badge/Version-0.1.0--dev-F97316?style=for-the-badge)](CHANGELOG.md)

</div>

---

## O que é o MacroKids?

MacroKids é um editor visual de fluxos de automação para Windows. A criança monta blocos como peças de um quebra-cabeça e cria automações reais — enquanto aprende, sem perceber, conceitos fundamentais de programação: **eventos, condições, loops e variáveis**.

Pense num cruzamento entre **Scratch**, **Unreal Blueprint** e **Node-RED**, projetado para rodar no seu computador.

---

## Funcionalidades

| Categoria | Blocos |
|---|---|
| ⚡ **Eventos** | Tecla pressionada, Timer, Inicialização |
| ⌨️ **Teclado** | Pressionar tecla, Digitar texto, Atalhos |
| 🖱️ **Mouse** | Mover, Clicar, Scroll, Arrastar |
| 🎮 **Controle** | Xbox, DualShock, Joystick |
| 🔁 **Fluxo** | Repetir, Se/Senão, Enquanto, Aguardar |
| 📦 **Variáveis** | Definir, Ler, Incrementar, Lista |
| 🖼️ **Tela** | Capturar pixel, Comparar imagem *(futuro)* |
| 🔡 **OCR** | Ler texto da tela *(futuro)* |
| 🤖 **IA** | Comandos por visão computacional *(futuro)* |
| 🔌 **Plugins** | Adicione seus próprios blocos com uma DLL |

---

## Interface

```
┌──────────────────────────────────────────────────────────────────────┐
│  MacroKids                                    ▶ Run  🐛 Debug  ■ Stop │
├──────────────┬──────────────────────────────────────┬────────────────┤
│  🔍 Buscar   │                                      │  Propriedades  │
│              │   [Quando F8] ──► [Aguardar 500ms]   │                │
│ ⚡ Eventos   │        │                             │  Bloco: Clicar │
│ ⌨️ Teclado   │   [Mover Mouse] ──► [Clique Esq.]   │  X: [500    ]  │
│ 🖱️ Mouse     │        │                             │  Y: [300    ]  │
│ 🎮 Controle  │   [Se contador > 3]                  │                │
│ 🔁 Loops     │   ├── Sim → [Pressionar E]           │                │
│ 📦 Variáveis │   └── Não → [Aguardar 1s]            │                │
│ 🖼️ Imagens   │                                      │                │
│ 🔌 Plugins   │                                      │                │
└──────────────┴──────────────────────────────────────┴────────────────┘
```

---

## Arquitetura

MacroKids é dividido em 7 projetos independentes:

```
MacroKids/
├── src/
│   ├── MacroKids.Core/        # Modelos, interfaces, eventos — C# puro, sem Win32
│   ├── MacroKids.NodeEditor/  # Canvas drag-and-drop WPF (reutilizável)
│   ├── MacroKids.Nodes/       # Biblioteca de blocos nativos
│   ├── MacroKids.Runtime/     # Engine de execução de fluxos
│   ├── MacroKids.Recorder/    # Gravador de ações via hooks Win32
│   ├── MacroKids.Plugins/     # SDK para extensões de terceiros
│   └── MacroKids.UI/          # Aplicação WPF principal
└── tests/
    ├── MacroKids.Core.Tests/
    └── MacroKids.Runtime.Tests/
```

> 📖 Veja a [Documentação de Arquitetura](docs/ARCHITECTURE.md) para detalhes completos.

---

## Filosofia de Design

### UI nunca conhece nós concretos
A camada de interface nunca importa `PressKeyNode` ou `MoveMouseNode`. Cada bloco se descreve através de metadados (`NodeMetadata`) e a UI renderiza qualquer bloco automaticamente. Isso permite:
- Adicionar centenas de novos blocos sem alterar o editor
- Terceiros publicarem plugins com novos blocos via uma DLL
- Reutilizar o `Core` no futuro em apps mobile (MAUI)

### Event Bus
Toda a comunicação entre a engine e a UI passa por um `IEventBus` de publish/subscribe. O bloco que está sendo executado **ilumina** na tela em tempo real. A criança vê o fluxo "ganhar vida".

---

## Instalação para Desenvolvimento

### Requisitos
- Windows 10 ou 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022+ ou VS Code com extensão C#

### Clonar e rodar

```bash
git clone https://github.com/luizsauer/MacroKids.git
cd MacroKids
dotnet restore
dotnet build
dotnet run --project src/MacroKids.UI
```

### Rodar os testes

```bash
dotnet test
```

### Verificar formatação

```bash
dotnet format --verify-no-changes
```

---

## Formato de Projeto

Os projetos MacroKids são salvos como **`.mkproject`** — um arquivo ZIP renomeado, idêntico ao `.docx` ou `.unitypackage`:

```
meufluxo.mkproject (ZIP)
├── project.json    ← grafo serializado + versão
├── preview.png     ← thumbnail gerado automaticamente
└── assets/
    └── images/
```

---

## Plugins

Qualquer desenvolvedor pode criar novos blocos:

```csharp
public class MeuPlugin : INodePlugin
{
    public PluginManifest Manifest => new()
    {
        Id      = "com.dev.meuplugin",
        Name    = "Meu Plugin",
        Version = new Version(1, 0, 0),
        Author  = "Dev"
    };

    public IEnumerable<(NodeMetadata, INodeExecutor)> GetNodes()
    {
        yield return (MeuBlocoMetadata.Instance, new MeuBlocoExecutor());
    }
}
```

Coloque a DLL em `%AppData%\MacroKids\plugins\` e os blocos aparecem automaticamente na sidebar.

---

## Roadmap

### v0.1 — Fundação *(atual)*
- [x] Solution com 7 projetos + testes
- [x] Modelos de domínio (FlowNode, FlowDocument, NodeMetadata, NodePin)
- [x] Event Bus e contexto de execução
- [x] Toolchain de qualidade (editorconfig, CI, analyzers)
- [ ] Interfaces restantes (INodeExecutor, INodeRegistry)
- [ ] Command Pattern (Undo/Redo)
- [ ] Serialização (.mkproject)

### v0.2 — Node Editor
- [ ] Canvas com pan/zoom/grid
- [ ] Blocos renderizados dinamicamente por metadados
- [ ] Conexões Bézier animadas

### v0.3 — Engine + Blocos
- [ ] FlowExecutor com debug visual
- [ ] Blocos: Keyboard, Mouse, Flow, Variables
- [ ] Painel de log ao vivo

### v0.4 — Gravador + Projetos
- [ ] Gravar ações e converter em blocos
- [ ] Tela de projetos recentes
- [ ] Auto-save

### v1.0 — Lançamento Público
- [ ] Tutorial interativo
- [ ] Biblioteca de exemplos
- [ ] Plugin SDK público
- [ ] Site de download

---

## Contribuindo

Contribuições são bem-vindas! Veja o [CHANGELOG.md](CHANGELOG.md) para histórico de mudanças.

1. Fork o repositório
2. Crie uma branch: `git checkout -b feature/minha-feature`
3. Commit: `git commit -m "feat: adiciona bloco de delay aleatório"`
4. Push: `git push origin feature/minha-feature`
5. Abra um Pull Request

---

## Identidade Visual

<div align="center">
  <img src="Assets/MacroKids-MiniLogo.png" alt="MacroKids Icon" width="120"/>
</div>

| Cor | Hex | Uso |
|---|---|---|
| Azul Navy | `#1A2035` | Background principal |
| Azul | `#3B82F6` | Blocos Keyboard |
| Verde | `#22C55E` | Blocos Mouse |
| Laranja | `#F97316` | Blocos Gamepad |
| Roxo | `#8B5CF6` | Blocos Variables |
| Amarelo | `#EAB308` | Blocos Loops |
| Rosa | `#EC4899` | Blocos Events |
| Ciano | `#14B8A6` | Blocos OCR |
| Lilás | `#A855F7` | Blocos AI |

**Fontes:** Nunito (interface) · Fira Code (valores técnicos/código)

---

## Licença

MIT © 2026 MacroKids — Feito com ❤️ para crianças curiosas.
