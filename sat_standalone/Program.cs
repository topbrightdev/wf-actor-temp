using System;
using System.IO;
using showdown.controller;

namespace sat_standalone
{
    class Program
    {
        /// <summary>
        /// Name of this application. Usually this is name of the game's actor.
        /// Add a suffix to indicate this is satellite.
        /// </summary>
        public const string SourceApplication = "showdown (sat)";

        static void Main(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            Config.Setup(typeof(showdown.satellite.SatMain), args, SourceApplication);
            showdown.satellite.SatMain.Entry(args);
        }
    }
}
