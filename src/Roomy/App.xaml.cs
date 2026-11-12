using System.Windows;
using Roomy.ViewModels;

namespace Roomy;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args = e.Args;
        string? Arg(string name)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show(ex.Exception.Message, "Roomy hit a snag", MessageBoxButton.OK, MessageBoxImage.Warning);
            ex.Handled = true;
        };

        // Build-time helper: render the logo into a multi-size .ico (+ a big PNG).
        if (Arg("--export-icon") is { } iconPath)
        {
            IconExporter.Export(iconPath);
            Shutdown();
            return;
        }

        // Local-only tools can hook in here (not part of the public build).
        var handled = false;
        RunExtras(args, ref handled);
        if (handled) return;

        if (Arg("--export-store-art") is { } artDir)
        {
            IconExporter.ExportStoreArt(artDir);
            Shutdown();
            return;
        }

        // Diagnostic: run every scan step (read-only) and write what was found to a text file.
        if (Arg("--scan-report") is { } reportPath)
        {
            var ctx = new Core.ScanContext { IsAdmin = Core.Admin.IsElevated };
            var lines = new List<string> { $"Roomy scan report · admin={ctx.IsAdmin} · {DateTime.Now:u}" };
            foreach (var step in Core.Scanner.Steps)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var cat = step.Run(ctx);
                    lines.Add($"{step.Title,-16} {(cat is null ? "-" : cat.Blocked ? "needs admin" : Core.Format.Bytes(cat.Size)),12}  ({sw.ElapsedMilliseconds} ms)");
                    if (cat != null)
                        foreach (var i in cat.Items.Take(8)) lines.Add($"    {i.Name,-40} {Core.Format.Bytes(i.Size),10}  {i.Detail}");
                }
                catch (Exception ex) { lines.Add($"{step.Title,-16} ERROR {ex.Message}"); }
            }
            File.WriteAllLines(reportPath, lines);
            Shutdown();
            return;
        }

        var shots = Arg("--shots");
        var record = Arg("--record");
        if (record != null) shots = null;
        var demo = shots != null || record != null || args.Contains("--demo");
        var vm = new MainVM(demo);
        var window = new MainWindow(vm);

        if (shots != null || record != null)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = -20000;
            window.Top = 0;
            window.Width = 1240;
            window.Height = record != null ? 780 : 800;
            window.ShowActivated = false;
            window.ShowInTaskbar = false;
        }

        MainWindow = window;
        window.Show();

        if (shots != null) _ = window.RunScreenshotsAsync(shots);
        else if (record != null) _ = window.RecordDemoAsync(record);
        else if (args.Contains("--scan")) vm.ScanCommand.Execute(null);
    }

    partial void RunExtras(string[] args, ref bool handled);
}
