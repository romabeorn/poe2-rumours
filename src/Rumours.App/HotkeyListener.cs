using System.Windows.Input;
using System.Windows.Interop;

namespace Rumours.App;

public sealed class HotkeyListener : IDisposable
{
    private const uint NoRepeat = 0x4000;

    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = [];
    private int _nextId = 1;

    public HotkeyListener(HwndSource source)
    {
        _source = source;
        _source.AddHook(OnMessage);
    }

    public bool Register(string hotkey, Action action)
    {
        if (string.IsNullOrWhiteSpace(hotkey)) return true;
        if (!TryParse(hotkey, out var modifiers, out var key)) return false;

        var id = _nextId++;
        if (!Native.RegisterHotKey(_source.Handle, id, (uint)modifiers | NoRepeat, (uint)KeyInterop.VirtualKeyFromKey(key)))
            return false;

        _actions[id] = action;
        return true;
    }

    public static bool TryParse(string hotkey, out ModifierKeys modifiers, out Key key)
    {
        (modifiers, key) = (ModifierKeys.None, Key.None);
        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        foreach (var part in parts[..^1])
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl": modifiers |= ModifierKeys.Control; break;
                case "alt": modifiers |= ModifierKeys.Alt; break;
                case "shift": modifiers |= ModifierKeys.Shift; break;
                case "win": modifiers |= ModifierKeys.Windows; break;
                default: return false;
            }
        }
        return Enum.TryParse(parts[^1], ignoreCase: true, out key) && key != Key.None;
    }

    private IntPtr OnMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == Native.WmHotkey && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            handled = true;
            _source.Dispatcher.BeginInvoke(action);
        }
        return IntPtr.Zero;
    }

    public void Clear()
    {
        foreach (var id in _actions.Keys) Native.UnregisterHotKey(_source.Handle, id);
        _actions.Clear();
    }

    public void Dispose()
    {
        Clear();
        _source.RemoveHook(OnMessage);
    }
}
