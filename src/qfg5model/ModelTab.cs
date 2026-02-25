using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QFG5Extractor.qfg5model
{
    public class ModelTab : UserControl
    {
        private ComboBox operationComboBox;
        private TextBox inputFolderTextBox;
        private TextBox outputFolderTextBox;
        private Button runButton;
        private Label statusLabel;
        private ProgressBar progressBar;
        private Label currentFileLabel;
        private ListBox convertedListBox;
        private Button viewLogButton;

        public ModelTab()
        {
            Dock = DockStyle.Fill;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Label opLabel = new Label { Text = "Operation", Left = 10, Top = 20, Width = 100 };
            operationComboBox = new ComboBox { Left = 120, Top = 18, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            
            operationComboBox.Items.Add("Export BMP (MDL -> BMP)");
            operationComboBox.Items.Add("Import BMP (BMP -> MDL)");
            operationComboBox.Items.Add("Export HAK (MDL -> HAK)");
            operationComboBox.Items.Add("Import HAK (HAK -> MDL)");
            operationComboBox.SelectedIndex = 0;

            Label inputLabel = new Label { Text = "Input Folder", Left = 10, Top = 60, Width = 100 };
            inputFolderTextBox = new TextBox { Left = 120, Top = 58, Width = 480 };
            Button inputBrowseButton = new Button { Text = "Browse...", Left = 610, Top = 56, Width = 90 };

            Label outputLabel = new Label { Text = "Output Folder", Left = 10, Top = 100, Width = 100 };
            outputFolderTextBox = new TextBox { Left = 120, Top = 98, Width = 480 };
            Button outputBrowseButton = new Button { Text = "Browse...", Left = 610, Top = 96, Width = 90 };

            progressBar = new ProgressBar { Left = 10, Top = 140, Width = 580, Height = 28 };
            runButton = new Button { Text = "Run", Left = 610, Top = 136, Width = 90, Height = 32 };
            viewLogButton = new Button { Text = "View Log", Left = 610, Top = 176, Width = 90, Height = 28, Visible = false };
            currentFileLabel = new Label { Left = 10, Top = 175, Width = 590, Height = 24, Text = "Current file: -" };
            statusLabel = new Label { Left = 10, Top = 198, Width = 690, Height = 24, Text = "Select folders and click Run.", ForeColor = Color.Black };

            Label infoLabel = new Label { Left = 10, Top = 228, Width = 690, Height = 60, Text = "Note:\nWhen importing, the output folder MUST contain the original .MDL files\nthat the BMP or HAK files were originally extracted from. The output .MDL will be updated/overwritten." };
            
            Label convertedListLabel = new Label { Left = 10, Top = 288, Width = 200, Height = 24, Text = "Converted files" };
            convertedListBox = new ListBox { Left = 10, Top = 312, Width = 690, Height = 170 };

            inputBrowseButton.Click += delegate { inputFolderTextBox.Text = BrowseFolder(inputFolderTextBox.Text); };
            viewLogButton.Click += (s, e) => {
                Logger.OpenLogFile();
            };
            outputBrowseButton.Click += delegate { outputFolderTextBox.Text = BrowseFolder(outputFolderTextBox.Text); };
            runButton.Click += RunButton_Click;
            operationComboBox.SelectedIndexChanged += OperationComboBox_SelectedIndexChanged;

            Controls.Add(opLabel);
            Controls.Add(operationComboBox);
            Controls.Add(inputLabel);
            Controls.Add(inputFolderTextBox);
            Controls.Add(inputBrowseButton);
            Controls.Add(outputLabel);
            Controls.Add(outputFolderTextBox);
            Controls.Add(outputBrowseButton);
            Controls.Add(progressBar);
            Controls.Add(runButton);
            Controls.Add(currentFileLabel);
            Controls.Add(statusLabel);
            Controls.Add(infoLabel);
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

        private void OperationComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetStatus("Operation changed. Please ensure your folder selections are correct.", Color.Black);
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            string inputFolder = inputFolderTextBox.Text.Trim();
            string outputFolder = outputFolderTextBox.Text.Trim();
            int op = operationComboBox.SelectedIndex;

            if (!Directory.Exists(inputFolder))
            {
                MessageBox.Show("Input folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            progressBar.Value = 0;
            SetStatus("Running...", Color.RoyalBlue);
            Application.DoEvents();

            int converted = 0;
            int failed = 0;

            try
            {
                bool exportBitmap = (op == 0);
                bool importBitmap = (op == 1);
                bool exportMesh   = (op == 2);
                bool importMesh   = (op == 3);

                string[] allFiles = Directory.GetFiles(inputFolder);
                List<string> workingFiles = new List<string>();

                foreach (string f in allFiles)
                {
                    if (exportBitmap || exportMesh)
                    {
                        if (f.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase)) workingFiles.Add(f);
                    }
                    else if (importBitmap)
                    {
                        if (f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)) workingFiles.Add(f);
                    }
                    else if (importMesh)
                    {
                        if (f.EndsWith(".hak", StringComparison.OrdinalIgnoreCase)) workingFiles.Add(f);
                    }
                }

                if (workingFiles.Count == 0)
                {
                    MessageBox.Show("No applicable input files found in selected folder.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus("No files to process.", Color.DarkOrange);
                    return;
                }
                
                progressBar.Minimum = 0;
                progressBar.Maximum = workingFiles.Count;
                progressBar.Value = 0;

                List<string> processedBases = new List<string>();

                foreach (string inputFile in workingFiles)
                {
                    string baseName = Path.GetFileNameWithoutExtension(inputFile);
                    string originalMdlName = baseName;
                    
                    int firstDash = baseName.IndexOf('-');
                    if (firstDash > 0)
                    {
                        originalMdlName = baseName.Substring(0, firstDash);
                    }

                    // For imports, we only want to trigger the process ONCE per host MDL
                    if (importBitmap || importMesh)
                    {
                        if (processedBases.Contains(originalMdlName.ToLower()))
                        {
                            progressBar.Value = Math.Min(progressBar.Value + 1, progressBar.Maximum);
                            continue;
                        }
                        processedBases.Add(originalMdlName.ToLower());
                    }

                    currentFileLabel.Text = "Current file: " + originalMdlName;
                    
                    string outputFile = "";
                    string converterInputFile = inputFile;

                    if (exportBitmap) outputFile = Path.Combine(outputFolder, baseName + ".bmp");
                    else if (exportMesh) outputFile = Path.Combine(outputFolder, baseName + ".hak");
                    else if (importBitmap || importMesh)
                    {
                        // For import, we use a dummy path with the base name prefix so the converter 
                        // can find the -X-Name.hak/bmp files in the input folder.
                        converterInputFile = Path.Combine(inputFolder, originalMdlName + (importBitmap ? ".bmp" : ".hak"));
                        
                        outputFile = Path.Combine(outputFolder, originalMdlName + ".mdl");
                        if (!File.Exists(outputFile))
                        {
                            outputFile = Path.Combine(outputFolder, originalMdlName + ".MDL");
                        }
                    }

                    try
                    {
                        ModelConverter.importOrExportBitmapOrMesh(converterInputFile, outputFile, exportBitmap, importBitmap, exportMesh, importMesh);
                        converted++;
                        convertedListBox.Items.Add(Path.GetFileName(converterInputFile) + " -> " + Path.GetFileName(outputFile));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(originalMdlName, ex.Message + "\n" + ex.StackTrace);
                        failed++;
                    }

                    progressBar.Value = Math.Min(progressBar.Value + 1, progressBar.Maximum);
                    SetStatus($"Running... Processed: {converted}, failed: {failed}", Color.RoyalBlue);
                    convertedListBox.TopIndex = Math.Max(0, convertedListBox.Items.Count - 1);
                    Application.DoEvents();
                }

                string result = $"Done. Processed: {converted}, failed: {failed}";
                Color finalColor = (failed > 0) ? Color.DarkOrange : Color.ForestGreen;
                SetStatus(result, finalColor);
                currentFileLabel.Text = "Current file: done";
                MessageBox.Show(result, "QFG5 Model", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus("Error.", Color.Firebrick);
                currentFileLabel.Text = "Current file: error";
                MessageBox.Show("Unexpected error: " + ex.Message + "\n\nStackTrace:\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
