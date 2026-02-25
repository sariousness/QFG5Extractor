using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QFG5Extractor.qfg5pano
{
    public class PanoTab : UserControl
    {
        private TextBox nodFolderTextBox;
        private TextBox imgFolderTextBox;
        private TextBox outputFolderTextBox;
        private Button runButton;
        private Label statusLabel;
        private Label currentFileLabel;
        private ProgressBar progressBar;
        private ListBox convertedListBox;
        private PictureBox previewPictureBox;
        private Button viewLogButton;

        public PanoTab()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Label nodLabel = new Label { Text = "NOD folder", Left = 10, Top = 20, Width = 100 };
            nodFolderTextBox = new TextBox { Left = 120, Top = 18, Width = 480 };
            Button nodBrowseButton = new Button { Text = "Browse...", Left = 610, Top = 16, Width = 90 };

            Label imgLabel = new Label { Text = "IMG folder", Left = 10, Top = 60, Width = 100 };
            imgFolderTextBox = new TextBox { Left = 120, Top = 58, Width = 480 };
            Button imgBrowseButton = new Button { Text = "Browse...", Left = 610, Top = 56, Width = 90 };

            Label outLabel = new Label { Text = "Output folder", Left = 10, Top = 100, Width = 100 };
            outputFolderTextBox = new TextBox { Left = 120, Top = 98, Width = 480 };
            Button outBrowseButton = new Button { Text = "Browse...", Left = 610, Top = 96, Width = 90 };

            progressBar = new ProgressBar { Left = 10, Top = 140, Width = 580, Height = 28 };
            runButton = new Button { Text = "Run", Left = 610, Top = 136, Width = 90, Height = 32 };
            viewLogButton = new Button { Text = "View Log", Left = 610, Top = 176, Width = 90, Height = 28, Visible = false };
            currentFileLabel = new Label { Left = 10, Top = 175, Width = 590, Height = 24, Text = "Current file: -" };
            statusLabel = new Label { Left = 10, Top = 198, Width = 690, Height = 24, Text = "Select folders and click Run.", ForeColor = Color.Black };
            Label convertedListLabel = new Label { Left = 10, Top = 228, Width = 200, Height = 24, Text = "Converted files" };
            convertedListBox = new ListBox { Left = 10, Top = 252, Width = 340, Height = 220 };
            
            previewPictureBox = new PictureBox { Left = 360, Top = 252, Width = 340, Height = 220, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };

            nodBrowseButton.Click += delegate { nodFolderTextBox.Text = BrowseFolder(nodFolderTextBox.Text); };
            viewLogButton.Click += (s, e) => {
                Logger.OpenLogFile();
            };
            imgBrowseButton.Click += delegate { imgFolderTextBox.Text = BrowseFolder(imgFolderTextBox.Text); };
            outBrowseButton.Click += delegate { outputFolderTextBox.Text = BrowseFolder(outputFolderTextBox.Text); };
            runButton.Click += RunButtonClick;
            convertedListBox.SelectedIndexChanged += ConvertedListBox_SelectedIndexChanged;

            Controls.Add(nodLabel);
            Controls.Add(nodFolderTextBox);
            Controls.Add(nodBrowseButton);
            Controls.Add(imgLabel);
            Controls.Add(imgFolderTextBox);
            Controls.Add(imgBrowseButton);
            Controls.Add(outLabel);
            Controls.Add(outputFolderTextBox);
            Controls.Add(outBrowseButton);
            Controls.Add(progressBar);
            Controls.Add(runButton);
            Controls.Add(currentFileLabel);
            Controls.Add(statusLabel);
            Controls.Add(convertedListLabel);
            Controls.Add(convertedListBox);
            Controls.Add(previewPictureBox);
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
            string nodFolder = nodFolderTextBox.Text.Trim();
            string imgFolder = imgFolderTextBox.Text.Trim();
            string outputFolder = outputFolderTextBox.Text.Trim();

            if (!Directory.Exists(nodFolder))
            {
                MessageBox.Show("NOD folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(imgFolder))
            {
                MessageBox.Show("IMG folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            int missing = 0;
            int failed = 0;

            try
            {
                string[] allNodFiles = Directory.GetFiles(nodFolder);
                List<string> nodList = new List<string>();
                foreach (string f in allNodFiles) if (f.EndsWith(".nod", StringComparison.OrdinalIgnoreCase)) nodList.Add(f);
                string[] nodFiles = nodList.ToArray();
                Array.Sort(nodFiles, StringComparer.OrdinalIgnoreCase);

                if (nodFiles.Length == 0)
                {
                    MessageBox.Show("No .nod files found in selected NOD folder.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus("No files to process.", Color.DarkOrange);
                    return;
                }

                progressBar.Minimum = 0;
                progressBar.Maximum = nodFiles.Length;
                progressBar.Value = 0;

                string[] allImgFiles = Directory.GetFiles(imgFolder);
                List<string> imgList = new List<string>();
                foreach (string f in allImgFiles) if (f.EndsWith(".img", StringComparison.OrdinalIgnoreCase)) imgList.Add(f);
                string[] imgFiles = imgList.ToArray();

                foreach (string nodFile in nodFiles)
                {
                    string baseName = Path.GetFileNameWithoutExtension(nodFile);
                    currentFileLabel.Text = "Current file: " + baseName;

                    string imgFile = Array.Find(imgFiles, f => string.Equals(Path.GetFileNameWithoutExtension(f), baseName, StringComparison.OrdinalIgnoreCase));
                    string outFile = Path.Combine(outputFolder, baseName + ".bmp");

                    if (string.IsNullOrEmpty(imgFile))
                    {
                        missing++;
                    }
                    else
                    {
                        try
                        {
                            PanoConverter.exportFromFiles(nodFile, imgFile, outFile);
                            converted++;
                            convertedListBox.Items.Add(baseName + ".bmp");
                            
                            if (previewPictureBox.Image != null)
                            {
                                previewPictureBox.Image.Dispose();
                            }
                            previewPictureBox.Image = Image.FromFile(outFile);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(baseName, ex.Message);
                            failed++;
                        }
                    }

                    progressBar.Value = Math.Min(progressBar.Value + 1, progressBar.Maximum);
                    SetStatus($"Running... Converted: {converted}, missing IMG: {missing}, failed: {failed}", Color.RoyalBlue);
                    convertedListBox.TopIndex = Math.Max(0, convertedListBox.Items.Count - 1);
                    Application.DoEvents();
                }

                string result = $"Done. Converted: {converted}, missing IMG: {missing}, failed: {failed}";
                Color finalColor = (missing > 0 || failed > 0) ? Color.DarkOrange : Color.ForestGreen;
                SetStatus(result, finalColor);
                currentFileLabel.Text = "Current file: done";
                MessageBox.Show(result, "QFG5 Pano", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void ConvertedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (convertedListBox.SelectedItem == null) return;
            
            string fileName = convertedListBox.SelectedItem.ToString();
            string outputFolder = outputFolderTextBox.Text.Trim();
            string fullPath = Path.Combine(outputFolder, fileName);
            
            if (File.Exists(fullPath))
            {
                try
                {
                    if (previewPictureBox.Image != null)
                    {
                        previewPictureBox.Image.Dispose();
                        previewPictureBox.Image = null;
                    }
                    previewPictureBox.Image = Image.FromFile(fullPath);
                }
                catch { }
            }
        }

        private void SetStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }
    }
}
