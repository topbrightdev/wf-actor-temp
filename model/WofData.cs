using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;

namespace showdown.model
{
    public class GameData
    {
        public List<Wedge> wedges { get; set; }
        public List<Category> categorys { get; set; }
        public string cardFaces { get; set; }
        public List<ScoreElement> scoreTable { get; set; }
    }
    public class WebPlayerPayload
    {
        public string _deviceId { get; set; }
        public int _avatarId { get; set; }
        public string _nickname { get; set; }
        public WebPlayerPayload(string deviceId, string nickName, int avatarId)
        {
            _deviceId = deviceId;
            _nickname = nickName;
            _avatarId = avatarId;
        }

    }
    public class PlayerInfo
    {
        public string playerID;
        public string nickname;
        public int avatarID;
        public List<string> cards = new List<string>();

        public bool isContestantSelected; //for redraw contestant
        public bool isContestant;
        public bool revealed;
        public int contestantIndex, contestantRoundBalance, contestantBankBalance;

        public PlayerInfo(string _playerID = "", string _nickname = "", int _avatarID = 0)
        {
            playerID = _playerID;
            nickname = _nickname;
            avatarID = _avatarID;

            isContestantSelected = false;
            isContestant = false;
            contestantIndex = -1;
            contestantRoundBalance = 0;
            contestantBankBalance = 0;
        }
    }

    public class PlayerInfoPayload
    {
        public int contestantIndex;
        public string name;
        public int balance;
        public bool revealed;
        public int cardRank;
        public int avatarID;
        public PlayerInfoPayload(int _contestantIndex, string _name, int _balance, bool _revealed, int _cardRank, int avatarID)
        {
            contestantIndex = _contestantIndex;
            name = _name;
            balance = _balance;
            revealed = _revealed;
            cardRank = _cardRank;
            this.avatarID = avatarID;
        }
    }

    public class IntPayload
    {
        public int num;
    }

    public class StringPayload
    {
        public string str;
    }

    public class RoundResultPayLoad
    {
        public bool processResult;
        public bool finalLeaderboard;
        public List<PlayerInfoPayload> topPlayers;
        public List<PlayerInfoPayload> contestants;
        public RoundResultPayLoad(List<PlayerInfoPayload> _contestants, List<PlayerInfoPayload> _topPlayers, bool _finalLeaderboard, bool _processResult)
        {
            contestants = _contestants;
            topPlayers = _topPlayers;
            processResult = _processResult;
            finalLeaderboard = _finalLeaderboard;
        }
    }

    public class LeaderBoardPointPayLoad
    {
        public bool processResult;
        public int pointIndex;
        public int topPlayersCount;
        public LeaderBoardPointPayLoad(int _pointIndex, int _topPlayersCount, bool _processResult)
        {
            processResult = _processResult;
            pointIndex = _pointIndex;
            topPlayersCount = _topPlayersCount;
        }
    }
    public class FinalBoardPointPayLoad
    {
        public bool processResult;
        public int pointIndex;
        public int topPlayersCount;
        public FinalBoardPointPayLoad(int _pointIndex, int _topPlayersCount, bool _processResult)
        {
            processResult = _processResult;
            pointIndex = _pointIndex;
            topPlayersCount = _topPlayersCount;
        }
    }

    #region ScoreTable
    public class ScoreElement
    {
        public string key;
        public int value;
    }

    #endregion

    #region AudienceCard
    public class CardFaces
    {
        public List<Card> mobileCards { get; set; }
        public List<Card> physicalCards { get; set; }
    }

    public class Card
    {
        [JsonProperty(PropertyName = "s")]
        public string serialNumber { get; set; }
        [JsonProperty(PropertyName = "p")]
        public List<List<string>> points { get; set; }
        public List<List<Point>> pointData { get; set; } = new List<List<Point>>();
        public bool inUse { get; set; }
        public string owner { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
    }

    public class Point
    {
        public string point { get; set; }
        public int value { get; set; }
        public int matchCount { get; set; }
        public Point(string point, int value, int matchCount)
        {
            this.point = point;
            this.value = value;
            this.matchCount = matchCount;
        }
    }
    #endregion

    #region PuzzleData
    public class Category
    {
        public string category { get; set; }
        public List<Puzzle> puzzles { get; set; }
    }

