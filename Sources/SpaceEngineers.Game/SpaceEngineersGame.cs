using System;
using Multiplayer;
using Sandbox;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.Render;
using SpaceEngineers.Game.GUI;
using SpaceEngineers.Game.VoiceChat;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox.Engine.Voxels;
using SpaceEngineers.Game.ModAPI;
using VRage.Compiler;
using VRage.Data.Audio;
using VRage.FileSystem;
using VRage.Game;
using VRage.Utils;
using VRageRender;
using World;
using VRage.Input;

namespace SpaceEngineers.Game
{
    public partial class SpaceEngineersGame : MySandboxGame
    {
        const int SE_VERSION = 01146006;

        #region Constructor

        public SpaceEngineersGame(VRageGameServices services, string[] commandlineArgs)
            : base(services, commandlineArgs)
        {
            MySandboxGame.GameCustomInitialization = new MySpaceGameCustomInitialization();
        }
        #endregion

        public static void SetupBasicGameInfo()
        {
            MyPerGameSettings.BasicGameInfo.GameVersion = SE_VERSION;

            MyPerGameSettings.BasicGameInfo.GameName = "Space Engineers";
            MyPerGameSettings.BasicGameInfo.GameNameSafe = "SpaceEngineers";
            MyPerGameSettings.BasicGameInfo.ApplicationName = "SpaceEngineers";
            MyPerGameSettings.BasicGameInfo.GameAcronym = "SE";
            MyPerGameSettings.BasicGameInfo.MinimumRequirementsWeb = "http://www.spaceengineersgame.com";
            MyPerGameSettings.BasicGameInfo.SplashScreenImage = "..\\Content\\Textures\\Logo\\splashscreen.png";
        }

        public static void SetupPerGameSettings()
        {
            MyPerGameSettings.Game = GameEnum.SE_GAME;
            MyPerGameSettings.GameIcon = "SpaceEngineers.ico";
            MyPerGameSettings.EnableGlobalGravity = false;
            MyPerGameSettings.GameModAssembly = "SpaceEngineers.Game.dll";
            MyPerGameSettings.GameModObjBuildersAssembly = "SpaceEngineers.ObjectBuilders.dll";
            MyPerGameSettings.OffsetVoxelMapByHalfVoxel = true;
            MyPerGameSettings.EnablePregeneratedAsteroidHack = true;
            MySandboxGame.ConfigDedicated = new MyConfigDedicated<MyObjectBuilder_SessionSettings>("SpaceEngineers-Dedicated.cfg");
            MySandboxGame.GameCustomInitialization = new MySpaceGameCustomInitialization();
            MyPerGameSettings.ShowObfuscationStatus = false;
            MyPerGameSettings.UseNewDamageEffects = true;

            //audio
            MyPerGameSettings.UseVolumeLimiter = MyFakes.ENABLE_NEW_SOUNDS && MyFakes.ENABLE_REALISTIC_LIMITER;
            MyPerGameSettings.UseSameSoundLimiter = true;
            MyPerGameSettings.UseMusicController = true;

            MyPerGameSettings.Destruction = false;
            //MyPerGameSettings.ConstantVoxelAmbient = -0.35f;
            MyFakes.ENABLE_SUN_BILLBOARD = true;

            MyPerGameSettings.MainMenuTrack = new MyMusicTrack()
            {
                TransitionCategory = MyStringId.GetOrCompute("NoRandom"),
                MusicCategory = MyStringId.GetOrCompute("MusicMenu")
            };

            MyPerGameSettings.BallFriendlyPhysics = false;

            MyPerGameSettings.BotFactoryType = typeof(SpaceEngineers.Game.AI.MySpaceBotFactory);

            MyPerGameSettings.ControlMenuInitializerType = typeof(MySpaceControlMenuInitializer);

            MyPerGameSettings.EnableScenarios = true;
            MyPerGameSettings.EnableTutorials = true;

            MyPerGameSettings.EnableJumpDrive = true;
            MyPerGameSettings.EnableShipSoundSystem = true;
			MyFakes.ENABLE_PLANETS_JETPACK_LIMIT_IN_CREATIVE = true;
			MyFakes.ENABLE_DRIVING_PARTICLES = true;

            MyPerGameSettings.EnablePathfinding = false;
            MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS_HELPERS = true;

            // RAGDOLL PARAMATERS
            // TODO: after the ragdoll models are correctly configured this can be removed..
            MyFakes.ENABLE_RAGDOLL_DEFAULT_PROPERTIES = true;
            //MyPerGameSettings.EnableRagdollModels = false;
            MyPerGameSettings.EnableRagdollInJetpack = true;
            //MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION = false;            

            MyPerGameSettings.EnableKinematicMPCharacter = false;

            MyPerGameSettings.GUI.OptionsScreen = typeof(MyGuiScreenOptionsSpace);
            MyPerGameSettings.GUI.CreateFactionScreen = typeof(MyGuiScreenCreateOrEditFactionSpace);
            MyPerGameSettings.DefaultGraphicsRenderer = MySandboxGame.DirectX11RendererKey;

            MyPerGameSettings.EnableWelderAutoswitch = true;
			MyPerGameSettings.InventoryMass = true;
			MyPerGameSettings.CompatHelperType = typeof(MySpaceSessionCompatHelper);

            MyPerGameSettings.GUI.MainMenuBackgroundVideos = new string[] 
            {
                @"Videos\Background01_720p.wmv",
                @"Videos\Background02_720p.wmv",
                @"Videos\Background03_720p.wmv",
                @"Videos\Background04_720p.wmv",
                @"Videos\Background05_720p.wmv",
                @"Videos\Background06_720p.wmv",
                @"Videos\Background07_720p.wmv",
                @"Videos\Background08_720p.wmv",
                @"Videos\Background09_720p.wmv",
                @"Videos\Background10_720p.wmv",
                @"Videos\Background11_720p.wmv",
                @"Videos\Background12_720p.wmv",
           };

			SetupRender();
            FillCredits();

            MyPerGameSettings.VoiceChatEnabled = false;
            MyPerGameSettings.VoiceChatLogic = typeof(MyVoiceChatLogic);
            MyRenderSettings.PerInstanceLods = false;

			MyPerGameSettings.ClientStateType = typeof(MySpaceClientState);
            //MyFakes.ENABLE_HAVOK_MULTITHREADING = true;
            MyVoxelPhysicsBody.UseLod1VoxelPhysics = true;

            MyPerGameSettings.EnableAi = true;
            MyPerGameSettings.EnablePathfinding = true;

            // This must be done last
            MyFakesLocal.SetupLocalPerGameSettings();
        }

