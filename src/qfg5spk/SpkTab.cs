using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QFG5Extractor.qfg5spk
{
    public class SpkTab : UserControl
    {
        private TextBox spkFileTextBox;
        private TextBox outputFolderTextBox;
        private Button browseSpkButton;
        private Button browseOutputButton;
        private Button runButton;
        private Button repackButton;
        private ListBox filesListBox;
        private Label statusLabel;
        private ProgressBar progressBar;
        private Button viewLogButton;

        public SpkTab()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Label spkLabel = new Label { Text = "SPK file", Left = 10, Top = 20, Width = 100 };
            spkFileTextBox = new TextBox { Left = 120, Top = 18, Width = 480 };
            browseSpkButton = new Button { Text = "Browse...", Left = 610, Top = 16, Width = 90 };

            Label outLabel = new Label { Text = "Output/Mod folder", Left = 10, Top = 60, Width = 110 };
            outputFolderTextBox = new TextBox { Left = 120, Top = 58, Width = 480 };
            browseOutputButton = new Button { Text = "Browse...", Left = 610, Top = 56, Width = 90 };

            runButton = new Button { Text = "Extract All", Left = 510, Top = 100, Width = 90, Height = 32 };
            repackButton = new Button { Text = "Repack SPK", Left = 390, Top = 100, Width = 110, Height = 32 };
            Button batchExtractButton = new Button { Text = "Batch Folder", Left = 610, Top = 100, Width = 90, Height = 32 };
            viewLogButton = new Button { Text = "View Log", Left = 610, Top = 140, Width = 90, Height = 28, Visible = false };
            progressBar = new ProgressBar { Left = 10, Top = 140, Width = 580, Height = 28 };
            
            statusLabel = new Label { Left = 10, Top = 175, Width = 690, Height = 24, Text = "Select an SPK file and an output folder." };
            
            Label filesLabel = new Label { Text = "Files in archive:", Left = 10, Top = 205, Width = 200 };
            filesListBox = new ListBox { Left = 10, Top = 230, Width = 690, Height = 460 };

            browseSpkButton.Click += (s, e) => {
                using (OpenFileDialog dialog = new OpenFileDialog { Filter = "SPK files (*.spk)|*.spk|All files (*.*)|*.*", Multiselect = true }) {
                    if (dialog.ShowDialog() == DialogResult.OK) spkFileTextBox.Text = string.Join(";", dialog.FileNames);
                }
            };

            browseOutputButton.Click += (s, e) => {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                    if (dialog.ShowDialog() == DialogResult.OK) outputFolderTextBox.Text = dialog.SelectedPath;
                }
            };

            runButton.Click += RunButton_Click;
            repackButton.Click += RepackButton_Click;
            batchExtractButton.Click += BatchExtractButton_Click;
            viewLogButton.Click += (s, e) => {
                Logger.OpenLogFile();
            };

            Controls.Add(spkLabel);
            Controls.Add(spkFileTextBox);
            Controls.Add(browseSpkButton);
            Controls.Add(outLabel);
            Controls.Add(outputFolderTextBox);
            Controls.Add(browseOutputButton);
            Controls.Add(repackButton);
            Controls.Add(runButton);
            Controls.Add(batchExtractButton);
            Controls.Add(viewLogButton);
            Controls.Add(progressBar);
            Controls.Add(statusLabel);
            Controls.Add(filesLabel);
            Controls.Add(filesListBox);
        }

        private void BatchExtractButton_Click(object sender, EventArgs e)
        {
            string outDir = outputFolderTextBox.Text.Trim();

            if (!Directory.Exists(outDir)) {
                MessageBox.Show("Output directory not found. Please select an output directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string inputDir = "";
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                dialog.Description = "Select folder containing SPK files to batch extract";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                inputDir = dialog.SelectedPath;
            }

            string[] spkFiles = Directory.GetFiles(inputDir, "*.spk");

            if (spkFiles.Length == 0) {
                MessageBox.Show("No SPK files found in the selected folder.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            progressBar.Maximum = spkFiles.Length;
            progressBar.Value = 0;
            int count = 0;
            int failed = 0;

            foreach (string f in spkFiles)
            {
                statusLabel.Text = "Extracting: " + Path.GetFileName(f);
                Application.DoEvents();
                try {
                    SpkConverter.Extract(f, outDir);
                    count++;
                } catch (Exception ex) {
                    Logger.LogError(f, ex.Message);
                    failed++;
                }
                progressBar.Value++;
            }

            statusLabel.Text = $"Done. Extracted: {count}, Failed: {failed}";
            MessageBox.Show(statusLabel.Text, "Batch SPK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            viewLogButton.Visible = true;
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            string spkFilesText = spkFileTextBox.Text.Trim();
            string outDir = outputFolderTextBox.Text.Trim();

            if (string.IsNullOrEmpty(spkFilesText)) {
                MessageBox.Show("Please select at least one SPK file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(outDir)) {
                MessageBox.Show("Output directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] spkFiles = spkFilesText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            progressBar.Maximum = spkFiles.Length;
            progressBar.Value = 0;
            int count = 0;
            int failed = 0;

            foreach (string spkFile in spkFiles)
            {
                if (!File.Exists(spkFile)) {
                    Logger.LogError(spkFile, "File not found.");
                    failed++;
                    continue;
                }

                statusLabel.Text = "Extracting: " + Path.GetFileName(spkFile);
                Application.DoEvents();

                try
                {
                    SpkConverter.Extract(spkFile, outDir);
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogError(spkFile, ex.Message);
                    failed++;
                }
                progressBar.Value++;
            }

            statusLabel.Text = $"Done. Extracted: {count}, Failed: {failed}";
            
            if (spkFiles.Length > 1) {
                MessageBox.Show(statusLabel.Text, "Extract SPK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else if (failed > 0) {
                MessageBox.Show("Error occurred during extraction. Check log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            viewLogButton.Visible = true;
        }

        private void RepackButton_Click(object sender, EventArgs e)
        {
            string spkFilesText = spkFileTextBox.Text.Trim();
            string modifiedFolder = outputFolderTextBox.Text.Trim();

            if (string.IsNullOrEmpty(spkFilesText))
            {
                MessageBox.Show("Please select one source SPK file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] spkFiles = spkFilesText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (spkFiles.Length != 1)
            {
                MessageBox.Show("Repack requires exactly one source SPK file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string sourceSpk = spkFiles[0];
            if (!File.Exists(sourceSpk))
            {
                MessageBox.Show("Source SPK file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(modifiedFolder))
            {
                MessageBox.Show("Modified folder not found. Select the extracted/modded folder in Output folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string outputPath;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                string baseName = Path.GetFileNameWithoutExtension(sourceSpk);
                string sourceDir = Path.GetDirectoryName(sourceSpk);
                dialog.Filter = "SPK files (*.spk)|*.spk|All files (*.*)|*.*";
                dialog.FileName = baseName + "_modded.spk";
                dialog.InitialDirectory = sourceDir;

                if (dialog.ShowDialog() != DialogResult.OK) return;
                outputPath = dialog.FileName;
            }

            try
            {
                statusLabel.Text = "Repacking: " + Path.GetFileName(sourceSpk);
                Application.DoEvents();
                SpkConverter.Repack(sourceSpk, modifiedFolder, outputPath);
                statusLabel.Text = "Repack complete: " + Path.GetFileName(outputPath);
                MessageBox.Show("Repack completed:\n" + outputPath, "Repack SPK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(sourceSpk, ex.Message);
                statusLabel.Text = "Repack failed. Check log.";
                MessageBox.Show("Error during SPK repack. Check log for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            viewLogButton.Visible = true;
        }
    }
}
