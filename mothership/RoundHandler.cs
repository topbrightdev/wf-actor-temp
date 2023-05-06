using System;
using System.Text;
using rmb.shared;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using showdown.model;
using Newtonsoft.Json;
namespace showdown.mothership
{
    public enum SendBalanceType
    {
        round,
        bank
    }

    public class PuzzleLetter
    {
        public string letter;
        public bool isRevealed;
        public bool isVowel;
        public PuzzleLetter(string _letter)
        {
            letter = _letter;
        }
    }

    public class RoundHandler
    {
        private Showdown showdown;
        public RoundState roundState;
        public GameStepType gameStep;
        private Wheel wheel;
        private List<string> vowels = new List<string> { "A", "E", "I", "O", "U" };
        private List<PuzzleLetter> puzzleLetters = new List<PuzzleLetter>();
        private bool cruiseSelected;
        public RoundHandler(Showdown _showdown)
        {
            showdown = _showdown;
            roundState = new RoundState();
            gameStep = GameStepType.none;
            roundState.currentContestantIndex = 2;
            cruiseSelected = false;
        }
        public void CreateWheel(List<Wedge> wedges)
        {
            wheel = new Wheel(wedges);
        }

        public void InitRound(PuzzleDataPayLoad _puzzleData)
        {
            roundState.currentPointKeys.Clear();
            GoToNextContestant();
            roundState.roundStep = RoundStepType.firstTurnStep;
            roundState.puzzleData = _puzzleData;
            roundState.letterState = new LetterState(new List<string>(), false, false);
            roundState.currentRoundIndex++;
            roundState.currentWedge = null;
            puzzleLetters.Clear();
            for (int i = 0; i < _puzzleData.puzzle.Length; i++)
            {
                if (char.IsLetter(_puzzleData.puzzle[i]))
                {
                    PuzzleLetter puzzleLetter = new PuzzleLetter(_puzzleData.puzzle[i].ToString().ToUpper());
                    puzzleLetter.isVowel = CheckVowelLetter(_puzzleData.puzzle[i].ToString());
                    puzzleLetters.Add(puzzleLetter);
                }
            }
            var contestants = showdown.GetAllContestant();
            foreach (var item in contestants)
            {
                item.contestantRoundBalance = 0;
            }
            UpdateContestantsState(SendBalanceType.round);
            Util.log.Info("RoundHandler.InitRound done puzzleLetters >> " + JsonConvert.SerializeObject(puzzleLetters));
        }

        public void UpdateContestantsState(SendBalanceType sendType)
        {
            List<PlayerInfo> contestants = showdown.GetAllContestant();
            if (contestants == null)
            {
                return;
            }
            roundState.contestants.Clear();
            contestants.Sort((a, b) => b.contestantRoundBalance.CompareTo(a.contestantRoundBalance));
            foreach (var item in contestants)
            {
                if (sendType == SendBalanceType.round)
                {
                    roundState.contestants.Add(new PlayerInfoPayload(item.contestantIndex, item.nickname, item.contestantRoundBalance, item.revealed, 0, item.avatarID));
                }
                else if (sendType == SendBalanceType.bank)
                {
                    roundState.contestants.Add(new PlayerInfoPayload(item.contestantIndex, item.nickname, item.contestantBankBalance, item.revealed, 0, item.avatarID));
                }

            }
            Util.log.Info("RoundHandler.UpdateContestantsState" + JsonConvert.SerializeObject(roundState.contestants));
        }

        public void GoToNextContestant()
        {
            roundState.currentContestantIndex = (roundState.currentContestantIndex + 1) % 3;
        }

