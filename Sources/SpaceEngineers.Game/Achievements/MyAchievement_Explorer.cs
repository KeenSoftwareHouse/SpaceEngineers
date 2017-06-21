using System.Collections;
using System.Collections.Generic;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Explorer
     * 
     * Visit all planets and moons in Survival mode.
     */
    public class MyAchievement_Explorer : MySteamAchievementBase
    {
        // Can be changed according the number of planets and intervals needed.
        private const uint CHECK_INTERVAL_S = 3;
        private const uint PLANET_COUNT = 6;

        // Stat data
        public const string     StatNameTag     = "Explorer_ExplorePlanetsData";
        public override string  AchievementTag  { get { return "MyAchievement_Explorer"; } }
        // Update flag
        public override bool NeedsUpdate { get { return m_globalConditionsMet; } }

        // explored planet data storage
        private BitArray        m_exploredPlanetData;
        private readonly int[]  m_bitArrayConversionArray = new int[1];

        // Check timer data
        private uint            m_lastCheckS;
        // Planet index lookup dictionary
        private readonly Dictionary<MyStringHash, int> m_planetNamesToIndexes = new Dictionary<MyStringHash, int>(); 
        // Achievement global conditions met flag
        private bool            m_globalConditionsMet;

        public override void Init()
        {
            base.Init();

            if(IsAchieved) return;

            ReadSteamData();

            // Create the plance lookup data
            m_planetNamesToIndexes.Add(MyStringHash.GetOrCompute("Alien"),      0);
            m_planetNamesToIndexes.Add(MyStringHash.GetOrCompute("EarthLike"),  1);
            m_planetNamesToIndexes.Add(MyStringHash.GetOrCompute("Europa"),     2);
            m_planetNamesToIndexes.Add(MyStringHash.GetOrCompute("Mars"),       3);
            m_planetNamesToIndexes.Add(MyStringHash.GetOrCompute("Moon"),       4);
            m_planetNamesToIndexes.Add(MyStringHash.GetOrCompute("Titan"),      5);
        }

        public override void SessionLoad()
        {
            // Cannot be achived in creative mode.
            m_globalConditionsMet = !MySession.Static.CreativeMode;
            m_lastCheckS = 0;
        }

        public override void SessionUpdate()
        {
            // Check for needed for dedicated server. The character is loaded later.
            if(MySession.Static.LocalCharacter == null) return;

            var playTimeS = (uint)MySession.Static.ElapsedPlayTime.TotalSeconds;
            var elapsedTimeS = playTimeS - m_lastCheckS;
            // Check interval.
            if (elapsedTimeS > CHECK_INTERVAL_S)
            {
                if (MySession.Static.LocalCharacter == null) return;
                
                var characterPosition = MySession.Static.LocalCharacter.PositionComp.GetPosition();
                var gravityInCharactersPosition = MyGravityProviderSystem.CalculateNaturalGravityInPoint(characterPosition);

                m_lastCheckS = playTimeS;

                // Is not in gravity of any planet.
                if(gravityInCharactersPosition == Vector3.Zero)
                    return;

                var planet = MyGamePruningStructure.GetClosestPlanet(characterPosition);
                if (planet != null)
                {
                    int index;
                    if (m_planetNamesToIndexes.TryGetValue(planet.Generator.Id.SubtypeId, out index))
                    {
                        // Supported vanilla planet.
                        if (!m_exploredPlanetData[index])
                        {
                            // Was not discovered already.
                            m_exploredPlanetData[index] = true;

                            // Count amount of discovered planets from bit array.
                            uint discoveredCount = 0;
                            for (int i = 0; i < PLANET_COUNT; i++)
                            {
                                if(m_exploredPlanetData[i])
                                    discoveredCount++;
                            }

                            // Store the new exploration data.
                            StoreSteamData();

                            // Final condition.
                            if(discoveredCount < PLANET_COUNT)
                            {
                                // Update players progress.
                                MySteam.API.IndicateAchievementProgress(AchievementTag, discoveredCount, PLANET_COUNT);
                            }
                            else
                            {
                                // Done!
                                NotifyAchieved();
                            }
                        }
                    }
                }
            }
        }

        // Create a bit array from steam side stored int value.
        private void ReadSteamData()
        {
            var data = MySteam.API.GetStatInt(StatNameTag);
            m_exploredPlanetData = new BitArray(new[] { data });
        }

        // Convert the bit array back to int and send it to steam proxy.
        private void StoreSteamData()
        {
            m_exploredPlanetData.CopyTo(m_bitArrayConversionArray, 0);
            MySteam.API.SetStat(StatNameTag, m_bitArrayConversionArray[0]);
        }
     }
}
