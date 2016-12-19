using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenHelpSpace : MyGuiScreenBase
    {
        struct ControlWithDescription
        {
            public StringBuilder BoundButtons;
            public StringBuilder Description;
            public string LeftFont;
            public string RightFont;

            public StringBuilder LeftIcon;
            public Color LeftIconColor;

            public ControlWithDescription(string boundButtons, string description, string leftFont = MyFontEnum.Red, string rightFont = MyFontEnum.White)
                : this(new StringBuilder(boundButtons), new StringBuilder(description), leftFont, rightFont)
            { }
            public ControlWithDescription(StringBuilder boundButtons, StringBuilder description, string leftFont = MyFontEnum.Red, string rightFont = MyFontEnum.White)
            {
                BoundButtons = new StringBuilder(boundButtons.Length).AppendStringBuilder(boundButtons);
                Description = new StringBuilder(description.Length).AppendStringBuilder(description);
                LeftFont = leftFont;
                RightFont = rightFont;
                LeftIcon = null;
                LeftIconColor = Color.White;
            }
            public ControlWithDescription(StringBuilder boundButtons, StringBuilder description, StringBuilder leftIcon, Color leftIconColor, string leftFont = MyFontEnum.Red, string rightFont = MyFontEnum.White)
            {
                BoundButtons = new StringBuilder(boundButtons.Length).AppendStringBuilder(boundButtons);
                Description = new StringBuilder(description.Length).AppendStringBuilder(description);
                LeftFont = leftFont;
                RightFont = rightFont;
                LeftIcon = leftIcon;
                LeftIconColor = leftIconColor;
            }
            public ControlWithDescription(MyStringId control)
            {
                MyControl c = MyInput.Static.GetGameControl(control);
                BoundButtons = null;
                c.AppendBoundButtonNames(ref BoundButtons, unassignedText: MyInput.Static.GetUnassignedName());
                Description = MyTexts.Get(c.GetControlDescription() ?? c.GetControlName());
                LeftFont = MyFontEnum.Red;
                RightFont = MyFontEnum.White;
                LeftIcon = null;
                LeftIconColor = Color.White;
            }
        }

        class HelpPage
        {
            public List<ControlWithDescription> LeftColumn = new List<ControlWithDescription>();
            public List<ControlWithDescription> RightColumn = new List<ControlWithDescription>();
        }

        enum HelpPageEnum
        {
            Basic,
            Advanced,
            Advanced2,
            Spectator,
            Performance,
            Developer,
            Developer2,
        }

        bool m_wasPause = false;

        HelpPage[] m_pages;
        String[] m_pageTitles;
        HelpPageEnum m_currentPage;

        public MyGuiScreenHelpSpace()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(1f, 0.98f))
        {
            EnabledBackgroundFade = true;

            m_pages = new HelpPage[typeof(HelpPageEnum).GetEnumValues().Length];
            m_pageTitles = new String[m_pages.Length];

            m_pages[(int)HelpPageEnum.Basic] = new HelpPage();
            m_pages[(int)HelpPageEnum.Advanced] = new HelpPage();
            m_pages[(int)HelpPageEnum.Advanced2] = new HelpPage();
            m_pages[(int)HelpPageEnum.Spectator] = new HelpPage();
            m_pages[(int)HelpPageEnum.Performance] = new HelpPage();
            m_pages[(int)HelpPageEnum.Developer] = new HelpPage();
            m_pages[(int)HelpPageEnum.Developer2] = new HelpPage();

            m_pageTitles[(int)HelpPageEnum.Basic] = MyTexts.GetString(MyCommonTexts.BasicControls);
            m_pageTitles[(int)HelpPageEnum.Advanced] = MyTexts.GetString(MyCommonTexts.AdvancedControls);
            m_pageTitles[(int)HelpPageEnum.Advanced2] = MyTexts.GetString(MyCommonTexts.AdvancedControls);
            m_pageTitles[(int)HelpPageEnum.Spectator] = MyTexts.GetString(MyCommonTexts.SpectatorControls);
            m_pageTitles[(int)HelpPageEnum.Performance] = MyTexts.GetString(MyCommonTexts.PerformanceWarningHelpHeader);
            m_pageTitles[(int)HelpPageEnum.Developer] = "Developer Controls";
            m_pageTitles[(int)HelpPageEnum.Developer2] = "Developer Controls";

            HelpPage basicPage = m_pages[(int)HelpPageEnum.Basic];
            HelpPage advancedPage = m_pages[(int)HelpPageEnum.Advanced];
            HelpPage advancedPage2 = m_pages[(int)HelpPageEnum.Advanced2];
            HelpPage spectatorPage = m_pages[(int)HelpPageEnum.Spectator];
            HelpPage performancePage = m_pages[(int)HelpPageEnum.Performance];
            HelpPage developerPage = m_pages[(int)HelpPageEnum.Developer];
            HelpPage developer2Page = m_pages[(int)HelpPageEnum.Developer2];

            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.BUILD_SCREEN));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.FORWARD));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.BACKWARD));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.STRAFE_LEFT));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.STRAFE_RIGHT));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.ROLL_LEFT));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.ROLL_RIGHT));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.JUMP));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CROUCH));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SWITCH_WALK));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CAMERA_MODE));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.DAMPING));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.THRUSTS));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.BROADCASTING));
            basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.HELMET));
			if (MySession.Static != null && (MySession.Static.IsScenario || MySession.Static.Settings.ScenarioEditMode))
				basicPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.MISSION_SETTINGS));

            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.PRIMARY_TOOL_ACTION));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT1));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT2));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT3));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT4));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT5));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT6));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT7));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT8));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT9));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SLOT0));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.TOOLBAR_UP));
            basicPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.TOOLBAR_DOWN));
            basicPage.RightColumn.Add(new ControlWithDescription("Shift + " + MyInput.Static.GetGameControl(MyControlsSpace.SLOT1).ToString(), "Select toolbar page 1"));
            basicPage.RightColumn.Add(new ControlWithDescription("Shift + " + MyInput.Static.GetGameControl(MyControlsSpace.SLOT2).ToString(), "Select toolbar page 2"));
            basicPage.RightColumn.Add(new ControlWithDescription("Shift + " + MyInput.Static.GetGameControl(MyControlsSpace.SLOT3).ToString(), "Select toolbar page 3"));
            basicPage.RightColumn.Add(new ControlWithDescription("Shift + " + MyInput.Static.GetGameControl(MyControlsSpace.SLOT4).ToString(), "Select toolbar page 4"));

            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SPRINT));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.LOOKAROUND));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.LANDING_GEAR));

            advancedPage.LeftColumn.Add(new ControlWithDescription("Shift + " + MyInput.Static.GetGameControl(MyControlsSpace.LANDING_GEAR).ToString(), "Pick color from cube into slot"));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyTexts.Get(MyCommonTexts.MouseWheel), MyTexts.Get(MySpaceTexts.ControlDescZoom)));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SECONDARY_TOOL_ACTION));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.USE));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.INVENTORY));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.TOGGLE_REACTORS));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.TERMINAL));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.HEADLIGHTS));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SUICIDE));
            advancedPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.TOGGLE_HUD));

            advancedPage.LeftColumn.Add(new ControlWithDescription("Ctrl + C", "Copy object"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Ctrl + Shift + C", "Copy object detached"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Ctrl + V", "Paste object"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Ctrl + X", "Delete object"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Ctrl + B", "Create/manage blueprints"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("F10", "Open blueprint screen"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Shift + F10", "Open spawn screen"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Alt + F10", "Open admin screen"));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Ctrl + " + MyTexts.GetString(MyCommonTexts.MouseWheel), MyTexts.GetString(MyCommonTexts.ControlDescCopyPasteMove)));
            advancedPage.LeftColumn.Add(new ControlWithDescription("Ctrl + Alt+ E", MyTexts.GetString(MyCommonTexts.ControlDescExportModel)));

            StringBuilder repaintControlText = null;
            MyInput.Static.GetGameControl(MyControlsSpace.CUBE_COLOR_CHANGE).AppendBoundButtonNames(ref repaintControlText, unassignedText: MyInput.Static.GetUnassignedName());
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.HELP_SCREEN));
            //advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.CHAT_SCREEN));
            advancedPage.RightColumn.Add(new ControlWithDescription("F3", MyTexts.GetString(MyCommonTexts.ControlDescPlayersList)));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.CHAT_SCREEN));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.CONSOLE));

            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SCREENSHOT));
            //advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SHOW_DAMAGED));
            advancedPage.RightColumn.Add(new ControlWithDescription("F5", MyTexts.GetString(MyCommonTexts.ControlDescQuickLoad)));
            advancedPage.RightColumn.Add(new ControlWithDescription("Shift + F5", MyTexts.GetString(MyCommonTexts.ControlDescQuickSave)));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.PAUSE_GAME));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyInput.Static.GetGameControl(MyControlsSpace.LANDING_GEAR).ToStringBuilder(MyInput.Static.GetUnassignedName()), MyTexts.Get(MySpaceTexts.TerminalControlPanel_Cockpit_Handbrake)));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyInput.Static.GetGameControl(MyControlsSpace.JUMP).ToStringBuilder(MyInput.Static.GetUnassignedName()), MyTexts.Get(MySpaceTexts.ControlDescBrake)));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyInput.Static.GetGameControl(MyControlsSpace.INVENTORY).ToStringBuilder(MyInput.Static.GetUnassignedName()).Append("/").Append(MyInput.Static.GetGameControl(MyControlsSpace.TERMINAL).ToString()),
                MyTexts.Get(MySpaceTexts.ControlDescLoot)));
            advancedPage.RightColumn.Add(new ControlWithDescription("", ""));
            advancedPage.RightColumn.Add(new ControlWithDescription(new StringBuilder(), MyTexts.Get(MyCommonTexts.Factions), rightFont: MyFontEnum.Red));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyTexts.Get(MySpaceTexts.HelpScreen_FactionColor_Blue), MyTexts.Get(MySpaceTexts.Factions_YourBlock), new StringBuilder("Textures\\HUD\\marker_self.dds"), new Color(117, 201, 241), MyFontEnum.Blue));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyTexts.Get(MySpaceTexts.HelpScreen_FactionColor_Green), MyTexts.Get(MySpaceTexts.Factions_YourFaction), new StringBuilder("Textures\\HUD\\marker_friendly.dds"), new Color(101, 178, 90), MyFontEnum.Green));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyTexts.Get(MySpaceTexts.HelpScreen_FactionColor_White), MyTexts.Get(MySpaceTexts.Factions_NeutralFaction), new StringBuilder("Textures\\HUD\\marker_neutral.dds"), Color.White, MyFontEnum.White));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyTexts.Get(MySpaceTexts.HelpScreen_FactionColor_Red), MyTexts.Get(MySpaceTexts.Factions_EnemyFaction), new StringBuilder("Textures\\HUD\\marker_enemy.dds"), new Color(227, 62, 63)));

            advancedPage.RightColumn.Add(new ControlWithDescription("", ""));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.ROTATION_LEFT));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.ROTATION_RIGHT));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.ROTATION_UP));
            advancedPage.RightColumn.Add(new ControlWithDescription(MyControlsSpace.ROTATION_DOWN));

            advancedPage2.RightColumn.Add(new ControlWithDescription(repaintControlText, MyTexts.Get(MySpaceTexts.ControlDescSingleAllMode)));
            advancedPage2.RightColumn.Add(new ControlWithDescription(repaintControlText, MyTexts.Get(MySpaceTexts.ControlDescHoldToColor)));
            advancedPage2.RightColumn.Add(new ControlWithDescription("Ctrl + " + repaintControlText, MyTexts.GetString(MySpaceTexts.ControlDescMediumBrush)));
            advancedPage2.RightColumn.Add(new ControlWithDescription("Shift + " + repaintControlText, MyTexts.GetString(MySpaceTexts.ControlDescLargeBrush)));

            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE));
            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE));
            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE));
            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE));
            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE));
            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE));
            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.FREE_ROTATION));
            advancedPage2.LeftColumn.Add(new ControlWithDescription("Ctrl + G", MyTexts.GetString(MySpaceTexts.SwitchBuilderMode)));

            // Get control for toggling the block size
            StringBuilder resizeBlockControl = null;
            MyControl cubeBuilderCubesizeModeControl = MyInput.Static.GetGameControl(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE);
            if (cubeBuilderCubesizeModeControl != null)
                cubeBuilderCubesizeModeControl.AppendBoundButtonNames(ref resizeBlockControl, unassignedText: MyInput.Static.GetUnassignedName());

            // Add block editing controls
            advancedPage2.LeftColumn.Add(new ControlWithDescription(MyTexts.Get(MyCommonTexts.MouseWheel), MyTexts.Get(MyCommonTexts.ControlName_ChangeBlockVariants)));
            if (cubeBuilderCubesizeModeControl != null)
                advancedPage2.LeftColumn.Add(new ControlWithDescription(resizeBlockControl, MyTexts.Get(cubeBuilderCubesizeModeControl.GetControlName())));

            advancedPage2.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SWITCH_LEFT));
            advancedPage2.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SWITCH_RIGHT));
            advancedPage2.RightColumn.Add(new ControlWithDescription(MyTexts.Get(MyCommonTexts.MouseWheel), MyTexts.Get(MySpaceTexts.ControlDescCameraZoom)));
            advancedPage2.RightColumn.Add(new ControlWithDescription(MyControlsSpace.SYMMETRY_SWITCH));
            advancedPage2.RightColumn.Add(new ControlWithDescription(MyControlsSpace.USE_SYMMETRY));
            advancedPage2.RightColumn.Add(new ControlWithDescription("", ""));
            advancedPage2.RightColumn.Add(new ControlWithDescription("Ctrl + H", MyTexts.GetString(MySpaceTexts.ControlDescNetgraph)));

            advancedPage2.RightColumn.Add(new ControlWithDescription("[", MyTexts.GetString(MyCommonTexts.ControlDescNextVoxelMaterial)));
            advancedPage2.RightColumn.Add(new ControlWithDescription("]", MyTexts.GetString(MyCommonTexts.ControlDescPreviousVoxelMaterial)));
            advancedPage2.RightColumn.Add(new ControlWithDescription("H", MyTexts.GetString(MyCommonTexts.ControlDescOpenVoxelHandSettings)));


            spectatorPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SPECTATOR_NONE));
            spectatorPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SPECTATOR_DELTA));
            spectatorPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SPECTATOR_FREE));
            spectatorPage.LeftColumn.Add(new ControlWithDescription(MyControlsSpace.SPECTATOR_STATIC));

            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaBlocks), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaBlocksDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaGrid), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaGridDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaConveyor), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaConveyorDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaGyro), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaGyroDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaOxygen), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaOxygenDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaAI), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaAIDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaScripts), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaScriptsDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaPhysics), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaPhysicsDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaRender), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaRenderDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaTextures), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaTexturesDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription("", ""));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaClearAndGeometryRender), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaClearAndGeometryRenderDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaTransparentPass), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaTransparentPassDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaLights), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaLightsDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaShadows), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaShadowsDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaRenderFoliage), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaRenderFoliageDescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaSSAO), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaSSAODescription)));
            performancePage.LeftColumn.Add(new ControlWithDescription(MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaPostProcess), MyTexts.GetString(MyCommonTexts.PerformanceWarningAreaPostProcessDescription)));
            

            //These keys are to be used just for developers or testing
            if (MyInput.Static.ENABLE_DEVELOPER_KEYS)
            {
                // Developers
                developerPage.LeftColumn.Add(new ControlWithDescription("Ctrl + NumPad0", "Enable/Disable debug input - global"));
                developerPage.LeftColumn.Add(new ControlWithDescription("Ctrl + NumPad1", "Enable/Disable debug input - character"));
                developerPage.LeftColumn.Add(new ControlWithDescription("Ctrl + NumPad2", "Enable/Disable debug input - Ondra"));
                developerPage.LeftColumn.Add(new ControlWithDescription("Ctrl + NumPad3", "Enable/Disable debug input - Peta"));
                developerPage.LeftColumn.Add(new ControlWithDescription("Ctrl + NumPad4", "Enable/Disable debug input - Martin"));
                developerPage.LeftColumn.Add(new ControlWithDescription("F11", "Game statistics"));
                developerPage.LeftColumn.Add(new ControlWithDescription("Shift + F11", "Frame info (FPS and stuff)"));
                developerPage.LeftColumn.Add(new ControlWithDescription("F12", "Debug screen"));

                developerPage.LeftColumn.Add(new ControlWithDescription("", ""));
                developerPage.LeftColumn.Add(new ControlWithDescription("", "Character Debug Input"));
                developerPage.LeftColumn.Add(new ControlWithDescription("U", "Add astronaut (current color)"));
                developerPage.LeftColumn.Add(new ControlWithDescription("NumPad7", "Switch to next ship"));
                developerPage.LeftColumn.Add(new ControlWithDescription("NumPad8", "Switch to next character"));

                developerPage.RightColumn.Add(new ControlWithDescription("", "Global Debug Input"));
                developerPage.RightColumn.Add(new ControlWithDescription("F6", "Switch between astronauts"));
                developerPage.RightColumn.Add(new ControlWithDescription("F7", "Switch to fixed pos. 3rd person camera"));
                developerPage.RightColumn.Add(new ControlWithDescription("F8", "Switch to spectator camera"));
                developerPage.RightColumn.Add(new ControlWithDescription("Ctrl + F8", "Reset spectator camera"));
                developerPage.RightColumn.Add(new ControlWithDescription("F9", "Switch to static 3rd person"));
                developerPage.RightColumn.Add(new ControlWithDescription("Ctrl + Space", "Move character to spectator position"));
                developerPage.RightColumn.Add(new ControlWithDescription("NumPad3", "Apply large force to controlled object."));
                developerPage.RightColumn.Add(new ControlWithDescription("NumPad6", "Apply small force to controlled object."));
                developerPage.RightColumn.Add(new ControlWithDescription("Ctrl + Shift + Z", "Save prefab"));

                developer2Page.LeftColumn.Add(new ControlWithDescription("", "Debug Input - Ondra"));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Ctrl + Insert", "Merge all grids."));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Ctrl + Delete", "Remove all characters."));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Ctrl + NumDecimal", "Remove all except controlled object."));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Ctrl + J", "Duplicate current grid."));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Ctrl + C", "Copy target (of cube builder gizmo)"));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Ctrl + X", "Cut target (of cube builder gizmo)"));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Ctrl + V", "Paste"));

                developer2Page.LeftColumn.Add(new ControlWithDescription("", ""));
                developer2Page.LeftColumn.Add(new ControlWithDescription("", "Debug Input - Peta"));
                developer2Page.LeftColumn.Add(new ControlWithDescription("Numpad0", "Add weapons, ammo and components to inventory."));

                developer2Page.RightColumn.Add(new ControlWithDescription("", "Debug Input - Martin"));
                developer2Page.RightColumn.Add(new ControlWithDescription("M + Numpad2", "Toggle thrusts on/off."));
                developer2Page.RightColumn.Add(new ControlWithDescription("M + Numpad3", "Toggle shooting turrets."));
                developer2Page.RightColumn.Add(new ControlWithDescription("M + Numpad4", "Reload definitions."));
                developer2Page.RightColumn.Add(new ControlWithDescription("M + Numpad5", "Show screen with all definition icons."));
                developer2Page.RightColumn.Add(new ControlWithDescription("M + Numpad6", "Remove all floating objects."));


            }


            AddProfilerControls(developerPage);

            m_currentPage = HelpPageEnum.Basic;
            CloseButtonEnabled = true;
            RecreateControls(true);
        }

        [Conditional(ProfilerShort.PerformanceProfilingSymbol)]
        private static void AddProfilerControls(HelpPage developerPage)
        {
            developerPage.RightColumn.Add(new ControlWithDescription("Alt + Num0", "Enable/Disable render profiler or leave current child node."));
            developerPage.RightColumn.Add(new ControlWithDescription("Alt + Num1-Num9", "Enter child node in render profiler"));
            developerPage.RightColumn.Add(new ControlWithDescription("Alt + Enter", "Pause/Unpause profiler"));
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(m_pageTitles[(int)m_currentPage].ToString());

            var page = m_pages[(int)m_currentPage];

            const float LINE_HEIGHT = 0.035f;
            const float VERTICAL_OFFSET = 0.10f;
            const float HORIZONTAL_OFFSET_LEFT_COLUMN = 0.15f;
            const float TEXT_SCALE = 1.0f;

            var originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            var descriptionAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;

            Vector2 controlPosition = -m_size.Value / 2.0f + new Vector2(HORIZONTAL_OFFSET_LEFT_COLUMN, VERTICAL_OFFSET);
            Vector2 descriptionPosition = -m_size.Value / 2.0f + new Vector2(HORIZONTAL_OFFSET_LEFT_COLUMN + 0.09f, VERTICAL_OFFSET);
            foreach (var line in page.LeftColumn)
            {
                Controls.Add(new MyGuiControlLabel(
                    position: controlPosition,
                    text: line.BoundButtons.ToString(),
                    textScale: TEXT_SCALE,
                    originAlign: originAlign,
                    font: line.LeftFont));
                Controls.Add(new MyGuiControlLabel(
                    position: descriptionPosition,
                    text: line.Description.ToString(),
                    textScale: TEXT_SCALE,
                    originAlign: descriptionAlign,
                    font: line.RightFont));
                if (line.LeftIcon != null)
                {
                    Controls.Add(new MyGuiControlImage(
                        position: controlPosition - new Vector2(0.05f, -0.002f),
                        size: new Vector2(0.02f, 0.02f),
                        textures: new string[] { line.LeftIcon.ToString() },
                        backgroundColor: line.LeftIconColor));
                }
                controlPosition.Y += LINE_HEIGHT;
                descriptionPosition.Y += LINE_HEIGHT;
            }

            const float HORIZONTAL_OFFSET_RIGHT_COLUMN = 0.07f;

            controlPosition = new Vector2(HORIZONTAL_OFFSET_RIGHT_COLUMN, VERTICAL_OFFSET - m_size.Value.Y / 2.0f);
            descriptionPosition = new Vector2(HORIZONTAL_OFFSET_RIGHT_COLUMN + 0.09f, VERTICAL_OFFSET - m_size.Value.Y / 2.0f);

            foreach (var line in page.RightColumn)
            {
                Controls.Add(new MyGuiControlLabel(
                    position: controlPosition,
                    text: line.BoundButtons.ToString(),
                    textScale: TEXT_SCALE,
                    originAlign: originAlign,
                    font: line.LeftFont));
                Controls.Add(new MyGuiControlLabel(
                    position: descriptionPosition,
                    text: line.Description.ToString(),
                    textScale: TEXT_SCALE,
                    originAlign: descriptionAlign,
                    font: line.RightFont));
                if (line.LeftIcon != null)
                {
                    Controls.Add(new MyGuiControlImage(
                        position: controlPosition - new Vector2(0.05f, -0.002f),
                        size: new Vector2(0.02f, 0.02f),
                        textures: new string[] { line.LeftIcon.ToString() },
                        backgroundColor: line.LeftIconColor));
                }
                controlPosition.Y += LINE_HEIGHT;
                descriptionPosition.Y += LINE_HEIGHT;
            }

            // Create bottom buttons.
            {
                var position = new Vector2(-0.38f, 0.43f);
                Controls.Add(MakeButton(position, MyCommonTexts.SteamGuide, OnSteamGuideClick));
                position.X = 0.18f;
                Controls.Add(MakeButton(position, MyCommonTexts.PreviousPage, OnPrevPageClick));
                position.X += 0.2f;
                Controls.Add(MakeButton(position, MyCommonTexts.NextPage, OnNextPageClick));
            }
        }

        private MyGuiControlButton MakeButton(Vector2 position, MyStringId text, Action<MyGuiControlButton> onClick)
        {
            var size = MyGuiConstants.BACK_BUTTON_SIZE;
            var bgColor = MyGuiConstants.BACK_BUTTON_BACKGROUND_COLOR;
            var textColor = MyGuiConstants.BACK_BUTTON_TEXT_COLOR;
            var textScale = MyGuiConstants.DEFAULT_TEXT_SCALE;
            return new MyGuiControlButton(
                position: position,
                size: size,
                colorMask: bgColor,
                text: MyTexts.Get(text),
                textScale: textScale,
                onButtonClick: onClick);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenHelp";
        }

        private void OnCloseClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
        }

        private void OnNextPageClick(MyGuiControlButton sender)
        {
            ShowNextPage();
        }

        private void OnPrevPageClick(MyGuiControlButton sender)
        {
            ShowPreviousPage();
        }

        private void OnSteamGuideClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.OpenUrlWithFallback(MySteamConstants.URL_GUIDE_DEFAULT, "Steam Guide");
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            base.HandleUnhandledInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F1) || MyInput.Static.IsNewKeyPressed(MyKeys.PageDown))
                ShowNextPage();
            else if (MyInput.Static.IsNewKeyPressed(MyKeys.PageUp))
                ShowPreviousPage();
        }

        private void ShowPreviousPage()
        {
            int lastPage = CalcLastPageIdx();
            int current = (int)m_currentPage;
            current = (current == 0) ? lastPage : current - 1;
            m_currentPage = (HelpPageEnum)current;
            RecreateControls(false);
        }

        private void ShowNextPage()
        {
            int lastPage = CalcLastPageIdx();
            int current = (int)m_currentPage;
            current = (current + 1) % (lastPage + 1);
            m_currentPage = (HelpPageEnum)current;
            RecreateControls(false);
        }

        private int CalcLastPageIdx()
        {
            // Developer page is always last. Skip it if we didn't enable developer keys.
            return (MyInput.Static.ENABLE_DEVELOPER_KEYS) ? m_pages.Length - 1
                                                      : (int)HelpPageEnum.Spectator;
        }

    }
}
