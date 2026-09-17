using System.Collections.Immutable;

namespace Rumours.App;

public enum HotkeyAction
{
    Scan,
    Reset,
    Toggle,
}

public sealed class HotkeyBindings
{
    private readonly ImmutableDictionary<HotkeyAction, string> _keys;

    private HotkeyBindings(ImmutableDictionary<HotkeyAction, string> keys) => _keys = keys;

    public static HotkeyBindings Of(params (HotkeyAction Action, string Hotkey)[] bindings) =>
        new(bindings.ToImmutableDictionary(b => b.Action, b => b.Hotkey ?? ""));

    public string this[HotkeyAction action] => _keys.GetValueOrDefault(action, "");

    public (HotkeyBindings Bindings, HotkeyAction? Displaced) Assign(HotkeyAction action, string hotkey)
    {
        var keys = _keys;
        HotkeyAction? displaced = null;
        if (hotkey != "")
            foreach (var (other, taken) in _keys)
                if (other != action && string.Equals(taken, hotkey, StringComparison.OrdinalIgnoreCase))
                {
                    displaced = other;
                    keys = keys.SetItem(other, "");
                }
        return (new HotkeyBindings(keys.SetItem(action, hotkey)), displaced);
    }
}
