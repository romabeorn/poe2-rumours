using System.Drawing;
using System.Reflection;
using Forms = System.Windows.Forms;

namespace Rumours.App;

public sealed class TrayMenu : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Icon _image;
    private readonly Action _openMenu, _exit;

    public TrayMenu(UiText text, Action openMenu, Action exit)
    {
        (_openMenu, _exit) = (openMenu, exit);
        _image = Load();
        _icon = new Forms.NotifyIcon { Icon = _image, Visible = true };
        _icon.MouseClick += (_, click) =>
        {
            if (click.Button == Forms.MouseButtons.Left) _openMenu();
        };
        SwitchLanguage(text);
    }

    public void SwitchLanguage(UiText text)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(text.MenuOpen, null, (_, _) => _openMenu());
        menu.Items.Add(text.MenuExit, null, (_, _) => _exit());

        _icon.Text = text.TrayTooltip;
        (var old, _icon.ContextMenuStrip) = (_icon.ContextMenuStrip, menu);
        old?.Dispose();
    }

    public void Announce(string title, string body) => _icon.ShowBalloonTip(4000, title, body, Forms.ToolTipIcon.None);

    private static Icon Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico")
            ?? throw new InvalidOperationException("The application icon is missing from the build");
        return new Icon(stream, Forms.SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _image.Dispose();
    }
}
