namespace VPNExclude
{
    partial class StartForm
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
            layoutRoot = new TableLayoutPanel();
            lblTitle = new Label();
            btnOpenRules = new Button();
            btnVpnSelect = new Button();
            layoutRoot.SuspendLayout();
            SuspendLayout();
            // 
            // layoutRoot
            // 
            layoutRoot.ColumnCount = 3;
            layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutRoot.Controls.Add(lblTitle, 1, 0);
            layoutRoot.Controls.Add(btnOpenRules, 1, 1);
            layoutRoot.Controls.Add(btnVpnSelect, 1, 2);
            layoutRoot.Dock = DockStyle.Fill;
            layoutRoot.Location = new Point(0, 0);
            layoutRoot.Name = "layoutRoot";
            layoutRoot.RowCount = 5;
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layoutRoot.Size = new Size(500, 300);
            layoutRoot.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 204);
            lblTitle.Location = new Point(123, 69);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(254, 50);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "WireGuard";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnOpenRules
            // 
            btnOpenRules.Dock = DockStyle.Fill;
            btnOpenRules.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 204);
            btnOpenRules.Location = new Point(123, 122);
            btnOpenRules.Name = "btnOpenRules";
            btnOpenRules.Size = new Size(254, 50);
            btnOpenRules.TabIndex = 1;
            btnOpenRules.Text = "Исключение Ips/Domens";
            btnOpenRules.UseVisualStyleBackColor = true;
            // 
            // btnVpnSelect
            // 
            btnVpnSelect.Dock = DockStyle.Fill;
            btnVpnSelect.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 204);
            btnVpnSelect.Location = new Point(123, 178);
            btnVpnSelect.Name = "btnVpnSelect";
            btnVpnSelect.Size = new Size(254, 50);
            btnVpnSelect.TabIndex = 2;
            btnVpnSelect.Text = "VPN select";
            btnVpnSelect.UseVisualStyleBackColor = true;
            // 
            // StartForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(500, 300);
            Controls.Add(layoutRoot);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "StartForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VPNExclude";
            layoutRoot.ResumeLayout(false);
            layoutRoot.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel layoutRoot;
        private Label lblTitle;
        private Button btnOpenRules;
        private Button btnVpnSelect;
    }
}
