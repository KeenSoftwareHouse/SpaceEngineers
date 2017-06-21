using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Terminal.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Projector))]
    public class MySpaceProjector : MyProjectorBase
    {
        public MySpaceProjector()
        {
            CreateTerminalControls();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MySpaceProjector>())
                return;
            base.CreateTerminalControls();
            if (!MyFakes.ENABLE_PROJECTOR_BLOCK)
            {
                return;
            }

            var blueprintBtn = new MyTerminalControlButton<MySpaceProjector>("Blueprint", MyCommonTexts.Blueprints, MySpaceTexts.Blank, (p) => p.SelectBlueprint());
            blueprintBtn.Enabled = (b) => b.CanProject();
            blueprintBtn.SupportsMultipleBlocks = false;

            MyTerminalControlFactory.AddControl(blueprintBtn);

            var removeBtn = new MyTerminalControlButton<MySpaceProjector>("Remove", MySpaceTexts.RemoveProjectionButton, MySpaceTexts.Blank, (p) => p.SendRemoveProjection());
            removeBtn.Enabled = (b) => b.IsProjecting();
            MyTerminalControlFactory.AddControl(removeBtn);

            var keepProjectionToggle = new MyTerminalControlCheckbox<MySpaceProjector>("KeepProjection", MySpaceTexts.KeepProjectionToggle, MySpaceTexts.KeepProjectionTooltip);
            keepProjectionToggle.Getter = (x) => x.KeepProjection;
            keepProjectionToggle.Setter = (x, v) => x.KeepProjection = v;
            keepProjectionToggle.EnableAction();
            keepProjectionToggle.Enabled = (b) => b.IsProjecting();
            MyTerminalControlFactory.AddControl(keepProjectionToggle);

            //ShowOnlyBuildable
            var showOnlyBuildableBlockToggle = new MyTerminalControlCheckbox<MySpaceProjector>("ShowOnlyBuildable", MySpaceTexts.ShowOnlyBuildableBlockToggle, MySpaceTexts.ShowOnlyBuildableTooltip);
            showOnlyBuildableBlockToggle.Getter = (x) => x.m_showOnlyBuildable;
            showOnlyBuildableBlockToggle.Setter = (x, v) =>
            {
                x.m_showOnlyBuildable = v;
                x.OnOffsetsChanged();
            };
            showOnlyBuildableBlockToggle.Enabled = (b) => b.IsProjecting();
            MyTerminalControlFactory.AddControl(showOnlyBuildableBlockToggle);

            //Position
            var offsetX = new MyTerminalControlSlider<MySpaceProjector>("X", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetX, MySpaceTexts.Blank);
            offsetX.SetLimits(-50, 50);
            offsetX.DefaultValue = 0;
            offsetX.Getter = (x) => x.m_projectionOffset.X;
            offsetX.Setter = (x, v) =>
            {
                x.m_projectionOffset.X = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            offsetX.Writer = (x, result) => result.AppendInt32((int)(x.m_projectionOffset.X));
            offsetX.EnableActions(step: 0.01f);
            offsetX.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(offsetX);

            var offsetY = new MyTerminalControlSlider<MySpaceProjector>("Y", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetY, MySpaceTexts.Blank);
            offsetY.SetLimits(-50, 50);
            offsetY.DefaultValue = 0;
            offsetY.Getter = (x) => x.m_projectionOffset.Y;
            offsetY.Setter = (x, v) =>
            {
                x.m_projectionOffset.Y = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            offsetY.Writer = (x, result) => result.AppendInt32((int)(x.m_projectionOffset.Y));
            offsetY.EnableActions(step: 0.01f);
            offsetY.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(offsetY);

            var offsetZ = new MyTerminalControlSlider<MySpaceProjector>("Z", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetZ, MySpaceTexts.Blank);
            offsetZ.SetLimits(-50, 50);
            offsetZ.DefaultValue = 0;
            offsetZ.Getter = (x) => x.m_projectionOffset.Z;
            offsetZ.Setter = (x, v) =>
            {
                x.m_projectionOffset.Z = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            offsetZ.Writer = (x, result) => result.AppendInt32((int)(x.m_projectionOffset.Z));
            offsetZ.EnableActions(step: 0.01f);
            offsetZ.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(offsetZ);

            //Rotation

            var rotationX = new MyTerminalControlSlider<MySpaceProjector>("RotX", MySpaceTexts.BlockPropertyTitle_ProjectionRotationX, MySpaceTexts.Blank);
            rotationX.SetLimits(-2, 2);
            rotationX.DefaultValue = 0;
            rotationX.Getter = (x) => x.m_projectionRotation.X;
            rotationX.Setter = (x, v) =>
            {
                x.m_projectionRotation.X = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            rotationX.Writer = (x, result) => result.AppendInt32((int)x.m_projectionRotation.X * 90).Append("°");
            rotationX.EnableActions(step: 0.2f);
            rotationX.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(rotationX);

            var rotationY = new MyTerminalControlSlider<MySpaceProjector>("RotY", MySpaceTexts.BlockPropertyTitle_ProjectionRotationY, MySpaceTexts.Blank);
            rotationY.SetLimits(-2, 2);
            rotationY.DefaultValue = 0;
            rotationY.Getter = (x) => x.m_projectionRotation.Y;
            rotationY.Setter = (x, v) =>
            {
                x.m_projectionRotation.Y = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            rotationY.Writer = (x, result) => result.AppendInt32((int)x.m_projectionRotation.Y * 90).Append("°");
            rotationY.EnableActions(step: 0.2f);
            rotationY.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(rotationY);

            var rotationZ = new MyTerminalControlSlider<MySpaceProjector>("RotZ", MySpaceTexts.BlockPropertyTitle_ProjectionRotationZ, MySpaceTexts.Blank);
            rotationZ.SetLimits(-2, 2);
            rotationZ.DefaultValue = 0;
            rotationZ.Getter = (x) => x.m_projectionRotation.Z;
            rotationZ.Setter = (x, v) =>
            {
                x.m_projectionRotation.Z = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            rotationZ.Writer = (x, result) => result.AppendInt32((int)x.m_projectionRotation.Z * 90).Append("°");
            rotationZ.EnableActions(step: 0.2f);
            rotationZ.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(rotationZ);

            var scenarioSettingsSeparator = new MyTerminalControlSeparator<MySpaceProjector>();
            scenarioSettingsSeparator.Visible = (p) => p.ScenarioSettingsEnabled();
            MyTerminalControlFactory.AddControl(scenarioSettingsSeparator);

            var scenarioSettingsLabel = new MyTerminalControlLabel<MySpaceProjector>(MySpaceTexts.TerminalScenarioSettingsLabel);
            scenarioSettingsLabel.Visible = (p) => p.ScenarioSettingsEnabled();
            MyTerminalControlFactory.AddControl(scenarioSettingsLabel);

            var spawnProjectionButton = new MyTerminalControlButton<MySpaceProjector>("SpawnProjection", MySpaceTexts.BlockPropertyTitle_ProjectionSpawn, MySpaceTexts.Blank, (p) => p.TrySpawnProjection());
            spawnProjectionButton.Visible = (p) => p.ScenarioSettingsEnabled();
            spawnProjectionButton.Enabled = (p) => p.CanSpawnProjection();
            spawnProjectionButton.EnableAction();
            MyTerminalControlFactory.AddControl(spawnProjectionButton);

            var instantBuildingCheckbox = new MyTerminalControlCheckbox<MySpaceProjector>("InstantBuilding", MySpaceTexts.BlockPropertyTitle_Projector_InstantBuilding, MySpaceTexts.BlockPropertyTitle_Projector_InstantBuilding_Tooltip);
            instantBuildingCheckbox.Visible = (p) => p.ScenarioSettingsEnabled();
            instantBuildingCheckbox.Enabled = (p) => p.CanEnableInstantBuilding();
            instantBuildingCheckbox.Getter = (p) => p.InstantBuildingEnabled;
            instantBuildingCheckbox.Setter = (p, v) => p.TrySetInstantBuilding(v);
            MyTerminalControlFactory.AddControl(instantBuildingCheckbox);

            var getOwnershipCheckbox = new MyTerminalControlCheckbox<MySpaceProjector>("GetOwnership", MySpaceTexts.BlockPropertyTitle_Projector_GetOwnership, MySpaceTexts.BlockPropertiesTooltip_Projector_GetOwnership);
            getOwnershipCheckbox.Visible = (p) => p.ScenarioSettingsEnabled();
            getOwnershipCheckbox.Enabled = (p) => p.CanEditInstantBuildingSettings();
            getOwnershipCheckbox.Getter = (p) => p.GetOwnershipFromProjector;
            getOwnershipCheckbox.Setter = (p, v) => p.TrySetGetOwnership(v);
            MyTerminalControlFactory.AddControl(getOwnershipCheckbox);

            var numberOfProjections = new MyTerminalControlSlider<MySpaceProjector>("NumberOfProjections", MySpaceTexts.BlockPropertyTitle_Projector_NumberOfProjections, MySpaceTexts.BlockPropertyTitle_Projector_NumberOfProjections_Tooltip);
            numberOfProjections.Visible = (p) => p.ScenarioSettingsEnabled();
            numberOfProjections.Enabled = (p) => p.CanEditInstantBuildingSettings();
            numberOfProjections.Getter = (p) => p.MaxNumberOfProjections;
            numberOfProjections.Setter = (p, v) => p.TryChangeNumberOfProjections(v);
            numberOfProjections.Writer = (p, s) =>
            {
                if (p.MaxNumberOfProjections == MAX_NUMBER_OF_PROJECTIONS)
                {
                    s.AppendStringBuilder(MyTexts.Get(MySpaceTexts.ScreenTerminal_Infinite));
                }
                else
                {
                    s.AppendInt32(p.MaxNumberOfProjections);
                }
            };
            numberOfProjections.SetLogLimits(1, MAX_NUMBER_OF_PROJECTIONS);
            MyTerminalControlFactory.AddControl(numberOfProjections);

            var numberOfBlocks = new MyTerminalControlSlider<MySpaceProjector>("NumberOfBlocks", MySpaceTexts.BlockPropertyTitle_Projector_BlocksPerProjection, MySpaceTexts.BlockPropertyTitle_Projector_BlocksPerProjection_Tooltip);
            numberOfBlocks.Visible = (p) => p.ScenarioSettingsEnabled();
            numberOfBlocks.Enabled = (p) => p.CanEditInstantBuildingSettings();
            numberOfBlocks.Getter = (p) => p.MaxNumberOfBlocksPerProjection;
            numberOfBlocks.Setter = (p, v) => p.TryChangeMaxNumberOfBlocksPerProjection(v);
            numberOfBlocks.Writer = (p, s) =>
            {
                if (p.MaxNumberOfBlocksPerProjection == MAX_NUMBER_OF_BLOCKS)
                {
                    s.AppendStringBuilder(MyTexts.Get(MySpaceTexts.ScreenTerminal_Infinite));
                }
                else
                {
                    s.AppendInt32(p.MaxNumberOfBlocksPerProjection);
                }
            };
            numberOfBlocks.SetLogLimits(1, MAX_NUMBER_OF_BLOCKS);
            MyTerminalControlFactory.AddControl(numberOfBlocks);
        }

        protected override bool CheckIsWorking()
        {

            if (ResourceSink != null && !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                return false;
            }

            // not sure if it was correct earlier
            //return Enabled && base.CheckIsWorking();

            return base.CheckIsWorking();

        }
    }
}
