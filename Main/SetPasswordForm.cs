using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClassLock
{
    public sealed class SetPasswordForm : Form
    {
        private readonly TextBox _txtOld, _txtNew, _txtConfirm;
        private readonly Label _lblError;

        public SetPasswordForm()
        {
            Text            = "修改解锁密码";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            ClientSize      = new Size(400, 260);
            Font            = new Font("Microsoft YaHei", 10f);
            BackColor       = Color.FromArgb(245, 247, 250);

            var lblOld = new Label { Text = "当前密码：", Location = new Point(30, 25), AutoSize = true };
            _txtOld = new TextBox { Location = new Point(120, 22), Size = new Size(240, 26), UseSystemPasswordChar = true };

            var lblNew = new Label { Text = "新密码：", Location = new Point(30, 65), AutoSize = true };
            _txtNew = new TextBox { Location = new Point(120, 62), Size = new Size(240, 26), UseSystemPasswordChar = true };

            var lblConfirm = new Label { Text = "确认新密码：", Location = new Point(30, 105), AutoSize = true };
            _txtConfirm = new TextBox { Location = new Point(120, 102), Size = new Size(240, 26), UseSystemPasswordChar = true };

            _lblError = new Label { Location = new Point(30, 140), Size = new Size(340, 20), ForeColor = Color.OrangeRed };

            var btnOk = new Button { Text = "保存", Location = new Point(200, 200), Size = new Size(80, 32) };
            btnOk.Click += (s, e) => Save();

            var btnCancel = new Button { Text = "取消", Location = new Point(290, 200), Size = new Size(80, 32) };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblOld, _txtOld, lblNew, _txtNew, lblConfirm, _txtConfirm, _lblError, btnOk, btnCancel
            });

            Shown += (s, e) => _txtOld.Focus();
        }

        private void Save()
        {
            if (!PasswordStore.Verify(_txtOld.Text))
            {
                _lblError.Text = "当前密码不正确";
                _txtOld.SelectAll();
                _txtOld.Focus();
                return;
            }
            if (string.IsNullOrEmpty(_txtNew.Text) || _txtNew.Text.Length < 4)
            {
                _lblError.Text = "新密码至少 4 位";
                _txtNew.Focus();
                return;
            }
            if (_txtNew.Text != _txtConfirm.Text)
            {
                _lblError.Text = "两次输入的新密码不一致";
                _txtConfirm.SelectAll();
                _txtConfirm.Focus();
                return;
            }

            PasswordStore.SetPassword(_txtNew.Text);
            MessageBox.Show(this, "密码修改成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}