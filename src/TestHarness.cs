using System;
using System.IO;
using QFG5Extractor.qfg5model;

class TestHarness {
    static void Main() {
        string dir = "/home/vandali/Desktop/test/mdl/";
        if (!Directory.Exists(dir)) return;
        string[] files = Directory.GetFiles(dir, "*.MDL");
        int failed = 0;
        foreach (var f in files) {
            try {
                ModelConverter.importOrExportBitmapOrMesh(f.Replace(".MDL", ".hak"), f, false, false, false, true); // Import HAK
                ModelConverter.importOrExportBitmapOrMesh(f.Replace(".MDL", ".bmp"), f, false, true, false, false); // Import BMP
            } catch (Exception ex) {
                Console.WriteLine($"Error on {f}: {ex.Message}");
                //Console.WriteLine(ex.StackTrace);
                failed++;
                if (failed > 5) break; 
            }
        }
    }
}
