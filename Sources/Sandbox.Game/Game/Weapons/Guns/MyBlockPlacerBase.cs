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
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game.Weapons
{
    public abstract class MyBlockPlacerBase : MyEngineerToolBase
    {
        public static MyHudNotificationBase MissingComponentNotification =
             new MyHudNotification(MySpaceTexts.NotificationMissingComponentToPlaceBlockFormat, font: MyFontEnum.Red, priority: 1);

        protected abstract MyBlockBuilderBase BlockBuilder { get; }

        protected int m_lastKeyPress;
        protected bool m_firstShot;
        protected bool m_closeAfterBuild;

        protected MyBlockPlacerBase(MyHandItemDefinition definition)
            : base(definition, 0.5f, 500)
        {
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Init(null, null, null, null, null);
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;

            HasSecondaryEffect = false;
            HasPrimaryEffect = false;
            m_firstShot = true;

            //PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
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

        public override void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            if (MySession.Static.CreativeMode)
                return;

            m_closeAfterBuild = false;

            base.Shoot(action, direction);
            ShakeAmount = 0.0f;

            if (action == MyShootActionEnum.PrimaryAction && m_firstShot)
            {
                m_firstShot = false;

                m_lastKeyPress = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                var definition = MyCubeBuilder.Static.HudBlockDefinition;
                if (definition == null)
                {
                    return;
                }

                if (!Owner.ControllerInfo.IsLocallyControlled())
                    return;

                // Must have first component to start building
                if (MyCubeBuilder.Static.CanStartConstruction(Owner))
                {
                    bool placingGrid = MyCubeBuilder.Static.ShipCreationClipboard.IsActive;
                    m_closeAfterBuild = MyCubeBuilder.Static.AddConstruction(Owner) && placingGrid;
                    return;
                }
                else
                {
                    if (!MySession.Static.Battle)
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

            MyCharacter character = CharacterInventory.Owner as MyCharacter;
            if (m_closeAfterBuild)
            {
                Debug.Assert(character != null, "Character inventory was not owned by a character");

                if (character.ControllerInfo.IsRemotelyControlled())
                    return;

                character.SwitchToWeapon(null);
            }
            else
            {
                if (MyPerGameSettings.UseAnimationInsteadOfIK)
                    character.PlayCharacterAnimation("Building_pose", true, MyPlayAnimationMode.Immediate | MyPlayAnimationMode.Play, 0.2f);
            }
        }

        public override void OnControlReleased()
        {
            Debug.Assert(Owner != null, "Owner was null in OnControlReleased of MyCubePlacer");

            if (Owner != null && Owner.ControllerInfo.IsLocallyHumanControlled())
            {
                BlockBuilder.Deactivate();
            }

            base.OnControlReleased();
        }

        public override void OnControlAcquired(MyCharacter owner)
        {
            base.OnControlAcquired(owner);

            Debug.Assert(Owner != null, "Owner was null in OnControlAcquired of MyCubePlacer");

            if (Owner != null)
            {
                if (MyPerGameSettings.UseAnimationInsteadOfIK)
                    Owner.PlayCharacterAnimation("Building_pose", true, MyPlayAnimationMode.Play, 0.2f); 
                if (Owner.ControllerInfo.IsLocallyHumanControlled())
                {
                    BlockBuilder.Activate();
                }
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
