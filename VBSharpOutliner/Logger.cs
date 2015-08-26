using System;
using System.IO;

namespace VBSharpOutliner
{
    internal class Logger
    {
        private readonly static string UserAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private readonly static string ExtensionAppDir = Path.Combine(UserAppDataDir, "Nicologies");
        private readonly static string LogFilePath = Path.Combine(ExtensionAppDir, "VBSharpOutliner.log");

        public static void WriteLog(Exception ex, string message = null)
        {
            if(!Directory.Exists(ExtensionAppDir))
            {
                Directory.CreateDirectory(ExtensionAppDir);
            }
            using (var sw = new StreamWriter(LogFilePath, append: true))
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    sw.WriteLine(message);
                }
                sw.WriteLine(ex.ToString());
            }
        }
    }
}