        public RoundState RotateWheel(WheelStrengthState strengthType)
        {
            try
            {
                roundState.currentPointKeys.Clear();

                List<string> matchedAudiences = new List<string>();
                Wedge wedge = wheel.PreDetermineValue(strengthType);
                roundState.currentPointKeys.Add(wedge.value);
                wedge.pts = showdown.gameData.scoreTable.Find(item => item.key == wedge.value).value;
                roundState.currentWedge = wedge;
                // wedge testing start
                /*
                string inputString = wedge.value;
                if (Int32.TryParse(inputString, out int numValue))
                {
                    if (numValue == 20 || numValue == 30 || numValue == 40 || numValue == 50)
                    {
                        roundState.currentWedge.wedgeIndex = 7;
                        roundState.currentWedge.spokeIndex = 2;
                        roundState.currentWedge.value = "Cruise";
                        roundState.currentWedge.frequency = 6;
                        roundState.currentWedge.odds = 0.8333333333333;
                    }
                }
                */
                // wedge testing end
                roundState.currentStrengthType = strengthType;
                if (wedge.value == "Lose a Turn")
                {
                    roundState.roundStep = RoundStepType.loseTurnStep;
                }
                else if (wedge.value == "Bankrupt")
                {
                    roundState.roundStep = RoundStepType.loseTurnStep;
                    SetPlayerRoundBalance(roundState.currentContestantIndex, 0);
                }
                else if (wedge.value == "Cruise")
                {
                    roundState.roundStep = RoundStepType.afterTurnStep;
                    if (cruiseSelected)
                    {
                        wedge.value = "150";
                        wedge.pts = showdown.gameData.scoreTable.Find(item => item.key == wedge.value).value;
                    }
                    else
                    {
                        cruiseSelected = true;
                    }
                }
                else
                {
                    roundState.roundStep = RoundStepType.afterTurnStep;
                }

                //update players score

                foreach (var card in showdown.cards.FindAll(x => x.inUse))
                {
                    var roundPoints = card.pointData[roundState.currentRoundIndex];
                    for (int i = 0; i < roundPoints.Count; i++)
                    {
                        if (roundPoints[i].point == wedge.value)
                        {
                            card.pointData[roundState.currentRoundIndex][i].matchCount += 1;
                            card.score += card.pointData[roundState.currentRoundIndex][i].value;
                            if (showdown.GetPlayerByID(card.owner) != null)
                            {
                                string name = showdown.GetPlayerByID(card.owner).nickname;
                                if (!matchedAudiences.Contains(name))
                                {
                                    matchedAudiences.Add(name);
                                }
                            }
                        }
                    }
                }
                showdown.SendMatchedAudienceToGameServer(matchedAudiences);
                UpdateCardsRank();
                Util.log.Info($"RoundHandler.RotateWheel:Wedge result >> {JsonConvert.SerializeObject(roundState)}");
            }
            catch (Exception ex)
            {

                Util.log.Error("RoundHandler.RotateWheel >> fail " + " => " + ex.Message + " => " + ex.StackTrace);
            }
            return roundState;
        }

        public RoundState PlayerSelect(int index)
        {
            roundState.currentContestantIndex = index;
            roundState.roundStep = RoundStepType.normalStep;
            roundState.currentWedge = null;
            return roundState;
        }

        public RoundState BuyVowel()
        {
            var player = showdown.GetContestantFormIndex(roundState.currentContestantIndex);
            if (player.contestantRoundBalance < roundState.buyBowelCost)
            {
                return null;
            }
            player.contestantRoundBalance -= roundState.buyBowelCost;
            roundState.roundStep = RoundStepType.buyVowelStep;
            UpdateContestantsState(SendBalanceType.round);
            return roundState;
        }

        public RoundState Solve(bool shoutOut = false)
        {
            roundState.currentPointKeys.Clear();
            // update cards statue
            foreach (var item in puzzleLetters)
            {
                if (!item.isRevealed)
                {
                    if (!roundState.currentPointKeys.Contains(item.letter.ToUpper()))
                    {
                        roundState.currentPointKeys.Add(item.letter.ToUpper());
                    }
                    foreach (var card in showdown.cards.FindAll(x => x.inUse))
                    {
                        var roundPoints = card.pointData[roundState.currentRoundIndex];
                        for (int i = 0; i < roundPoints.Count; i++)
                        {
                            if (roundPoints[i].point == item.letter)
                            {
                                card.pointData[roundState.currentRoundIndex][i].matchCount++;
                                card.score += card.pointData[roundState.currentRoundIndex][i].value;
                            }
                        }
                    }
                }
            }
            UpdateCardsRank();
            if (!shoutOut)
            {
                var contestants = showdown.GetAllContestant();
                foreach (var item in contestants)
                {
                    if (item.contestantIndex == roundState.currentContestantIndex)
                    {
                        item.contestantBankBalance += item.contestantRoundBalance;
                    }
                    else
                    {
                        item.contestantRoundBalance = 0;
                    }
                }
            }
            UpdateContestantsState(SendBalanceType.bank);   //total scores of contestants
            // UpdateContestantsState(SendBalanceType.round);
            roundState.roundStep = RoundStepType.end;
            return roundState;
        }

