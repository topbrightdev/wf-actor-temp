using System;

namespace showdown.model
{
    public enum NetworkId : int
    {
        GetOpConScreen = -101,

        PlayerJoined = 101,
        ControllerInfo = 102,
        CardInfo = 103,

        RequestCard = 221,

        //SetActorPeerType = 700,
        SetActorSessionID = 701,
        EndActorSession = 702,
        SetActorLateJoinMsg = 703,

        // Originated From Bigscreen (For organization we use ids 1000 - 1999 for msgs from the gameserver/big screen)
        AskQuestion = 1000,
        GetResults = 1001,
        SetActorPeerType = 1221,
        // Originated From Actor (For organization we use ids 2000 - 2999 for msgs from the actor)
        SendResults = 2000,
        SendPlayerResults = 2001,
        // Originated From Player (For organization we use ids 3000 - 3999 for msgs from the mobile app)
        SendPlayerInput = 3000,
        // ------- opcon events -----------//
        LaunchWof = 4100,
        Contestant_Draw = 4102,
        Contestant_Reveal = 4103,
        PreRoundStage = 4104,
        AudienceGameIntroStage = 4105,
        RevealPuzzle = 4106,
        SpinWheel = 4107,
        BuyVowel = 4108,
        Solve = 4109,
        CallLetter = 4110,
        Player_Select = 4111,
        AudienceLeaderboardStage = 4112,
        RevealAudiencePoint = 4113,
        ThankYouStage = 4114,
        EndGame = 4115,
        Back = 4116,
        ShoutOut = 4117,
        RevealFinalPoint = 4118, 

        // ------- gameserver events ----- // 
        //CardDataBase = 4300,
        GameData = 4301,
        WedgeEvent = 4302,
        CallLetterEvent = 4303,
        GetRecentJoinedPlayers = 4304,
        WheelRotationCompleted = 4306,
        ContestantRevealFinished = 4308,
        CallLetterFinished = 4309,

        // ------- actor events ---------- //
        // to gameserver
        GameReady = 4200,
        RecentJoinedPlayers = 4201,
        UpdateContestantInfo = 4202,
        WedgeData = 4203,
        MatchedAudience = 4204,
        // to actor
        LetterState = 4250,

        // --------- controller events ------ //
        //from controller
        ControllerReadyRequestingQuestionInfo = 4400,

        // to controller
        AudienceState = 4401,

        //
        Error = 4999
    }


}