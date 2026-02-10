using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoApp.GUI
{
    public sealed class FswWindow : Form
    {
        private readonly AppSettings _settings;

 
        private readonly Color _bgColor = Color.FromArgb(32, 33, 36);
        private readonly Color _panelColor = Color.FromArgb(45, 46, 50);
        private readonly Color _accentColor = Color.FromArgb(66, 133, 244);
        private readonly Color _btnColor = Color.FromArgb(60, 64, 67);
        private readonly Color _textColor = Color.WhiteSmoke;
        private readonly Color _successColor = Color.FromArgb(129, 201, 149);
        private readonly Color _errorColor = Color.FromArgb(242, 139, 130);

        private readonly ComboBox cmbAlgo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White };
        private readonly TextBox txtKey = new() { PlaceholderText = "Unesite 16 bajtova ključa", Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label lblKeyBytes = new() { AutoSize = true, Padding = new Padding(10, 5, 0, 0) };

        private readonly TextBox txtNonce = new() { PlaceholderText = "8 bajtova", Visible = false, Dock = DockStyle.Fill, BackColor = Color.FromArgb(60, 64, 67), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label lblNonce = new() { Text = "Nonce (8):", AutoSize = true, Visible = false };
        private readonly Label lblNonceBytes = new() { AutoSize = true, Visible = false, Padding = new Padding(10, 5, 0, 0) };

    
        private readonly Button btnStart = new() { Text = "POKRENI NADZOR", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        private readonly Button btnStop = new() { Text = "PREKINI", FlatStyle = FlatStyle.Flat, Enabled = false, Cursor = Cursors.Hand };
        private readonly Button btnClear = new() { Text = "OČISTI LOG", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        private readonly Button btnClose = new() { Text = "NAZAD", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private readonly TextBox txtLog = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(180, 255, 180), // Matrix stil
            BorderStyle = BorderStyle.None
        };

        private FileSystemWatcher? _watcher;
        private volatile bool _running = false;
        private static readonly HashSet<string> _processing = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _gate = new();

        public FswWindow(AppSettings settings)
        {
            _settings = settings;

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Automatsko Nadgledanje Foldera (FSW)";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1100, 700);
            BackColor = _bgColor;
            ForeColor = _textColor;
            Font = new Font("Segoe UI", 9.5f);

      
            var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = _panelColor, Padding = new Padding(20, 10, 20, 10) };
            var lblPaths = new Label
            {
                AutoSize = true,
                Text = $"AKTIVNI FOLDER: {_settings.TargetFolder}\nIZLAZNI FOLDER: {_settings.EncryptedFolder}",
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI Semibold", 9f),
                Location = new Point(20, 20)
            };
            header.Controls.Add(lblPaths);

        
            var configPanel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, AutoSize = true, Padding = new Padding(20), BackColor = _bgColor };
            configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); 
            configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));   
            configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); 
            configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));  

         
            configPanel.Controls.Add(CreateLabel("Algoritam:"), 0, 0);
            cmbAlgo.Items.AddRange(new object[] { "TEA", "LEA", "LEA-CTR" });
            cmbAlgo.SelectedIndex = 0;
            configPanel.Controls.Add(cmbAlgo, 1, 0);

            btnStart.Size = new Size(180, 40); btnStart.BackColor = _accentColor; btnStart.ForeColor = Color.White;
            btnStart.FlatAppearance.BorderSize = 0;
            configPanel.Controls.Add(btnStart, 3, 0);

          
            configPanel.Controls.Add(CreateLabel("Ključ (16):"), 0, 1);
            configPanel.Controls.Add(txtKey, 1, 1);
            configPanel.Controls.Add(lblKeyBytes, 2, 1);

            btnStop.Size = new Size(180, 40); btnStop.BackColor = _btnColor;
            btnStop.FlatAppearance.BorderSize = 0;
            configPanel.Controls.Add(btnStop, 3, 1);

        
            configPanel.Controls.Add(lblNonce, 0, 2);
            lblNonce.Padding = new Padding(0, 12, 30, 12); lblNonce.ForeColor = Color.DarkGray;
            configPanel.Controls.Add(txtNonce, 1, 2);
            configPanel.Controls.Add(lblNonceBytes, 2, 2);

            btnClear.Size = new Size(180, 40); btnClear.BackColor = _btnColor;
            btnClear.FlatAppearance.BorderSize = 0;
            configPanel.Controls.Add(btnClear, 3, 2);

       
            btnClose.Size = new Size(180, 40); btnClose.BackColor = _btnColor;
            btnClose.FlatAppearance.BorderSize = 0;
            configPanel.Controls.Add(btnClose, 3, 3);

      
            var logContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20) };
            var logLabel = new Label { Text = "ISTORIJA AKTIVNOSTI:", Dock = DockStyle.Top, Height = 30, ForeColor = _accentColor, Font = new Font("Segoe UI Bold", 9f) };
            logContainer.Controls.Add(txtLog);
            logContainer.Controls.Add(logLabel);

       
            Controls.Add(logContainer);
            Controls.Add(configPanel);
            Controls.Add(header);

       
            cmbAlgo.SelectedIndexChanged += (_, __) => UpdateCtrVisibility();
            txtKey.TextChanged += (_, __) => UpdateKeyBytes();
            txtNonce.TextChanged += (_, __) => UpdateNonceBytes();
            btnStart.Click += (_, __) => StartWatch();
            btnStop.Click += (_, __) => StopWatch();
            btnClear.Click += (_, __) => txtLog.Clear();
            btnClose.Click += (_, __) => Close();
            FormClosing += (_, e) => StopWatch();

            UpdateCtrVisibility();
            UpdateKeyBytes();
            UpdateNonceBytes();
            Append($"[SISTEM] Prozor spreman. Izabrani folder: {_settings.TargetFolder}");
        }

        private Label CreateLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Padding = new Padding(0, 12, 30, 12), ForeColor = Color.DarkGray };
        }

        private void Append(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Append), line); return; }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        }

        // --- LOGIKA (Ostaje nepromenjena) ---

        private void UpdateCtrVisibility()
        {
            bool isCtr = (cmbAlgo.SelectedItem?.ToString() == "LEA-CTR");
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

        private void StartWatch()
        {
            if (_running) return;
            if (Encoding.UTF8.GetByteCount(txtKey.Text ?? "") != 16) { MessageBox.Show("Ključ mora biti 16 bajtova."); return; }

            _watcher = new FileSystemWatcher(_settings.TargetFolder)
            {
                IncludeSubdirectories = false,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size
            };
            _watcher.Created += OnFileCreated;
            _watcher.EnableRaisingEvents = true;

            _running = true; btnStart.Enabled = false; btnStop.Enabled = true;
            btnStart.BackColor = Color.Gray;
            Append(">>> NADZOR AKTIVIRAN");
        }

        private void StopWatch()
        {
            if (!_running) return;
            if (_watcher != null) { _watcher.EnableRaisingEvents = false; _watcher.Dispose(); _watcher = null; }
            _running = false; btnStart.Enabled = true; btnStop.Enabled = false;
            btnStart.BackColor = _accentColor;
            Append("<<< NADZOR ZAUSTAVLJEN");
        }

        private void OnFileCreated(object? sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            if (Path.GetExtension(path).ToLower() is ".tmp" or ".part") return;
            lock (_gate) { if (_processing.Contains(path)) return; _processing.Add(path); }
            Task.Run(() => ProcessNewFileSafe(path));
        }

        private void ProcessNewFileSafe(string filePath)
        {
            try
            {
                if (!WaitForFileReady(filePath, 30000, 400, 2)) return;
                byte[] data = File.ReadAllBytes(filePath);
                var algo = cmbAlgo.SelectedItem?.ToString() ?? "TEA";
                byte[] key = Encoding.UTF8.GetBytes(txtKey.Text);
                string outExt = algo switch { "TEA" => ".tea", "LEA" => ".lea", "LEA-CTR" => ".ctr", _ => ".enc" };

                if (algo == "LEA-CTR")
                {
                    if (Encoding.UTF8.GetByteCount(txtNonce.Text ?? "") != 8) { Append("[GREŠKA] Nonce nije 8 bajtova."); return; }
                    data = CTR.Process(data, key, Encoding.UTF8.GetBytes(txtNonce.Text));
                }
                else if (algo == "TEA") data = TEA.Encrypt(data, key);
                else data = LEA.Encrypt(data, key);

                string outputPath = Path.Combine(_settings.EncryptedFolder, Path.GetFileName(filePath) + outExt);
                File.WriteAllBytes(outputPath, data);
                Append($"[OK] {Path.GetFileName(filePath)} -> {outExt}");
            }
            catch (Exception ex) { Append($"[GREŠKA] {ex.Message}"); }
            finally { lock (_gate) { _processing.Remove(filePath); } }
        }

        private static bool WaitForFileReady(string path, int maxWaitMs, int settleMs, int stableReadsRequired)
        {
            var sw = Stopwatch.StartNew();
            long lastLen = -1; int stableCount = 0;
            while (sw.ElapsedMilliseconds < maxWaitMs)
            {
                try
                {
                    if (!File.Exists(path)) { Thread.Sleep(settleMs); continue; }
                    long len = new FileInfo(path).Length;
                    if (len == lastLen && len > 0) { if (++stableCount >= stableReadsRequired) return true; }
                    else { stableCount = 0; lastLen = len; }
                }
                catch { }
                Thread.Sleep(settleMs);
            }
            return false;
        }
    }
}