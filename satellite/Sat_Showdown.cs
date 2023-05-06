using System;
using System.Text;
using System.Linq;
using rmb.shared;
using System.Collections.Generic;
using showdown.model;
using Newtonsoft.Json;

namespace showdown.satellite
{
    public class AudienceOwner
    {
        public string owner { get; set; }
    }
    public class Showdown
    {
        // @Francois please explain what this is
        private const string mBucketName = "showdown_super_bucket";

        private rmb.satellite.Handler mHandler;
        private Dictionary<string, PlayerData> mChoices = new Dictionary<string, PlayerData>();
        private Question mSavedQuestion = new Question();
        private float mMaxTime = 15.0f;
        private int mMaxScore = 100;

        //testing
        private List<PlayerInfo> testPlayers = new List<PlayerInfo>();

        public Showdown(rmb.satellite.Handler handler)
        {
            this.mHandler = handler;
        }

        public bool ProcessMessage(byte[] payload, string str)
        {
            try
            {
                Util.log.Info($"[Showdown.satellite] ProcessMessage: {str}");
                //Note: You can send w.e object you want & deserialize it to w.e object you want. 
                //      However in our learnings a Dictionary<int,object> has been the best to work with
                var data = JsonConvert.DeserializeObject<Dictionary<int, object>>(str);
                foreach (var msg in data)
                {
                    string value = JsonConvert.SerializeObject(msg.Value);
                    switch (msg.Key)
                    {
                        case (int)NetworkId.PlayerJoined:
                            {
                                PlayerJoined(value, payload);
                                break;
                            }
                        case (int)NetworkId.ControllerReadyRequestingQuestionInfo:
                            {
                                PlayerJoined(value, payload);
                                break;
                            }
                        case (int)NetworkId.AudienceState:
                            {
                                AudienceOwner audienceOwner = JsonConvert.DeserializeObject<AudienceOwner>(value);
                                if (audienceOwner != null)
                                {
                                    mHandler.SendToController(payload, audienceOwner.owner);
                                    Util.log.Info($"[Showdown.satellite] Send to Controller playerID = {audienceOwner.owner}");
                                }
                                break;
                            }
                        default:
                            {
                                Util.log.Info($"[Showdown.satellite] Sat received invalid message with id[{msg.Key}] and value: {value}");
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Util.log.Error("[Showdown.satellite] Bad message:" + str + " => " + ex.Message + " => " + ex.StackTrace);
            }
            return true;
        }

        private void PlayerJoined(string value, byte[] payload)
        {
            //send the compiled data to the mothership code of Showdown.
            mHandler.SendToMothership(payload);
            Util.log.Info($"[Showdown.satellite] Satellite has send to joniendPlayer to mothership");
        }

        private void NewQuestion(string value, byte[] payload)
        {
            //clear any previous choices stored.
            mChoices.Clear();
            //use the helper method to deserialize the payload into a question object.
            mSavedQuestion = JsonConvert.DeserializeObject<Question>(value);
            Util.log.Info($"Received question {mSavedQuestion.id} => {mSavedQuestion.text} => {mSavedQuestion.left} => {mSavedQuestion.right} ");
            //send the question to the players unchanged.
            mHandler.SendToControllers(payload, null);
        }

        private void GetResults(string value)
        {
            if (int.TryParse(value, out int id) && id == mSavedQuestion.id)
            {
                //request all players currently playing, will be used later 
                mHandler.RequestAllPlayers();

                //compile the responses received by players connected to this satellite.
                int[] payload = new int[2];
                foreach (var kvp in mChoices)
                {
                    payload[0] += kvp.Value.selection[0];
                    payload[1] += kvp.Value.selection[1];
                }

                var response = new Dictionary<int, object>() { { (int)NetworkId.SendResults, payload } };
                var str = JsonConvert.SerializeObject(response);
                var bytes = UTF8Encoding.UTF8.GetBytes(str);
                //send the compiled data to the mothership code of Showdown.
                mHandler.SendToMothership(bytes);
                Util.log.Info($"[Showdown.satellite] Satellite has responded to GetResults[{(int)NetworkId.GetResults}] with value: {str}");

            }
            else
            {
                Util.log.Info(string.Format("[Showdown.satellite] Received compile request mismatch {0} != {1}", id, mSavedQuestion.id));
            }
        }

        private void ProcessPlayerInput(string value)
        {
            Util.log.Info($"[Showdown.satellite] Processing player's input {value}");

            //Processing player's input
            // Note: if we wanted we could deserialize as Dictionary<string,object> to allow each element to be of any type but also to accomodate future extension without compromising backward compatibility.
            var answer = JsonConvert.DeserializeObject<PlayerAnswer>(value);
            var playerID = answer.playerID;
            var questionID = answer.questionID;
            //first check if the player is answering to the correct question
            if (questionID == mSavedQuestion.id)
            {
                //the first element of the array should be a string representing the playerID
                Util.log.Info($"[Showdown.satellite] Player {playerID}'s responding to question {questionID}");
                //check if the player has already answered this question.
                if (!mChoices.ContainsKey(playerID))
                {
                    //next in the array, the player selection 1 or 0 fon the left(index 2) or right (index 3) choice
                    PlayerData data;
                    int left = answer.choice == 0 ? 1 : 0;
                    int right = answer.choice == 1 ? 1 : 0;
                    data.selection = new int[] { left, right };

                    // finally we read how long the player wait to make the above choice from the time he received the question.
                    data.elapsed = answer.time;
                    mChoices.Add(playerID, data);
                    Util.log.Info($"[Showdown.satellite] Stored response from player[{playerID}] with answer:{value}. Current choices {data.selection[0]} # {data.selection[1]} in {data.elapsed} ms");
                }
                else
                {
                    Util.log.Info("[Showdown.satellite] Already received answer for player " + playerID);
                }
            }
            else
            {
                Util.log.Info(string.Format("[Showdown.satellite] player {0}'s response question id mismatch {1} # {2}", playerID, questionID, mSavedQuestion.id));
            }
        }

        private void SendRoundResults(string value)
        {
            Util.log.Info($"[Showdown.satellite] SendRoundResults with value: {value}");
            //parsed the compiled results received from the showdown mothership code as an array of int
            int[] results = JsonConvert.DeserializeObject<int[]>(value);
            if (results.Length >= 3)
            {
                if (mSavedQuestion.id == results[0])
                {
                    // Send Results to controllers
                    var compiledMsg = new Dictionary<int, object>() { { (int)NetworkId.SendResults, results } };
                    var str = JsonConvert.SerializeObject(compiledMsg);
                    var bytes = UTF8Encoding.UTF8.GetBytes(str);
                    mHandler.SendToControllers(bytes, null);
                    Util.log.Info($"[Showdown.satellite] Showdown sending results to all players: {str}");
                    Util.log.Info($"[Showdown.satellite] compare results  left[{results[1]}] and right[{results[2]}]");

                    bool tie = results[1] == results[2];
                    bool leftWins = results[1] > results[2];
                    bool rightWins = results[1] < results[2];

                    //mHandler.Players is populated with the response of the mHandler.RequestAllPlayers() call
                    if (mHandler.Players != null)
                    {
                        //Now run over all the players and check who answered and if they are part of the majority
                        Util.log.Info($"[Showdown.satellite] Update mHandler.Players score: {JsonConvert.SerializeObject(mHandler.Players)}");
                        foreach (var playerID in mHandler.Players)
                        {
                            Util.log.Info(string.Format("[Showdown.satellite] Assigning score to player {0} ", playerID));
                            int score = 0;
                            var val = mChoices[playerID];
                            bool selectedLeft = val.selection[0] > val.selection[1];
                            if (tie || (leftWins && selectedLeft) || (rightWins && !selectedLeft))
                            {
                                //very simple formula to assign a score based on how quick the player answered
                                float fraction = val.elapsed / mMaxTime;
                                fraction = 1.0f - fraction;
                                if (fraction >= 1)
                                {
                                    //super fast, wasted no time, assign maxscore
                                    score = mMaxScore;
                                }
                                else if (fraction <= 0)
                                {
                                    //give consolotation score if the player wasn't part of the majority.
                                    score = 1;
                                }
                                else
                                {
                                    //assign a proportional score.
                                    score = (int)Math.Ceiling(fraction * mMaxScore);
                                }
                            }

                            //format the action as a string for reference in reports and/or logs
                            string actions = string.Format("{0} - {1} in {2} ms", val.selection[0], val.selection[1], val.elapsed);
                            mHandler.PlayerScored(playerID, score, actions);
                            mHandler.PlayerScored(playerID, -score, actions, mBucketName);

                            // Send updated score to this player
                            var playerMsg = new Dictionary<int, object>() { { (int)NetworkId.SendPlayerResults, score } };
                            var playerStr = JsonConvert.SerializeObject(playerMsg);
                            var playerBytes = UTF8Encoding.UTF8.GetBytes(playerStr);
                            mHandler.SendToController(playerBytes, playerID);
                            Util.log.Info($"[Showdown.satellite] Player {playerID} was got a score {score}, sent data: {playerStr}");
                        }
                    }
                    else
                    {
                        Util.log.Info("[Showdown.satellite] mHandler.Players == null");
                    }
                    mHandler.UpdateBucketCache();
                    mHandler.UpdateBucketCache(mBucketName);
                }
                else
                {
                    Util.log.Info(string.Format("[Showdown.satellite] Compiled questionID mismatch: {0} != {1}", mSavedQuestion.id, results[0]));
                }

                Util.log.Info($"[Showdown.satellite] Showdown finished sending results to all players");
            }
            else
            {
                Util.log.Info("[Showdown.satellite] Not enough data in compiled message");
            }
        }
    }
}
