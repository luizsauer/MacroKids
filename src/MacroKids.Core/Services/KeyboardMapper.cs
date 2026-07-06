using System;

namespace MacroKids.Core.Services;

public static class KeyboardMapper
{
    public static byte GetVirtualKeyCode(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        
        string cleanKey = key.Trim().ToUpperInvariant();
        
        // Letra única ou número (A-Z, 0-9)
        if (cleanKey.Length == 1)
        {
            char c = cleanKey[0];
            if (c >= 'A' && c <= 'Z') return (byte)c;
            if (c >= '0' && c <= '9') return (byte)c;
        }

        // Teclas de Função F1 - F12
        if (cleanKey.StartsWith("F") && cleanKey.Length > 1 && int.TryParse(cleanKey.Substring(1), out int fNum) && fNum >= 1 && fNum <= 12)
        {
            return (byte)(0x6F + fNum); // F1 = 0x70, F2 = 0x71, ..., F12 = 0x7B
        }

        // Mapeamento de Teclas Especiais e de Controle
        return cleanKey switch
        {
            "CTRL" or "CONTROL" or "CONTROL ESQUERDO" => 0x11,
            "SHIFT" or "SHIFT ESQUERDO" => 0x10,
            "ALT" or "ALT ESQUERDO" => 0x12,
            "RCTRL" or "CONTROL DIREITO" => 0xA3,
            "RSHIFT" or "SHIFT DIREITO" => 0xA1,
            "RALT" or "ALT GR" or "ALT DIREITO" => 0xA5,
            
            "ENTER" or "RETORNO" => 0x0D,
            "SPACE" or "ESPAÇO" => 0x20,
            "BACKSPACE" or "BACK" or "APAGAR" => 0x08,
            "TAB" or "TABULAÇÃO" => 0x09,
            "ESCAPE" or "ESC" => 0x1B,
            
            "UP" or "CIMA" or "SETA CIMA" => 0x26,
            "DOWN" or "BAIXO" or "SETA BAIXO" => 0x28,
            "LEFT" or "ESQUERDA" or "SETA ESQUERDA" => 0x25,
            "RIGHT" or "DIREITA" or "SETA DIREITA" => 0x27,
            
            "DELETE" or "DEL" or "EXCLUIR" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" or "INÍCIO" => 0x24,
            "END" or "FIM" => 0x23,
            "PAGEUP" or "PGUP" or "PÁGINA CIMA" => 0x21,
            "PAGEDOWN" or "PGDN" or "PÁGINA BAIXO" => 0x22,
            
            "CAPSLOCK" or "CAPS" or "FIXA" => 0x14,
            "NUMLOCK" => 0x90,
            "SCROLLLOCK" => 0x91,
            "PRINTSCREEN" or "PRTSC" or "PRINT" => 0x2C,
            "PAUSE" or "BREAK" => 0x13,
            "LWIN" or "WIN" or "WINDOWS" or "WIN ESQUERDO" => 0x5B,
            "RWIN" or "WIN DIREITO" => 0x5C,
            "APPS" or "MENU CONTEXTO" => 0x5D,

            // Teclado Numérico (Numpad)
            "NUMPAD0" => 0x60,
            "NUMPAD1" => 0x61,
            "NUMPAD2" => 0x62,
            "NUMPAD3" => 0x63,
            "NUMPAD4" => 0x64,
            "NUMPAD5" => 0x65,
            "NUMPAD6" => 0x66,
            "NUMPAD7" => 0x67,
            "NUMPAD8" => 0x68,
            "NUMPAD9" => 0x69,
            "MULTIPLY" or "MULTIPLICAR" => 0x6A,
            "ADD" or "SOMAR" => 0x6B,
            "SUBTRACT" or "SUBTRAIR" => 0x6D,
            "DECIMAL" or "PONTO NUMÉRICO" => 0x6E,
            "DIVIDE" or "DIVIDIR" => 0x6F,

            _ => 0
        };
    }
}
