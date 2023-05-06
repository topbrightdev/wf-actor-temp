using System;
using System.Text;
using rmb.shared;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using showdown.model;
using Newtonsoft.Json;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace showdown.mothership
{
    public class Showdown
    {
        private rmb.mothership.Handler mHandler;
        private RoundHandler roundHandler;
        public GameData gameData;
        public List<Card> cards = new List<Card>();
        Random cardSelectRnd = new Random();
        public List<PlayerInfo> players = new List<PlayerInfo>();
        private List<PlayerInfo> newlyPlayers = new List<PlayerInfo>();
        private int currentLeaderboardPointIndex = 0;
        private int currentFinalboardPointIndex = 0;
        string gameDataPath = "";
        public bool gameReady;

        public Showdown(rmb.mothership.Handler handler)
        {
            this.gameReady = false;
            this.mHandler = handler;
            this.roundHandler = new RoundHandler(this);
        }

        public void MakeTestPlayers()
        {
            List<WebPlayerPayload> testPlayers = new List<WebPlayerPayload>();
            Random avatarRnd = new Random();
            for (int i = 0; i < 7; i++)
            {
                testPlayers.Add(new WebPlayerPayload("@" + i, "@test" + i, avatarRnd.Next(232)));
            }
            for (int i = 0; i < testPlayers.Count; i++)
            {

                //var resMsg = new Dictionary<int, object>() { { (int)NetworkId.SendResults, testPlayers[i] } };
                var str = JsonConvert.SerializeObject(testPlayers[i]);
                PlayerJoined(str, null);
            }
            Util.log.Info("Ms_Showdown.MakeTestPlayers finished");
        }

        public bool ProcessMessage(byte[] payload, string str)
        {
            try
            {
                Util.log.Info($"[Showdown.mothership] ProcessMessage: {str}");

                //Note: You can send w.e object you want & deserialize it to w.e object you want. 
                //      However in our learnings a Dictionary<int,object> has been the best to work with
                var data = JsonConvert.DeserializeObject<Dictionary<int, object>>(str);
                foreach (var msg in data)
                {
                    string value = JsonConvert.SerializeObject(msg.Value);
                    switch (msg.Key)
                    {
                        case (int)NetworkId.GetOpConScreen:
                            {
                                SendGameStateToOpCon();
                                break;
                            }
                        case (int)NetworkId.SetActorPeerType:
                            {
                                HandleSessionData(value, payload);
                                break;
                            }
                        case (int)NetworkId.ControllerReadyRequestingQuestionInfo:
                            {
                                PlayerJoined(value, payload);
                                break;
                            }
                        case (int)NetworkId.GetRecentJoinedPlayers:
                            {
                                SendRecentJoinedPlayers(value, payload);
                                break;
                            }
                        //form gameserver
                        case (int)NetworkId.GameData:
                            {
                                DownloadGameData(value, payload);
                                break;
                            }
                        case (int)NetworkId.CallLetterEvent:
                            {
                                CallLetterEventHandler(value, payload);
                                break;
                            };
                        //WheelRotationCompleted
                        case (int)NetworkId.WheelRotationCompleted:
                            {
                                SendAllAudienceStateToSatlites();
                                break;
                            };
                        // Call letter finished
                        case (int)NetworkId.CallLetterFinished:
                            {
                                SendAllAudienceStateToSatlites();
                                break;
                            }
                        case (int)NetworkId.ContestantRevealFinished:
                            {
                                SendAllAudienceStateToSatlites();
                                break;
                            }
                        //From Opcon
                        case (int)NetworkId.LaunchWof:
                            {
                                LaunchWof(value, payload);
                                break;
                            };
                        case (int)NetworkId.Contestant_Draw:
                            {
                                ContestantDraw(value, payload);
                                break;
                            };
                        case (int)NetworkId.Contestant_Reveal:
                            {
                                ContestantReveal(value, payload);
                                break;
                            };
                        case (int)NetworkId.PreRoundStage:
                            {
                                SendPreRoundEventToGameServer();
                                break;
                            }
                        case (int)NetworkId.AudienceGameIntroStage:
                            {
                                AudienceGameIntroEventHandler();
                                break;
                            }
                        case (int)NetworkId.RevealPuzzle:
                            {
                                RevealPuzzleEventHandler();
                                break;
                            }
                        case (int)NetworkId.SpinWheel:
                            {
                                SpinWheelEventHandler(value, payload);
                                break;
                            }
                        case (int)NetworkId.CallLetter:
                            {
                                CallLetterEventHandler(value, payload);
                                break;
                            }
                        case (int)NetworkId.Player_Select:
                            {
                                PlayerSelectEventHandler(value, payload);
                                break;
                            }
                        case (int)NetworkId.BuyVowel:
                            {
                                BuyVowelEventHandler();
                                break;
                            }
                        case (int)NetworkId.Solve:
                            {
                                SolveEventHandler(false);
                                break;
                            }
                        case (int)NetworkId.ShoutOut:
                            {
                                SolveEventHandler(true);
                                break;
                            }
                        case (int)NetworkId.AudienceLeaderboardStage:
                            {
                                LeaderboardStageEventHandler();
                                break;
                            }
                        case (int)NetworkId.RevealAudiencePoint:
                            {
                                RevealLeaderboardPointEventHandler();
                                break;
                            }
                        case (int)NetworkId.RevealFinalPoint:
                            {
                                RevealFinalboardPointEventHandler();
                                break;
                            }
                        case (int)NetworkId.ThankYouStage:
                            {
                                ThankYouStageHandler();
                                break;
                            }
                        case (int)NetworkId.EndGame:
                            {
                                EndGameEventHandler();
                                break;
                            }
                        default:
                            {
                                Util.log.Info($"[Showdown.mothership] Mothership received invalid message passing down to satellite with id[{msg.Key}] and value: {value}");
                                mHandler.SendToSatellites(payload);

                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Util.log.Error(" Bad message:" + str + " => " + ex.Message + " => " + ex.StackTrace);
            }
            return true;
        }

        private void HandleSessionData(string value, byte[] payload)
        {


        }

        private void SendGameStateToOpCon()
        {
            RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.roundState, gameReady);
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.GameReady, sendData } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            Util.log.Info("[Ms_ShowDown.SendGameStateToOpCon: " + gameReady);
        }
        public Dictionary<string, object> GetRunningInfo(string usedURL)
        {
            var res = new Dictionary<string, object>();
            res["source"] = usedURL;
            res["info"] = roundHandler.roundState;
            res["name"] = "Showdown";
            return res;
        }

        private void PlayerJoined(string value, byte[] payload)
        {
            Util.log.Info($"mothership-showdown: receive joinedPlayer from satellite player: {value} cardCount:{cards.Count} playerCount:{players.Count}");
            try
            {
                WebPlayerPayload webPlayer = JsonConvert.DeserializeObject<WebPlayerPayload>(value);
                PlayerInfo player = new PlayerInfo(webPlayer._deviceId, webPlayer._nickname, webPlayer._avatarId);
                bool isNewPlayer = players.Find((x) => x.playerID == player.playerID) == null;
                Util.log.Info("mothership-showdown: isNewPlayer == " + isNewPlayer);
                if (!isNewPlayer)
                {
                    SendAudienceStateToSatlites(player.playerID);
                    return;
                }
                newlyPlayers.Add(player);
                Random cardNumRnd = new Random();

                List<Card> assignableCards = cards.FindAll((x) => x.inUse == false);
                if (assignableCards.Count < 10)
                {
                    Util.log.Error("mothership-showdown: no enough assignable card");
                }

                int randomCardCount = cardNumRnd.Next(1, 5);

                for (int p = 0; p < randomCardCount; p++)
                {
                    int cardIndex = cardSelectRnd.Next(assignableCards.Count);
                    Util.log.Info("mothership-showdown: random cardIndex == " + cardIndex);
                    var card = assignableCards[cardIndex];
                    Util.log.Info("mothership-showdown: random card == " + JsonConvert.SerializeObject(card));
                    player.cards.Add(card.serialNumber);
                    card.inUse = true;
                    card.owner = player.playerID;
                    card.score = 0;
                    card.rank = -1;
                    for (int i = 0; i < card.points.Count; i++)
                    {
                        card.pointData.Add(new List<Point>());
                        for (int k = 0; k < card.points[i].Count; k++)
                        {
                            int pointValue = gameData.scoreTable.Find(item => item.key == card.points[i][k]).value;
                            card.pointData[i].Add(new Point(card.points[i][k], pointValue, 0));
                        }
                    }
                    card.points = null;
                    assignableCards.RemoveAt(cardIndex);
                }
                players.Add(player);
                SendAudienceStateToSatlites(player.playerID);
            }
            catch (Exception ex)
            {
                Util.log.Error("mothership-showdown: PlayerJoined >> fail player join " + " => " + ex.Message + " => " + ex.StackTrace);
            }
        }

        private void SendAudienceStateToSatlites(string playerID)
        {
            List<Card> audienceCards = cards.FindAll(x => x.inUse && x.owner == playerID).ToList();
            List<PlayerInfo> reveledContestants = GetAllContestant().FindAll(x => x.revealed);
            List<string> revealedConestantIDs = new List<string>();
            foreach (var item in reveledContestants)
            {
                revealedConestantIDs.Add(item.playerID);
            }
            GameStepType gameStep = roundHandler.gameStep;
            int currentContestantIndex = roundHandler.roundState.currentContestantIndex;
            int currentRoundIndex = roundHandler.roundState.currentRoundIndex;
            List<string> currentPointKeys = roundHandler.roundState.currentPointKeys;

            var payload = new AudienceState(playerID, audienceCards, gameStep, revealedConestantIDs, currentContestantIndex, currentRoundIndex, currentPointKeys);
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.AudienceState, payload } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToSatellites(bytes);
            Util.log.Info("Showdown mothership sent satellite msg:" + str);
        }

        public void SendAllAudienceStateToSatlites()
        {
            foreach (var item in players)
            {
                SendAudienceStateToSatlites(item.playerID);
            }
        }

        private void DownloadGameData(string value, byte[] payload)
        {
            try
            {
                string gameDataUrl = JsonConvert.DeserializeObject<string>(value);
                Util.log.Info($"mothership-showdown: received gameData url form gameserver >> {gameDataUrl}");
                Download(gameDataUrl);
            }
            catch (Exception ex)
            {
                Util.log.Error("mothership-showdown: received puzzleData form gameserver >> fail load json " + " => " + ex.Message + " => " + ex.StackTrace);
            }
        }
        private void SendRecentJoinedPlayers(string value, byte[] payload)
        {
            try
            {
                if (newlyPlayers.Count == 0) return;
                int requestNum = JsonConvert.DeserializeObject<int>(value);
                List<PlayerInfo> sendPlayers = new List<PlayerInfo>();
                if (newlyPlayers.Count > requestNum)
                {
                    sendPlayers = newlyPlayers.GetRange(0, requestNum);
                    newlyPlayers.RemoveRange(0, requestNum);
                }
                else
                {
                    sendPlayers = newlyPlayers.GetRange(0, newlyPlayers.Count);
                    newlyPlayers.Clear();
                }
                List<PlayerInfoPayload> sendData = new List<PlayerInfoPayload>();
                foreach (var item in sendPlayers)
                {
                    sendData.Add(new PlayerInfoPayload(-1, item.nickname, 0, false, 0, item.avatarID));
                }
                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.RecentJoinedPlayers, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);
                Util.log.Info($"mothership-showdown: SendRecentJoinedPlayers >> {JsonConvert.SerializeObject(sendData)}");
            }
            catch (Exception ex)
            {
                Util.log.Error("mothership-showdown: SendRecentJoinedPlayers >> fail load json " + " => " + ex.Message + " => " + ex.StackTrace);
            }
        }

        private void LaunchWof(string value, byte[] payload)
        {
            roundHandler.gameStep = GameStepType.welcomeStep;
            var sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.roundState, true);
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.LaunchWof, sendData } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            Util.log.Info($"mothership-showdown.LaunchWof >> {JsonConvert.SerializeObject(sendData)}");
        }

        private void ContestantDraw(string value, byte[] _payload)
        {
            try
            {
                IntPayload payload = JsonConvert.DeserializeObject<IntPayload>(value);
                if (SetRandomContestant(payload.num))
                {
                    roundHandler.gameStep = GameStepType.contestantDrawing;
                    roundHandler.roundState.currentContestantIndex = payload.num;
                    var sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.roundState, true);
                    var resMsg = new Dictionary<int, object>() { { (int)NetworkId.Contestant_Draw, sendData } };
                    var str = JsonConvert.SerializeObject(resMsg);
                    var bytes = UTF8Encoding.UTF8.GetBytes(str);
                    mHandler.SendToRoom(bytes);
                    SendAllAudienceStateToSatlites();
                    Util.log.Info($"mothership-showdown.ContestantDraw >> {JsonConvert.SerializeObject(sendData)}");
                }
                else
                {
                    SendErrorMessageToGameServer("[OPERATOR ERROR - FROM ACTOR] No audience, wait more for audience engagement.");
                }
            }
            catch (System.Exception e)
            {
                Util.log.Info($"mothership-showdown.ContestantDraw.err >> " + e);
                throw;
            }
        }

        private void ContestantReveal(string vlaue, byte[] payload)
        {
            PlayerInfo contestant = GetContestantFormIndex(roundHandler.roundState.currentContestantIndex);
            if (contestant != null)
            {
                roundHandler.gameStep = GameStepType.contestantRevealed;
                contestant.revealed = true;
                roundHandler.UpdateContestantsState(SendBalanceType.round);
                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.Contestant_Reveal, new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.roundState, true) } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);
                Util.log.Info($"mothership-showdown.ContestantReveal send command");
            }
            else
            {
                Util.log.Info($"mothership-showdown.ContestantReveal send failed");
            }
        }

        private void SendPreRoundEventToGameServer()
        {
            if (roundHandler.roundState.contestants.Count < 3)
            {
                SendErrorMessageToGameServer("Can't go preround because contestant num is no 3");
                return;
            }
            RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.roundState, true);
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.PreRoundStage, sendData } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            Util.log.Info($"mothership-showdown: SendRoundContestantsToGameServer >> {JsonConvert.SerializeObject(sendData)}");
        }

        private void AudienceGameIntroEventHandler()
        {

        }

        private void RevealPuzzleEventHandler()
        {
            roundHandler.gameStep = GameStepType.roundStep;
            currentLeaderboardPointIndex = 0;
            PuzzleDataPayLoad puzzleData = GetRandomPuzzle();
            roundHandler.InitRound(puzzleData);
            RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.roundState, true);

            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.RevealPuzzle, sendData } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            SendAllAudienceStateToSatlites();
            Util.log.Info($"mothership-showdown.RevealPuzzleEventHandler >> {JsonConvert.SerializeObject(sendData)}");
        }

        private void SpinWheelEventHandler(string value, byte[] _payload)
        {
            try
            {
                roundHandler.gameStep = GameStepType.roundStep;
                IntPayload payload = JsonConvert.DeserializeObject<IntPayload>(value);
                WheelStrengthState strengthType = (WheelStrengthState)payload.num;
                RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.RotateWheel(strengthType), true);

                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.SpinWheel, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);

                Util.log.Info($"mothership-showdown.SpinWheelEventHandler >> {JsonConvert.SerializeObject(sendData)}");
            }
            catch (System.Exception e)
            {
                Util.log.Info($"mothership-showdown.SpinWheelEventHandler.err >> " + e);
                throw;
            }
        }

        private void PlayerSelectEventHandler(string value, byte[] _payload)
        {
            try
            {
                roundHandler.gameStep = GameStepType.roundStep;
                IntPayload payload = JsonConvert.DeserializeObject<IntPayload>(value);
                int playerIndex = payload.num;
                RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.PlayerSelect(playerIndex), true);

                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.Player_Select, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);

                Util.log.Info($"mothership-showdown.PlayerSelectEventHandler >> {JsonConvert.SerializeObject(sendData)}");
            }
            catch (System.Exception e)
            {
                Util.log.Info($"mothership-showdown.PlayerSelectEventHandler.err >> " + e);
                throw;
            }
        }

        private void CallLetterEventHandler(string value, byte[] _payload)
        {
            try
            {
                roundHandler.gameStep = GameStepType.roundStep;
                StringPayload payload = JsonConvert.DeserializeObject<StringPayload>(value);
                string letter = payload.str;
                RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.CallLetter(letter), true);

                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.CallLetter, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);

                Util.log.Info($"mothership-showdown.CallLetterEventHandler >> {JsonConvert.SerializeObject(sendData)}");
            }
            catch (System.Exception e)
            {
                Util.log.Info($"mothership-showdown.CallLetterEventHandler.err >> " + e);
                throw;
            }
        }

        private void BuyVowelEventHandler()
        {
            try
            {
                roundHandler.gameStep = GameStepType.roundStep;
                RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.BuyVowel(), true);

                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.BuyVowel, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);

                Util.log.Info($"mothership-showdown.BuyVowelEventHandler >> {JsonConvert.SerializeObject(sendData)}");
            }
            catch (System.Exception e)
            {
                Util.log.Info($"mothership-showdown.BuyVowelEventHandler.err >> " + e);
                throw;
            }
        }

        private void SolveEventHandler(bool shoutOut = false)
        {
            try
            {
                roundHandler.gameStep = GameStepType.roundStep;
                RoundStatePayload sendData = new RoundStatePayload(mHandler.CustomTarget, roundHandler.gameStep, roundHandler.Solve(shoutOut), true);

                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.Solve, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);
                SendAllAudienceStateToSatlites();
                Util.log.Info($"mothership-showdown.SolveEventHandler >> {JsonConvert.SerializeObject(sendData)}");
            }
            catch (System.Exception e)
            {
                Util.log.Info($"mothership-showdown.SolveEventHandler.err >> " + e);
                throw;
            }
        }

        private void LeaderboardStageEventHandler()
        {
            roundHandler.gameStep = GameStepType.leaderBoardStep;
            List<PlayerInfoPayload> contestants = new List<PlayerInfoPayload>();

            RoundState roundState = roundHandler.roundState;
            bool isFinal = roundState.currentRoundIndex == 3;

            List<Card> inUseCards = cards.FindAll(x => x.inUse);
            int maxCount = inUseCards.Count >= 100 ? 100 : inUseCards.Count;
            List<Card> topCards = inUseCards.GetRange(0, maxCount);
            List<PlayerInfoPayload> gamePlayers = new List<PlayerInfoPayload>();

            foreach (var card in topCards)
            {
                gamePlayers.Add(new PlayerInfoPayload(-1, players.Find(x => x.playerID == card.owner).nickname, card.score, false, card.rank, players.Find(x => x.playerID == card.owner).avatarID));
            }
            contestants = roundHandler.roundState.contestants;
            RoundResultPayLoad sendData = new RoundResultPayLoad(contestants, gamePlayers, isFinal, true);

            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.AudienceLeaderboardStage, sendData } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            SendAllAudienceStateToSatlites();
            Util.log.Info($"mothership-showdown: LeaderboardStageEventHandler >> {JsonConvert.SerializeObject(sendData)}");
        }

        private void RevealLeaderboardPointEventHandler()   //when I pressed each player buttons
        {
            List<Card> inUseCards = cards.FindAll(x => x.inUse);
            if (currentLeaderboardPointIndex < inUseCards.Count)
            {
                if (currentLeaderboardPointIndex == 10)
                {
                    SendErrorMessageToGameServer("RevealLeaderboardPointEventHandler 1");
                    return;
                }
                roundHandler.gameStep = GameStepType.leaderBoardStep;
                currentLeaderboardPointIndex++;

                int topPlayersCount = inUseCards.Count >= 10 ? 10 : inUseCards.Count;

                LeaderBoardPointPayLoad sendData = new LeaderBoardPointPayLoad(currentLeaderboardPointIndex, topPlayersCount, true);

                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.RevealAudiencePoint, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);
                Util.log.Info($"mothership-showdown: LeaderboardStageEventHandler >> {JsonConvert.SerializeObject(sendData)}");
            }
            else
            {
                SendErrorMessageToGameServer("RevealLeaderboardPointEventHandler 2");
            }
        }
        private void RevealFinalboardPointEventHandler() {
            if (currentFinalboardPointIndex < 3)
            {
                currentFinalboardPointIndex++;
                FinalBoardPointPayLoad sendData = new FinalBoardPointPayLoad(currentFinalboardPointIndex, 3, true);

                var resMsg = new Dictionary<int, object>() { { (int)NetworkId.RevealFinalPoint, sendData } };
                var str = JsonConvert.SerializeObject(resMsg);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                mHandler.SendToRoom(bytes);
                Util.log.Info($"mothership-showdown: FinalboardStageEventHandler >> {JsonConvert.SerializeObject(sendData)}");
            }
            else
            {
                SendErrorMessageToGameServer("RevealFinalboardPointEventHandler 2");
            }
        }
        private void ThankYouStageHandler()
        {
            roundHandler.gameStep = GameStepType.thankYouStep;
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.ThankYouStage, null } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            SendAllAudienceStateToSatlites();
        }

        private void EndGameEventHandler()
        {

        }

        public T LoadJson<T>(string path)
        {
            Util.log.Info($"[mothership]LoadJson >>> param >>> {path}");
            T items;
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                Util.log.Info("[mothership]LoadJson >>> end read");
                items = JsonConvert.DeserializeObject<T>(json);
            }
            return (T)Convert.ChangeType(items, typeof(T));
        }

        public void SendErrorMessageToGameServer(string err)
        {
            Util.log.Info($"mothership-showdown.SendErrorMessageToGameServer >> {err}");
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.Error, err } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
        }

        #region game logic
        public bool SetRandomContestant(int index)
        {
            bool isReSelect;
            PlayerInfo randomContestant = new PlayerInfo();
            List<PlayerInfo> availablePlayers = players.FindAll(x => !x.isContestantSelected && !x.isContestant);
            if (availablePlayers == null)
            {
                return false;
            }
            else
            {
                if (availablePlayers.Count == 0)
                {
                    return false;
                }
            }

            if (index < roundHandler.roundState.contestants.Count)
            {
                var contestant = GetContestantFormIndex(index);
                if (contestant != null)
                {
                    isReSelect = true;
                    contestant.isContestant = false;
                    contestant.revealed = false;
                    roundHandler.UpdateContestantsState(SendBalanceType.round);
                }
                else
                {
                    return false;
                }
            }
            availablePlayers = Shuffle(availablePlayers);
            Util.log.Info($"mothership-showdown.SetRandomContestant >> {JsonConvert.SerializeObject(availablePlayers)}");

            availablePlayers[0].isContestantSelected = true;
            availablePlayers[0].isContestant = true;
            availablePlayers[0].contestantIndex = index;
            roundHandler.UpdateContestantsState(SendBalanceType.round);
            return true;
        }

        public List<PlayerInfo> Shuffle(List<PlayerInfo> list)
        {
            Random rng = new Random();
            int n = list.Count;  //asas
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                PlayerInfo value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }
        private PuzzleDataPayLoad GetRandomPuzzle()
        {
            Random rng1 = new Random();
            Random rng2 = new Random();
            int categoryIndex = rng1.Next(gameData.categorys.Count);
            int puzzleIndex = rng2.Next(gameData.categorys[categoryIndex].puzzles.Count);
            return new PuzzleDataPayLoad(gameData.categorys[categoryIndex].category, gameData.categorys[categoryIndex].puzzles[puzzleIndex].puzzle);
        }
        public PlayerInfo GetContestantFormIndex(int index)
        {
            PlayerInfo contestant = players.Find(a => a.isContestant && (a.contestantIndex == index));
            Util.log.Info($"mothership-showdown.GetContestantFormIndex >>" + JsonConvert.SerializeObject(contestant));
            return contestant;
        }

        public List<PlayerInfo> GetAllContestant()
        {
            List<PlayerInfo> contestants = players.FindAll(a => a.isContestant);
            if (contestants != null)
            {
                contestants.Sort((a, b) =>
                {
                    return a.contestantIndex > b.contestantIndex ? 1 : -1;
                });
            }
            Util.log.Info($"mothership-showdown.GetAllContestant >>" + JsonConvert.SerializeObject(contestants));
            return contestants;
        }
        #endregion
        // util
        public void Download(string remoteUri)
        {
            gameDataPath = Directory.GetCurrentDirectory() + "/tepdownload/" + Path.GetFileName(remoteUri); // path where download file to be saved, with filename, here I have taken file name from supplied remote url
            Util.log.Info("[Ms_SHowdonw.Download] gameDataPath" + gameDataPath);
            using (WebClient client = new WebClient())
            {
                try
                {
                    if (!Directory.Exists("tepdownload"))
                    {
                        Directory.CreateDirectory("tepdownload");
                    }
                    Uri uri = new Uri(remoteUri);
                    //password username of your file server eg. ftp username and password
                    //client.Credentials = new NetworkCredential("username", "password");
                    //delegate method, which will be called after file download has been complete.
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(Extract);
                    //delegate method for progress notification handler.
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgessChanged);
                    // uri is the remote url where filed needs to be downloaded, and FilePath is the location where file to be saved
                    client.DownloadFileAsync(uri, gameDataPath);
                }
                catch (Exception e)
                {
                    Util.log.Info("[Ms_SHowdonw.Download] Failed download" + e);
                }
            }
        }
        private void Extract(object sender, AsyncCompletedEventArgs e)
        {
            Util.log.Info("File has been downloaded." + JsonConvert.SerializeObject(sender) + "e=>" + JsonConvert.SerializeObject(e));
            SetGameData();
        }
        private void ProgessChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Util.log.Info($"Download status: {e.ProgressPercentage}%.");
        }
        private void SetGameData()
        {
            if (gameDataPath != "")
            {
                try
                {
                    Util.log.Info("[Ms_SHowdonw.Download] setGameData: start");
                    string text = File.ReadAllText(gameDataPath);
                    gameData = JsonConvert.DeserializeObject<GameData>(text);
                    if (gameData != null)
                    {
                        cards = JsonConvert.DeserializeObject<CardFaces>(Decompress(gameData.cardFaces)).mobileCards;
                        roundHandler.CreateWheel(gameData.wedges);
                        SendWedgeDataToGameServer();
                        MakeTestPlayers();
                        Util.log.Info("[Ms_SHowdonw.Download] setGameData finished: mobile card count == " + cards.Count);
                        gameReady = true;
                    }
                    SendGameStateToOpCon();
                }
                catch (Exception e)
                {
                    Util.log.Info("[Ms_SHowdonw.Download] Failed read gameDataFile" + e);
                }
            }
        }

        public string Decompress(string input)
        {
            byte[] compressed = Convert.FromBase64String(input);
            byte[] decompressed = Decompress(compressed);
            return Encoding.UTF8.GetString(decompressed);
        }

        public byte[] Decompress(byte[] input)
        {
            using (var source = new System.IO.MemoryStream(input))
            {
                byte[] lengthBytes = new byte[4];
                source.Read(lengthBytes, 0, 4);

                var length = BitConverter.ToInt32(lengthBytes, 0);
                using (var decompressionStream = new System.IO.Compression.GZipStream(source,
                    System.IO.Compression.CompressionMode.Decompress))
                {
                    var result = new byte[length];
                    decompressionStream.Read(result, 0, length);
                    return result;
                }
            }
        }

        private void SendWedgeDataToGameServer()
        {
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.WedgeData, gameData.wedges } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            Util.log.Info("[Ms_SHowdonw.Download] Send wedge data to gameserver");
        }

        public PlayerInfo GetPlayerByID(string id)
        {
            return players.Find((x) => x.playerID == id);
        }

        public void SendMatchedAudienceToGameServer(List<string> payload)
        {
            var resMsg = new Dictionary<int, object>() { { (int)NetworkId.MatchedAudience, payload } };
            var str = JsonConvert.SerializeObject(resMsg);
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            mHandler.SendToRoom(bytes);
            Util.log.Info("[Ms_SHowdonw.Download] Send mateched audiences to gameserver: count = " + JsonConvert.SerializeObject(payload.Count));
        }
    }
}
