using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClassLock
{
    public sealed class PasswordForm : Form
    {
        private readonly TextBox _txtPwd;
        private readonly Label _lblError;

        public PasswordForm()
        {
            // 1. 禁用 DPI 缩放，防止字体大小计算崩溃
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;

            Text = "解锁";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            // 不用 CenterScreen，改为 CenterParent 或手动指定，防止多屏计算溢出
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 200);




            var lbl = new Label();
            lbl.Text = "请输入解锁密码：";
            lbl.Location = new Point(30, 22);
            lbl.Size = new Size(200, 25); // 【关键】放弃 AutoSize，写死大小
            lbl.TextAlign = ContentAlignment.MiddleLeft;

            _txtPwd = new TextBox();
            _txtPwd.Location = new Point(30, 50);
            _txtPwd.Width = 320;
            _txtPwd.PasswordChar = '*';

            _lblError = new Label();
            _lblError.Location = new Point(30, 86);
            _lblError.Size = new Size(320, 25); // 【关键】放弃 AutoSize，写死大小
            _lblError.ForeColor = Color.Red;
            _lblError.Text = "";

            var btnOk = new Button();
            btnOk.Text = "提交";
            btnOk.Location = new Point(190, 125);
            btnOk.Size = new Size(76, 32);
            btnOk.Click += (s, e) => Submit();

            var btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(274, 125);
            btnCancel.Size = new Size(76, 32);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(lbl);
            Controls.Add(_txtPwd);
            Controls.Add(_lblError);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            Shown += (s, e) => _txtPwd.Focus();

            //
            // 测试代码
            //
#if DEBUG
            // 在 MainForm 的 BuildUi() 方法最后面加上：
            Button btnTest = new Button();
            btnTest.Text = "单独测试密码框";
            btnTest.Location = new Point(20, 430); // 随便找个地方放
            btnTest.Click += (s, ev) =>
            {
                using (var dlg = new PasswordForm())
                {
                    dlg.ShowDialog();
                }
            };
            Controls.Add(btnTest);
#endif

        }

        private void Submit()
        {
            if (PasswordStore.Verify(_txtPwd.Text))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblError.Text = "密码错误，请重试";
                _txtPwd.SelectAll();
                _txtPwd.Focus();
            }
        }
    }
}