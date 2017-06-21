using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Entity;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: I've Got Present For You
     * 
     * Detonate a warhead that kills you and another player.
     */
    class MyAchievement_IHaveGotPresentForYou : MySteamAchievementBase
    {

        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievement_IHaveGotPresentForYou"; } }
        public override bool NeedsUpdate { get { return false; } }

        private bool m_someoneIsDead = false;
        private bool m_imDead = false;
        private long m_lastWarheadGrid;//Last warhead I detonated
        private long m_lastAttackerID;

        public override void Init()
        {
            base.Init();

            if (IsAchieved) return;

            MyCharacter.OnCharacterDied += MyCharacter_OnCharacterDied;
            MyWarhead.OnWarheadDetonatedClient += MyWarhead_OnWarheadDetonatedClient;
        }

        private void MyWarhead_OnWarheadDetonatedClient(MyWarhead obj)
        {
            //Get last by you detonated warhead
            m_lastWarheadGrid = obj.CubeGrid.EntityId;
        }

        private void MyCharacter_OnCharacterDied(MyCharacter character)
        {
            if (character.StatComp.LastDamage.Type != MyDamageType.Explosion) //Not an explosion
                return;

            //detect new explosion
            long lastAttackerID = character.StatComp.LastDamage.AttackerId;
            if (lastAttackerID != m_lastAttackerID)
            {
                m_someoneIsDead = false;
                m_imDead = false;
                m_lastAttackerID = lastAttackerID;
            }

            if (character.GetPlayerIdentityId() == MySession.Static.LocalHumanPlayer.Identity.IdentityId) //I died
            {
                m_imDead = true;
            }
            else if (character.IsPlayer)//someone died
            {
                m_someoneIsDead = true;
            }

            if (m_imDead &&// Im already dead
                m_someoneIsDead && //someone died also
                m_lastAttackerID == lastAttackerID &&//and we died by one explosion
                m_lastWarheadGrid == m_lastAttackerID)//and it is MY warhead what cause it
            {
                NotifyAchieved();
                MyCharacter.OnCharacterDied -= MyCharacter_OnCharacterDied;
                MyWarhead.OnWarheadDetonatedClient -= MyWarhead_OnWarheadDetonatedClient;
            }
        }


    }
}
