#if !XB1

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using VRage;
using VRage.Common.Utils;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using VRage.Game.Entity;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Large Ship properties")]
    class MyGuiScreenDebugShipLargeProperties : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugShipLargeProperties()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("System large ship properties", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddLabel("Front light", Color.Yellow.ToVector4(), 1.2f);
            //AddSlider(new StringBuilder("Small thrust glare size"), 0.01f, 10.0f, null, MemberHelper.GetMember(() => MyThrust.LARGE_BLOCK_GLARE_SIZE_SMALL));
            //AddSlider(new StringBuilder("Large thrust glare size"), 0.01f, 10.0f, null, MemberHelper.GetMember(() => MyThrust.LARGE_BLOCK_GLARE_SIZE_LARGE));

            AddButton(new StringBuilder("Set min. build level"), onClick: OnClick_SetMinBuildLevel);
            AddButton(new StringBuilder("Upgrade build level"), onClick: OnClick_UpgradeBuildLevel);
            AddButton(new StringBuilder("Randomize build level"), onClick: OnClick_RandomizeBuildLevel);
            AddButton(new StringBuilder("Reset structural sim."), onClick: OnClick_ResetStructuralSimulation);
            AddCheckBox("Enable structural integrity", null, MemberHelper.GetMember(() => MyStructuralIntegrity.Enabled));
            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugShipLargeProperties";
        }

        private void OnClick_RandomizeBuildLevel(MyGuiControlButton button)
        {
            var ship = GetTargetShip();
            foreach (var block in ship.GetBlocks())
            {
                block.RandomizeBuildLevel();
                block.UpdateVisual();
            }
        }

        private void OnClick_UpgradeBuildLevel(MyGuiControlButton button)
        {
            var ship = GetTargetShip();
            foreach (var block in ship.GetBlocks())
            {
                block.UpgradeBuildLevel();
                block.UpdateVisual();
            }
        }

        private void OnClick_SetMinBuildLevel(MyGuiControlButton button)
        {
            var ship = GetTargetShip();
            foreach (var block in ship.GetBlocks())
            {
                block.SetToConstructionSite();
                block.UpdateVisual();
            }
        }

        private void OnClick_ResetStructuralSimulation(MyGuiControlButton button)
        {
            foreach (var entity in MyEntities.GetEntities())
            {
                var grid = entity as MyCubeGrid;
                if (grid == null)
                    continue;

                grid.ResetStructuralIntegrity();
            }
        }

        private MyCubeGrid GetTargetShip()
        {
            MyEntity entity = MyCubeBuilder.Static.FindClosestGrid();

            if (entity == null)
            {
                var line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 10000);

                List<MyLineSegmentOverlapResult<MyEntity>> result = new List<MyLineSegmentOverlapResult<MyEntity>>();
                MyEntities.OverlapAllLineSegment(ref line, result);
                if (result.Count > 0)
                {
                    entity = result.OrderBy(s => s.Distance).First().Element;

                    entity = entity.GetTopMostParent();
                }
            }

            return entity as MyCubeGrid;
        }
    }
}

#endif