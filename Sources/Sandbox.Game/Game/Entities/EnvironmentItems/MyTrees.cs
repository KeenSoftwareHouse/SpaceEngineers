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
using VRage.Library.Utils;
using VRageMath;
using VRage.Utils;
using VRage.ObjectBuilders;
using Sandbox.Game.GameSystems;
using VRage;
using Sandbox.Game.Entities.Debris;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRage.Profiler;
using VRageRender;

namespace Sandbox.Game.Entities.EnvironmentItems
{
    /// <summary>
    /// Class for managing all static trees as one entity.
    /// </summary>
    [MyEntityType(typeof(MyObjectBuilder_TreesMedium), mainBuilder: false)]
    [MyEntityType(typeof(MyObjectBuilder_Trees), mainBuilder: true)]
    [StaticEventOwner]
    public class MyTrees : MyEnvironmentItems, IMyDecalProxy
    {
        private struct MyCutTreeInfo
        {
            public int ItemInstanceId;
            public float Progress { get { return MathHelper.Clamp((MaxPoints - HitPoints) / MaxPoints, 0, 1); } }
            public int LastHit;
            public float HitPoints;
            public float MaxPoints;
        }
        private List<MyCutTreeInfo> m_cutTreeInfos = new List<MyCutTreeInfo>();

        private const float MAX_TREE_CUT_DURATION = 60.0f;

        private const int BrokenTreeLifeSpan = 20 * 1000;
        
        public MyTrees() { }

        public override void DoDamage(float damage, int itemInstanceId, Vector3D position, Vector3 normal, MyStringHash type)
        {
            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];
            MyDefinitionId id = new MyDefinitionId(Definition.ItemDefinitionType, itemData.SubtypeId);
            var itemDefinition = (MyTreeDefinition)MyDefinitionManager.Static.GetEnvironmentItemDefinition(id);


            int effectId;
            if (itemDefinition.CutEffect != null && MyParticlesLibrary.GetParticleEffectsID(itemDefinition.CutEffect, out effectId))
            {
                MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect(effectId, out effect))
                {
                    effect.WorldMatrix = MatrixD.CreateWorld(position, Vector3.CalculatePerpendicularVector(normal), normal);
                }
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

                cutTreeInfo.MaxPoints = cutTreeInfo.HitPoints = itemDefinition.HitPoints;

                index = m_cutTreeInfos.Count;
                m_cutTreeInfos.Add(cutTreeInfo);
            }

