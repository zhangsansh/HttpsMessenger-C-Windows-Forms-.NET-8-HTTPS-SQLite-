namespace HttpsMessenger;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem menuHelp = null!;
    private ToolStripMenuItem menuOpenHelp = null!;
    private ToolStripMenuItem menuAbout = null!;
    private ToolStripMenuItem menuLanguage = null!;
    private ToolStripMenuItem menuLangZh = null!;
    private ToolStripMenuItem menuLangEn = null!;
    private ToolStripMenuItem menuLangBi = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblMessageCount = null!;
    private ToolStripStatusLabel lblServerStatus = null!;
    private ToolStripStatusLabel lblDbPath = null!;

    private TabControl tabMain = null!;
    private TabPage tabComm = null!;
    private TabPage tabRules = null!;
    private TabPage tabHistory = null!;
    private TabPage tabLog = null!;

    private TableLayoutPanel tblComm = null!;
    private TableLayoutPanel tblTop = null!;
    private SplitContainer splitBottom = null!;
    private SplitContainer splitMessages = null!;

    private GroupBox grpServer = null!;
    private Label lblLocalName = null!;
    private TextBox txtLocalName = null!;
    private Label lblListenPort = null!;
    private TextBox txtListenPort = null!;
    private Button btnStartServer = null!;
    private Button btnStopServer = null!;
    private Button btnRegenCert = null!;
    private Label lblCertTitle = null!;
    private TextBox txtCertInfo = null!;

    private GroupBox grpPeer = null!;
    private Label lblPeerUrl = null!;
    private TextBox txtPeerUrl = null!;
    private CheckBox chkIgnoreCert = null!;
    private CheckBox chkAutoReply = null!;
    private Button btnPing = null!;
    private Label lblReqField = null!;
    private TextBox txtReqField = null!;
    private Label lblReqValue = null!;
    private TextBox txtReqValue = null!;

    private GroupBox grpSend = null!;
    private RichTextBox rtbSend = null!;
    private TextBox txtInput = null!;
    private Button btnSend = null!;
    private Button btnClearSend = null!;
    private Panel pnlSendBottom = null!;

    private GroupBox grpRecv = null!;
    private RichTextBox rtbRecv = null!;
    private Button btnClearRecv = null!;
    private Panel pnlRecvBottom = null!;

    private GroupBox grpProcess = null!;
    private RichTextBox rtbProcess = null!;
    private Label lblResponseTitle = null!;
    private RichTextBox rtbResponse = null!;
    private Button btnOpenHelp = null!;
    private Button btnExportMessages = null!;
    private Panel pnlProcessBottom = null!;
    private SplitContainer splitProcess = null!;

    private DataGridView dgvRules = null!;
    private Button btnRuleAdd = null!;
    private Button btnRuleEdit = null!;
    private Button btnRuleDelete = null!;
    private Button btnRuleRefresh = null!;
    private Label lblRuleHint = null!;
    private Panel pnlRulesBottom = null!;

    private SplitContainer splitHistory = null!;
    private ListBox lstHistory = null!;
    private RichTextBox rtbHistoryDetail = null!;
    private Button btnHistoryRefresh = null!;
    private Button btnHistoryClear = null!;
    private Panel pnlHistoryBottom = null!;

    private RichTextBox rtbLog = null!;
    private ComboBox cmbLogLevel = null!;
    private Label lblLogLevel = null!;
    private Button btnLogRefresh = null!;
    private Button btnLogClear = null!;
    private Button btnLogExport = null!;
    private Button btnLogOpenFolder = null!;
    private Label lblLogPath = null!;
    private Panel pnlLogTop = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        menuStrip = new MenuStrip();
        menuLanguage = new ToolStripMenuItem();
        menuLangZh = new ToolStripMenuItem();
        menuLangEn = new ToolStripMenuItem();
        menuLangBi = new ToolStripMenuItem();
        menuHelp = new ToolStripMenuItem();
        menuOpenHelp = new ToolStripMenuItem();
        menuAbout = new ToolStripMenuItem();
        statusStrip = new StatusStrip();
        lblServerStatus = new ToolStripStatusLabel();
        lblMessageCount = new ToolStripStatusLabel();
        lblDbPath = new ToolStripStatusLabel();

        tabMain = new TabControl();
        tabComm = new TabPage();
        tabRules = new TabPage();
        tabHistory = new TabPage();
        tabLog = new TabPage();

        tblComm = new TableLayoutPanel();
        tblTop = new TableLayoutPanel();
        splitBottom = new SplitContainer();
        splitMessages = new SplitContainer();

        grpServer = new GroupBox();
        lblLocalName = new Label();
        txtLocalName = new TextBox();
        lblListenPort = new Label();
        txtListenPort = new TextBox();
        btnStartServer = new Button();
        btnStopServer = new Button();
        btnRegenCert = new Button();
        lblCertTitle = new Label();
        txtCertInfo = new TextBox();

        grpPeer = new GroupBox();
        lblPeerUrl = new Label();
        txtPeerUrl = new TextBox();
        chkIgnoreCert = new CheckBox();
        chkAutoReply = new CheckBox();
        btnPing = new Button();
        lblReqField = new Label();
        txtReqField = new TextBox();
        lblReqValue = new Label();
        txtReqValue = new TextBox();

        grpSend = new GroupBox();
        rtbSend = new RichTextBox();
        txtInput = new TextBox();
        btnSend = new Button();
        btnClearSend = new Button();
        pnlSendBottom = new Panel();

        grpRecv = new GroupBox();
        rtbRecv = new RichTextBox();
        btnClearRecv = new Button();
        pnlRecvBottom = new Panel();

        grpProcess = new GroupBox();
        rtbProcess = new RichTextBox();
        lblResponseTitle = new Label();
        rtbResponse = new RichTextBox();
        btnOpenHelp = new Button();
        btnExportMessages = new Button();
        pnlProcessBottom = new Panel();
        splitProcess = new SplitContainer();

        dgvRules = new DataGridView();
        btnRuleAdd = new Button();
        btnRuleEdit = new Button();
        btnRuleDelete = new Button();
        btnRuleRefresh = new Button();
        lblRuleHint = new Label();
        pnlRulesBottom = new Panel();

        splitHistory = new SplitContainer();
        lstHistory = new ListBox();
        rtbHistoryDetail = new RichTextBox();
        btnHistoryRefresh = new Button();
        btnHistoryClear = new Button();
        pnlHistoryBottom = new Panel();

        rtbLog = new RichTextBox();
        cmbLogLevel = new ComboBox();
        lblLogLevel = new Label();
        btnLogRefresh = new Button();
        btnLogClear = new Button();
        btnLogExport = new Button();
        btnLogOpenFolder = new Button();
        lblLogPath = new Label();
        pnlLogTop = new Panel();

        ((System.ComponentModel.ISupportInitialize)dgvRules).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitBottom).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitMessages).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitProcess).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitHistory).BeginInit();
        SuspendLayout();

        // menu
        menuLanguage.DropDownItems.AddRange(new ToolStripItem[] { menuLangZh, menuLangEn, menuLangBi });
        menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuOpenHelp, menuAbout });
        menuStrip.Items.AddRange(new ToolStripItem[] { menuLanguage, menuHelp });
        menuOpenHelp.ShortcutKeys = Keys.F1;
        menuLangZh.Checked = true;

        // status
        statusStrip.Items.AddRange(new ToolStripItem[] { lblServerStatus, lblMessageCount, lblDbPath });
        lblServerStatus.Spring = true;
        lblServerStatus.TextAlign = ContentAlignment.MiddleLeft;

        // tabs
        tabMain.Dock = DockStyle.Fill;
        tabMain.Controls.Add(tabComm);
        tabMain.Controls.Add(tabRules);
        tabMain.Controls.Add(tabHistory);
        tabMain.Controls.Add(tabLog);
        tabComm.Text = "HTTPS 通讯";
        tabRules.Text = "请求/响应字段规则";
        tabHistory.Text = "通讯过程历史";
        tabLog.Text = "运行日志";
        tabComm.Padding = new Padding(8);
        tabRules.Padding = new Padding(8);
        tabHistory.Padding = new Padding(8);
        tabLog.Padding = new Padding(8);

        // ===== Comm page：顶部两容器固定尺寸；下方消息区可随窗口缩放 =====
        tabComm.Controls.Add(tblComm);
        tabComm.AutoScroll = true;
        tblComm.Dock = DockStyle.Fill;
        tblComm.ColumnCount = 1;
        tblComm.RowCount = 2;
        tblComm.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
        tblComm.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblComm.Padding = new Padding(6);
        tblComm.AutoScroll = true;

        // 顶部宿主：可横向滚动，但内部两个 GroupBox 尺寸固定
        var pnlTopHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        tblTop.Location = new Point(0, 0);
        tblTop.Size = new Size(1000, 208);
        tblTop.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        tblTop.ColumnCount = 2;
        tblTop.RowCount = 1;
        tblTop.ColumnStyles.Clear();
        tblTop.RowStyles.Clear();
        tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 475F));
        tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 515F));
        tblTop.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
        tblTop.Controls.Add(grpServer, 0, 0);
        tblTop.Controls.Add(grpPeer, 1, 0);
        pnlTopHost.Controls.Add(tblTop);

        // ----- 本机 HTTPS 服务：固定布局，不随窗口缩放 -----
        grpServer.Dock = DockStyle.Fill;
        grpServer.MinimumSize = new Size(460, 195);
        lblLocalName.AutoSize = true;
        lblLocalName.Location = new Point(12, 28);
        txtLocalName.Location = new Point(100, 24);
        txtLocalName.Size = new Size(150, 23);
        txtLocalName.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        lblListenPort.AutoSize = true;
        lblListenPort.Location = new Point(270, 28);
        txtListenPort.Location = new Point(320, 24);
        txtListenPort.Size = new Size(80, 23);
        txtListenPort.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnStartServer.Location = new Point(12, 56);
        btnStartServer.Size = new Size(100, 28);
        btnStopServer.Location = new Point(120, 56);
        btnStopServer.Size = new Size(100, 28);
        btnStopServer.Enabled = false;
        btnRegenCert.Location = new Point(228, 56);
        btnRegenCert.Size = new Size(120, 28);
        lblCertTitle.AutoSize = true;
        lblCertTitle.Location = new Point(12, 92);
        lblCertTitle.Text = "证书信息：";
        txtCertInfo.Location = new Point(12, 112);
        txtCertInfo.Size = new Size(440, 72);
        txtCertInfo.Multiline = true;
        txtCertInfo.ReadOnly = true;
        txtCertInfo.ScrollBars = ScrollBars.Both;
        txtCertInfo.WordWrap = true;
        txtCertInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        txtCertInfo.BackColor = Color.White;
        grpServer.Controls.AddRange(new Control[]
        {
            lblLocalName, txtLocalName, lblListenPort, txtListenPort,
            btnStartServer, btnStopServer, btnRegenCert, lblCertTitle, txtCertInfo
        });

        // ----- 对端连接与请求字段：固定布局，不随窗口缩放 -----
        grpPeer.Dock = DockStyle.Fill;
        grpPeer.MinimumSize = new Size(500, 195);
        lblPeerUrl.AutoSize = true;
        lblPeerUrl.Location = new Point(12, 24);
        txtPeerUrl.Location = new Point(12, 46);
        txtPeerUrl.Size = new Size(370, 23);
        txtPeerUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnPing.Location = new Point(390, 44);
        btnPing.Size = new Size(110, 28);
        btnPing.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        chkIgnoreCert.AutoSize = true;
        chkIgnoreCert.Location = new Point(12, 78);
        chkIgnoreCert.Checked = true;
        chkAutoReply.AutoSize = true;
        chkAutoReply.Location = new Point(160, 78);
        lblReqField.AutoSize = true;
        lblReqField.Location = new Point(12, 112);
        txtReqField.Location = new Point(140, 108);
        txtReqField.Size = new Size(140, 23);
        txtReqField.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        lblReqValue.AutoSize = true;
        lblReqValue.Location = new Point(300, 112);
        txtReqValue.Location = new Point(360, 108);
        txtReqValue.Size = new Size(140, 23);
        txtReqValue.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        grpPeer.Controls.AddRange(new Control[]
        {
            lblPeerUrl, txtPeerUrl, btnPing, chkIgnoreCert, chkAutoReply,
            lblReqField, txtReqField, lblReqValue, txtReqValue
        });

        tblComm.Controls.Add(pnlTopHost, 0, 0);
        splitBottom.Dock = DockStyle.Fill;
        splitBottom.Orientation = Orientation.Vertical;
        splitBottom.SplitterDistance = 640;
        splitBottom.Panel1.Controls.Add(splitMessages);
        splitBottom.Panel2.Controls.Add(grpProcess);

        splitMessages.Dock = DockStyle.Fill;
        splitMessages.Orientation = Orientation.Vertical;
        splitMessages.SplitterDistance = 320;
        splitMessages.Panel1.Controls.Add(grpSend);
        splitMessages.Panel2.Controls.Add(grpRecv);

        // Send
        grpSend.Dock = DockStyle.Fill;
        ConfigureRichTextScroll(rtbSend);
        rtbSend.Dock = DockStyle.Fill;
        rtbSend.ReadOnly = true;
        rtbSend.Font = new Font("Consolas", 9F);
        rtbSend.BackColor = Color.FromArgb(245, 248, 255);
        pnlSendBottom.Dock = DockStyle.Bottom;
        pnlSendBottom.Height = 70;
        pnlSendBottom.AutoScroll = true;
        txtInput.Location = new Point(8, 8);
        txtInput.Width = 200;
        txtInput.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        btnSend.Location = new Point(216, 4);
        btnSend.Size = new Size(80, 30);
        btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearSend.Location = new Point(8, 38);
        btnClearSend.Size = new Size(100, 28);
        pnlSendBottom.Controls.AddRange(new Control[] { txtInput, btnSend, btnClearSend });
        grpSend.Controls.Add(rtbSend);
        grpSend.Controls.Add(pnlSendBottom);
        pnlSendBottom.Resize += (_, _) =>
        {
            btnSend.Left = Math.Max(120, pnlSendBottom.ClientSize.Width - btnSend.Width - 8);
            txtInput.Width = Math.Max(80, btnSend.Left - 16);
        };

        // Recv
        grpRecv.Dock = DockStyle.Fill;
        ConfigureRichTextScroll(rtbRecv);
        rtbRecv.Dock = DockStyle.Fill;
        rtbRecv.ReadOnly = true;
        rtbRecv.Font = new Font("Consolas", 9F);
        rtbRecv.BackColor = Color.FromArgb(245, 255, 248);
        pnlRecvBottom.Dock = DockStyle.Bottom;
        pnlRecvBottom.Height = 40;
        pnlRecvBottom.AutoScroll = true;
        btnClearRecv.Location = new Point(8, 6);
        btnClearRecv.Size = new Size(100, 28);
        pnlRecvBottom.Controls.Add(btnClearRecv);
        grpRecv.Controls.Add(rtbRecv);
        grpRecv.Controls.Add(pnlRecvBottom);

        // Process
        grpProcess.Dock = DockStyle.Fill;
        splitProcess.Dock = DockStyle.Fill;
        splitProcess.Orientation = Orientation.Horizontal;
        splitProcess.SplitterDistance = 180;
        splitProcess.Panel1.AutoScroll = true;
        splitProcess.Panel2.AutoScroll = true;
        ConfigureRichTextScroll(rtbProcess);
        rtbProcess.Dock = DockStyle.Fill;
        rtbProcess.ReadOnly = true;
        rtbProcess.Font = new Font("Consolas", 8.5F);
        rtbProcess.BackColor = Color.FromArgb(250, 250, 252);
        splitProcess.Panel1.Controls.Add(rtbProcess);

        var processPanel2 = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        lblResponseTitle.Dock = DockStyle.Top;
        lblResponseTitle.Height = 22;
        ConfigureRichTextScroll(rtbResponse);
        rtbResponse.Dock = DockStyle.Fill;
        rtbResponse.ReadOnly = true;
        rtbResponse.Font = new Font("Consolas", 8.5F);
        rtbResponse.BackColor = Color.FromArgb(245, 255, 250);
        pnlProcessBottom.Dock = DockStyle.Bottom;
        pnlProcessBottom.Height = 40;
        pnlProcessBottom.AutoScroll = true;
        btnExportMessages.Location = new Point(8, 6);
        btnExportMessages.Size = new Size(100, 28);
        btnOpenHelp.Location = new Point(116, 6);
        btnOpenHelp.Size = new Size(80, 28);
        pnlProcessBottom.Controls.AddRange(new Control[] { btnExportMessages, btnOpenHelp });
        processPanel2.Controls.Add(rtbResponse);
        processPanel2.Controls.Add(lblResponseTitle);
        processPanel2.Controls.Add(pnlProcessBottom);
        splitProcess.Panel2.Controls.Add(processPanel2);
        grpProcess.Controls.Add(splitProcess);

        tblComm.Controls.Add(splitBottom, 0, 1);
        splitBottom.Panel1.AutoScroll = true;
        splitBottom.Panel2.AutoScroll = true;
        splitMessages.Panel1.AutoScroll = true;
        splitMessages.Panel2.AutoScroll = true;

        // ===== Rules =====
        lblRuleHint.Dock = DockStyle.Top;
        lblRuleHint.AutoSize = false;
        lblRuleHint.Height = 48;
        lblRuleHint.Padding = new Padding(8, 8, 8, 0);
        dgvRules.Dock = DockStyle.Fill;
        dgvRules.AllowUserToAddRows = false;
        dgvRules.AllowUserToDeleteRows = false;
        dgvRules.ReadOnly = true;
        dgvRules.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvRules.MultiSelect = false;
        dgvRules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        dgvRules.ScrollBars = ScrollBars.Both;
        dgvRules.RowHeadersVisible = false;
        dgvRules.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        pnlRulesBottom.Dock = DockStyle.Bottom;
        pnlRulesBottom.Height = 48;
        pnlRulesBottom.AutoScroll = true;
        btnRuleAdd.Location = new Point(8, 8);
        btnRuleAdd.Size = new Size(90, 32);
        btnRuleEdit.Location = new Point(110, 8);
        btnRuleEdit.Size = new Size(90, 32);
        btnRuleDelete.Location = new Point(212, 8);
        btnRuleDelete.Size = new Size(90, 32);
        btnRuleRefresh.Location = new Point(314, 8);
        btnRuleRefresh.Size = new Size(90, 32);
        pnlRulesBottom.Controls.AddRange(new Control[] { btnRuleAdd, btnRuleEdit, btnRuleDelete, btnRuleRefresh });
        tabRules.AutoScroll = true;
        tabRules.Controls.Add(dgvRules);
        tabRules.Controls.Add(lblRuleHint);
        tabRules.Controls.Add(pnlRulesBottom);

        // ===== History =====
        splitHistory.Dock = DockStyle.Fill;
        splitHistory.SplitterDistance = 320;
        splitHistory.Panel1.AutoScroll = true;
        splitHistory.Panel2.AutoScroll = true;
        lstHistory.Dock = DockStyle.Fill;
        lstHistory.HorizontalScrollbar = true;
        lstHistory.IntegralHeight = false;
        lstHistory.ScrollAlwaysVisible = true;
        ConfigureRichTextScroll(rtbHistoryDetail);
        rtbHistoryDetail.Dock = DockStyle.Fill;
        rtbHistoryDetail.ReadOnly = true;
        rtbHistoryDetail.Font = new Font("Consolas", 9F);
        splitHistory.Panel1.Controls.Add(lstHistory);
        splitHistory.Panel2.Controls.Add(rtbHistoryDetail);
        pnlHistoryBottom.Dock = DockStyle.Bottom;
        pnlHistoryBottom.Height = 48;
        pnlHistoryBottom.AutoScroll = true;
        btnHistoryRefresh.Location = new Point(8, 8);
        btnHistoryRefresh.Size = new Size(110, 32);
        btnHistoryClear.Location = new Point(128, 8);
        btnHistoryClear.Size = new Size(110, 32);
        pnlHistoryBottom.Controls.AddRange(new Control[] { btnHistoryRefresh, btnHistoryClear });
        tabHistory.AutoScroll = true;
        tabHistory.Controls.Add(splitHistory);
        tabHistory.Controls.Add(pnlHistoryBottom);

        // ===== Log =====
        pnlLogTop.Dock = DockStyle.Top;
        pnlLogTop.Height = 52;
        pnlLogTop.AutoScroll = true;
        lblLogLevel.AutoSize = true;
        lblLogLevel.Location = new Point(8, 14);
        cmbLogLevel.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLogLevel.Location = new Point(90, 10);
        cmbLogLevel.Size = new Size(100, 25);
        cmbLogLevel.Items.AddRange(new object[] { "全部", "INFO", "WARN", "ERROR", "DEBUG" });
        cmbLogLevel.SelectedIndex = 0;
        btnLogRefresh.Location = new Point(200, 8);
        btnLogRefresh.Size = new Size(80, 28);
        btnLogClear.Location = new Point(290, 8);
        btnLogClear.Size = new Size(120, 28);
        btnLogExport.Location = new Point(420, 8);
        btnLogExport.Size = new Size(100, 28);
        btnLogOpenFolder.Location = new Point(530, 8);
        btnLogOpenFolder.Size = new Size(120, 28);
        lblLogPath.AutoSize = true;
        lblLogPath.Location = new Point(660, 14);
        lblLogPath.ForeColor = Color.DimGray;
        pnlLogTop.Controls.AddRange(new Control[]
        {
            lblLogLevel, cmbLogLevel, btnLogRefresh, btnLogClear, btnLogExport, btnLogOpenFolder, lblLogPath
        });
        ConfigureRichTextScroll(rtbLog);
        rtbLog.Dock = DockStyle.Fill;
        rtbLog.ReadOnly = true;
        rtbLog.Font = new Font("Consolas", 9F);
        rtbLog.BackColor = Color.FromArgb(248, 249, 250);
        tabLog.AutoScroll = true;
        tabLog.Controls.Add(rtbLog);
        tabLog.Controls.Add(pnlLogTop);

        tabComm.AutoScroll = true;
        tblComm.AutoScroll = true;

        // MainForm - resizable
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 720);
        MinimumSize = new Size(900, 600);
        Controls.Add(tabMain);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "HttpsMessenger";

        ((System.ComponentModel.ISupportInitialize)dgvRules).EndInit();
        ((System.ComponentModel.ISupportInitialize)splitBottom).EndInit();
        ((System.ComponentModel.ISupportInitialize)splitMessages).EndInit();
        ((System.ComponentModel.ISupportInitialize)splitProcess).EndInit();
        ((System.ComponentModel.ISupportInitialize)splitHistory).EndInit();
        splitBottom.ResumeLayout(false);
        splitMessages.ResumeLayout(false);
        splitProcess.ResumeLayout(false);
        splitHistory.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>
    /// 为 RichTextBox 启用纵横滚动条，便于查看完整长文本。
    /// </summary>
    private static void ConfigureRichTextScroll(RichTextBox box)
    {
        box.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
        box.WordWrap = false;
        box.HideSelection = false;
        box.DetectUrls = false;
    }
}
