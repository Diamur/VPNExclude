using System.Diagnostics;

namespace VPNExclude
{
    public partial class StartForm : Form
    {
        private const string WireGuardPath = @"C:\Program Files\WireGuard\wireguard.exe";

        public StartForm()
        {
            InitializeComponent();

            btnOpenRules.Click += BtnOpenRules_Click;
            btnVpnSelect.Click += BtnVpnSelect_Click;
        }

        private void BtnOpenRules_Click(object? sender, EventArgs e)
        {
            btnOpenRules.Enabled = false;
            Hide();

            using var rulesForm = new Form1();
            rulesForm.ShowDialog(this);

            Show();
            Activate();
            btnOpenRules.Enabled = true;
        }

        private void BtnVpnSelect_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(WireGuardPath))
                {
                    MessageBox.Show(
                        $"Не найден файл WireGuard:\n{WireGuardPath}",
                        "WireGuard не найден",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    AddLog($"Файл не найден: {WireGuardPath}");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = WireGuardPath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                AddLog("WireGuard запущен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось запустить WireGuard:\n{ex.Message}",
                    "Ошибка запуска",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                AddLog($"Ошибка запуска WireGuard: {ex.Message}");
            }
        }

        private static void AddLog(string message)
        {
            Debug.WriteLine($"[StartForm] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }
    }
}
