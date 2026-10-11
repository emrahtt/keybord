using System.ComponentModel;
using System.Runtime.InteropServices;

namespace YazimAsistani.Desktop.Services;

public static class TextInjector
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventUnicode = 0x0004;
    private const uint KeyEventKeyUp = 0x0002;
    private const ushort VirtualKeyBackspace = 0x08;

    public static void ReplacePreviousCharacters(int characterCount, string replacement)
    {
        if (characterCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(characterCount));
        }

        for (var index = 0; index < characterCount; index++)
        {
            SendVirtualKey(VirtualKeyBackspace);
        }

        SendUnicodeText(replacement);
    }

    public static void SendUnicodeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var inputs = new List<Input>(text.Length * 2);
        foreach (var character in text)
        {
            inputs.Add(CreateUnicodeInput(character, keyUp: false));
            inputs.Add(CreateUnicodeInput(character, keyUp: true));
        }

        Send(inputs);
    }

    private static void SendVirtualKey(ushort virtualKey)
    {
        Send(
        [
            CreateVirtualKeyInput(virtualKey, keyUp: false),
            CreateVirtualKeyInput(virtualKey, keyUp: true)
        ]);
    }

    private static void Send(IReadOnlyCollection<Input> inputs)
    {
        if (inputs.Count == 0)
        {
            return;
        }

        var buffer = inputs.ToArray();
        var sent = SendInput((uint)buffer.Length, buffer, Marshal.SizeOf<Input>());
        if (sent != buffer.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static Input CreateUnicodeInput(char character, bool keyUp) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion
        {
            Keyboard = new KeyboardInput
            {
                Scan = character,
                Flags = KeyEventUnicode | (keyUp ? KeyEventKeyUp : 0)
            }
        }
    };

    private static Input CreateVirtualKeyInput(ushort virtualKey, bool keyUp) => new()
    {
        Type = InputKeyboard,
        Data = new InputUnion
        {
            Keyboard = new KeyboardInput
            {
                VirtualKey = virtualKey,
                Flags = keyUp ? KeyEventKeyUp : 0
            }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
}

