using System.Drawing;
using System.Windows.Forms;

namespace Win11Debloater;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    private Panel panelSidebar;
    private Label lblAppTitle;
    private Label lblAppSub;

    private Panel panelActions;
    private Button btnRunAll;
    private Button btnRunQuick;
    private Button btnRestart;

    private Panel panelTasksScroll;
    private FlowLayoutPanel flpTasks;

    private Panel panelLogWrapper;
    private Panel panelLogHeader;
    private Label lblLogTitle;
    private Button btnClearLog;
    private RichTextBox rtbLog;

    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;
    private ToolStripProgressBar progressBar;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        panelSidebar = new Panel();
        lblAppTitle = new Label();
        lblAppSub = new Label();

        panelActions = new Panel();
        btnRunAll = new Button();
        btnRunQuick = new Button();
        btnRestart = new Button();

        panelTasksScroll = new Panel();
        flpTasks = new FlowLayoutPanel();

        panelLogWrapper = new Panel();
        panelLogHeader = new Panel();
        lblLogTitle = new Label();
        btnClearLog = new Button();
        rtbLog = new RichTextBox();

        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        progressBar = new ToolStripProgressBar();

        SuspendLayout();
        panelSidebar.SuspendLayout();
        panelActions.SuspendLayout();
        panelLogWrapper.SuspendLayout();
        panelLogHeader.SuspendLayout();
        statusStrip.SuspendLayout();

        // ===== Sidebar esquerda =====
        panelSidebar.BackColor = Color.FromArgb(243, 243, 243);
        panelSidebar.Dock = DockStyle.Left;
        panelSidebar.Width = 360;
        panelSidebar.Padding = new Padding(20, 20, 12, 12);
        panelSidebar.Controls.Add(panelTasksScroll);
        panelSidebar.Controls.Add(btnRestart);
        panelSidebar.Controls.Add(panelActions);
        panelSidebar.Controls.Add(lblAppSub);
        panelSidebar.Controls.Add(lblAppTitle);

        lblAppTitle.Dock = DockStyle.Top;
        lblAppTitle.AutoSize = true;
        lblAppTitle.Margin = new Padding(0, 0, 0, 4);
        lblAppTitle.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
        lblAppTitle.ForeColor = Color.FromArgb(26, 26, 26);
        lblAppTitle.Text = "Win11 Optimizer";

        lblAppSub.Dock = DockStyle.Top;
        lblAppSub.AutoSize = true;
        lblAppSub.Margin = new Padding(0, 0, 0, 16);
        lblAppSub.Font = new Font("Segoe UI", 9F);
        lblAppSub.ForeColor = Color.FromArgb(110, 110, 110);
        lblAppSub.Text = "Ferramentas individuais ou pacote completo.";

        panelActions.Dock = DockStyle.Top;
        panelActions.Height = 46;
        panelActions.Margin = new Padding(0, 0, 0, 16);
        panelActions.Controls.Add(btnRunQuick);
        panelActions.Controls.Add(btnRunAll);

        btnRunAll.Location = new Point(0, 0);
        btnRunAll.Size = new Size(150, 46);
        btnRunAll.BackColor = Color.FromArgb(0, 103, 192);
        btnRunAll.FlatStyle = FlatStyle.Flat;
        btnRunAll.FlatAppearance.BorderSize = 0;
        btnRunAll.ForeColor = Color.White;
        btnRunAll.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnRunAll.Text = "⚡ Executar tudo";
        btnRunAll.Cursor = Cursors.Hand;
        btnRunAll.UseVisualStyleBackColor = false;

        btnRunQuick.Location = new Point(158, 0);
        btnRunQuick.Size = new Size(170, 46);
        btnRunQuick.BackColor = Color.FromArgb(0, 122, 110);
        btnRunQuick.FlatStyle = FlatStyle.Flat;
        btnRunQuick.FlatAppearance.BorderSize = 0;
        btnRunQuick.ForeColor = Color.White;
        btnRunQuick.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnRunQuick.Text = "🚀 Sem DISM/SFC";
        btnRunQuick.Cursor = Cursors.Hand;
        btnRunQuick.UseVisualStyleBackColor = false;

        btnRestart.Dock = DockStyle.Bottom;
        btnRestart.Height = 46;
        btnRestart.Margin = new Padding(0, 12, 0, 0);
        btnRestart.BackColor = Color.FromArgb(196, 43, 43);
        btnRestart.FlatStyle = FlatStyle.Flat;
        btnRestart.FlatAppearance.BorderSize = 0;
        btnRestart.ForeColor = Color.White;
        btnRestart.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnRestart.Text = "🔁 Reiniciar computador";
        btnRestart.Cursor = Cursors.Hand;
        btnRestart.UseVisualStyleBackColor = false;

        panelTasksScroll.Dock = DockStyle.Fill;
        panelTasksScroll.AutoScroll = true;
        panelTasksScroll.Controls.Add(flpTasks);

        flpTasks.FlowDirection = FlowDirection.TopDown;
        flpTasks.WrapContents = false;
        flpTasks.AutoSize = true;
        flpTasks.Dock = DockStyle.Top;
        flpTasks.Padding = new Padding(0, 0, 4, 0);
        flpTasks.Margin = new Padding(0);

        // ===== Área de log =====
        panelLogWrapper.Dock = DockStyle.Fill;
        panelLogWrapper.Padding = new Padding(8, 20, 20, 12);
        panelLogWrapper.Controls.Add(rtbLog);
        panelLogWrapper.Controls.Add(panelLogHeader);

        panelLogHeader.Dock = DockStyle.Top;
        panelLogHeader.Height = 36;
        panelLogHeader.Margin = new Padding(0, 0, 0, 8);
        panelLogHeader.Controls.Add(btnClearLog);
        panelLogHeader.Controls.Add(lblLogTitle);

        lblLogTitle.Dock = DockStyle.Left;
        lblLogTitle.AutoSize = true;
        lblLogTitle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        lblLogTitle.ForeColor = Color.FromArgb(70, 70, 70);
        lblLogTitle.Text = "Console de execução";
        lblLogTitle.TextAlign = ContentAlignment.MiddleLeft;

        btnClearLog.Dock = DockStyle.Right;
        btnClearLog.Width = 70;
        btnClearLog.Height = 26;
        btnClearLog.BackColor = Color.FromArgb(229, 229, 229);
        btnClearLog.FlatStyle = FlatStyle.Flat;
        btnClearLog.FlatAppearance.BorderSize = 0;
        btnClearLog.ForeColor = Color.FromArgb(60, 60, 60);
        btnClearLog.Font = new Font("Segoe UI", 8.5F);
        btnClearLog.Text = "Limpar";
        btnClearLog.Cursor = Cursors.Hand;
        btnClearLog.UseVisualStyleBackColor = false;

        rtbLog.Dock = DockStyle.Fill;
        rtbLog.BackColor = Color.FromArgb(30, 30, 30);
        rtbLog.ForeColor = Color.FromArgb(220, 220, 220);
        rtbLog.BorderStyle = BorderStyle.None;
        rtbLog.Font = new Font("Cascadia Mono", 9.5F);
        rtbLog.ReadOnly = true;
        rtbLog.Padding = new Padding(16, 12, 16, 12);

        // ===== Barra de status =====
        statusStrip.BackColor = Color.FromArgb(236, 236, 236);
        statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, progressBar });

        lblStatus.ForeColor = Color.FromArgb(90, 90, 90);
        lblStatus.Text = "Pronto";

        progressBar.Alignment = ToolStripItemAlignment.Right;
        progressBar.Width = 180;
        progressBar.Margin = new Padding(8, 5, 12, 5);
        progressBar.Style = ProgressBarStyle.Continuous;

        // ===== Janela principal =====
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(243, 243, 243);
        ClientSize = new Size(1020, 660);
        Controls.Add(panelLogWrapper);
        Controls.Add(panelSidebar);
        Controls.Add(statusStrip);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Win11 Debloater & Optimizer";
        Font = new Font("Segoe UI", 9F);

        panelActions.ResumeLayout(false);
        panelSidebar.ResumeLayout(false);
        panelSidebar.PerformLayout();
        panelLogHeader.ResumeLayout(false);
        panelLogHeader.PerformLayout();
        panelLogWrapper.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}