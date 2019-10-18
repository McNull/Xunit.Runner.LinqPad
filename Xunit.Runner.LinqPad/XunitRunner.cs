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
        private static readonly object sync = new object();
        private ManualResetEvent done;
        private int result = 0;
        private Assembly testAssembly = null;

        public static object Sync => sync;

        public XunitRunner(Assembly assembly)
        {
            if(assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            this.testAssembly = assembly;    
        }

        public static int Run(Assembly assembly, Action<AssemblyRunner> configureRunner = null)
        {
            var runner = new XunitRunner(assembly);
            return runner.Run(configureRunner);
        }

        public int Run(Action<AssemblyRunner> configureRunner = null)
        {
            var targetAssembly = GetTargetAssemblyFilename(this.testAssembly);

            using (var runner = AssemblyRunner.WithoutAppDomain(targetAssembly))
            {
                using (this.done = new ManualResetEvent(false))
                {
                    runner.OnDiscoveryComplete = this.OnDiscoveryComplete;
                    runner.OnExecutionComplete = this.OnExecutionComplete;
                    runner.OnTestFailed = this.OnTestFailed;
                    runner.OnTestSkipped = this.OnTestSkipped;

                    configureRunner?.Invoke(runner);

                    runner.Start();

                    this.done.WaitOne();
                }

                return this.result;
            }
        }


        protected virtual void OnDiscoveryComplete(DiscoveryCompleteInfo info)
        {
            lock (sync)
                Console.WriteLine($"Running {info.TestCasesToRun} of {info.TestCasesDiscovered} tests...");
        }

        protected virtual void OnExecutionComplete(ExecutionCompleteInfo info)
        {
            lock (sync)
                Console.WriteLine($"Finished: {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed, {info.TestsSkipped} skipped)");

            this.done.Set();
        }

        protected virtual void OnTestFailed(TestFailedInfo info)
        {
            lock (sync)
            {
                Console.WriteLine("[FAIL] {0}: {1}", info.TestDisplayName, info.ExceptionMessage);

                if (info.ExceptionStackTrace != null)
                    Console.WriteLine(info.ExceptionStackTrace);
            }

            result = 1;
        }

        protected virtual void OnTestSkipped(TestSkippedInfo info)
        {
            lock (sync)
            {
                Console.WriteLine("[SKIP] {0}: {1}", info.TestDisplayName, info.SkipReason);
            }
        }

        static string GetTargetAssemblyFilename(Assembly assembly)
        {
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
    }
}
