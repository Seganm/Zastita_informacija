using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class EncryptWindow : Form
    {
        // Paleta boja iz Main-a
        private readonly Color _bgColor = Color.FromArgb(32, 33, 36);
        private readonly Color _panelColor = Color.FromArgb(45, 46, 50);
        private readonly Color _accentColor = Color.FromArgb(66, 133, 244);
        private readonly Color _btnColor = Color.FromArgb(60, 64, 67);
        private readonly Color _textColor = Color.WhiteSmoke;
        private readonly Color _successColor = Color.FromArgb(129, 201, 149);
        private readonly Color _errorColor = Color.FromArgb(242, 139, 130);

        private readonly AppSettings _settings;

        private readonly TextBox txtInput = new() { ReadOnly = true, Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Button btnBrowseIn = new() { Text = "Pretraži", FlatStyle = FlatStyle.Flat, Width = 100, Cursor = Cursors.Hand };

        private readonly TextBox txtOutput = new() { ReadOnly = true, Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Button btnBrowseOut = new() { Text = "Izmeni", FlatStyle = FlatStyle.Flat, Width = 100, Cursor = Cursors.Hand };

        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White };

        private readonly TextBox txtKey = new() { PlaceholderText = "Unesite 16 bajtova ključa", Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label lblKeyBytes = new() { AutoSize = true, Padding = new Padding(10, 5, 0, 0) };

        private readonly TextBox txtNonce = new() { PlaceholderText = "Unesite 8 bajtova nonce", Visible = false, Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, Visible = false, Padding = new Padding(10, 5, 0, 0) };

        private readonly Button btnEncrypt = new() { Text = "KODIRAJ FAJL", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        private readonly Button btnClose = new() { Text = "ZATVORI", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private readonly OpenFileDialog ofd = new() { Title = "Odaberi ulazni fajl", Filter = "Svi fajlovi (*.*)|*.*" };
        private readonly SaveFileDialog sfd = new() { Title = "Sačuvaj kodirani fajl", Filter = "Svi fajlovi (*.*)|*.*" };

        public EncryptWindow(AppSettings settings)
        {
            _settings = settings;

            // Podešavanja prozora
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Ručna enkripcija fajlova";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 580);
            BackColor = _bgColor;
            ForeColor = _textColor;
            Font = new Font("Segoe UI", 9.5f);

            // --- GORNJI PANEL (Putanje i Algoritam) ---
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true,
                Padding = new Padding(30),
                BackColor = _panelColor
            };
            // Fiksna širina za labele kao u prethodnim prozorima
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Dodavanje komponenti u grid
            AddGridRow(grid, "Ulazni fajl:", txtInput, btnBrowseIn, 0);

            grid.Controls.Add(CreateLabel("Algoritam:"), 0, 1);
            cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
            cmbAlgo.SelectedIndex = 0;
            grid.Controls.Add(cmbAlgo, 1, 1);

            grid.Controls.Add(CreateLabel("Ključ:"), 0, 2);
            grid.Controls.Add(txtKey, 1, 2);
            grid.Controls.Add(lblKeyBytes, 2, 2);

            grid.Controls.Add(lblNonce, 0, 3);
            lblNonce.Padding = new Padding(0, 12, 30, 12);
            lblNonce.ForeColor = Color.DarkGray;
            grid.Controls.Add(txtNonce, 1, 3);
            grid.Controls.Add(lblNonceBytes, 2, 3);

            AddGridRow(grid, "Izlazni fajl:", txtOutput, btnBrowseOut, 4);

            // --- DONJI PANEL (Dugmići) ---
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 30, 30, 0),
                AutoSize = true
            };

            btnClose.Width = 140; btnClose.Height = 45;
            btnClose.BackColor = _btnColor;
            btnClose.FlatAppearance.BorderSize = 0;

            btnEncrypt.Width = 180; btnEncrypt.Height = 45;
            btnEncrypt.BackColor = _accentColor;
            btnEncrypt.ForeColor = Color.White;
            btnEncrypt.FlatAppearance.BorderSize = 0;
            btnEncrypt.Margin = new Padding(15, 0, 0, 0);

            buttonPanel.Controls.Add(btnEncrypt);
            buttonPanel.Controls.Add(btnClose);

            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.Controls.Add(grid, 0, 0);
            mainLayout.Controls.Add(buttonPanel, 0, 1);
            Controls.Add(mainLayout);

            // Eventi
            btnBrowseIn.Click += (_, __) => BrowseInput();
            btnBrowseOut.Click += (_, __) => BrowseOutput();
            btnEncrypt.Click += (_, __) => DoEncrypt();
            btnClose.Click += (_, __) => Close();
            cmbAlgo.SelectedIndexChanged += (_, __) => UpdateAlgoUi();
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();

            UpdateAlgoUi();
            UpdateKeyBytes();
            UpdateNonceBytes();
        }

        private void AddGridRow(TableLayoutPanel grid, string labelText, Control input, Control btn, int row)
        {
            grid.Controls.Add(CreateLabel(labelText), 0, row);
            grid.Controls.Add(input, 1, row);
            if (btn != null)
            {
                btn.Margin = new Padding(10, 5, 0, 5);
                grid.Controls.Add(btn, 2, row);
            }
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(0, 12, 30, 12), // 30px desno kao u Mainu
                ForeColor = Color.DarkGray
            };
        }

        // --- LOGIKA (Ostaje ista) ---

        private void BrowseInput()
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            txtInput.Text = ofd.FileName;
            ProposeOutputPath();
        }

        private void BrowseOutput()
        {
            ProposeOutputPath();
            try
            {
                var dir = Path.GetDirectoryName(txtOutput.Text);
                sfd.InitialDirectory = Directory.Exists(dir!) ? dir : _settings.EncryptedFolder;
            }
            catch { }
            sfd.FileName = Path.GetFileName(txtOutput.Text);
            if (sfd.ShowDialog(this) == DialogResult.OK) txtOutput.Text = sfd.FileName;
        }

        private void UpdateAlgoUi()
        {
            bool isCtr = (cmbAlgo.SelectedItem?.ToString() == "LEA-CTR");
            txtNonce.Visible = isCtr; lblNonce.Visible = isCtr; lblNonceBytes.Visible = isCtr;
            ProposeOutputPath();
        }

        private void ProposeOutputPath()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtInput.Text) || !File.Exists(txtInput.Text)) { txtOutput.Text = ""; return; }
                string ext = cmbAlgo.SelectedItem?.ToString() switch { "TEA" => ".tea", "LEA" => ".lea", "LEA-CTR" => ".ctr", _ => ".enc" };
                string name = Path.GetFileName(txtInput.Text) + ext;
                string destDir = Directory.Exists(_settings.EncryptedFolder) ? _settings.EncryptedFolder : Path.GetDirectoryName(txtInput.Text)!;
                txtOutput.Text = Path.Combine(destDir, name);
            }
            catch { txtOutput.Text = ""; }
        }

        private void UpdateKeyBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            lblKeyBytes.Text = $"{n} / 16 B";
            lblKeyBytes.ForeColor = (n == 16) ? _successColor : _errorColor;
        }

        private void UpdateNonceBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
            lblNonceBytes.Text = $"{n} / 8 B";
            lblNonceBytes.ForeColor = (n == 8) ? _successColor : _errorColor;
        }

        private void DoEncrypt()
        {
            if (!File.Exists(txtInput.Text)) { MessageBox.Show("Ulazni fajl ne postoji."); return; }
            int keyLen = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            if (keyLen != 16) { MessageBox.Show("Ključ mora biti 16 bajtova."); return; }
            byte[] key = Encoding.UTF8.GetBytes(txtKey.Text);

            string algo = cmbAlgo.SelectedItem?.ToString() ?? "TEA";
            byte[]? nonce = null;
            if (algo == "LEA-CTR")
            {
                if (Encoding.UTF8.GetByteCount(txtNonce.Text ?? "") != 8) { MessageBox.Show("Nonce mora biti 8 bajtova."); return; }
                nonce = Encoding.UTF8.GetBytes(txtNonce.Text);
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(txtOutput.Text)!);
                byte[] input = File.ReadAllBytes(txtInput.Text);
                byte[] enc = algo switch
                {
                    "TEA" => TEA.Encrypt(input, key),
                    "LEA" => LEA.Encrypt(input, key),
                    "LEA-CTR" => CTR.Process(input, key, nonce!),
                    _ => throw new Exception("Greška")
                };
                File.WriteAllBytes(txtOutput.Text, enc);
                MessageBox.Show("Fajl uspešno kodiran!");
            }
            catch (Exception ex) { MessageBox.Show("Greška: " + ex.Message); }
        }
    }
}