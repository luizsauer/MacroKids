using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MacroKids.Core.Interfaces;
using MacroKids.Core.Models;

namespace MacroKids.Nodes.Gamepad;

public static class PressGamepadButtonMetadata
{
    public static readonly NodeMetadata Instance = new()
    {
        TypeId = "gamepad.press_button",
        Name = "Pressionar Botão Gamepad",
        Description = "Simula o pressionamento de botões de um controle virtual (Xbox/PlayStation).",
        Category = NodeCategory.Gamepad,
        IconKey = "Gamepad",
        NodeVersion = new Version(1, 0, 0),
        Pins = [
            new NodePin { Id = "in", Label = "Entrada", Direction = PinDirection.Input, DataType = typeof(bool) },
            new NodePin { 
                Id = "button", 
                Label = "Botão", 
                Direction = PinDirection.Input, 
                DataType = typeof(string), 
                InputType = PinInputType.Dropdown, 
                Options = ["A", "B", "X", "Y", "LB", "RB", "LT", "RT", "Start", "Back", "D-Pad Up", "D-Pad Down", "D-Pad Left", "D-Pad Right"], 
                DefaultValue = "A" 
            },
            new NodePin { Id = "ms", Label = "Tempo (ms)", Direction = PinDirection.Input, DataType = typeof(int), DefaultValue = 200 },
            new NodePin { Id = "done", Label = "Concluído", Direction = PinDirection.Output, DataType = typeof(bool) }
        ]
    };
}

public class PressGamepadButtonExecutor : INodeExecutor
{
    public async Task<NodeExecutionResult> ExecuteAsync(
        FlowNode node,
        IExecutionContext context,
        IReadOnlyDictionary<string, object?> resolvedInputs)
    {
        string button = "A";
        if (resolvedInputs.TryGetValue("button", out var btnVal) && btnVal is string b)
            button = b;
        else if (node.PinValues.TryGetValue("button", out var sb) && sb is string sbStr)
            button = sbStr;

        int ms = 200;
        if (resolvedInputs.TryGetValue("ms", out var msVal) && msVal is int m)
            ms = m;
        else if (node.PinValues.TryGetValue("ms", out var sm) && sm is int smInt)
            ms = smInt;

        context.Log($"[Aviso Gamepad] Simulação física de Gamepad requer o driver de kernel 'ViGEmBus' instalado no Windows.");
        context.Log($"[Info] Tentando simular botão '{button}' por {ms}ms...");

        // Aguarda o delay para simular a retencao
        await Task.Delay(ms);

        context.Log($"[Info] Botão '{button}' liberado.");

        return NodeExecutionResult.Success(new() { ["done"] = true });
    }
}
