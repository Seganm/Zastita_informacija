using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class ReceiveWindow : Form
    {
        private readonly AppSettings _settings;

        // Paleta boja
        private readonly Color _bgColor = Color.FromArgb(32, 33, 36);
        private readonly Color _panelColor = Color.FromArgb(45, 46, 50);
        private readonly Color _accentColor = Color.FromArgb(66, 133, 244);
        private readonly Color _btnColor = Color.FromArgb(60, 64, 67);
        private readonly Color _textColor = Color.WhiteSmoke;
        private readonly Color _successColor = Color.FromArgb(129, 201, 149);

        // Kontrole
        private readonly TextBox txtPort = new() { Text = "5000", BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Width = 120 };
        private readonly TextBox txtIps = new() { Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(20, 20, 20), ForeColor = Color.DarkGray, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 8.5f) };

        private readonly CheckBox chkAuto = new()
        {
            Text = "Automatski dešifruj fajlove nakon prijema",
            Checked = true,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f)
        };

        private readonly Label lblHint = new()
        {
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            Text = "Napomena: Sistem će detektovati algoritam po ekstenziji (.tea/.lea/.ctr) i tražiti ključ."
        };

        // Dugmići sa novim nazivima
        private readonly Button btnStart = new() { Text = "AKTIVIRAJ PRIJEM", FlatStyle = FlatStyle.Flat, Height = 45, Cursor = Cursors.Hand };
        private readonly Button btnStop = new() { Text = "ZAUSTAVI", FlatStyle = FlatStyle.Flat, Height = 45, Enabled = false, Cursor = Cursors.Hand };
        private readonly Button btnClear = new() { Text = "OBRIŠI ISTORIJU", FlatStyle = FlatStyle.Flat, Height = 45, Cursor = Cursors.Hand };
        private readonly Button btnClose = new() { Text = "IZLAZ", FlatStyle = FlatStyle.Flat, Height = 45, Cursor = Cursors.Hand };

        private readonly TextBox txtLog = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(180, 255, 180),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 10)
        };

        private FileReceiver? _receiver;

        public ReceiveWindow(AppSettings settings)
        {
            _settings = settings;

            // Inicijalna podešavanja prozora
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Mrežni Prijem Podataka (TCP Receiver)";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1100, 700);
            BackColor = _bgColor;
            ForeColor = _textColor;
            Font = new Font("Segoe UI", 9.5f);

            // --- LEVI PANEL (Mrežne informacije) ---
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 260, BackColor = _panelColor, Padding = new Padding(20) };
            var lblIpTitle = new Label { Text = "VAŠE IP ADRESE:", Dock = DockStyle.Top, Height = 30, ForeColor = _accentColor, Font = new Font("Segoe UI Bold", 9f) };
            txtIps.Dock = DockStyle.Fill;
            leftPanel.Controls.Add(txtIps);
            leftPanel.Controls.Add(lblIpTitle);

            // --- GLAVNI PANEL (Sredina) ---
            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };

            // Grid za podešavanja
            var configGrid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            configGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            configGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            configGrid.Controls.Add(CreateLabel("Port za slušanje:"), 0, 0);
            configGrid.Controls.Add(txtPort, 1, 0);

            // Opcije za dešifrovanje
            var optPanel = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(0, 20, 0, 0) };
            chkAuto.Location = new Point(0, 20);
            lblHint.Location = new Point(0, 50);
            optPanel.Controls.Add(chkAuto);
            optPanel.Controls.Add(lblHint);

            // Kontrolni dugmići
            var btnFlow = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 20, 0, 20), AutoSize = true };

            btnStart.Width = 180; btnStart.BackColor = _accentColor; btnStart.ForeColor = Color.White; btnStart.FlatAppearance.BorderSize = 0;
            btnStop.Width = 140; btnStop.BackColor = _btnColor; btnStop.FlatAppearance.BorderSize = 0;
            btnClear.Width = 160; btnClear.BackColor = _btnColor; btnClear.FlatAppearance.BorderSize = 0;
            btnClose.Width = 120; btnClose.BackColor = _btnColor; btnClose.FlatAppearance.BorderSize = 0;

            btnStop.Margin = new Padding(10, 0, 0, 0);
            btnClear.Margin = new Padding(10, 0, 0, 0);
            btnClose.Margin = new Padding(10, 0, 0, 0);

            btnFlow.Controls.AddRange(new Control[] { btnStart, btnStop, btnClear, btnClose });

            // Log kontejner
            var logContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
            var lblLogTitle = new Label { Text = "LOG PRIJEMA:", Dock = DockStyle.Top, Height = 25, ForeColor = _accentColor, Font = new Font("Segoe UI Bold", 9f) };
            txtLog.Dock = DockStyle.Fill;
            logContainer.Controls.Add(txtLog);
            logContainer.Controls.Add(lblLogTitle);

            centerPanel.Controls.Add(logContainer);
            centerPanel.Controls.Add(btnFlow);
            centerPanel.Controls.Add(optPanel);
            centerPanel.Controls.Add(configGrid);

            Controls.Add(centerPanel);
            Controls.Add(leftPanel);

            // Eventi
            btnStart.Click += (_, __) => StartReceive();
            btnStop.Click += (_, __) => StopReceive();
            btnClear.Click += (_, __) => txtLog.Clear();
            btnClose.Click += (_, __) => Close();
            FormClosing += (_, e) => { if (_receiver != null) _receiver.StopGui(); };

            FillLocalIps();
            AppendLog("[SISTEM] Modul za prijem spreman.");
        }

        private Label CreateLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Padding = new Padding(0, 10, 30, 10), ForeColor = Color.DarkGray };
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(AppendLog), text); return; }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        }

        // --- LOGIKA (Nepromenjeno) ---

        private void FillLocalIps()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var props = ni.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !addr.Address.ToString().StartsWith("127."))
                            sb.AppendLine($"• {ni.Name}\r\n  {addr.Address}\r\n");
                    }
                }
                txtIps.Text = sb.ToString();
            }
            catch { txtIps.Text = "Greška pri čitanju IP adresa."; }
        }

        private void StartReceive()
        {
            if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Nevažeći port."); return;
            }

            try
            {
                _receiver = new FileReceiver(port, _settings.ReceivedFolder);
                _receiver.SetLogger(AppendLog);

                if (chkAuto.Checked)
                {
                    _receiver.ConfigureAutoDecrypt(true, info => {
                        using var dlg = new DecryptPromptDialog(info);
                        return dlg.ShowDialog(this) == DialogResult.OK ? dlg.GetParams() : null;
                    });
                }
                else
                {
                    _receiver.ConfigureAutoDecrypt(false, null);
                }

                _receiver.StartGui();
                btnStart.Enabled = false; btnStop.Enabled = true;
                btnStart.BackColor = Color.Gray;
                AppendLog(">>> SERVER POKRENUT (Slušanje na portu " + port + ")");
            }
            catch (Exception ex)
            {
                _receiver = null;
                MessageBox.Show("Greška: " + ex.Message);
            }
        }

        private void StopReceive()
        {
            if (_receiver == null) return;
            try
            {
                _receiver.StopGui();
                AppendLog("<<< SERVER ZAUSTAVLJEN.");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            finally
            {
                _receiver = null;
                btnStart.Enabled = true; btnStop.Enabled = false;
                btnStart.BackColor = _accentColor;
            }
        }
    }
}