using System;
using System.Text;
using System.Linq;
using rmb.shared;
using System.Collections.Generic;

namespace showdown.controller
{
    public class Controller
    {
        public static bool running = true;

        public static bool mustUpdate = true;

        public static void RegisterActivity(int size)
        {
            mustUpdate = true;
        }

        public static void Stop()
        {
            running = false;
        }

        public static void MainLoop(rmb.shared.Handler handler)
        {
            running = true;
            while (running)
            {
                if (mustUpdate)
                {
                    handler.Update();
                }
                else
                {
                    System.Threading.Thread.Sleep(50);
                    RegisterActivity(0);
                }
            }
            Util.log.Info("Controller is done.");
        }
    }
}
