using System.Drawing;
using Forms = System.Windows.Forms;

namespace YazimAsistani.Desktop.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconService(Action showWindow, Action toggleLanguage, Action exitApplication)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Aç / Open", null, (_, _) => showWindow());
        menu.Items.Add("TR ↔ EN", null, (_, _) => toggleLanguage());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Çıkış / Exit", null, (_, _) => exitApplication());

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Yazım Asistanı",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => showWindow();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        GC.SuppressFinalize(this);
    }
}

