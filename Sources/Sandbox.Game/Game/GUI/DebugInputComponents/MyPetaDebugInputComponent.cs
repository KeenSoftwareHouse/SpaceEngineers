#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Import;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageMath.Spatial;
using VRageRender;
using VRageRender.Import;

#endregion

namespace Sandbox.Game.Gui
{

    class MyDebugEntity : MyEntity
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Render.ModelStorage = VRage.Game.Models.MyModels.GetModelOnlyData(@"Models\StoneRoundLargeFull.mwm");
        }     
    }

  
    public class MyPetaInputComponent : MyDebugComponent
    {
        public static bool ENABLE_SI_DESTRUCTIONS = true;
        public static bool OLD_SI = false;
        public static bool DEBUG_DRAW_TENSIONS = false;
        public static bool DEBUG_DRAW_PATHS = false;
        public static float SI_DYNAMICS_MULTIPLIER = 1;


        static string[] s_viewVectorData = new string[]
        {
    "-0.707107,-0.707107,0.000000, 0.000000,-0.000000,1.000000, 0.707107,-0.707107,-0.000000", 
    "-0.613941,-0.789352,0.000000, 0.000000,-0.000000,1.000000, 0.789352,-0.613941,-0.000000",
    "-0.707107,-0.707107,0.000000, 0.105993,-0.105993,0.850104, 0.601114,-0.601114,-0.149896",
    "-0.789352,-0.613940,0.000000, 0.000000,-0.000000,1.000000, 0.613940,-0.789352,-0.000000",
    "-0.485643,-0.874157,0.000000, 0.000000,-0.000000,1.000000, 0.874157,-0.485643,-0.000000",
    "-0.600000,-0.800000,0.000000, 0.119917,-0.089938,0.850104, 0.680083,-0.510062,-0.149896",
    "-0.707107,-0.707107,0.000000, 0.188689,-0.188689,0.733154, 0.518418,-0.518418,-0.266846",
    "-0.800000,-0.600000,0.000000, 0.089938,-0.119917,0.850104, 0.510062,-0.680083,-0.149896",
    "-0.874157,-0.485643,0.000000, 0.000000,-0.000000,1.000000, 0.485643,-0.874157,-0.000000",
    "-0.316228,-0.948683,0.000000, 0.000000,-0.000000,1.000000, 0.948683,-0.316228,-0.000000",
    "-0.447214,-0.894427,0.000000, 0.134071,-0.067036,0.850104, 0.760356,-0.380178,-0.149896",
    "-0.581238,-0.813734,0.000000, 0.217142,-0.155101,0.733154, 0.596592,-0.426137,-0.266846",
    "-0.707107,-0.707107,0.000000, 0.258819,-0.258819,0.633975, 0.448288,-0.448288,-0.366025",
    "-0.813734,-0.581238,0.000000, 0.155101,-0.217142,0.733154, 0.426137,-0.596592,-0.266846",
    "-0.894427,-0.447214,0.000000, 0.067036,-0.134071,0.850104, 0.380178,-0.760356,-0.149896",
    "-0.948683,-0.316228,0.000000, 0.000000,-0.000000,1.000000, 0.316228,-0.948683,-0.000000",
    "-0.110431,-0.993884,0.000000, 0.000000,-0.000000,1.000000, 0.993884,-0.110431,-0.000000",
    "-0.242536,-0.970143,0.000000, 0.145421,-0.036355,0.850104, 0.824722,-0.206180,-0.149896",
    "-0.393919,-0.919145,0.000000, 0.245270,-0.105116,0.733154, 0.673875,-0.288803,-0.266846",
    "-0.554700,-0.832050,0.000000, 0.304552,-0.203034,0.633975, 0.527499,-0.351666,-0.366025",
    "-0.707107,-0.707107,0.000000, 0.322621,-0.322621,0.543744, 0.384485,-0.384485,-0.456256",
    "-0.832050,-0.554700,0.000000, 0.203034,-0.304552,0.633975, 0.351666,-0.527499,-0.366025",
    "-0.919145,-0.393919,0.000000, 0.105116,-0.245270,0.733154, 0.288803,-0.673875,-0.266846",
    "-0.970142,-0.242536,0.000000, 0.036355,-0.145421,0.850104, 0.206181,-0.824722,-0.149896",
    "-0.993884,-0.110432,0.000000, 0.000000,-0.000000,1.000000, 0.110432,-0.993884,-0.000000",
    " 0.110431,-0.993884,0.000000, 0.000000, 0.000000,1.000000, 0.993884, 0.110431,-0.000000",
    "-0.000000,-1.000000,0.000000, 0.149896,-0.000000,0.850104, 0.850104,-0.000000,-0.149896",
    "-0.141422,-0.989949,0.000000, 0.264164,-0.037738,0.733154, 0.725785,-0.103684,-0.266846",
    "-0.316228,-0.948683,0.000000, 0.347242,-0.115747,0.633975, 0.601441,-0.200480,-0.366025",
    "-0.514496,-0.857493,0.000000, 0.391236,-0.234742,0.543744, 0.466257,-0.279754,-0.456256",
    "-0.707107,-0.707107,0.000000, 0.384485,-0.384485,0.456256, 0.322621,-0.322621,-0.543744",
    "-0.857493,-0.514496,0.000000, 0.234742,-0.391236,0.543744, 0.279754,-0.466257,-0.456256",
    "-0.948683,-0.316228,0.000000, 0.115747,-0.347242,0.633975, 0.200480,-0.601441,-0.366025",
    "-0.989950,-0.141421,0.000000, 0.037738,-0.264164,0.733154, 0.103684,-0.725785,-0.266846",
    "-1.000000, 0.000000,0.000000,-0.000000,-0.149896,0.850104,-0.000000,-0.850104,-0.149896",
    "-0.993884, 0.110431,0.000000,-0.000000,-0.000000,1.000000,-0.110431,-0.993884,-0.000000",
    " 0.316228,-0.948683,0.000000, 0.000000, 0.000000,1.000000, 0.948683, 0.316228,-0.000000",
    " 0.242536,-0.970143,0.000000, 0.145421, 0.036355,0.850104, 0.824722, 0.206180,-0.149896",
    " 0.141422,-0.989949,0.000000, 0.264164, 0.037738,0.733154, 0.725785, 0.103684,-0.266846",
    "-0.000000,-1.000000,0.000000, 0.366025,-0.000000,0.633975, 0.633975,-0.000000,-0.366025",
    "-0.196116,-0.980581,0.000000, 0.447395,-0.089479,0.543744, 0.533185,-0.106637,-0.456256",
    "-0.447214,-0.894427,0.000000, 0.486340,-0.243170,0.456256, 0.408087,-0.204044,-0.543744",
    "-0.707107,-0.707107,0.000000, 0.448288,-0.448288,0.366025, 0.258819,-0.258819,-0.633975",
    "-0.894427,-0.447214,0.000000, 0.243170,-0.486340,0.456256, 0.204044,-0.408087,-0.543744",
    "-0.980581,-0.196116,0.000000, 0.089479,-0.447395,0.543744, 0.106637,-0.533185,-0.456256",
    "-1.000000, 0.000000,0.000000,-0.000000,-0.366025,0.633975,-0.000000,-0.633975,-0.366025",
    "-0.989950, 0.141421,0.000000,-0.037738,-0.264164,0.733154,-0.103684,-0.725785,-0.266846",
    "-0.970143, 0.242536,0.000000,-0.036355,-0.145421,0.850104,-0.206180,-0.824722,-0.149896",
    "-0.948683, 0.316228,0.000000,-0.000000,-0.000000,1.000000,-0.316228,-0.948683,-0.000000",
    " 0.485643,-0.874157,0.000000, 0.000000, 0.000000,1.000000, 0.874157, 0.485643,-0.000000",
    " 0.447214,-0.894427,0.000000, 0.134071, 0.067036,0.850104, 0.760356, 0.380178,-0.149896",
    " 0.393919,-0.919145,0.000000, 0.245270, 0.105116,0.733154, 0.673875, 0.288803,-0.266846",
    " 0.316228,-0.948683,0.000000, 0.347242, 0.115747,0.633975, 0.601441, 0.200480,-0.366025",
    " 0.196116,-0.980581,0.000000, 0.447395, 0.089479,0.543744, 0.533185, 0.106637,-0.456256",
    "-0.000000,-1.000000,0.000000, 0.543744,-0.000000,0.456256, 0.456256,-0.000000,-0.543744",
    "-0.316228,-0.948683,0.000000, 0.601441,-0.200480,0.366025, 0.347242,-0.115747,-0.633975",
    "-0.707107,-0.707107,0.000000, 0.518418,-0.518418,0.266846, 0.188689,-0.188689,-0.733154",
    "-0.948683,-0.316228,0.000000, 0.200480,-0.601441,0.366025, 0.115747,-0.347242,-0.633975",
    "-1.000000, 0.000000,0.000000,-0.000000,-0.543744,0.456256,-0.000000,-0.456256,-0.543744",
    "-0.980581, 0.196116,0.000000,-0.089479,-0.447395,0.543744,-0.106637,-0.533185,-0.456256",
    "-0.948683, 0.316228,0.000000,-0.115747,-0.347242,0.633975,-0.200480,-0.601441,-0.366025",
    "-0.919145, 0.393919,0.000000,-0.105116,-0.245270,0.733154,-0.288803,-0.673875,-0.266846",
    "-0.894427, 0.447214,0.000000,-0.067036,-0.134071,0.850104,-0.380178,-0.760356,-0.149896",
    "-0.874157, 0.485643,0.000000,-0.000000,-0.000000,1.000000,-0.485643,-0.874157,-0.000000",
    " 0.613941,-0.789352,0.000000, 0.000000, 0.000000,1.000000, 0.789352, 0.613941,-0.000000",
    " 0.600000,-0.800000,0.000000, 0.119917, 0.089938,0.850104, 0.680083, 0.510062,-0.149896",
    " 0.581238,-0.813734,0.000000, 0.217142, 0.155101,0.733154, 0.596592, 0.426137,-0.266846",
    " 0.554700,-0.832050,0.000000, 0.304552, 0.203034,0.633975, 0.527499, 0.351666,-0.366025",
    " 0.514496,-0.857493,0.000000, 0.391236, 0.234742,0.543744, 0.466257, 0.279754,-0.456256",
    " 0.447214,-0.894427,0.000000, 0.486340, 0.243170,0.456256, 0.408087, 0.204044,-0.543744",
    " 0.316228,-0.948683,0.000000, 0.601441, 0.200480,0.366025, 0.347242, 0.115747,-0.633975",
    "-0.000000,-1.000000,0.000000, 0.733154,-0.000000,0.266846, 0.266846,-0.000000,-0.733154",
    "-0.707107,-0.707107,0.000000, 0.601114,-0.601114,0.149896, 0.105993,-0.105993,-0.850104",
    "-1.000000, 0.000000,0.000000,-0.000000,-0.733154,0.266846,-0.000000,-0.266846,-0.733154",
    "-0.948683, 0.316228,0.000000,-0.200480,-0.601441,0.366025,-0.115747,-0.347242,-0.633975",
    "-0.894427, 0.447214,0.000000,-0.243170,-0.486340,0.456256,-0.204044,-0.408087,-0.543744",
    "-0.857493, 0.514496,0.000000,-0.234742,-0.391236,0.543744,-0.279754,-0.466257,-0.456256",
    "-0.832050, 0.554700,0.000000,-0.203034,-0.304552,0.633975,-0.351666,-0.527499,-0.366025",
    "-0.813733, 0.581238,0.000000,-0.155101,-0.217142,0.733154,-0.426137,-0.596592,-0.266846",
    "-0.800000, 0.600000,0.000000,-0.089938,-0.119917,0.850104,-0.510062,-0.680083,-0.149896",
    "-0.789352, 0.613941,0.000000,-0.000000,-0.000000,1.000000,-0.613941,-0.789352,-0.000000",
    " 0.707107,-0.707107,0.000000, 0.000000, 0.000000,1.000000, 0.707107, 0.707107,-0.000000",
    " 0.707107,-0.707107,0.000000, 0.105993, 0.105993,0.850104, 0.601114, 0.601114,-0.149896",
    " 0.707107,-0.707107,0.000000, 0.188689, 0.188689,0.733154, 0.518418, 0.518418,-0.266846",
    " 0.707107,-0.707107,0.000000, 0.258819, 0.258819,0.633975, 0.448288, 0.448288,-0.366025",
    " 0.707107,-0.707107,0.000000, 0.322621, 0.322621,0.543744, 0.384485, 0.384485,-0.456256",
    " 0.707107,-0.707107,0.000000, 0.384485, 0.384485,0.456256, 0.322621, 0.322621,-0.543744",
    " 0.707107,-0.707107,0.000000, 0.448288, 0.448288,0.366025, 0.258819, 0.258819,-0.633975",
    " 0.707107,-0.707107,0.000000, 0.518418, 0.518418,0.266846, 0.188689, 0.188689,-0.733154",
    " 0.707107,-0.707107,0.000000, 0.601114, 0.601114,0.149896, 0.105993, 0.105993,-0.850104",
    " 0.000000, 1.000000,0.000000,-1.000000, 0.000000,0.000000, 0.000000, 0.000000,-1.000000",
    "-0.707107, 0.707107,0.000000,-0.601114,-0.601114,0.149896,-0.105993,-0.105993,-0.850104",
    "-0.707107, 0.707107,0.000000,-0.518418,-0.518418,0.266846,-0.188689,-0.188689,-0.733154",
    "-0.707107, 0.707107,0.000000,-0.448288,-0.448288,0.366025,-0.258819,-0.258819,-0.633975",
    "-0.707107, 0.707107,0.000000,-0.384485,-0.384485,0.456256,-0.322621,-0.322621,-0.543744",
    "-0.707107, 0.707107,0.000000,-0.322621,-0.322621,0.543744,-0.384485,-0.384485,-0.456256",
    "-0.707107, 0.707107,0.000000,-0.258819,-0.258819,0.633975,-0.448288,-0.448288,-0.366025",
    "-0.707107, 0.707107,0.000000,-0.188689,-0.188689,0.733154,-0.518418,-0.518418,-0.266846",
    "-0.707107, 0.707107,0.000000,-0.105993,-0.105993,0.850104,-0.601114,-0.601114,-0.149896",
    "-0.707107, 0.707107,0.000000,-0.000000,-0.000000,1.000000,-0.707107,-0.707107,-0.000000",
    " 0.789352,-0.613941,0.000000, 0.000000, 0.000000,1.000000, 0.613941, 0.789352,-0.000000",
    " 0.800000,-0.600000,0.000000, 0.089938, 0.119917,0.850104, 0.510062, 0.680083,-0.149896",
    " 0.813734,-0.581238,0.000000, 0.155101, 0.217142,0.733154, 0.426137, 0.596592,-0.266846",
    " 0.832050,-0.554700,0.000000, 0.203034, 0.304551,0.633975, 0.351666, 0.527499,-0.366025",
    " 0.857493,-0.514496,0.000000, 0.234742, 0.391236,0.543744, 0.279754, 0.466257,-0.456256",
    " 0.894427,-0.447214,0.000000, 0.243170, 0.486340,0.456256, 0.204044, 0.408087,-0.543744",
    " 0.948683,-0.316228,0.000000, 0.200480, 0.601441,0.366025, 0.115747, 0.347242,-0.633975",
    " 1.000000, 0.000000,0.000000, 0.000000, 0.733154,0.266846, 0.000000, 0.266846,-0.733154",
    " 0.707107, 0.707107,0.000000,-0.601114, 0.601114,0.149896,-0.105993, 0.105993,-0.850104",
    " 0.000000, 1.000000,0.000000,-0.733154, 0.000000,0.266846,-0.266846, 0.000000,-0.733154",
    "-0.316228, 0.948683,0.000000,-0.601441,-0.200480,0.366025,-0.347242,-0.115747,-0.633975",
    "-0.447214, 0.894427,0.000000,-0.486340,-0.243170,0.456256,-0.408087,-0.204044,-0.543744",
    "-0.514496, 0.857493,0.000000,-0.391236,-0.234742,0.543744,-0.466257,-0.279754,-0.456256",
    "-0.554700, 0.832050,0.000000,-0.304552,-0.203034,0.633975,-0.527499,-0.351666,-0.366025",
    "-0.581238, 0.813734,0.000000,-0.217142,-0.155101,0.733154,-0.596592,-0.426137,-0.266846",
    "-0.600000, 0.800000,0.000000,-0.119917,-0.089938,0.850104,-0.680083,-0.510062,-0.149896",
    "-0.613941, 0.789352,0.000000,-0.000000,-0.000000,1.000000,-0.789352,-0.613941,-0.000000",
    " 0.874157,-0.485643,0.000000, 0.000000, 0.000000,1.000000, 0.485643, 0.874157,-0.000000",
    " 0.894427,-0.447214,0.000000, 0.067036, 0.134071,0.850104, 0.380178, 0.760356,-0.149896",
    " 0.919145,-0.393919,0.000000, 0.105116, 0.245270,0.733154, 0.288803, 0.673875,-0.266846",
    " 0.948683,-0.316228,0.000000, 0.115747, 0.347242,0.633975, 0.200480, 0.601441,-0.366025",
    " 0.980581,-0.196116,0.000000, 0.089479, 0.447395,0.543744, 0.106637, 0.533185,-0.456256",
    " 1.000000, 0.000000,0.000000, 0.000000, 0.543744,0.456256, 0.000000, 0.456256,-0.543744",
    " 0.948683, 0.316228,0.000000,-0.200480, 0.601441,0.366025,-0.115747, 0.347242,-0.633975",
    " 0.707107, 0.707107,0.000000,-0.518418, 0.518418,0.266846,-0.188689, 0.188689,-0.733154",
    " 0.316228, 0.948683,0.000000,-0.601441, 0.200480,0.366025,-0.347242, 0.115747,-0.633975",
    " 0.000000, 1.000000,0.000000,-0.543744, 0.000000,0.456256,-0.456256, 0.000000,-0.543744",
    "-0.196116, 0.980581,0.000000,-0.447395,-0.089479,0.543744,-0.533185,-0.106637,-0.456256",
    "-0.316228, 0.948683,0.000000,-0.347242,-0.115747,0.633975,-0.601441,-0.200480,-0.366025",
    "-0.393919, 0.919145,0.000000,-0.245270,-0.105116,0.733154,-0.673875,-0.288803,-0.266846",
    "-0.447214, 0.894427,0.000000,-0.134071,-0.067036,0.850104,-0.760356,-0.380178,-0.149896",
    "-0.485643, 0.874157,0.000000,-0.000000,-0.000000,1.000000,-0.874157,-0.485643,-0.000000",
    " 0.948683,-0.316228,0.000000, 0.000000, 0.000000,1.000000, 0.316228, 0.948683,-0.000000",
    " 0.970142,-0.242536,0.000000, 0.036355, 0.145421,0.850104, 0.206180, 0.824722,-0.149896",
    " 0.989950,-0.141421,0.000000, 0.037738, 0.264164,0.733154, 0.103684, 0.725785,-0.266846",
    " 1.000000, 0.000000,0.000000, 0.000000, 0.366025,0.633975, 0.000000, 0.633975,-0.366025",
    " 0.980581, 0.196116,0.000000,-0.089479, 0.447395,0.543744,-0.106637, 0.533185,-0.456256",
    " 0.894427, 0.447214,0.000000,-0.243170, 0.486340,0.456256,-0.204044, 0.408087,-0.543744",
    " 0.707107, 0.707107,0.000000,-0.448288, 0.448288,0.366025,-0.258819, 0.258819,-0.633975",
    " 0.447214, 0.894427,0.000000,-0.486340, 0.243170,0.456256,-0.408087, 0.204044,-0.543744",
    " 0.196116, 0.980581,0.000000,-0.447395, 0.089479,0.543744,-0.533185, 0.106637,-0.456256",
    " 0.000000, 1.000000,0.000000,-0.366025, 0.000000,0.633975,-0.633975, 0.000000,-0.366025",
    "-0.141421, 0.989949,0.000000,-0.264164,-0.037738,0.733154,-0.725785,-0.103684,-0.266846",
    "-0.242536, 0.970143,0.000000,-0.145421,-0.036355,0.850104,-0.824722,-0.206180,-0.149896",
    "-0.316228, 0.948683,0.000000,-0.000000,-0.000000,1.000000,-0.948683,-0.316228,-0.000000",
    " 0.993884,-0.110432,0.000000, 0.000000, 0.000000,1.000000, 0.110432, 0.993884,-0.000000",
    " 1.000000, 0.000000,0.000000, 0.000000, 0.149896,0.850104, 0.000000, 0.850104,-0.149896",
    " 0.989949, 0.141421,0.000000,-0.037738, 0.264164,0.733154,-0.103684, 0.725785,-0.266846",
    " 0.948683, 0.316228,0.000000,-0.115747, 0.347242,0.633975,-0.200480, 0.601441,-0.366025",
    " 0.857493, 0.514496,0.000000,-0.234742, 0.391236,0.543744,-0.279754, 0.466257,-0.456256",
    " 0.707107, 0.707107,0.000000,-0.384485, 0.384485,0.456256,-0.322621, 0.322621,-0.543744",
    " 0.514496, 0.857493,0.000000,-0.391236, 0.234742,0.543744,-0.466257, 0.279754,-0.456256",
    " 0.316228, 0.948683,0.000000,-0.347242, 0.115747,0.633975,-0.601441, 0.200480,-0.366025",
    " 0.141421, 0.989949,0.000000,-0.264164, 0.037738,0.733154,-0.725785, 0.103684,-0.266846",
    " 0.000000, 1.000000,0.000000,-0.149896, 0.000000,0.850104,-0.850104, 0.000000,-0.149896",
    "-0.110432, 0.993884,0.000000,-0.000000,-0.000000,1.000000,-0.993884,-0.110432,-0.000000",
    " 0.993884, 0.110431,0.000000,-0.000000, 0.000000,1.000000,-0.110431, 0.993884,-0.000000",
    " 0.970143, 0.242536,0.000000,-0.036355, 0.145421,0.850104,-0.206180, 0.824722,-0.149896",
    " 0.919145, 0.393919,0.000000,-0.105116, 0.245270,0.733154,-0.288803, 0.673875,-0.266846",
    " 0.832050, 0.554700,0.000000,-0.203034, 0.304552,0.633975,-0.351666, 0.527499,-0.366025",
    " 0.707107, 0.707107,0.000000,-0.322621, 0.322621,0.543744,-0.384485, 0.384485,-0.456256",
    " 0.554700, 0.832050,0.000000,-0.304552, 0.203034,0.633975,-0.527499, 0.351666,-0.366025",
    " 0.393919, 0.919145,0.000000,-0.245270, 0.105116,0.733154,-0.673875, 0.288803,-0.266846",
    " 0.242536, 0.970143,0.000000,-0.145421, 0.036355,0.850104,-0.824722, 0.206180,-0.149896",
    " 0.110432, 0.993884,0.000000,-0.000000, 0.000000,1.000000,-0.993884, 0.110432,-0.000000",
    " 0.948683, 0.316228,0.000000,-0.000000, 0.000000,1.000000,-0.316228, 0.948683,-0.000000",
    " 0.894427, 0.447214,0.000000,-0.067036, 0.134071,0.850104,-0.380178, 0.760356,-0.149896",
    " 0.813733, 0.581238,0.000000,-0.155101, 0.217142,0.733154,-0.426137, 0.596592,-0.266846",
    " 0.707107, 0.707107,0.000000,-0.258819, 0.258819,0.633975,-0.448288, 0.448288,-0.366025",
    " 0.581238, 0.813733,0.000000,-0.217142, 0.155101,0.733154,-0.596592, 0.426137,-0.266846",
    " 0.447214, 0.894427,0.000000,-0.134071, 0.067036,0.850104,-0.760356, 0.380178,-0.149896",
    " 0.316228, 0.948683,0.000000,-0.000000, 0.000000,1.000000,-0.948683, 0.316228,-0.000000",
    " 0.874157, 0.485643,0.000000,-0.000000, 0.000000,1.000000,-0.485643, 0.874157,-0.000000",
    " 0.800000, 0.600000,0.000000,-0.089938, 0.119917,0.850104,-0.510062, 0.680083,-0.149896",
    " 0.707107, 0.707107,0.000000,-0.188689, 0.188689,0.733154,-0.518418, 0.518418,-0.266846",
    " 0.600000, 0.800000,0.000000,-0.119917, 0.089938,0.850104,-0.680083, 0.510062,-0.149896",
    " 0.485643, 0.874157,0.000000,-0.000000, 0.000000,1.000000,-0.874157, 0.485643,-0.000000",
    " 0.789352, 0.613941,0.000000,-0.000000, 0.000000,1.000000,-0.613941, 0.789352,-0.000000",
    " 0.707107, 0.707107,0.000000,-0.105993, 0.105993,0.850104,-0.601114, 0.601114,-0.149896",
    " 0.613941, 0.789352,0.000000,-0.000000, 0.000000,1.000000,-0.789352, 0.613941,-0.000000",
    " 0.707107, 0.707107,0.000000,-0.000000, 0.000000,1.000000,-0.707107, 0.707107,-0.000000",
        };


        static Matrix[] s_viewVectors;

        
        public override string GetName()
        {
            return "Peta";
        }

        public MyPetaInputComponent()
        {
            AddShortcut(MyKeys.OemBackslash, true, true, false, false,
                () => "Debug draw physics clusters: " + MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS,
                delegate
                {
                    MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS = !MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS;
                    return true;
                });

            AddShortcut(MyKeys.OemBackslash, false, false, false, false,
                () => "Advance all moving entities",
                delegate
                {

                    AdvanceEntities();
                    return true;

                });

            AddShortcut(MyKeys.S, true, true, false, false,
               () => "Insert controllable sphere",
               delegate
               {
                   if (Sync.IsServer)
                   {
                       MyControllableSphere sphere = new MyControllableSphere();
                       sphere.Init();
                       MyEntities.Add(sphere);

                       sphere.PositionComp.SetPosition(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector);
                       sphere.Physics.Enabled = false;

                       MySession.Static.LocalHumanPlayer.Controller.TakeControl(sphere);
                   }
                   return true;
               });

            AddShortcut(MyKeys.Back, true, true, false, false,
               () => "Freeze gizmo: " + MyCubeBuilder.Static.FreezeGizmo,
               delegate
               {
                   MyCubeBuilder.Static.FreezeGizmo = !MyCubeBuilder.Static.FreezeGizmo;
                   return true;
               });

            AddShortcut(MyKeys.NumPad8, true, false, false, false,
            //() => "Wave to friend",
            () => "Insert 2 trees",
            delegate
            {
                //MySession.Static.LocalCharacter.AddCommand(new MyAnimationCommand()
                //{
                //    AnimationSubtypeName = "Wave",
                //    BlendTime = 0.3f,
                //    TimeScale = 1,
                //}, true);
                SpawnTrees();
                return true;
            });

            AddShortcut(MyKeys.NumPad9, true, false, false, false,
              () => "Dynamics multiplier: " + ((int)SI_DYNAMICS_MULTIPLIER).ToString(),
              delegate
              {
                  SI_DYNAMICS_MULTIPLIER *= 10;
                  if (SI_DYNAMICS_MULTIPLIER > 10000)
                      SI_DYNAMICS_MULTIPLIER = 1;
                  return true;
              });

              //AddShortcut(MyKeys.NumPad7, true, false, false, false,
              //() => "Use next ship",
              //delegate
              //{
              //    MyCharacterInputComponent.UseNextShip();
              //    return true;
              //});

            AddShortcut(MyKeys.NumPad7, true, false, false, false,
            () => "Highlight GScreen",
            delegate
            {
                HighlightGScreen();
                return true;
            });

              //AddShortcut(MyKeys.NumPad5, true, false, false, false,
              //  () => "Insert tree",
              //  delegate
              //  {
              //      InsertTree();
              //      return true;
              //  });

              AddShortcut(MyKeys.NumPad5, true, false, false, false,
                 () => "Insert voxelmap",
                 delegate
                 {
                     InsertVoxelMap();
                     return true;
                 });


              AddShortcut(MyKeys.NumPad6, true, false, false, false,
             () => "SI Debug draw paths",
             delegate
             {
                 MyDebugDrawSettings.ENABLE_DEBUG_DRAW = !MyDebugDrawSettings.ENABLE_DEBUG_DRAW;
                 if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                 {
                     MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY = true;
                     DEBUG_DRAW_PATHS = true;
                     DEBUG_DRAW_TENSIONS = false;
                 }

                //foreach (var entity in MyEntities.GetEntities())
                //{
                //    if (entity.GetTopMostParent().Physics != null)
                //    {
                //        var body = entity.GetTopMostParent().Physics;
                //        if (body.RigidBody != null)
                //        {
                //            Vector3 newVel = body.Entity.Physics.LinearVelocity;
                //            float y = newVel.Y;
                //            newVel.Y = -newVel.Z;
                //            newVel.Z = y;
                //            body.RigidBody.ApplyLinearImpulse(newVel * body.Mass * 20);
                //        }
                //    }
                //}
                 return true;
             });

              AddShortcut(MyKeys.NumPad3, true, false, false, false,
             () => "SI Debug draw tensions",
             delegate
             {
                 MyDebugDrawSettings.ENABLE_DEBUG_DRAW = !MyDebugDrawSettings.ENABLE_DEBUG_DRAW;
                 if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                 {
                     MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY = true;
                     DEBUG_DRAW_PATHS = false;
                     DEBUG_DRAW_TENSIONS = true;
                 }
                 //foreach (var entity in MyEntities.GetEntities())
                 //{
                 //    if (entity.GetTopMostParent().Physics != null)
                 //    {
                 //        var body = entity.GetTopMostParent().Physics;
                 //        if (body.RigidBody != null)
                 //        {
                 //            Vector3 newVel = body.Entity.Physics.LinearVelocity;
                 //            float x = newVel.X;
                 //            newVel.X = -newVel.X;
                 //            newVel.Z = x;
                 //            body.RigidBody.ApplyLinearImpulse(newVel * body.Mass * 20);
                 //        }
                 //    }
                 //}
                 return true;
             });

              AddShortcut(MyKeys.NumPad4, true, false, false, false,
                 () => "Enable SI destructions: " + ENABLE_SI_DESTRUCTIONS,
                 delegate
                 {
                     ENABLE_SI_DESTRUCTIONS = !ENABLE_SI_DESTRUCTIONS;
                     return true;
                 });


            
              AddShortcut(MyKeys.Up, true, false, false, false,
                 () => "SI Selected cube up",
                 delegate
                 {
                     if (!MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY)
                         return false;

                     MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y + 1, MyAdvancedStaticSimulator.SelectedCube.Z);
                     return true;
                 });
              AddShortcut(MyKeys.Down, true, false, false, false,
                 () => "SI Selected cube down",
                 delegate
                 {
                     if (!MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY)
                         return false;

                     MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y - 1, MyAdvancedStaticSimulator.SelectedCube.Z);
                     return true;
                 });
              AddShortcut(MyKeys.Left, true, false, false, false,
                   () => "SI Selected cube left",
                   delegate
                   {
                       if (!MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY)
                           return false;

                       MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X - 1, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z);
                       return true;
                   });
              AddShortcut(MyKeys.Right, true, false, false, false,
                   () => "SI Selected cube right",
                   delegate
                   {
                       if (!MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY)
                           return false;

                       MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X + 1, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z);
                       return true;
                   });
              AddShortcut(MyKeys.Up, true, true, false, false,
                     () => "SI Selected cube forward",
                     delegate
                     {
                         MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z - 1);
                         return true;
                     });
              AddShortcut(MyKeys.Down, true, true, false, false,
                       () => "SI Selected cube back",
                       delegate
                       {
                           if (!MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY)
                               return false;

                           MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z + 1);
                           return true;
                       });

              AddShortcut(MyKeys.NumPad2, true, false, false, false,
                        () => "Spawn simple skinned object",
                        delegate
                        {
                            //MyDebugDrawSettings.ENABLE_DEBUG_DRAW = true;
                            //MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES = !MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES;

                            //foreach (var entity in MyEntities.GetEntities())
                            //{
                            //    if (entity is MyFracturedPiece)
                            //    {
                            //        foreach (var id in entity.Render.RenderObjectIDs)
                            //        {
                            //            VRageRender.MyRenderProxy.UpdateRenderObjectVisibility(id, !MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES, false);
                            //        }
                            //    }
                            //}

                            SpawnSimpleSkinnedObject();

                            return true;
                        });

            
        }

        private static void AdvanceEntities()
        {
            //foreach (var entity in MyEntities.GetEntities().ToList())
            //{
            //    if (entity.Physics != null)
            //    {
            //        Vector3D posDelta = entity.Physics.LinearVelocity * SI_DYNAMICS_MULTIPLIER;
            //        MyPhysics.EnsurePhysicsSpace(entity.PositionComp.WorldAABB + posDelta);

            //        if (entity.Physics.LinearVelocity.Length() > 0.1f)
            //        {
            //            entity.PositionComp.SetPosition(entity.PositionComp.GetPosition() + posDelta);
            //        }
            //    }
            //}


            Vector3D posDelta = Vector3.Forward * SI_DYNAMICS_MULTIPLIER * 500;

            //foreach (var entity in MyEntities.GetEntities().ToList())
            //{
            //    if (entity.Physics != null)
            //    {
            //        MyPhysics.EnsurePhysicsSpace(entity.PositionComp.WorldAABB + posDelta);

            //        entity.PositionComp.SetPosition(entity.PositionComp.GetPosition() + posDelta);
            //    }
            //}


            int framesMovingSimulate = 1000;

            while (framesMovingSimulate-- > 0)
            {
                foreach (var entity in MyEntities.GetEntities().ToList())
                {
                    if (entity.Physics != null)
                    {
                        Vector3D velocity = entity.Physics.LinearVelocity * SI_DYNAMICS_MULTIPLIER;

                        var aabb = MyClusterTree.AdjustAABBByVelocity(entity.PositionComp.WorldAABB, velocity);

                        MyPhysics.EnsurePhysicsSpace(aabb);
                    }
                }

                MySession.Static.Update(MyTimeSpan.Zero);
                //foreach (var entity in MyEntities.GetEntities().ToList())
                //{
                //    if (entity.Physics != null)
                //    {
                //        //Vector3D posDelta = entity.Physics.LinearVelocity * SI_DYNAMICS_MULTIPLIER;
                //        //entity.PositionComp.SetPosition(entity.PositionComp.GetPosition() + posDelta);

                //    }
                //}
            }

            //Vector3D posDelta2 = Vector3.Forward * SI_DYNAMICS_MULTIPLIER * 500;

            //foreach (var entity in MyEntities.GetEntities().ToList())
            //{
            //    if (entity.Physics != null)
            //    {
            //        MyPhysics.EnsurePhysicsSpace(entity.PositionComp.WorldAABB + posDelta2);

            //        entity.PositionComp.SetPosition(entity.PositionComp.GetPosition() + posDelta2);
            //    }
            //}


        }

        public override bool HandleInput()
        {
            if (base.HandleInput())
                return true;

            bool handled = false;

          
            //foreach (var ent in MyEntities.GetEntities())
            //{
            //    if (ent is MyCubeGrid)
            //    {
            //        if (ent.PositionComp.WorldAABB.Contains(MySector.MainCamera.Position) == ContainmentType.Disjoint)
            //        {
            //            ent.Close();
            //        }
            //    }
            //}

            //var measureStart = new VRage.Library.Utils.MyTimeSpan(System.Diagnostics.Stopwatch.GetTimestamp());

            var list = MyDefinitionManager.Static.GetAnimationDefinitions();

            foreach (var skin in m_skins)
            {                
                skin.UpdateAnimation(Vector3.Distance(MySector.MainCamera.Position,skin.PositionComp.GetPosition()));

                if (MyRandom.Instance.NextFloat() > 0.95f)
                {
                    var randomAnim = list.ItemAt(MyRandom.Instance.Next(0, list.Count));
                    var command = new MyAnimationCommand()
                    {
                        AnimationSubtypeName = randomAnim.Id.SubtypeName,
                        FrameOption = MyFrameOption.Loop,
                        TimeScale = 1,
                        BlendTime = 0.3f
                    };
                    skin.AddCommand(command);
                }
            }


//            var measureEnd = new VRage.Library.Utils.MyTimeSpan(System.Diagnostics.Stopwatch.GetTimestamp());
 //           var total = measureEnd - measureStart;
   //         m_skins.Clear();

            if (m_voxelMap != null && m_voxelMap.PositionComp != null)
            {
                Vector3D center = m_voxelMap.PositionComp.WorldAABB.Center;
                Vector3D centerDelta = m_voxelMap.PositionComp.WorldMatrix.Translation - center;



                MatrixD worldMatrix = m_voxelMap.PositionComp.WorldMatrix;

                var rotationMatrix = Matrix.CreateRotationZ(0.01f);
                worldMatrix *= rotationMatrix;
                centerDelta = Vector3.TransformNormal(centerDelta, rotationMatrix);

                worldMatrix.Translation = center + centerDelta;

                //m_voxelMap.PositionComp.WorldMatrix = worldMatrix;


                SI_DYNAMICS_MULTIPLIER += 0.01f;

                //var tr = worldMatrix.Translation;
                //worldMatrix.Translation = new Vector3D(tr.X += (double)MyMath.FastSin(SI_DYNAMICS_MULTIPLIER), tr.Y, tr.Z);
                //worldMatrix.Translation = new Vector3D(tr.X + 100, tr.Y, tr.Z);

                //m_voxelMap.PositionComp.WorldMatrix = worldMatrix;

                var localBB = m_voxelMap.PositionComp.LocalAABB;
                MyOrientedBoundingBoxD orb = new MyOrientedBoundingBoxD((BoundingBoxD)localBB, m_voxelMap.PositionComp.WorldMatrix);
                VRageRender.MyRenderProxy.DebugDrawOBB(orb, Color.White, 0, false, false);
            }

            return handled;
        }

        const int N = 9;
        const int NT = 2 * N * (N + 1) + 1;


        int viewNumber(int i, int j)
        {
            return i * ((2 * N + 1) - Math.Abs(i)) + j + (N * (N + 1));
        }

  

        void findViews(int species, Vector3 cDIR, out Vector3I vv, out Vector3 rr)
        {
            Vector3 VDIR = new Vector3(cDIR.X, Math.Max(-cDIR.Y, 0.01f), cDIR.Z);
            float a = Math.Abs(VDIR.X) > Math.Abs(VDIR.Z) ? -VDIR.Z / VDIR.X : -VDIR.X / -VDIR.Z;
            float nxx = N * (1.0f - a) * (float)Math.Acos(MathHelper.Clamp(VDIR.Y, -1, 1)) / 3.141592657f; // uniform sampling in theta
            float nyy = N * (1.0f + a) * (float)Math.Acos(MathHelper.Clamp(VDIR.Y, -1, 1)) / 3.141592657f;
            int i = (int)Math.Floor(nxx);
            int j = (int)Math.Floor(nyy);
            float ti = nxx - i;
            float tj = nyy - j;
            float alpha = 1.0f - ti - tj;
            bool b = alpha > 0.0;
            Vector3I ii = new Vector3I(b ? i : i + 1, i + 1, i);
            Vector3I jj = new Vector3I(b ? j : j + 1, j, j + 1);
            rr = new Vector3(Math.Abs(alpha), b ? ti : 1.0 - tj, b ? tj : 1.0 - ti);
            if (Math.Abs(VDIR.Z) >= Math.Abs(VDIR.X))
            {
                Vector3I tmp = ii;
                ii = -jj;
                jj = tmp;
            }
            if (Math.Abs(VDIR.X + -VDIR.Z) > 0.00001f)
            {
                ii *= (int)Math.Sign(VDIR.X + -VDIR.Z);
                jj *= (int)Math.Sign(VDIR.X + -VDIR.Z);
            }
            vv = new Vector3I(species * NT) + new Vector3I(viewNumber(ii.X, jj.X), viewNumber(ii.Y, jj.Y), viewNumber(ii.Z, jj.Z));
        }


        public override void Draw()
        {
            if (MySector.MainCamera == null)
                return;

            base.Draw();

            //Vector3D compassPosition = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector + MySector.MainCamera.LeftVector / 1.5f - MySector.MainCamera.UpVector / 2;
            //VRageRender.MyRenderProxy.DebugDrawAxis(MatrixD.CreateTranslation(compassPosition), 0.1f, false);
            //VRageRender.MyRenderProxy.DebugDrawSphere(compassPosition, 0.02f, Color.White, 0.4f, false);

            if (m_voxelMap != null)
            {
                VRageRender.MyRenderProxy.DebugDrawAxis(m_voxelMap.WorldMatrix, 100f, false);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES)
            {
                foreach (var entity in MyEntities.GetEntities())
                {
                    var piece = entity as MyFracturedPiece;
                    if (piece != null)
                    {
                        MyPhysicsDebugDraw.DebugDrawBreakable(piece.Physics.BreakableBody, piece.Physics.ClusterToWorld(Vector3D.Zero));
                    }
                }
            }

            return;

            VRageRender.MyRenderProxy.DebugDrawAxis(m_teapotMatrix, 100f, false);


            var viewVectors = new Matrix[s_viewVectorData.Length];
            for (int i = 0; i < s_viewVectorData.Length; i++)
            {
                Matrix mm = Matrix.Identity;
                string[] sp = s_viewVectorData[i].Split(',');
                mm.M11 = Convert.ToSingle(sp[0]);
                mm.M12 = Convert.ToSingle(sp[1]);
                mm.M13 = Convert.ToSingle(sp[2]);
                mm.M21 = Convert.ToSingle(sp[3]);
                mm.M22 = Convert.ToSingle(sp[4]);
                mm.M23 = Convert.ToSingle(sp[5]);
                mm.M31 = Convert.ToSingle(sp[6]);
                mm.M32 = Convert.ToSingle(sp[7]);
                mm.M33 = Convert.ToSingle(sp[8]);
                mm = Matrix.Normalize(mm);
                mm = mm * Matrix.CreateRotationX(MathHelper.PiOver2);
                mm.Up = -mm.Up;
                viewVectors[i] = mm;
            }
            
            
            

            VRageRender.MyRenderProxy.DebugDrawSphere(m_teapotMatrix.Translation, 1.5f, Color.White, 1, false);

            for (int i = 0; i < viewVectors.Length; i++)
            {
                Matrix am = viewVectors[i];
                am = am * m_teapotMatrix.GetOrientation();
                am.Translation = m_teapotMatrix.Translation - 20 * am.Forward;
                VRageRender.MyRenderProxy.DebugDrawAxis(am, 2, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(am.Translation, i.ToString(), Color.White, 0.5f, false);
                //VRageRender.MyRenderProxy.DebugDrawLine3D(m_teapotMatrix.Translation, m_teapotMatrix.Translation + 20 * s_viewVectors[i].Forward, Color.White, Color.Wheat, false);
            }


            Matrix[] viewVectors2 = new Matrix[181];
            Vector3D genPos = m_teapotMatrix.Translation + m_teapotMatrix.Right * 80;
            VRageRender.MyRenderProxy.DebugDrawSphere(genPos, 1.5f, Color.Yellow, 1, false);














            //for (int i = -N; i <= N; ++i)
            //{
            //    for (int j = -N + Math.Abs(i); j <= N - Math.Abs(i); ++j)
            //    {

            //        float x = (i + j) / (float)N;
            //        float y = (j - i) / (float)N;
            //        float angle = 90.0f - Math.Max(Math.Abs(x), Math.Abs(y)) * 90.0f;
            //        float alpha = x == 0.0f && y == 0.0f ? 0.0f : (float)Math.Atan2(y, x) / MathHelper.Pi * 180.0f;

            //        Matrix cameraToWorld = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateRotationX(MathHelper.ToRadians(-angle));
            //        Matrix worldToCamera = Matrix.Invert(cameraToWorld);

            //        //box3f b;
            //        //b = b.enlarge((worldToCamera * vec4f(-1.0, -1.0, zmin, 1.0)).xyz());
            //        //b = b.enlarge((worldToCamera * vec4f(+1.0, -1.0, zmin, 1.0)).xyz());
            //        //b = b.enlarge((worldToCamera * vec4f(-1.0, +1.0, zmin, 1.0)).xyz());
            //        //b = b.enlarge((worldToCamera * vec4f(+1.0, +1.0, zmin, 1.0)).xyz());
            //        //b = b.enlarge((worldToCamera * vec4f(-1.0, -1.0, zmax, 1.0)).xyz());
            //        //b = b.enlarge((worldToCamera * vec4f(+1.0, -1.0, zmax, 1.0)).xyz());
            //        //b = b.enlarge((worldToCamera * vec4f(-1.0, +1.0, zmax, 1.0)).xyz());
            //        //b = b.enlarge((worldToCamera * vec4f(+1.0, +1.0, zmax, 1.0)).xyz());
            //        //mat4f c2s = mat4f::orthoProjection(b.xmax, b.xmin, b.ymax, b.ymin, -2.0 * b.zmax, -2.0 * b.zmin);
            //        Matrix w2s = /*c2s */ worldToCamera * Matrix.CreateRotationZ(MathHelper.ToRadians(-90.0f - alpha));
            //        //vec3f dir = ((mat4f::rotatez(90.0 + alpha) * cameraToWorld) * vec4f(0.0, 0.0, 1.0, 0.0)).xyz();

            //        Matrix am = w2s;
            //        am.Translation = genPos - 20 * am.Forward;

            //        int view = i * (1 - Math.Abs(i)) + j + 2 * N * i + N * (N + 1);

            //        VRageRender.MyRenderProxy.DebugDrawAxis(am, 2, false);
            //        VRageRender.MyRenderProxy.DebugDrawText3D(am.Translation, view.ToString(), Color.White, 0.5f, false);
            //    }
            //}
















            //float angleStepX = MathHelper.Pi / (2 * N);
            //float angleStepY = MathHelper.TwoPi / (4 * N);
            //int k = 0;
            //for (float x = 0; x < MathHelper.Pi; x += angleStepX)
            //{
            //    for (float y = 0; y < MathHelper.TwoPi; y += angleStepY)
            //    {
            //        Matrix am = Matrix.CreateRotationX(x) * Matrix.CreateRotationZ(y);
            //        am.Translation = genPos - 20 * am.Forward;

            //        if ((am.Translation - genPos).Y >= 0)
            //        {
            //            VRageRender.MyRenderProxy.DebugDrawAxis(am, 2, false);
            //            VRageRender.MyRenderProxy.DebugDrawText3D(am.Translation, k.ToString(), Color.White, 0.5f, false);
            //            k++;
            //        }
            //    }
            //}

            //for (int i = 0; i < viewVectors2.Length; i++)
            //{
            //    viewVectors[i] = Matrix.Identity;
            //}


            
            //for (int i = 0; i < viewVectors2.Length; i++)
            //{
            //    Matrix am = viewVectors[i];
            //    am = am * m_teapotMatrix.GetOrientation();
            //    am.Translation = genPos - 20 * am.Forward;
            //    VRageRender.MyRenderProxy.DebugDrawAxis(am, 2, false);
            //    VRageRender.MyRenderProxy.DebugDrawText3D(am.Translation, i.ToString(), Color.White, 0.5f, false);
            //    //VRageRender.MyRenderProxy.DebugDrawLine3D(m_teapotMatrix.Translation, m_teapotMatrix.Translation + 20 * s_viewVectors[i].Forward, Color.White, Color.Wheat, false);
            //}






            Vector3I vv;
            Vector3 rr;
            Vector3 cdir = Vector3.Normalize((Vector3)(m_teapotMatrix.Translation - MySector.MainCamera.Position));
            Matrix invT = Matrix.Invert(m_teapotMatrix.GetOrientation());
            
            cdir = Vector3.TransformNormal(cdir, invT);
            cdir.Z *= -1;
            //cdir = Vector3.TransformNormal(cdir, Matrix.CreateRotationX(MathHelper.Pi));
            findViews(0, cdir, out vv, out rr);

            VRageRender.MyRenderProxy.DebugDrawText3D(m_teapotMatrix.Translation, vv.ToString() + " " + rr.ToString(), Color.White, 0.5f, false);


            BoundingBoxD bbox = new BoundingBoxD(-new Vector3D(20), new Vector3D(20));
            bbox.Translate(new Vector3D(0, 0, -30));
          //  VRageRender.MyRenderProxy.DebugDrawAABB(bbox, Color.White, 1, 1, false);


            Matrix m = MatrixD.Identity;
            m.Translation = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 50;

          //  VRageRender.MyRenderProxy.DebugDrawAxis(m, 100f, false);

            Vector3I vvLight;
            Vector3 rrLight;
            Vector3 ldir = -MySector.DirectionToSunNormalized;
            ldir.Z *= -1;
            //ldir.Y *= -1;
            findViews(0, ldir, out vvLight, out rrLight);

         //   VRageRender.MyRenderProxy.DebugDrawText3D(m_teapotMatrix.Translation, "L: " + vvLight.ToString() + " " + rrLight.ToString(), Color.White, 0.5f, false);
            VRageRender.MyRenderProxy.DebugDrawLine3D(m_teapotMatrix.Translation, m_teapotMatrix.Translation + MySector.DirectionToSunNormalized * 100, Color.Yellow, Color.Yellow, false);


            //VRageRender.MyRenderProxy.SetUvIndices(m_spawnedTree, vv, rr, vvLight, rrLight);
            //VRageRender.MyRenderProxy.SetImpostorMatrices(s_viewVectors);

            return;
            var mat = "WeaponLaser";
            float thickness = 0.05f;

            float spacing = 5;
            int count = 100;
            Vector4 color = Color.White.ToVector4();

            //Vector3 center = MySector.MainCamera.Position + new Vector3(0, -10, 0);
            Vector3 center = Vector3.Zero;
            Vector3 startPos = center - new Vector3(spacing * count / 2, 0, 0);

            for (int i = 0; i < count; i++)
            {
                Vector3D lineStart = startPos + new Vector3(spacing * i, 0, -spacing * count / 2);
                Vector3D lineEnd = startPos + new Vector3(spacing * i, 0, spacing * count / 2);

                Vector3D closestPoint = Vector3.Zero;
                Vector3D campos = MySector.MainCamera.Position;
                closestPoint = MyUtils.GetClosestPointOnLine(ref lineStart, ref lineEnd, ref campos);
                var distance = MySector.MainCamera.GetDistanceFromPoint(closestPoint);

                var lineThickness = thickness * MathHelper.Clamp(distance, 0.1f, 10);

                MySimpleObjectDraw.DrawLine(lineStart, lineEnd, mat, ref color, (float)lineThickness );
            }

        }

        void InsertTree()
        {
            MyDefinitionId id = new MyDefinitionId(MyObjectBuilderType.Parse("MyObjectBuilder_Tree"), "Tree04_v2");
            var itemDefinition = MyDefinitionManager.Static.GetEnvironmentItemDefinition(id);

            if (VRage.Game.Models.MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes != null)
            {
                var breakableShape = VRage.Game.Models.MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes[0].Clone();
                MatrixD worldMatrix = MatrixD.CreateWorld(MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition() +2 * MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward, Vector3.Forward, Vector3.Up);



                List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
                breakableShape.GetChildren(children);
                children[0].Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                var piece = MyDestructionHelper.CreateFracturePiece(breakableShape, ref worldMatrix, false, itemDefinition.Id, true);
            }

        }

        MyVoxelMap m_voxelMap;

        void InsertVoxelMap()
        {
            //MatrixD worldMatrix = MatrixD.CreateWorld(MySector.MainCamera.Position + 25 * MySector.MainCamera.ForwardVector, MySector.MainCamera.ForwardVector, MySector.MainCamera.UpVector);


            //string[] vms = new string[]
            //    {
            //        "small_horse_overhang",
            //        //"small_largestone",
            //        //"small_mediumstone",
            //        "small_overhang_flat",
            //        //"small_smallstone"
            //    };


            //Matrix wm = Matrix.CreateRotationX(-MathHelper.PiOver2);
            
            //var name = vms[0];
            //var fileName = MyWorldGenerator.GetVoxelPrefabPath(name);
            //var storage = Sandbox.Engine.Voxels.MyStorageBase.Load(fileName);
            //m_voxelMap = MyWorldGenerator.AddVoxelMap(name, storage, worldMatrix);

            //m_voxelMap.IsStaticForCluster = false;

        }

        List<MySkinnedEntity> m_skins = new List<MySkinnedEntity>();
        void SpawnSimpleSkinnedObject()
        {

            var skin = new MySkinnedEntity();

            MyObjectBuilder_Character ob = new MyObjectBuilder_Character();
            ob.EntityDefinitionId = new SerializableDefinitionId(typeof(MyObjectBuilder_Character), "Medieval_barbarian");
            ob.PositionAndOrientation = new VRage.MyPositionAndOrientation(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector, MySector.MainCamera.ForwardVector, MySector.MainCamera.UpVector);
            skin.Init(null, @"Models\Characters\Basic\ME_barbar.mwm", null, null);
            skin.Init(ob);

            MyEntities.Add(skin);

            var command = new MyAnimationCommand()
            {
                AnimationSubtypeName = "IdleBarbar",
                FrameOption = MyFrameOption.Loop,
                TimeScale = 1
            };
            skin.AddCommand(command);

            m_skins.Add(skin);
        }


        MatrixD m_teapotMatrix = Matrix.Identity;
        uint m_spawnedTree;

        void SpawnTrees()
        {
            //MatrixD worldMatrix = MatrixD.CreateWorld(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector, MySector.MainCamera.ForwardVector, MySector.MainCamera.UpVector);
            //worldMatrix = Matrix.CreateRotationY(MathHelper.Pi) * worldMatrix;

            //MatrixD worldMatrix = MatrixD.CreateWorld(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector, Vector3.Forward, Vector3.Up);

            m_teapotMatrix = MatrixD.CreateWorld(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector, MySector.MainCamera.ForwardVector, MySector.MainCamera.UpVector);
           // m_teapotMatrix = MatrixD.CreateWorld(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector, Vector3.Forward, Vector3.Up);
            //m_teapotMatrix = MatrixD.CreateWorld(new Vector3D(0, 0, -30), Vector3.Forward, Vector3.Up);
            

             RenderFlags flags = RenderFlags.CastShadows | RenderFlags.Visible;

             m_spawnedTree = VRageRender.MyRenderProxy.CreateRenderEntity(
                       "Tree",
                       @"Models\Environment\Trees\LeafTree.mwm",
                       m_teapotMatrix,
                       MyMeshDrawTechnique.MESH,
                       flags,
                       CullingOptions.Default,
                       Vector3.One,
                       Vector3.Zero,
                       0,
                       1000);

             //worldMatrix.Translation += 50 * worldMatrix.Right;

             //var renderObjectId2 = VRageRender.MyRenderProxy.CreateRenderEntity(
             //           "Tree",
             //           @"Models\Environment\Trees\PineTree_LOD1.mwm",
             //           worldMatrix,
             //           MyMeshDrawTechnique.MESH,
             //           flags,
             //           CullingOptions.Default,
             //           Vector3.One,
             //           Vector3.Zero,
             //           0,
             //           1000);
        }

        static void HighlightGScreen()
        {
            ////Inventory
            //var terminal = MyScreenManager.GetScreenWithFocus();
            //var terminalTabs = terminal.GetControlByName("TerminalTabs");
            //var inventory = ((MyGuiControlTabControl)terminalTabs).GetControlByName("PageInventory");
            //var leftInventory = ((MyGuiControlTabPage)inventory).GetControlByName("LeftInventory");
            //var rightInventory = ((MyGuiControlTabPage)inventory).GetControlByName("RightInventory");
            //var leftFirst = ((Sandbox.Game.Screens.Helpers.MyGuiControlInventoryOwner)((MyGuiControlList)leftInventory).GetControlByName("MyGuiControlInventoryOwner"));
            //var rightFirst = ((Sandbox.Game.Screens.Helpers.MyGuiControlInventoryOwner)((MyGuiControlList)rightInventory).GetControlByName("MyGuiControlInventoryOwner"));
            //var firstLeftGrid = leftFirst.ContentGrids[0];
            //var firstRightGrid = rightFirst.ContentGrids[0];

            //var drag = ((MyGuiControlTabPage)inventory).GetControlByName("MyGuiControlGridDragAndDrop");

            //MyGuiScreenHighlight.MyHighlightControl[] controlData =
            //    new MyGuiScreenHighlight.MyHighlightControl[]
            //    {
            //        new MyGuiScreenHighlight.MyHighlightControl()
            //        {
            //            Control = firstLeftGrid,
            //            Indices = new int[] {0 ,1, 2}
            //        },
            //        new MyGuiScreenHighlight.MyHighlightControl()
            //        {
            //            Control = drag,
            //        },
            //        new MyGuiScreenHighlight.MyHighlightControl()
            //        {
            //            Control = firstRightGrid,
            //            Indices = new int[] {0}
            //        },
            //    };

            //GScreen
            var gscreen = MyScreenManager.GetScreenWithFocus();
            var panel = gscreen.GetControlByName("ScrollablePanel").Elements[0];
            var drag = gscreen.GetControlByName("MyGuiControlGridDragAndDrop");
            var toolbar = gscreen.GetControlByName("MyGuiControlToolbar").Elements[2];

            MyGuiScreenHighlight.MyHighlightControl[] controlData =
                new MyGuiScreenHighlight.MyHighlightControl[]
                {
                    new MyGuiScreenHighlight.MyHighlightControl()
                    {
                        Control = panel,
                        Indices = new int[] {0 ,1, 2}
                    },
                    new MyGuiScreenHighlight.MyHighlightControl()
                    {
                        Control = drag,
                    },
                    new MyGuiScreenHighlight.MyHighlightControl()
                    {
                        Control = toolbar,
                        Indices = new int[] {0}
                    },
                };


            MyGuiScreenHighlight.HighlightControls(controlData);
        }
    }
}
