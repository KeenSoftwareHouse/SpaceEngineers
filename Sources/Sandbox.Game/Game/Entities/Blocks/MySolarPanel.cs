using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Utils;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SolarPanel))]
    class MySolarPanel : MyTerminalBlock, Sandbox.ModAPI.Ingame.IMySolarPanel
    {
        static readonly string[] m_emissiveNames = new string[] { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

	    public MySolarPanelDefinition SolarPanelDefinition { get; private set; }
        public MySolarGameLogicComponent SolarComponent { get; private set; }
        protected MyEntity3DSoundEmitter m_soundEmitter;
        internal MyEntity3DSoundEmitter SoundEmitter { get { return m_soundEmitter; } }

	    private MyResourceSourceComponent m_sourceComponent;
		public MyResourceSourceComponent SourceComp
		{
			get { return m_sourceComponent; }
			set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComponent = value; }
		}

	    public MySolarPanel()
	    {
            SourceComp = new MyResourceSourceComponent();

            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
	    }

	    public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            SolarPanelDefinition = BlockDefinition as MySolarPanelDefinition;
            IsWorkingChanged += OnIsWorkingChanged;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

		    var sourceDataList = new List<MyResourceSourceInfo>
		    {
			    new MyResourceSourceInfo {ResourceTypeId = MyResourceDistributorComponent.ElectricityId, DefinedOutput = SolarPanelDefinition.MaxPowerOutput, IsInfiniteCapacity = true, ProductionToCapacityMultiplier = 60*60}
		    };

			SourceComp.Init(SolarPanelDefinition.ResourceSourceGroup, sourceDataList);

            GameLogic = new MySolarGameLogicComponent();
            SolarComponent = GameLogic as MySolarGameLogicComponent;

            SolarComponent.Initialize(SolarPanelDefinition.PanelOrientation, SolarPanelDefinition.IsTwoSided, SolarPanelDefinition.PanelOffset, this);

            AddDebugRenderComponent(new Components.MyDebugRenderComponentSolarPanel(this));
        }

        internal void UpdateDisplay()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxOutput));
            MyValueFormatter.AppendWorkInBestUnit(SourceComp.MaxOutput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentOutput));
			MyValueFormatter.AppendWorkInBestUnit(SourceComp.CurrentOutput, DetailedInfo);
            RaisePropertiesChanged();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;
            if (!IsWorking)
            {
                for (int i = 0; i < 4; i++)
                {
                    VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], Color.Black, 0);
                }
                return;
            }
			if (SourceComp.MaxOutput > 0)
            {
                for (int i = 0; i < 4; i++)
                {
					if (i < (SourceComp.MaxOutput / SolarPanelDefinition.MaxPowerOutput) * 4)
                        VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], Color.Green, 1);
                    else
                        VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], Color.Black, 1);
                }
            }
            else
            {
                VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[0], Color.Red, 0);
                for (int i = 1; i < 4; i++)
                {
                    VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], Color.Red, 0);
                }
            }
        }

        void OnIsWorkingChanged(MyCubeBlock obj)
        {
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            UpdateEmissivity();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            m_soundEmitter.Update();
            if (CubeGrid.Physics == null)
                return;

            float maxPowerOutput = SolarComponent.MaxOutput * SolarPanelDefinition.MaxPowerOutput;

			if (maxPowerOutput != SourceComp.MaxOutput)
			{
			    float oldPowerOutput = SourceComp.MaxOutput;
				SourceComp.SetMaxOutput(maxPowerOutput);
			    if (oldPowerOutput != maxPowerOutput)
			    {
			        SourceComp.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, maxPowerOutput != 0f);
			        UpdateDisplay();
			    }
            }
        }

        internal override void SetDamageEffect(bool show)
        {
            if (BlockDefinition.DamagedSound != null)
                if (show)
                    m_soundEmitter.PlaySound(BlockDefinition.DamagedSound, true);
                else
                    if (m_soundEmitter.SoundId == BlockDefinition.DamagedSound.SoundId)
                        m_soundEmitter.StopSound(false);
            base.SetDamageEffect(show);
        }
        internal override void StopDamageEffect()
        {
            if (BlockDefinition.DamagedSound != null && m_soundEmitter.SoundId == BlockDefinition.DamagedSound.SoundId)
                m_soundEmitter.StopSound(true);
            base.StopDamageEffect();
        }

#region IMySolarPanel interface

        public float CurrentOutput
        {
            get { if (SourceComp != null) return SourceComp.CurrentOutput; return 0; }
        }

        public float MaxOutput
        {
            get { if (SourceComp != null) return SourceComp.MaxOutput; return 0; }
        }

#endregion
    }
}
