using System;
using rmb.shared;

namespace showdown.controller
{
    public class Config
    {
        /// <summary>
        /// Version of this application.
        /// TODO: game developer should update this for each actor that's uploaded.
        /// </summary>
        public const string SourceVersion = "0.0.0-LOCALHOST";

        // /// <summary>
        // /// URL for actor to obtain credentials.
        // /// </summary>
        // public const string AuthUrl = "https://stg-auth.timeplay.com/auth/realms/Users/protocol/openid-connect/token";

        // /// <summary>
        // /// Client ID for this game's actor.
        // /// </summary>
        // public const string ClientID = "";

        // /// <summary>
        // /// Client secret for this game's actor. This is unique and should not be shared with anyone.
        // /// </summary>
        // public const string ClientSecret = "";

        // /// <summary>
        // /// URL of the Entity Manager.
        // /// </summary>
        // public const string EntityUrl = "https://sagames-dev-api.streamsix.com/entity-manager";
        // public const string LeaderboardUrl = "https://sagames-dev-api.streamsix.com/leaderboards";

        public static void Setup(Type eType, string[] args, string source)
        {
            Util.Setup(eType, args, source, SourceVersion);
        }
    }
}
