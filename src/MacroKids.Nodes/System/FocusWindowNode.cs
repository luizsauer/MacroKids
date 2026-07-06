using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.System;

public static class FocusWindowMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "window.focus",
        Name = "Focar Janela",
        Description = "Localiza uma janela pelo título ou processo, traz para frente e maximiza se necessário.",
        Category = NodeCategory.System,
        IconKey = "Window",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { Id = "title", Label = "Título Janela", Direction = PinDirection.Input, DataType = typeof(string), DefaultValue = "" },
            new NodePin { 
                Id = "action", 
                Label = "Ação", 
                Direction = PinDirection.Input, 
                DataType = typeof(string), 
                InputType = PinInputType.Dropdown, 
                Options = ["Focus/Restore", "Maximize", "Minimize"], 
                DefaultValue = "Focus/Restore" 
            },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class FocusWindowExecutor : INodeExecutor
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;
    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;

    public Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string title = "";
        if (resolvedInputs.TryGetValue("title", out var tVal) && tVal is string t)
            title = t;
        else if (node.PinValues.TryGetValue("title", out var st) && st is string stStr)
            title = stStr;

        string action = "Focus/Restore";
        if (resolvedInputs.TryGetValue("action", out var aVal) && aVal is string a)
            action = a;
        else if (node.PinValues.TryGetValue("action", out var sa) && sa is string saStr)
            action = saStr;

        if (string.IsNullOrWhiteSpace(title))
        {
            context.Log("Aviso: Título da janela vazio no bloco Focar Janela.");
            return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
        }

        context.Log($"Buscando janela com título contendo: '{title}'...");

        IntPtr hwnd = IntPtr.Zero;
        var processes = Process.GetProcesses();
        foreach (var p in processes)
        {
            try
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle) &&
                    p.MainWindowTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
                {
                    hwnd = p.MainWindowHandle;
                    break;
                }
            }
            catch
            {
                // Ignora processos sem permissão de acesso
            }
        }

        if (hwnd == IntPtr.Zero)
        {
            context.Log($"Aviso: Não foi possível encontrar nenhuma janela ativa com o título '{title}'.");
            return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
        }

        // Se estiver minimizado, restaura primeiro
        if (IsIconic(hwnd))
        {
            context.Log("Janela minimizada detectada. Restaurando...");
            ShowWindow(hwnd, SW_RESTORE);
        }

        switch (action)
        {
            case "Maximize":
                context.Log("Maximizando janela...");
                ShowWindow(hwnd, SW_MAXIMIZE);
                break;
            case "Minimize":
                context.Log("Minimizando janela...");
                ShowWindow(hwnd, SW_MINIMIZE);
                break;
            default:
                // Focus/Restore
                context.Log("Trazendo janela para primeiro plano...");
                SetForegroundWindow(hwnd);
                break;
        }

        return Task.FromResult(NodeExecutionResult.Success(new() { ["done"] = true }));
    }
}
