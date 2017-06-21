using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Generics;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{

    public enum MyNotificationSingletons
    {
        DisabledWeaponsAndTools,
        HideHints,
        HelpHint,
        GameOverload,
        MultiplayerDisabled,
        WeaponDisabledInWorldSettings,
        SuitEnergyLow,
        SuitEnergyCritical,
        InventoryFull,
        MissingComponent,
        ScreenHint,
        //SlotEquipHint,
        //HudHideHint,
        WorldLoaded,
        RespawnShipWarning,
        ClientCannotSave,
        WheelNotPlaced,
        ObstructingBlockDuringMerge,
        AccessDenied,
        CopyPasteBlockNotAvailable,
        CopyPasteFloatingObjectNotAvailable,
        CopyPasteAsteoridObstructed,
        TextPanelReadOnly,
        GameplayOptions,
        IncompleteGrid,
        AdminMenuNotAvailable,
        BuildingModeOn,
        BuildingModeOff,
        PasteFailed,
        ManipulatingDoorFailed,
        
        // Piston notifications
        HeadNotPlaced,
        HeadAlreadyExists,

        ShipOverLimits,

        PlayerDemotedNone,
        PlayerDemotedScripter,
        PlayerDemotedModerator,
        PlayerDemotedSpaceMaster,

        PlayerPromotedScripter,
        PlayerPromotedModerator,
        PlayerPromotedSpaceMaster,
        PlayerPromotedAdmin,

        BlueprintScriptsRemoved,
    }

    public class MyHudNotifications
    {
        public class ControlsHelper
        {
            MyControl[] m_controls;

            public ControlsHelper(params MyControl[] controls)
            {
                m_controls = controls;
            }

            public override string ToString()
            {
                return String.Join(", ", m_controls.Select(s => s.ButtonNamesIgnoreSecondary).Where(s => !String.IsNullOrEmpty(s)));
            }
        }
        // Ensure that we don't have thousands of priority groups.
        public const int MAX_PRIORITY = 5;

        #region private fields
        private Predicate<MyHudNotificationBase> m_disappearedPredicate;
        private Dictionary<int, List<MyHudNotificationBase>> m_notificationsByPriority;
        private List<StringBuilder> m_texts;
        private List<Vector2> m_textSizes;
        private MyObjectsPool<StringBuilder> m_textsPool;

        private object m_lockObject = new object();
        #endregion

        #region Singleton notifications

        private MyHudNotificationBase[] m_singletons;

        public void Add(MyNotificationSingletons singleNotification)
        {
            Add(m_singletons[(int)singleNotification]);
        }

        public void Remove(MyNotificationSingletons singleNotification)
        {
            Remove(m_singletons[(int)singleNotification]);
        }

        public MyHudNotificationBase Get(MyNotificationSingletons singleNotification)
        {
            return m_singletons[(int)singleNotification];
        }

        #endregion

        public Vector2 Position;

        public MyHudNotifications()
        {
            Position                  = MyNotificationConstants.DEFAULT_NOTIFICATION_MESSAGE_NORMALIZED_POSITION;
            m_disappearedPredicate    = x => !x.Alive;
            m_notificationsByPriority = new Dictionary<int, List<MyHudNotificationBase>>();
            m_texts                   = new List<StringBuilder>(MyNotificationConstants.MAX_DISPLAYED_NOTIFICATIONS_COUNT);
            m_textSizes               = new List<Vector2>(MyNotificationConstants.MAX_DISPLAYED_NOTIFICATIONS_COUNT);
            m_textsPool               = new MyObjectsPool<StringBuilder>(MyNotificationConstants.MAX_DISPLAYED_NOTIFICATIONS_COUNT);

            m_singletons = new MyHudNotificationBase[Enum.GetValues(typeof(MyNotificationSingletons)).Length];

            Register(MyNotificationSingletons.GameOverload,                  new MyHudNotification(font: MyFontEnum.Red, priority: 2, text: MyCommonTexts.NotificationMemoryOverload));
            Register(MyNotificationSingletons.SuitEnergyLow,                 new MyHudNotification(font: MyFontEnum.Red, priority: 2, text: MySpaceTexts.NotificationSuitEnergyLow));
            Register(MyNotificationSingletons.SuitEnergyCritical,            new MyHudNotification(font: MyFontEnum.Red, priority: 2, text: MySpaceTexts.NotificationSuitEnergyCritical));
            Register(MyNotificationSingletons.InventoryFull,                 new MyHudNotification(font: MyFontEnum.Red, priority: 2, text: MyCommonTexts.NotificationInventoryFull));
            Register(MyNotificationSingletons.IncompleteGrid,                new MyHudNotification(font: MyFontEnum.Red, priority: 2, text: MyCommonTexts.NotificationIncompleteGrid));

            Register(MyNotificationSingletons.DisabledWeaponsAndTools,       new MyHudNotification(font: MyFontEnum.Red, text: MyCommonTexts.NotificationToolDisabled, disappearTimeMs: 0));
            Register(MyNotificationSingletons.WeaponDisabledInWorldSettings, new MyHudNotification(font: MyFontEnum.Red, text: MyCommonTexts.NotificationWeaponDisabledInSettings));
            Register(MyNotificationSingletons.MultiplayerDisabled,           new MyHudNotification(font: MyFontEnum.Red, text: MyCommonTexts.NotificationMultiplayerDisabled, priority: MAX_PRIORITY));
            Register(MyNotificationSingletons.MissingComponent,              new MyHudMissingComponentNotification(MyCommonTexts.NotificationMissingComponentToPlaceBlockFormat, priority: 1));
            Register(MyNotificationSingletons.WorldLoaded,                   new MyHudNotification(text: MyCommonTexts.WorldLoaded));
            Register(MyNotificationSingletons.ObstructingBlockDuringMerge,   new MyHudNotification(font: MyFontEnum.Red, text: MySpaceTexts.NotificationObstructingBlockDuringMerge));

            Register(MyNotificationSingletons.HideHints,                    new MyHudNotification(disappearTimeMs: 0, level: MyNotificationLevel.Control, text: MyCommonTexts.NotificationHideHintsInGameOptions, priority: 2));
            Register(MyNotificationSingletons.HelpHint,                     new MyHudNotification(disappearTimeMs: 0, level: MyNotificationLevel.Control, text: MyCommonTexts.NotificationNeedShowHelpScreen, priority: 1));
            Register(MyNotificationSingletons.ScreenHint,                   new MyHudNotification(disappearTimeMs: 0, level: MyNotificationLevel.Control, text: MyCommonTexts.NotificationScreenFormat));
            //Register(MyNotificationSingletons.SlotEquipHint,                new MyHudNotification(disappearTimeMs: 0, level: MyNotificationLevel.Control, text: MyCommonTexts.NotificationSlotEquipFormat));
            //Register(MyNotificationSingletons.HudHideHint,                  new MyHudNotification(disappearTimeMs: 0, level: MyNotificationLevel.Control, text: MyCommonTexts.NotificationHudHideFormat));

            Register(MyNotificationSingletons.RespawnShipWarning,       new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationRespawnShipDelete, font: MyFontEnum.Red));

            Register(MyNotificationSingletons.PlayerDemotedNone, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerDemoted_None, font: MyFontEnum.Red));
            Register(MyNotificationSingletons.PlayerDemotedScripter, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerDemoted_Scripter, font: MyFontEnum.Red));
            Register(MyNotificationSingletons.PlayerDemotedModerator, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerDemoted_Moderator, font: MyFontEnum.Red));
            Register(MyNotificationSingletons.PlayerDemotedSpaceMaster, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerDemoted_SpaceMaster, font: MyFontEnum.Red));

            Register(MyNotificationSingletons.PlayerPromotedScripter, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerPromoted_Scripter, font: MyFontEnum.Blue));
            Register(MyNotificationSingletons.PlayerPromotedModerator, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerPromoted_Moderator, font: MyFontEnum.Blue));
            Register(MyNotificationSingletons.PlayerPromotedSpaceMaster, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerPromoted_SpaceMaster, font: MyFontEnum.Blue));
            Register(MyNotificationSingletons.PlayerPromotedAdmin, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, text: MySpaceTexts.NotificationPlayerPromoted_Admin, font: MyFontEnum.Blue));

            Register(MyNotificationSingletons.PasteFailed, new MyHudNotification(disappearTimeMs: 1300, level: MyNotificationLevel.Important, text: MyCommonTexts.NotificationPasteFailed, font: MyFontEnum.Red));

            Register(MyNotificationSingletons.ClientCannotSave, new MyHudNotification(font: MyFontEnum.Red, text: MyCommonTexts.NotificationClientCannotSave));
            Register(MyNotificationSingletons.WheelNotPlaced, new MyHudNotification(font: MyFontEnum.Red, text: MySpaceTexts.NotificationWheelNotPlaced));
            Register(MyNotificationSingletons.CopyPasteBlockNotAvailable, new MyHudNotification(font: MyFontEnum.Red, text: MyCommonTexts.NotificationCopyPasteBlockNotAvailable));
            Register(MyNotificationSingletons.CopyPasteFloatingObjectNotAvailable, new MyHudNotification(font: MyFontEnum.Red, text: MyCommonTexts.NotificationCopyPasteFloatingObjectNotAvailable));

            Register(MyNotificationSingletons.CopyPasteAsteoridObstructed, new MyHudNotification(font: MyFontEnum.Red, text: MySpaceTexts.NotificationCopyPasteAsteroidObstructed));

            Register(MyNotificationSingletons.TextPanelReadOnly, new MyHudNotification(font: MyFontEnum.Red, text: MyCommonTexts.NotificationTextPanelReadOnly));

            Register(MyNotificationSingletons.AccessDenied, new MyHudNotification(MyCommonTexts.AccessDenied, 2500, MyFontEnum.Red));
            Register(MyNotificationSingletons.AdminMenuNotAvailable, new MyHudNotification(disappearTimeMs: 10000, level: MyNotificationLevel.Important, font: MyFontEnum.Red, priority: 2, text: MySpaceTexts.AdminMenuNotAvailable));

            Register(MyNotificationSingletons.BuildingModeOn, new MyHudNotification(MySpaceTexts.BuilderModeOn, 2500, MyFontEnum.White));
            Register(MyNotificationSingletons.BuildingModeOff, new MyHudNotification(MySpaceTexts.BuilderModeOff, 2500, MyFontEnum.White));

            // Piston notifications
            Register(MyNotificationSingletons.HeadNotPlaced, new MyHudNotification(font: MyFontEnum.Red, text: MySpaceTexts.Notification_PistonHeadNotPlaced));
            Register(MyNotificationSingletons.HeadAlreadyExists, new MyHudNotification(font: MyFontEnum.Red, text: MySpaceTexts.Notification_PistonHeadAlreadyExists));

            Register(MyNotificationSingletons.ShipOverLimits, new MyHudNotification(font: MyFontEnum.Red, text: MySpaceTexts.NotificationShipOverLimits));

            if (MyPerGameSettings.Game == GameEnum.ME_GAME)
            {
                Register(MyNotificationSingletons.GameplayOptions, new MyHudNotification(MyCommonTexts.Notification_GameplayOptions, 0, level: MyNotificationLevel.Control));
                Add(MyNotificationSingletons.GameplayOptions);
            }

            Register(MyNotificationSingletons.ManipulatingDoorFailed, new MyHudNotification(disappearTimeMs: 2500, level: MyNotificationLevel.Important, text: MyCommonTexts.Notification_CannotManipulateDoor, font: MyFontEnum.Red));

            Register(MyNotificationSingletons.BlueprintScriptsRemoved, new MyHudNotification(MySpaceTexts.Notification_BlueprintScriptRemoved, 2500, MyFontEnum.Red));

            Add(MyNotificationSingletons.HelpHint);
            Add(MyNotificationSingletons.HideHints);
            Add(MyNotificationSingletons.ScreenHint);
            //Add(MyNotificationSingletons.SlotEquipHint);
            //Add(MyNotificationSingletons.HudHideHint);

            FormatNotifications(MyInput.Static.IsJoystickConnected() && MyFakes.ENABLE_CONTROLLER_HINTS);
            MyInput.Static.JoystickConnected += Static_JoystickConnected;
        }

        void Static_JoystickConnected(bool value)
        {
            FormatNotifications(value && MyFakes.ENABLE_CONTROLLER_HINTS);
        }

        public void Add(MyHudNotificationBase notification)
        {

            Debug.Assert(notification != null);
            Debug.Assert(notification.Priority <= MAX_PRIORITY);
            Debug.Assert(notification.Priority >= 0);
            lock (m_lockObject)
            {
                var group = GetNotificationGroup(notification.Priority);
                if (!group.Contains(notification))
                {
                    notification.BeforeAdd();
                    group.Add(notification);
                }
                notification.ResetAliveTime();
            }
        }

        public void Remove(MyHudNotificationBase notification)
        {
            if (notification == null)
                return;

            lock (m_lockObject)
            {
                var group = GetNotificationGroup(notification.Priority);
                //Debug.Assert(group.Contains(notification));
                group.Remove(notification);
            }
        }

        public void Clear()
        {
            MyInput.Static.JoystickConnected -= Static_JoystickConnected;
            lock (m_lockObject)
            {
                foreach (var entry in m_notificationsByPriority)
                    entry.Value.Clear();
            }
        }

        public void Draw()
        {
            int visibleCount;
            ProcessBeforeDraw(out visibleCount);
            DrawFog();
            DrawNotifications(visibleCount);
        }

        public void ReloadTexts()
        {
            FormatNotifications(MyInput.Static.IsJoystickConnected() && MyFakes.ENABLE_CONTROLLER_HINTS);
            lock (m_lockObject)
            {
                foreach (var entry in m_notificationsByPriority)
                {
                    foreach (var item in entry.Value)
                        item.SetTextDirty();
                }
            }
        }

        public void UpdateBeforeSimulation()
        {
            lock (m_lockObject)
            {
                foreach (var entry in m_notificationsByPriority)
                {
                    foreach (var item in entry.Value)
                        item.AddAliveTime(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
                }

                foreach (var entry in m_notificationsByPriority)
                {
                    foreach (var notification in entry.Value)
                    {
                        if (m_disappearedPredicate(notification))
                        {
                            notification.BeforeRemove();
                        }
                    }
                    entry.Value.RemoveAll(m_disappearedPredicate);
                }
            }
        }

        public void Register(MyNotificationSingletons singleton, MyHudNotificationBase notification)
        {
            m_singletons[(int)singleton] = notification;
        }

        public static MyHudNotification CreateControlNotification(MyStringId textId, params object[] args)
        {
            var notification = new MyHudNotification(text: textId, disappearTimeMs: 0, level: MyNotificationLevel.Control);
            notification.SetTextFormatArguments(args);
            return notification;
        }

        private void ClearTexts()
        {
            m_textSizes.Clear();
            foreach (StringBuilder text in m_texts)
            {
                text.Clear();
            }
            m_textsPool.DeallocateAll();
            m_texts.Clear();
        }

        private void ProcessBeforeDraw(out int visibleCount)
        {
            ClearTexts();

            visibleCount = 0;
            lock (m_lockObject)
            {
                for (int i = MAX_PRIORITY; i >= 0; --i)
                {
                    List<MyHudNotificationBase> notifications;
                    m_notificationsByPriority.TryGetValue(i, out notifications);
                    if (notifications == null)
                        continue;


                    foreach (var notification in notifications)
                    {
                        if (!IsDrawn(notification))
                            continue;

                        StringBuilder messageStringBuilder = m_textsPool.Allocate();
                        Debug.Assert(messageStringBuilder != null);

                        messageStringBuilder.Append(notification.GetText());

                        Vector2 textSize = MyGuiManager.MeasureString(notification.Font, messageStringBuilder, MyGuiSandbox.GetDefaultTextScaleWithLanguage());

                        m_textSizes.Add(textSize);
                        m_texts.Add(messageStringBuilder);
                        ++visibleCount;
                        if (visibleCount == MyNotificationConstants.MAX_DISPLAYED_NOTIFICATIONS_COUNT)
                            return;
                    }
                }
            }
        }

        private void DrawFog()
        {
            Vector2 notificationPosition = Position;

            for (int i = 0; i < m_textSizes.Count; i++)
            {
                var textSize = m_textSizes[i];
                MyGuiTextShadows.DrawShadow(ref notificationPosition, ref textSize);
                notificationPosition.Y += textSize.Y;
            }
        }

        private void DrawNotifications(int visibleCount)
        {
            lock (m_lockObject)
            {
                var notificationPosition = Position;
                int textIdx = 0;
                for (int i = MAX_PRIORITY; i >= 0; --i)
                {

                    List<MyHudNotificationBase> notifications;
                    m_notificationsByPriority.TryGetValue(i, out notifications);
                    if (notifications == null)
                        continue;

                    foreach (var notification in notifications)
                    {
                        if (!IsDrawn(notification))
                            continue;

                        MyGuiManager.DrawString(notification.Font, m_texts[textIdx], notificationPosition,
                                                MyGuiSandbox.GetDefaultTextScaleWithLanguage(), Color.White,
                                                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyVideoSettingsManager.IsTripleHead());
                        notificationPosition.Y += m_textSizes[textIdx].Y;
                        ++textIdx;
                        --visibleCount;
                        if (visibleCount == 0)
                            return;
                    }
                }
            }
        }

        private List<MyHudNotificationBase> GetNotificationGroup(int priority)
        {
            List<MyHudNotificationBase> output;
            if (!m_notificationsByPriority.TryGetValue(priority, out output))
            {
                output = new List<MyHudNotificationBase>();
                m_notificationsByPriority[priority] = output;
            }
            return output;
        }

        private bool IsDrawn(MyHudNotificationBase notification)
        {
            bool isDrawn = notification.Alive;
            if (notification.IsControlsHint)
                isDrawn = isDrawn && MySandboxGame.Config.ControlsHints;
            if (MyHud.MinimalHud && !MyHud.CutsceneHud && notification.Level != MyNotificationLevel.Important)
                isDrawn = false;
            if(MyHud.CutsceneHud && notification.Level == MyNotificationLevel.Control)
                isDrawn = false;

            return isDrawn;
        }

        private void SetNotificationTextAndArgs(MyNotificationSingletons type, MyStringId textId, params object[] args)
        {
            var notification = Get(type) as MyHudNotification;
            notification.Text = textId;
            notification.SetTextFormatArguments(args);
            Add(notification);
        }

        private void FormatNotifications(bool forJoystick)
        {
            if (forJoystick)
            {
                var cx_base = MySpaceBindingCreator.CX_BASE;
                var cx_char = MySpaceBindingCreator.CX_CHARACTER;

                var controlMenuCode = MyControllerHelper.GetCodeForControl(cx_base, MyControlsSpace.CONTROL_MENU);
                var nextItemCode = MyControllerHelper.GetCodeForControl(cx_char, MyControlsSpace.TOOLBAR_NEXT_ITEM);
                var prevItemCode = MyControllerHelper.GetCodeForControl(cx_char, MyControlsSpace.TOOLBAR_PREV_ITEM);

                //Remove(MyNotificationSingletons.HudHideHint);
                if (MyPerGameSettings.Game == GameEnum.ME_GAME)
                {
                    Remove(MyNotificationSingletons.GameplayOptions);
                }

                SetNotificationTextAndArgs(MyNotificationSingletons.HelpHint, MyCommonTexts.NotificationJoystickControlMenuFormat, controlMenuCode);
                SetNotificationTextAndArgs(MyNotificationSingletons.ScreenHint, MyCommonTexts.NotificationJoystickMenus);
                //SetNotificationTextAndArgs(MyNotificationSingletons.SlotEquipHint, MyCommonTexts.NotificationJoystickSlotEquipFormat, prevItemCode, nextItemCode);
            }
            else
            {          
                var hud = MyInput.Static.GetGameControl(MyControlsSpace.TOGGLE_HUD);
                var slot1 = MyInput.Static.GetGameControl(MyControlsSpace.SLOT1);
                var slot2 = MyInput.Static.GetGameControl(MyControlsSpace.SLOT2);
                var slot3 = MyInput.Static.GetGameControl(MyControlsSpace.SLOT3);
                var buildScreen = MyInput.Static.GetGameControl(MyControlsSpace.BUILD_SCREEN);
                var help = MyInput.Static.GetGameControl(MyControlsSpace.HELP_SCREEN);
                var compoundToggle = MyInput.Static.GetGameControl(MyControlsSpace.SWITCH_COMPOUND);             

                //Add(MyNotificationSingletons.HudHideHint);
                if (MyPerGameSettings.Game == GameEnum.ME_GAME)
                {
                    Add(MyNotificationSingletons.GameplayOptions);
                    SetNotificationTextAndArgs(MyNotificationSingletons.GameplayOptions, MyCommonTexts.Notification_GameplayOptions, MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL));
                }

                SetNotificationTextAndArgs(MyNotificationSingletons.HelpHint, MyCommonTexts.NotificationNeedShowHelpScreen, help);
                SetNotificationTextAndArgs(MyNotificationSingletons.ScreenHint, MyCommonTexts.NotificationScreenFormat, buildScreen);
                //SetNotificationTextAndArgs(MyNotificationSingletons.HudHideHint, MyCommonTexts.NotificationHudHideFormat, hud);
                //SetNotificationTextAndArgs(MyNotificationSingletons.SlotEquipHint, MyCommonTexts.NotificationSlotEquipFormat, slot1, slot2, slot3);
            }
        }
    }
}
