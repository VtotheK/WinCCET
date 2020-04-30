using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorTracker
{
    public enum LogType { Information, Warning, Error, FATALERROR }
    static class DebugLogger 
    {
        const string DebugFileName = "debug.txt";
        private static string debugTextFilePath = null;

        public static void CreateLog(string path)
        {
            //Debug.WriteLine(path);
            try
            {
                debugTextFilePath = path + '\\' +  DebugFileName;
                if (!File.Exists(debugTextFilePath))
                {
                    var file = File.Create(debugTextFilePath, 4096);
                    file.Close();
                }
                else
                {
                    debugTextFilePath = path + '\\' + DateTime.Now.ToShortTimeString() + DebugFileName;
                    var file = File.Create(debugTextFilePath, 4096);
                    file.Close();
                }
            }
            catch(UnauthorizedAccessException ex)
            {
                debugTextFilePath = Environment.CurrentDirectory.ToString();
                Log(LogType.Error, ex.ToString());
            }
            catch(ArgumentException ex)
            {
                debugTextFilePath = Environment.CurrentDirectory.ToString();
                Log(LogType.Error, ex.ToString());
            }
        }

        public static void Log(LogType type, string message)
        {
            if (debugTextFilePath != null && message.Length > 0 && message != null)
            {
                try
                {
                    using (StreamWriter sw = File.AppendText(debugTextFilePath))
                    {
                        sw.WriteLine(type.ToString() + ":" + message);
                        sw.WriteLine("-------------------------------------------------------");
                    }
                }
                catch (Exception e)
                {
                    if (!File.Exists(Environment.CurrentDirectory + "\\" + DebugFileName))
                    {
                        debugTextFilePath = Environment.CurrentDirectory + "\\" + DebugFileName;
                        var file = File.Create(debugTextFilePath);
                        Log(LogType.Error, $"Failed to create a debug log. Ironic. Exception: {e.ToString()}");
                        file.Close();
                    }
                    try
                    {
                        using (StreamWriter sw = File.AppendText(Environment.CurrentDirectory + "\\" + DebugFileName))
                        {
                            sw.WriteLine($"At {DateTime.Now.ToShortTimeString()} could not write log following message \"{message}\" with exception in the logger: {e.ToString()}");
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        //TODO
                    }
                }
            }
        }
        public static bool LogPathExists()
        {
            return debugTextFilePath != null;
        }
    }
}
