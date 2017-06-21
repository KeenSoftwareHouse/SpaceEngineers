using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Utils;
using VRage.Audio;
using VRage.Profiler;
using VRageRender.Utils;

namespace Sandbox.Game.Screens.Helpers
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyToolbarComponent : MySessionComponentBase
    {
        
        // Slot controls in order of toolbar items
        private static readonly MyStringId[] m_slotControls = new MyStringId[]
        {
            MyControlsSpace.SLOT1,
            MyControlsSpace.SLOT2,
            MyControlsSpace.SLOT3,
            MyControlsSpace.SLOT4,
            MyControlsSpace.SLOT5,
            MyControlsSpace.SLOT6,
            MyControlsSpace.SLOT7,
            MyControlsSpace.SLOT8,
            MyControlsSpace.SLOT9,
            MyControlsSpace.SLOT0,
        };

        private static MyToolbarComponent m_instance;
        private MyToolbar m_currentToolbar;
        private MyToolbar m_universalCharacterToolbar;
        private bool m_toolbarControlIsShown;

        #region Properties

        public static bool IsToolbarControlShown
        {
            get
            {
                if (m_instance == null)
                {
                    return false;
                }
                else
                {
                    return m_instance.m_toolbarControlIsShown;
                }
            }
            set
            {
                if (m_instance != null)
                {
                    m_instance.m_toolbarControlIsShown = value;
                }
            }
        }

        public static MyToolbar CurrentToolbar
        {
            get
            {
                return (m_instance != null) ? m_instance.m_currentToolbar : null;
            }

            set
            {
                if (m_instance.m_currentToolbar != value)
                {
                    m_instance.m_currentToolbar = value;
                    if (CurrentToolbarChanged != null)
                        CurrentToolbarChanged();
                }
            }
        }


        public static MyToolbar CharacterToolbar
        {
            get
            {
                return m_instance != null ? m_instance.m_universalCharacterToolbar : null;
            }
        }

        public static void UpdateCurrentToolbar()
        {
            if (!AutoUpdate)
                return;

            if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Toolbar != null && m_instance.m_currentToolbar != MySession.Static.ControlledEntity.Toolbar)
            {
                m_instance.m_currentToolbar = MySession.Static.ControlledEntity.Toolbar;
                if (CurrentToolbarChanged != null)
                    CurrentToolbarChanged();
            }
        }

        public static bool GlobalBuilding
        {
            get
            {
                if (!MySandboxGame.IsDedicated)
                {
                    Debug.Assert(MyGuiScreenGamePlay.Static != null && MySpectatorCameraController.Static != null, "There must be valid gameplay and spectator at this point!");
                }
                return MySession.Static.IsCameraUserControlledSpectator() && MyInput.Static.ENABLE_DEVELOPER_KEYS;
            }
        }

        public static bool CreativeModeEnabled
        {
            get
            {
                return MyFakes.UNLIMITED_CHARACTER_BUILDING || MySession.Static.CreativeMode;
            }
        }

        #endregion

        public static event Action CurrentToolbarChanged;

        public MyToolbarComponent()
        {
            m_universalCharacterToolbar = new MyToolbar(MyToolbarType.Character);
            m_currentToolbar = m_universalCharacterToolbar;
            AutoUpdate = true;
        }

        #region Overrides

        public override void LoadData()
        {
            m_instance = this;
            base.LoadData();
        }

        public override void HandleInput()
        {
            ProfilerShort.Begin("MyToolbarComponent.HandleInput");
            try
            {
                var context = MySession.Static.ControlledEntity != null ? MySession.Static.ControlledEntity.ControlContext : MyStringId.NullOrEmpty;
                var focusedScreen = MyScreenManager.GetScreenWithFocus();
                if ((focusedScreen == MyGuiScreenGamePlay.Static ||
                    IsToolbarControlShown ) &&
                    CurrentToolbar != null && !MyGuiScreenGamePlay.DisableInput)
                {
                    {
                        for (int i = 0; i < m_slotControls.Length; i++)
                        {
                            if (MyControllerHelper.IsControl(context, m_slotControls[i], MyControlStateType.NEW_PRESSED))
                            {
                                if (!MyInput.Static.IsAnyCtrlKeyPressed())
                                {
                                    if ((focusedScreen is MyGuiScreenScriptingTools|| focusedScreen == MyGuiScreenGamePlay.Static ||
                                            (focusedScreen is MyGuiScreenCubeBuilder || focusedScreen is MyGuiScreenToolbarConfigBase) && ((MyGuiScreenToolbarConfigBase)focusedScreen).AllowToolbarKeys()) &&
                                            CurrentToolbar != null)
                                        CurrentToolbar.ActivateItemAtSlot(i);
                                }
                                else if (i < CurrentToolbar.PageCount)
                                {
                                    MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                                    CurrentToolbar.SwitchToPage(i);
                                }
                            }
                        }
                    }

                    if ((focusedScreen == MyGuiScreenGamePlay.Static ||
                                (focusedScreen is MyGuiScreenCubeBuilder || focusedScreen is MyGuiScreenToolbarConfigBase) && ((MyGuiScreenToolbarConfigBase)focusedScreen).AllowToolbarKeys()) &&
                                CurrentToolbar != null)
                    {
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.TOOLBAR_NEXT_ITEM, MyControlStateType.NEW_PRESSED))
                            CurrentToolbar.SelectNextSlot();
                        else if (MyControllerHelper.IsControl(context, MyControlsSpace.TOOLBAR_PREV_ITEM, MyControlStateType.NEW_PRESSED))
                            CurrentToolbar.SelectPreviousSlot();
                        if (MySpectator.Static.SpectatorCameraMovement != MySpectatorCameraMovementEnum.ConstantDelta)
                        {
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.TOOLBAR_UP, MyControlStateType.NEW_PRESSED))
                            {
                                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                                CurrentToolbar.PageUp();
                            }
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.TOOLBAR_DOWN, MyControlStateType.NEW_PRESSED))
                            {
                                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                                CurrentToolbar.PageDown();
                            }
                        }
                    }
                }
            }
            finally
            {
                ProfilerShort.End();
            }

            base.HandleInput();
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("MyToolbarComponent.UpdateBeforeSimulation");

            try
            {
                using (Stats.Generic.Measure("Toolbar.Update()"))
                {
                    UpdateCurrentToolbar();
                    if (CurrentToolbar != null)
                        CurrentToolbar.Update();
                }
            }
            finally
            {
                ProfilerShort.End();
            }

            base.UpdateBeforeSimulation();
        }

        protected override void UnloadData()
        {
            m_instance = null;
            base.UnloadData();
        }

        #endregion

        static MyToolbar GetToolbar()
        {
            return m_instance.m_currentToolbar;
        }

        public static void InitCharacterToolbar(MyObjectBuilder_Toolbar characterToolbar)
        {
            m_instance.m_universalCharacterToolbar.Init(characterToolbar, null, true);
        }

        public static void InitToolbar(MyToolbarType type, MyObjectBuilder_Toolbar builder)
        {
            if (builder != null)
            {
                Debug.Assert(type == builder.ToolbarType, string.Format("Toolbar type mismatch during init. {0} != {1}", type, builder.ToolbarType));
                if (builder.ToolbarType != type)
                    builder.ToolbarType = type;
            }
            m_instance.m_currentToolbar.Init(builder, null, true);
        }

        public static MyObjectBuilder_Toolbar GetObjectBuilder(MyToolbarType type)
        {
            var builder = m_instance.m_currentToolbar.GetObjectBuilder();
            Debug.Assert(type == builder.ToolbarType, string.Format("Toolbar type mismatch when saving. {0} != {1}", type, builder.ToolbarType));
            builder.ToolbarType = type;
            return builder;
        }

        private static StringBuilder m_slotControlTextCache = new StringBuilder();
        public static StringBuilder GetSlotControlText(int slotIndex)
        {
            if (!m_slotControls.IsValidIndex(slotIndex))
                return null;

            m_slotControlTextCache.Clear();
            MyInput.Static.GetGameControl(m_slotControls[slotIndex])
                .AppendBoundKeyJustOne(ref m_slotControlTextCache);
            return m_slotControlTextCache;
        }

        private MyToolbarType GetCurrentToolbarType()
        {
            if (MyCubeBuilder.SpectatorIsBuilding)
            {
                return MyToolbarType.Spectator;
            }

            return MySession.Static.ControlledEntity != null ? MySession.Static.ControlledEntity.ToolbarType : MyToolbarType.Spectator;
        }

        public static bool AutoUpdate { get; set; }
    }
}
