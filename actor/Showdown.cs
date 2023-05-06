using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using showdown.controller;
using rmb.shared;

namespace actor
{
    class Showdown
    {
        /// <summary>
        /// Name of this application. Usually this is name of the game's actor.
        /// Add a suffix to indicate this is local.
        /// </summary>
        public const string SourceApplication = "showdown (local)";

        static string rmbFullPath;
        static string rmbDir;
        static string configFullPath;

        public static void Exec()
        {
            new Thread(() =>
            {
                Spawner spawner = new Spawner(9025, Process.GetCurrentProcess().Id, new Dictionary<string, bool>() { { "showdown", true } });
            }).Start();
            Thread.Sleep(500);

            Util.log.Info($"starting rmb process at {rmbFullPath}");

            var process = new Process();
            process.StartInfo.FileName = rmbFullPath;
            process.StartInfo.WorkingDirectory = rmbDir;
            process.StartInfo.Arguments = $"-cfg {configFullPath}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender, data) =>
            {
                Util.log.Info(data.Data);
            };

            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (sender, data) =>
            {
                Util.log.Error(data.Data);
            };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Util.log.ErrorFormat($"Failed to start rmb. ${ex}");
            }
        }

        static void Main(string[] args)
        {
            var rmbPath = "";

            var osVersion = Environment.OSVersion;
            if (osVersion.Platform == PlatformID.Win32NT || osVersion.Platform == PlatformID.Win32Windows ||
                osVersion.Platform == PlatformID.Xbox)
            {
                rmbPath = "lib/bin/windows/amd64/rmb.exe";
            }
            else if (osVersion.Platform == PlatformID.Unix)
            {
                //always return mac for now
                rmbPath = "lib/bin/mac/rmb";
            }

            if (rmbPath.Length == 0)
            {
                throw new Exception("Unsupported OS.");
            }

            var basePath = "../../../../";
            rmbFullPath = Path.GetFullPath(basePath + rmbPath);
            rmbDir = Path.GetDirectoryName(rmbFullPath);
            configFullPath = Path.GetFullPath(basePath + "rmb.config.json");

            // inject bus config into arguments (simulate rmb behaviour when it launch actor)
            var file = File.ReadAllText(configFullPath);
            var vals = new List<string>(args);
            var index = vals.FindIndex(0, x => x == "-busconfig");
            if (index >= 0 && index + 1 < vals.Count)
            {
                // replace existing value
                vals[index + 1] = file;
            }
            else
            {
                // add bus config
                vals.Add("-busconfig");
                vals.Add(file);
            }
            Util.Setup(typeof(Showdown), vals.ToArray(), SourceApplication, Config.SourceVersion);

            Exec();
        }
    }
}
