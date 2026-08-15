using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleDecrypt
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly TextBox input = new TextBox();
        private readonly TextBox output = new TextBox();
        private readonly TextBox key = new TextBox();
        private readonly TextBox iv = new TextBox();
        private readonly Label keyLabel = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
        private readonly Label ivLabel = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = "IV:" };
        private readonly Label keyHint = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
        private readonly Label ivHint = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = "CBC IV" };
        private readonly ComboBox keySize = new ComboBox();
        private readonly ComboBox algorithm = new ComboBox();
        private readonly RadioButton encrypt = new RadioButton { Text = "Encrypt", Checked = true, AutoSize = true };
        private readonly RadioButton decrypt = new RadioButton { Text = "Decrypt", AutoSize = true };
        private readonly RadioButton fileMode = new RadioButton { Text = "File", Checked = true, AutoSize = true };
        private readonly RadioButton folderMode = new RadioButton { Text = "Folder", AutoSize = true };
        private readonly CheckBox includeSubfolders = new CheckBox { Text = "Include subfolders", Checked = true, AutoSize = true };
        private readonly CheckBox overwrite = new CheckBox { Text = "Overwrite existing files", AutoSize = true };
        private readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };
        private readonly Label status = new Label { AutoSize = true, Text = "Drop a file or folder here." };
        private readonly Button runButton = new Button { Text = "Process", Width = 100, Height = 30 };
        private readonly Button helpButton = new Button { Text = "Help", Width = 70, Height = 26 };

        public MainForm()
        {
            Text = "SimpleCrypt - File Encryptor";
            Width = 720;
            Height = 470;
            MinimumSize = new System.Drawing.Size(650, 420);
            StartPosition = FormStartPosition.CenterScreen;
            AllowDrop = true;

            input.AllowDrop = true;
            input.ReadOnly = true;
            output.ReadOnly = true;
            key.UseSystemPasswordChar = true;
            iv.UseSystemPasswordChar = true;

            algorithm.DropDownStyle = ComboBoxStyle.DropDownList;
            algorithm.Items.AddRange(new object[] { "AES-CBC", "DES-CBC", "TripleDES-CBC", "RC2-CBC" });
            algorithm.SelectedIndex = 0;

            encrypt.CheckedChanged += delegate { UpdateOutputSuggestion(); };
            decrypt.CheckedChanged += delegate { UpdateOutputSuggestion(); };
            fileMode.CheckedChanged += delegate { UpdateOutputSuggestion(); UpdateModeControls(); };
            folderMode.CheckedChanged += delegate { UpdateOutputSuggestion(); UpdateModeControls(); };
            algorithm.SelectedIndexChanged += delegate { UpdateParameterHints(); };
            runButton.Click += async delegate { await RunAsync(); };
            helpButton.Click += delegate { ShowHelp(); };
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 3, RowCount = 12 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            for (var i = 0; i < 11; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 1 ? 48 : 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var topBar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            topBar.Controls.Add(new Label { Text = "File Encryptor", Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 5, 12, 0) });
            topBar.Controls.Add(helpButton);
            layout.Controls.Add(topBar, 0, 0);
            layout.SetColumnSpan(topBar, 3);

            var drop = new Label { Text = "Drop a file or folder here", TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.AliceBlue, AllowDrop = true };
            drop.DragEnter += OnDragEnter;
            drop.DragDrop += OnDragDrop;
            layout.Controls.Add(drop, 0, 1);
            layout.SetColumnSpan(drop, 3);

            AddChoiceRow(layout, "Operation:", new Control[] { encrypt, decrypt }, 2);
            AddChoiceRow(layout, "Input type:", new Control[] { fileMode, folderMode }, 3);
            AddPathRow(layout, "Input:", input, 4, "Browse", BrowseInput);
            AddPathRow(layout, "Output:", output, 5, "Browse", BrowseOutput);
            AddComboRow(layout, "Algorithm:", algorithm, 6);
            AddParameterRows(layout, 7);

            var options = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            options.Controls.Add(includeSubfolders);
            options.Controls.Add(overwrite);
            layout.Controls.Add(options, 1, 9);
            layout.SetColumnSpan(options, 2);

            layout.Controls.Add(progress, 1, 10);
            layout.SetColumnSpan(progress, 2);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            actions.Controls.Add(runButton);
            layout.Controls.Add(actions, 1, 11);
            layout.Controls.Add(status, 2, 11);

            Controls.Add(layout);
            UpdateParameterHints();
            UpdateModeControls();
        }

        private void AddChoiceRow(TableLayoutPanel layout, string label, Control[] controls, int row)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            foreach (var control in controls) panel.Controls.Add(control);
            layout.Controls.Add(panel, 1, row);
            layout.SetColumnSpan(panel, 2);
        }

        private void AddPathRow(TableLayoutPanel layout, string label, TextBox box, int row, string buttonText, Action action)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            box.Dock = DockStyle.Fill;
            layout.Controls.Add(box, 1, row);
            var button = new Button { Text = buttonText, Dock = DockStyle.Fill };
            button.Click += delegate { action(); };
            layout.Controls.Add(button, 2, row);
        }

        private void AddComboRow(TableLayoutPanel layout, string label, ComboBox box, int row)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            box.Dock = DockStyle.Left;
            box.Width = 180;
            layout.Controls.Add(box, 1, row);

            keySize.DropDownStyle = ComboBoxStyle.DropDownList;
            keySize.Width = 120;
            layout.Controls.Add(keySize, 2, row);
        }

        private void AddParameterRows(TableLayoutPanel layout, int row)
        {
            layout.Controls.Add(keyLabel, 0, row);
            key.Dock = DockStyle.Fill;
            layout.Controls.Add(key, 1, row);
            layout.Controls.Add(keyHint, 2, row);

            layout.Controls.Add(ivLabel, 0, row + 1);
            iv.Dock = DockStyle.Fill;
            layout.Controls.Add(iv, 1, row + 1);
            layout.Controls.Add(ivHint, 2, row + 1);

        }

        private void UpdateModeControls()
        {
            includeSubfolders.Enabled = folderMode.Checked;
            overwrite.Enabled = true;
            output.ReadOnly = true;
        }

        private void ShowHelp()
        {
            var text =
                "Key and IV input\n\n" +
                "1. Plain text\n" +
                "   Example: my-secret-key\n" +
                "   The text is converted to UTF-8 bytes. The byte length must match the algorithm.\n\n" +
                "2. Hexadecimal\n" +
                "   Prefix with hex: or 0x. Spaces and hyphens are allowed.\n" +
                "   Example: hex:00112233445566778899AABBCCDDEEFF\n" +
                "   Example: 0x0011 2233 4455 6677 8899 AABB CCDD EEFF\n\n" +
                "Required lengths\n" +
                "AES-CBC: 16, 24 or 32-byte Key; 16-byte IV\n" +
                "DES-CBC: 8-byte Key; 8-byte IV\n" +
                "TripleDES-CBC: 16 or 24-byte Key; 8-byte IV\n" +
                "RC2-CBC: 5 to 16-byte Key; 8-byte IV\n\n" +
                "The tool does not add a custom file header or detect the algorithm automatically.\n" +
                "Select the same algorithm, Key and IV when decrypting.";
            MessageBox.Show(this, text, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateParameterHints()
        {
            var name = algorithm.SelectedItem as string;
            Text = "SimpleCrypt - " + (name ?? "File Encryptor");
            keySize.Items.Clear();
            if (name == "AES-CBC")
            {
                keyLabel.Text = "AES Key:";
                keyHint.Text = "16 / 24 / 32 bytes";
                ivHint.Text = "16-byte IV";
                keySize.Items.AddRange(new object[] { "128-bit", "192-bit", "256-bit" });
            }
            else if (name == "DES-CBC")
            {
                keyLabel.Text = "DES Key:";
                keyHint.Text = "8 bytes";
                ivHint.Text = "8-byte IV";
                keySize.Items.Add("64-bit");
            }
            else if (name == "TripleDES-CBC")
            {
                keyLabel.Text = "3DES Key:";
                keyHint.Text = "16 or 24 bytes";
                ivHint.Text = "8-byte IV";
                keySize.Items.AddRange(new object[] { "128-bit", "192-bit" });
            }
            else
            {
                keyLabel.Text = "RC2 Key:";
                keyHint.Text = "5 to 16 bytes";
                ivHint.Text = "8-byte IV";
                keySize.Items.AddRange(new object[] { "40-bit", "64-bit", "128-bit" });
            }
            keySize.SelectedIndex = 0;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var paths = e.Data == null ? null : e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length == 0) return;
            var path = paths[0];
            if (Directory.Exists(path)) folderMode.Checked = true;
            else if (File.Exists(path)) fileMode.Checked = true;
            SetInput(path);
        }

        private void BrowseInput()
        {
            if (folderMode.Checked)
            {
                using (var dialog = new FolderBrowserDialog { Description = "Select input folder" })
                    if (dialog.ShowDialog(this) == DialogResult.OK) SetInput(dialog.SelectedPath);
            }
            else
            {
                using (var dialog = new OpenFileDialog { Title = "Select input file" })
                    if (dialog.ShowDialog(this) == DialogResult.OK) SetInput(dialog.FileName);
            }
        }

        private void BrowseOutput()
        {
            if (folderMode.Checked)
            {
                using (var dialog = new FolderBrowserDialog { Description = "Select output folder" })
                    if (dialog.ShowDialog(this) == DialogResult.OK) output.Text = Path.GetFullPath(dialog.SelectedPath);
            }
            else
            {
                using (var dialog = new SaveFileDialog { Title = "Select output file", FileName = Path.GetFileName(output.Text) })
                    if (dialog.ShowDialog(this) == DialogResult.OK) output.Text = Path.GetFullPath(dialog.FileName);
            }
        }

        private void SetInput(string path)
        {
            input.Text = Path.GetFullPath(path);
            UpdateOutputSuggestion();
        }

        private void UpdateOutputSuggestion()
        {
            if (string.IsNullOrWhiteSpace(input.Text)) return;
            if (folderMode.Checked)
            {
                output.Text = Path.Combine(Path.GetDirectoryName(input.Text.TrimEnd(Path.DirectorySeparatorChar)) ?? input.Text,
                    Path.GetFileName(input.Text.TrimEnd(Path.DirectorySeparatorChar)) + (encrypt.Checked ? ".encrypted" : ".decrypted"));
                return;
            }

            if (encrypt.Checked) output.Text = input.Text + ".enc";
            else output.Text = input.Text.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)
                ? input.Text.Substring(0, input.Text.Length - 4)
                : input.Text + ".decrypted";
        }

        private async Task RunAsync()
        {
            try
            {
                ValidateInput();
                var source = input.Text;
                var destination = output.Text;
                var algorithmName = (string)algorithm.SelectedItem;
                var keyBytes = ParseBytes(key.Text, "Key");
                var ivBytes = ParseBytes(iv.Text, "IV");
                ValidateKeyAndIv(algorithmName, keyBytes, ivBytes);

                runButton.Enabled = false;
                progress.Value = 0;
                status.Text = "Processing...";
                await Task.Run(delegate
                {
                    if (fileMode.Checked) ProcessFile(source, destination, encrypt.Checked, algorithmName, keyBytes, ivBytes);
                    else ProcessFolder(source, destination, encrypt.Checked, algorithmName, keyBytes, ivBytes);
                });
                progress.Value = 100;
                status.Text = "Completed.";
                MessageBox.Show(this, "Completed.\nOutput: " + destination, "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                MessageBox.Show(this, ex.Message, "Operation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                runButton.Enabled = true;
            }
        }

        private void ValidateInput()
        {
            if (fileMode.Checked && !File.Exists(input.Text)) throw new InvalidOperationException("Input file does not exist.");
            if (folderMode.Checked && !Directory.Exists(input.Text)) throw new InvalidOperationException("Input folder does not exist.");
            if (string.IsNullOrWhiteSpace(output.Text)) throw new InvalidOperationException("Please select an output path.");
            if (fileMode.Checked && PathsEqual(input.Text, output.Text)) throw new InvalidOperationException("Output file must be different from input file.");
            if (folderMode.Checked && PathsEqual(input.Text, output.Text)) throw new InvalidOperationException("Output folder must be different from input folder.");
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(Path.GetFullPath(left).TrimEnd('\\'), Path.GetFullPath(right).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        private void ProcessFolder(string source, string destination, bool doEncrypt, string algorithmName, byte[] keyBytes, byte[] ivBytes)
        {
            var files = Directory.GetFiles(source, "*", includeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            Directory.CreateDirectory(destination);
            for (var i = 0; i < files.Length; i++)
            {
                var relative = files[i].Substring(source.TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar);
                var targetRelative = doEncrypt ? relative + ".enc" : RemoveEncSuffix(relative);
                var target = Path.Combine(destination, targetRelative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                ProcessFile(files[i], target, doEncrypt, algorithmName, keyBytes, ivBytes);
                var value = (int)((i + 1) * 100L / Math.Max(files.Length, 1));
                BeginInvoke((Action)delegate { progress.Value = value; status.Text = "Processed " + (i + 1) + "/" + files.Length; });
            }
        }

        private static string RemoveEncSuffix(string path)
        {
            return path.EndsWith(".enc", StringComparison.OrdinalIgnoreCase) ? path.Substring(0, path.Length - 4) : path + ".decrypted";
        }

        private void ProcessFile(string source, string destination, bool doEncrypt, string algorithmName, byte[] keyBytes, byte[] ivBytes)
        {
            if (File.Exists(destination) && !overwrite.Checked) throw new IOException("Output file already exists: " + destination);
            if (PathsEqual(source, destination)) throw new InvalidOperationException("Output file must be different from input file: " + source);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)));
            var temp = destination + ".tmp";
            try
            {
                using (var cipher = CreateCipher(algorithmName, keyBytes, ivBytes))
                using (var inputStream = File.OpenRead(source))
                using (var outputStream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var crypto = new CryptoStream(outputStream, doEncrypt ? cipher.CreateEncryptor() : cipher.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    inputStream.CopyTo(crypto);
                    crypto.FlushFinalBlock();
                }
                if (File.Exists(destination)) File.Delete(destination);
                File.Move(temp, destination);
            }
            catch
            {
                if (File.Exists(temp)) File.Delete(temp);
                throw;
            }
        }

        private static SymmetricAlgorithm CreateCipher(string name, byte[] keyBytes, byte[] ivBytes)
        {
            SymmetricAlgorithm cipher;
            switch (name)
            {
                case "AES-CBC": cipher = Aes.Create(); break;
                case "DES-CBC": cipher = DES.Create(); break;
                case "TripleDES-CBC": cipher = TripleDES.Create(); break;
                case "RC2-CBC": cipher = RC2.Create(); break;
                default: throw new InvalidOperationException("Unsupported algorithm.");
            }
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = keyBytes;
            cipher.IV = ivBytes;
            return cipher;
        }

        private void ValidateKeyAndIv(string name, byte[] keyBytes, byte[] ivBytes)
        {
            var requiredIv = name == "AES-CBC" ? 16 : 8;
            if (ivBytes.Length != requiredIv) throw new InvalidOperationException(name + " IV must be " + requiredIv + " bytes.");
            if (name == "AES-CBC" && keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
                throw new InvalidOperationException("AES Key must be 16, 24 or 32 bytes.");
            if (name == "DES-CBC" && keyBytes.Length != 8) throw new InvalidOperationException("DES Key must be 8 bytes.");
            if (name == "TripleDES-CBC" && keyBytes.Length != 16 && keyBytes.Length != 24)
                throw new InvalidOperationException("TripleDES Key must be 16 or 24 bytes.");
            if (name == "RC2-CBC" && (keyBytes.Length < 5 || keyBytes.Length > 16))
                throw new InvalidOperationException("RC2 Key must be 5 to 16 bytes.");
        }

        private static byte[] ParseBytes(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(name + " cannot be empty.");
            var text = value.Trim();
            var isHex = text.StartsWith("hex:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            if (isHex)
            {
                var hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text.Substring(2) : text.Substring(4);
                hex = new string(hex.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
                if (hex.Length % 2 != 0 || !hex.All(Uri.IsHexDigit)) throw new InvalidOperationException(name + " has an invalid hex value.");
                return Enumerable.Range(0, hex.Length / 2).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();
            }
            return Encoding.UTF8.GetBytes(text);
        }
    }
}
