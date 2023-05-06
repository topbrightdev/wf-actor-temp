using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using rmb.shared;
using Newtonsoft.Json;
using showdown.controller;

namespace actor
{
    public class SpawningInfo
    {
        public string name;
        public string key;
        public bool ms;
        public string connectionString;
        public bool success;
    }
    public class Spawner
    {
        TcpListener server = null;
        int pid;
        Dictionary<string, bool> games;
        System.Timers.Timer roomConnectionTimer;
        public Spawner(int port, int pid, Dictionary<string, bool> games)
        {
            this.pid = pid;
            this.games = games;

            var ip = "0.0.0.0";
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            server.Start();
            StartListener();
        }
        private void InitializeRMB(NetworkStream stream)
        {
            var dict = new { pid = this.pid, games = this.games };
            var json = JsonConvert.SerializeObject(dict);
            var payload = Encoding.UTF8.GetBytes(json);
            WritePayload(stream, payload);
            Util.log.Info($"Initialized rmb with {json}");
        }
        private void WritePayload(NetworkStream stream, byte[] payload)
        {
            byte[] sizeBuf = BitConverter.GetBytes(payload.Length);
            stream.Write(sizeBuf, 0, sizeBuf.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }
        private void Done()
        {
            if (server != null)
            {
                Controller.Stop();
                var toStop = server;
                server = null;
                toStop.Stop();
            }
        }
        private bool ProcessMessage(string json, NetworkStream stream)
        {
            //Dictionary<string, object> msg;
            var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach (var kvp in msg)
            {
                if (kvp.Key == "init")
                {
                    Util.log.Info("Received init request");
                    InitializeRMB(stream);
                }
                else
                if (kvp.Key == "room")
                {
                    Util.log.Info($"Received room connection event: {kvp.Value}");
                    var innerJson = JsonConvert.SerializeObject(kvp.Value);
                    var connected = JsonConvert.DeserializeObject<bool>(innerJson);
                    if (connected)
                    {
                        if (roomConnectionTimer != null)
                        {
                            roomConnectionTimer.Stop();
                            roomConnectionTimer = null;

                            
                        }
                    }
                    else
                    {
                        if (roomConnectionTimer == null)
                        {
                            roomConnectionTimer = new System.Timers.Timer(1500);
                            roomConnectionTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                            {
                                var src = roomConnectionTimer;
                                roomConnectionTimer = null;
                                src.Stop();
                                Done();
                                Thread.Sleep(1000);
                                Util.log.Info($"Restarting main processing...");
                                Showdown.Exec();


                            };
                            roomConnectionTimer.Start();

                            //return false to stop client processing.
                            return false;
                        }
                    }
                    
                }
                else if (kvp.Key == "done")
                {
                    Util.log.Info("Received done signal");
                    Done();
                    return false;
                }
                else if (kvp.Key == "spawn")
                {
                    Util.log.Info("Processing spawn request...");
                    var innerJson = JsonConvert.SerializeObject(kvp.Value);
                    var game = JsonConvert.DeserializeObject<SpawningInfo>(innerJson);
                    Util.log.InfoFormat("Parsed game name: {0}", game.name);
                    game.success = false;
                    if (this.games.ContainsKey(game.name))
                    {
                        var args = new string[] { "-gamename", game.name, "-key", game.key, "-connection", game.connectionString };
                        new Thread(() =>
                        {
                            if (game.ms)
                            {
                                showdown.mothership.MothershipMain.Entry(args);
                            }
                            else
                            {
                                showdown.satellite.SatMain.Entry(args);
                            }

                        }).Start();

                        game.success = true;
                        var dict = new Dictionary<string, SpawningInfo> { { "spawned", game } };
                        var res = JsonConvert.SerializeObject(dict);
                        WritePayload(stream, Encoding.UTF8.GetBytes(res));

                    }


                }
            }

            return true;
        }
        public void StartListener()
        {
            try
            {
                while (true)
                {
                    Util.log.Info("Waiting for RMB to connect");
                    TcpClient client = server.AcceptTcpClient();
                    Util.log.InfoFormat("New RMB connected.");

                    new Thread(HandleRMB).Start(client);
                }
            }
            catch (Exception e)
            {
                if (server != null)
                {
                    Util.log.Error($"SocketException: {e}");
                    server.Stop();
                }
                else
                {
                    Util.log.Info($"Exception with server set to null, ignore.");
                }
            }
        }

        public void HandleRMB(object obj)
        {
            TcpClient client = obj as TcpClient;
            var stream = client.GetStream();
            try
            {
                while (true)
                {
                    byte[] sizeBuffer = new byte[4];
                    int count = 0;
                    count = stream.Read(sizeBuffer, 0, sizeBuffer.Length);
                    if (count == sizeBuffer.Length)
                    {
                        var size = BitConverter.ToInt32(sizeBuffer, 0);
                        if (size > 0)
                        {
                            var payload = new byte[size];
                            count = stream.Read(payload, 0, payload.Length);
                            if (count == payload.Length)
                            {
                                var raw = Encoding.UTF8.GetString(payload);
                                Util.log.InfoFormat("Spawner received raw message: {0}", raw);
                                if (!ProcessMessage(raw, stream))
                                {
                                    break;
                                }
                            }
                            else
                            {
                                Util.log.ErrorFormat("Failed to read all bytes for payload {0} <> {1}", payload.Length, count);
                            }
                        }
                        
                    }
                    else
                    {
                        if (count > 0)
                        {
                            Util.log.ErrorFormat("Failed to read all bytes for size {0} <> {1}", sizeBuffer.Length, count);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (server != null)
                {
                    Util.log.ErrorFormat("Exception: {0}", e.ToString());
                    client.Close();
                }
            }
            Util.log.Info("Done handling rmb.");
        }
    }
}
