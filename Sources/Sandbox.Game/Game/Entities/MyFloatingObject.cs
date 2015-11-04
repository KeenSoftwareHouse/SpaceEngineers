#region Using
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GameSystems;
using Sandbox.Common.ModAPI;
using Sandbox.Game.Entities.UseObject;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Components;
using VRage.Game.Entity.UseObject;

#endregion

namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_FloatingObject))]
    public class MyFloatingObject : MyEntity, IMyUseObject, IMyUsableEntity, IMyDestroyableObject, IMyFloatingObject
    {
        static MySoundPair TAKE_ITEM_SOUND = new MySoundPair("PlayTakeItem");
        static MyStringHash m_explosives = MyStringHash.GetOrCompute("Explosives");
		static public MyObjectBuilder_Ore ScrapBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Scrap");

        private StringBuilder m_displayedText = new StringBuilder();

        public MyPhysicalInventoryItem Item;

        public MyVoxelMaterialDefinition VoxelMaterial;
        public long CreationTime;
        private float m_health = 100.0f;

        private MyEntity3DSoundEmitter m_soundEmitter;
        public int m_lastTimePlayedSound;

        //-1 -- not synced at all (top priority)
        public float ClosestDistanceToAnyPlayerSquared = -1;

        public bool WasRemovedFromWorld { get; set; }

        public int NumberOfFramesInsideVoxel = 0;
        public const int NUMBER_OF_FRAMES_INSIDE_VOXEL_TO_REMOVE = 5;
        
        public long SyncWaitCounter; // counting how many times this object was skipped on sync;

        public MyFloatingObject()
        {
            WasRemovedFromWorld = false;
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            m_lastTimePlayedSound = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            Render = new Components.MyRenderComponentFloatingObject();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var builder = objectBuilder as MyObjectBuilder_FloatingObject;
            if (builder.Item.Amount <= 0)
            {
                // I can only prevent creation of entity by throwing exception. This might cause crashes when thrown outside of MyEntities.CreateFromObjectBuilder().
                throw new ArgumentOutOfRangeException("MyPhysicalInventoryItem.Amount", string.Format("Creating floating object with invalid amount: {0}x '{1}'", builder.Item.Amount, builder.Item.Content.GetId()));
            }
            base.Init(objectBuilder);

            this.Item = new MyPhysicalInventoryItem(builder.Item);

            InitInternal();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            UseDamageSystem = true;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            //onAddedToScene can remove floating object !!
            MyFloatingObjects.RegisterFloatingObject(this);
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var builder = (MyObjectBuilder_FloatingObject)base.GetObjectBuilder(copy);
            builder.Item = Item.GetObjectBuilder();
            return builder;
        }

        private void InitInternal()
        {
            // TODO: This will be fixed and made much more simple once ore models are done
            // https://app.asana.com/0/6594565324126/10473934569658

            var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(Item.Content);

            m_health = physicalItem.Health;

            string model = physicalItem.Model;

            VoxelMaterial = null;
            float scale = 1.0f;

            if (Item.Content is MyObjectBuilder_Ore)
            {
                string oreSubTypeId = physicalItem.Id.SubtypeId.ToString();
                foreach (var mat in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
                {
                    if (oreSubTypeId == mat.MinedOre)
                    {
                        VoxelMaterial = mat;
                        model = MyDebris.GetRandomDebrisVoxel();
                        scale = (float)Math.Pow((float)Item.Amount * physicalItem.Volume / MyDebris.VoxelDebrisModelVolume, 0.333f);
                        break;
                    }
                }

                scale = (float)Math.Pow((float)Item.Amount * physicalItem.Volume / MyDebris.VoxelDebrisModelVolume, 0.333f);
            }

            if (scale < 0.05f)
                Close();
            else if (scale < 0.15f)
                scale = 0.15f;

            FormatDisplayName(m_displayedText, Item);
            Init(m_displayedText, model, null, null, null);

            PositionComp.Scale = scale; // Must be set after init


            var massProperties = new HkMassProperties();
            var mass = MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(physicalItem.Mass) : physicalItem.Mass;

            HkShape shape = GetPhysicsShape(mass * (float)Item.Amount, scale, out massProperties);
            var scaleMatrix = Matrix.CreateScale(scale);

            if (Physics != null)
                Physics.Close();
            Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEBRIS);

            if (VoxelMaterial != null)
            {
                HkConvexTransformShape transform = new HkConvexTransformShape((HkConvexShape)shape, ref scaleMatrix, HkReferencePolicy.None);
        
                Physics.CreateFromCollisionObject(transform, Vector3.Zero, MatrixD.Identity, massProperties, MyPhysics.FloatingObjectCollisionLayer);
               
                Physics.Enabled = true;
                transform.Base.RemoveReference();
            }
            else
            {
                Physics.CreateFromCollisionObject(shape, Vector3.Zero, MatrixD.Identity, massProperties, MyPhysics.FloatingObjectCollisionLayer);
                Physics.Enabled = true;
            }

            Physics.MaterialType = VoxelMaterial != null ? MyMaterialType.ROCK : MyMaterialType.METAL;
            Physics.PlayCollisionCueEnabled = true;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public void RefreshDisplayName()
        {
            FormatDisplayName(m_displayedText, Item);
        }

        private void FormatDisplayName(StringBuilder outputBuffer, MyPhysicalInventoryItem item)
        {
            var definition = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Content);
            outputBuffer.Clear().Append(definition.DisplayNameText);
            if (Item.Amount != 1)
            {
                outputBuffer.Append(" (");
                MyGuiControlInventoryOwner.FormatItemAmount(item, outputBuffer);
                outputBuffer.Append(")");
            }
        }

        protected override void Closing()
        {
            MyFloatingObjects.UnregisterFloatingObject(this);
            base.Closing();
        }

        // Don't call remove reference on this, this shape is pooled
        protected virtual HkShape GetPhysicsShape(float mass, float scale, out HkMassProperties massProperties)
        {
            const bool SimpleShape = false;

            Vector3 halfExtents = (Model.BoundingBox.Max - Model.BoundingBox.Min) / 2;
            HkShapeType shapeType;

            if (VoxelMaterial != null)
            {
                shapeType = HkShapeType.Sphere;
                massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(Model.BoundingSphere.Radius * scale, mass);
            }
            else
            {
                shapeType = HkShapeType.Box;
                massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(halfExtents, mass);
                massProperties.CenterOfMass = Model.BoundingBox.Center;
            }

            return MyDebris.Static.GetDebrisShape(Model, SimpleShape ? shapeType : HkShapeType.ConvexVertices);
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return 2.0f; }
        }

        MatrixD IMyUseObject.ActivationMatrix
        {
            get { return PositionComp != null ? Matrix.CreateScale(this.PositionComp.LocalAABB.Size) * WorldMatrix : MatrixD.Zero; }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return WorldMatrix; }
        }

        int IMyUseObject.RenderObjectID
        {
            get
            {
                if (Render.RenderObjectIDs.Length > 0)
                    return (int)Render.RenderObjectIDs[0];
                return -1;
            }
        }

        bool IMyUseObject.ShowOverlay
        {
            get { return false; }
        }

        UseActionEnum IMyUseObject.SupportedActions
        {
            get { return UseActionEnum.Manipulate; }
        }

        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            if (!MarkedForClose)
            {
                MyFixedPoint amount = MyFixedPoint.Min(Item.Amount, user.GetInventory().ComputeAmountThatFits(Item.Content.GetId()));
                if (amount == 0)
                {
                    if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimePlayedSound > 2500)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudVocInventoryFull);
                        m_lastTimePlayedSound = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    }

                    MyHud.Notifications.Add(MyNotificationSingletons.InventoryFull);
                    return;
                }
              
                if (amount > 0)
                {
                    if (MySession.ControlledEntity == user)
                        MyAudio.Static.PlaySound(TAKE_ITEM_SOUND.SoundId);
                    user.GetInventory().PickupItem(this, amount);
                }

                MyHud.Notifications.ReloadTexts();
            }
        }

        public void UpdateInternalState()
        {
            if (Item.Amount <= 0)
                Close();
            else
            {
                Render.UpdateRenderObject(false);
                InitInternal();
                Physics.Activate();
                InScene = true;
                Render.UpdateRenderObject(true);
                MyHud.Notifications.ReloadTexts();
            }
        }

        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        {
            var key = MyInput.Static.GetGameControl(MyControlsSpace.USE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationPickupObject,
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE), m_displayedText },
                IsTextControlHint = false,
                JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.USE), m_displayedText },
            };
        }

        bool IMyUseObject.ContinuousUsage
        {
            get { return true; }
        }

        UseActionResult IMyUsableEntity.CanUse(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            return MarkedForClose ? UseActionResult.Closed : UseActionResult.OK; // When object is not collected, it's usable
        }

        bool IMyUseObject.PlayIndicatorSound
        {
            get { return false; }
        }

        public void DoDamage(float damage, MyStringHash damageType, bool sync, long attackerId)
        {
            if (MarkedForClose)
                return;

            if (sync)
            {
                if (!Sync.IsServer)
                    return;
                else
                {
                    MySyncHelper.DoDamageSynced(this, damage, damageType, attackerId);
                    return;
                }
            }

            MyDamageInformation damageinfo = new MyDamageInformation(false, damage, damageType, attackerId);
            if(UseDamageSystem)
                MyDamageSystem.Static.RaiseBeforeDamageApplied(this, ref damageinfo);

            var typeId = Item.Content.TypeId;
            if (typeId == typeof(MyObjectBuilder_Ore) ||
                typeId == typeof(MyObjectBuilder_Ingot))
            {
                if (Item.Amount < 1)
                {
                    //TODO: SYNC particle
                    MyParticleEffect effect;
                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Construction, out effect))
                    {
                        effect.WorldMatrix = WorldMatrix;
                        effect.UserScale = 0.4f;
                    }
                    MyFloatingObjects.RemoveFloatingObject(this);
                }
                else
                {
                    if (Sync.IsServer)
                        MyFloatingObjects.RemoveFloatingObject(this, (MyFixedPoint)damageinfo.Amount);
                }
            }
            else
            {
                m_health -= (10 + 90 * DamageMultiplier) * damageinfo.Amount;

                if(UseDamageSystem)
                    MyDamageSystem.Static.RaiseAfterDamageApplied(this, damageinfo);

                if (m_health < 0)
                {
                    //TODO: SYNC particle
                    MyParticleEffect effect;
                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Construction, out effect))
                    {
                        effect.WorldMatrix = WorldMatrix;
                        effect.UserScale = 0.4f;
                    }
                    if (Sync.IsServer)
                        MyFloatingObjects.RemoveFloatingObject(this);
                    //TODO: dont compare to string?
                    if (Item.Content.SubtypeId == m_explosives && Sync.IsServer)
                    {
                        var expSphere = new BoundingSphere(WorldMatrix.Translation, (float)Item.Amount * 0.01f + 0.5f);// MathHelper.Clamp((float)Item.Amount, 0, 300) * 0.5f);
                        MyExplosionInfo info = new MyExplosionInfo()
                        {
                            PlayerDamage = 0,
                            Damage = 800,
                            ExplosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_15,
                            ExplosionSphere = expSphere,
                            LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                            CascadeLevel = 0,
                            HitEntity = this,
                            ParticleScale = 1,
                            OwnerEntity = this,
                            Direction = WorldMatrix.Forward,
                            VoxelExplosionCenter = expSphere.Center,
                            ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION,
                            VoxelCutoutScale = 0.5f,
                            PlaySound = true,
                            ApplyForceAndDamage = true,
                            ObjectsRemoveDelayInMiliseconds = 40
                        };
                        MyExplosions.AddExplosion(ref info);
                    }

                    if (MyFakes.ENABLE_SCRAP && Sync.IsServer)
                    {
                        if (Item.Content.SubtypeId == ScrapBuilder.SubtypeId)
                            return;

                        var contentDefinitionId = Item.Content.GetId();
                        if (contentDefinitionId.TypeId == typeof(MyObjectBuilder_Component))
                        {
                            var definition = MyDefinitionManager.Static.GetComponentDefinition((Item.Content as MyObjectBuilder_Component).GetId());
                            if (MyRandom.Instance.NextFloat() < definition.DropProbability)
                                MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(Item.Amount * 0.8f, ScrapBuilder), PositionComp.GetPosition(), WorldMatrix.Forward, WorldMatrix.Up);
                        }
                    }

                    if(UseDamageSystem)
                        MyDamageSystem.Static.RaiseDestroyed(this, damageinfo);
                }
            }

            return;
        }

        private float DamageMultiplier
        {
            get
            {
                var contentDefinitionId = Item.Content.GetId();
                if (contentDefinitionId.TypeId == typeof(MyObjectBuilder_Component))
                {
                    var definition = MyDefinitionManager.Static.GetComponentDefinition(contentDefinitionId);
                    return 1 - definition.DropProbability;
                }
                return 0.0f;
            }
        }

        public void RemoveUsers(bool local)
        {
        }

        public void OnDestroy()
        {
        }

        public float Integrity
        {
            get { return m_health; }
        }

        public bool UseDamageSystem { get; private set; }

        void IMyDestroyableObject.OnDestroy()
        {
            OnDestroy();
        }

        void IMyDestroyableObject.DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            DoDamage(damage, damageType, sync, attackerId);
        }

        float IMyDestroyableObject.Integrity
        {
            get { return Integrity; }
        }

        bool IMyDestroyableObject.UseDamageSystem
        {
            get { return UseDamageSystem; }
        }

        bool IMyUseObject.HandleInput() { return false; }

        void IMyUseObject.OnSelectionLost() { }
        
    }
}