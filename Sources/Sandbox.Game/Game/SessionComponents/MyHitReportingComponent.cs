using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.SessionComponents
{
    /// <summary>
    /// Handles client-side reactions to hits (change in crosshair color, hit sounds, etc...).
    /// Also handles sending the hit messages on the server so that clients can react on them.
    /// 
    /// CH: TODO: The logic of how the game will react to the hits should be defined somewhere and not hardcoded
    /// But please think about this before doint it and don't put it into MyPerGameSettings, as it's already full of terrible stuff!
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    [StaticEventOwner]
    public class MyHitReportingComponent : MySessionComponentBase
    {
        private int m_crosshairCtr = 0;

        private static MyHitReportingComponent m_static = null;
        private const int TIMEOUT = 500;
        private const int BLEND_TIME = 500;
        private static Color HIT_COLOR = Color.White;

        private static MyStringId m_hitReportingSprite;

        static MyHitReportingComponent()
        {
            m_hitReportingSprite = MyStringId.GetOrCompute("HitReporting");
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return MyPerGameSettings.Game == GameEnum.ME_GAME || MyPerGameSettings.Game == GameEnum.SE_GAME || MyPerGameSettings.Game == GameEnum.VRS_GAME;
            }
        }

        public override Type[] Dependencies
        {
            get
            {
                return new Type[] { typeof(MyDamageSystem) };
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            m_static = this;

            if (Sync.IsServer)
            {
                MyDamageSystem.Static.RegisterAfterDamageHandler(1000, AfterDamageApplied);
            }
        }

        protected override void UnloadData()
        {
            m_static = null;

            base.UnloadData();
        }

        private void AfterDamageApplied(object target, MyDamageInformation info)
        {
            MyCharacter targetCharacter = target as MyCharacter;
            if (targetCharacter == null || targetCharacter.IsDead) return;

            MyEntity entity = null;
            MyEntities.TryGetEntityById(info.AttackerId, out entity);

            MyPlayer attackerPlayer = null;
            MyStringHash hitCue = MyStringHash.NullOrEmpty;

            // Because damage system is retarded...
            if (entity is MyCharacter || entity is MyCubeGrid || entity is MyCubeBlock)
            {
                attackerPlayer = Sync.Players.GetControllingPlayer(entity);
                if (attackerPlayer == null) return;
            }
            else
            {
                var gunBaseUser = entity as IMyGunBaseUser;
                if (gunBaseUser == null) return;
                if (gunBaseUser.Owner == null) return;
                attackerPlayer = Sync.Players.GetControllingPlayer(gunBaseUser.Owner);

                if (MyPerGameSettings.Game == GameEnum.ME_GAME)
                {
                    //hitCue = MyStringHash.GetOrCompute("ToolCrossbHitBody");//this causes to play the hit sound at full volume regardless of distance
                }
            }

            if (attackerPlayer == null || attackerPlayer.Client == null || attackerPlayer.IsBot) return;
            if (targetCharacter.ControllerInfo.Controller != null && targetCharacter.ControllerInfo.Controller.Player == attackerPlayer) return;

            if (MyPerGameSettings.Game == GameEnum.ME_GAME)
            {
                MyMultiplayer.RaiseStaticEvent(s => AfterDamageAppliedClient, hitCue, new EndpointId(attackerPlayer.Client.SteamUserId));
            }
            else if (MyPerGameSettings.Game == GameEnum.SE_GAME || MyPerGameSettings.Game == GameEnum.VRS_GAME)
            {
                MyMultiplayer.RaiseStaticEvent(s => AfterDamageAppliedClient, hitCue, new EndpointId(attackerPlayer.Client.SteamUserId));
            }
        }

        [Event, Client]
        private static void AfterDamageAppliedClient(MyStringHash cueId)
        {
            MyHud.Crosshair.AddTemporarySprite(VRage.Game.Gui.MyHudTexturesEnum.hit_confirmation, m_hitReportingSprite, TIMEOUT, BLEND_TIME, HIT_COLOR);
            if (cueId != MyStringHash.NullOrEmpty) MyAudio.Static.PlaySound(new MyCueId(cueId));
        }
    }
}
