using System;
using System.Diagnostics;

namespace SVNIntegration
{
    public class Executor
    {

        public string WorkingDirectory { get; set; }
        public string FileName { get; set; }
        public string Arguments { get; set; }
        public string ErrorMatchString { get; set; }
        private int _errorCode;

        public int ErrorCode { get { return _errorCode; } }

        protected void ProcOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null && e.Data.Contains(ErrorMatchString))
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.WriteLine(e.Data);
        }

        protected void ProcErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Data);
        }

         public void Exec()
         {


             ProcessStartInfo procStartInfo = new ProcessStartInfo();

             procStartInfo.WorkingDirectory = WorkingDirectory;
             procStartInfo.FileName = FileName;
             procStartInfo.Arguments = Arguments;
             procStartInfo.UseShellExecute = false;
             procStartInfo.RedirectStandardOutput = true;
             procStartInfo.CreateNoWindow = true;
             procStartInfo.RedirectStandardOutput = true;
             procStartInfo.RedirectStandardError = true;

             using (Process proc = new Process())
             {
                 proc.StartInfo = procStartInfo;
                 proc.Start();
                 proc.BeginOutputReadLine();
                 proc.OutputDataReceived += ProcOutputDataReceived;
                 proc.ErrorDataReceived += ProcErrorDataReceived;
                 proc.WaitForExit();
                 _errorCode = proc.ExitCode;
                 if (proc.ExitCode == 0)
                 {
                     Console.WriteLine("OK");
                 }
                 else
                 {
                     Console.ForegroundColor = ConsoleColor.Red;
                     Console.WriteLine("ERROR: " + proc.ExitCode);
                     throw new ExecutorException(procStartInfo, proc.ExitCode, "");
                 }
             }
         }
    }

    public class ExecutorException : Exception
    {
        public int ExitCode { get; set; }
        public ExecutorException(ProcessStartInfo procStartInfo, int exitCode, string message): base(string.Format("Errore esecuzione {0} {1}.", procStartInfo.FileName, procStartInfo.Arguments))
        {
            ExitCode = exitCode;
        }
    }
}