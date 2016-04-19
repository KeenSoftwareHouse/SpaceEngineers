using System;
using System.Collections.Generic;
using System.Diagnostics;
using Havok;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Utils;

using VRage;
using VRage.Generics;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender;

using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities.Debris
{

    /// <summary>
    /// This class has two functions: it encapsulated one instance of voxel debris object, and also holds static prepared/preallocated 
    /// object pool for those instances - and if needed adds them to main phys objects collection for short life.
    /// It's because initializing JLX object is slow and we don't want to initialize 20 (or so) objects for every explosion.
    /// </summary>
    class MyDebrisVoxel : MyDebrisBase
    {
        public MyDebrisVoxel()
        {
            GameLogic = new MyDebrisVoxelLogic();
            Render = new Components.MyRenderComponentDebrisVoxel();
        }

        internal class MyDebrisVoxelPhysics : MyDebrisBase.MyDebrisPhysics
        {
            private IMyEntity Entity1;
            private RigidBodyFlag rigidBodyFlag;
            private const float VoxelDensity = 260;
            public MyDebrisVoxelPhysics(IMyEntity Entity1, RigidBodyFlag rigidBodyFlag) : base(Entity1, rigidBodyFlag)
            {
            }
            
            public override void CreatePhysicsShape(out HkShape shape, ref HkMassProperties massProperties)
            {
                var sphereShape = new HkSphereShape(((MyEntity)Entity).Render.GetModel().BoundingSphere.Radius * Entity.PositionComp.Scale.Value);
                shape = sphereShape;
                var mass = SphereMass(sphereShape.Radius, VoxelDensity);
                massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(sphereShape.Radius, mass);
            }

            public override void ScalePhysicsShape(ref HkMassProperties massProperties)
            {
                var shape = RigidBody.GetShape();
                var sphereShape = (HkSphereShape)shape;
                sphereShape.Radius = ((MyEntity)Entity).Render.GetModel().BoundingSphere.Radius * Entity.PositionComp.Scale.Value;
                var mass = SphereMass(sphereShape.Radius, VoxelDensity);
                massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(sphereShape.Radius, mass);

                RigidBody.SetShape(sphereShape);
                RigidBody.SetMassProperties(ref massProperties);
                RigidBody.UpdateShape();
            }

            private float SphereMass(float radius, float density)
            {
                return radius * radius * radius * MathHelper.Pi * 4 * 0.333f * density;
            }
        }
        internal class MyDebrisVoxelLogic : MyDebrisBase.MyDebrisBaseLogic
        {
            protected override MyPhysicsComponentBase GetPhysics(RigidBodyFlag rigidBodyFlag)
            {
                return new MyDebrisVoxelPhysics(Container.Entity, rigidBodyFlag);
            }
            /// <summary>
            /// Somewhat of a hack to add setting of default voxel material when calling base version of the method.
            /// </summary>
            public override void Start(Vector3D position, Vector3D initialVelocity, float scale)
            {
                this.Start(position, initialVelocity, scale, MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition());
            }

            public void Start(Vector3D position, Vector3D initialVelocity, float scale, MyVoxelMaterialDefinition mat)
            {
                Components.MyRenderComponentDebrisVoxel voxelDebrisRender = Container.Entity.Render as Components.MyRenderComponentDebrisVoxel;

                voxelDebrisRender.TexCoordOffset = MyUtils.GetRandomFloat(5, 15);
                voxelDebrisRender.TexCoordScale = MyUtils.GetRandomFloat(8, 12);
                voxelDebrisRender.VoxelMaterialIndex = mat.Index;
                base.Start(position, initialVelocity, scale);
                Container.Entity.Render.NeedsResolveCastShadow = true;
                Container.Entity.Render.FastCastShadowResolve = true;
            }
        }
    }
}
