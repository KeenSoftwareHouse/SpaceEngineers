using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using VRage;
using VRageMath;

namespace Sandbox.Game.Screens.DebugScreens
{

#if !XB1_TMP
    [MyDebugScreen("VRage", "Debug draw settings 2")]
    class MyGuiScreenDebugDrawSettings2 : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugDrawSettings";
        }

        public MyGuiScreenDebugDrawSettings2()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Debug draw settings 2", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddCheckBox("Entity components", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_ENTITY_COMPONENTS));
            AddCheckBox("Grid names", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_GRID_NAMES));
            AddCheckBox("Grid control", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_GRID_CONTROL));
            AddCheckBox("Controlled entities", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CONTROLLED_ENTITIES));
            AddCheckBox("Conveyor line IDs", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS_LINE_IDS));
            AddCheckBox("Conveyor line capsules", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS_LINE_CAPSULES));
            AddCheckBox("Ship tools", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_SHIP_TOOLS));
            AddCheckBox("Removed cube coordinates", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_REMOVE_CUBE_COORDS));
            AddCheckBox("Thrash removal", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_TRASH_REMOVAL));
            AddCheckBox("Grid counter", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_GRID_COUNTER));
            AddCheckBox("Grid terminal systems", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_GRID_TERMINAL_SYSTEMS));
            AddCheckBox("Grid dirty blocks", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_GRID_DIRTY_BLOCKS));
            AddCheckBox("Connectors and merge blocks", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_CONNECTORS_AND_MERGE_BLOCKS));
            AddCheckBox("Copy paste", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE));
            AddCheckBox("Grid origins", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_GRID_ORIGINS));
            AddCheckBox("Thruster damage", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_THRUSTER_DAMAGE));
            AddCheckBox("Block groups", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_BLOCK_GROUPS));
            AddCheckBox("Rotors", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_ROTORS));
            AddCheckBox("Gyros", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_GYROS));
            AddCheckBox("Voxel geometry cell", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_VOXEL_GEOMETRY_CELL));
            AddCheckBox("Voxel map AABB", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_VOXEL_MAP_AABB));
            AddCheckBox("Respawn ship counters", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_RESPAWN_SHIP_COUNTERS));
            AddCheckBox("Explosion Havok raycasts", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_HAVOK_RAYCASTS));
            AddCheckBox("Explosion DDA raycasts", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_EXPLOSION_DDA_RAYCASTS));
            AddCheckBox("Physics clusters", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS));
            AddCheckBox("Environment items (trees, bushes, ...)", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_ENVIRONMENT_ITEMS));
            AddCheckBox("Block groups - small to large", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_SMALL_TO_LARGE_BLOCK_GROUPS));
            AddCheckBox("Ropes", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_ROPES));
            AddCheckBox("Oxygen", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_OXYGEN));
            AddCheckBox("Voxel physics prediction", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_VOXEL_PHYSICS_PREDICTION));
            AddCheckBox("Update trigger", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER));
        }
    }
#endif
}
