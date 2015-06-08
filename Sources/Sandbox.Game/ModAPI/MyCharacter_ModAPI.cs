using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;
using VRageMath;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;

namespace Sandbox.Game.Entities.Character
{
    public partial class MyCharacter : IMyCharacter
    {
        public void SetHealth(float health, bool sync)
        {
            health = MathHelper.Clamp(health, 0, MaxHealth);

            if (!m_health.HasValue)
                m_health = MaxHealth;

            if (sync && m_health.Value != health)
                SyncObject.UpdateStat(MySyncCharacter.UpdateStatEnum.HEALTH, health);

            m_health = health;
        }

        void IMyCharacter.DoDamage(float damage, Sandbox.Common.ObjectBuilders.Definitions.MyDamageType damageType, bool forceKill, bool sync)
        {
            DoDamage(damage, damageType, sync, forceKill);
        }

        float IMyCharacter.AccumulatedDamage
        {
            get { return CharacterAccumulatedDamage; }
        }

        public void Kill(bool ask, MyDamageType damageType, bool forceKill, bool sync)
        {
            if (ask)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextSuicide),
                focusedResult: MyGuiScreenMessageBox.ResultEnum.NO,
                callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                {
                    Kill(false, damageType, forceKill, sync);
                }));
            }
            else
            {
                DoDamage(MaxHealth + 1000, damageType, sync, forceKill);
            }
        }

        float IMyCharacter.SuitOxygenLevel
        {
            get { return Definition.OxygenCapacity == 0 ? 0 : MathHelper.Clamp(m_suitOxygenAmount / Definition.OxygenCapacity, 0.0f, 1.0f); }
        }

        float IMyCharacter.SuitOxygen
        {
            get { return SuitOxygenAmount; }
        }

        float IMyCharacter.SuitMaxOxygen
        {
            get { return Definition.OxygenCapacity; }
        }

        void IMyCharacter.SetOxygenLevel(float level, bool sync)
        {
            m_suitOxygenAmount = MathHelper.Clamp(level * Definition.OxygenCapacity, 0.0f, Definition.OxygenCapacity);

            if (sync)
                SyncObject.UpdateStat(MySyncCharacter.UpdateStatEnum.OXYGEN, m_suitOxygenAmount);
        }

        float IMyCharacter.EnvironmentOxygenLevel
        {
            get { return EnvironmentOxygenLevel; }
        }

        bool IMyCharacter.SuitNeedsOxygen
        {
            get { return Definition.NeedsOxygen; }
        }

        float IMyCharacter.CurrentSpeed
        {
            get { return m_currentSpeed; }
        }

        bool IMyCharacter.CanFly
        {
            get { return CanFly(); }
        }

        bool IMyCharacter.CanJump
        {
            get { return m_canJump; }
        }

        bool IMyCharacter.CanDie
        {
            get { return CharacterCanDie; }
        }

        MyCharacterMovementEnum IMyCharacter.CurrentMovement
        {
            get { return m_currentMovementState; }
        }

        bool IMyCharacter.IsWalking
        {
            get { return m_movementFlags == MyCharacterMovementFlags.Walk; }
        }

        bool IMyCharacter.IsFlying
        {
            get { return m_isFlying; }
        }

        bool IMyCharacter.IsAimingDownSights
        {
            get { return m_zoomMode == MyZoomModeEnum.IronSight; }
        }

        float IMyCharacter.BatteryLevel
        {
            get { return MathHelper.Clamp(SuitBattery.RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY, 0.0f, 1.0f); }
        }

        IMySuitBattery IMyCharacter.Battery
        {
            get { return m_suitBattery; }
        }

        bool IMyCharacter.BroadcastingEnabled
        {
            get { return m_radioBroadcaster.Enabled; }
        }

        float IMyCharacter.BroadcastingRadius
        {
            get { return m_radioBroadcaster.BroadcastRadius; }
        }

        Vector3 IMyCharacter.ArtificialGravity
        {
            get { return m_artificialGravity; }
        }

        bool IMyCharacter.MinimalHud
        {
            get { return MyHud.MinimalHud; }
        }

        void IMyCharacter.SetLights(bool enable, bool sync)
        {
            EnableLights(enable, sync);
        }

        void IMyCharacter.SetBroadcasting(bool enable, bool notify, bool sync)
        {
            if (!IsDead)
            {
                EnableBroadcasting(enable, sync);

                if (notify)
                {
                    if (m_radioBroadcaster.Enabled)
                        MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.NotificationCharacterBroadcastingOn));
                    else
                        MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.NotificationCharacterBroadcastingOff));
                }
            }
        }

        void IMyCharacter.SetDampeners(bool enable, bool notify, bool sync)
        {
            if (!IsDead)
            {
                EnableDampeners(enable, sync);

                if (notify)
                {
                    if (m_dampenersEnabled)
                        MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.NotificationInertiaDampenersOn));
                    else
                        MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.NotificationInertiaDampenersOff));
                }
            }
        }

        void IMyCharacter.SetJetpack(bool enable, bool notify, bool sync)
        {
            if (m_jetpackEnabled != enable)
                EnableJetpack(enable: enable, notify: notify, updateSync: sync);
        }

        Vector3 IMyCharacter.SuitColorHSV
        {
            get { return ColorMask; }
        }

        void IMyCharacter.SetModelAndColor(string model, Vector3 colorMaskHSV, bool sync)
        {
            if (sync)
            {
                ChangeModelAndColor(model, colorMaskHSV);
            }
            else
            {
                ChangeModelAndColorInternal(model, colorMaskHSV);
            }
        }

        IMyFaction IMyCharacter.GetFaction()
        {
            return MySession.Static.Factions.TryGetPlayerFaction(MySession.LocalPlayerId);
        }

        IMyEntity IMyCharacter.UsingEntity
        {
            get { return m_usingEntity; }
        }

        Sandbox.ModAPI.Interfaces.IMyControllableEntity IMyCharacter.RemoteControlledEntity
        {
            get { return CurrentRemoteControl; }
        }

        IMyEntity Sandbox.ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void ModAPI.Interfaces.IMyControllableEntity.DrawHud(ModAPI.Interfaces.IMyCameraController camera, long playerId)
        {
            if (camera is IMyCameraController)
                DrawHud(camera as IMyCameraController, playerId);
        }
    }
}
