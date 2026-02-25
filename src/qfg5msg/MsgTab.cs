using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QFG5Extractor.qfg5msg
{
    public class MsgTab : UserControl
    {
        private TextBox qgmFolderTextBox;
        private TextBox outputFolderTextBox;
        private Button runButton;
        private Label statusLabel;
        private Label currentFileLabel;
        private ProgressBar progressBar;
        private ListBox convertedListBox;
        private Button viewLogButton;

        public MsgTab()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Label qgmLabel = new Label { Text = "QGM folder", Left = 10, Top = 20, Width = 100 };
            qgmFolderTextBox = new TextBox { Left = 120, Top = 18, Width = 480 };
            Button qgmBrowseButton = new Button { Text = "Browse...", Left = 610, Top = 16, Width = 90 };

            Label outLabel = new Label { Text = "Output folder", Left = 10, Top = 60, Width = 100 };
            outputFolderTextBox = new TextBox { Left = 120, Top = 58, Width = 480 };
            Button outBrowseButton = new Button { Text = "Browse...", Left = 610, Top = 56, Width = 90 };

            progressBar = new ProgressBar { Left = 10, Top = 100, Width = 580, Height = 28 };
            runButton = new Button { Text = "Run", Left = 610, Top = 96, Width = 90, Height = 32 };
            viewLogButton = new Button { Text = "View Log", Left = 610, Top = 136, Width = 90, Height = 28, Visible = false };
            currentFileLabel = new Label { Left = 10, Top = 135, Width = 590, Height = 24, Text = "Current file: -" };
            statusLabel = new Label { Left = 10, Top = 158, Width = 690, Height = 24, Text = "Select folders and click Run.", ForeColor = Color.Black };
            Label convertedListLabel = new Label { Left = 10, Top = 188, Width = 200, Height = 24, Text = "Converted files" };
            convertedListBox = new ListBox { Left = 10, Top = 212, Width = 690, Height = 260 };

            qgmBrowseButton.Click += delegate { qgmFolderTextBox.Text = BrowseFolder(qgmFolderTextBox.Text); };
            viewLogButton.Click += (s, e) => {
                Logger.OpenLogFile();
            };
            outBrowseButton.Click += delegate { outputFolderTextBox.Text = BrowseFolder(outputFolderTextBox.Text); };
            runButton.Click += RunButtonClick;

            Controls.Add(qgmLabel);
            Controls.Add(qgmFolderTextBox);
            Controls.Add(qgmBrowseButton);
            Controls.Add(outLabel);
            Controls.Add(outputFolderTextBox);
            Controls.Add(outBrowseButton);
            Controls.Add(progressBar);
            Controls.Add(runButton);
            Controls.Add(currentFileLabel);
            Controls.Add(statusLabel);
            Controls.Add(convertedListLabel);
            Controls.Add(convertedListBox);
            Controls.Add(viewLogButton);
        }

        private static string BrowseFolder(string currentPath)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    dialog.SelectedPath = currentPath;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }
            return currentPath;
        }

        private void RunButtonClick(object sender, EventArgs e)
        {
            string qgmFolder = qgmFolderTextBox.Text.Trim();
            string outputFolder = outputFolderTextBox.Text.Trim();

            if (!Directory.Exists(qgmFolder))
            {
                MessageBox.Show("QGM folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                MessageBox.Show("Output folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            runButton.Enabled = false;
            convertedListBox.Items.Clear();
            currentFileLabel.Text = "Current file: -";
            SetStatus("Running...", Color.RoyalBlue);
            Application.DoEvents();

            int converted = 0;
            int failed = 0;

            try
            {
                string[] allQgmFiles = Directory.GetFiles(qgmFolder);
                List<string> qgmList = new List<string>();
                foreach (string f in allQgmFiles) if (f.EndsWith(".qgm", StringComparison.OrdinalIgnoreCase)) qgmList.Add(f);
                string[] qgmFiles = qgmList.ToArray();
                Array.Sort(qgmFiles, StringComparer.OrdinalIgnoreCase);

                if (qgmFiles.Length == 0)
                {
                    MessageBox.Show("No .qgm files found in selected folder.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus("No files to process.", Color.DarkOrange);
                    return;
                }

                progressBar.Minimum = 0;
                progressBar.Maximum = qgmFiles.Length;
                progressBar.Value = 0;

                foreach (string file in qgmFiles)
                {
                    string baseName = Path.GetFileNameWithoutExtension(file);
                    currentFileLabel.Text = "Current file: " + baseName;

                    string outFile = Path.Combine(outputFolder, baseName + ".txt");

                    try
                    {
                        MsgConverter.ExtractMsg(file, outFile);
                        converted++;
                        convertedListBox.Items.Add(baseName + ".txt");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(baseName, ex.Message);
                        failed++;
                    }

                    progressBar.Value = Math.Min(progressBar.Value + 1, progressBar.Maximum);
                    SetStatus($"Running... Converted: {converted}, failed: {failed}", Color.RoyalBlue);
                    Application.DoEvents();
                }

                string result = $"Done. Converted: {converted}, failed: {failed}";
                Color finalColor = (failed > 0) ? Color.DarkOrange : Color.ForestGreen;
                SetStatus(result, finalColor);
                currentFileLabel.Text = "Current file: done";
                MessageBox.Show(result, "QFG5 Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("General", ex.Message);
                SetStatus("Error.", Color.Firebrick);
                currentFileLabel.Text = "Current file: error";
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                runButton.Enabled = true;
                viewLogButton.Visible = true;
            }
        }

        private void SetStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }
    }
}