		public static void SetupRender()
		{
			// Video settings manager has not been initialized yet, so accessing config file directly.
			if (MySandboxGame.Config != null && // Dedicated server calls this as first thing, even before it has loaded config ... doesn't need render though.
				MySandboxGame.Config.GraphicsRenderer == MySandboxGame.DirectX11RendererKey)
			{
				MyPostProcessVolumetricSSAO2.MinRadius = 0.115f;
				MyPostProcessVolumetricSSAO2.MaxRadius = 25;
				MyPostProcessVolumetricSSAO2.RadiusGrowZScale = 1.007f;
				MyPostProcessVolumetricSSAO2.Falloff = 3.08f;
				MyPostProcessVolumetricSSAO2.Bias = 0.25f;
				MyPostProcessVolumetricSSAO2.Contrast = 2.617f;
				MyPostProcessVolumetricSSAO2.NormValue = 0.075f;

				MyPostprocessSettingsWrapper.Settings.Brightness = 0;
				MyPostprocessSettingsWrapper.Settings.Contrast = 0;
				MyPostprocessSettingsWrapper.Settings.LuminanceExposure = 0.0f;
				MyPostprocessSettingsWrapper.Settings.BloomExposure = 0;
				MyPostprocessSettingsWrapper.Settings.BloomMult = 0.1f;
				MyPostprocessSettingsWrapper.Settings.EyeAdaptationTau = 3;
				MyPostprocessSettingsWrapper.Settings.MiddleGreyAt0 = 0.068f;
				MyPostprocessSettingsWrapper.Settings.MiddleGreyCurveSharpness = 4.36f;
				MyPostprocessSettingsWrapper.Settings.LogLumThreshold = -5.0f;
                MyPostprocessSettingsWrapper.Settings.NightLogLumThreshold = MyPostprocessSettingsWrapper.Settings.LogLumThreshold;
				MyPostprocessSettingsWrapper.Settings.BlueShiftRapidness = 0;
				MyPostprocessSettingsWrapper.Settings.BlueShiftScale = 0;
				MyPostprocessSettingsWrapper.Settings.Tonemapping_A = 0.147f;
				MyPostprocessSettingsWrapper.Settings.Tonemapping_B = 0.120f;
				MyPostprocessSettingsWrapper.Settings.Tonemapping_C = 0.321f;
				MyPostprocessSettingsWrapper.Settings.Tonemapping_D = 0.699f;
				MyPostprocessSettingsWrapper.Settings.Tonemapping_E = 0.001f;
				MyPostprocessSettingsWrapper.Settings.Tonemapping_F = 0.160f;

				MyPostprocessSettingsWrapper.PlanetSettings = MyPostprocessSettingsWrapper.Settings;
				MyPostprocessSettingsWrapper.PlanetSettings.LuminanceExposure = 1.2f;

                MyRenderProxy.Settings.ShadowCascadeCount = 6;
			}

			MyRenderProxy.Settings.ShadowFadeoutMultiplier = 0.0f;
		    MyRenderProxy.Settings.UpdateCascadesEveryFrame = false;
		    MyRenderProxy.Settings.ShadowCascadeMaxDistance = 8000f;
		    MyRenderProxy.Settings.ShadowCascadeZOffset = 6000f;
		    MyRenderProxy.Settings.ShadowCascadeSpreadFactor = 0f;
		    MyRenderProxy.Settings.GrassMaxDrawDistance = 400;
            MyRenderProxy.Settings.DrawMergeInstanced = false;
		}

