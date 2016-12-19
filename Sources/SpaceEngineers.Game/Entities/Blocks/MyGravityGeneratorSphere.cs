#region Using

using System;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.EntityComponents.DebugRenders;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

#endregion

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_GravityGeneratorSphere))]
    public class MyGravityGeneratorSphere : MyGravityGeneratorBase, IMyGravityGeneratorSphere
    {
        private new MyGravityGeneratorSphereDefinition BlockDefinition
        {
            get { return (MyGravityGeneratorSphereDefinition)base.BlockDefinition; }
        }

        private const float DEFAULT_RADIUS = 100f;
        private readonly Sync<float> m_radius;
        public float Radius
        {
            get { return m_radius; }
            set
            {
                m_radius.Value = value;
            }
        }

        public override float GetRadius()
        {
            return m_radius;
        }

        private float m_defaultVolume;

        private float MaxInput { get { return (float)(Math.Pow(BlockDefinition.MaxRadius, BlockDefinition.ConsumptionPower) / (float)(Math.Pow(DEFAULT_RADIUS, BlockDefinition.ConsumptionPower)) * BlockDefinition.BasePowerInput); } }

        public MyGravityGeneratorSphere()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_radius = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();
            m_radius.ValueChanged += (x) => UpdateFieldShape();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyGravityGeneratorSphere>())
                return;
            base.CreateTerminalControls();
            if (MyFakes.ENABLE_GRAVITY_GENERATOR_SPHERE)
            {
                var fieldRadius = new MyTerminalControlSlider<MyGravityGeneratorSphere>("Radius", MySpaceTexts.BlockPropertyTitle_GravityFieldRadius, MySpaceTexts.BlockPropertyDescription_GravityFieldRadius);
                fieldRadius.DefaultValue = DEFAULT_RADIUS;
                fieldRadius.Getter = (x) => x.Radius;
                fieldRadius.Setter = (x, v) =>
                {
                    if (v < x.BlockDefinition.MinRadius)
                    {
                        v = x.BlockDefinition.MinRadius;
                    }
                    x.Radius = v;
                };
                fieldRadius.Normalizer = (x, v) =>
                {
                    if (v == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return (v - x.BlockDefinition.MinRadius) / (x.BlockDefinition.MaxRadius - x.BlockDefinition.MinRadius);
                    }
                };
                fieldRadius.Denormalizer = (x, v) =>
                {
                    if (v == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return v * (x.BlockDefinition.MaxRadius - x.BlockDefinition.MinRadius) + x.BlockDefinition.MinRadius;
                    }
                };
                fieldRadius.Writer = (x, result) => result.AppendInt32((int)x.m_radius).Append(" m");
                fieldRadius.EnableActions();
                MyTerminalControlFactory.AddControl(fieldRadius);

                var gravityAcceleration = new MyTerminalControlSlider<MyGravityGeneratorSphere>("Gravity", MySpaceTexts.BlockPropertyTitle_GravityAcceleration, MySpaceTexts.BlockPropertyDescription_GravityAcceleration);
                gravityAcceleration.SetLimits((x) => x.BlockDefinition.MinGravityAcceleration, (x) => x.BlockDefinition.MaxGravityAcceleration);
                gravityAcceleration.DefaultValue = MyGravityProviderSystem.G;
                gravityAcceleration.Getter = (x) => x.GravityAcceleration;
                gravityAcceleration.Setter = (x, v) => x.GravityAcceleration =  v;
                gravityAcceleration.Writer = (x, result) => result.AppendDecimal(x.m_gravityAcceleration / MyGravityProviderSystem.G, 2).Append(" G");
                gravityAcceleration.EnableActions();
                MyTerminalControlFactory.AddControl(gravityAcceleration);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            var builder = (MyObjectBuilder_GravityGeneratorSphere)objectBuilder;
            Radius = builder.Radius;
            GravityAcceleration = builder.GravityAcceleration;

            m_defaultVolume = (float)(Math.Pow(DEFAULT_RADIUS, BlockDefinition.ConsumptionPower) * Math.PI * 0.75);
	        
			if (CubeGrid.CreatePhysics)
				AddDebugRenderComponent(new MyDebugRenderComponentGravityGeneratorSphere(this));
        }

	    protected override void InitializeSinkComponent()
	    {
			var sinkComp = new MyResourceSinkComponent();
			sinkComp.Init(
					BlockDefinition.ResourceSinkGroup,
                    MaxInput,
					CalculateRequiredPowerInput);
		    ResourceSink = sinkComp;

		    if (CubeGrid.CreatePhysics)
		    {
			    ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
			    ResourceSink.RequiredInputChanged += Receiver_RequiredInputChanged;
				AddDebugRenderComponent(new Sandbox.Game.Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));
		    }
	    }

	    public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_GravityGeneratorSphere)base.GetObjectBuilderCubeBlock(copy);

            builder.Radius = m_radius;
            builder.GravityAcceleration = m_gravityAcceleration;

            return builder;
        }

        public override void UpdateBeforeSimulation()
        {
            if (MyFakes.ENABLE_GRAVITY_GENERATOR_SPHERE)
            {
                base.UpdateBeforeSimulation();
            }
        }

        protected override float CalculateRequiredPowerInput()
        {
            if (Enabled && IsFunctional)
            {
                return CalculateRequiredPowerInputForRadius(m_radius);
            }
            else
            {
                return 0.0f;
            }
        }

        private float CalculateRequiredPowerInputForRadius(float radius)
        {
            float volume = (float)(Math.Pow(radius, BlockDefinition.ConsumptionPower) * Math.PI * 0.75);
            return (volume / m_defaultVolume) * BlockDefinition.BasePowerInput * (Math.Abs(m_gravityAcceleration) / MyGravityProviderSystem.G);
        }

        protected override void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0, DetailedInfo);
            RaisePropertiesChanged();
        }

        public override bool IsPositionInRange(Vector3D worldPoint)
        {
            return (WorldMatrix.Translation - worldPoint).Length() < m_radius;
        }

        public override Vector3 GetWorldGravity(Vector3D worldPoint)
        {
            Vector3D direction = WorldMatrix.Translation - worldPoint;
            direction.Normalize();
            return (Vector3)direction * GravityAcceleration;
        }

        protected override HkShape GetHkShape()
        {
            return new HkSphereShape(m_radius);
        }

        #region ModAPI
        float ModAPI.IMyGravityGeneratorSphere.Radius
        {
            get { return Radius; }
            set { Radius = value; }
        }

        float ModAPI.Ingame.IMyGravityGeneratorSphere.Radius
        {
            get { return Radius; }
            set { Radius = MathHelper.Clamp(value, BlockDefinition.MinRadius, BlockDefinition.MaxRadius); }
        }
        #endregion ModAPI
    }
}

