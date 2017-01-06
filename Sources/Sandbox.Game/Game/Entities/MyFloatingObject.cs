#region Using
#if XB1
using System.Collections.Generic;
#endif // XB1
using Havok;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities.UseObject;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using System.Diagnostics;
using VRage.Network;
using VRage.Game.Entity;
using VRage.Import;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Engine.Multiplayer;
using VRage.Audio;
using VRage.Sync;
using VRageRender.Import;
using System.Collections.Generic;
using IMyEntity = VRage.ModAPI.IMyEntity;

#endregion

namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_FloatingObject))]
    public class MyFloatingObject : MyEntity, IMyUseObject, IMyUsableEntity, IMyDestroyableObject, IMyFloatingObject, IMyEventProxy
    {
        static MyStringHash m_explosives = MyStringHash.GetOrCompute("Explosives");
        static public MyObjectBuilder_Ore ScrapBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Scrap");

        private StringBuilder m_displayedText = new StringBuilder();

        public MyPhysicalInventoryItem Item;
        private int m_modelVariant;

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

        public MyPhysicalItemDefinition ItemDefinition { get; private set; }

        private DateTime lastTimeSound = DateTime.MinValue;

        private Vector3 m_smoothGravity;
        private Vector3 m_smoothGravityDir;
        private List<Vector3> m_supportNormals;
        
        public new MyPhysicsBody Physics
        {
            get { return base.Physics as MyPhysicsBody; }
            set { base.Physics = value; }
        }

        public Sync<MyFixedPoint> Amount;

        public MyFloatingObject()
        {
            WasRemovedFromWorld = false;
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            m_lastTimePlayedSound = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            Render = new Components.MyRenderComponentFloatingObject();

#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType = SyncHelpers.Compose(this);
#else // XB1
            SyncType = new SyncType(new List<SyncBase>());
            Amount = SyncType.CreateAndAddProp<MyFixedPoint>();
#endif // XB1

            Amount.ValueChanged += (x) => { Item.Amount = Amount.Value; UpdateInternalState(); };
        }

        private HkEasePenetrationAction m_easeCollisionForce;
        private TimeSpan m_timeFromSpawn = new TimeSpan();

        public readonly SyncType SyncType;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var builder = objectBuilder as MyObjectBuilder_FloatingObject;
            if (builder.Item.Amount <= 0)
            {
                // I can only prevent creation of entity by throwing exception. This might cause crashes when thrown outside of MyEntities.CreateFromObjectBuilder().
                throw new ArgumentOutOfRangeException("MyPhysicalInventoryItem.Amount", string.Format("Creating floating object with invalid amount: {0}x '{1}'", builder.Item.Amount, builder.Item.PhysicalContent.GetId()));
            }
            base.Init(objectBuilder);

            this.Item = new MyPhysicalInventoryItem(builder.Item);
            this.m_modelVariant = builder.ModelVariant;

            InitInternal();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            UseDamageSystem = true;

            MyPhysicalItemDefinition itemDefinition = null;
            if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(Item.GetDefinitionId(), out itemDefinition))
            {
                System.Diagnostics.Debug.Fail("Creating floating object, but it's physical item definition wasn't found! - " + Item.ItemId);
                ItemDefinition = null;
            }
            else
                ItemDefinition = itemDefinition;
            m_timeFromSpawn = MySession.Static.ElapsedPlayTime;


            m_smoothGravity = Physics.RigidBody.Gravity;
            m_smoothGravityDir = m_smoothGravity;
            m_smoothGravityDir.Normalize();
            m_supportNormals = new List<Vector3>();
            m_supportNormals.Capacity = 3;

            Physics.RigidBody.ContactPointCallbackEnabled = true;
            Physics.RigidBody.ContactPointCallbackDelay = 0;
            Physics.RigidBody.ContactPointCallback += RigidBody_ContactPointCallback;
          
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            // DA: Consider using havok fields (buoyancy demo) for gravity of planets.
            //<ib.floating>
            //Physics.RigidBody.Gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(PositionComp.GetPosition());
            Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(PositionComp.GetPosition());
                        

            if (Physics.RigidBody.GetShape().ShapeType == HkShapeType.Sphere)
            {
                const float w = 0.5f;
                m_smoothGravity = m_smoothGravity * w + gravity * (1 - w);
                m_smoothGravityDir = m_smoothGravity;
                m_smoothGravityDir.Normalize();
                
                bool anySupport = false;
                foreach (var supportNormal in m_supportNormals)
                {
                    float cosalpha = supportNormal.Dot(m_smoothGravityDir);
                    const float cosalphaLimit = 0.8f; // +/- 12 Degrees
                    if (cosalpha > cosalphaLimit)
                    {
                        anySupport = true;
                        break;
                    }
                }
                m_supportNormals.Clear();
               
                if (anySupport)
                {
                    //Physics.RigidBody.Gravity = Vector3.Zero;
                    const float limitGravity = 0.01f;
                    if ((Physics.RigidBody.Gravity.Length() > limitGravity))
                    {
                        Physics.RigidBody.Gravity *= 0.99f; // Decay gravity
                    }
                }
                else
                {
                    Physics.RigidBody.Gravity = m_smoothGravity;
                }
            }
            else 
            {
                Physics.RigidBody.Gravity = gravity;
            }
            

            if (m_massChangeForCollisions < 1f)
            {
                if ((MySession.Static.ElapsedPlayTime.TotalMilliseconds - m_timeFromSpawn.TotalMilliseconds) >= 2000)
                {
                    m_massChangeForCollisions = 1f;
                }
            }

       }

        void RigidBody_ContactPointCallback(ref HkContactPointEvent e)
        {
            //if (e.Base.BodyA.IsFixed || e.Base.BodyB.IsFixed)
            {
                Vector3 supportNormal = e.ContactPoint.Position - Physics.RigidBody.Position;
                float normalLen = supportNormal.Normalize();

                if (normalLen > 0.001f)
                {
                    m_supportNormals.Add(supportNormal);
                }
            }            
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
            builder.ModelVariant = m_modelVariant;
            return builder;
        }

        public bool HasConstraints()
        {
            return Physics.RigidBody.HasConstraints();
        }

        private void InitInternal()
        {
            // TODO: This will be fixed and made much more simple once ore models are done
            // https://app.asana.com/0/6594565324126/10473934569658

            var itemDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition(Item.Content);

            m_health = itemDefinition.Health;

            // Setting voxel material (if applicable)
            VoxelMaterial = null;
            if (itemDefinition.VoxelMaterial != MyStringHash.NullOrEmpty)
            {
                VoxelMaterial = MyDefinitionManager.Static.GetVoxelMaterialDefinition(itemDefinition.VoxelMaterial.String);
            }
            else if (Item.Content is MyObjectBuilder_Ore)
            {
                string oreSubTypeId = itemDefinition.Id.SubtypeName;
                string materialName = (Item.Content as MyObjectBuilder_Ore).GetMaterialName();
                bool hasMaterialName = (Item.Content as MyObjectBuilder_Ore).HasMaterialName();

                foreach (var mat in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
                {
                    if ((hasMaterialName && materialName == mat.Id.SubtypeName) || (hasMaterialName == false && oreSubTypeId == mat.MinedOre))
                    {
                        VoxelMaterial = mat;
                        break;
                    }
                }
            }

            // Setting the item's model
            string model = itemDefinition.Model;
            if (itemDefinition.HasModelVariants)
            {
                int modelNum = itemDefinition.Models.Length;
                Debug.Assert(m_modelVariant >= 0 && m_modelVariant < modelNum, "Model variant overflow. This can happen if model variants changed");
                m_modelVariant = m_modelVariant % modelNum;

                model = itemDefinition.Models[m_modelVariant];
            }
            else if (Item.Content is MyObjectBuilder_Ore && VoxelMaterial != null)
            {
                // Only ores without found voxel material use the defined model (otherwise, the scrap metal does not work)
                model = MyDebris.GetRandomDebrisVoxel();
            }

            // Setting the scale
            float scale = this.Item.Scale;
            if (Item.Content is MyObjectBuilder_Ore)
            {
                scale *= (float)Math.Pow((float)Item.Amount * itemDefinition.Volume / MyDebris.VoxelDebrisModelVolume, 0.333f);
            }
            else
            {
                scale *= (float)Math.Pow(itemDefinition.Volume / itemDefinition.ModelVolume, 0.333f);
            }
            if (scale < 0.05f)
                Close();
            else if (scale < 0.15f)
                scale = 0.15f;

            FormatDisplayName(m_displayedText, Item);
            Debug.Assert(model != null, "Floating object model is null");
            Init(m_displayedText, model, null, null, null);

            PositionComp.Scale = scale; // Must be set after init

            var massProperties = new HkMassProperties();
            var mass = MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(itemDefinition.Mass) : itemDefinition.Mass;
            mass = mass * (float)Item.Amount;

            HkShape shape = GetPhysicsShape(mass, scale, out massProperties);
            var scaleMatrix = Matrix.CreateScale(scale);

            if (Physics != null)
                Physics.Close();
           
            Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT); // Default wasRigidBodyFlag.RBF_DEBRIS

            int layer = mass > MyPerGameSettings.MinimumLargeShipCollidableMass ? MyPhysics.CollisionLayers.FloatingObjectCollisionLayer : MyPhysics.CollisionLayers.LightFloatingObjectCollisionLayer;


            if ((VoxelMaterial != null || (shape.IsConvex && scale != 1f)) && (shape.ShapeType != HkShapeType.Sphere))
            {
                HkConvexTransformShape transform = new HkConvexTransformShape((HkConvexShape)shape, ref scaleMatrix, HkReferencePolicy.None);

                Physics.CreateFromCollisionObject(transform, Vector3.Zero, MatrixD.Identity, massProperties, layer);

                Physics.Enabled = true;
                transform.Base.RemoveReference();
            }
            else
            {
                Physics.CreateFromCollisionObject(shape, Vector3.Zero, MatrixD.Identity, massProperties, layer);
                Physics.Enabled = true;
            }

            Physics.MaterialType = this.EvaluatePhysicsMaterial(itemDefinition.PhysicalMaterial);
            Physics.PlayCollisionCueEnabled = true;
            Physics.RigidBody.ContactSoundCallbackEnabled = true;
            
            //m_easeCollisionForce = new HkEasePenetrationAction(Physics.RigidBody, 2f);
            m_massChangeForCollisions = 0.010f;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

            Physics.RigidBody.SetProperty(HkCharacterRigidBody.FLOATING_OBJECT,0);

            //Some physical parameters modifications for stones and ore (sphere objects)
            if (shape.ShapeType == HkShapeType.Sphere)
            {
                //Physics.RigidBody.Friction = 1.0f;
                //Physics.RigidBody.Restitution = 0.05f;
                Physics.RigidBody.LinearDamping = 0.9f;
                Physics.RigidBody.AngularDamping = 2.0f;
                Physics.RigidBody.AllowedPenetrationDepth = 0.05f;
            }
        }

        /// <summary>
        /// Evaluates what kind of material should be used for this floating object. If material is not defined than returns empty one and throws an assert.
        /// </summary>
        /// <param name="originalMaterial">Original material set in this object.</param>
        /// <returns>Final material.</returns>
        private MyStringHash EvaluatePhysicsMaterial(MyStringHash originalMaterial)
        {

            //Debug.Assert(originalMaterial.String != string.Empty, "No physical material set for this object, please define it in coresponding cbs file");

            return VoxelMaterial != null ? MyMaterialType.ROCK : originalMaterial;

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
            const bool SimpleShape = true;
            const float halfExtentsScale = 2.0f;
           
            Debug.Assert(Model != null, "Invalid floating object model: " + Item.GetDefinitionId());
            if (Model == null)
                MyLog.Default.WriteLine("Invalid floating object model: " + Item.GetDefinitionId());

            HkShapeType shapeType;

            if (VoxelMaterial != null)
            {
                shapeType = HkShapeType.Sphere;
                massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(Model.BoundingSphere.Radius * scale, mass);
            }
            else
            {
                shapeType = HkShapeType.Box;
                Vector3 halfExtents = halfExtentsScale * (Model.BoundingBox.Max - Model.BoundingBox.Min) / 2;
                massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(halfExtents, mass);
                massProperties.Mass = mass;
                massProperties.CenterOfMass = Model.BoundingBox.Center;
            }

            return MyDebris.Static.GetDebrisShape(Model, SimpleShape ? shapeType : HkShapeType.ConvexVertices);
        }

        IMyEntity IMyUseObject.Owner
        {
            get { return this; }
        }

        MyModelDummy IMyUseObject.Dummy
        {
            get { return null; }
        }

        float IMyUseObject.InteractiveDistance
        {
            get { return MyConstants.FLOATING_OBJ_INTERACTIVE_DISTANCE; }
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

        void IMyUseObject.SetRenderID(uint id)
        {
        }

        int IMyUseObject.InstanceID
        {
            get
            {
                return -1;
            }
        }

        void IMyUseObject.SetInstanceID(int id)
        {
        }

        bool IMyUseObject.ShowOverlay
        {
            get { return false; }
        }

        UseActionEnum IMyUseObject.SupportedActions
        {
            get { return MyFakes.ENABLE_SEPARATE_USE_AND_PICK_UP_KEY ?  UseActionEnum.PickUp : UseActionEnum.Manipulate; }
        }

        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            if (!MarkedForClose)
            {
                System.Diagnostics.Debug.Assert((user.GetInventory() as MyInventory) != null, "Null or unexpected inventory type returned!");

                MyFixedPoint amount = MyFixedPoint.Min(Item.Amount, (user.GetInventory() as MyInventory).ComputeAmountThatFits(Item.Content.GetId()));
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
                    if (MySession.Static.ControlledEntity == user && (lastTimeSound == DateTime.MinValue || (DateTime.UtcNow - lastTimeSound).TotalMilliseconds > 500))
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.PlayTakeItem);
                        lastTimeSound = DateTime.UtcNow;
                    }
                    System.Diagnostics.Debug.Assert((user.GetInventory() as MyInventory) != null, "Null or unexpected inventory type returned");
                    (user.GetInventory() as MyInventory).PickupItem(this, amount);
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
            string key = "";
            switch (actionEnum)
            {
                case UseActionEnum.PickUp:
                    key = MyInput.Static.GetGameControl(MyControlsSpace.PICK_UP).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
                    return new MyActionDescription()
                    {
                        Text = MyCommonTexts.NotificationPickupObject,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.PICK_UP), m_displayedText },
                        IsTextControlHint = false,
                        JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PICK_UP), m_displayedText },
                    };
                case UseActionEnum.Manipulate:
                    key = MyInput.Static.GetGameControl(MyControlsSpace.USE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
                    return new MyActionDescription()
                    {
                        Text = MyCommonTexts.NotificationPickupObject,
                        FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE), m_displayedText },
                        IsTextControlHint = false,
                        JoystickFormatParams = new object[] { MyControllerHelper.GetCodeForControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.USE), m_displayedText },
                    };
                default:
                    return new MyActionDescription();
            }
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

        public bool DoDamage(float damage, MyStringHash damageType, bool sync, long attackerId)
        {
            if (MarkedForClose)
                return false;

            if (sync)
            {
                if (Sync.IsServer)
                {
                    MySyncDamage.DoDamageSynced(this, damage, damageType, attackerId);
                    return true;
                }
                else
                {
                    return false;
                }

            }

            MyDamageInformation damageinfo = new MyDamageInformation(false, damage, damageType, attackerId);
            if (UseDamageSystem)
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
                    if (Sync.IsServer)
                    {
                        MyFloatingObjects.RemoveFloatingObject(this);
                    }
                }
                else
                {
                    if (Sync.IsServer)
                    {
                        MyFloatingObjects.RemoveFloatingObject(this, (MyFixedPoint)damageinfo.Amount);
                    }
                }
            }
            else
            {
                m_health -= 10 * damageinfo.Amount;

                if (UseDamageSystem)
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
                            return true;

                        var contentDefinitionId = Item.Content.GetId();
                        if (contentDefinitionId.TypeId == typeof(MyObjectBuilder_Component))
                        {
                            var definition = MyDefinitionManager.Static.GetComponentDefinition((Item.Content as MyObjectBuilder_Component).GetId());
                            if (MyRandom.Instance.NextFloat() < definition.DropProbability)
                                MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(Item.Amount * 0.8f, ScrapBuilder), PositionComp.GetPosition(), WorldMatrix.Forward, WorldMatrix.Up);
                        }
                    }

                    if (ItemDefinition != null && ItemDefinition.DestroyedPieceId.HasValue && Sync.IsServer)
                    {
                        MyPhysicalItemDefinition pieceDefinition;
                        if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(ItemDefinition.DestroyedPieceId.Value, out pieceDefinition))
                        {
                            MyFloatingObjects.Spawn(pieceDefinition, WorldMatrix.Translation, WorldMatrix.Forward, WorldMatrix.Up, ItemDefinition.DestroyedPieces);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Trying to spawn piece of the item after being destroyed, but definition wasn't found! - " + ItemDefinition.DestroyedPieceId.Value);
                        }
                    }

                    if (UseDamageSystem)
                        MyDamageSystem.Static.RaiseDestroyed(this, damageinfo);
                }
            }

            return true;
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

        bool IMyDestroyableObject.DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            return DoDamage(damage, damageType, sync, attackerId);
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


        public void SendCloseRequest()
        {
            MyMultiplayer.RaiseEvent(this,x => x.OnClosedRequest);
        }

        [Event, Reliable, Server]
        void OnClosedRequest()
        {
            if (!MySession.Static.CreativeMode && !MyEventContext.Current.IsLocallyInvoked && !MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            Close();
        }

    }
}