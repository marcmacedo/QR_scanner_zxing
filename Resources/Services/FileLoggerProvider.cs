using System.IO;


namespace QR_scanner_zxing.Resources.Services
{
    public static class Logger
    {
        private static readonly String logFilePath = Path.Combine(FileSystem.AppDataDirectory, "application.log");
        
        public static void Log(string level, string message)
        {
            try
            {
                ManageLogSize();
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level.ToUpper()}] - {message}\n";
                File.AppendAllText(logFilePath, logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] - [FileLoggerProvider][Log] Erro ao registrar log:  {ex.Message}");
            }
        }
        public static string GetLogFilePath()
        {
            return logFilePath;
        }

        public static void ManageLogSize(int maxFileSizeInKb = 1024)
        {
            try
            {
                FileInfo logFile = new FileInfo(logFilePath);
                if (logFile.Exists && logFile.Length > maxFileSizeInKb * 1024)
                {
                    File.WriteAllText(logFilePath, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] - [FileLoggerProvider][ManageLogSize] Erro ao gerenciar tamanho do log: {ex.Message}");
            }
        }
    }
}
