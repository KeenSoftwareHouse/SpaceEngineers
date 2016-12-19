#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;

using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Common;
using Sandbox.Graphics;
using Sandbox.Definitions;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.ObjectBuilders;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI.Weapons;
using VRage.Audio;

#endregion

namespace Sandbox.Game.Weapons
{
    public abstract class MyBlockPlacerBase : MyEngineerToolBase, IMyBlockPlacerBase
    {
        public static MyHudNotificationBase MissingComponentNotification =
             new MyHudNotification(MyCommonTexts.NotificationMissingComponentToPlaceBlockFormat, font: MyFontEnum.Red, priority: 1);

        protected abstract MyBlockBuilderBase BlockBuilder { get; }

        protected int m_lastKeyPress;
        protected bool m_firstShot;
        protected bool m_closeAfterBuild;
        MyHandItemDefinition m_definition;

        protected MyBlockPlacerBase(MyHandItemDefinition definition)
            : base(500)
        {
            m_definition = definition;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder, m_definition.PhysicalItemId);

            Init(null, null, null, null, null);
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;

            HasSecondaryEffect = false;
            HasPrimaryEffect = false;
            m_firstShot = true;
            if (PhysicalObject!=null)
                PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
        }

        public override bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            bool retval = base.CanShoot(action, shooter, out status);

            // No need for cooldown for the first shot
            if (status == MyGunStatusEnum.Cooldown && action == MyShootActionEnum.PrimaryAction && m_firstShot == true)
            {
                status = MyGunStatusEnum.OK;
                retval = true;
            }

            return retval;
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            if (MySession.Static.CreativeMode)
                return;

            m_closeAfterBuild = false;

            base.Shoot(action, direction, null, gunAction);
            ShakeAmount = 0.0f;

            if (action == MyShootActionEnum.PrimaryAction && m_firstShot)
            {
                m_firstShot = false;

                m_lastKeyPress = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                var definition = MyCubeBuilder.Static.CubeBuilderState.CurrentBlockDefinition;
                if (definition == null)
                {
                    return;
                }

                if (!Owner.ControllerInfo.IsLocallyControlled())
                {
                    var val = Owner.IsUsing as MyCockpit;
                    if (val != null && !val.ControllerInfo.IsLocallyControlled())
                        return;
                }

                // Must have first component to start building
                if (MyCubeBuilder.Static.CanStartConstruction(Owner))
                {
                    MyCubeBuilder.Static.AddConstruction(Owner);
                }
                else
                {
                    if (!MySession.Static.CreativeToolsEnabled(Sync.MyId))
                        OnMissingComponents(definition);
                }
            }
        }

        public static void OnMissingComponents(MyCubeBlockDefinition definition)
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);

            (MyHud.Notifications.Get(MyNotificationSingletons.MissingComponent) as MyHudMissingComponentNotification).SetBlockDefinition(definition);
            MyHud.Notifications.Add(MyNotificationSingletons.MissingComponent);
        }

        public override void EndShoot(MyShootActionEnum action)
        {
            base.EndShoot(action);
            m_firstShot = true;

            if (CharacterInventory == null)
            {
                Debug.Fail("Character inventory was null !" );
                return;
            }

            MyCharacter character = CharacterInventory.Owner as MyCharacter;

            if (character == null)
            {
                Debug.Fail("Character inventory was not owned by a character");
                return;
            }

            if (m_closeAfterBuild)
            {

                if (character.ControllerInfo != null && character.ControllerInfo.IsRemotelyControlled())
                    return;

                character.SwitchToWeapon(null);
            }
            else
            {
                //if (MyPerGameSettings.CheckUseAnimationInsteadOfIK())
                //    character.PlayCharacterAnimation("Building_pose", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f);
            }
        }

        public override void OnControlReleased()
        {
            Debug.Assert(Owner != null, "Owner was null in OnControlReleased of MyCubePlacer");

            if (Owner != null && Owner.ControllerInfo.IsLocallyHumanControlled())
            {
                //BlockBuilder.Deactivate();
                MySession.Static.GameFocusManager.Clear();
            }

            base.OnControlReleased();
        }

        public override void OnControlAcquired(MyCharacter owner)
        {
            base.OnControlAcquired(owner);

            Debug.Assert(Owner != null, "Owner was null in OnControlAcquired of MyCubePlacer");

            if (Owner != null)
            {
                if (owner.UseNewAnimationSystem)
                {
                    Owner.TriggerCharacterAnimationEvent("building", false);
                }
                else
                {
                    Owner.PlayCharacterAnimation("Building_pose", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f);
                }

                //if (Owner.ControllerInfo.IsLocallyHumanControlled())
                //{
                //    BlockBuilder.Activate();
                //}
            }
        }

        protected override void AddHudInfo()
        {
        }

        protected override void RemoveHudInfo()
        {
        }

        protected override void DrawHud()
        {
        }
    }
}
