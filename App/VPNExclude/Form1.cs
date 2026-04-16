using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace VPNExclude
{
    public partial class Form1 : Form
    {
        private const string DefaultGateway = "192.168.1.1";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly List<ExclusionRule> _rules = new();
        private readonly string _jsonPath;
        private ExclusionRule? _selectedRule;
        private string? _pendingCheckedAt;
        private bool _isInternalSelectionChange;

        public Form1()
        {
            InitializeComponent();

            _jsonPath = ResolveJsonPath();

            dgvRules.SelectionChanged += DgvRules_SelectionChanged;
            btnAddDomain.Click += (_, _) => BeginAddMode("Domain");
            btnAddIp.Click += (_, _) => BeginAddMode("IP");
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnCheckDomain.Click += BtnCheckDomain_Click;
            btnApplyRoutes.Click += BtnApplyRoutes_Click;

            Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadRulesFromDisk();
        }

        private static string ResolveJsonPath()
        {
            var currentDirectory = AppContext.BaseDirectory;

            for (var i = 0; i < 8; i++)
            {
                var candidate = Path.Combine(currentDirectory, "PS", "vpnexclude.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(currentDirectory);
                if (parent == null)
                {
                    break;
                }

                currentDirectory = parent.FullName;
            }

            return Path.Combine(AppContext.BaseDirectory, "vpnexclude.json");
        }

        private void LoadRulesFromDisk()
        {
            try
            {
                _rules.Clear();

                if (!File.Exists(_jsonPath))
                {
                    RefreshGridAndSelection();
                    SetStatus($"Файл не найден: {_jsonPath}");
                    AddLog("Файл JSON не найден. Будет создан при первом сохранении.");
                    return;
                }

                var json = File.ReadAllText(_jsonPath);
                var loaded = JsonSerializer.Deserialize<List<ExclusionRule>>(json, JsonOptions) ?? new List<ExclusionRule>();

                _rules.AddRange(loaded);

                RefreshGridAndSelection();
                SetStatus($"Загружено записей: {_rules.Count}");
                AddLog($"Загружено {_rules.Count} записей из JSON.");
            }
            catch (Exception ex)
            {
                _rules.Clear();
                RefreshGridAndSelection();
                MessageBox.Show($"Ошибка чтения JSON:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Ошибка чтения JSON");
                AddLog($"Ошибка чтения JSON: {ex.Message}");
            }
        }

        private void RefreshGridAndSelection()
        {
            var key = _selectedRule == null ? null : BuildRuleKey(_selectedRule);

            _isInternalSelectionChange = true;
            dgvRules.Rows.Clear();

            foreach (var rule in _rules)
            {
                var rowIndex = dgvRules.Rows.Add(
                    rule.Type,
                    rule.Target,
                    string.Join(", ", rule.Ips),
                    rule.Gateway,
                    rule.Comment,
                    rule.CreatedAt,
                    rule.UpdatedAt,
                    rule.CheckedAt);

                dgvRules.Rows[rowIndex].Tag = rule;
            }

            _isInternalSelectionChange = false;

            RestoreSelectionByKey(key);

            if (_selectedRule == null)
            {
                ClearDetailsFields(false);
            }
        }

        private void RestoreSelectionByKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            for (var i = 0; i < dgvRules.Rows.Count; i++)
            {
                var row = dgvRules.Rows[i];
                if (row.Tag is not ExclusionRule rule)
                {
                    continue;
                }

                if (!string.Equals(BuildRuleKey(rule), key, StringComparison.Ordinal))
                {
                    continue;
                }

                _isInternalSelectionChange = true;
                row.Selected = true;
                if (dgvRules.CurrentCell == null && row.Cells.Count > 0)
                {
                    dgvRules.CurrentCell = row.Cells[0];
                }
                _isInternalSelectionChange = false;

                ApplyRuleToDetails(rule);
                return;
            }

            _selectedRule = null;
        }

        private static string BuildRuleKey(ExclusionRule rule)
        {
            return $"{rule.Type}|{rule.Target}|{rule.CreatedAt}";
        }

        private void DgvRules_SelectionChanged(object? sender, EventArgs e)
        {
            if (_isInternalSelectionChange)
            {
                return;
            }

            if (dgvRules.SelectedRows.Count == 0)
            {
                _selectedRule = null;
                return;
            }

            if (dgvRules.SelectedRows[0].Tag is not ExclusionRule rule)
            {
                return;
            }

            ApplyRuleToDetails(rule);
            SetStatus($"Выбрана запись: {rule.Target}");
            AddLog($"Выбрана запись: {rule.Type} {rule.Target}");
        }

        private void ApplyRuleToDetails(ExclusionRule rule)
        {
            _selectedRule = rule;
            txtTarget.Text = rule.Target;
            cmbType.SelectedItem = rule.Type;
            txtIps.Text = string.Join(", ", rule.Ips);
            txtGateway.Text = rule.Gateway;
            txtComment.Text = rule.Comment;
            _pendingCheckedAt = rule.CheckedAt;
        }

        private void BeginAddMode(string type)
        {
            _selectedRule = null;
            _pendingCheckedAt = null;
            _isInternalSelectionChange = true;
            dgvRules.ClearSelection();
            _isInternalSelectionChange = false;

            ClearDetailsFields(true);
            cmbType.SelectedItem = type;
            txtTarget.Focus();

            SetStatus($"Режим добавления: {type}");
            AddLog($"Подготовлена новая запись типа {type}.");
        }

        private void ClearDetailsFields(bool keepType)
        {
            txtTarget.Clear();
            txtIps.Clear();
            txtGateway.Clear();
            txtComment.Clear();

            if (!keepType)
            {
                cmbType.SelectedIndex = -1;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var target = txtTarget.Text.Trim();
            var type = (cmbType.SelectedItem?.ToString() ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(target))
            {
                MessageBox.Show("Поле Target обязательно для сохранения.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Сохранение отменено: пустой Target");
                AddLog("Сохранение отменено: пустой Target.");
                return;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Поле Type обязательно для сохранения.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Сохранение отменено: пустой Type");
                AddLog("Сохранение отменено: пустой Type.");
                return;
            }

            var ips = txtIps.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (type.Equals("Domain", StringComparison.OrdinalIgnoreCase) && ips.Count == 0)
            {
                if (!TryResolveDomainIpv4(target, out var resolvedIps, out var errorMessage))
                {
                    MessageBox.Show(
                        $"Не удалось сохранить Domain-запись без IP. {errorMessage}",
                        "Проверка домена",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    SetStatus("Сохранение отменено: не удалось получить IP домена");
                    AddLog($"Сохранение отменено для {target}: {errorMessage}");
                    return;
                }

                ips = resolvedIps;
                txtIps.Text = string.Join(", ", ips);
                _pendingCheckedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                AddLog($"Автоподстановка IP для домена {target}: {txtIps.Text}");
            }

            if (type.Equals("IP", StringComparison.OrdinalIgnoreCase) && ips.Count == 0)
            {
                ips.Add(target);
            }

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var gateway = string.IsNullOrWhiteSpace(txtGateway.Text) ? DefaultGateway : txtGateway.Text.Trim();

            if (_selectedRule == null)
            {
                var newRule = new ExclusionRule
                {
                    Target = target,
                    Type = type,
                    Ips = ips,
                    Gateway = gateway,
                    Comment = txtComment.Text.Trim(),
                    CreatedAt = now,
                    UpdatedAt = null,
                    CheckedAt = _pendingCheckedAt
                };

                _rules.Add(newRule);
                _selectedRule = newRule;

                AddLog($"Создана новая запись: {type} {target}.");
            }
            else
            {
                _selectedRule.Target = target;
                _selectedRule.Type = type;
                _selectedRule.Ips = ips;
                _selectedRule.Gateway = gateway;
                _selectedRule.Comment = txtComment.Text.Trim();
                _selectedRule.UpdatedAt = now;
                _selectedRule.CheckedAt = _pendingCheckedAt;

                AddLog($"Обновлена запись: {type} {target}.");
            }

            if (!SaveRulesToDisk())
            {
                return;
            }

            RefreshGridAndSelection();
            SetStatus("Изменения сохранены");

            if (type.Equals("Domain", StringComparison.OrdinalIgnoreCase))
            {
                AddLog($"Сохранена Domain-запись {target} с IP: {string.Join(", ", ips)}");
            }
        }

        private bool SaveRulesToDisk()
        {
            try
            {
                var directory = Path.GetDirectoryName(_jsonPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_rules, JsonOptions);
                File.WriteAllText(_jsonPath, json);
                AddLog($"JSON сохранён: {_jsonPath}");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи JSON:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Ошибка записи JSON");
                AddLog($"Ошибка записи JSON: {ex.Message}");
                return false;
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedRule == null)
            {
                MessageBox.Show("Сначала выберите запись для удаления.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("Удаление отменено: запись не выбрана");
                AddLog("Удаление отменено: запись не выбрана.");
                return;
            }

            var confirmation = MessageBox.Show(
                $"Удалить запись '{_selectedRule.Target}'?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                SetStatus("Удаление отменено пользователем");
                AddLog("Удаление отменено пользователем.");
                return;
            }

            var removedTarget = _selectedRule.Target;
            _rules.Remove(_selectedRule);
            _selectedRule = null;
            _pendingCheckedAt = null;

            if (!SaveRulesToDisk())
            {
                return;
            }

            RefreshGridAndSelection();
            ClearDetailsFields(false);
            SetStatus("Запись удалена");
            AddLog($"Запись удалена: {removedTarget}");
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadRulesFromDisk();
            SetStatus("Список обновлён из файла");
            AddLog("Выполнено обновление списка из JSON.");
        }

        private async void BtnCheckDomain_Click(object? sender, EventArgs e)
        {
            var type = (cmbType.SelectedItem?.ToString() ?? string.Empty).Trim();
            var target = txtTarget.Text.Trim();

            if (!type.Equals("Domain", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Проверка домена доступна только для записей типа Domain.", "Проверка домена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("Проверка домена отменена: тип не Domain");
                AddLog("Проверка домена отменена: тип записи не Domain.");
                return;
            }

            if (string.IsNullOrWhiteSpace(target) || Uri.CheckHostName(target) != UriHostNameType.Dns)
            {
                MessageBox.Show("Введите корректный домен в поле Target.", "Проверка домена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Проверка домена отменена: некорректный Target");
                AddLog("Проверка домена отменена: Target пустой или некорректный.");
                return;
            }

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(target);
                var uniqueIpv4 = addresses
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (uniqueIpv4.Count == 0)
                {
                    MessageBox.Show("IPv4-адреса для домена не найдены.", "Проверка домена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus("Проверка домена: IPv4 не найден");
                    AddLog($"Для домена {target} IPv4-адреса не найдены.");
                    return;
                }

                var ipListAsText = string.Join(", ", uniqueIpv4);
                txtIps.Text = ipListAsText;
                _pendingCheckedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (_selectedRule != null)
                {
                    _selectedRule.Ips = uniqueIpv4;
                    _selectedRule.CheckedAt = _pendingCheckedAt;
                    RefreshGridAndSelection();
                }

                var statusWithIps = uniqueIpv4.Count == 1
                    ? $"Проверка домена: {uniqueIpv4[0]}"
                    : $"Проверка домена: {uniqueIpv4[0]} (+{uniqueIpv4.Count - 1})";

                SetStatus(statusWithIps);
                AddLog($"Проверка домена {target}: {ipListAsText}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка DNS-запроса:\n{ex.Message}", "Проверка домена", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Ошибка проверки домена");
                AddLog($"Ошибка DNS-проверки для {target}: {ex.Message}");
            }
        }

        private static bool TryResolveDomainIpv4(string domain, out List<string> ips, out string errorMessage)
        {
            ips = new List<string>();
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(domain) || Uri.CheckHostName(domain) != UriHostNameType.Dns)
            {
                errorMessage = "Введите корректное доменное имя в Target.";
                return false;
            }

            try
            {
                ips = Dns.GetHostAddresses(domain)
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (ips.Count == 0)
                {
                    errorMessage = "DNS не вернул IPv4-адреса для домена.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Ошибка DNS: {ex.Message}";
                return false;
            }
        }

        private void BtnApplyRoutes_Click(object? sender, EventArgs e)
        {
            const string message = "Системное применение маршрутов будет реализовано на следующем этапе.";
            MessageBox.Show(message, "Пока недоступно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SetStatus("Применение маршрутов пока недоступно");
            AddLog(message);
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = message;
        }

        private void AddLog(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            rtbLog.AppendText(line + Environment.NewLine);
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }
    }
}
