using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace QFG5Extractor.qfg5spk
{
    public class SpkConverter
    {
        private class SpkEntry
        {
            public string FileName;
            public int FileNameLength;
            public int CompressedSize;
            public long ActualOffset;
        }

        public static void Extract(string spkFile, string outputFolder)
        {
            Logger.LogInfo($"Extracting {spkFile} to {outputFolder}");

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            using (FileStream fs = new FileStream(spkFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                uint val1;
                uint val2;
                short magic;
                long dataPrefixLength;
                long dirOffset;
                long dirEnd;
                List<SpkEntry> entries = ParseEntries(fs, br, out val1, out val2, out magic, out dataPrefixLength, out dirOffset, out dirEnd);

                Logger.LogInfo($"Dir offset: {dirOffset}, val1: {val1}, val2: {val2}, files: {entries.Count}");

                for (int i = 0; i < entries.Count; i++)
                {
                    SpkEntry entry = entries[i];
                    Logger.LogInfo($"File: {entry.FileName}, Offset: {entry.ActualOffset}, Size: {entry.CompressedSize}");

                    // Extract the file
                    fs.Seek(entry.ActualOffset, SeekOrigin.Begin);
                    byte[] data = ReadExact(br, entry.CompressedSize);

                    string outPath = Path.Combine(outputFolder, ToSystemPath(entry.FileName));
                    string outDir = Path.GetDirectoryName(outPath);
                    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                    File.WriteAllBytes(outPath, data);
                }
            }
        }

        public static void Repack(string originalSpkFile, string modifiedFolder, string outputSpkFile)
        {
            Logger.LogInfo($"Repacking {originalSpkFile} using modified files from {modifiedFolder} into {outputSpkFile}");

            if (!File.Exists(originalSpkFile)) throw new FileNotFoundException("Original SPK file not found.", originalSpkFile);
            if (!Directory.Exists(modifiedFolder)) throw new DirectoryNotFoundException("Modified folder not found: " + modifiedFolder);
            if (string.Equals(Path.GetFullPath(originalSpkFile), Path.GetFullPath(outputSpkFile), StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Output SPK must be a different path than the original SPK.");
            }

            List<SpkEntry> entries;
            long dataPrefixLength;

            using (FileStream fs = new FileStream(originalSpkFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                uint val1;
                uint val2;
                short magic;
                long dirOffset;
                long dirEnd;
                entries = ParseEntries(fs, br, out val1, out val2, out magic, out dataPrefixLength, out dirOffset, out dirEnd);
            }

            if (entries.Count == 0) throw new Exception("No files found in SPK directory.");

            List<byte[]> replacementData = new List<byte[]>(entries.Count);
            int replacedCount = 0;

            using (FileStream sourceFs = new FileStream(originalSpkFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader sourceBr = new BinaryReader(sourceFs))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    SpkEntry entry = entries[i];
                    string modPath = Path.Combine(modifiedFolder, ToSystemPath(entry.FileName));

                    byte[] data;
                    if (File.Exists(modPath))
                    {
                        data = File.ReadAllBytes(modPath);
                        replacedCount++;
                    }
                    else
                    {
                        sourceFs.Seek(entry.ActualOffset, SeekOrigin.Begin);
                        data = ReadExact(sourceBr, entry.CompressedSize);
                    }

                    if (data.Length > int.MaxValue - 1024)
                    {
                        throw new Exception("File too large to repack safely: " + entry.FileName);
                    }

                    replacementData.Add(data);
                }
            }

            string outDir = Path.GetDirectoryName(outputSpkFile);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            using (FileStream sourceFs = new FileStream(originalSpkFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader sourceBr = new BinaryReader(sourceFs))
            using (FileStream outFs = new FileStream(outputSpkFile, FileMode.Create, FileAccess.Write))
            using (BinaryWriter outBw = new BinaryWriter(outFs))
            {
                CopyBytes(sourceBr, outBw, dataPrefixLength);

                for (int i = 0; i < entries.Count; i++)
                {
                    SpkEntry entry = entries[i];
                    byte[] nameBytes = Encoding.ASCII.GetBytes(entry.FileName);
                    byte[] data = replacementData[i];
                    int newSize = data.Length;

                    if (nameBytes.Length > short.MaxValue)
                    {
                        throw new Exception("Filename too long to repack: " + entry.FileName);
                    }

                    CopyBytes(sourceBr, outBw, 18);
                    outBw.Write(newSize);
                    int oldCompressedSize = sourceBr.ReadInt32();
                    outBw.Write(newSize);

                    sourceFs.Seek(4, SeekOrigin.Current);
                    outBw.Write((short)nameBytes.Length);
                    short oldNameLength = sourceBr.ReadInt16();

                    CopyBytes(sourceBr, outBw, 2);
                    outBw.Write(nameBytes);
                    sourceFs.Seek(oldNameLength, SeekOrigin.Current);

                    CopyBytes(sourceBr, outBw, 36);
                    outBw.Write(data);
                    sourceFs.Seek(oldCompressedSize, SeekOrigin.Current);
                }

                long runningRelativeOffset = 0;
                long footerVal2 = 0;
                long footerVal1 = 0;

                for (int i = 0; i < entries.Count; i++)
                {
                    SpkEntry entry = entries[i];
                    byte[] nameBytes = Encoding.ASCII.GetBytes(entry.FileName);
                    byte[] data = replacementData[i];
                    int newSize = data.Length;

                    CopyBytes(sourceBr, outBw, 20);
                    outBw.Write(newSize);
                    outBw.Write(newSize);

                    sourceFs.Seek(8, SeekOrigin.Current);
                    outBw.Write(nameBytes.Length);
                    int oldDirNameLength = sourceBr.ReadInt32();

                    CopyBytes(sourceBr, outBw, 10);
                    outBw.Write((int)runningRelativeOffset);
                    sourceFs.Seek(4, SeekOrigin.Current);
                    outBw.Write(nameBytes);
                    sourceFs.Seek(oldDirNameLength, SeekOrigin.Current);

                    long span = newSize + 66L + nameBytes.Length;
                    runningRelativeOffset += span;
                    footerVal2 += span;
                    footerVal1 += 46L + nameBytes.Length;
                }

                CopyBytes(sourceBr, outBw, 12);
                outBw.Write((int)footerVal1);
                sourceFs.Seek(4, SeekOrigin.Current);
                outBw.Write((int)footerVal2);
                sourceFs.Seek(4, SeekOrigin.Current);
                CopyBytes(sourceBr, outBw, 2);
            }

            Logger.LogInfo($"Repack complete. Files: {entries.Count}, replaced from mod folder: {replacedCount}");
        }

        private static List<SpkEntry> ParseEntries(FileStream fs, BinaryReader br, out uint val1, out uint val2, out short magic, out long dataPrefixLength, out long dirOffset, out long dirEnd)
        {
            long fileLength = fs.Length;
            if (fileLength < 10) throw new Exception("File too short for SPK.");

            fs.Seek(fileLength - 10, SeekOrigin.Begin);
            val1 = br.ReadUInt32();
            val2 = br.ReadUInt32();
            magic = br.ReadInt16();

            dataPrefixLength = fileLength - val1 - val2 - 22;
            dirOffset = fileLength - val1 - 22;
            dirEnd = fileLength - 22;

            if (dataPrefixLength < 0 || dirOffset < 0 || dirEnd < 0 || dirOffset > dirEnd || dirEnd > fileLength)
            {
                throw new Exception("Invalid SPK footer values.");
            }

            List<SpkEntry> entries = new List<SpkEntry>();
            fs.Seek(dirOffset, SeekOrigin.Begin);

            while (fs.Position < dirEnd)
            {
                if (dirEnd - fs.Position < 42) break;

                fs.Seek(20, SeekOrigin.Current);
                br.ReadUInt32(); // Uncompressed size in directory entry (not needed for extraction/repack math).
                uint compressedSize = br.ReadUInt32();
                int filenameLength = br.ReadInt32();

                if (filenameLength <= 0 || filenameLength > 4096) break;
                if (dirEnd - fs.Position < 14 + filenameLength) break;

                fs.Seek(10, SeekOrigin.Current);
                uint relativeOffset = br.ReadUInt32();

                byte[] nameBytes = ReadExact(br, filenameLength);
                string filename = Encoding.ASCII.GetString(nameBytes);

                long actualOffset = relativeOffset + dataPrefixLength + 66 + filenameLength;
                if (actualOffset < 0 || actualOffset > fileLength) throw new Exception("Invalid SPK file offset.");
                if (actualOffset + compressedSize > fileLength) throw new Exception("Invalid SPK file size.");

                SpkEntry entry = new SpkEntry();
                entry.FileName = filename;
                entry.FileNameLength = filenameLength;
                entry.CompressedSize = (int)compressedSize;
                entry.ActualOffset = actualOffset;
                entries.Add(entry);
            }

            return entries;
        }

        private static byte[] ReadExact(BinaryReader br, int count)
        {
            byte[] bytes = br.ReadBytes(count);
            if (bytes.Length != count) throw new EndOfStreamException("Unexpected end of file.");
            return bytes;
        }

        private static void CopyBytes(BinaryReader source, BinaryWriter destination, long count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            const int BufferSize = 64 * 1024;
            byte[] buffer = new byte[BufferSize];
            long remaining = count;
            while (remaining > 0)
            {
                int toRead = remaining > BufferSize ? BufferSize : (int)remaining;
                int read = source.Read(buffer, 0, toRead);
                if (read <= 0) throw new EndOfStreamException("Unexpected end of file while copying SPK data.");
                destination.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        private static string ToSystemPath(string archivePath)
        {
            return archivePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
