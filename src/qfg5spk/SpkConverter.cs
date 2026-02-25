using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace QFG5Extractor.qfg5spk
{
    public class SpkConverter
    {
        public static void Extract(string spkFile, string outputFolder)
        {
            Logger.LogInfo($"Extracting {spkFile} to {outputFolder}");
            
            using (FileStream fs = new FileStream(spkFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                long fileLength = fs.Length;
                if (fileLength < 10) throw new Exception("File too short for SPK.");

                // Read 10-byte footer
                fs.Seek(fileLength - 10, SeekOrigin.Begin);
                uint val1 = br.ReadUInt32(); // lload 5?
                uint val2 = br.ReadUInt32(); // lload 7?
                // short magic = br.ReadInt16();

                long lload9 = fileLength - val1 - val2 - 22;
                long dirOffset = fileLength - val1 - 22;

                Logger.LogInfo($"Dir offset: {dirOffset}, val1: {val1}, val2: {val2}");

                fs.Seek(dirOffset, SeekOrigin.Begin);
                long dirEnd = fileLength - 22;

                while (fs.Position < dirEnd)
                {
                    fs.Seek(20, SeekOrigin.Current);
                    uint uncompressedSize = br.ReadUInt32();
                    uint compressedSize = br.ReadUInt32();
                    int filenameLength = br.ReadInt32();
                    
                    if (filenameLength <= 0 || filenameLength > 1024) break;

                    fs.Seek(10, SeekOrigin.Current);
                    uint relativeOffset = br.ReadUInt32();
                    
                    byte[] nameBytes = br.ReadBytes(filenameLength);
                    string filename = Encoding.ASCII.GetString(nameBytes);

                    long actualOffset = relativeOffset + lload9 + 66 + filenameLength;

                    Logger.LogInfo($"File: {filename}, Offset: {actualOffset}, Size: {compressedSize}");

                    // Extract the file
                    long currentPos = fs.Position;
                    fs.Seek(actualOffset, SeekOrigin.Begin);
                    byte[] data = br.ReadBytes((int)compressedSize);
                    
                    string outPath = Path.Combine(outputFolder, filename);
                    string outDir = Path.GetDirectoryName(outPath);
                    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                    
                    File.WriteAllBytes(outPath, data);
                    
                    fs.Seek(currentPos, SeekOrigin.Begin);
                }
            }
        }
    }
}
