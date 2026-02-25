using System;
using System.Drawing;
using System.Windows.Forms;
using QFG5Extractor.qfg5pano;
using QFG5Extractor.qfg5msg;
using QFG5Extractor.qfg5model;
using QFG5Extractor.qfg5spk;

namespace QFG5Extractor
{
    public class MainForm : Form
    {
        private TabControl tabControl;

        public MainForm()
        {
            Text = "QFG5 Extraction Tools (Pano, Msg, Model)";
            Width = 750;
            Height = 560;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            
            TabPage panoTab = new TabPage("Pano (.bmp/.nod/.img)");
            panoTab.Controls.Add(new PanoTab());
            
            TabPage msgTab = new TabPage("Message (.qgm)");
            msgTab.Controls.Add(new MsgTab());

            TabPage modelTab = new TabPage("Model (.mdl/.bmp/.hak)");
            modelTab.Controls.Add(new ModelTab());

            TabPage spkTab = new TabPage("SPK (.spk)");
            spkTab.Controls.Add(new SpkTab());

            tabControl.TabPages.Add(panoTab);
            tabControl.TabPages.Add(msgTab);
            tabControl.TabPages.Add(modelTab);
            tabControl.TabPages.Add(spkTab);

            Controls.Add(tabControl);
        }
    }
}
