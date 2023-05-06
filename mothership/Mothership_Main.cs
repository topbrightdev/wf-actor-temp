using System;
using System.Text;
using rmb.shared;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using showdown.controller;

namespace showdown.mothership
{
    public class MothershipMain : rmb.shared.MothershipHandler
    {
        private static Showdown game;
        public static void Entry(string[] args)
        {
            Util.log.Info("Mothership showdown start test windows.");
            var ctrl = new MothershipMain();
            var handler = rmb.mothership.Handler.Create(Util.log, args, ctrl);
            game = new Showdown(handler);
            //the following call will return only once the game ends.
            Controller.MainLoop(handler);
            Util.log.Info("Mothership showdown done.");
        }
        public bool OnMessage(byte[] payload)
        {
            Util.log.Info("Received common message something");
            Controller.RegisterActivity(payload.Count());
            //When using json to format messages, it is a good idea to log the content before any attempted processing.
            var str = UTF8Encoding.UTF8.GetString(payload);
            Util.log.Info("Received common message string" + str);
            game.ProcessMessage(payload, str);
            return true;
        }

        public bool OnClose()
        {
            Util.log.Info("Received close message");
            return true;
        }
        public bool OnStart(byte target)
        {
            Util.log.Info("Received start message " + target);
            return true;
        }
        public bool OnStop()
        {
            Util.log.Info("Received stop message");
            return true;
        }
        public bool OnPlayerCount(int count)
        {
            Util.log.Info("Received player count " + count);
            return true;
        }

        public bool OnAllSatellitesResponded(int pending)
        {
            //game.CheckSatPending(pending);
            return true;
        }

        public Dictionary<string, object> OnRunningInfo(string usedURL)
        {
            Util.log.Info("Received Running Info Request: " + usedURL);
            var res = game.GetRunningInfo(usedURL);
            Util.log.Info("Returned Running Info: " + Newtonsoft.Json.JsonConvert.SerializeObject(res));
            return res;
        }
    }
}
