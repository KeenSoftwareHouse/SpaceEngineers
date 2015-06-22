using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Common;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.Multiplayer;
using Sandbox;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using VRage.Library.Utils;
using VRageMath;
using VRage.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities.EnvironmentItems
{
    /// <summary>
    /// Class for managing all static trees as one entity.
    /// </summary>
    [MyEntityType(typeof(MyObjectBuilder_TreesMedium), mainBuilder: false)]
    [MyEntityType(typeof(MyObjectBuilder_Trees), mainBuilder: true)]
    public class MyTrees : MyEnvironmentItems
    {
        private struct MyCutTreeInfo
        {
            public int ItemInstanceId;
            public float Progress;
            public int LastHit;
        }
        private List<MyCutTreeInfo> m_cutTreeInfos = new List<MyCutTreeInfo>();

        private const float MAX_TREE_CUT_DURATION = 60.0f;

        private static MySoundPair m_soundTreeBreak;

        static MyTrees()
        {
            m_soundTreeBreak = new MySoundPair("ImpTreeBreak");
        }

        public MyTrees() { }

        public override void DoDamage(float damage, int itemInstanceId, Vector3D position, Vector3 normal, MyDamageType type)
        {
            // CH: TODO: Move the particle effect to definitions
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.ChipOff_Wood, out effect))
            {
                effect.WorldMatrix = MatrixD.CreateWorld(position, Vector3.CalculatePerpendicularVector(normal), normal);
                effect.AutoDelete = true;
            }

            if (!Sync.IsServer)
            {
                return;
            }

            MyCutTreeInfo cutTreeInfo = default(MyCutTreeInfo);
            int index = -1;
            for (int i = 0; i < m_cutTreeInfos.Count; ++i)
            {
                cutTreeInfo = m_cutTreeInfos[i];
                if (itemInstanceId == cutTreeInfo.ItemInstanceId)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                cutTreeInfo = new MyCutTreeInfo();
                cutTreeInfo.ItemInstanceId = itemInstanceId;

                index = m_cutTreeInfos.Count;
                m_cutTreeInfos.Add(cutTreeInfo);
            }

            cutTreeInfo.LastHit = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            cutTreeInfo.Progress += damage;

            if (cutTreeInfo.Progress >= 1)
            {
                CutTree(itemInstanceId, position, normal, type == MyDamageType.Drill ? 1.0f : 4.0f);
                m_cutTreeInfos.RemoveAtFast(index);
            }
            else
            {
                m_cutTreeInfos[index] = cutTreeInfo;
            }

            return;
        }

		public static bool IsEntityFracturedTree(VRage.ModAPI.IMyEntity entity)
		{
			return (entity is MyFracturedPiece) && ((MyFracturedPiece)entity).OriginalBlocks != null && ((MyFracturedPiece)entity).OriginalBlocks.Count > 0
				&& (((MyFracturedPiece)entity).OriginalBlocks[0].TypeId == typeof(MyObjectBuilder_Tree)
				|| ((MyFracturedPiece)entity).OriginalBlocks[0].TypeId == typeof(MyObjectBuilder_DestroyableItem)) && ((MyFracturedPiece)entity).Physics != null;
		}

        protected override void OnRemoveItem(int instanceId, ref Matrix matrix, MyStringHash myStringId)
        {
            base.OnRemoveItem(instanceId, ref matrix, myStringId);

            var emitter = MyAudioComponent.TryGetSoundEmitter();
            if (emitter == null)
                return;

            emitter.SetPosition(matrix.Translation);
            emitter.PlaySound(m_soundTreeBreak);
        }

        private void CutTree(int itemInstanceId, Vector3D hitWorldPosition, Vector3 hitNormal, float forceMultiplier = 1.0f)
        {
            HkStaticCompoundShape shape = (HkStaticCompoundShape)Physics.RigidBody.GetShape();
            int physicsInstanceId;

            if (m_localIdToPhysicsShapeInstanceId.TryGetValue(itemInstanceId, out physicsInstanceId))
            {
                //Remove static tree
                MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];

                RemoveItem(itemInstanceId, physicsInstanceId, sync: true);

                //Create fractured tree
                MyDefinitionId id = new MyDefinitionId(Definition.ItemDefinitionType, itemData.SubtypeId);
                var itemDefinition = MyDefinitionManager.Static.GetEnvironmentItemDefinition(id);
                if (MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes != null)
                {
                    var breakableShape = MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes[0].Clone();
                    MatrixD world = itemData.Transform.TransformMatrix;
                    breakableShape.SetMassRecursively(500);
                    breakableShape.SetStrenghtRecursively(5000, 0.7f);

                    breakableShape.GetChildren(m_childrenTmp);

                    var test = MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes;

                    Vector3 hitLocalPosition = Vector3D.Transform(hitWorldPosition, MatrixD.Normalize(MatrixD.Invert(world)));
                    float cutLocalYPosition = (float)(hitWorldPosition.Y - (double)itemData.Transform.Position.Y);
                    List<HkdShapeInstanceInfo> childrenBelow = new List<HkdShapeInstanceInfo>();
                    List<HkdShapeInstanceInfo> childrenAbove = new List<HkdShapeInstanceInfo>();
                    HkdShapeInstanceInfo? stumpInstanceInfo = null;

                    foreach (var shapeInst in m_childrenTmp)
                    {
                        // The first child shape in the breakable shape should be the stump!
                        if (stumpInstanceInfo == null || shapeInst.CoM.Y < stumpInstanceInfo.Value.CoM.Y)
                            stumpInstanceInfo = shapeInst;

                        if (shapeInst.CoM.Y > cutLocalYPosition)
                            childrenAbove.Add(shapeInst);
                        else
                            childrenBelow.Add(shapeInst);
                    }

                    // Resolve stump - if we have 2 children bellow then move one to above list
                    if (childrenBelow.Count == 2)
                    {
                        if (childrenBelow[0].CoM.Y < childrenBelow[1].CoM.Y && cutLocalYPosition < childrenBelow[1].CoM.Y + 1.25f)
                        {
                            childrenAbove.Insert(0, childrenBelow[1]);
                            childrenBelow.RemoveAt(1);
                        }
                        else if (childrenBelow[0].CoM.Y > childrenBelow[1].CoM.Y && cutLocalYPosition < childrenBelow[0].CoM.Y + 1.25f)
                        {
                            childrenAbove.Insert(0, childrenBelow[0]);
                            childrenBelow.RemoveAt(0);
                        }
                    }
                    else if (childrenBelow.Count == 0)
                    {
                        if (childrenAbove.Remove(stumpInstanceInfo.Value))
                            childrenBelow.Add(stumpInstanceInfo.Value);
                        else
                            Debug.Fail("Cannot remove shape instance from collection");
                    }

                    if (childrenBelow.Count > 0)
                        CreateFracturePiece(itemDefinition, breakableShape, world, hitNormal, childrenBelow, forceMultiplier, true);

                    if (childrenAbove.Count > 0)
                        CreateFracturePiece(itemDefinition, breakableShape, world, hitNormal, childrenAbove, forceMultiplier, false);

                    m_childrenTmp.Clear();
                }               
            }
        }

        public static void CreateFracturePiece(MyEnvironmentItemDefinition itemDefinition, HkdBreakableShape oldBreakableShape, MatrixD worldMatrix, Vector3 hitNormal, List<HkdShapeInstanceInfo> shapeList,
            float forceMultiplier, bool canContainFixedChildren)
        {
            bool containsFixedChildren = false;
            if (canContainFixedChildren)
            {
                foreach (var shapeInst in shapeList)
                {
                    shapeInst.Shape.SetMotionQualityRecursively(HkdBreakableShape.BodyQualityType.QUALITY_DEBRIS);

                    var t = worldMatrix.Translation + worldMatrix.Up * 1.5f;
                    var o = Quaternion.CreateFromRotationMatrix(worldMatrix.GetOrientation());
                    MyPhysics.GetPenetrationsShape(shapeInst.Shape.GetShape(), ref t, ref o, m_tmpResults, MyPhysics.DefaultCollisionLayer);
                    foreach (var res in m_tmpResults)
                    {
                        if (res.GetEntity() is MyVoxelMap)
                        {
                            shapeInst.Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                            containsFixedChildren = true;
                            break;
                        }
                    }
                    m_tmpResults.Clear();
                }
            }

            HkdBreakableShape compound = new HkdCompoundBreakableShape(oldBreakableShape, shapeList);
            ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
            //compound.SetMassRecursively(500);
            //compound.SetStrenghtRecursively(5000, 0.7f);

            var fp = MyDestructionHelper.CreateFracturePiece(compound, MyPhysics.SingleWorld.DestructionWorld, ref worldMatrix, containsFixedChildren, itemDefinition.Id, true);
            if (fp != null && !canContainFixedChildren)
            {
                ApplyImpulseToTreeFracture(ref worldMatrix, ref hitNormal, shapeList, ref compound, fp, forceMultiplier);
                fp.Physics.ForceActivate();
            }
        }

        public static void ApplyImpulseToTreeFracture(ref MatrixD worldMatrix, ref Vector3 hitNormal, List<HkdShapeInstanceInfo> shapeList, ref HkdBreakableShape compound, MyFracturedPiece fp, float forceMultiplier = 1.0f)
        {
            float mass = compound.GetMass();
            Vector3 coMMaxY = Vector3.MinValue;
            shapeList.ForEach(s => coMMaxY = (s.CoM.Y > coMMaxY.Y) ? s.CoM : coMMaxY);

            Vector3 forceVector = hitNormal;
            forceVector.Y = 0;
            forceVector.Normalize();

            Vector3 force = 0.3f * forceMultiplier * mass * forceVector;
            fp.Physics.Enabled = true;//so we get the body in world
            Vector3 worldForcePoint = fp.Physics.WorldToCluster(Vector3D.Transform(coMMaxY, worldMatrix));

            fp.Physics.RigidBody.AngularDamping = MyPerGameSettings.DefaultAngularDamping;
            fp.Physics.RigidBody.LinearDamping = MyPerGameSettings.DefaultLinearDamping;

            fp.Physics.RigidBody.ApplyPointImpulse(force, worldForcePoint);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            UpdateTreeInfos();
        }

        private void UpdateTreeInfos()
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            int maxDuration = (int)(MAX_TREE_CUT_DURATION * 1000);

            for (int i = m_cutTreeInfos.Count - 1; i >= 0; i--)
            {
                var info = m_cutTreeInfos[i];
                if (currentTime - info.LastHit > maxDuration)
                {
                    m_cutTreeInfos.RemoveAtFast(i);
                }
            }
        }
    }
}
