using System.Diagnostics;
using FlaUI.Cli.Rpc;
using FlaUI.Core.AutomationElements;

namespace FlaUI.Cli.Core;

internal static class WindowExtensions
{
    extension(Window window)
    {
        public WindowInfo ToWindowInfo()
        {
            var processId = window.Properties.ProcessId.ValueOrDefault;
            using var process = Process.GetProcessById(processId);
            return new(window.Title, process.ProcessName);
        }
    }
}
