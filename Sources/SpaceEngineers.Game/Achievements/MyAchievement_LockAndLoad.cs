using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
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
     * Achievement: Lock And Load
     * 
     * Kill your first enemy.
     */
    class MyAchievement_LockAndLoad : MySteamAchievementBase
    {
        public override string AchievementTag { get { return "MyAchievement_LockAndLoad"; } }
        public override bool NeedsUpdate { get { return false; } }

        public override void Init()
        {
            base.Init();

            if (IsAchieved) return;

            MyCharacter.OnCharacterDied += MyCharacter_OnCharacterDied;
        }

        void MyCharacter_OnCharacterDied(MyCharacter character)
        {
            MyEntity attacker;
            MyEntities.TryGetEntityById(character.StatComp.LastDamage.AttackerId, out attacker);

            if (character.GetPlayerIdentityId() != MySession.Static.LocalHumanPlayer.Identity.IdentityId && //filter my dead
                character.StatComp.LastDamage.Type == MyDamageType.Bullet && //filter dead by bullet
                (attacker is MyAutomaticRifleGun)&& //attacking by riflegun
                (attacker as MyAutomaticRifleGun).Owner.GetPlayerIdentityId() == MySession.Static.LocalHumanPlayer.Identity.IdentityId //Im attacker
                )
            {
                NotifyAchieved();
                MyCharacter.OnCharacterDied -= MyCharacter_OnCharacterDied;
            }
        }

    }
}
