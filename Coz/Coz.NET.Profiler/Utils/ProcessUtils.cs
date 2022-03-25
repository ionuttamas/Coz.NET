using System.Diagnostics;

namespace Coz.NET.Profiler.Utils
{
    public class ProcessUtils
    {
        public static void StartProcess(string executablePath, 
            string arguments, 
            string workingDirectory="",
            bool redirectStandardOutput = false,
            bool useShellExecute = true, 
            bool createNoWindow = true)
        {
            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = workingDirectory,
                    FileName = executablePath,
                    Arguments = arguments,
                    RedirectStandardOutput = redirectStandardOutput,
                    UseShellExecute = useShellExecute,
                    CreateNoWindow = createNoWindow,
                    
                }
            };

            process.Start();
            process.WaitForExit();
        }
    }
}
