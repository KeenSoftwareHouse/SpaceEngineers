using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Number 5 is alive
     * 
     * Connect your suit to power with less than 1% power remaining in Survival.
     */
    public class MyAchievement_NumberFiveIsAlive : MySteamAchievementBase
    {
        public override string AchievementTag { get { return "MyAchievement_NumberFiveIsAlive"; } }
        public override bool NeedsUpdate { get { return !MySession.Static.CreativeMode; } }

        public override void SessionUpdate()
        {
            // Check for needed for dedicated server. The character is loaded later.
            if (MySession.Static.LocalCharacter == null) return;

            if (MySession.Static.LocalCharacter == null) return;

            var connectedEntity = MySession.Static.LocalCharacter.SuitBattery.ResourceSink.TemporaryConnectedEntity;
            // Below one percent and connected to something
            if (MySession.Static.LocalCharacter.SuitEnergyLevel < 0.01 
                && connectedEntity != null 
                && connectedEntity != MySession.Static.LocalCharacter)
            {
                // Connected to something -- Maybe a medbay? Or a Cockpit?

                var medBay = connectedEntity as MyMedicalRoom;
                if (medBay != null)
                {
                    if (medBay.IsWorking && medBay.RefuelAllowed)
                    {
                        NotifyAchieved();
                        return;
                    }
                }

                var cockpit = connectedEntity as MyCockpit;
                if (cockpit != null)
                {
                    if (cockpit.hasPower)
                    {
                        NotifyAchieved();
                    }
                }
            }
        }
    }
}
