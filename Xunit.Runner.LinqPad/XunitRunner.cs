using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Runners;

namespace Xunit.Runner.LinqPad
{
    public class XunitRunner
    {
        // We use consoleLock because messages can arrive in parallel, so we want to make sure we get
        // consistent console output.
        static object consoleLock = new object();

        // Use an event to know when we're done
        static ManualResetEvent finished = new ManualResetEvent(false);

        // Start out assuming success; we'll set this to 1 if we get a failed test
        static int result = 0;

        public static int Run(Assembly assembly)
        {
            var targetAssembly = GetTargetAssemblyFilename(assembly);
                        
            using (var runner = AssemblyRunner.WithoutAppDomain(targetAssembly))
            {
                runner.OnDiscoveryComplete = OnDiscoveryComplete;
                runner.OnExecutionComplete = OnExecutionComplete;
                runner.OnTestFailed = OnTestFailed;
                runner.OnTestSkipped = OnTestSkipped;

                Console.WriteLine("Discovering...");

                runner.Start();

                finished.WaitOne();
                finished.Dispose();

                return result;
            }
        }

        static string GetTargetAssemblyFilename(Assembly assembly)
        {
            if(assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            var assemblyFilename = assembly.Location;

            var shadowFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var xunitFolder = Path.GetDirectoryName(typeof(Xunit.Assert).Assembly.Location);

            //Console.WriteLine($"ShadowFolder \"{ shadowFolder }\"");
            //Console.WriteLine($"XUnitFolder \"{ xunitFolder }\"");

            if (shadowFolder != xunitFolder || Directory.GetFiles(shadowFolder, "xunit.execution.*.dll").Length == 0)
            {
                throw new InvalidOperationException("Please enable the shadow folder option for none-framework references (F4 -> Advanced).");
            }

            var targetAssembly = Path.Combine(shadowFolder, Path.GetFileName(assemblyFilename));

            //Console.WriteLine($"Copy \"{ assemblyFilename }\" -> \"{ targetAssembly }\"");
            File.Copy(assemblyFilename, targetAssembly, true);

            return targetAssembly;
        }

        static void OnDiscoveryComplete(DiscoveryCompleteInfo info)
        {
            lock (consoleLock)
                Console.WriteLine($"Running {info.TestCasesToRun} of {info.TestCasesDiscovered} tests...");
        }

        static void OnExecutionComplete(ExecutionCompleteInfo info)
        {
            lock (consoleLock)
                Console.WriteLine($"Finished: {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed, {info.TestsSkipped} skipped)");

            finished.Set();
        }

        static void OnTestFailed(TestFailedInfo info)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("[FAIL] {0}: {1}", info.TestDisplayName, info.ExceptionMessage);
                if (info.ExceptionStackTrace != null)
                    Console.WriteLine(info.ExceptionStackTrace);

                Console.ResetColor();
            }

            result = 1;
        }

        static void OnTestSkipped(TestSkippedInfo info)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[SKIP] {0}: {1}", info.TestDisplayName, info.SkipReason);
                Console.ResetColor();
            }
        }
    }
}
