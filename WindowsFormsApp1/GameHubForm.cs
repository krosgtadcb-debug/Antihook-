using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public sealed class GameHubForm : Form
    {
        private readonly TableLayoutPanel games;
        private readonly Label sessionLabel;
        private readonly Antihookclient.WebSocketAntiHookClient websocketClient;

        public GameHubForm(string username, bool isAdmin) : this(username, isAdmin, null)
        {
        }

        public GameHubForm(string username, bool isAdmin, Antihookclient.WebSocketAntiHookClient websocketClient)
        {
            this.websocketClient = websocketClient;
            Text = "Antihook | GameHub";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(860, 520);
            BackColor = Color.FromArgb(18, 24, 38);
            ForeColor = Color.White;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(28) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var header = new Panel { Dock = DockStyle.Fill };
            var title = new Label { Text = "GameHub", AutoSize = true, Font = new Font("Segoe UI", 25, FontStyle.Bold), ForeColor = Color.FromArgb(237, 242, 255), Location = new Point(0, 5) };
            sessionLabel = new Label { Text = "Sesión: " + (username ?? "usuario"), AutoSize = true, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(162, 174, 205), Location = new Point(4, 45) };
            header.Controls.Add(title);
            header.Controls.Add(sessionLabel);
            root.Controls.Add(header, 0, 0);

            var subtitle = new Label { Text = "Selecciona un juego para continuar", AutoSize = true, Font = new Font("Segoe UI", 12), ForeColor = Color.FromArgb(184, 195, 220), Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            root.Controls.Add(subtitle, 0, 1);

            games = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent };
            games.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            games.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            games.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            AddGameCard("Battlefield 3", "Servidores, jugadores y protección activa", Color.FromArgb(45, 116, 180), OpenBattlefield3, 0);
            AddGameCard("Próximamente", "Más juegos en futuras versiones", Color.FromArgb(53, 63, 84), null, 1);
            if (isAdmin) AddGameCard("Administración", "Usuarios, sesiones y eventos", Color.FromArgb(122, 82, 170), OpenAdmin, 2);
            else AddGameCard("Estado del sistema", "Conexión y versión del cliente", Color.FromArgb(53, 63, 84), null, 2);
            root.Controls.Add(games, 0, 2);
        }

        private void AddGameCard(string name, string description, Color accent, EventHandler action, int column)
        {
            var card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Color.FromArgb(29, 38, 58), Padding = new Padding(18) };
            var stripe = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = accent };
            var title = new Label { Text = name, AutoSize = true, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.White, Location = new Point(28, 22) };
            var detail = new Label { Text = description, AutoSize = false, Width = 210, Height = 52, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(179, 190, 215), Location = new Point(28, 58) };
            var button = new Button { Text = action == null ? "Próximamente" : "Abrir", Width = 112, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.White, Location = new Point(28, 125), Enabled = action != null };
            button.FlatAppearance.BorderSize = 0;
            if (action != null) button.Click += action;
            card.Controls.Add(stripe);
            card.Controls.Add(title);
            card.Controls.Add(detail);
            card.Controls.Add(button);
            games.Controls.Add(card, column, 0);
        }

        private void OpenBattlefield3(object sender, EventArgs e)
        {
            using (var form = new Battlefield3Form(websocketClient)) form.ShowDialog(this);
        }

        private void OpenAdmin(object sender, EventArgs e)
        {
            MessageBox.Show(this, "El panel administrativo se conectará al canal WebSocket seguro en la siguiente iteración.", "Administración", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public sealed class Battlefield3Form : Form
    {
        private readonly DataGridView servers = new DataGridView();
        private readonly Antihookclient.WebSocketAntiHookClient websocketClient;

        public Battlefield3Form() : this(null)
        {
        }

        public Battlefield3Form(Antihookclient.WebSocketAntiHookClient websocketClient)
        {
            this.websocketClient = websocketClient;
            Text = "Antihook | Battlefield 3";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(900, 560);
            BackColor = Color.FromArgb(18, 24, 38);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(24) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var heading = new Label { Text = "Battlefield 3  ·  Servidores", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.White };
            layout.Controls.Add(heading, 0, 0);
            servers.Dock = DockStyle.Fill;
            servers.BackgroundColor = Color.FromArgb(29, 38, 58);
            servers.ForeColor = Color.White;
            servers.BorderStyle = BorderStyle.None;
            servers.AllowUserToAddRows = false;
            servers.ReadOnly = true;
            servers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            servers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 116, 180);
            servers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            servers.EnableHeadersVisualStyles = false;
            servers.Columns.Add("name", "Servidor");
            servers.Columns.Add("map", "Mapa");
            servers.Columns.Add("players", "Jugadores");
            servers.Columns.Add("ping", "Ping");
            layout.Controls.Add(servers, 0, 1);
            Controls.Add(layout);
            if (this.websocketClient != null)
            {
                this.websocketClient.ServersUpdated += OnServersUpdated;
                FormClosed += delegate { this.websocketClient.ServersUpdated -= OnServersUpdated; };
            }
        }

        private void OnServersUpdated(System.Collections.Generic.List<BF3AntiHook.BF3AntiHook.Servers> snapshot)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<System.Collections.Generic.List<BF3AntiHook.BF3AntiHook.Servers>>(OnServersUpdated), snapshot);
                return;
            }
            servers.Rows.Clear();
            foreach (var item in snapshot)
                servers.Rows.Add(item.gname, item.levelocation, item.playersonline + "/" + item.maxplaers, "—");
        }
    }
}