        static void FillCredits()
        {
            //  Director
            MyCreditsDepartment director = new MyCreditsDepartment("Produced and Directed By");
            MyPerGameSettings.Credits.Departments.Add(director);
            director.Persons = new List<MyCreditsPerson>();
            director.Persons.Add(new MyCreditsPerson("MAREK ROSA"));

            //  Lead Programmers
            MyCreditsDepartment leadProgrammers = new MyCreditsDepartment("Lead Programmers");
            MyPerGameSettings.Credits.Departments.Add(leadProgrammers);
            leadProgrammers.Persons = new List<MyCreditsPerson>();
            leadProgrammers.Persons.Add(new MyCreditsPerson("PETR MINARIK"));
            leadProgrammers.Persons.Add(new MyCreditsPerson("ONDREJ PETRZILKA"));

            //  Lead Designers
            MyCreditsDepartment leadDesigners = new MyCreditsDepartment("Lead Designer");
            MyPerGameSettings.Credits.Departments.Add(leadDesigners);
            leadDesigners.Persons = new List<MyCreditsPerson>();
            leadDesigners.Persons.Add(new MyCreditsPerson("TOMAS RAMPAS"));

            //  Project Managers
            MyCreditsDepartment projectManagers = new MyCreditsDepartment("Project Manager");
            MyPerGameSettings.Credits.Departments.Add(projectManagers);
            projectManagers.Persons = new List<MyCreditsPerson>();
            projectManagers.Persons.Add(new MyCreditsPerson("TOMAS PSENICKA"));

            //  Programmers
            MyCreditsDepartment programmers = new MyCreditsDepartment("Programmers");
            MyPerGameSettings.Credits.Departments.Add(programmers);
            programmers.Persons = new List<MyCreditsPerson>();
            programmers.Persons.Add(new MyCreditsPerson("CESTMIR HOUSKA"));
            programmers.Persons.Add(new MyCreditsPerson("JAN NEKVAPIL"));
            programmers.Persons.Add(new MyCreditsPerson("DUSAN ANDRAS"));
            programmers.Persons.Add(new MyCreditsPerson("DANIEL ILHA"));
            programmers.Persons.Add(new MyCreditsPerson("MARKO KORHONEN"));            
            programmers.Persons.Add(new MyCreditsPerson("ALEX FLOREA"));
            programmers.Persons.Add(new MyCreditsPerson("JAN VEBERSIK"));
            programmers.Persons.Add(new MyCreditsPerson("MICHAL WROBEL"));
            programmers.Persons.Add(new MyCreditsPerson("ONDREJ MAZANY"));
            programmers.Persons.Add(new MyCreditsPerson("RADOVAN KOTRLA"));
            programmers.Persons.Add(new MyCreditsPerson("JAKUB TYRCHA"));
            programmers.Persons.Add(new MyCreditsPerson("PEDRO VERAS DA SILVA"));
            programmers.Persons.Add(new MyCreditsPerson("MARTIN KROSLAK"));
            programmers.Persons.Add(new MyCreditsPerson("STANISLAV \"NOBRAIN\" KRAL"));

            //  Artists
            MyCreditsDepartment artists = new MyCreditsDepartment("Artists");
            MyPerGameSettings.Credits.Departments.Add(artists);
            artists.Persons = new List<MyCreditsPerson>();
            artists.Persons.Add(new MyCreditsPerson("ANTON \"TOTAL\" BAUER"));
            artists.Persons.Add(new MyCreditsPerson("NATIQ AGHAYEV"));
            artists.Persons.Add(new MyCreditsPerson("JAN GOLMIC"));
            artists.Persons.Add(new MyCreditsPerson("RENE RODER"));
            artists.Persons.Add(new MyCreditsPerson("JIRI RUZICKA"));
            artists.Persons.Add(new MyCreditsPerson("PAVEL OCOVAJ"));
            artists.Persons.Add(new MyCreditsPerson("RASTKO STANOJEVIC"));
            artists.Persons.Add(new MyCreditsPerson("SLOBODAN STEVIC"));
            artists.Persons.Add(new MyCreditsPerson("ARTEM TARASSENKO"));
            artists.Persons.Add(new MyCreditsPerson("ADAM TOWARD"));
            artists.Persons.Add(new MyCreditsPerson("LUKAS CHRAPEK"));
            artists.Persons.Add(new MyCreditsPerson("NIKITA OLHOVSKIS"));
            artists.Persons.Add(new MyCreditsPerson("KEVIN STUTH"));
            


            // Sound design
            MyCreditsDepartment soundDesign = new MyCreditsDepartment("Sound Design");
            MyPerGameSettings.Credits.Departments.Add(soundDesign);
            soundDesign.Persons = new List<MyCreditsPerson>();
            soundDesign.Persons.Add(new MyCreditsPerson("LUKAS TVRDON"));

            // Music
            MyCreditsDepartment music = new MyCreditsDepartment("Music");
            MyPerGameSettings.Credits.Departments.Add(music);
            music.Persons = new List<MyCreditsPerson>();
            music.Persons.Add(new MyCreditsPerson("KAREL ANTONIN"));
            music.Persons.Add(new MyCreditsPerson("\"Spazzmatica Polka\" Kevin MacLeod (incompetech.com) "));
            music.Persons.Add(new MyCreditsPerson("MAREK MRKVICKA"));
            music.Persons.Add(new MyCreditsPerson("ANNA KALHAUSOVA (cello)"));
            music.Persons.Add(new MyCreditsPerson("MARIE SVOBODOVA (vocals)"));

            //  Game Designers
            MyCreditsDepartment gameDesigners = new MyCreditsDepartment("Game Designers");
            MyPerGameSettings.Credits.Departments.Add(gameDesigners);
            gameDesigners.Persons = new List<MyCreditsPerson>();
            gameDesigners.Persons.Add(new MyCreditsPerson("LUKAS JANDIK"));
            gameDesigners.Persons.Add(new MyCreditsPerson("ADAM WILLIAMS"));
            gameDesigners.Persons.Add(new MyCreditsPerson("SIMON LESKA"));

            //  Community & PR Managers
            MyCreditsDepartment managers = new MyCreditsDepartment("Community & PR Manager");
            MyPerGameSettings.Credits.Departments.Add(managers);
            managers.Persons = new List<MyCreditsPerson>();
            managers.Persons.Add(new MyCreditsPerson("GEORGE MAMAKOS"));

            //  Testers
            MyCreditsDepartment testers = new MyCreditsDepartment("Testers");
            MyPerGameSettings.Credits.Departments.Add(testers);
            testers.Persons = new List<MyCreditsPerson>();
            testers.Persons.Add(new MyCreditsPerson("MARKETA JAROSOVA"));
            testers.Persons.Add(new MyCreditsPerson("VACLAV NOVOTNY"));
            testers.Persons.Add(new MyCreditsPerson("MAREK OBRSAL"));
            testers.Persons.Add(new MyCreditsPerson("DUSAN REPIK"));
            testers.Persons.Add(new MyCreditsPerson("ALES KOZAK"));
            testers.Persons.Add(new MyCreditsPerson("CHARLES WINTERS"));
            testers.Persons.Add(new MyCreditsPerson("MICHAL ZAVADAK"));


            //  Community Manager
            MyCreditsDepartment communityManagers = new MyCreditsDepartment("Community Managers");
            MyPerGameSettings.Credits.Departments.Add(communityManagers);
            communityManagers.Persons = new List<MyCreditsPerson>();
            communityManagers.Persons.Add(new MyCreditsPerson("ADAM TOWARD"));
            communityManagers.Persons.Add(new MyCreditsPerson("AUSTEN LINDSAY"));
            communityManagers.Persons.Add(new MyCreditsPerson("Dr Vagax"));
            communityManagers.Persons.Add(new MyCreditsPerson("FILIP \"Tazoo\" JULIN"));
            communityManagers.Persons.Add(new MyCreditsPerson("RocketRacer"));
            communityManagers.Persons.Add(new MyCreditsPerson("NICK \"Drakon\" MILLER"));
            communityManagers.Persons.Add(new MyCreditsPerson("SEBASTIAN SCHNEIDER"));

          
            //  Translators
            MyCreditsDepartment translators = new MyCreditsDepartment("Translators");
            MyPerGameSettings.Credits.Departments.Add(translators);
            translators.Persons = new List<MyCreditsPerson>();
            translators.Persons.Add(new MyCreditsPerson("George Grivas"));
            translators.Persons.Add(new MyCreditsPerson("Олег \"AaLeSsHhKka\" Цюпка"));
            translators.Persons.Add(new MyCreditsPerson("Maxim \"Ma)(imuM\" Lyashuk"));
            translators.Persons.Add(new MyCreditsPerson("Axazel"));
            translators.Persons.Add(new MyCreditsPerson("Baly94"));
            translators.Persons.Add(new MyCreditsPerson("Dyret"));
            translators.Persons.Add(new MyCreditsPerson("gon.gged"));
            translators.Persons.Add(new MyCreditsPerson("Huberto"));
            translators.Persons.Add(new MyCreditsPerson("HunterNephilim"));
            translators.Persons.Add(new MyCreditsPerson("madafaka"));
            translators.Persons.Add(new MyCreditsPerson("nintendo22"));
            translators.Persons.Add(new MyCreditsPerson("Quellix"));
            translators.Persons.Add(new MyCreditsPerson("raviool"));

            MyCreditsDepartment explorationContentCreators = new MyCreditsDepartment("Exploration content creators");
            MyPerGameSettings.Credits.Departments.Add(explorationContentCreators);
            explorationContentCreators.Persons = new List<MyCreditsPerson>();
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Yatem"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"LorenzoPingue"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Weiss"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"smiffyjoebob"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"GEC"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"XD1LLW33DX"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"The Senate"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Thokari"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"zure87"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Gompasta"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"PeterHammerman"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"The_7th_Gamer (GameGunner5)"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"NeoValkyrion"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Tyriosh"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Tainja"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Agronom"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"govrom"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"vSure"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"G-Lu"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"snowshoe_hare"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"InfestedHydra"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Kovendon "));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Noxy"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Spetzy"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"SOLDIER 1st class"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Lord_matthew82"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Невероятный Алк"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Kaii-Killer"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"RelicSage"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"ErAgon"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"-=\\\Raeffi///=-"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"[MGE]LeonserG"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Lt Losho"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"jerryfanfan"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"Stone Cold Jane Austen"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"NeXiZ"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson(@"KillYaSoon"));
            explorationContentCreators.Persons.Add(new MyCreditsPerson("Dorian \"JD.Horx\" Flores"));

