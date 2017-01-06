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
        const int SE_VERSION = 01169002;

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
            MyPerGameSettings.EnableResearch = true;

            //audio
            MyPerGameSettings.UseVolumeLimiter = MyFakes.ENABLE_NEW_SOUNDS && MyFakes.ENABLE_REALISTIC_LIMITER;
            MyPerGameSettings.UseSameSoundLimiter = true;
            MyPerGameSettings.UseMusicController = true;
            MyPerGameSettings.UseReverbEffect = true;

            MyPerGameSettings.Destruction = false;
            //MyPerGameSettings.ConstantVoxelAmbient = -0.35f;
            MyFakes.ENABLE_SUN_BILLBOARD = true;

            MyPerGameSettings.MainMenuTrack = new MyMusicTrack()
            {
                TransitionCategory = MyStringId.GetOrCompute("NoRandom"),
                MusicCategory = MyStringId.GetOrCompute("MusicMenu")
            };

            MyPerGameSettings.BallFriendlyPhysics = false;

            if (MyFakes.ENABLE_CESTMIR_PATHFINDING)
            {
                MyPerGameSettings.PathfindingType = typeof(Sandbox.Game.AI.Pathfinding.MyPathfinding);
            }
            else
                MyPerGameSettings.PathfindingType = typeof(Sandbox.Game.AI.Pathfinding.MyRDPathfinding);

            MyPerGameSettings.BotFactoryType = typeof(SpaceEngineers.Game.AI.MySpaceBotFactory);

            MyPerGameSettings.ControlMenuInitializerType = typeof(MySpaceControlMenuInitializer);

            MyPerGameSettings.EnableScenarios = true;

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
            MyPerGameSettings.GUI.PerformanceWarningScreen = typeof(MyGuiScreenPerformanceWarnings);
            MyPerGameSettings.GUI.CreateFactionScreen = typeof(MyGuiScreenCreateOrEditFactionSpace);
            MyPerGameSettings.GUI.MainMenu = typeof(MyGuiScreenMainMenu);
            MyPerGameSettings.DefaultGraphicsRenderer = MySandboxGame.DirectX11RendererKey;

            MyPerGameSettings.EnableWelderAutoswitch = true;
			MyPerGameSettings.InventoryMass = true;
			MyPerGameSettings.CompatHelperType = typeof(MySpaceSessionCompatHelper);

            MyPerGameSettings.GUI.MainMenuBackgroundVideos = new string[] 
            {
                @"Videos\Background01_720p.wmv",
                @"Videos\Background02_720p.wmv",
                //@"Videos\Background03_720p.wmv",
            //    @"Videos\Background04_720p.wmv",
             //   @"Videos\Background05_720p.wmv",
             //   @"Videos\Background06_720p.wmv",
            //    @"Videos\Background07_720p.wmv",
            //    @"Videos\Background08_720p.wmv",
                @"Videos\Background09_720p.wmv",
                @"Videos\Background10_720p.wmv",
           //     @"Videos\Background11_720p.wmv",
                @"Videos\Background12_720p.wmv",
                @"Videos\Background13_720p.wmv",
            };

			SetupRender();
            FillCredits();

            /*MyPerGameSettings.DefaultRenderDeviceSettings = new MyRenderDeviceSettings()
            {
                AdapterOrdinal = -1,
                BackBufferHeight = 800,
                BackBufferWidth = 1280,
                WindowMode = MyWindowModeEnum.FullscreenWindow,
                RefreshRate = 60000,
                VSync = false,
            };*/

            MyPerGameSettings.VoiceChatEnabled = true;
            MyPerGameSettings.VoiceChatLogic = typeof(MyVoiceChatLogic);

			MyPerGameSettings.ClientStateType = typeof(MySpaceClientState);
            //MyFakes.ENABLE_HAVOK_MULTITHREADING = true;
            MyVoxelPhysicsBody.UseLod1VoxelPhysics = true;

            MyPerGameSettings.EnableAi = true;
            MyPerGameSettings.EnablePathfinding = true;

            // This must be done last
            MyFakesLocal.SetupLocalPerGameSettings();
        }

        [Obsolete]
        // REMOVE-ME: Move all settings in the MyEnvironmentDefinition, and store them in MySector
		public static void SetupRender()
		{
		    MyRenderProxy.Settings.GrassMaxDrawDistance = 400;
            MyRenderProxy.Settings.DrawMergeInstanced = false; 
            MyRenderProxy.Settings.PerInstanceLods = false;
            MyRenderProxy.Settings.UseGeometryArrayTextures = false;
		}

        static void FillCredits()
        {
            //  Director
            MyCreditsDepartment director = new MyCreditsDepartment("Executive Producer");
            MyPerGameSettings.Credits.Departments.Add(director);
            director.Persons = new List<MyCreditsPerson>();
            director.Persons.Add(new MyCreditsPerson("MAREK ROSA"));

            //  Producer
            MyCreditsDepartment producer = new MyCreditsDepartment("Lead Producer");
            MyPerGameSettings.Credits.Departments.Add(producer);
            producer.Persons = new List<MyCreditsPerson>();
            producer.Persons.Add(new MyCreditsPerson("PETR MINARIK"));


            //  Producer
            MyCreditsDepartment producerAss = new MyCreditsDepartment("Assistent Producer");
            MyPerGameSettings.Credits.Departments.Add(producerAss);
            producerAss.Persons = new List<MyCreditsPerson>();
            producerAss.Persons.Add(new MyCreditsPerson("ALES KOZAK"));

            //  Lead Programmers
            MyCreditsDepartment leadProgrammers = new MyCreditsDepartment("Lead Programmers");
            MyPerGameSettings.Credits.Departments.Add(leadProgrammers);
            leadProgrammers.Persons = new List<MyCreditsPerson>();
            leadProgrammers.Persons.Add(new MyCreditsPerson("PETR MINARIK"));
            leadProgrammers.Persons.Add(new MyCreditsPerson("JAN \"CENDA\" HLOUSEK"));

            //  Lead Designers
            //MyCreditsDepartment leadDesigners = new MyCreditsDepartment("Game Designers");
            //MyPerGameSettings.Credits.Departments.Add(leadDesigners);
            //leadDesigners.Persons = new List<MyCreditsPerson>();

            //  Project Managers
            //MyCreditsDepartment projectManagers = new MyCreditsDepartment("Project Manager");
            //MyPerGameSettings.Credits.Departments.Add(projectManagers);
            //projectManagers.Persons = new List<MyCreditsPerson>();
            //projectManagers.Persons.Add(new MyCreditsPerson("TOMAS PSENICKA"));

                        //  Programmers
            MyCreditsDepartment programmers = new MyCreditsDepartment("Programmers");
            MyPerGameSettings.Credits.Departments.Add(programmers);
            programmers.Persons = new List<MyCreditsPerson>();
            programmers.Persons.Add(new MyCreditsPerson("JAN NEKVAPIL"));
            programmers.Persons.Add(new MyCreditsPerson("MICHAL ZAK"));
            programmers.Persons.Add(new MyCreditsPerson("SANDRA LENARDOVA"));
            programmers.Persons.Add(new MyCreditsPerson("MICHAL KUCIS"));
            programmers.Persons.Add(new MyCreditsPerson("TOMAS KOSEK"));
            programmers.Persons.Add(new MyCreditsPerson("LUKAS VILIM"));                        
            programmers.Persons.Add(new MyCreditsPerson("ALES BRICH"));
            programmers.Persons.Add(new MyCreditsPerson("JOAO CARIAS"));
            programmers.Persons.Add(new MyCreditsPerson("GREGORY KONTADAKIS"));
            programmers.Persons.Add(new MyCreditsPerson("IVAN BARAN"));
            programmers.Persons.Add(new MyCreditsPerson("PAVEL CHLAD"));
            programmers.Persons.Add(new MyCreditsPerson("BRANT MARTIN"));

            //  Additional Programmers
            MyCreditsDepartment additionalProgrammers = new MyCreditsDepartment("Additional Programmers");
            MyPerGameSettings.Credits.Departments.Add(additionalProgrammers);
            additionalProgrammers.Persons.Add(new MyCreditsPerson("JAN VEBERSIK"));
            additionalProgrammers.Persons.Add(new MyCreditsPerson("TIM TOXOPEUS"));
            additionalProgrammers.Persons.Add(new MyCreditsPerson("DANIEL ILHA"));            
            additionalProgrammers.Persons.Add(new MyCreditsPerson("MIRO FARKAS"));


            //  Artists
            MyCreditsDepartment artists = new MyCreditsDepartment("Artists");
            MyPerGameSettings.Credits.Departments.Add(artists);
            artists.Persons = new List<MyCreditsPerson>();
            artists.Persons.Add(new MyCreditsPerson("NATIQ AGHAYEV"));
            artists.Persons.Add(new MyCreditsPerson("ANTON \"TOTAL\" BAUER"));
            artists.Persons.Add(new MyCreditsPerson("JAN TRAUSKE"));
            artists.Persons.Add(new MyCreditsPerson("KRISTIAAN RENAERTS"));
            artists.Persons.Add(new MyCreditsPerson("ABDULAZIZ ALDIGS"));

            MyCreditsDepartment additionalArtists = new MyCreditsDepartment("Additional Artists");
            MyPerGameSettings.Credits.Departments.Add(additionalArtists);
            additionalArtists.Persons = new List<MyCreditsPerson>();
            additionalArtists.Persons.Add(new MyCreditsPerson("JAN GOLMIC"));
            additionalArtists.Persons.Add(new MyCreditsPerson("THEO ESCAMEZ"));


            //  Game Designers
            MyCreditsDepartment gameDesigners = new MyCreditsDepartment("Game Designers");
            MyPerGameSettings.Credits.Departments.Add(gameDesigners);
            gameDesigners.Persons = new List<MyCreditsPerson>();
            gameDesigners.Persons.Add(new MyCreditsPerson("JOACHIM KOOLHOF"));
            gameDesigners.Persons.Add(new MyCreditsPerson("ADAM WILLIAMS"));

            //  Additional Designers
            MyCreditsDepartment additionalDesigners = new MyCreditsDepartment("Additional Designers");
            MyPerGameSettings.Credits.Departments.Add(additionalDesigners);
            additionalDesigners.Persons.Add(new MyCreditsPerson("TOMAS RAMPAS"));
            additionalDesigners.Persons.Add(new MyCreditsPerson("LUKAS JANDIK"));


            // Sound design
            MyCreditsDepartment soundDesign = new MyCreditsDepartment("Sound Design");
            MyPerGameSettings.Credits.Departments.Add(soundDesign);
            soundDesign.Persons = new List<MyCreditsPerson>();
            soundDesign.Persons.Add(new MyCreditsPerson("LUKAS TVRDON"));
            soundDesign.Persons.Add(new MyCreditsPerson("DOMINIK RAGANCIK"));

            // Music
            MyCreditsDepartment music = new MyCreditsDepartment("Music");
            MyPerGameSettings.Credits.Departments.Add(music);
            music.Persons = new List<MyCreditsPerson>();
            music.Persons.Add(new MyCreditsPerson("KAREL ANTONIN"));
            music.Persons.Add(new MyCreditsPerson("ANNA KALHAUSOVA (cello)"));
            music.Persons.Add(new MyCreditsPerson("MARIE SVOBODOVA (vocals)"));

        

            //  Community & PR Managers
            MyCreditsDepartment managers = new MyCreditsDepartment("Community & PR Manager");
            MyPerGameSettings.Credits.Departments.Add(managers);
            managers.Persons = new List<MyCreditsPerson>();
            managers.Persons.Add(new MyCreditsPerson("JOEL \"XOCLIW\" WILCOX"));

            //  Testers
            MyCreditsDepartment testers = new MyCreditsDepartment("Testers");
            MyPerGameSettings.Credits.Departments.Add(testers);
            testers.Persons = new List<MyCreditsPerson>();
            testers.Persons.Add(new MyCreditsPerson("MATEJ VLK"));
            testers.Persons.Add(new MyCreditsPerson("VACLAV NOVOTNY"));
            testers.Persons.Add(new MyCreditsPerson("SEAN MATLOCK"));
            testers.Persons.Add(new MyCreditsPerson("JAN HRIVNAC"));

            MyCreditsDepartment additionalTesters = new MyCreditsDepartment("Additional Testers");
            MyPerGameSettings.Credits.Departments.Add(additionalTesters);
            additionalTesters.Persons.Add(new MyCreditsPerson("CHARLES WINTERS"));
            additionalTesters.Persons.Add(new MyCreditsPerson("DUSAN REPIK"));
            additionalTesters.Persons.Add(new MyCreditsPerson("JAKUB HRNCIR"));
            additionalTesters.Persons.Add(new MyCreditsPerson("MICHAL ZAVADAK"));


            // Office
            MyCreditsDepartment office = new MyCreditsDepartment("Office");
            MyPerGameSettings.Credits.Departments.Add(office);
            office.Persons = new List<MyCreditsPerson>();
            office.Persons.Add(new MyCreditsPerson("RADKA LISA"));
            office.Persons.Add(new MyCreditsPerson("PETR KREJCI"));
            office.Persons.Add(new MyCreditsPerson("VACLAV NOVOTNY"));
            office.Persons.Add(new MyCreditsPerson("TOMAS STROUHAL"));


            //  Community Manager
            MyCreditsDepartment communityManagers = new MyCreditsDepartment("Community Managers");
            MyPerGameSettings.Credits.Departments.Add(communityManagers);
            communityManagers.Persons = new List<MyCreditsPerson>();
            communityManagers.Persons.Add(new MyCreditsPerson("Dr Vagax"));
            communityManagers.Persons.Add(new MyCreditsPerson("Conrad Larson"));
            communityManagers.Persons.Add(new MyCreditsPerson("Dan2D3D"));
            communityManagers.Persons.Add(new MyCreditsPerson("RayvenQ"));
            communityManagers.Persons.Add(new MyCreditsPerson("Redphoenix"));
            communityManagers.Persons.Add(new MyCreditsPerson("TodesRitter"));

            MyCreditsDepartment modContributors = new MyCreditsDepartment("Mod Contributors");
            MyPerGameSettings.Credits.Departments.Add(modContributors);
            modContributors.Persons = new List<MyCreditsPerson>();
            modContributors.Persons.Add(new MyCreditsPerson("Tyrsis"));
            modContributors.Persons.Add(new MyCreditsPerson("Phoenix84"));
            modContributors.Persons.Add(new MyCreditsPerson("Malware"));
            modContributors.Persons.Add(new MyCreditsPerson("Arindel"));
            modContributors.Persons.Add(new MyCreditsPerson("Darth Biomech"));
            modContributors.Persons.Add(new MyCreditsPerson("Night Lone"));
            modContributors.Persons.Add(new MyCreditsPerson("Mexmer"));
            modContributors.Persons.Add(new MyCreditsPerson("JD.Horx"));
            


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
            translators.Persons.Add(new MyCreditsPerson("nintendo22"));
            translators.Persons.Add(new MyCreditsPerson("Quellix"));
            translators.Persons.Add(new MyCreditsPerson("raviool"));
         
      
            //  Thanks
            MyCreditsDepartment thanks = new MyCreditsDepartment("Special Thanks");
            MyPerGameSettings.Credits.Departments.Add(thanks);
            thanks.Persons = new List<MyCreditsPerson>();
            thanks.Persons.Add(new MyCreditsPerson("ONDREJ PETRZILKA"));
            thanks.Persons.Add(new MyCreditsPerson("CESTMIR HOUSKA"));
            thanks.Persons.Add(new MyCreditsPerson("MICHAL WROBEL"));
            thanks.Persons.Add(new MyCreditsPerson("DUSAN ANDRAS"));
            thanks.Persons.Add(new MyCreditsPerson("MARKO KORHONEN"));
            thanks.Persons.Add(new MyCreditsPerson("ALEX FLOREA"));
            thanks.Persons.Add(new MyCreditsPerson("FRANCESKO PRETTO"));
            thanks.Persons.Add(new MyCreditsPerson("RADOVAN KOTRLA"));
            thanks.Persons.Add(new MyCreditsPerson("MARTIN KOCISEK"));
            thanks.Persons.Add(new MyCreditsPerson("LUKAS CHRAPEK"));            
            thanks.Persons.Add(new MyCreditsPerson("GEORGE MAMAKOS"));
            thanks.Persons.Add(new MyCreditsPerson("TOMAS PSENICKA"));
            thanks.Persons.Add(new MyCreditsPerson("JOELLEN KOESTER"));
            thanks.Persons.Add(new MyCreditsPerson("MARKETA JAROSOVA"));
            thanks.Persons.Add(new MyCreditsPerson("VILEM SOULAK"));
            thanks.Persons.Add(new MyCreditsPerson("MAREK OBRSAL"));
            
                        

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
            vrageNotice.CreditNoticeLines.Add(new StringBuilder("Copyright © 2013-2016 KEEN SWH LTD."));
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
