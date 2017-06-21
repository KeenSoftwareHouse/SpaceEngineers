using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using VRageRender.Utils;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1

    [MyDebugScreen("Game", "AI")]
    class MyGuiScreenDebugAi : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugAi";
        }

        public MyGuiScreenDebugAi()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Debug screen AI", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddLabel("Options:", Color.OrangeRed.ToVector4(), 1.0f);

            AddCheckBox("Spawn barbarians near the player", null, MemberHelper.GetMember(() => MyFakes.BARBARIANS_SPAWN_NEAR_PLAYER)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Remove voxel navmesh cells", null, MemberHelper.GetMember(() => MyFakes.REMOVE_VOXEL_NAVMESH_CELLS)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Debug draw bots", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_BOTS)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("    * Bot steering", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_BOT_STEERING)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("    * Bot aiming", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_BOT_AIMING)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("    * Bot navigation", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_BOT_NAVIGATION));

            m_currentPosition.Y += 0.01f;
            AddLabel("Navmesh debug draw:", Color.OrangeRed.ToVector4(), 1.0f);

            AddCheckBox("Draw found path", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_FOUND_PATH)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Draw funnel path refining", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_FUNNEL)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Processed voxel navmesh cells", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_PROCESSED_VOXEL_CELLS)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Prepared voxel navmesh cells", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_PREPARED_VOXEL_CELLS)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Cells on paths", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_CELLS_ON_PATHS)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Voxel navmesh connection helper", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_VOXEL_CONNECTION_HELPER)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("Draw navmesh links", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_LINKS)); m_currentPosition.Y -= 0.005f;

            m_currentPosition.Y += 0.01f;
            AddLabel("Hierarchical pathfinding:", Color.OrangeRed.ToVector4(), 1.0f);
            AddCheckBox("Navmesh cell borders", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_CELL_BORDERS)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("HPF (draw navmesh hierarchy)", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("    * (Lite version)", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY_LITE)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("    + Explored HL cells", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_EXPLORED_HL_CELLS)); m_currentPosition.Y -= 0.005f;
            AddCheckBox("    + Fringe HL cells", null, MemberHelper.GetMember(() => MyFakes.DEBUG_DRAW_NAVMESH_FRINGE_HL_CELLS)); m_currentPosition.Y -= 0.005f;

            m_currentPosition.Y += 0.01f;
            AddLabel("Winged-edge mesh debug draw:", Color.OrangeRed.ToVector4(), 1.0f);

            var savedPosition = m_currentPosition;
            AddCheckBox("    Lines",
                (Func<bool>)(() => (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.LINES) != 0),
                (Action<bool>)((bool b) => MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES = b ? MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES | MyWEMDebugDrawMode.LINES : MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & ~MyWEMDebugDrawMode.LINES),
                checkBoxOffset: new Vector2(-0.15f, 0.0f));
            m_currentPosition = savedPosition + new Vector2(0.15f, 0.0f);
            AddCheckBox("    Lines Z-culled",
                (Func<bool>)(() => (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.LINES_DEPTH) != 0),
                (Action<bool>)((bool b) => MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES = b ? MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES | MyWEMDebugDrawMode.LINES_DEPTH : MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & ~MyWEMDebugDrawMode.LINES_DEPTH),
                checkBoxOffset: new Vector2(-0.15f, 0.0f));

            m_currentPosition.X = savedPosition.X;
            savedPosition = m_currentPosition;
            AddCheckBox("    Edges",
                (Func<bool>)(() => (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.EDGES) != 0),
                (Action<bool>)((bool b) => MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES = b ? MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES | MyWEMDebugDrawMode.EDGES : MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & ~MyWEMDebugDrawMode.EDGES),
                checkBoxOffset: new Vector2(-0.15f, 0.0f));
            m_currentPosition = savedPosition + new Vector2(0.15f, 0.0f);
            AddCheckBox("    Faces",
                (Func<bool>)(() => (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.FACES) != 0),
                (Action<bool>)((bool b) => MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES = b ? MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES | MyWEMDebugDrawMode.FACES : MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & ~MyWEMDebugDrawMode.FACES),
                checkBoxOffset: new Vector2(-0.15f, 0.0f));

            m_currentPosition.X = savedPosition.X;
            savedPosition = m_currentPosition;
            AddCheckBox("    Vertices",
                (Func<bool>)(() => (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.VERTICES) != 0),
                (Action<bool>)((bool b) => MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES = b ? MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES | MyWEMDebugDrawMode.VERTICES : MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & ~MyWEMDebugDrawMode.VERTICES),
                checkBoxOffset: new Vector2(-0.15f, 0.0f));
            m_currentPosition = savedPosition + new Vector2(0.15f, 0.0f);
            AddCheckBox("    Vertices detailed",
                (Func<bool>)(() => (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.VERTICES_DETAILED) != 0),
                (Action<bool>)((bool b) => MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES = b ? MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES | MyWEMDebugDrawMode.VERTICES_DETAILED : MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & ~MyWEMDebugDrawMode.VERTICES_DETAILED),
                checkBoxOffset: new Vector2(-0.15f, 0.0f));

            m_currentPosition.X = savedPosition.X;
            savedPosition = m_currentPosition;
            AddCheckBox("    Normals",
                (Func<bool>)(() => (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & MyWEMDebugDrawMode.NORMALS) != 0),
                (Action<bool>)((bool b) => MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES = b ? MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES | MyWEMDebugDrawMode.NORMALS : MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES & ~MyWEMDebugDrawMode.NORMALS),
                checkBoxOffset: new Vector2(-0.15f, 0.0f));

            AddCheckBox("Animals", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_ANIMALS));
            AddCheckBox("Spiders", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_FAUNA_COMPONENT));

            var checkbox = AddCheckBox("Switch Survival/Creative", 
                (Func<bool>)(() => MySession.Static.CreativeMode), 
                (Action<bool>)((bool b) => MySession.Static.Settings.GameMode = b ? MyGameModeEnum.Creative : MyGameModeEnum.Survival));            

        }
    }

#endif
}
