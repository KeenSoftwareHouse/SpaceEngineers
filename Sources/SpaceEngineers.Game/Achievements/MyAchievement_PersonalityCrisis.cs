using System;
using Sandbox.Game.Screens;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRageMath;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Personality Crisis
     * 
     * Change astronaut style (color or skin) 20 times in 10 minutes.
     */
    public class MyAchievement_PersonalityCrisis : MySteamAchievementBase
    {
        public override string AchievementTag { get { return "MyAchievement_PersonalityCrisis"; } }
        // This achievement does not need updates when the player haven't changed the look yet.
        public override bool NeedsUpdate { get { return m_startS != UInt32.MaxValue; } }

        private uint m_startS;
        private uint m_changeCounter;

        public override void SessionLoad()
        {
            m_startS = UInt32.MaxValue;
            MyGuiScreenWardrobe.LookChanged += MyGuiScreenWardrobeOnLookChanged;
        }

        private void MyGuiScreenWardrobeOnLookChanged(string prevModel, Vector3 prevColorMask, string newModel, Vector3 newColorMask)
        {
            // Aditional check for model change.
            if (prevModel != newModel || prevColorMask != newColorMask)
            {
                // Activation condition.
                if (m_startS == UInt32.MaxValue)
                {
                    m_startS = (uint)MySession.Static.ElapsedPlayTime.TotalSeconds;
                    // Counter reset.
                    m_changeCounter = 0;
                }

                m_changeCounter++;

                // Final condition
                if (m_changeCounter == 20)
                {
                    // Unregister and trigger achived.
                    MyGuiScreenWardrobe.LookChanged -= MyGuiScreenWardrobeOnLookChanged;
                    NotifyAchieved();
                }
            }
        }

        public override void SessionUpdate()
        {
            uint playTimeInS = (uint)MySession.Static.ElapsedPlayTime.TotalSeconds;
            uint elapsedTimeS = playTimeInS - m_startS;

            // Fail condition 10mins
            if (elapsedTimeS > 600)
            {
                // Resets the system and stops updates
                m_startS = UInt32.MaxValue;
            }
        }
    }
}
