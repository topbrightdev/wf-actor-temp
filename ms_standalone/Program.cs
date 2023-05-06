using System;
using System.IO;
using showdown.controller;

namespace ms_standalone
{
    class Program
    {
        /// <summary>
        /// Name of this application. Usually this is name of the game's actor.
        /// Add a suffix to indicate this is mothership.
        /// </summary>
        public const string SourceApplication = "showdown (ms)";

        static void Main(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            Config.Setup(typeof(showdown.mothership.MothershipMain), args, SourceApplication);
            showdown.mothership.MothershipMain.Entry(args);
        }
    }
}
