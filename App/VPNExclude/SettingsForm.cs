using System.Net;

namespace VPNExclude
{
    public sealed class SettingsForm : Form
    {
        private readonly TextBox _txtGateway;
        private readonly TextBox _txtInterfaceAlias;
        private readonly TextBox _txtInterfaceIndex;
        private readonly TextBox _txtLocalIpv4;
        private readonly Button _btnAuto;
        private readonly Button _btnSave;
        private readonly Button _btnCancel;

        public string Gateway => _txtGateway.Text.Trim();
        public string InterfaceAlias => _txtInterfaceAlias.Text.Trim();
        public int InterfaceIndex => int.TryParse(_txtInterfaceIndex.Text.Trim(), out var value) ? value : 0;
        public string LocalIpv4 => _txtLocalIpv4.Text.Trim();

        public SettingsForm(string gateway, string alias, int index, string localIpv4)
        {
            Text = "Настройки обхода VPN";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 230);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label { Text = "Gateway", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            _txtGateway = new TextBox { Dock = DockStyle.Fill, Text = gateway };
            layout.Controls.Add(_txtGateway, 1, 0);

            layout.Controls.Add(new Label { Text = "InterfaceAlias", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            _txtInterfaceAlias = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = alias };
            layout.Controls.Add(_txtInterfaceAlias, 1, 1);

            layout.Controls.Add(new Label { Text = "InterfaceIndex", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            _txtInterfaceIndex = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = index > 0 ? index.ToString() : string.Empty };
            layout.Controls.Add(_txtInterfaceIndex, 1, 2);

            layout.Controls.Add(new Label { Text = "LocalIPv4", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            _txtLocalIpv4 = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Text = localIpv4 };
            layout.Controls.Add(_txtLocalIpv4, 1, 3);

            _btnAuto = new Button { Text = "Автоопределить", AutoSize = true };
            _btnSave = new Button { Text = "Сохранить", AutoSize = true };
            _btnCancel = new Button { Text = "Отмена", AutoSize = true };
            _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            _btnSave.Click += BtnSave_Click;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnSave);
            buttons.Controls.Add(_btnAuto);
            layout.SetColumnSpan(buttons, 2);
            layout.Controls.Add(buttons, 0, 5);

            Controls.Add(layout);

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        public void SetAutoDetected(string gateway, string alias, int index, string localIpv4)
        {
            _txtGateway.Text = gateway;
            _txtInterfaceAlias.Text = alias;
            _txtInterfaceIndex.Text = index > 0 ? index.ToString() : string.Empty;
            _txtLocalIpv4.Text = localIpv4;
        }

        public void SetAutoDetectHandler(EventHandler handler)
        {
            _btnAuto.Click += handler;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!IPAddress.TryParse(_txtGateway.Text.Trim(), out var gatewayAddress) || gatewayAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                MessageBox.Show("Gateway должен быть валидным IPv4.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(_txtInterfaceIndex.Text.Trim(), out var idx) || idx <= 0)
            {
                MessageBox.Show("InterfaceIndex должен быть больше 0.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
        }
    }
}
