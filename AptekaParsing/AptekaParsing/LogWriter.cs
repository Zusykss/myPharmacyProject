using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptekaParsing
{
    public static class LogWriter
    {
        
        public static string Path;


        public static void LogCritical(string message)
        {
            Log(LogLevel.Critical, message);
        }
        public static void LogCritical(Exception ex)
        {
            Log(LogLevel.Critical, ex);
        }
        public static void LogInformation(string message)
        {
            Log(LogLevel.Information, message);
        }

        public static void Log(LogLevel logLevel, string message, params object?[] args)
        {
            var levelMessage = getLevelMessage(logLevel);

            var logMessage = levelMessage + message;
            Console.WriteLine(logMessage);
            WriteToFile(logMessage);
        }
        public static void Log(LogLevel logLevel, Exception? exception)
        {
            var levelMessage = getLevelMessage(logLevel);
            var logMessage = levelMessage + exception.Message;
            Console.WriteLine(logMessage);
            WriteToFile(logMessage);
            



        }


        private static string getLevelMessage(LogLevel logLevel)
        {
            string levelMessage = logLevel switch
            {
                LogLevel.Critical => "Critical Exception: ",
                LogLevel.Information => "Information: ",
                _ => "Debug: "
            };
            return levelMessage;
        }
        private static void  WriteToFile(string message)
        {
            if (!File.Exists(Path))
            {
                using (StreamWriter sw = File.CreateText(Path))
                {
                    sw.WriteLine(message);
                }
                return;
            }

            using (StreamWriter sw = File.AppendText(Path))
            {
                sw.WriteLine(message);
            }

        }


    }
}
