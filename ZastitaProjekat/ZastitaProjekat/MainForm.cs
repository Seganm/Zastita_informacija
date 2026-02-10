using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class MainForm : Form
    {
        private readonly Color _bgColor = Color.FromArgb(32, 33, 36);
        private readonly Color _panelColor = Color.FromArgb(45, 46, 50);
        private readonly Color _accentColor = Color.FromArgb(66, 133, 244);
        private readonly Color _btnColor = Color.FromArgb(60, 64, 67);
        private readonly Color _textColor = Color.WhiteSmoke;

        private TextBox txtTarget = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        private TextBox txtX = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        private TextBox txtRecv = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };

        private Button btnBrowseTarget = new() { Text = "Traži", FlatStyle = FlatStyle.Flat };
        private Button btnBrowseX = new() { Text = "Traži", FlatStyle = FlatStyle.Flat };
        private Button btnBrowseRecv = new() { Text = "Traži", FlatStyle = FlatStyle.Flat };
        private Button btnSavePaths = new() { Text = "Sačuvaj konfiguraciju putanja", FlatStyle = FlatStyle.Flat };

        private Button btnFSW = new() { Text = "1) FSW", Enabled = false, FlatStyle = FlatStyle.Flat };
        private Button btnEnc = new() { Text = "2) Kodiraj ", Enabled = false, FlatStyle = FlatStyle.Flat };
        private Button btnDec = new() { Text = "3) Dekodiraj ", Enabled = false, FlatStyle = FlatStyle.Flat };
        private Button btnSend = new() { Text = "4) Pošalji", Enabled = false, FlatStyle = FlatStyle.Flat };
        private Button btnRecv = new() { Text = "5) Primi ", Enabled = false, FlatStyle = FlatStyle.Flat };
        private Button btnSha = new() { Text = "6) SHA-256", Enabled = false, FlatStyle = FlatStyle.Flat };

        private StatusStrip status = new();
        private ToolStripStatusLabel lblStatus = new() { Text = "Spremno." };

        private FolderBrowserDialog fbd = new();
        private OpenFileDialog ofd = new() { Title = "Odaberi fajl", Filter = "Svi fajlovi (*.*)|*.*" };

        private AppSettings settings;

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Kripto Menadžer - Zaštita informacija";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 600);
            BackColor = _bgColor;
            Font = new Font("Segoe UI", 9.5f);
            ForeColor = _textColor;

            ApplyCustomStyles();

            status.Items.Add(lblStatus);
            status.BackColor = _panelColor;
            status.ForeColor = _textColor;

            // --- GORNJI PANEL (Putanje) ---
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true,
                Padding = new Padding(30), // Veći padding oko celog panela
                BackColor = _panelColor
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            grid.Controls.Add(CreateLabel("Direktorijum"), 0, 0);
            grid.Controls.Add(txtTarget, 1, 0);
            grid.Controls.Add(btnBrowseTarget, 2, 0);

            grid.Controls.Add(CreateLabel("Šifrovani fajlovi"), 0, 1);
            grid.Controls.Add(txtX, 1, 1);
            grid.Controls.Add(btnBrowseX, 2, 1);

            grid.Controls.Add(CreateLabel("Primljeni fajlovi:"), 0, 2);
            grid.Controls.Add(txtRecv, 1, 2);
            grid.Controls.Add(btnBrowseRecv, 2, 2);

            grid.Controls.Add(btnSavePaths, 1, 3);
            btnSavePaths.Margin = new Padding(0, 20, 0, 0);
            btnSavePaths.BackColor = _accentColor;
            btnSavePaths.ForeColor = Color.White;
            btnSavePaths.Height = 35;

            // --- DONJI PANEL (Operacije - Horizontalno) ---
            var actionsContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };

            var lblTitle = new Label
            {
                Text = "OPERACIJE:",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = _accentColor,
                Location = new Point(30, 10)
            };

            var actionsFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight, // Poredjani jedan pored drugog
                WrapContents = true, // Ako nema mesta, prelaze u novi red
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 40, 0, 0), // Ostavlja mesta za naslov iznad
                BackColor = Color.Transparent
            };

            var cmdButtons = new[] { btnFSW, btnEnc, btnDec, btnSend, btnRecv, btnSha };
            foreach (var b in cmdButtons)
            {
                b.Width = 280; // Smanjena širina da bi stali jedan pored drugog
                b.Height = 60; // Malo viši za bolji klik
                b.Margin = new Padding(0, 0, 15, 15); // Razmak između dugmića
                b.FlatAppearance.BorderSize = 0;
                b.BackColor = _btnColor;
                b.Cursor = Cursors.Hand;
                actionsFlow.Controls.Add(b);
            }

            actionsContainer.Controls.Add(lblTitle);
            actionsContainer.Controls.Add(actionsFlow);

            // GLAVNI RASPORED
            var main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.Controls.Add(grid, 0, 0);
            main.Controls.Add(actionsContainer, 0, 1);
            main.Controls.Add(status, 0, 2);

            Controls.Add(main);

            // LOGIKA (Nepromenjeno)
            settings = Settings.Load();
            txtTarget.Text = settings.TargetFolder;
            txtX.Text = settings.EncryptedFolder;
            txtRecv.Text = settings.ReceivedFolder;

            btnBrowseTarget.Click += (_, __) => BrowseInto(txtTarget);
            btnBrowseX.Click += (_, __) => BrowseInto(txtX);
            btnBrowseRecv.Click += (_, __) => BrowseInto(txtRecv);
            btnSavePaths.Click += btnSavePaths_Click;

            EnableFeatureButtons(Directory.Exists(settings.TargetFolder) &&
                                 Directory.Exists(settings.EncryptedFolder) &&
                                 Directory.Exists(settings.ReceivedFolder));

            btnFSW.Click += (_, __) => { using var win = new FswWindow(settings); win.ShowDialog(this); };
            btnEnc.Click += (_, __) => { using var win = new EncryptWindow(settings); win.ShowDialog(this); };
            btnDec.Click += (_, __) => { using var win = new DecryptWindow(settings); win.ShowDialog(this); };
            btnSend.Click += (_, __) => { using var win = new SendWindow(settings); win.ShowDialog(this); };
            btnRecv.Click += (_, __) => { using var win = new ReceiveWindow(settings); win.ShowDialog(this); };
            btnSha.Click += BtnSha_Click;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                // Povećan Padding desno (sa 10 na 30) da odmaknemo tekst od inputa
                Padding = new Padding(0, 12, 30, 12),
                ForeColor = Color.DarkGray
            };
        }

        private void ApplyCustomStyles()
        {
            var boxes = new[] { txtTarget, txtX, txtRecv };
            foreach (var tb in boxes)
            {
                tb.BackColor = _btnColor;
                tb.ForeColor = Color.White;
                tb.Margin = new Padding(0, 10, 0, 10);
            }

            var browseBtns = new[] { btnBrowseTarget, btnBrowseX, btnBrowseRecv };
            foreach (var b in browseBtns)
            {
                b.BackColor = _btnColor;
                b.FlatAppearance.BorderSize = 0;
                b.Width = 100;
                b.Height = 30;
                b.Margin = new Padding(10, 10, 0, 10);
            }
        }

        private void btnSavePaths_Click(object? sender, EventArgs e)
        {
            try
            {
                EnsureDir(txtTarget.Text); EnsureDir(txtX.Text); EnsureDir(txtRecv.Text);
                settings.TargetFolder = txtTarget.Text;
                settings.EncryptedFolder = txtX.Text;
                settings.ReceivedFolder = txtRecv.Text;
                Settings.Save(settings);
                lblStatus.Text = "Podešavanja sačuvana.";
                EnableFeatureButtons(true);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnSha_Click(object? sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                string hex = ComputeSha256HexStreaming(ofd.FileName);
                using var dlg = new Sha256Dialog(ofd.FileName, hex);
                dlg.ShowDialog(this);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private static string ComputeSha256HexStreaming(string filePath)
        {
            using var sha = SHA256.Create();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return ToHex(sha.ComputeHash(fs));
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private void EnableFeatureButtons(bool enabled)
        {
            var btns = new[] { btnFSW, btnEnc, btnDec, btnSend, btnRecv, btnSha };
            foreach (var b in btns)
            {
                b.Enabled = enabled;
                b.ForeColor = enabled ? _textColor : Color.FromArgb(100, _textColor);
            }
        }

        private void BrowseInto(TextBox target)
        {
            if (Directory.Exists(target.Text)) fbd.SelectedPath = target.Text;
            if (fbd.ShowDialog(this) == DialogResult.OK) target.Text = fbd.SelectedPath;
        }

        private static void EnsureDir(string path)
        {
            if (!string.IsNullOrWhiteSpace(path)) Directory.CreateDirectory(path);
        }
    }
}