        public RoundState CallLetter(string letter)
        {
            roundState.currentPointKeys.Clear();
            roundState.currentPointKeys.Add(letter.ToUpper());
            bool buyVowelMode = roundState.roundStep == RoundStepType.buyVowelStep;
            bool isVowel = CheckVowelLetter(letter);
            if (isVowel != buyVowelMode)
            {
                return null;
            }

            int matchCount = 0;
            foreach (var item in puzzleLetters)
            {
                if (item.letter.ToUpper() == letter.ToUpper())
                {
                    if (item.isRevealed)
                    {
                        return null;
                    }
                    else
                    {
                        matchCount++;
                        item.isRevealed = true;
                    }
                }
            }

            //update audiences score
            try
            {
                List<string> matchedAudiences = new List<string>();
                foreach (var card in showdown.cards.FindAll(x => x.inUse))
                {
                    var roundPoints = card.pointData[roundState.currentRoundIndex];
                    for (int i = 0; i < roundPoints.Count; i++)
                    {
                        if (roundPoints[i].point == letter)
                        {
                            card.pointData[roundState.currentRoundIndex][i].matchCount = matchCount;
                            card.score += card.pointData[roundState.currentRoundIndex][i].value * matchCount;
                            if (showdown.GetPlayerByID(card.owner) != null)
                            {
                                string name = showdown.GetPlayerByID(card.owner).nickname;
                                if (!matchedAudiences.Contains(name))
                                {
                                    matchedAudiences.Add(name);
                                }
                            }
                        }
                    }

                }
                showdown.SendMatchedAudienceToGameServer(matchedAudiences);
                UpdateCardsRank();
                Util.log.Info($"RoundHandler.CallLetter:Score result >> {JsonConvert.SerializeObject(showdown.cards.FindAll(x => x.inUse))}");
            }
            catch (Exception ex)
            {
                Util.log.Error("RoundHandler.CallLetter >> failed load json " + " => " + ex.Message + " => " + ex.StackTrace);
            }

            //update contestants round score
            PlayerInfo currentContestant = showdown.GetContestantFormIndex(roundState.currentContestantIndex);
            try
            {
                if (!CheckVowelLetter(letter))
                {
                    int value = int.Parse(roundState.currentWedge.value);
                    currentContestant.contestantRoundBalance += value * matchCount;
                    UpdateContestantsState(SendBalanceType.round);
                }
            }
            catch (FormatException)
            {
                Util.log.Info("RoundHandler.CallLetter >> failed get integer for wedge.value");
            }
            roundState.letterState.calledLetter = letter;
            roundState.letterState.matchCount = matchCount;
            roundState.letterState.isNoneConsonant = GetRemainConsonantCount() == 0;
            roundState.letterState.isNoneVowel = GetRemainVowelCount() == 0;
            if (matchCount > 0)
            {
                roundState.roundStep = RoundStepType.normalStep;
            }
            else
            {
                roundState.roundStep = RoundStepType.loseTurnStep;
            }
            AddUsedLetter(letter);
            Util.log.Info($"RoundHandler.CallLetter:final result >> {JsonConvert.SerializeObject(roundState)}");
            return roundState;
        }

        private bool CheckVowelLetter(string letter)
        {
            bool isVowel = false;
            foreach (var item in vowels)
            {
                if (item.ToUpper() == letter.ToUpper())
                {
                    isVowel = true;
                    break;
                }
            }
            return isVowel;
        }

        private int GetRemainVowelCount()
        {
            int count = 0;
            var remainVowels = puzzleLetters.FindAll(i => i.isVowel && !i.isRevealed);
            if (remainVowels != null)
            {
                count = remainVowels.Count;
            }
            return count;
        }

        private int GetRemainConsonantCount()
        {
            int count = 0;
            var remainConsonants = puzzleLetters.FindAll(i => !i.isVowel && !i.isRevealed);
            if (remainConsonants != null)
            {
                count = remainConsonants.Count;
            }
            return count;
        }

        private void AddUsedLetter(string letter)
        {
            string bufLetter = roundState.letterState.usedLetters.Find(a => a.ToUpper() == letter.ToUpper());
            if (bufLetter == null)
            {
                roundState.letterState.usedLetters.Add(letter);
            }
        }

        private void SetPlayerRoundBalance(int index, int value)
        {
            PlayerInfo player = showdown.GetContestantFormIndex(index);
            if (player != null)
            {
                player.contestantRoundBalance = value;
            }
            UpdateContestantsState(SendBalanceType.round);
        }

        public void UpdateCardsRank()
        {
            DebugCardScore("RoundHandler.UpdateCardsRank: before statue >>  ");
            //TODO
            int preValue = 0;
            showdown.cards.Sort((a, b) =>
            {
                return a.score <= b.score ? 1 : -1;
            });
            var sortedCards = showdown.cards.FindAll(x => x.inUse);
            for (int k = 0; k < sortedCards.Count; k++)
            {
                if (k == 0)
                {
                    sortedCards[k].rank = 1;
                }
                else
                {
                    if (sortedCards[k].score == preValue)
                    {
                        sortedCards[k].rank = sortedCards[k - 1].rank;
                    }
                    else
                    {
                        sortedCards[k].rank = k + 1;
                    }
                }
                preValue = sortedCards[k].score;
            }
            DebugCardScore("RoundHandler.UpdateCardsRank: ranked statue >>  ");
        }

        private void DebugCardScore(string str)
        {
            try
            {
                string debugText = "";
                foreach (var item in showdown.cards.FindAll(x => x.inUse))
                {
                    debugText += string.Format(" ---{0}:[owner:{1}, score:{2}, rank:{3}]", item.serialNumber, item.owner, item.score, item.rank);
                }
                Util.log.Info(str + debugText);
            }
            catch (Exception e)
            {
                Util.log.Info("RoundHandler.DebugCardScore: failed >> " + e.Message + " => " + e.StackTrace);
            }

        }
    }

}