    public class Puzzle
    {
        //public int id { get; set; }
        public string puzzle { get; set; }
        public int id { get; set; }

    }
    public class PuzzleDataPayLoad
    {
        public string category;
        public string puzzle;
        public PuzzleDataPayLoad(string _category = "", string _puzzle = "")
        {
            category = _category;
            puzzle = _puzzle;
        }
    }
    #endregion

    #region roundState
    public enum GameStepType
    {
        none = 0,
        welcomeStep = 1,
        contestantDrawing = 2,
        contestantRevealed = 3,
        roundStep = 4,
        leaderBoardStep = 5,
        thankYouStep = 6,
        endGameStep = 7
    }
    public enum RoundStepType
    {
        none = 0,
        firstTurnStep = 1,
        normalStep = 2,
        callConsonantStep = 3,
        callVowelStep = 4,
        solveStep = 5,
        afterTurnStep = 6,
        onlyVowelsRemainStep = 7,
        loseTurnStep = 8,
        buyVowelStep = 9,
        end = 10
    }
    public class RoundState
    {
        public int buyBowelCost;

        public RoundStepType roundStep;

        public int currentRoundIndex;
        public int currentContestantIndex;
        public List<string> currentPointKeys;

        public Wedge currentWedge;
        public WheelStrengthState currentStrengthType;

        public PuzzleDataPayLoad puzzleData = new PuzzleDataPayLoad();
        public List<PlayerInfoPayload> contestants = new List<PlayerInfoPayload>();
        public LetterState letterState = new LetterState(new List<string>(), false, false);
        public RoundState()
        {
            currentRoundIndex = -1;
            currentContestantIndex = 0;
            roundStep = RoundStepType.none;
            buyBowelCost = 20;
            currentPointKeys = new List<string>();
        }
    }
    public class RoundStatePayload
    {
        public byte actorPeerID;
        public GameStepType gameStep;
        public RoundState roundState = new RoundState();
        public bool processResult;
        public RoundStatePayload(byte _actorPeerID, GameStepType _gameStep, RoundState _roundState, bool _processResult)
        {
            actorPeerID = _actorPeerID;
            gameStep = _gameStep;
            roundState = _roundState;
            processResult = _processResult;
        }
    }
    public class LetterState
    {
        public string calledLetter;
        public int matchCount;
        public List<string> usedLetters = new List<string>();
        public bool isNoneVowel;
        public bool isNoneConsonant;
        public LetterState(List<string> _usedLetters, bool _isNoneVowel, bool _isNoneConsonant)
        {
            usedLetters = _usedLetters;
            isNoneConsonant = _isNoneConsonant;
            isNoneVowel = _isNoneVowel;
            calledLetter = "";
            matchCount = 0;
        }
    }
    #endregion

    #region  wheel
    public class Wedge
    {
        public int wedgeIndex;
        public int spokeIndex;
        public string value;
        public int frequency;
        public double odds;
        public int pts;
        public Wedge(int _wedgeIndex = 1, int _spokeIndex = 1, string _value = "Lose a Turn", int _frequency = 1, double _odds = 0.0138888888888889)
        {
            wedgeIndex = _wedgeIndex;
            spokeIndex = _spokeIndex;
            value = _value;
            frequency = _frequency;
            odds = _odds;
        }
    }
    public enum WheelStrengthState
    {
        Weak = 0,
        Regular = 1,
        Strong = 2,
    }
    #endregion

    #region controller playload
    public class AudienceState
    {
        public string owner { get; set; }
        public List<Card> cards { get; set; }
        public GameStepType gameStep { get; set; }
        public List<string> revealedContestants { get; set; }
        public int currentContestantIndex { get; set; }
        public int currentRoundIndex { get; set; }
        public List<string> currentPointKeys { get; set; }
        public AudienceState(string owner, List<Card> cards, GameStepType gameStep, List<string> revealedContestants, int currentContestantIndex, int currentRoundIndex, List<string> currentPointKeys)
        {
            this.owner = owner;
            this.cards = cards;
            this.gameStep = gameStep;
            this.revealedContestants = revealedContestants;
            this.currentContestantIndex = currentContestantIndex;
            this.currentRoundIndex = currentRoundIndex;
            this.currentPointKeys = currentPointKeys;
        }
    }
    #endregion
}