            MyCreditsDepartment modContributors = new MyCreditsDepartment("Mod Contributors");
            MyPerGameSettings.Credits.Departments.Add(modContributors);
            modContributors.Persons = new List<MyCreditsPerson>();
            modContributors.Persons.Add(new MyCreditsPerson("Darth Biomech (fighter cockpit)"));
            modContributors.Persons.Add(new MyCreditsPerson("Night Lone (programmable block extensions)"));
            modContributors.Persons.Add(new MyCreditsPerson("Mexmer (transparent GUI)"));
            modContributors.Persons.Add(new MyCreditsPerson("JD.Horx (IMDC faction fleet)"));

            

            //  Final
            MyCreditsDepartment final = new MyCreditsDepartment("For more information see");
            MyPerGameSettings.Credits.Departments.Add(final);
            final.Persons = new List<MyCreditsPerson>();
            final.Persons.Add(new MyCreditsPerson("www.SpaceEngineersGame.com"));
            final.Persons.Add(new MyCreditsPerson("Like us on Facebook: www.facebook.com/SpaceEngineers"));
            final.Persons.Add(new MyCreditsPerson("Follow us on Twitter: twitter.com/SpaceEngineersG"));


            MyCreditsNotice vrageNotice = new MyCreditsNotice();
            vrageNotice.LogoTexture = "Textures\\Logo\\vrage_logo_2_0_white.dds";
            vrageNotice.LogoScale = 0.8f;
            vrageNotice.CreditNoticeLines.Add(new StringBuilder("Powered by VRAGE 2.0"));
            vrageNotice.CreditNoticeLines.Add(new StringBuilder("Copyright © 2013-2015 KEEN SWH LTD."));
            vrageNotice.CreditNoticeLines.Add(new StringBuilder("Space Engineers® and VRAGE™ are trademarks of KEEN SWH LTD."));
            vrageNotice.CreditNoticeLines.Add(new StringBuilder("www.keenswh.com"));
            MyPerGameSettings.Credits.CreditNotices.Add(vrageNotice);

