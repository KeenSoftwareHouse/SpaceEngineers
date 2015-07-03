using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.Render;
using SpaceEngineers.Game.GUI;
using SpaceEngineers.Game.VoiceChat;
using System.Collections.Generic;
using System.Text;
using VRage.Utils;

namespace SpaceEngineers.Game
{
    public static partial class SpaceEngineersGame
    {
        public static readonly MyStringId DirectX9RendererKey = MyStringId.GetOrCompute("DirectX 9");
        public static readonly MyStringId DirectX11RendererKey = MyStringId.GetOrCompute("DirectX 11");

        public static void SetupPerGameSettings()
        {
            MyPerGameSettings.Game = GameEnum.SE_GAME;
            MyPerGameSettings.GameName = "Space Engineers";
            MyPerGameSettings.GameIcon = "SpaceEngineers.ico";
            MyPerGameSettings.EnableGlobalGravity = false;
            MyPerGameSettings.GameModAssembly = "SpaceEngineers.Game.dll";
            MyPerGameSettings.OffsetVoxelMapByHalfVoxel = true;
            MyPerGameSettings.EnablePregeneratedAsteroidHack = true;
            MySandboxGame.ConfigDedicated = new MyConfigDedicated<MyObjectBuilder_SessionSettings>("SpaceEngineers-Dedicated.cfg");
            MyPerGameSettings.ShowObfuscationStatus = false;

            MyPerGameSettings.CreationSettings = new MyPlacementSettings()
            {
                SmallGrid = new MyGridPlacementSettings()
                {
                    Mode = MyGridPlacementSettings.SnapMode.OneFreeAxis,
                    SearchHalfExtentsDeltaRatio = -0.1f,
                    SearchHalfExtentsDeltaAbsolute = -0.13f, //this is value at whitch you can place new small ship and is ok for wheels too
                    Penetration = new MyGridPlacementSettings.GroundPenetration()
                    {
                        Unit = MyGridPlacementSettings.PenetrationUnitEnum.Ratio,
                        MinAllowed = 0f,
                        MaxAllowed = 0.50f,
                    },
                    EnablePreciseRotationWhenSnapped = false,
                },
                LargeGrid = new MyGridPlacementSettings()
                {
                    Mode = MyGridPlacementSettings.SnapMode.OneFreeAxis,
                    SearchHalfExtentsDeltaRatio = -0.1f,
                    SearchHalfExtentsDeltaAbsolute = -0.13f,
                    Penetration = new MyGridPlacementSettings.GroundPenetration()
                    {
                        Unit = MyGridPlacementSettings.PenetrationUnitEnum.Ratio,
                        MinAllowed = 0f,
                        MaxAllowed = 0.2f,
                    },
                    EnablePreciseRotationWhenSnapped = false,
                },
                LargeStaticGrid = new MyGridPlacementSettings()
                {
                    Mode = MyGridPlacementSettings.SnapMode.Base6Directions,
                    SearchHalfExtentsDeltaRatio = -0.1f,
                    SearchHalfExtentsDeltaAbsolute = -0.13f,
                    Penetration = new MyGridPlacementSettings.GroundPenetration()
                    {
                        Unit = MyGridPlacementSettings.PenetrationUnitEnum.Ratio,
                        MinAllowed = 0f,
                        MaxAllowed = 0.8f,
                    },
                    EnablePreciseRotationWhenSnapped = true,
                },
                StaticGridAlignToCenter = true,
            };
            MyPerGameSettings.PastingSettings.StaticGridAlignToCenter = true;
            MyPerGameSettings.BuildingSettings.LargeStaticGrid = MyPerGameSettings.CreationSettings.LargeStaticGrid;
            MyPerGameSettings.Destruction = false;
            MyPerGameSettings.ConstantVoxelAmbient = -0.35f;
            MyFakes.ENABLE_SUN_BILLBOARD = true;
            MyFakes.ENABLE_PLANETS = true;

            MyPerGameSettings.BallFriendlyPhysics = true;

            MyPerGameSettings.EnableAi = false;

            MyPerGameSettings.BotFactoryType = typeof(Sandbox.Game.AI.MySandboxBotFactory);

            MyPerGameSettings.ControlMenuInitializerType = typeof(MySpaceControlMenuInitializer);

            MyPerGameSettings.EnableScenarios = true;


            MyFakes.ENABLE_PATHFINDING = false;
            MyDebugDrawSettings.DEBUG_DRAW_MOUNT_POINTS_AXIS_HELPERS = true;

            // RAGDOLL PARAMATERS
            // TODO: after the ragdoll models are correctly configured this can be removed..
            MyFakes.ENABLE_RAGDOLL_DEFAULT_PROPERTIES = true;
            //MyPerGameSettings.EnableRagdollModels = false;
            MyPerGameSettings.EnableRagdollInJetpack = true;
            //MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION = false;
            MyFakes.ENABLE_RAGDOLL_CLIENT_SYNC = true;

            MyPerGameSettings.EnableKinematicMPCharacter = true;

            MyPerGameSettings.GUI.OptionsScreen = typeof(MyGuiScreenOptionsSpace);
            MyPerGameSettings.DefaultGraphicsRenderer = DirectX9RendererKey;

            MyPerGameSettings.EnableWelderAutoswitch = true;

            FillCredits();

            // Video settings manager has not been initialized yet, so accessing config file directly.
            if (MySandboxGame.Config != null && // Dedicated server calls this as first thing, even before it has loaded config ... doesn't need render though.
                MySandboxGame.Config.GraphicsRenderer == DirectX11RendererKey)
            {
                MyPostProcessVolumetricSSAO2.MinRadius = 0.095f;
                MyPostProcessVolumetricSSAO2.MaxRadius = 4.16f;
                MyPostProcessVolumetricSSAO2.RadiusGrowZScale = 1.007f;
                MyPostProcessVolumetricSSAO2.Falloff = 3.08f;
                MyPostProcessVolumetricSSAO2.Bias = 0.25f;
                MyPostProcessVolumetricSSAO2.Contrast = 2.617f;
                MyPostProcessVolumetricSSAO2.NormValue = 0.075f;

                MyPostprocessSettingsWrapper.Settings.Brightness = 0;
                MyPostprocessSettingsWrapper.Settings.Contrast = 0;
                MyPostprocessSettingsWrapper.Settings.LuminanceExposure = 0;
                MyPostprocessSettingsWrapper.Settings.BloomExposure = 0;
                MyPostprocessSettingsWrapper.Settings.BloomMult = 0.1f;
                MyPostprocessSettingsWrapper.Settings.EyeAdaptationTau = 3;
                MyPostprocessSettingsWrapper.Settings.MiddleGreyAt0 = 0.068f;
                MyPostprocessSettingsWrapper.Settings.MiddleGreyCurveSharpness = 4.36f;
                MyPostprocessSettingsWrapper.Settings.LogLumThreshold = -6.0f;
                MyPostprocessSettingsWrapper.Settings.BlueShiftRapidness = 0;
                MyPostprocessSettingsWrapper.Settings.BlueShiftScale = 0;
                MyPostprocessSettingsWrapper.Settings.Tonemapping_A = 0.147f;
                MyPostprocessSettingsWrapper.Settings.Tonemapping_B = 0.120f;
                MyPostprocessSettingsWrapper.Settings.Tonemapping_C = 0.321f;
                MyPostprocessSettingsWrapper.Settings.Tonemapping_D = 0.699f;
                MyPostprocessSettingsWrapper.Settings.Tonemapping_E = 0.001f;
                MyPostprocessSettingsWrapper.Settings.Tonemapping_F = 0.160f;
            }

            MyPerGameSettings.VoiceChatEnabled = false;
            MyPerGameSettings.VoiceChatLogic = typeof(MyVoiceChatLogic);
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

            //  Lead Artists
            MyCreditsDepartment leadArtists = new MyCreditsDepartment("Lead Artist");
            MyPerGameSettings.Credits.Departments.Add(leadArtists);
            leadArtists.Persons = new List<MyCreditsPerson>();
            leadArtists.Persons.Add(new MyCreditsPerson("TOMAS RAMPAS"));

            //  Programmers
            MyCreditsDepartment programmers = new MyCreditsDepartment("Programmers");
            MyPerGameSettings.Credits.Departments.Add(programmers);
            programmers.Persons = new List<MyCreditsPerson>();
            programmers.Persons.Add(new MyCreditsPerson("MARTIN KROSLAK"));
            programmers.Persons.Add(new MyCreditsPerson("PEDRO VERAS DA SILVA"));
            programmers.Persons.Add(new MyCreditsPerson("CESTMIR HOUSKA"));
            programmers.Persons.Add(new MyCreditsPerson("JAN NEKVAPIL"));
            programmers.Persons.Add(new MyCreditsPerson("STANISLAV \"NOBRAIN\" KRAL"));
            programmers.Persons.Add(new MyCreditsPerson("JAKUB TYRCHA"));
            programmers.Persons.Add(new MyCreditsPerson("ALES RENNER"));
            programmers.Persons.Add(new MyCreditsPerson("ALEX FLOREA"));
            programmers.Persons.Add(new MyCreditsPerson("DUSAN ANDRAS"));
            programmers.Persons.Add(new MyCreditsPerson("JAKUB DOBIAS"));
            programmers.Persons.Add(new MyCreditsPerson("RADOVAN KOTRLA"));
            programmers.Persons.Add(new MyCreditsPerson("MICHAL WROBEL"));
            programmers.Persons.Add(new MyCreditsPerson("JAN VEBERSIK"));

            //  Artists
            MyCreditsDepartment artists = new MyCreditsDepartment("Artists");
            MyPerGameSettings.Credits.Departments.Add(artists);
            artists.Persons = new List<MyCreditsPerson>();
            artists.Persons.Add(new MyCreditsPerson("PAVEL OCOVAJ"));
            artists.Persons.Add(new MyCreditsPerson("RASTKO STANOJEVIC"));
            artists.Persons.Add(new MyCreditsPerson("SLOBODAN STEVIC"));
            artists.Persons.Add(new MyCreditsPerson("ARTEM TARASSENKO"));
            artists.Persons.Add(new MyCreditsPerson("ADAM TOWARD"));
            artists.Persons.Add(new MyCreditsPerson("LUKAS CHRAPEK"));
            artists.Persons.Add(new MyCreditsPerson("NIKITA OLHOVSKIS"));
            artists.Persons.Add(new MyCreditsPerson("KEVIN STUTH"));
            artists.Persons.Add(new MyCreditsPerson("JAN GOLMIC"));


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

            //  Community & PR Managers
            MyCreditsDepartment managers = new MyCreditsDepartment("Community & PR Manager");
            MyPerGameSettings.Credits.Departments.Add(managers);
            managers.Persons = new List<MyCreditsPerson>();
            managers.Persons.Add(new MyCreditsPerson("GEORGE MAMAKOS"));

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

            //  Testers
            MyCreditsDepartment testers = new MyCreditsDepartment("Testers");
            MyPerGameSettings.Credits.Departments.Add(testers);
            testers.Persons = new List<MyCreditsPerson>();
            testers.Persons.Add(new MyCreditsPerson("MARKETA JAROSOVA"));
            testers.Persons.Add(new MyCreditsPerson("JIRI GAZDA"));
            testers.Persons.Add(new MyCreditsPerson("VACLAV NOVOTNY"));
            testers.Persons.Add(new MyCreditsPerson("MAREK OBRSAL"));
            testers.Persons.Add(new MyCreditsPerson("LUKAS  \"LUQIN\" JANDIK"));
            testers.Persons.Add(new MyCreditsPerson("DUSAN REPIK"));

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

            MyCreditsDepartment modContributors = new MyCreditsDepartment("Mod Contributors");
            MyPerGameSettings.Credits.Departments.Add(modContributors);
            modContributors.Persons = new List<MyCreditsPerson>();
            modContributors.Persons.Add(new MyCreditsPerson("Darth Biomech (fighter cockpit)"));
            modContributors.Persons.Add(new MyCreditsPerson("Night Lone (programmable block extensions)"));


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
        }

        static partial void SetupSecrets();
    }
}
