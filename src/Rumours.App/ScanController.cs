using Rumours.Capture;

namespace Rumours.App;

public sealed class ScanController(TooltipScanner scanner, ScanLog log, OverlayViewModel viewModel, OverlayWindow window)
{
    private bool _scanning;

    public async void Scan()
    {
        if (_scanning) return;
        _scanning = true;
        try
        {
            var monitor = Native.MonitorUnderCursor(out var cursor);
            if (!window.HiddenFromCapture) window.Hide();
            using var report = await scanner.ScanAsync(monitor, cursor);

            if (report.Outcome.Map is { } map) viewModel.Inspect(map.Island, map.Language);
            else viewModel.Apply(report.Outcome.Scan);
            log.Write(report);

            window.Show();
        }
        catch (Exception error)
        {
            viewModel.Fail(error.Message);
            log.WriteError(error);
        }
        finally
        {
            _scanning = false;
        }
    }
}
