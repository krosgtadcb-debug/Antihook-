using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Antihookclient
{
    public sealed class AntiCheatFinding
    {
        public string Code { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Severity { get; set; }
    }

    /// <summary>
    /// Detección defensiva y transparente. No inyecta, no lee memoria arbitraria,
    /// no termina procesos y no requiere privilegios elevados.
    /// </summary>
    public sealed class DefensiveAntiCheat : IDisposable
    {
        private readonly string gameProcessName;
        private readonly HashSet<string> approvedModuleNames;
        private CancellationTokenSource cancellation;
        private Task worker;

        public event Action<AntiCheatFinding> FindingDetected;

        public DefensiveAntiCheat(string gameProcessName, IEnumerable<string> approvedModuleNames)
        {
            this.gameProcessName = (gameProcessName ?? "LanBf3").Trim();
            this.approvedModuleNames = new HashSet<string>(approvedModuleNames ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public void Start(TimeSpan interval)
        {
            if (worker != null) return;
            cancellation = new CancellationTokenSource();
            worker = Task.Run(async () =>
            {
                while (!cancellation.IsCancellationRequested)
                {
                    ScanOnce();
                    await Task.Delay(interval, cancellation.Token).ConfigureAwait(false);
                }
            }, cancellation.Token);
        }

        public void ScanOnce()
        {
            foreach (var process in Process.GetProcessesByName(gameProcessName))
            {
                try
                {
                    foreach (ProcessModule module in process.Modules)
                    {
                        if (approvedModuleNames.Count > 0 && !approvedModuleNames.Contains(module.ModuleName))
                            Report("unexpected-module", module.ModuleName, "review");
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    Report("module-access-denied", "No se pudo enumerar módulos sin elevar privilegios.", "info");
                }
                catch (InvalidOperationException)
                {
                    Report("process-exited", "El proceso terminó durante la inspección.", "info");
                }
                finally { process.Dispose(); }
            }
        }

        public static string HashFile(string path)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = System.IO.File.OpenRead(path))
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private void Report(string code, string detail, string severity)
        {
            FindingDetected?.Invoke(new AntiCheatFinding { Code = code, Detail = detail, Severity = severity, CreatedAtUtc = DateTime.UtcNow });
        }

        public void Dispose()
        {
            if (cancellation != null) cancellation.Cancel();
            worker = null;
            if (cancellation != null) cancellation.Dispose();
        }
    }
}
