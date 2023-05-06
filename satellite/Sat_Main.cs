using System;
using System.Text;
using System.Linq;
using rmb.shared;
using System.Collections.Generic;
using showdown.controller;
using Newtonsoft.Json;
using System.Threading.Tasks;
using showdown.model;

namespace showdown.satellite
{
    public class SatMain : rmb.shared.SatelliteHandler
    {
        private static Showdown game;
        public static rmb.satellite.Handler handler;
        public static void Entry(string[] args)
        {
            //Controller.SetupLogging(typeof(Showdown), args);
            var ctrl = new SatMain();
            handler = rmb.satellite.Handler.Create(Util.log, args, ctrl);
            game = new Showdown(handler);

            System.Timers.Timer timer = new System.Timers.Timer(interval: 1500);
            timer.Elapsed += async (sender, e) => await HandleTimerAsync();
            timer.Start();

            //the following call will return only once the game ends.
            Controller.MainLoop(handler);
            timer.Dispose();
            Util.log.Info("Satellite showdown done.");
        }
        static Task HandleTimerAsync()
        {
            if (handler.CustomTarget != 0)
            {
                var compiledMsg = new Dictionary<int, object>() { { (int)NetworkId.SetActorPeerType, handler.CustomTarget } };
                var str = JsonConvert.SerializeObject(compiledMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                handler.SendToControllers(bytes, null);
                Util.log.Info("[Sat_Main.HandleTimerAsync] send ActorPeerType to controller, actorPeerType:" + handler.CustomTarget);
            }
            return Task.CompletedTask;
        }

        public bool OnMessage(byte[] payload)
        {
            Controller.RegisterActivity(payload.Count());
            //dump the payload as a string before any deserialization attempt.
            var str = UTF8Encoding.UTF8.GetString(payload);
            Util.log.Info("Received common message " + str);
            return game.ProcessMessage(payload, str);
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

        public bool OnPlayerDisconnect(string playerID)
        {
            Util.log.Info("Player Disconnected: " + playerID);
            return true;
        }
        public bool OnPlayerConnected(string playerID)
        {
            Util.log.Info("Player Connected: " + playerID);
            handler.RequestAllPlayers();

            Util.log.Info("all players" + handler.Players.Count);
            return true;
        }
        public bool OnAllPlayersReceived(IList<string> playerIDs)
        {
            Util.log.Info(string.Format("Received all players: {0} ", playerIDs.Count));
            Util.log.Info("all players" + JsonConvert.SerializeObject(handler.Players));
            return true;
        }
    }
}