            MyCreditsNotice havokNotice = new MyCreditsNotice();
            havokNotice.LogoTexture = "Textures\\Logo\\havok.dds";
            havokNotice.LogoScale = 0.65f;
            havokNotice.CreditNoticeLines.Add(new StringBuilder("“Space Engineers” uses Havok®."));
            havokNotice.CreditNoticeLines.Add(new StringBuilder("©Copyright 1999-2008 Havok.com, Inc (and its Licensors). All Rights Reserved."));
            havokNotice.CreditNoticeLines.Add(new StringBuilder("See www.havok.com for details."));
            MyPerGameSettings.Credits.CreditNotices.Add(havokNotice);


            SetupSecrets();

            // Must be initialized after secrets are set
            if (MyFakes.ENABLE_INFINARIO)// if (MyFinalBuildConstants.IS_OFFICIAL || MyFakes.ENABLE_INFINARIO)
            {
                MyPerGameSettings.AnalyticsTracker = MyInfinarioAnalytics.Instance;
            }
        }

        static partial void SetupSecrets();

        protected override void InitInput()
        {
            base.InitInput();

            // Add signals render mode toggle control
            MyGuiDescriptor helper = new MyGuiDescriptor(MyCommonTexts.ControlName_ToggleSignalsMode, MyCommonTexts.ControlName_ToggleSignalsMode_Tooltip);
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOGGLE_SIGNALS, helper);
            MyControl control = new MyControl(MyControlsSpace.TOGGLE_SIGNALS, helper.NameEnum, MyGuiControlTypeEnum.Systems1, null, MyKeys.H, description: helper.DescriptionEnum);
            MyInput.Static.AddDefaultControl(MyControlsSpace.TOGGLE_SIGNALS, control);

            // Add cube size build mode for cube builder control
            helper = new MyGuiDescriptor(MyCommonTexts.ControlName_CubeSizeMode, MyCommonTexts.ControlName_CubeSizeMode_Tooltip);
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE, helper);
            control = new MyControl(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE, helper.NameEnum, MyGuiControlTypeEnum.Systems3, null, MyKeys.R, description: helper.DescriptionEnum);
            MyInput.Static.AddDefaultControl(MyControlsSpace.CUBE_BUILDER_CUBESIZE_MODE, control);

    }
}
}
