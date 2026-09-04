using HttpsMessenger.Localization;
using HttpsMessenger.Models;

namespace HttpsMessenger;

/// <summary>
/// 新增/编辑字段规则对话框。
/// </summary>
public sealed class RuleEditForm : Form
{
    private readonly TextBox _txtName = new();
    private readonly TextBox _txtRequestField = new();
    private readonly ComboBox _cmbMatchMode = new();
    private readonly TextBox _txtRequestValue = new();
    private readonly TextBox _txtResponseField = new();
    private readonly TextBox _txtResponseValue = new();
    private readonly NumericUpDown _numStatus = new();
    private readonly TextBox _txtResponseMessage = new();
    private readonly CheckBox _chkEnabled = new();
    private readonly TextBox _txtRemark = new();
    private readonly List<Label> _labels = new();
    private readonly Button _btnOk = new();
    private readonly Button _btnCancel = new();
    private readonly bool _isNew;

    public FieldRule Rule { get; private set; }

    public RuleEditForm(FieldRule? existing = null)
    {
        _isNew = existing is null;
        Rule = existing is null
            ? new FieldRule()
            : new FieldRule
            {
                Id = existing.Id,
                Name = existing.Name,
                RequestField = existing.RequestField,
                MatchMode = existing.MatchMode,
                RequestValue = existing.RequestValue,
                ResponseField = existing.ResponseField,
                ResponseValue = existing.ResponseValue,
                HttpStatus = existing.HttpStatus,
                ResponseMessage = existing.ResponseMessage,
                IsEnabled = existing.IsEnabled,
                Remark = existing.Remark,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt
            };

        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 460);
        MinimumSize = new Size(520, 430);
        AutoScroll = true;
        Font = new Font("Microsoft YaHei UI", 9F);

        var y = 16;
        void AddRow(string labelKey, Control control, int height = 23)
        {
            var lbl = new Label
            {
                Tag = labelKey,
                Location = new Point(16, y + 3),
                AutoSize = true
            };
            _labels.Add(lbl);
            control.Location = new Point(160, y);
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            control.Width = Math.Max(200, ClientSize.Width - 190);
            if (control is TextBox tb)
            {
                // 长文本输入也给垂直滚动（多行时可扩展）
                tb.ScrollBars = ScrollBars.Horizontal;
            }
            if (control is not CheckBox)
            {
                control.Height = height;
            }

            Controls.Add(lbl);
            Controls.Add(control);
            y += height + 14;
        }

        _cmbMatchMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbMatchMode.Items.AddRange(new object[] { "Equals", "Contains", "Any" });
        _numStatus.Minimum = 100;
        _numStatus.Maximum = 599;
        _numStatus.Value = 200;
        _chkEnabled.AutoSize = true;
        _chkEnabled.Tag = "Enabled";

        AddRow("RuleName", _txtName);
        AddRow("RequestField", _txtRequestField);
        AddRow("MatchMode", _cmbMatchMode);
        AddRow("RequestValue", _txtRequestValue);
        AddRow("ResponseField", _txtResponseField);
        AddRow("ResponseValue", _txtResponseValue);
        AddRow("HttpStatus", _numStatus);
        AddRow("ResponseMessage", _txtResponseMessage);
        AddRow("Enabled", _chkEnabled);
        AddRow("Remark", _txtRemark);

        _txtName.Text = Rule.Name;
        _txtRequestField.Text = Rule.RequestField;
        _cmbMatchMode.SelectedItem = string.IsNullOrWhiteSpace(Rule.MatchMode) ? "Equals" : Rule.MatchMode;
        if (_cmbMatchMode.SelectedIndex < 0)
        {
            _cmbMatchMode.SelectedIndex = 0;
        }

        _txtRequestValue.Text = Rule.RequestValue;
        _txtResponseField.Text = Rule.ResponseField;
        _txtResponseValue.Text = Rule.ResponseValue;
        _numStatus.Value = Math.Clamp(Rule.HttpStatus, 100, 599);
        _txtResponseMessage.Text = Rule.ResponseMessage;
        _chkEnabled.Checked = Rule.IsEnabled;
        _txtRemark.Text = Rule.Remark;

        _btnOk.DialogResult = DialogResult.None;
        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.Location = new Point(ClientSize.Width - 210, ClientSize.Height - 50);
        _btnOk.Size = new Size(90, 30);
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.Location = new Point(ClientSize.Width - 110, ClientSize.Height - 50);
        _btnCancel.Size = new Size(90, 30);
        _btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)
                || string.IsNullOrWhiteSpace(_txtRequestField.Text)
                || string.IsNullOrWhiteSpace(_txtResponseField.Text))
            {
                MessageBox.Show(this, Lang.T("RuleRequired"), Lang.T("Prompt"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Rule.Name = _txtName.Text.Trim();
            Rule.RequestField = _txtRequestField.Text.Trim();
            Rule.MatchMode = _cmbMatchMode.SelectedItem?.ToString() ?? "Equals";
            Rule.RequestValue = _txtRequestValue.Text;
            Rule.ResponseField = _txtResponseField.Text.Trim();
            Rule.ResponseValue = _txtResponseValue.Text;
            Rule.HttpStatus = (int)_numStatus.Value;
            Rule.ResponseMessage = _txtResponseMessage.Text.Trim();
            Rule.IsEnabled = _chkEnabled.Checked;
            Rule.Remark = _txtRemark.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = Lang.T(_isNew ? "RuleAddTitle" : "RuleEditTitle");
        foreach (var lbl in _labels)
        {
            var key = lbl.Tag as string;
            if (!string.IsNullOrEmpty(key))
            {
                lbl.Text = Lang.T(key);
            }
        }

        _chkEnabled.Text = Lang.T("Enabled");
        _btnOk.Text = Lang.T("Save");
        _btnCancel.Text = Lang.T("Cancel");
    }
}
