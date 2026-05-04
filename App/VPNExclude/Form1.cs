using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace VPNExclude
{
    public partial class Form1 : Form
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static readonly JsonSerializerOptions RouteJsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly List<ExclusionRule> _rules = new();
        private readonly List<SystemRouteInfo> _systemRoutes = new();
        private readonly string _jsonPath;
        private ExclusionRule? _selectedRule;
        private string? _pendingCheckedAt;
        private bool _isInternalSelectionChange;

        private sealed class SystemRouteInfo
        {
            public string Ip { get; set; } = string.Empty;
            public string Gateway { get; set; } = string.Empty;
            public string Interface { get; set; } = string.Empty;
            public string Metric { get; set; } = string.Empty;
            public bool Active { get; set; }
            public bool Persistent { get; set; }
        }
        private sealed class PhysicalInterfaceInfo
        {
            public int InterfaceIndex { get; set; }
            public string InterfaceAlias { get; set; } = string.Empty;
            public string Gateway { get; set; } = string.Empty;
            [JsonPropertyName("LocalIPv4")]
            public string LocalIpv4 { get; set; } = string.Empty;
            [JsonPropertyName("InterfaceDescription")]
            public string Description { get; set; } = string.Empty;
        }

        public Form1()
        {
            InitializeComponent();

            _jsonPath = ResolveJsonPath();

            dgvRules.SelectionChanged += DgvRules_SelectionChanged;
            btnAddDomain.Click += BtnAddDomain_Click;
            btnAddIp.Click += BtnAddIp_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnCheckDomain.Click += BtnCheckDomain_Click;
            btnApplyRoutes.Click += BtnApplyRoutes_Click;
            btnLoadJson.Click += BtnLoadJson_Click;
            btnSaveJson.Click += BtnSaveJson_Click;
            btnLoadSystemRoutes.Click += BtnLoadSystemRoutes_Click;
            btnCompareRoutes.Click += BtnCompareRoutes_Click;
            btnRefreshSystemRoutes.Click += BtnLoadSystemRoutes_Click;

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
                    rule.CheckedAt,
                    IsRuleFullyInSystem(rule));

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

        private void SwitchToRecordsTab()
        {
            if (tabControlMain.SelectedTab != tabRecords)
            {
                tabControlMain.SelectedTab = tabRecords;
            }
        }

        private void BtnAddDomain_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
            BeginAddMode("Domain");
        }

        private void BtnAddIp_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
            BeginAddMode("IP");
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

                var existingIps = new List<string>();
                if (_selectedRule != null)
                {
                    existingIps.AddRange(_selectedRule.Ips);
                }

                ips = MergeIpLists(existingIps, resolvedIps);
                txtIps.Text = string.Join(", ", ips);
                _pendingCheckedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                AddLog($"Автоподстановка IP для домена {target}. DNS вернул: {string.Join(", ", resolvedIps)}");
                AddLog($"Итоговый список IP для сохранения: {txtIps.Text}");
            }

            if (type.Equals("IP", StringComparison.OrdinalIgnoreCase) && ips.Count == 0)
            {
                ips.Add(target);
            }

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var gateway = txtGateway.Text.Trim();

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

        private void BtnSaveJson_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
            using var saveDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Экспорт настроек в JSON",
                FileName = "vpnexclude.json",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (saveDialog.ShowDialog(this) != DialogResult.OK)
            {
                SetStatus("Экспорт JSON отменён пользователем");
                AddLog("Экспорт JSON отменён пользователем.");
                return;
            }

            try
            {
                var exportJson = JsonSerializer.Serialize(_rules, JsonOptions);
                File.WriteAllText(saveDialog.FileName, exportJson);

                AddLog($"Экспортировано {_rules.Count} записей в файл {saveDialog.FileName}");
                SetStatus($"Экспорт JSON завершён: {saveDialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта JSON:\n{ex.Message}", "Экспорт JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Ошибка экспорта JSON");
                AddLog($"Ошибка экспорта JSON: {ex.Message}");
            }
        }

        private void BtnLoadJson_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
            var confirmation = MessageBox.Show(
                "Выбранный JSON заменит текущие записи приложения. Продолжить?",
                "Подтверждение импорта JSON",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmation != DialogResult.Yes)
            {
                SetStatus("Импорт JSON отменён пользователем");
                AddLog("Импорт JSON отменён пользователем.");
                return;
            }

            using var openDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Импорт настроек из JSON",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog(this) != DialogResult.OK)
            {
                SetStatus("Импорт JSON отменён: файл не выбран");
                AddLog("Импорт JSON отменён: файл не выбран.");
                return;
            }

            try
            {
                var importJson = File.ReadAllText(openDialog.FileName);
                var loadedRules = JsonSerializer.Deserialize<List<ExclusionRule>>(importJson, JsonOptions);

                if (loadedRules == null)
                {
                    MessageBox.Show("Выбранный JSON не содержит список записей в ожидаемом формате.", "Импорт JSON", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SetStatus("Импорт JSON отменён: невалидный формат");
                    AddLog("Импорт JSON отменён: JSON не содержит валидный список записей.");
                    return;
                }

                _rules.Clear();
                _rules.AddRange(loadedRules);
                _selectedRule = null;
                _pendingCheckedAt = null;

                if (!SaveRulesToDisk())
                {
                    return;
                }

                RefreshGridAndSelection();
                ClearDetailsFields(false);

                if (_systemRoutes.Count > 0)
                {
                    PopulateSystemRoutesGrid(includeComparison: false);
                }

                AddLog($"Импортировано {_rules.Count} записей из файла {openDialog.FileName}");
                AddLog("Основной JSON приложения обновлён после импорта");
                SetStatus($"Импорт JSON завершён: {_rules.Count} записей");
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка формата JSON:\n{ex.Message}", "Импорт JSON", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Импорт JSON отменён: ошибка формата");
                AddLog($"Импорт JSON отменён: ошибка формата JSON ({ex.Message}).");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта JSON:\n{ex.Message}", "Импорт JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Ошибка импорта JSON");
                AddLog($"Ошибка импорта JSON: {ex.Message}");
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
            if (_selectedRule == null)
            {
                MessageBox.Show("Сначала выберите запись для удаления.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("Удаление отменено: запись не выбрана");
                AddLog("Удаление отменено: запись не выбрана.");
                return;
            }

            var ruleToDelete = _selectedRule;
            var removedTarget = ruleToDelete.Target;
            var removedType = ruleToDelete.Type;
            var gateway = ruleToDelete.Gateway.Trim();
            var ipsToProcess = NormalizeIpv4List(ruleToDelete.Ips);

            var confirmation = MessageBox.Show(
                $"Удалить запись '{removedTarget}' и связанные host-маршруты из Windows?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                SetStatus("Удаление отменено пользователем");
                AddLog("Удаление отменено пользователем.");
                return;
            }

            AddLog($"Начато удаление записи {removedTarget} ({removedType}).");

            if (ipsToProcess.Count > 0 && !IsRunningAsAdministrator())
            {
                const string message = "Для удаления записи с системными маршрутами нужно запустить приложение от имени администратора.";
                MessageBox.Show(message, "Недостаточно прав", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Удаление отменено: нет прав администратора");
                AddLog(message);
                return;
            }

            var deletedRoutes = 0;
            var alreadyMissingRoutes = 0;
            var skippedSharedRoutes = 0;
            var routeDeletionErrors = new List<string>();

            foreach (var ip in ipsToProcess)
            {
                if (IsIpUsedByOtherRules(ip, ruleToDelete))
                {
                    skippedSharedRoutes++;
                    AddLog($"Маршрут сохранён: {ip} используется другой записью JSON.");
                    continue;
                }

                AddLog(string.IsNullOrWhiteSpace(gateway)
                    ? $"Удаление маршрута: {ip} (без явного Gateway)"
                    : $"Удаление маршрута: {ip} -> {gateway}");

                var alreadyAbsent = false;
                string error;
                var deleteSucceeded = IsValidIpv4(gateway)
                    ? TryDeleteHostRoute(ip, gateway, out alreadyAbsent, out error)
                    : TryDeleteRouteByIp(ip, out error);

                if (deleteSucceeded)
                {
                    if (IsValidIpv4(gateway) && alreadyAbsent)
                    {
                        alreadyMissingRoutes++;
                        AddLog($"Маршрут уже отсутствует: {ip}");
                    }
                    else
                    {
                        deletedRoutes++;
                        AddLog($"Маршрут удалён: {ip}");
                    }
                }
                else
                {
                    routeDeletionErrors.Add($"{ip}: {error}");
                    AddLog($"Ошибка удаления маршрута {ip}: {error}");
                }
            }

            if (routeDeletionErrors.Count > 0)
            {
                var errorText = string.Join(Environment.NewLine, routeDeletionErrors);
                MessageBox.Show(
                    $"Не удалось удалить все связанные системные маршруты. Запись не удалена.{Environment.NewLine}{errorText}",
                    "Удаление",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetStatus("Удаление отменено: ошибка удаления системных маршрутов");
                AddLog($"Удаление записи отменено из-за ошибок удаления маршрутов: {removedTarget}");
                return;
            }

            _rules.Remove(ruleToDelete);
            _selectedRule = null;
            _pendingCheckedAt = null;

            if (!SaveRulesToDisk())
            {
                return;
            }

            AddLog($"Запись удалена из JSON: {removedTarget}");

            try
            {
                _systemRoutes.Clear();
                _systemRoutes.AddRange(LoadSystemHostRoutes());
                PopulateSystemRoutesGrid(includeComparison: false);
                LogRulesInSystemSummary();
            }
            catch (Exception ex)
            {
                AddLog($"Не удалось обновить таблицу системных маршрутов после удаления: {ex.Message}");
            }

            RefreshGridAndSelection();
            ClearDetailsFields(false);
            SetStatus($"Синхронизация завершена: удалено маршрутов {deletedRoutes}, уже отсутствовали {alreadyMissingRoutes}, сохранено (shared) {skippedSharedRoutes}");
            AddLog($"Синхронизация завершена: удалено маршрутов {deletedRoutes}, уже отсутствовали {alreadyMissingRoutes}, сохранено shared {skippedSharedRoutes}");
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
            LoadRulesFromDisk();
            SetStatus("Список обновлён из файла");
            AddLog("Выполнено обновление списка из JSON.");
        }

        private async void BtnCheckDomain_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
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
                var existingIps = MergeIpLists(
                    ParseIpv4FromText(txtIps.Text),
                    _selectedRule == null ? new List<string>() : _selectedRule.Ips);
                var mergedIps = MergeIpLists(existingIps, uniqueIpv4);
                var addedIps = mergedIps
                    .Except(existingIps, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                txtIps.Text = string.Join(", ", mergedIps);
                _pendingCheckedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (_selectedRule != null)
                {
                    _selectedRule.Ips = mergedIps;
                    _selectedRule.CheckedAt = _pendingCheckedAt;
                    RefreshGridAndSelection();
                }

                var statusWithIps = addedIps.Count == 0
                    ? "Проверка домена: новых IP не найдено"
                    : $"Проверка домена: добавлено {addedIps.Count} IP";

                SetStatus(statusWithIps);
                AddLog($"Проверка домена {target}. Текущие IP: {FormatIpList(existingIps)}");
                AddLog($"DNS вернул: {ipListAsText}");
                AddLog(addedIps.Count == 0
                    ? "Новых IP не найдено. Список не изменился."
                    : $"Добавлены новые IP: {string.Join(", ", addedIps)}");
                AddLog($"Итоговый список IP: {txtIps.Text}");
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

        private void BtnLoadSystemRoutes_Click(object? sender, EventArgs e)
        {
            try
            {
                _systemRoutes.Clear();
                _systemRoutes.AddRange(LoadSystemHostRoutes());
                PopulateSystemRoutesGrid(includeComparison: false);
                RefreshGridAndSelection();
                LogRulesInSystemSummary();

                SetStatus($"Загружено {_systemRoutes.Count} системных host-маршрутов");
                AddLog($"Загружено {_systemRoutes.Count} системных host-маршрутов.");
                var gatewayCount = _systemRoutes.Count(route => string.Equals(route.Gateway, "192.168.1.1", StringComparison.OrdinalIgnoreCase));
                AddLog($"Найдено {gatewayCount} маршрутов через 192.168.1.1.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки системных маршрутов:\n{ex.Message}", "Системные маршруты", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Ошибка загрузки системных маршрутов");
                AddLog($"Ошибка загрузки системных маршрутов: {ex.Message}");
            }
        }

        private void BtnCompareRoutes_Click(object? sender, EventArgs e)
        {
            if (_systemRoutes.Count == 0)
            {
                _systemRoutes.AddRange(LoadSystemHostRoutes());
            }

            PopulateSystemRoutesGrid(includeComparison: true);
            RefreshGridAndSelection();
            LogRulesInSystemSummary();

            var routesInJson = _systemRoutes.Count(route => FindJsonTargetsByIp(route.Ip).Count > 0);
            var routesOnlyInSystem = _systemRoutes.Count - routesInJson;
            var jsonOnlyIps = GetJsonIpMap().Keys.Count(ip => !_systemRoutes.Any(route => string.Equals(route.Ip, ip, StringComparison.OrdinalIgnoreCase)));

            SetStatus($"Сравнение завершено: совпало {routesInJson}, только в системе {routesOnlyInSystem}, только в JSON {jsonOnlyIps}");
            AddLog($"Найдено {routesInJson} маршрутов, совпадающих с JSON.");
            AddLog($"В системе есть {routesOnlyInSystem} маршрутов, которых нет в JSON.");
            AddLog($"В JSON есть {jsonOnlyIps} IP, которых нет в системе.");
        }

        private List<SystemRouteInfo> LoadSystemHostRoutes()
        {
            const string activeQuery = "Get-NetRoute -AddressFamily IPv4 -PolicyStore ActiveStore | Select-Object ifIndex,DestinationPrefix,NextHop,RouteMetric,ifMetric,InterfaceAlias | ConvertTo-Json -Depth 3";
            const string persistentQuery = "Get-NetRoute -AddressFamily IPv4 -PolicyStore PersistentStore | Select-Object ifIndex,DestinationPrefix,NextHop,RouteMetric,ifMetric,InterfaceAlias | ConvertTo-Json -Depth 3";
            const string fallbackQuery = "Get-NetRoute -AddressFamily IPv4 | Select-Object ifIndex,DestinationPrefix,NextHop,RouteMetric,ifMetric,InterfaceAlias | ConvertTo-Json -Depth 3";

            var activeResult = RunPowerShellQuery(activeQuery);
            if (activeResult.ExitCode != 0)
            {
                throw new InvalidOperationException($"ActiveStore: {GetProcessError(activeResult)}");
            }
            WriteRouteDebugOutput("get_netroute_active_debug.json", activeResult.StdOut);

            var persistentResult = RunPowerShellQuery(persistentQuery);
            if (persistentResult.ExitCode != 0)
            {
                throw new InvalidOperationException($"PersistentStore: {GetProcessError(persistentResult)}");
            }
            WriteRouteDebugOutput("get_netroute_persistent_debug.json", persistentResult.StdOut);

            var routes = new Dictionary<string, SystemRouteInfo>(StringComparer.OrdinalIgnoreCase);
            ParseNetRouteJsonInto(routes, activeResult.StdOut, isActive: true, isPersistent: false);
            ParseNetRouteJsonInto(routes, persistentResult.StdOut, isActive: false, isPersistent: true);

            if (routes.Count == 0)
            {
                var fallbackResult = RunPowerShellQuery(fallbackQuery);
                if (fallbackResult.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Fallback Get-NetRoute: {GetProcessError(fallbackResult)}");
                }
                WriteRouteDebugOutput("get_netroute_fallback_debug.json", fallbackResult.StdOut);
                ParseNetRouteJsonInto(routes, fallbackResult.StdOut, isActive: true, isPersistent: false);
            }

            AddLog($"Парсер JSON распознал {routes.Count} host-маршрутов.");
            return routes.Values.OrderBy(route => route.Ip, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private sealed class NetRouteJsonEntry
        {
            [JsonPropertyName("ifIndex")]
            public int? IfIndex { get; set; }

            [JsonPropertyName("DestinationPrefix")]
            public string? DestinationPrefix { get; set; }

            [JsonPropertyName("NextHop")]
            public string? NextHop { get; set; }

            [JsonPropertyName("RouteMetric")]
            public int? RouteMetric { get; set; }

            [JsonPropertyName("ifMetric")]
            public int? IfMetric { get; set; }

            [JsonPropertyName("InterfaceAlias")]
            public string? InterfaceAlias { get; set; }
        }

        private void ParseNetRouteJsonInto(
            IDictionary<string, SystemRouteInfo> routes,
            string rawJson,
            bool isActive,
            bool isPersistent)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return;
            }

            var trimmed = rawJson.Trim();
            List<NetRouteJsonEntry> entries;

            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                entries = JsonSerializer.Deserialize<List<NetRouteJsonEntry>>(trimmed, RouteJsonOptions) ?? new List<NetRouteJsonEntry>();
            }
            else
            {
                var single = JsonSerializer.Deserialize<NetRouteJsonEntry>(trimmed, RouteJsonOptions);
                entries = single == null ? new List<NetRouteJsonEntry>() : new List<NetRouteJsonEntry> { single };
            }

            foreach (var entry in entries)
            {
                if (!TryMapJsonEntryToHostRoute(entry, out var ip, out var gateway, out var iface, out var metric))
                {
                    continue;
                }

                var key = $"{ip}|{gateway}";
                if (!routes.TryGetValue(key, out var route))
                {
                    route = new SystemRouteInfo
                    {
                        Ip = ip,
                        Gateway = gateway,
                        Interface = iface,
                        Metric = metric
                    };
                    routes[key] = route;
                }

                route.Active |= isActive;
                route.Persistent |= isPersistent;

                if (string.IsNullOrWhiteSpace(route.Interface))
                {
                    route.Interface = iface;
                }

                if (string.IsNullOrWhiteSpace(route.Metric))
                {
                    route.Metric = metric;
                }
            }
        }

        private static bool TryMapJsonEntryToHostRoute(
            NetRouteJsonEntry entry,
            out string ip,
            out string gateway,
            out string iface,
            out string metric)
        {
            ip = string.Empty;
            gateway = string.Empty;
            iface = string.Empty;
            metric = string.Empty;

            if (entry.DestinationPrefix == null || entry.NextHop == null)
            {
                return false;
            }

            var prefixParts = entry.DestinationPrefix.Split('/');
            if (prefixParts.Length != 2 || prefixParts[1] != "32")
            {
                return false;
            }

            var destinationIp = prefixParts[0].Trim();
            var nextHop = entry.NextHop.Trim();

            if (!IsValidIpv4(destinationIp) || !IsValidIpv4(nextHop))
            {
                return false;
            }

            if (nextHop == "0.0.0.0" || destinationIp.StartsWith("127.", StringComparison.Ordinal) || nextHop.StartsWith("127.", StringComparison.Ordinal))
            {
                return false;
            }

            ip = destinationIp;
            gateway = nextHop;
            iface = string.IsNullOrWhiteSpace(entry.InterfaceAlias) ? $"ifIndex:{entry.IfIndex}" : entry.InterfaceAlias.Trim();
            metric = entry.RouteMetric?.ToString() ?? entry.IfMetric?.ToString() ?? string.Empty;
            return true;
        }

        private void PopulateSystemRoutesGrid(bool includeComparison)
        {
            dgvSystemRoutes.Rows.Clear();
            var jsonIpMap = includeComparison ? GetJsonIpMap() : new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var systemIpSet = new HashSet<string>(_systemRoutes.Select(route => route.Ip), StringComparer.OrdinalIgnoreCase);

            foreach (var route in _systemRoutes)
            {
                var targets = includeComparison && jsonIpMap.TryGetValue(route.Ip, out var mappedTargets)
                    ? mappedTargets
                    : new List<string>();

                var inJson = targets.Count > 0;
                var status = includeComparison
                    ? (inJson ? "Совпадает" : "Только в системе")
                    : route.Active && route.Persistent ? "Активный + постоянный" : route.Active ? "Активный" : "Постоянный";

                dgvSystemRoutes.Rows.Add(
                    route.Ip,
                    route.Gateway,
                    route.Interface,
                    route.Metric,
                    route.Active,
                    route.Persistent,
                    inJson,
                    string.Join(", ", targets),
                    status);
            }

            if (includeComparison)
            {
                foreach (var jsonIp in jsonIpMap.Keys.Where(ip => !systemIpSet.Contains(ip)))
                {
                    var targets = jsonIpMap[jsonIp];
                    dgvSystemRoutes.Rows.Add(
                        jsonIp,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        false,
                        false,
                        true,
                        string.Join(", ", targets),
                        "Только в JSON");
                }
            }
        }

        private Dictionary<string, List<string>> GetJsonIpMap()
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in _rules)
            {
                var ips = NormalizeIpv4List(rule.Ips);
                foreach (var ip in ips)
                {
                    if (!map.TryGetValue(ip, out var targets))
                    {
                        targets = new List<string>();
                        map[ip] = targets;
                    }

                    if (!targets.Contains(rule.Target, StringComparer.OrdinalIgnoreCase))
                    {
                        targets.Add(rule.Target);
                    }
                }
            }

            return map;
        }

        private List<string> FindJsonTargetsByIp(string ip)
        {
            var map = GetJsonIpMap();
            return map.TryGetValue(ip, out var targets) ? targets : new List<string>();
        }

        private bool IsRuleFullyInSystem(ExclusionRule rule)
        {
            var ruleIps = NormalizeIpv4List(rule.Ips);
            if (ruleIps.Count == 0)
            {
                return false;
            }

            var systemIpSet = new HashSet<string>(_systemRoutes.Select(route => route.Ip), StringComparer.OrdinalIgnoreCase);
            return ruleIps.All(ip => systemIpSet.Contains(ip));
        }

        private void LogRulesInSystemSummary()
        {
            var confirmed = _rules.Count(IsRuleFullyInSystem);
            var missing = _rules.Count - confirmed;
            AddLog($"Для {confirmed} записей подтверждено наличие маршрутов в системе.");
            AddLog($"Для {missing} записей маршруты в системе отсутствуют полностью или частично.");
        }

        private void BtnApplyRoutes_Click(object? sender, EventArgs e)
        {
            SwitchToRecordsTab();
            if (_selectedRule == null)
            {
                MessageBox.Show("Сначала выберите запись в таблице.", "Применить маршруты", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("Применение маршрутов отменено: запись не выбрана");
                AddLog("Применение маршрутов отменено: запись не выбрана.");
                return;
            }

            var normalizedIps = NormalizeIpv4List(_selectedRule.Ips);
            if (normalizedIps.Count == 0)
            {
                MessageBox.Show("В выбранной записи нет валидных IPv4 для применения маршрутов.", "Применить маршруты", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Применение маршрутов отменено: IP отсутствуют");
                AddLog($"Применение маршрутов отменено для {_selectedRule.Target}: валидные IP не найдены.");
                return;
            }

            if (!IsRunningAsAdministrator())
            {
                const string message = "Для применения маршрутов нужно запустить приложение от имени администратора.";
                MessageBox.Show(message, "Недостаточно прав", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Применение маршрутов отменено: нет прав администратора");
                AddLog(message);
                return;
            }

            if (!TryGetPhysicalInterfaceForBypass(out var interfaceInfo, out var detectError))
            {
                MessageBox.Show($"Не удалось определить физический интерфейс для обхода VPN.\n{detectError}", "Применить маршруты", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("Применение маршрутов отменено: не найден физический интерфейс");
                AddLog($"Применение маршрутов отменено: {detectError}");
                return;
            }

            var gateway = interfaceInfo.Gateway;
            _selectedRule.Gateway = gateway;
            txtGateway.Text = gateway;
            AddLog($"Выбран интерфейс обхода VPN: {interfaceInfo.InterfaceAlias} ({interfaceInfo.Description}), IF={interfaceInfo.InterfaceIndex}, IPv4={interfaceInfo.LocalIpv4}, GW={gateway}");
            AddLog($"Применение маршрутов для {_selectedRule.Target} через IF {interfaceInfo.InterfaceIndex}");

            var added = 0;
            var existed = 0;
            var failed = 0;

            foreach (var ip in normalizedIps)
            {
                try
                {
                    var deleted = TryDeleteRouteByIp(ip, out var deleteError);
                    if (!deleted)
                    {
                        failed++;
                        AddLog($"Ошибка удаления старого маршрута {ip}: {deleteError}");
                        continue;
                    }

                    var addArgs = $"-p add {ip} mask 255.255.255.255 {gateway} metric 1 IF {interfaceInfo.InterfaceIndex}";
                    AddLog($"Команда: route.exe {addArgs}");
                    var addResult = RunProcess("route.exe", addArgs);
                    if (addResult.ExitCode == 0)
                    {
                        if (RouteExists(ip, gateway, interfaceInfo.InterfaceIndex))
                        {
                            added++;
                            AddLog($"Маршрут добавлен: {ip} -> {gateway} IF {interfaceInfo.InterfaceIndex}");
                            continue;
                        }

                        failed++;
                        AddLog($"Маршрут {ip} добавлен, но проверка IF не пройдена.");
                        continue;
                    }

                    if (RouteExists(ip, gateway, interfaceInfo.InterfaceIndex))
                    {
                        added++;
                        existed++;
                        AddLog($"Маршрут уже присутствует в нужном виде: {ip} -> {gateway} IF {interfaceInfo.InterfaceIndex}");
                        continue;
                    }

                    failed++;
                    var errorText = string.IsNullOrWhiteSpace(addResult.StdErr)
                        ? addResult.StdOut
                        : addResult.StdErr;
                    AddLog($"Ошибка добавления маршрута: {ip} -> {errorText.Trim()}");
                }
                catch (Exception ex)
                {
                    failed++;
                    AddLog($"Ошибка добавления маршрута: {ip} -> {ex.Message}");
                }
            }

            SetStatus($"Маршруты применены: добавлено {added}, уже было {existed}, ошибок {failed}");

            try
            {
                _systemRoutes.Clear();
                _systemRoutes.AddRange(LoadSystemHostRoutes());
                PopulateSystemRoutesGrid(includeComparison: false);
                RefreshGridAndSelection();
                LogRulesInSystemSummary();
            }
            catch (Exception ex)
            {
                AddLog($"Не удалось обновить список системных маршрутов после применения: {ex.Message}");
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool IsValidIpv4(string value)
        {
            return IPAddress.TryParse(value, out var address) && address.AddressFamily == AddressFamily.InterNetwork;
        }

        private static List<string> ParseIpv4FromText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            var source = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return NormalizeIpv4List(source);
        }

        private static List<string> NormalizeIpv4List(IEnumerable<string> values)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in values)
            {
                var candidates = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var candidate in candidates)
                {
                    if (IPAddress.TryParse(candidate, out var address) && address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        result.Add(address.ToString());
                    }
                }
            }

            return result.ToList();
        }

        private static List<string> MergeIpLists(IEnumerable<string> primary, IEnumerable<string> secondary)
        {
            var merged = NormalizeIpv4List(primary);
            var secondaryNormalized = NormalizeIpv4List(secondary);

            foreach (var ip in secondaryNormalized)
            {
                if (!merged.Contains(ip, StringComparer.OrdinalIgnoreCase))
                {
                    merged.Add(ip);
                }
            }

            return merged;
        }

        private static string FormatIpList(IEnumerable<string> ips)
        {
            var normalized = NormalizeIpv4List(ips);
            return normalized.Count == 0 ? "(пусто)" : string.Join(", ", normalized);
        }

        private bool RouteExists(string ip, string gateway, int interfaceIndex)
        {
            var printResult = RunProcess("route.exe", "print -4");
            if (printResult.ExitCode != 0)
            {
                return false;
            }

            var normalizedOutput = printResult.StdOut.Replace("\r", string.Empty);
            var pattern = $@"^\s*{Regex.Escape(ip)}\s+255\.255\.255\.255\s+{Regex.Escape(gateway)}\s+[0-9\.]+\s+{interfaceIndex}\s+";
            return Regex.IsMatch(normalizedOutput, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        private bool TryDeleteRouteByIp(string ip, out string error)
        {
            error = string.Empty;
            var deleteResult = RunProcess("route.exe", $"delete {ip}");
            if (deleteResult.ExitCode == 0)
            {
                AddLog($"Старый маршрут удалён (если существовал): {ip}");
                return true;
            }

            var processError = GetProcessError(deleteResult);
            if (IsRouteAlreadyAbsent(processError))
            {
                AddLog($"Старый маршрут для {ip} не найден, продолжаем.");
                return true;
            }

            error = processError;
            return false;
        }

        private bool TryGetPhysicalInterfaceForBypass(out PhysicalInterfaceInfo info, out string error)
        {
            info = new PhysicalInterfaceInfo();
            error = string.Empty;
            const string query = "$cfg = Get-NetIPConfiguration | Where-Object { $_.NetAdapter.Status -eq 'Up' -and $_.IPv4DefaultGateway -ne $null -and $_.IPv4Address -ne $null } | Select-Object InterfaceAlias,InterfaceIndex,IPv4DefaultGateway,IPv4Address,NetProfile,NetAdapter; $cfg | ForEach-Object { [PSCustomObject]@{ InterfaceAlias = $_.InterfaceAlias; InterfaceIndex = $_.InterfaceIndex; Gateway = $_.IPv4DefaultGateway.NextHop; LocalIPv4 = ($_.IPv4Address | Select-Object -First 1 -ExpandProperty IPAddress); InterfaceDescription = $_.NetAdapter.InterfaceDescription; InterfaceType = $_.NetAdapter.InterfaceType; Name = $_.NetAdapter.Name } } | ConvertTo-Json -Depth 4";
            var result = RunPowerShellQuery(query);
            if (result.ExitCode != 0)
            {
                error = $"Get-NetIPConfiguration завершился с ошибкой: {GetProcessError(result)}";
                return false;
            }

            var items = ParsePhysicalInterfaces(result.StdOut);
            var filtered = items
                .Where(i => IsPreferredPhysicalInterface(i.InterfaceAlias, i.Description))
                .OrderByDescending(i => GetInterfacePriority(i.InterfaceAlias, i.Description))
                .ToList();

            var selected = filtered.FirstOrDefault();
            if (selected == null)
            {
                error = "Не найден подходящий физический интерфейс с IPv4 шлюзом.";
                return false;
            }

            info = selected;
            return true;
        }

        private static List<PhysicalInterfaceInfo> ParsePhysicalInterfaces(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<PhysicalInterfaceInfo>();
            }

            var normalized = json.TrimStart();
            return normalized.StartsWith("[", StringComparison.Ordinal)
                ? JsonSerializer.Deserialize<List<PhysicalInterfaceInfo>>(json, RouteJsonOptions) ?? new List<PhysicalInterfaceInfo>()
                : new List<PhysicalInterfaceInfo> { JsonSerializer.Deserialize<PhysicalInterfaceInfo>(json, RouteJsonOptions) ?? new PhysicalInterfaceInfo() };
        }

        private static bool IsPreferredPhysicalInterface(string alias, string description)
        {
            var text = $"{alias} {description}".ToLowerInvariant();
            var blockedTokens = new[] { "wireguard", "wg", "tap", "vpn", "tun", "virtual", "hyper-v", "loopback", "wintun" };
            return !blockedTokens.Any(text.Contains);
        }

        private static int GetInterfacePriority(string alias, string description)
        {
            var text = $"{alias} {description}".ToLowerInvariant();
            if (text.Contains("ethernet", StringComparison.Ordinal) || text.Contains("wi-fi", StringComparison.Ordinal) || text.Contains("wifi", StringComparison.Ordinal))
            {
                return 2;
            }

            return 1;
        }

        private static ProcessResult RunPowerShellQuery(string query)
        {
            var encodedCommand = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(query));
            return RunProcess("powershell.exe", $"-NoProfile -EncodedCommand {encodedCommand}");
        }

        private bool TryDeleteHostRoute(string ip, string gateway, out bool alreadyAbsent, out string error)
        {
            alreadyAbsent = false;
            error = string.Empty;

            var deleteResult = RunProcess("route.exe", $"delete {ip} mask 255.255.255.255 {gateway}");
            if (deleteResult.ExitCode == 0)
            {
                return true;
            }

            var errorText = GetProcessError(deleteResult);
            if (IsRouteAlreadyAbsent(errorText))
            {
                alreadyAbsent = true;
                return true;
            }

            error = string.IsNullOrWhiteSpace(errorText)
                ? "Неизвестная ошибка удаления маршрута."
                : errorText;
            return false;
        }

        private static bool IsRouteAlreadyAbsent(string processOutput)
        {
            if (string.IsNullOrWhiteSpace(processOutput))
            {
                return false;
            }

            var normalized = processOutput.ToLowerInvariant();
            return normalized.Contains("not found", StringComparison.Ordinal)
                || normalized.Contains("the route specified was not found", StringComparison.Ordinal)
                || normalized.Contains("элемент не найден", StringComparison.Ordinal)
                || normalized.Contains("маршрут не найден", StringComparison.Ordinal);
        }

        private bool IsIpUsedByOtherRules(string ip, ExclusionRule excludedRule)
        {
            foreach (var rule in _rules)
            {
                if (ReferenceEquals(rule, excludedRule))
                {
                    continue;
                }

                var normalizedIps = NormalizeIpv4List(rule.Ips);
                if (normalizedIps.Contains(ip, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ProcessResult RunProcess(string fileName, string arguments, int timeoutMs = 15000)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeoutMs))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // ignore kill errors for already-terminated process
                }

                var timeoutError = $"Таймаут выполнения процесса ({timeoutMs} мс): {fileName} {arguments}";
                return new ProcessResult(-1, string.Empty, timeoutError);
            }

            Task.WaitAll(new Task[] { stdOutTask, stdErrTask }, 2000);
            var stdOut = stdOutTask.IsCompletedSuccessfully ? stdOutTask.Result : string.Empty;
            var stdErr = stdErrTask.IsCompletedSuccessfully ? stdErrTask.Result : string.Empty;

            return new ProcessResult(process.ExitCode, stdOut, stdErr);
        }

        private static string GetProcessError(ProcessResult result)
        {
            return string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut.Trim() : result.StdErr.Trim();
        }

        private readonly record struct ProcessResult(int ExitCode, string StdOut, string StdErr);

        private void WriteRouteDebugOutput(string fileName, string rawOutput)
        {
            try
            {
                var debugPath = Path.Combine(AppContext.BaseDirectory, fileName);
                File.WriteAllText(debugPath, rawOutput);
                AddLog($"RAW JSON маршрутов сохранён в debug-файл: {debugPath}");
            }
            catch (Exception ex)
            {
                AddLog($"Не удалось сохранить debug-файл маршрутов: {ex.Message}");
            }
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