            cutTreeInfo.LastHit = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            cutTreeInfo.HitPoints -= damage;

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
                || ((MyFracturedPiece)entity).OriginalBlocks[0].TypeId == typeof(MyObjectBuilder_DestroyableItem)
                || ((MyFracturedPiece)entity).OriginalBlocks[0].TypeId == typeof(MyObjectBuilder_TreeDefinition)) && ((MyFracturedPiece)entity).Physics != null;
        }

        protected override void OnRemoveItem(int instanceId, ref Matrix matrix, MyStringHash myStringId, int userData)
        {
            base.OnRemoveItem(instanceId, ref matrix, myStringId, userData);
        }

        private void CutTree(int itemInstanceId, Vector3D hitWorldPosition, Vector3 hitNormal, float forceMultiplier = 1.0f)
        {
            HkStaticCompoundShape shape = (HkStaticCompoundShape)Physics.RigidBody.GetShape();
            int physicsInstanceId;

            if (m_localIdToPhysicsShapeInstanceId.TryGetValue(itemInstanceId, out physicsInstanceId))
            {
                MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];
                MyDefinitionId id = new MyDefinitionId(Definition.ItemDefinitionType, itemData.SubtypeId);
                var itemDefinition = (MyTreeDefinition)MyDefinitionManager.Static.GetEnvironmentItemDefinition(id);

                //Remove static tree
                if (RemoveItem(itemInstanceId, physicsInstanceId, sync: true, immediateUpdate: true) && itemDefinition != null && itemDefinition.BreakSound != null && itemDefinition.BreakSound.Length > 0)
                {
                    MyMultiplayer.RaiseStaticEvent(s => PlaySound,hitWorldPosition,itemDefinition.BreakSound);
                }
                
                //Create fractured tree
                if (MyPerGameSettings.Destruction && VRage.Game.Models.MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes != null)
                {
                    if (itemDefinition.FallSound != null && itemDefinition.FallSound.Length > 0)
                        CreateBreakableShape(itemDefinition, ref itemData, ref hitWorldPosition, hitNormal, forceMultiplier, itemDefinition.FallSound);
                    else
                        CreateBreakableShape(itemDefinition, ref itemData, ref hitWorldPosition, hitNormal, forceMultiplier);
                }
            }
        }

        [Event, Reliable, Server, Broadcast]
        private static void PlaySound(Vector3D position, string cueName)
        {
            MySoundPair sound = new MySoundPair(cueName);
            if (sound == MySoundPair.Empty)
                return;
            var emitter = MyAudioComponent.TryGetSoundEmitter();
            if (emitter == null)
                return;

            emitter.SetPosition(position);
            emitter.PlaySound(sound);            
        }

        protected override MyEntity DestroyItem(int itemInstanceId)
        {
            int physicsInstanceId;
            if (!m_localIdToPhysicsShapeInstanceId.TryGetValue(itemInstanceId, out physicsInstanceId))
            {
                physicsInstanceId = -1;
            }
            //Remove static tree
            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];

            RemoveItem(itemInstanceId, physicsInstanceId, sync: false, immediateUpdate: true);

            ProfilerShort.Begin("Spawning tree");
            // This is for SE when you hit a tree, it will create a floating object with the same model. In case it affects ME, it may be changed. Contact DusanA for it.
            Debug.Assert(MyPerGameSettings.Game == GameEnum.SE_GAME);
            //MyPhysicalInventoryItem Item = new MyPhysicalInventoryItem() { Amount = 1, Scale = 1f, Content = new MyObjectBuilder_TreeObject() { SubtypeName = itemData.SubtypeId.ToString() } };
            Vector3D pos = itemData.Transform.Position;
            var s = itemData.Model.AssetName.Insert(itemData.Model.AssetName.Length - 4, "_broken");
            MyEntity debris;
            bool hasBrokenModel = false;

            if (VRage.Game.Models.MyModels.GetModelOnlyData(s) != null)
            {
                hasBrokenModel = true;
                debris = MyDebris.Static.CreateDebris(s);
            }
            else
                debris = MyDebris.Static.CreateDebris(itemData.Model.AssetName);
            var debrisLogic = (debris.GameLogic as Sandbox.Game.Entities.Debris.MyDebrisBase.MyDebrisBaseLogic);
            debrisLogic.RandomScale = 1;
            debrisLogic.LifespanInMiliseconds = BrokenTreeLifeSpan;
            var m = MatrixD.CreateFromQuaternion(itemData.Transform.Rotation);
            m.Translation = pos + m.Up*(hasBrokenModel ? 0 : 5);
            debrisLogic.Start(m, Vector3.Zero, 1, false);
            //MyFloatingObjects.Spawn(Item, pos + gravity, MyUtils.GetRandomPerpendicularVector(ref gravity), gravity);
            ProfilerShort.End();
            return debris;
        }

        private void CreateBreakableShape(MyEnvironmentItemDefinition itemDefinition, ref MyEnvironmentItemData itemData, ref Vector3D hitWorldPosition, Vector3 hitNormal, float forceMultiplier, string fallSound = "")
        {
            var breakableShape = VRage.Game.Models.MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes[0].Clone();
            MatrixD world = itemData.Transform.TransformMatrix;
            breakableShape.SetMassRecursively(500);
            breakableShape.SetStrenghtRecursively(5000, 0.7f);

            breakableShape.GetChildren(m_childrenTmp);

            var test = VRage.Game.Models.MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes;

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
                CreateFracturePiece(itemDefinition, breakableShape, world, hitNormal, childrenAbove, forceMultiplier, false, fallSound);

            m_childrenTmp.Clear();
        }

        public static void CreateFracturePiece(MyEnvironmentItemDefinition itemDefinition, HkdBreakableShape oldBreakableShape, MatrixD worldMatrix, Vector3 hitNormal, List<HkdShapeInstanceInfo> shapeList,
            float forceMultiplier, bool canContainFixedChildren, string fallSound = "")
        {
            bool containsFixedChildren = false;
            if (canContainFixedChildren)
            {
                foreach (var shapeInst in shapeList)
                {
                    shapeInst.Shape.SetMotionQualityRecursively(HkdBreakableShape.BodyQualityType.QUALITY_DEBRIS);

                    var t = worldMatrix.Translation + worldMatrix.Up * 1.5f;
                    var o = Quaternion.CreateFromRotationMatrix(worldMatrix.GetOrientation());
                    MyPhysics.GetPenetrationsShape(shapeInst.Shape.GetShape(), ref t, ref o, m_tmpResults, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                    bool flagSet = false;
                    foreach (var res in m_tmpResults)
                    {
                        var entity = res.GetCollisionEntity();

                        if (entity is MyVoxelMap)
                        {
                            shapeInst.Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                            containsFixedChildren = true;
                            flagSet = true;
                            break;
                        }

                        if (flagSet)
                            break;
                    }
                    m_tmpResults.Clear();
                }
            }

            HkdBreakableShape compound = new HkdCompoundBreakableShape(oldBreakableShape, shapeList);
            ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
            //compound.SetMassRecursively(500);
            //compound.SetStrenghtRecursively(5000, 0.7f);

            var fp = MyDestructionHelper.CreateFracturePiece(compound, ref worldMatrix, containsFixedChildren, itemDefinition.Id, true);
            if (fp != null && !canContainFixedChildren)
            {
                ApplyImpulseToTreeFracture(ref worldMatrix, ref hitNormal, shapeList, ref compound, fp, forceMultiplier);
                fp.Physics.ForceActivate();
                if(fallSound.Length > 0)
                    fp.StartFallSound(fallSound);
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

        void IMyDecalProxy.AddDecals(MyHitInfo hitInfo, MyStringHash source, object customdata, IMyDecalHandler decalHandler, MyStringHash material)
        {
            MyDecalRenderInfo info = new MyDecalRenderInfo();
            info.Position = hitInfo.Position;
            info.Normal = hitInfo.Normal;
            info.RenderObjectId = -1;
            info.Flags = MyDecalFlags.World;

            if (material.GetHashCode() == 0)            
                info.Material = Physics.MaterialType;
            else
                info.Material = material;

            decalHandler.AddDecal(ref info);
        }
    }
}
