using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public sealed class AdminUsersForm : Form
    {
        private readonly DataGridView users = new DataGridView();
        private readonly ContextMenuStrip userMenu = new ContextMenuStrip();
        private readonly Action<string, string, string> executeAction;

        public AdminUsersForm(IEnumerable<BF3AntiHook.BF3AntiHook.User> connectedUsers, Action<string, string, string> executeAction)
        {
            this.executeAction = executeAction;
            Text = "Antihook | Usuarios conectados";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(980, 600);
            BackColor = Color.FromArgb(18, 24, 38);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(24) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label { Text = "Usuarios conectados", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.White }, 0, 0);

            users.Dock = DockStyle.Fill;
            users.BackgroundColor = Color.FromArgb(29, 38, 58);
            users.ForeColor = Color.White;
            users.BorderStyle = BorderStyle.None;
            users.ReadOnly = true;
            users.MultiSelect = false;
            users.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            users.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            users.AllowUserToAddRows = false;
            users.Columns.Add("name", "Nombre");
            users.Columns.Add("hwid", "HWID");
            users.Columns.Add("ip", "IP");
            users.Columns.Add("longIp", "LongIP");
            users.ContextMenuStrip = userMenu;
            users.MouseDown += UsersMouseDown;
            layout.Controls.Add(users, 0, 1);
            Controls.Add(layout);

            foreach (var user in connectedUsers ?? new List<BF3AntiHook.BF3AntiHook.User>())
                users.Rows.Add(user.Username, user.HWID, user.IP, user.LongIP);

            AddAction("Captura de pantalla", "screenshot");
            AddAction("Capturar procesos", "processes");
            AddAction("Capturar módulos", "modules");
            AddAction("Enviar Speech", "speech");
            userMenu.Items.Add(new ToolStripSeparator());
            AddAction("Expulsar", "kick");
            AddAction("Banear", "ban");
        }

        private void AddAction(string label, string action)
        {
            userMenu.Items.Add(label, null, delegate
            {
                if (users.SelectedRows.Count == 0) return;
                var row = users.SelectedRows[0];
                var username = Convert.ToString(row.Cells[0].Value);
                var reason = PromptReason(label);
                if (String.IsNullOrWhiteSpace(reason)) return;
                executeAction?.Invoke(action, username, reason);
            });
        }

        private string PromptReason(string action)
        {
            using (var dialog = new Form { Text = action, Width = 420, Height = 160, StartPosition = FormStartPosition.CenterParent, BackColor = Color.FromArgb(29, 38, 58) })
            {
                var input = new TextBox { Left = 16, Top = 18, Width = 370 };
                var ok = new Button { Text = "Confirmar", Left = 280, Top = 58, Width = 105, DialogResult = DialogResult.OK };
                dialog.Controls.Add(input); dialog.Controls.Add(ok); dialog.AcceptButton = ok;
                return dialog.ShowDialog(this) == DialogResult.OK ? input.Text.Trim() : null;
            }
        }

        private void UsersMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = users.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0) return;
            users.ClearSelection();
            users.Rows[hit.RowIndex].Selected = true;
        }
    }
}
