using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private Antihookclient.AntiHookClient anthook;
        private Antihookclient.WebSocketAntiHookClient websocketClient;
        private Task conectar;

        public Form1()
        {
            InitializeComponent();
            var fieldBack = Color.FromArgb(29, 38, 58);
            var fieldFore = Color.FromArgb(235, 240, 255);
            textBox1.BackColor = fieldBack; textBox1.ForeColor = fieldFore;
            textBox2.BackColor = fieldBack; textBox2.ForeColor = fieldFore;
            textBox3.BackColor = fieldBack; textBox3.ForeColor = fieldFore;
            panel1.BackColor = Color.FromArgb(23, 31, 48);
            button1.BackColor = Color.FromArgb(45, 116, 180); button1.ForeColor = Color.White; button1.FlatStyle = FlatStyle.Flat; button1.FlatAppearance.BorderSize = 0;
            button2.BackColor = Color.FromArgb(53, 63, 84); button2.ForeColor = Color.White; button2.FlatStyle = FlatStyle.Flat; button2.FlatAppearance.BorderSize = 0;
            label1.ForeColor = fieldFore; label2.ForeColor = fieldFore; label3.ForeColor = fieldFore;
        }

        private void Label3_Click(object sender, EventArgs e)
        {

        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            try
            {
                if (websocketClient != null) websocketClient.Dispose();
                websocketClient = new Antihookclient.WebSocketAntiHookClient();
                websocketClient.ConnectionChanged += Anthook_OnConnects;
                websocketClient.Notification += message => { if (!IsDisposed) BeginInvoke(new Action(() => Text = "Antihook | " + message)); };
                var connected = await websocketClient.ConnectAndLoginAsync(textBox1.Text.Trim(), 4040, textBox2.Text.Trim(), textBox3.Text, BF3AntiHook.BF3AntiHook.Mensaje.GetDiskId(), System.Threading.CancellationToken.None);
                if (connected)
                {
                    Hide();
                    using (var hub = new GameHubForm(textBox2.Text.Trim(), false, websocketClient)) hub.ShowDialog(this);
                    Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo iniciar sesión: " + ex.Message, "Antihook", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { button1.Enabled = true; }
        }

        private void Anthook_baneado()
        {
            if (panel1.InvokeRequired)
            {
                panel1.Invoke(new Action(() => { panel1.Controls.Clear(); }));
            }

            Process[] localProcess = Process.GetProcesses();

            foreach (Process process in localProcess)
            {
                if (process.ProcessName.Trim().ToLower() == "LanBf3".Trim().ToLower())
                {
                    Process processa = new Process();
                    processa.StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        FileName = "cmd.exe",
                        Arguments = "/C taskkill /F /PID " + process.Id
                    };
                    processa.Start();
                }
            }
            this.Invoke(new Action(() =>
            {


                DialogResult dialogResult = MessageBox.Show("Has sido baneado por usar hacks contacte a los admins", "!!!ATENCION", MessageBoxButtons.OK, MessageBoxIcon.Question);

                if (dialogResult == DialogResult.OK)
                {

                


                }
            }));
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => { this.Close(); }));

            }

        }
     
        
        

        private void Anthook_OnserversGet(List<BF3AntiHook.BF3AntiHook.Servers> a)
        {
            int yPosition = 0;
            foreach (var e in a) {

                if (panel1.InvokeRequired)
                {

                    panel1.Invoke(new Action(() =>
                    {
                        Controles.UserControl1 item = new Controles.UserControl1();                      
                        item.servidor = e; // Almacenar tu objeto personalizado aquí
                        item.settext(e.gname + " " + e.levelocation + " " + e.playersonline + "/" + e.maxplaers);
                        item.AutoSize = true;
                        item.cliekado += Item_cliekado;       
                        item.Location = new Point(0, yPosition); // Ajusta la posición vertical basada en el número de elementos
                        panel1.Controls.Add(item); // Agrega el elemento al panel
                        yPosition += item.Height + 2;
                    }));
                }
                else
                {
                    Controles.UserControl1 item = new Controles.UserControl1();

                    item.servidor = e; // Almacenar tu objeto personalizado aquí
                    item.settext(e.gname + " " + e.levelocation + " " + e.playersonline + "/" + e.maxplaers);
                    item.AutoSize = true;
                    item.cliekado += Item_cliekado;
                    item.Location = new Point(0, yPosition); // Ajusta la posición vertical basada en el número de elementos
                    panel1.Controls.Add(item); // Agrega el elemento al panel
                    yPosition += item.Height + 2;

                }
            }

        }

        private void Item_cliekado(BF3AntiHook.BF3AntiHook.Servers a)
        {
         
            Task  asd = Task.Run(async () => await anthook.RunBf3(a));

          
        }


        private void Anthook_OnConnects(bool conectado)
            {
            if (conectado)
            {
                if (label4.InvokeRequired)
                {
                    label4.Invoke(new Action(() =>
                    {
                        label4.Text = "Connected";
                        label4.ForeColor = Color.Green;
                    }));
                }
                else
                {

                    label4.Text = "Connected";
                    label4.ForeColor = Color.Green;
                }
             
            }
            else
            {
                if (label4.InvokeRequired)
                {
                    label4.Invoke(new Action(() =>
                    {
                        label4.Text = "Disconnect";
                        label4.ForeColor = Color.Red;
                    }));
                }
                else
                {

                    label4.Text = "Disconnect";
                    label4.ForeColor = Color.Red;
                }
         
            }

        }
        public void Asegurar()
        {
            var a  = Antihookclient.AntiHookClient.PreOpen();
            if (a)
            {
                this.Close();
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
        Asegurar();
            var a =  BF3AntiHOOK.ConfigFile.LoadConfig();
            textBox1.Text = a.server;
            textBox2.Text = a.username;
            textBox3.Text = a.password;
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            BF3AntiHOOK.ConfigFile.SaveConfig(textBox2.Text, textBox1.Text, textBox3.Text);
            Process[] localProcess = Process.GetProcesses();

            foreach (Process process in localProcess)
            {
                if (process.ProcessName == "LanBf3")
                {
                    Process processa = new Process();
                    processa.StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        FileName = "cmd.exe",
                        Arguments = "/C taskkill /F /PID " + process.Id
                    };
                    processa.Start();
                }
            }
        
        }


        private void Button2_Click(object sender, EventArgs e)
        {
            panel1.Controls.Clear();

            Task ad = Task.Run(async () =>
            {
                await anthook.Connect(textBox1.Text, textBox2.Text, textBox3.Text);
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            BF3AntiHOOK.ConfigFile.SaveConfig(textBox2.Text, textBox1.Text, textBox3.Text);
            Process[] localProcess = Process.GetProcesses();

            foreach (Process process in localProcess)
            {
                if (process.ProcessName == "LanBf3")
                {
                    Process processa = new Process();
                    processa.StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        FileName = "cmd.exe",
                        Arguments = "/C taskkill /F /PID " + process.Id
                    };
                    processa.Start();
                }
            }
        }
    }
}
