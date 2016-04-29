using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using System.Collections.Generic;
using System.Text;

namespace SpaceEngineers.Game
{
    public partial class SpaceEngineersGame : MySandboxGame
    {
        static partial void SetupSecrets()
        {
            MyPerGameSettings.GA_Public_GameKey = "27bae5ba5219bcd64ddbf83113eabb30";
            MyPerGameSettings.GA_Public_SecretKey = "d04e0431f97f90fae73b9d6ea99fc9746695bd11";
            MyPerGameSettings.GA_Dev_GameKey = "3a6b6ebdc48552beba3efe173488d8ba";
            MyPerGameSettings.GA_Dev_SecretKey = "caecaaa4a91f6b2598cf8ffb931b3573f20b4343";
            MyPerGameSettings.GA_Pirate_GameKey = "41827f7c8bfed902495e0e27cb57c495";
            MyPerGameSettings.GA_Pirate_SecretKey = "493b7cb3f0a472f940c0ba0c38efbb49e902cbec";
            MyPerGameSettings.GA_Other_GameKey = "4f02769277e62b4344da70967e99a2a0";
            MyPerGameSettings.GA_Other_SecretKey = "7fa773c228ce9534181adcfebf30d18bc6807d2b";
            MyPerGameSettings.InfinarioOfficial = "8e22a788-5c4a-11e5-be0f-549f3510fedc";
            MyPerGameSettings.InfinarioDebug = "fc18bd40-55ba-11e5-8410-b083fedeed2e";
        }
    }
}
