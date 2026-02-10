using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class DecryptPromptDialog : Form
    {
        // Paleta boja
        private readonly Color _bgColor = Color.FromArgb(32, 33, 36);
        private readonly Color _panelColor = Color.FromArgb(45, 46, 50);
        private readonly Color _accentColor = Color.FromArgb(66, 133, 244);
        private readonly Color _btnColor = Color.FromArgb(60, 64, 67);
        private readonly Color _textColor = Color.WhiteSmoke;
        private readonly Color _successColor = Color.FromArgb(129, 201, 149); // Svetlo zelena za tamnu temu
        private readonly Color _errorColor = Color.FromArgb(242, 139, 130);   // Svetlo crvena

        private readonly FileReceiver.ReceivedFileInfo _info;

        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Visible = false, Width = 220, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        private readonly Label lblAlgoDetected = new() { AutoSize = true, ForeColor = Color.FromArgb(129, 201, 149), Visible = false, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

        private readonly TextBox txtKey = new()
        {
            PlaceholderText = "Ključ – tačno 16 bajtova",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(60, 64, 67),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        private readonly Label lblKeyBytes = new() { AutoSize = true, ForeColor = Color.Gray };

        private readonly TextBox txtNonce = new()
        {
            PlaceholderText = "Nonce – tačno 8 bajtova",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(60, 64, 67),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, ForeColor = Color.Gray, Visible = false };

        private readonly Button btnOk = new() { Text = "Potvrdi", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        private readonly Button btnCancel = new() { Text = "Otkaži", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private string _algoToUse = "TEA";

        public DecryptPromptDialog(FileReceiver.ReceivedFileInfo info)
        {
            _info = info;

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Dešifrovanje fajla";
            StartPosition = FormStartPosition.CenterParent;
            BackColor = _bgColor;
            ForeColor = _textColor;
            Font = new Font("Segoe UI", 9.5f);
            MinimumSize = new Size(750, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
                Text = $"Primljen fajl: {info.FileName}",
                ForeColor = _accentColor,
                Padding = new Padding(0, 0, 0, 20)
            };

            // ALGORITAM RED
            var algoRow = CreateRowLayout();
            algoRow.Controls.Add(CreateLabel("Algoritam:"), 0, 0);

            if (info.DetectedAlgorithm is string detected)
            {
                _algoToUse = detected;
                lblAlgoDetected.Text = detected;
                lblAlgoDetected.Visible = true;
                algoRow.Controls.Add(lblAlgoDetected, 1, 0);
            }
            else
            {
                cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
                cmbAlgo.SelectedIndex = 0;
                cmbAlgo.Visible = true;
                algoRow.Controls.Add(cmbAlgo, 1, 0);
                cmbAlgo.SelectedIndexChanged += (_, __) => UpdateCtrVisibility(GetSelectedAlgo());
            }

            // KLJUČ RED
            var keyRow = CreateRowLayout();
            keyRow.Controls.Add(CreateLabel("Ključ (16):"), 0, 0);
            keyRow.Controls.Add(txtKey, 1, 0);
            keyRow.Controls.Add(lblKeyBytes, 2, 0);

            // NONCE RED
            var nonceRow = CreateRowLayout();
            nonceRow.Controls.Add(lblNonce, 0, 0);
            nonceRow.Controls.Add(txtNonce, 1, 0);
            nonceRow.Controls.Add(lblNonceBytes, 2, 0);
            // Podešavanje stila za lblNonce jer se kreira van CreateLabel
            lblNonce.Padding = new Padding(0, 8, 30, 8);
            lblNonce.ForeColor = Color.DarkGray;

            var outHint = new Label
            {
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Text = $"Izlazna putanja: {_info.DefaultDecryptedPath}",
                Padding = new Padding(0, 15, 0, 15)
            };

            // DUGMIĆI (1 pored drugog)
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 20, 0, 0),
                BackColor = Color.Transparent
            };

            btnOk.Width = 120; btnOk.Height = 40;
            btnOk.BackColor = _accentColor;
            btnOk.ForeColor = Color.White;
            btnOk.FlatAppearance.BorderSize = 0;

            btnCancel.Width = 120; btnCancel.Height = 40;
            btnCancel.BackColor = _btnColor;
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Margin = new Padding(0, 0, 10, 0);

            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);

            var main = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(30),
                AutoScroll = true
            };

            main.Controls.Add(title);
            main.Controls.Add(algoRow);
            main.Controls.Add(keyRow);
            main.Controls.Add(nonceRow);
            main.Controls.Add(outHint);
            main.Controls.Add(buttons);
            Controls.Add(main);

            // Eventi
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();
            btnOk.Click += (_, __) => OnOk();
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            UpdateKeyBytes();
            UpdateNonceBytes();
            UpdateCtrVisibility(info.DetectedAlgorithm ?? GetSelectedAlgo());
        }

        private TableLayoutPanel CreateRowLayout()
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true, Margin = new Padding(0, 5, 0, 5) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // Fiksna širina za labele da bi sve bilo poravnato
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return row;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(0, 8, 30, 8),
                ForeColor = Color.DarkGray
            };
        }

        private string GetSelectedAlgo()
            => cmbAlgo.Visible ? (cmbAlgo.SelectedItem?.ToString() ?? "TEA") : (_info.DetectedAlgorithm ?? "TEA");

        private void UpdateCtrVisibility(string algo)
        {
            bool isCtr = algo == "LEA-CTR";
            txtNonce.Visible = isCtr; lblNonce.Visible = isCtr; lblNonceBytes.Visible = isCtr;
            _algoToUse = algo;
        }

        private void UpdateKeyBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            lblKeyBytes.Text = $"{n} / 16 B";
            lblKeyBytes.ForeColor = (n == 16) ? _successColor : _errorColor;
            lblKeyBytes.Padding = new Padding(10, 8, 0, 0);
        }

        private void UpdateNonceBytes()
        {
            int n = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
            lblNonceBytes.Text = $"{n} / 8 B";
            lblNonceBytes.ForeColor = (n == 8) ? _successColor : _errorColor;
            lblNonceBytes.Padding = new Padding(10, 8, 0, 0);
        }

        private void OnOk()
        {
            int keyLen = Encoding.UTF8.GetByteCount(txtKey.Text ?? "");
            if (keyLen != 16)
            {
                MessageBox.Show("Ključ mora biti tačno 16 bajtova (trenutno: " + keyLen + ")", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_algoToUse == "LEA-CTR")
            {
                int nLen = Encoding.UTF8.GetByteCount(txtNonce.Text ?? "");
                if (nLen != 8)
                {
                    MessageBox.Show("Nonce mora biti tačno 8 bajtova.", "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        public FileReceiver.DecryptParams GetParams()
        {
            var algo = _algoToUse;
            byte[] key = Encoding.UTF8.GetBytes(txtKey.Text);
            byte[]? nonce = null;

            if (algo == "LEA-CTR")
                nonce = Encoding.UTF8.GetBytes(txtNonce.Text);

            return new FileReceiver.DecryptParams(algo, key, nonce, outputPath: null);
        }
    }
}