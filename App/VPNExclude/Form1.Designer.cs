namespace VPNExclude
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            topPanel = new Panel();
            topButtonsLayout = new FlowLayoutPanel();
            btnAddDomain = new Button();
            btnAddIp = new Button();
            btnCheckDomain = new Button();
            btnDelete = new Button();
            btnRefresh = new Button();
            btnApplyRoutes = new Button();
            mainPanel = new Panel();
            dgvRules = new DataGridView();
            colType = new DataGridViewTextBoxColumn();
            colTarget = new DataGridViewTextBoxColumn();
            colIps = new DataGridViewTextBoxColumn();
            colGateway = new DataGridViewTextBoxColumn();
            colComment = new DataGridViewTextBoxColumn();
            colCreatedAt = new DataGridViewTextBoxColumn();
            colUpdatedAt = new DataGridViewTextBoxColumn();
            colCheckedAt = new DataGridViewTextBoxColumn();
            bottomPanel = new Panel();
            detailsAndLogLayout = new TableLayoutPanel();
            grpDetails = new GroupBox();
            detailsLayout = new TableLayoutPanel();
            lblTarget = new Label();
            txtTarget = new TextBox();
            lblType = new Label();
            cmbType = new ComboBox();
            lblIps = new Label();
            txtIps = new TextBox();
            lblGateway = new Label();
            txtGateway = new TextBox();
            lblComment = new Label();
            txtComment = new TextBox();
            btnSave = new Button();
            grpLog = new GroupBox();
            rtbLog = new RichTextBox();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            topPanel.SuspendLayout();
            topButtonsLayout.SuspendLayout();
            mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRules).BeginInit();
            bottomPanel.SuspendLayout();
            detailsAndLogLayout.SuspendLayout();
            grpDetails.SuspendLayout();
            detailsLayout.SuspendLayout();
            grpLog.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // topPanel
            // 
            topPanel.Controls.Add(topButtonsLayout);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(12, 8, 12, 8);
            topPanel.Size = new Size(1200, 52);
            topPanel.TabIndex = 0;
            // 
            // topButtonsLayout
            // 
            topButtonsLayout.Controls.Add(btnAddDomain);
            topButtonsLayout.Controls.Add(btnAddIp);
            topButtonsLayout.Controls.Add(btnCheckDomain);
            topButtonsLayout.Controls.Add(btnDelete);
            topButtonsLayout.Controls.Add(btnRefresh);
            topButtonsLayout.Controls.Add(btnApplyRoutes);
            topButtonsLayout.Dock = DockStyle.Fill;
            topButtonsLayout.Location = new Point(12, 8);
            topButtonsLayout.Name = "topButtonsLayout";
            topButtonsLayout.Size = new Size(1176, 36);
            topButtonsLayout.TabIndex = 0;
            topButtonsLayout.WrapContents = false;
            // 
            // btnAddDomain
            // 
            btnAddDomain.AutoSize = true;
            btnAddDomain.Location = new Point(0, 0);
            btnAddDomain.Margin = new Padding(0, 0, 8, 0);
            btnAddDomain.Name = "btnAddDomain";
            btnAddDomain.Size = new Size(123, 31);
            btnAddDomain.TabIndex = 0;
            btnAddDomain.Text = "Добавить домен";
            btnAddDomain.UseVisualStyleBackColor = true;
            // 
            // btnAddIp
            // 
            btnAddIp.AutoSize = true;
            btnAddIp.Location = new Point(131, 0);
            btnAddIp.Margin = new Padding(0, 0, 8, 0);
            btnAddIp.Name = "btnAddIp";
            btnAddIp.Size = new Size(95, 31);
            btnAddIp.TabIndex = 1;
            btnAddIp.Text = "Добавить IP";
            btnAddIp.UseVisualStyleBackColor = true;
            // 
            // btnCheckDomain
            // 
            btnCheckDomain.AutoSize = true;
            btnCheckDomain.Location = new Point(234, 0);
            btnCheckDomain.Margin = new Padding(0, 0, 8, 0);
            btnCheckDomain.Name = "btnCheckDomain";
            btnCheckDomain.Size = new Size(133, 31);
            btnCheckDomain.TabIndex = 2;
            btnCheckDomain.Text = "Проверить домен";
            btnCheckDomain.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.AutoSize = true;
            btnDelete.Location = new Point(375, 0);
            btnDelete.Margin = new Padding(0, 0, 8, 0);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(79, 31);
            btnDelete.TabIndex = 3;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            btnRefresh.AutoSize = true;
            btnRefresh.Location = new Point(462, 0);
            btnRefresh.Margin = new Padding(0, 0, 8, 0);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(90, 31);
            btnRefresh.TabIndex = 4;
            btnRefresh.Text = "Обновить";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnApplyRoutes
            // 
            btnApplyRoutes.AutoSize = true;
            btnApplyRoutes.Location = new Point(560, 0);
            btnApplyRoutes.Margin = new Padding(0);
            btnApplyRoutes.Name = "btnApplyRoutes";
            btnApplyRoutes.Size = new Size(147, 31);
            btnApplyRoutes.TabIndex = 5;
            btnApplyRoutes.Text = "Применить маршруты";
            btnApplyRoutes.UseVisualStyleBackColor = true;
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(dgvRules);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 52);
            mainPanel.Name = "mainPanel";
            mainPanel.Padding = new Padding(12, 0, 12, 12);
            mainPanel.Size = new Size(1200, 428);
            mainPanel.TabIndex = 1;
            // 
            // dgvRules
            // 
            dgvRules.AllowUserToAddRows = false;
            dgvRules.AllowUserToDeleteRows = false;
            dgvRules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRules.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRules.Columns.AddRange(new DataGridViewColumn[] { colType, colTarget, colIps, colGateway, colComment, colCreatedAt, colUpdatedAt, colCheckedAt });
            dgvRules.Dock = DockStyle.Fill;
            dgvRules.Location = new Point(12, 0);
            dgvRules.MultiSelect = false;
            dgvRules.Name = "dgvRules";
            dgvRules.ReadOnly = true;
            dgvRules.RowHeadersVisible = false;
            dgvRules.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRules.Size = new Size(1176, 416);
            dgvRules.TabIndex = 0;
            // 
            // colType
            // 
            colType.HeaderText = "Тип";
            colType.Name = "colType";
            colType.ReadOnly = true;
            // 
            // colTarget
            // 
            colTarget.HeaderText = "Цель";
            colTarget.Name = "colTarget";
            colTarget.ReadOnly = true;
            // 
            // colIps
            // 
            colIps.HeaderText = "IP";
            colIps.Name = "colIps";
            colIps.ReadOnly = true;
            // 
            // colGateway
            // 
            colGateway.HeaderText = "Шлюз";
            colGateway.Name = "colGateway";
            colGateway.ReadOnly = true;
            // 
            // colComment
            // 
            colComment.HeaderText = "Комментарий";
            colComment.Name = "colComment";
            colComment.ReadOnly = true;
            // 
            // colCreatedAt
            // 
            colCreatedAt.HeaderText = "Добавлено";
            colCreatedAt.Name = "colCreatedAt";
            colCreatedAt.ReadOnly = true;
            // 
            // colUpdatedAt
            // 
            colUpdatedAt.HeaderText = "Изменено";
            colUpdatedAt.Name = "colUpdatedAt";
            colUpdatedAt.ReadOnly = true;
            // 
            // colCheckedAt
            // 
            colCheckedAt.HeaderText = "Проверено";
            colCheckedAt.Name = "colCheckedAt";
            colCheckedAt.ReadOnly = true;
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(detailsAndLogLayout);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(0, 480);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(12, 0, 12, 8);
            bottomPanel.Size = new Size(1200, 180);
            bottomPanel.TabIndex = 2;
            // 
            // detailsAndLogLayout
            // 
            detailsAndLogLayout.ColumnCount = 2;
            detailsAndLogLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            detailsAndLogLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            detailsAndLogLayout.Controls.Add(grpDetails, 0, 0);
            detailsAndLogLayout.Controls.Add(grpLog, 1, 0);
            detailsAndLogLayout.Dock = DockStyle.Fill;
            detailsAndLogLayout.Location = new Point(12, 0);
            detailsAndLogLayout.Name = "detailsAndLogLayout";
            detailsAndLogLayout.RowCount = 1;
            detailsAndLogLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            detailsAndLogLayout.Size = new Size(1176, 172);
            detailsAndLogLayout.TabIndex = 0;
            // 
            // grpDetails
            // 
            grpDetails.Controls.Add(detailsLayout);
            grpDetails.Dock = DockStyle.Fill;
            grpDetails.Location = new Point(0, 0);
            grpDetails.Margin = new Padding(0, 0, 8, 0);
            grpDetails.Name = "grpDetails";
            grpDetails.Padding = new Padding(10, 8, 10, 10);
            grpDetails.Size = new Size(815, 172);
            grpDetails.TabIndex = 0;
            grpDetails.TabStop = false;
            grpDetails.Text = "Детали записи";
            // 
            // detailsLayout
            // 
            detailsLayout.ColumnCount = 4;
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            detailsLayout.Controls.Add(lblTarget, 0, 0);
            detailsLayout.Controls.Add(txtTarget, 1, 0);
            detailsLayout.Controls.Add(lblType, 2, 0);
            detailsLayout.Controls.Add(cmbType, 3, 0);
            detailsLayout.Controls.Add(lblIps, 0, 1);
            detailsLayout.Controls.Add(txtIps, 1, 1);
            detailsLayout.Controls.Add(lblGateway, 2, 1);
            detailsLayout.Controls.Add(txtGateway, 3, 1);
            detailsLayout.Controls.Add(lblComment, 0, 2);
            detailsLayout.Controls.Add(txtComment, 1, 2);
            detailsLayout.Controls.Add(btnSave, 3, 2);
            detailsLayout.Dock = DockStyle.Fill;
            detailsLayout.Location = new Point(10, 24);
            detailsLayout.Name = "detailsLayout";
            detailsLayout.RowCount = 3;
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            detailsLayout.Size = new Size(795, 138);
            detailsLayout.TabIndex = 0;
            // 
            // lblTarget
            // 
            lblTarget.Anchor = AnchorStyles.Left;
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(3, 9);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(35, 15);
            lblTarget.TabIndex = 0;
            lblTarget.Text = "Цель";
            // 
            // txtTarget
            // 
            txtTarget.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtTarget.Location = new Point(98, 5);
            txtTarget.Name = "txtTarget";
            txtTarget.Size = new Size(298, 23);
            txtTarget.TabIndex = 1;
            // 
            // lblType
            // 
            lblType.Anchor = AnchorStyles.Left;
            lblType.AutoSize = true;
            lblType.Location = new Point(402, 9);
            lblType.Name = "lblType";
            lblType.Size = new Size(26, 15);
            lblType.TabIndex = 2;
            lblType.Text = "Тип";
            // 
            // cmbType
            // 
            cmbType.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.FormattingEnabled = true;
            cmbType.Items.AddRange(new object[] { "IP", "Domain" });
            cmbType.Location = new Point(497, 5);
            cmbType.Name = "cmbType";
            cmbType.Size = new Size(295, 23);
            cmbType.TabIndex = 3;
            // 
            // lblIps
            // 
            lblIps.Anchor = AnchorStyles.Left;
            lblIps.AutoSize = true;
            lblIps.Location = new Point(3, 43);
            lblIps.Name = "lblIps";
            lblIps.Size = new Size(17, 15);
            lblIps.TabIndex = 4;
            lblIps.Text = "IP";
            // 
            // txtIps
            // 
            txtIps.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtIps.Location = new Point(98, 39);
            txtIps.Name = "txtIps";
            txtIps.Size = new Size(298, 23);
            txtIps.TabIndex = 5;
            // 
            // lblGateway
            // 
            lblGateway.Anchor = AnchorStyles.Left;
            lblGateway.AutoSize = true;
            lblGateway.Location = new Point(402, 43);
            lblGateway.Name = "lblGateway";
            lblGateway.Size = new Size(39, 15);
            lblGateway.TabIndex = 6;
            lblGateway.Text = "Шлюз";
            // 
            // txtGateway
            // 
            txtGateway.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtGateway.Location = new Point(497, 39);
            txtGateway.Name = "txtGateway";
            txtGateway.Size = new Size(295, 23);
            txtGateway.TabIndex = 7;
            // 
            // lblComment
            // 
            lblComment.Anchor = AnchorStyles.Left;
            lblComment.AutoSize = true;
            lblComment.Location = new Point(3, 77);
            lblComment.Name = "lblComment";
            lblComment.Size = new Size(77, 15);
            lblComment.TabIndex = 8;
            lblComment.Text = "Комментарий";
            // 
            // txtComment
            // 
            txtComment.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            detailsLayout.SetColumnSpan(txtComment, 2);
            txtComment.Location = new Point(98, 73);
            txtComment.Name = "txtComment";
            txtComment.Size = new Size(393, 23);
            txtComment.TabIndex = 9;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Right;
            btnSave.AutoSize = true;
            btnSave.Location = new Point(696, 71);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(96, 27);
            btnSave.TabIndex = 10;
            btnSave.Text = "Сохранить";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // grpLog
            // 
            grpLog.Controls.Add(rtbLog);
            grpLog.Dock = DockStyle.Fill;
            grpLog.Location = new Point(823, 0);
            grpLog.Margin = new Padding(0);
            grpLog.Name = "grpLog";
            grpLog.Padding = new Padding(8);
            grpLog.Size = new Size(353, 172);
            grpLog.TabIndex = 1;
            grpLog.TabStop = false;
            grpLog.Text = "Лог";
            // 
            // rtbLog
            // 
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Location = new Point(8, 24);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(337, 140);
            rtbLog.TabIndex = 0;
            rtbLog.Text = "";
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip.Location = new Point(0, 728);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1200, 22);
            statusStrip.TabIndex = 3;
            statusStrip.Text = "statusStrip";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(45, 17);
            lblStatus.Text = "Готово";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 750);
            Controls.Add(mainPanel);
            Controls.Add(bottomPanel);
            Controls.Add(statusStrip);
            Controls.Add(topPanel);
            MinimumSize = new Size(1000, 650);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VPNExclude";
            topPanel.ResumeLayout(false);
            topButtonsLayout.ResumeLayout(false);
            topButtonsLayout.PerformLayout();
            mainPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRules).EndInit();
            bottomPanel.ResumeLayout(false);
            detailsAndLogLayout.ResumeLayout(false);
            grpDetails.ResumeLayout(false);
            detailsLayout.ResumeLayout(false);
            detailsLayout.PerformLayout();
            grpLog.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel topPanel;
        private FlowLayoutPanel topButtonsLayout;
        private Button btnAddDomain;
        private Button btnAddIp;
        private Button btnCheckDomain;
        private Button btnDelete;
        private Button btnRefresh;
        private Button btnApplyRoutes;
        private Panel mainPanel;
        private DataGridView dgvRules;
        private DataGridViewTextBoxColumn colType;
        private DataGridViewTextBoxColumn colTarget;
        private DataGridViewTextBoxColumn colIps;
        private DataGridViewTextBoxColumn colGateway;
        private DataGridViewTextBoxColumn colComment;
        private DataGridViewTextBoxColumn colCreatedAt;
        private DataGridViewTextBoxColumn colUpdatedAt;
        private DataGridViewTextBoxColumn colCheckedAt;
        private Panel bottomPanel;
        private TableLayoutPanel detailsAndLogLayout;
        private GroupBox grpDetails;
        private TableLayoutPanel detailsLayout;
        private Label lblTarget;
        private TextBox txtTarget;
        private Label lblType;
        private ComboBox cmbType;
        private Label lblIps;
        private TextBox txtIps;
        private Label lblGateway;
        private TextBox txtGateway;
        private Label lblComment;
        private TextBox txtComment;
        private Button btnSave;
        private GroupBox grpLog;
        private RichTextBox rtbLog;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
    }
}
