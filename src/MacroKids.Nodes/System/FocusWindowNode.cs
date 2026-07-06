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
        NodeVersion = new Version(1, 1, 0),
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

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attachThread, uint attachToThread, bool attach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private const int SW_RESTORE = 9;
    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;
    private const int SW_SHOW = 5;

    public async Task<NodeExecutionResult> ExecuteAsync(
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
            return NodeExecutionResult.Success(new() { ["done"] = true });
        }

        string normalizedSearch = title.Trim().TrimStart('*').Trim();
        context.Log($"Buscando janela com título contendo: '{normalizedSearch}'...");

        IntPtr hwnd = FindWindowHandle(title, normalizedSearch);

        if (hwnd == IntPtr.Zero)
        {
            context.Log($"Aviso: Não foi possível encontrar janela com o título '{title}'.");
            return NodeExecutionResult.Success(new() { ["done"] = true });
        }

        if (IsIconic(hwnd))
        {
            context.Log("Janela minimizada detectada. Restaurando...");
            ShowWindow(hwnd, SW_RESTORE);
            await Task.Delay(150);
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
                context.Log("Trazendo janela para primeiro plano...");
                ForceForeground(hwnd);
                break;
        }

        await Task.Delay(400);
        return NodeExecutionResult.Success(new() { ["done"] = true });
    }

    private static IntPtr FindWindowHandle(string rawTitle, string normalizedSearch)
    {
        IntPtr hwnd = IntPtr.Zero;
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                string windowTitle = p.MainWindowTitle;
                if (string.IsNullOrEmpty(windowTitle))
                    continue;

                if (windowTitle.Contains(rawTitle, StringComparison.OrdinalIgnoreCase) ||
                    windowTitle.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                {
                    hwnd = p.MainWindowHandle;
                    break;
                }
            }
            catch
            {
                // Ignora processos sem permissão
            }
        }

        return hwnd;
    }

    private static void ForceForeground(IntPtr hwnd)
    {
        uint targetThread = GetWindowThreadProcessId(hwnd, out _);
        uint currentThread = GetCurrentThreadId();
        IntPtr foreground = GetForegroundWindow();
        uint foregroundThread = foreground != IntPtr.Zero
            ? GetWindowThreadProcessId(foreground, out _)
            : 0;

        bool attached = false;
        if (foregroundThread != 0 && foregroundThread != currentThread)
        {
            AttachThreadInput(currentThread, foregroundThread, true);
            attached = true;
        }

        if (targetThread != currentThread)
            AttachThreadInput(currentThread, targetThread, true);

        ShowWindow(hwnd, SW_SHOW);
        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);

        if (targetThread != currentThread)
            AttachThreadInput(currentThread, targetThread, false);

        if (attached)
            AttachThreadInput(currentThread, foregroundThread, false);
    }
}
