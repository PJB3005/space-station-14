using Robust.Client.Input;
using Robust.Shared;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde;

internal sealed partial class Clyde
{
    private bool _usQwertyKeys;

    private void InitKeys()
    {
        _cfg.OnValueChanged(CVars.DisplayUSQWERTYHotkeys, val =>
        {
            _usQwertyKeys = val;
            RaiseInputModeChanged();
        }, true);
    }

    private void RaiseInputModeChanged()
    {
        _inputManager.InputModeChanged();
    }

    public string GetKeyName(Keyboard.Key key)
    {
        DebugTools.AssertNotNull(_windowing);

        var name = _windowing!.KeyGetName(key);
        if (name != null)
        {
            // Yes this is a very silly way to do this
            // We need to ask the windowing impl if it has the key (indicating that the key changes with layout maybe).
            // Since we don't want to ToString special keys like enter.
            return _usQwertyKeys ? key.ToString() : name;
        }

        name = Keyboard.GetSpecialKeyName(key, _loc);
        if (name != null)
            return _loc.GetString(name);

        return _loc.GetString("input-key-unknown");
    }
}
