#region Using

using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Multiplayer;
using VRageMath;
using Sandbox.Game.Gui;
using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using Sandbox.Game.GameSystems;

#endregion

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_GravityGeneratorSphere))]
    class MyGravityGeneratorSphere : MyGravityGeneratorBase, IMyGravityGeneratorSphere
    {
        private new MyGravityGeneratorSphereDefinition BlockDefinition
        {
            get { return (MyGravityGeneratorSphereDefinition)base.BlockDefinition; }
        }

        private new MySyncGravityGeneratorSphere SyncObject;
        private const float DEFAULT_RADIUS = 100f;
        private float m_radius = DEFAULT_RADIUS;
        public float Radius
        {
            get { return m_radius; }
            set
            {
                if (m_radius != value)
                {
                    m_radius = value;
                    UpdateFieldShape();
                    RaisePropertiesChanged();
                }
            }
        }

        public override float GetRadius()
        {
            return m_radius;
        }

        private float m_defaultVolume;

        static MyGravityGeneratorSphere()
        {
            if (MyFakes.ENABLE_GRAVITY_GENERATOR_SPHERE)
            {
                var fieldRadius = new MyTerminalControlSlider<MyGravityGeneratorSphere>("Radius", MySpaceTexts.BlockPropertyTitle_GravityFieldRadius, MySpaceTexts.BlockPropertyDescription_GravityFieldRadius);
                fieldRadius.DefaultValue = DEFAULT_RADIUS;
                fieldRadius.Getter = (x) => x.m_radius;
                fieldRadius.Setter = (x, v) =>
                {
                    if (v < x.BlockDefinition.MinRadius)
                    {
                        v = x.BlockDefinition.MinRadius;
                    }
                    x.SyncObject.SendChangeGravityGeneratorRequest(v, x.GravityAcceleration);
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
                gravityAcceleration.SetLimits(-MyGravityProviderSystem.G, MyGravityProviderSystem.G);
                gravityAcceleration.DefaultValue = MyGravityProviderSystem.G;
                gravityAcceleration.Getter = (x) => x.GravityAcceleration;
                gravityAcceleration.Setter = (x, v) => x.SyncObject.SendChangeGravityGeneratorRequest(x.m_radius, v);
                gravityAcceleration.Writer = (x, result) => result.AppendDecimal(x.m_gravityAcceleration / MyGravityProviderSystem.G, 2).Append(" G");
                gravityAcceleration.EnableActions();
                MyTerminalControlFactory.AddControl(gravityAcceleration);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            var builder = (MyObjectBuilder_GravityGeneratorSphere)objectBuilder;
            m_radius = builder.Radius;
            m_gravityAcceleration = builder.GravityAcceleration;

            base.Init(objectBuilder, cubeGrid);
            
            SyncObject = new MySyncGravityGeneratorSphere(this);

            m_defaultVolume = (float)(Math.Pow(DEFAULT_RADIUS, BlockDefinition.ConsumptionPower) * Math.PI * 0.75);
	        
			if (CubeGrid.CreatePhysics)
				AddDebugRenderComponent(new Components.MyDebugRenderComponentGravityGeneratorSphere(this));
        }

	    protected override void InitializeSinkComponent()
	    {
			var sinkComp = new MyResourceSinkComponent();
			sinkComp.Init(
					BlockDefinition.ResourceSinkGroup,
					CalculateRequiredPowerInputForRadius(BlockDefinition.MaxRadius),
					CalculateRequiredPowerInput);
		    ResourceSink = sinkComp;

		    if (CubeGrid.CreatePhysics)
		    {
			    ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
			    ResourceSink.RequiredInputChanged += Receiver_RequiredInputChanged;
			    ResourceSink.Update();
				AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));
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
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPowered ? ResourceSink.RequiredInput : 0, DetailedInfo);
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

        float IMyGravityGeneratorSphere.Radius { get { return m_radius; } }
        float IMyGravityGeneratorSphere.Gravity { get { return GravityAcceleration;}}
    }
}

