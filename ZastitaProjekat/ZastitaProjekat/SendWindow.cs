using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class SendWindow : Form
    {
        private readonly AppSettings _settings;

        // Paleta boja
        private readonly Color _bgColor = Color.FromArgb(32, 33, 36);
        private readonly Color _panelColor = Color.FromArgb(45, 46, 50);
        private readonly Color _accentColor = Color.FromArgb(66, 133, 244);
        private readonly Color _btnColor = Color.FromArgb(60, 64, 67);
        private readonly Color _textColor = Color.WhiteSmoke;
        private readonly Color _successColor = Color.FromArgb(129, 201, 149);
        private readonly Color _errorColor = Color.FromArgb(242, 139, 130);

        // Kontrole
        private readonly TextBox txtIp = new() { BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly TextBox txtPort = new() { Width = 100, Text = "5000", BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly TextBox txtFile = new() { ReadOnly = true, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Button btnBrowse = new() { Text = "LOCIRAJ FAJL", FlatStyle = FlatStyle.Flat, Width = 120 };

        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White };
        private readonly TextBox txtKey = new() { PlaceholderText = "16 bajtova ključa", BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label lblKeyBytes = new() { AutoSize = true };

        private readonly TextBox txtNonce = new() { PlaceholderText = "8 bajtova", Visible = false, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, Visible = false };

        private readonly Label lblAlreadyEnc = new() { AutoSize = true, ForeColor = Color.FromArgb(129, 201, 149), Visible = false, Text = "● Fajl je već šifrovan - direktno slanje aktivno." };

        private readonly TextBox txtIps = new() { Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(20, 20, 20), ForeColor = Color.DarkGray, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 8.5f) };
        private readonly TextBox txtLog = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(20, 20, 20), ForeColor = Color.FromArgb(180, 255, 180), BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9.5f) };

        // Dugmići sa novim nazivima
        private readonly Button btnSend = new() { Text = "INICIJALIZUJ PRENOS", FlatStyle = FlatStyle.Flat, Height = 45, Cursor = Cursors.Hand };
        private readonly Button btnClear = new() { Text = "OBRIŠI ISTORIJU", FlatStyle = FlatStyle.Flat, Height = 45, Cursor = Cursors.Hand };
        private readonly Button btnClose = new() { Text = "IZLAZ", FlatStyle = FlatStyle.Flat, Height = 45, Cursor = Cursors.Hand };

        private readonly OpenFileDialog ofd = new() { Title = "Odaberi fajl za slanje", Filter = "Svi fajlovi (*.*)|*.*" };

        public SendWindow(AppSettings settings)
        {
            _settings = settings;

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Mrežni prenos podataka (TCP Sender)";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1100, 700);
            BackColor = _bgColor;
            ForeColor = _textColor;
            Font = new Font("Segoe UI", 9.5f);

            // --- LEVI PANEL (Lokalne IP adrese) ---
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = _panelColor, Padding = new Padding(15) };
            var lblIpTitle = new Label { Text = "VAŠE ADRESE:", Dock = DockStyle.Top, Height = 30, ForeColor = _accentColor, Font = new Font("Segoe UI Bold", 9f) };
            txtIps.Dock = DockStyle.Fill;
            leftPanel.Controls.Add(txtIps);
            leftPanel.Controls.Add(lblIpTitle);

            // --- GLAVNI SADRŽAJ (Sredina) ---
            var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25) };

            var grid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // Labele
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // Inputi
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Dugmići/Byte count

            // Redovi
            AddRow(grid, "IP Primaoca:", txtIp, null, 0);
            AddRow(grid, "Port:", txtPort, null, 1);
            AddRow(grid, "Fajl:", txtFile, btnBrowse, 2);

            grid.Controls.Add(CreateLabel("Algoritam:"), 0, 3);
            var algoFlow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            algoFlow.Controls.Add(cmbAlgo);
            algoFlow.Controls.Add(lblAlreadyEnc);
            grid.Controls.Add(algoFlow, 1, 3);

            grid.Controls.Add(CreateLabel("Ključ:"), 0, 4);
            grid.Controls.Add(txtKey, 1, 4);
            grid.Controls.Add(lblKeyBytes, 2, 4);

            grid.Controls.Add(lblNonce, 0, 5);
            lblNonce.Padding = new Padding(0, 10, 30, 10); lblNonce.ForeColor = Color.DarkGray;
            grid.Controls.Add(txtNonce, 1, 5);
            grid.Controls.Add(lblNonceBytes, 2, 5);

            // --- DUGMIĆI ---
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 20, 0, 20), AutoSize = true };

            btnSend.Width = 200; btnSend.BackColor = _accentColor; btnSend.ForeColor = Color.White; btnSend.FlatAppearance.BorderSize = 0;
            btnClear.Width = 150; btnClear.BackColor = _btnColor; btnClear.FlatAppearance.BorderSize = 0;
            btnClose.Width = 120; btnClose.BackColor = _btnColor; btnClose.FlatAppearance.BorderSize = 0;
            btnClear.Margin = new Padding(10, 0, 0, 0);
            btnClose.Margin = new Padding(10, 0, 0, 0);

            btnPanel.Controls.AddRange(new Control[] { btnSend, btnClear, btnClose });

            // --- LOG ---
            var logContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
            var lblLogTitle = new Label { Text = "LOG PRENOSA:", Dock = DockStyle.Top, Height = 25, ForeColor = _accentColor, Font = new Font("Segoe UI Bold", 9f) };
            txtLog.Dock = DockStyle.Fill;
            logContainer.Controls.Add(txtLog);
            logContainer.Controls.Add(lblLogTitle);

            centerPanel.Controls.Add(logContainer);
            centerPanel.Controls.Add(btnPanel);
            centerPanel.Controls.Add(grid);

            Controls.Add(centerPanel);
            Controls.Add(leftPanel);

            // Inicijalizacija
            cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
            cmbAlgo.SelectedIndex = 0;

            // Eventi
            btnBrowse.Click += (_, __) => BrowseFile();
            btnSend.Click += (_, __) => DoSend();
            btnClose.Click += (_, __) => Close();
            btnClear.Click += (_, __) => txtLog.Clear();
            cmbAlgo.SelectedIndexChanged += (_, __) => UpdateCtrVisibility();
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();

            FillLocalIps();
            UpdateKeyBytes();
            UpdateNonceBytes();
            UpdateCtrVisibility();
            Append("[SISTEM] Klijent spreman za slanje.");
        }

        private void AddRow(TableLayoutPanel grid, string label, Control input, Control btn, int row)
        {
            grid.Controls.Add(CreateLabel(label), 0, row);
            input.Dock = DockStyle.Fill;
            grid.Controls.Add(input, 1, row);
            if (btn != null) { btn.Margin = new Padding(5, 0, 0, 0); grid.Controls.Add(btn, 2, row); }
        }

        private Label CreateLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Padding = new Padding(0, 10, 30, 10), ForeColor = Color.DarkGray };
        }

        private void Append(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Append), line); return; }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        }

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

        // --- LOGIKA (Nepromenjeno, prilagođeno novim kontrolama) ---

        private void BrowseFile()
        {
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                txtFile.Text = ofd.FileName;
                UpdateForSelectedFile();
            }
        }

        private void UpdateForSelectedFile()
        {
            bool alreadyEnc = IsAlreadyEncrypted(txtFile.Text);
            lblAlreadyEnc.Visible = alreadyEnc;
            cmbAlgo.Enabled = !alreadyEnc;
            txtKey.Enabled = !alreadyEnc;
            UpdateCtrVisibility();
        }

        private void UpdateCtrVisibility()
        {
            bool alreadyEnc = IsAlreadyEncrypted(txtFile.Text);
            bool isCtr = !alreadyEnc && (cmbAlgo.SelectedItem?.ToString() == "LEA-CTR");
            txtNonce.Visible = isCtr; lblNonce.Visible = isCtr; lblNonceBytes.Visible = isCtr;
        }

        private void UpdateKeyBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            lblKeyBytes.Text = $"{n} B";
            lblKeyBytes.ForeColor = (n == 16) ? _successColor : _errorColor;
        }

        private void UpdateNonceBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
            lblNonceBytes.Text = $"{n} B";
            lblNonceBytes.ForeColor = (n == 8) ? _successColor : _errorColor;
        }

        private void DoSend()
        {
            string ip = txtIp.Text.Trim();
            if (string.IsNullOrEmpty(ip)) { MessageBox.Show("Unesite IP primaoca."); return; }
            if (!int.TryParse(txtPort.Text, out int port)) { MessageBox.Show("Nevažeći port."); return; }
            if (!File.Exists(txtFile.Text)) { MessageBox.Show("Izaberite fajl."); return; }

            bool alreadyEnc = IsAlreadyEncrypted(txtFile.Text);
            string algorithm = alreadyEnc ? "AUTO" : (cmbAlgo.SelectedItem?.ToString() ?? "TEA");
            byte[] key = alreadyEnc ? null : Encoding.UTF8.GetBytes(txtKey.Text);
            byte[] nonce = (algorithm == "LEA-CTR") ? Encoding.UTF8.GetBytes(txtNonce.Text) : null;

            if (!alreadyEnc && Encoding.UTF8.GetByteCount(txtKey.Text) != 16) { MessageBox.Show("Ključ mora biti 16 bajtova."); return; }

            try
            {
                Append($"[Sender] Pokušaj povezivanja: {ip}:{port}");
                var sender = new FileSender(ip, port);
                if (sender.TrySend(txtFile.Text, algorithm, key, nonce, out var err))
                    Append($"[USPEH] Poslat fajl: {Path.GetFileName(txtFile.Text)}");
                else
                    Append($"[GREŠKA] {err}");
            }
            catch (Exception ex) { Append($"[KRITIČNO] {ex.Message}"); }
        }

        private static bool IsAlreadyEncrypted(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext is ".tea" or ".lea" or ".ctr";
        }
    }
}