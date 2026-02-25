using System;
using System.IO;

namespace QFG5Extractor
{
    public static class Logger
    {
        public static string GetLogFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qfg5extractor.log");
        }

        static Logger()
        {
            // Initialize log file (clear it on first run or append if preferred)
            try
            {
                File.WriteAllText(GetLogFilePath(), $"--- Session Started: {DateTime.Now} ---{Environment.NewLine}");
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(GetLogFilePath(), $"[{DateTime.UtcNow}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        public static void OpenLogFile()
        {
            string path = GetLogFilePath();
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("xdg-open", $"\"{path}\"") { UseShellExecute = false });
                }
                else
                {
                    System.Diagnostics.Process.Start(path);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Could not open log: " + ex.Message);
            }
        }

        public static void LogError(string fileName, string error)
        {
            Log($"[ERROR] Failed processing {fileName}: {error}");
        }

        public static void LogInfo(string info)
        {
            Log($"[INFO] {info}");
        }

        public static void LogWarning(string warning)
        {
            Log($"[WARNING] {warning}");
        }
    }
}
