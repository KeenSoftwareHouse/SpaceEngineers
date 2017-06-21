using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Game.Entities.Character;
using VRageMath;
using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Common;
using Sandbox.Game.Entities;
using Sandbox.Definitions;
using VRage.Utils;
using VRageRender.Animations;

namespace Sandbox.Engine.Physics
{
    /// <summary>
    /// Maps Ragdoll instance to Character Bones
    /// </summary>
    public class MyRagdollMapper
    {
        public const float RAGDOLL_DEACTIVATION_TIME = 10f; // in seconds

        //Matrix boneToRigidBodyTransform = new Matrix(
        //                1, 0, 0, 0,
        //                0, 0, -1, 0,
        //                0, 1, 0, 0,
        //                0, 0, 0, 1);

        //Matrix rigidBodyToBoneTransform = new Matrix(
        //                1, 0, 0, 0,
        //                0, 0, 1, 0,
        //                0, -1, 0, 0,
        //                0, 0, 0, 1);

        // Not needed since having new Character models
        //static Matrix Identity3DSMax = Matrix.CreateWorld(Vector3.Zero, Vector3.Up, -Vector3.Forward);

        /// <summary>
        /// Dictionary map of rigid body indices to the corresponding list of the bones binded to the specific rigid body
        /// </summary>
        private Dictionary<int, List<int>> m_rigidBodiesToBonesIndices;

        /// <summary>
        /// Character associated with this ragdoll
        /// </summary>
        private MyCharacter m_character;

        /// <summary>
        /// Reference to bones which are used for mapping
        /// </summary>
        private MyCharacterBone[] m_bones;

        /// <summary>
        /// List for storing matrices between method calls
        /// </summary>
        private Matrix[] m_ragdollRigidBodiesAbsoluteTransforms;

        /// <summary>
        /// Rigid Body transform matrices as on load
        /// </summary>
        public Matrix[] BodiesRigTransfoms;

        /// <summary>
        /// Bodies absolute rig transforms
        /// </summary>
        public Matrix[] BonesRigTransforms;

        /// <summary>
        /// Bodies rig tranfsforms inverted
        /// </summary>
        public Matrix[] BodiesRigTransfomsInverted;


        /// <summary>
        /// Bones absolute rig transforms inverted
        /// </summary>
        public Matrix[] BonesRigTransformsInverted;

        /// <summary>
        /// List of rig transfomrs from ragdoll rigid body to a correspondig bone. This is used to eliminate artiffacts, when rigid bodie's shape centers are not always aligned with the bones.
        /// </summary>
        private Matrix[] m_bodyToBoneRigTransforms;

        /// <summary>
        /// Inversion of m_bodyToBoneRigTransforms
        /// </summary>
        private Matrix[] m_boneToBodyRigTransforms;

        /// <summary>
        /// Dictionary of rigid body names to their indices
        /// </summary>
        private Dictionary<String, int> m_rigidBodies;

        /// <summary>
        /// Indicates whether ragdoll associated with this mapper was set to keyframed or not
        /// </summary>
        public bool IsKeyFramed
        {
            get
            {
                if (Ragdoll == null) return false;
                return Ragdoll.IsKeyframed;
            }
        }

        /// <summary>
        /// true if position of ragdoll changed after simulation
        /// </summary>
        public bool PositionChanged;

        /// <summary>
        /// Indicates whether this mapper was inicialized
        /// </summary>
        private bool m_inicialized;

        /// <summary>
        /// List of Ragdoll Rigid Bodies indices which are set to be keyframed in simulation
        /// </summary>
        private List<int> m_keyframedBodies;

        /// <summary>
        /// List of Ragdoll Rigid Bodies indices which are set to be dynamic in simulation
        /// </summary>
        private List<int> m_dynamicBodies;

        /// <summary>
        /// List of mappings bodies to bones list
        /// </summary>
        private Dictionary<string, MyCharacterDefinition.RagdollBoneSet> m_ragdollBonesMappings;

        private MatrixD m_lastSyncedWorldMatrix = MatrixD.Identity;

        public float DeactivationCounter = RAGDOLL_DEACTIVATION_TIME;

        private bool m_changed;

        /// <summary>
        /// True if at least some of the bones are simulated and ragdoll was added to world. Partly = some bodies set to dynamic, some keyframed
        /// </summary>
        public bool IsPartiallySimulated { get; private set; }

        /// <summary>
        /// Dictionary of rigid bodies indices to the list of bones that should be bound to these bodies
        /// </summary>
        public Dictionary<int, List<int>> RigidBodiesToBonesIndices { get { return m_rigidBodiesToBonesIndices; } }


        /// <summary>
        /// True if the mapper was set to be active and can be used
        /// </summary>
        public bool IsActive { get; private set; }

        public HkRagdoll Ragdoll 
        { 
            get 
            {
                if (m_character == null) return null;
                if (m_character.Physics == null) return null;
                return m_character.Physics.Ragdoll; 
            } 
        }



        /// <summary>
        /// Constructs the new mapper
        /// </summary>
        /// <param name="ragdoll">The ragdoll model</param>
        /// <param name="bones">List of the mapped bones</param>
        public MyRagdollMapper(MyCharacter character, MyCharacterBone[] bones)
        {
            Debug.Assert(character.Physics.Ragdoll != null, "Creating ragdoll mapper without ragdoll?");
            Debug.Assert(bones != null && bones.Length > 0, "Creating ragdoll mapper without mapped bones?");


            m_rigidBodiesToBonesIndices = new Dictionary<int, List<int>>();
            m_character = character;
            m_bones = bones;

            m_rigidBodies = new Dictionary<string, int>();
            m_keyframedBodies = new List<int>();
            m_dynamicBodies = new List<int>();
            IsActive = false;
            m_inicialized = false;
            IsPartiallySimulated = false;
        }

        public int BodyIndex(string bodyName)
        {
            int index;
            if (m_rigidBodies.TryGetValue(bodyName, out index))
            {
                return index;
            }

            //Debug.Fail("Ragdoll bone with name " + bodyName + " was not found!");
            
            return 0;
        }

        /// <summary>
        /// Initializes the mapper
        /// </summary>
        /// <param name="ragdollBonesMappings">Dictionary containing rigid body names and the corresponding character bones names list</param>
        /// <returns></returns>
        public bool Init(Dictionary<string, MyCharacterDefinition.RagdollBoneSet> ragdollBonesMappings)
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.Init");
                MyLog.Default.WriteLine("MyRagdollMapper.Init");
            }
            
            Debug.Assert(ragdollBonesMappings != null, "Inicializing ragdoll mapper with null data?");
            
            m_ragdollBonesMappings = ragdollBonesMappings;            

            foreach (var boneSet in ragdollBonesMappings)
            {
                try
                {
                    String rigidBodyName = boneSet.Key;
                    String[] boneNames = boneSet.Value.Bones;
                    List<int> boneIndices = new List<int>();
                    int rigidBodyIndex = Ragdoll.FindRigidBodyIndex(rigidBodyName);
                    System.Diagnostics.Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(rigidBodyIndex), "Ragdoll bones mapping error!  Rigid body with name: " + rigidBodyName + " was not found in ragdoll.");
                    foreach (var bone in boneNames)
                    {
                        int boneIndex = Array.FindIndex(m_bones, x => x.Name == bone);
                        System.Diagnostics.Debug.Assert(m_bones.IsValidIndex(boneIndex), "Ragdoll bones mapping error! Bone with name: " + bone + " was not found in the character! ");
                        if (m_bones.IsValidIndex(boneIndex)) boneIndices.Add(boneIndex);
                        else return false;
                    }
                    if (Ragdoll.RigidBodies.IsValidIndex(rigidBodyIndex)) AddRigidBodyToBonesMap(rigidBodyIndex, boneIndices, rigidBodyName);
                    else return false;
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message);
                    return false;
                }
            }
            
            InitRigTransforms();

            m_inicialized = true;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.Init FINISHED");
                MyLog.Default.WriteLine("MyRagdollMapper.Init FINISHED");
            }

            return true;
        }



        /// <summary>
        /// This computes the rig transforms from rigid bodies to ragdoll bones. Must be called before any transforms are made on rigid bodies!
        /// </summary>
        private void InitRigTransforms()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.InitRigTransforms");
                MyLog.Default.WriteLine("MyRagdollMapper.InitRigTransforms");
            }
            
            m_ragdollRigidBodiesAbsoluteTransforms = new Matrix[Ragdoll.RigidBodies.Count];
            m_bodyToBoneRigTransforms = new Matrix[Ragdoll.RigidBodies.Count];
            m_boneToBodyRigTransforms = new Matrix[Ragdoll.RigidBodies.Count];
            BodiesRigTransfoms = new Matrix[Ragdoll.RigidBodies.Count];
            BodiesRigTransfomsInverted = new Matrix[Ragdoll.RigidBodies.Count];

            foreach (var bodyIndex in m_rigidBodiesToBonesIndices.Keys)
            {
                MyCharacterBone bone = m_bones[m_rigidBodiesToBonesIndices[bodyIndex].First()];

                Matrix boneRig = bone.GetAbsoluteRigTransform();

                Matrix rigidBodyTransform = Ragdoll.RigTransforms[bodyIndex];

                Matrix bodyToBone = boneRig * Matrix.Invert(rigidBodyTransform);

                Matrix boneToBody = rigidBodyTransform * Matrix.Invert(boneRig);

                m_bodyToBoneRigTransforms[bodyIndex] = bodyToBone;

                m_boneToBodyRigTransforms[bodyIndex] = boneToBody;

                BodiesRigTransfoms[bodyIndex] = rigidBodyTransform;

                BodiesRigTransfomsInverted[bodyIndex] = Matrix.Invert(rigidBodyTransform);

                Debug.Assert(m_bodyToBoneRigTransforms[bodyIndex].IsValid(), "Ragdoll body to bone transform is invalid!");
                Debug.Assert(m_boneToBodyRigTransforms[bodyIndex].IsValid(), "Ragdoll bone to body transform is invalid!");
                Debug.Assert(BodiesRigTransfoms[bodyIndex].IsValid(), "Ragdoll rig transform is invalid!");
                Debug.Assert(BodiesRigTransfomsInverted[bodyIndex].IsValid(), "Ragdoll inverted rig transform is invalid!");
            }

            BonesRigTransforms = new Matrix[m_bones.Length];
            BonesRigTransformsInverted = new Matrix[m_bones.Length];

            for (int i = 0; i < BonesRigTransforms.Length; i++)
            {
                BonesRigTransforms[i] = m_bones[i].GetAbsoluteRigTransform();

                BonesRigTransformsInverted[i] = Matrix.Invert(m_bones[i].GetAbsoluteRigTransform());

                Debug.Assert(BonesRigTransforms[i].IsValid(), "Bone rig transform is invalid!");
                Debug.Assert(BonesRigTransformsInverted[i].IsValid(), "Bone inverted rig transform is invalid!");
            }

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.InitRigTransforms - END");
                MyLog.Default.WriteLine("MyRagdollMapper.InitRigTransforms - END");
            }
        }

        /// <summary>
        /// Adds mappings
        /// </summary>
        /// <param name="rigidBodyIndex"></param>
        /// <param name="bonesIndices"></param>
        /// <param name="rigidBodyName"></param>
        private void AddRigidBodyToBonesMap(int rigidBodyIndex, List<int> bonesIndices, String rigidBodyName)
        {
            Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(rigidBodyIndex), "Ragdoll: RigidBody index is invalid. The ragdoll mode is invalid, is the rigid body definitions correct?");
            
            foreach (var index in bonesIndices)
            {
                Debug.Assert(m_bones.IsValidIndex(index), "The bone index is invalid. The ragdoll definition is invalid, use proper names for character bones in ragdoll mappings definition.");
            }
            
            m_rigidBodiesToBonesIndices.Add(rigidBodyIndex, bonesIndices);
            m_rigidBodies.Add(rigidBodyName, rigidBodyIndex);
        }


        /// <summary>
        /// Sets the pose of the ragdoll to match the pose of the bones
        /// </summary>        
        public void UpdateRagdollPose()
        {
            Debug.Assert(Ragdoll != null, "Ragdoll mapper ragdoll in not inicialized, can not update pose!");
            
            if (Ragdoll == null) return;
            if (!m_inicialized || !IsActive) return;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateRagdollPose");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollPose");
            }

            CalculateRagdollTransformsFromBones();
            UpdateRagdollRigidBodies();

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateRagdollPose - END");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollPose - END");
            }

        }


        /// <summary>
        /// Compute the transforms that should be used for rigid bodies based on current bones tranforms in the havokWorld
        /// </summary>
        /// <param name="worldMatrix"></param>
        private void CalculateRagdollTransformsFromBones()
        {
            Debug.Assert(Ragdoll != null, "Ragdoll mapper ragdoll in not inicialized, calculate ragdoll transforms!");
            
            if (Ragdoll == null) return;
            if (!m_inicialized || !IsActive) return;          

            foreach (var rigidBodyIndex in m_rigidBodiesToBonesIndices.Keys)
            {
                HkRigidBody rigidBody = Ragdoll.RigidBodies[rigidBodyIndex];
                var boneIndices = m_rigidBodiesToBonesIndices[rigidBodyIndex];

                Matrix finalTransform = m_bones[boneIndices.First()].AbsoluteTransform;

                m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex] = finalTransform;

                Debug.Assert(m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex].IsValid(), "Ragdoll body transform is invalid");
            }
        }

        /// <summary>
        /// Set the ragdolls rigid bodies to computed transforms
        /// </summary>
        private void UpdateRagdollRigidBodies()
        {
            Debug.Assert(Ragdoll != null, "Ragdoll mapper ragdoll in not inicialized, calculate ragdoll transforms!");
            
            if (Ragdoll == null) return;
            if (!m_inicialized || !IsActive) return; 
            
            Debug.Assert(Ragdoll.WorldMatrix.IsValid() && Ragdoll.WorldMatrix != MatrixD.Zero, "Ragdoll matrix is invalid!");

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateRagdollRigidBodies");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollRigidBodies");
            }

            foreach (var rigidBodyIndex in m_keyframedBodies)
            {
                Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(rigidBodyIndex), "Ragdoll rigid body index is invalid. Is the ragdoll model correctly built?");
                HkRigidBody rigidBody = Ragdoll.RigidBodies[rigidBodyIndex];

                Debug.Assert(m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex].IsValid(), "Ragdoll body absolute transform is invalid");
                Debug.Assert(m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex] != Matrix.Zero, "Ragdoll body absolute transform is zero");                               

                if (m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex].IsValid() && m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex] != Matrix.Zero)
                {
                    Matrix transform = m_boneToBodyRigTransforms[rigidBodyIndex] * m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex];
                    // Havok doesn't like transforms with not normalized rotations, need to fix here
                    Quaternion rotation = Quaternion.CreateFromRotationMatrix(transform.GetOrientation());
                    Vector3 translation = transform.Translation;
                    rotation.Normalize();
                    transform = Matrix.CreateFromQuaternion(rotation);
                    transform.Translation = translation;                    
                    Ragdoll.SetRigidBodyLocalTransform(rigidBodyIndex, transform);
                }
            }

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateRagdollRigidBodies - END");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollRigidBodies - END");
            }
        }

        /// <summary>
        /// Sets the character's bones to the ragdoll's pose
        /// </summary>
        /// <param name="weight">transform influence weight on dynamic bodies</param>
        public void UpdateCharacterPose(float dynamicBodiesWeight = 1.0f, float keyframedBodiesWeight = 1.0f)
        {
            Debug.Assert(Ragdoll != null, "Ragdoll mapper ragdoll in not inicialized, can't calculate ragdoll transforms!");

            if (!m_inicialized || !IsActive) return;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateCharacterPose");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateCharacterPose");
            }

            float weight = dynamicBodiesWeight;
            if (m_keyframedBodies.Contains(Ragdoll.m_ragdollTree.m_rigidBodyIndex))
            {
                weight = keyframedBodiesWeight;
            }

            // Instead of blind settings, we need to traverse tree from root to children             
            //SetBoneTo(Ragdoll.m_ragdollTree, weight, dynamicBodiesWeight, keyframedBodiesWeight, true );
            SetBoneTo(Ragdoll.m_ragdollTree, weight, dynamicBodiesWeight, keyframedBodiesWeight, false); //<ib.ragdoll> traslation can't be added for root..

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateCharacterPose - END");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateCharacterPose - END");
            }
        }

        private void SetBoneTo(RagdollBone ragdollBone, float weight, float dynamicChildrenWeight, float keyframedChildrenWeight, bool translationEnabled)
        {
            if (Ragdoll == null) return;
            if (!m_inicialized || !IsActive) return;

            int firstBoneIndex = m_rigidBodiesToBonesIndices[ragdollBone.m_rigidBodyIndex].First();

            MyCharacterBone bone = m_bones[firstBoneIndex];

            //Matrix localTransform = Ragdoll.GetRigidBodyLocalTransform(ragdollBone.m_rigidBodyIndex);

            Matrix localTransform = m_bodyToBoneRigTransforms[ragdollBone.m_rigidBodyIndex] * Ragdoll.GetRigidBodyLocalTransform(ragdollBone.m_rigidBodyIndex);

            Matrix parentMatrix = (bone.Parent != null) ? bone.Parent.AbsoluteTransform : Matrix.Identity;

            Matrix absoluteMatrixInverted = Matrix.Invert(bone.BindTransform * parentMatrix);

            Matrix finalTransform = localTransform * absoluteMatrixInverted;

            //finalTransform = rigidBodyToBoneTransform * finalTransform;

            Debug.Assert(finalTransform.IsValid() && finalTransform != Matrix.Zero, "Ragdoll - final bone transform is invalid!");

            if (finalTransform.IsValid() && finalTransform != Matrix.Zero)
            {

                if (weight == 1.0f)
                {
                    bone.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)finalTransform.GetOrientation()));

                    // NOTE: If enabled, sometimes ragdoll bodies got extra translation which leads to disproporced transfomations on limbs, therefore disabled on all bodies except the firs one                    
                    if (translationEnabled)// || m_character.IsDead) 
                    {
                        bone.Translation = finalTransform.Translation;
                    }
                }
                else
                {
                    bone.Rotation = Quaternion.Slerp(bone.Rotation, Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)finalTransform.GetOrientation())), weight);

                    // NOTE: If enabled, sometimes ragdoll bodies got extra translation which leads to disproporced transfomations on limbs, therefore disabled
                    if (translationEnabled)// || m_character.IsDead) 
                    {
                        bone.Translation = Vector3.Lerp(bone.Translation, finalTransform.Translation, weight);
                    }
                }
            }

            bone.ComputeAbsoluteTransform();

            foreach (var childBone in ragdollBone.m_children)
            {
                float childWeight = dynamicChildrenWeight;
                if (m_keyframedBodies.Contains(childBone.m_rigidBodyIndex))
                {
                    childWeight = keyframedChildrenWeight;
                }
                //SetBoneTo(childBone, childWeight, dynamicChildrenWeight, keyframedChildrenWeight, MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION);

                if (IsPartiallySimulated)
                {
                    SetBoneTo(childBone, childWeight, dynamicChildrenWeight, keyframedChildrenWeight, false);
                }
                else
                {
                    SetBoneTo(childBone, childWeight, dynamicChildrenWeight, keyframedChildrenWeight, (Ragdoll.IsRigidBodyPalmOrFoot(childBone.m_rigidBodyIndex)) ? false : MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION); 
                }               
            }
        }

        public void Activate()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.Activate");
                MyLog.Default.WriteLine("MyRagdollMapper.Activate");
            }

            if (Ragdoll == null)
            {
                IsActive = false;
                return;
            }
            IsActive = true;

            m_character.Physics.Ragdoll.AddedToWorld -= OnRagdollAdded;
            m_character.Physics.Ragdoll.AddedToWorld += OnRagdollAdded;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.Activate - END");
                MyLog.Default.WriteLine("MyRagdollMapper.Activate - END");
            }
        }

        public void Deactivate()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.Deactivate");
                MyLog.Default.WriteLine("MyRagdollMapper.Deactivate");
            }

            if (IsPartiallySimulated) 
            {
                DeactivatePartialSimulation();
            }
            
            IsActive = false;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.Deactivate - END");
                MyLog.Default.WriteLine("MyRagdollMapper.Deactivate -END");
            }
        }

        public void SetRagdollToKeyframed()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.SetRagdollToKeyframed");
                MyLog.Default.WriteLine("MyRagdollMapper.SetRagdollToKeyframed");
            }

            Debug.Assert(Ragdoll != null, "Can not set ragdoll to keyframed, ragdoll is null!");
            if (Ragdoll == null) return;
            Ragdoll.SetToKeyframed();
            m_dynamicBodies.Clear();
            m_keyframedBodies.Clear();
            m_keyframedBodies.AddRange(m_rigidBodies.Values);
            IsPartiallySimulated = false;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.SetRagdollToKeyframed - END");
                MyLog.Default.WriteLine("MyRagdollMapper.SetRagdollToKeyframed - END");
            }
        }

        public void SetRagdollToDynamic()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.SetRagdollToDynamic");
                MyLog.Default.WriteLine("MyRagdollMapper.SetRagdollToDynamic");
            }

            Debug.Assert(Ragdoll != null, "Can not set ragdoll to dynamic mode, ragdoll is null!");

            if (Ragdoll == null) return;

            Ragdoll.SetToDynamic();
            m_keyframedBodies.Clear();
            m_dynamicBodies.Clear();
            m_dynamicBodies.AddRange(m_rigidBodies.Values);
            IsPartiallySimulated = false;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.SetRagdollToDynamic - END");
                MyLog.Default.WriteLine("MyRagdollMapper.SetRagdollToDynamic - END");
            }
        }


        public List<int> GetBodiesBindedToBones(List<String> bones)
        {
            List<int> bodies = new List<int>();

            foreach (var bone in bones)
            {
                foreach (var pair in m_ragdollBonesMappings)
                {
                    if (pair.Value.Bones.Contains(bone))
                    {
                        if (!bodies.Contains(m_rigidBodies[pair.Key]))
                        {
                            bodies.Add(m_rigidBodies[pair.Key]);
                        }
                    }
                }
            }
            return bodies;
        }

        public void ActivatePartialSimulation(List<int> dynamicRigidBodies = null)
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.ActivatePartialSimulation");
                MyLog.Default.WriteLine("MyRagdollMapper.ActivatePartialSimulation");
            }

            if (!m_inicialized || Ragdoll == null || IsPartiallySimulated) return;

            if (dynamicRigidBodies != null)
            {
                m_dynamicBodies.Clear();
                m_dynamicBodies.AddList(dynamicRigidBodies);
                m_keyframedBodies.Clear();
                m_keyframedBodies.AddRange(m_rigidBodies.Values.Except(dynamicRigidBodies));
            }           

            SetBodiesSimulationMode();

            if (Ragdoll.InWorld)
            {
                Ragdoll.EnableConstraints();
                Ragdoll.Activate();
            }


            //Ragdoll.DisableConstraints();
            //Ragdoll.SetToKeyframed();
            //Ragdoll.ResetToRigPose();
            
            //Ragdoll.EnableConstraints();

            IsActive = true;
            IsPartiallySimulated = true;

            UpdateRagdollPose();
            Ragdoll.ResetVelocities();

            m_character.Physics.Ragdoll.AddedToWorld -= OnRagdollAdded;
            m_character.Physics.Ragdoll.AddedToWorld += OnRagdollAdded;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.ActivatePartialSimulation - END");
                MyLog.Default.WriteLine("MyRagdollMapper.ActivatePartialSimulation - END");
            }

        }

        private void SetBodiesSimulationMode()
        {
            foreach (var bodyIndex in m_dynamicBodies)
            {
                Ragdoll.SetToDynamic(bodyIndex);                
                Ragdoll.SwitchRigidBodyToLayer(bodyIndex, MyPhysics.CollisionLayers.RagdollCollisionLayer);
            }

            foreach (var bodyIndex in m_keyframedBodies)
            {
                Ragdoll.SetToKeyframed(bodyIndex);                
                Ragdoll.SwitchRigidBodyToLayer(bodyIndex, MyPhysics.CollisionLayers.RagdollCollisionLayer);
            }               
        }

        void OnRagdollAdded(HkRagdoll ragdoll)
        {
            Debug.Assert(ragdoll == Ragdoll, "Wrong ragdoll model!");
            if (MyFakes.ENABLE_RAGDOLL_DEBUG) Debug.WriteLine("MyRagdollMapper.OnRagdollAdded");
            if (IsPartiallySimulated)
            {
                SetBodiesSimulationMode();
            }
        }

        public void DeactivatePartialSimulation()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.DeactivatePartialSimulation");
                MyLog.Default.WriteLine("MyRagdollMapper.DeactivatePartialSimulation");
            }

            if (!IsPartiallySimulated) return;
            if (Ragdoll == null) return;

            if (Ragdoll.InWorld)
            {
                Ragdoll.DisableConstraints();
                Ragdoll.Deactivate();                
            }

            m_keyframedBodies.Clear();
            m_dynamicBodies.Clear();
            m_dynamicBodies.AddRange(m_rigidBodies.Values);
            SetBodiesSimulationMode();
            Ragdoll.ResetToRigPose();

            IsPartiallySimulated = false;
            IsActive = false;
            m_character.Physics.Ragdoll.AddedToWorld -= OnRagdollAdded;

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.DeactivatePartialSimulation - END");
                MyLog.Default.WriteLine("MyRagdollMapper.DeactivatePartialSimulation - END");
            }
        }

        public void DebugDraw(MatrixD worldMatrix)
        {
            //TODO: Create some debug draw for ragdoll
            if (!MyDebugDrawSettings.ENABLE_DEBUG_DRAW) return;

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_ORIGINAL_RIG)
            {
                foreach (var bodyIndex in m_rigidBodiesToBonesIndices.Keys)
                {
                    Matrix debug = BodiesRigTransfoms[bodyIndex] * worldMatrix;
                    VRageRender.MyRenderProxy.DebugDrawSphere(debug.Translation, 0.03f, Color.White, 0.1f, false);
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_ORIGINAL_RIG)
            {
                foreach (var bodyIndex in m_rigidBodiesToBonesIndices.Keys)
                {
                    Matrix debug = m_bodyToBoneRigTransforms[bodyIndex] * BodiesRigTransfoms[bodyIndex] * worldMatrix;
                    //VRageRender.MyRenderProxy.DebugDrawSphere(debug.Translation, 0.025f, Color.Purple, 0.8f, false);
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_BONES_DESIRED)
            {
                foreach (var bodyIndex in m_rigidBodiesToBonesIndices.Keys)
                {
                    Matrix debug = m_bodyToBoneRigTransforms[bodyIndex] * Ragdoll.GetRigidBodyLocalTransform(bodyIndex) * worldMatrix;
                    VRageRender.MyRenderProxy.DebugDrawSphere(debug.Translation, 0.035f, Color.Blue, 0.8f, false);
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_COMPUTED_BONES)
            {
                foreach (var bone in m_bones)
                {
                    Matrix debug = bone.AbsoluteTransform * worldMatrix;
                    VRageRender.MyRenderProxy.DebugDrawSphere(debug.Translation, 0.03f, Color.Red, 0.8f, false);
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_RAGDOLL_POSE)
            {
                foreach (var bodyIndex in m_rigidBodiesToBonesIndices.Keys)
                {
                    Color color = new Color(((bodyIndex & 1) * 255), ((bodyIndex & 2) * 255), ((bodyIndex & 4) * 255));
                    var matrix = (MatrixD)Ragdoll.GetRigidBodyLocalTransform(bodyIndex) * worldMatrix;
                    DrawShape(Ragdoll.RigidBodies[bodyIndex].GetShape(), matrix, color, 0.6f);
                    VRageRender.MyRenderProxy.DebugDrawAxis(matrix, 0.3f, false);
                    VRageRender.MyRenderProxy.DebugDrawSphere(matrix.Translation, 0.03f, Color.Green, 0.8f, false);
                }
            }
        }



        /// <summary>
        /// Update Ragdoll position in the Havok world to copy the Physics position
        /// </summary>
        public void UpdateRagdollPosition()
        {
            if (Ragdoll == null) return;
            if (!m_inicialized || !IsActive) return;
            if (!IsPartiallySimulated && !IsKeyFramed) return;

            //if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            //{
            //    Debug.WriteLine("MyRagdollMapper.UpdateRagdollPosition");
            //    MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollPosition");
            //}

            // Note: Character's world matrix can be changed by server and desync, this can cause artifacts, therefore it can be better to use physics pos
            MatrixD havokWorldMatrix;
            if (m_character.IsDead)
            {
                havokWorldMatrix = m_character.WorldMatrix;
                havokWorldMatrix.Translation = m_character.Physics.WorldToCluster(havokWorldMatrix.Translation);
                if (MyFakes.ENABLE_RAGDOLL_DEBUG)
                {
                    Debug.Assert(Vector3.Distance(havokWorldMatrix.Translation, m_character.Physics.GetWorldMatrix().Translation) <= 0.00001f, " Ragdoll debug: Position of render component and physics is desynced");
                }
            }
            else
            {
                havokWorldMatrix = m_character.Physics.GetWorldMatrix();
                havokWorldMatrix.Translation = m_character.Physics.WorldToCluster(havokWorldMatrix.Translation);
            }

            Debug.Assert(havokWorldMatrix.IsValid(), "Ragdoll world matrix in Havok is invalid");
            Debug.Assert(havokWorldMatrix != MatrixD.Zero, "Ragdoll world matrix in Havok is invalid");
            // If ragdoll is repositioned to a far distance instantly, havok doesn't hadle it properly. Simulation is broken etc.
            // Therefore in that case we need to reposition the ragdoll without breaking the simulation - setting all bodies to new position
            if (havokWorldMatrix.IsValid() && havokWorldMatrix != MatrixD.Zero)
            {
                double distance = (havokWorldMatrix.Translation - Ragdoll.WorldMatrix.Translation).LengthSquared();
                double forwardChange = ((Vector3)havokWorldMatrix.Forward - Ragdoll.WorldMatrix.Forward).LengthSquared();
                double upChange = ((Vector3)havokWorldMatrix.Up - Ragdoll.WorldMatrix.Up).LengthSquared();
                                
                m_changed = distance > 0.001f || forwardChange > 0.001f || upChange > 0.001f;

                if (distance > 10 || m_character.m_positionResetFromServer)
                {
                    m_character.m_positionResetFromServer = false;
                    if (MyFakes.ENABLE_RAGDOLL_DEBUG)
                    {
                        Debug.WriteLine("MyRagdollMapper.UpdateRagdollPosition");
                        MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollPosition");
                    }

                    Ragdoll.SetWorldMatrix(havokWorldMatrix);                    
                    if (MyFakes.ENABLE_RAGDOLL_DEBUG)
                    {
                        Debug.Fail(" Ragdoll debug: Position of ragdoll has changed more than 10 m");
                    }
                }
                else if (m_changed)
                {
                    if (MyFakes.ENABLE_RAGDOLL_DEBUG)
                    {
                        Debug.WriteLine("MyRagdollMapper.UpdateRagdollPosition - SetWorldMatrix");
                        MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollPosition");
                    }

                    //Original version: directly set world matrix
                    //Ragdoll.SetWorldMatrix(havokWorldMatrix, true);

                    // Smoothed version
                    if (IsPartiallySimulated)
                    {
                        MatrixD newHavokWorldMatrix = new MatrixD();

                        const float wr = 0.9f;
                        Vector3 Forward = (1 - wr) * Ragdoll.WorldMatrix.Forward + wr * havokWorldMatrix.Forward;
                        newHavokWorldMatrix = MatrixD.CreateFromDir(Forward, Ragdoll.WorldMatrix.Up);

                        const float wp = 0.9f;
                        newHavokWorldMatrix.Translation = (1 - wp) * Ragdoll.WorldMatrix.Translation + wp * havokWorldMatrix.Translation;

                        Ragdoll.SetWorldMatrix(newHavokWorldMatrix, true);
                    }
                    else
                    {
                        Ragdoll.SetWorldMatrix(havokWorldMatrix, true);
                    }

                                        
                }
            }

            //if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            //{
            //    Debug.WriteLine("MyRagdollMapper.UpdateRagdollPosition");
            //    MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollPosition");
            //}
        }

        public void ResetRagdollVelocities()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.ResetRagdollVelocities");
                MyLog.Default.WriteLine("MyRagdollMapper.ResetRagdollVelocities");
            }

            if (Ragdoll == null) return;
            
            Ragdoll.ResetVelocities();

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.ResetRagdollVelocities - END");
                MyLog.Default.WriteLine("MyRagdollMapper.ResetRagdollVelocities - END");
            }
        }

        public static void DrawShape(HkShape shape, MatrixD worldMatrix, Color color, float alpha, bool shaded = true)
        {
            color.A = (byte)(alpha * 255);

            switch (shape.ShapeType)
            {
                case HkShapeType.Capsule:
                    var capsule = (HkCapsuleShape)shape;
                    Vector3 vertexA = Vector3.Transform(capsule.VertexA, worldMatrix);
                    Vector3 vertexB = Vector3.Transform(capsule.VertexB, worldMatrix);
                    VRageRender.MyRenderProxy.DebugDrawCapsule(vertexA, vertexB, capsule.Radius, color, false, false);
                    break;
                default:
                    // TODO: Add code to draw shape properly, AABB is not usefull..
                    VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, 0.05f, color, 1.0f, false);
                    break;
            }
        }

        public void SetLinearVelocity(Vector3 linearVelocity, bool onKeyframedOnly = true)
        {
            if (!m_inicialized || !IsActive) return;
            if (onKeyframedOnly)
            {
                foreach (var bodyindex in m_keyframedBodies)
                {
                    Ragdoll.RigidBodies[bodyindex].LinearVelocity = linearVelocity;
                }
            }
            else
            {
                foreach (var body in Ragdoll.RigidBodies)
                {
                    body.LinearVelocity = linearVelocity;
                }
            }
        }

        public void SetAngularVelocity(Vector3 angularVelocity, bool onKeyframedOnly = true)
        {
            if (!m_inicialized || !IsActive) return;
            if (onKeyframedOnly)
            {
                foreach (var bodyindex in m_keyframedBodies)
                {
                    Ragdoll.RigidBodies[bodyindex].AngularVelocity = angularVelocity;
                }
            }
            else
            {
                foreach (var body in Ragdoll.RigidBodies)
                {
                    body.AngularVelocity = angularVelocity;
                }
            }
        }

        public void SetVelocities()
        {
            if (!m_inicialized || !IsActive) return;
            if (Ragdoll == null) return;
            
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.Assert(m_character.Physics.AngularVelocity.Length() <= 100f, " Ragdoll debug: Character's angular velocity over 100");
                Debug.Assert(m_character.Physics.LinearVelocity.Length() <= 150f, " Ragdoll debug: Character's angular velocity over 150");                
            }

            if (m_changed)
            {
                SetAngularVelocity(m_character.Physics.AngularVelocity);
                SetLinearVelocity(m_character.Physics.LinearVelocity);
            }
        }

        public void SetLimitedVelocities()
        {
            //<ib.change> Ragdoll in jetpack mode
            //Console.WriteLine("SetLimitedVelocities:");

            if (Ragdoll.RigidBodies[0] == null) return;

            const float linearVelocityEps = 1.0f;
            const float angularVelocityEps = 1.0f;
            float maxLinearVelocity = Math.Max(10.0f,Ragdoll.RigidBodies[0].LinearVelocity.Length() + linearVelocityEps);
            float maxAngularVelocity = Math.Max(4.0f*(float)Math.PI, Ragdoll.RigidBodies[0].AngularVelocity.Length() + angularVelocityEps);

            int counter = 0;
            foreach (var bodyindex in m_dynamicBodies)
            {
                if (IsPartiallySimulated)
                {
                    Ragdoll.RigidBodies[bodyindex].MaxLinearVelocity = maxLinearVelocity;
                    Ragdoll.RigidBodies[bodyindex].MaxAngularVelocity = maxAngularVelocity;
                    Ragdoll.RigidBodies[bodyindex].LinearDamping = 0.2f;
                    Ragdoll.RigidBodies[bodyindex].AngularDamping = 0.2f;
                    //Console.WriteLine(" {0} Index:{1} MaxLVel:{2} MaxAVel:{3} LD:{4} AD:{5}", counter, bodyindex, Ragdoll.RigidBodies[bodyindex].MaxLinearVelocity, Ragdoll.RigidBodies[bodyindex].MaxAngularVelocity, Ragdoll.RigidBodies[bodyindex].LinearDamping, Ragdoll.RigidBodies[bodyindex].AngularDamping);
                    counter++;
                }
                else
                {
                    Ragdoll.RigidBodies[bodyindex].MaxLinearVelocity = Ragdoll.MaxLinearVelocity;
                    Ragdoll.RigidBodies[bodyindex].MaxAngularVelocity = Ragdoll.MaxAngularVelocity;
                    Ragdoll.RigidBodies[bodyindex].LinearDamping = 0.5f;
                    Ragdoll.RigidBodies[bodyindex].AngularDamping = 0.5f;
                }
            }
        }

        public void UpdateRagdollAfterSimulation()
        {
            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateRagdollAfterSimulation");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollAfterSimulation");
            }

            if (!m_inicialized || !IsActive) return;
            if (Ragdoll == null || !Ragdoll.InWorld) return;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateRagdollAfterSimulation");                       

            MatrixD ragdollWorld = Ragdoll.WorldMatrix;
            Ragdoll.UpdateWorldMatrixAfterSimulation();
            Ragdoll.UpdateLocalTransforms();

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.Assert(Vector3.Distance(ragdollWorld.Translation, Ragdoll.WorldMatrix.Translation) <= 10f, " Ragdoll debug: ragdoll position changed more than 10 m/s in simulation step");               
            }

            PositionChanged = ragdollWorld != Ragdoll.WorldMatrix;

            // TODO: THIS DOESN'T WORK, UNFORTUNATELLY HAVOK DOESN'T DEACTIVATE THE RAGDOLL
            // SEEMS LIKE SOMETHING IS STILL INTERACTING - THIS COULD BE CAUSED BY CONSTRAINTS
            // WHICH DIDN'T SETTLED?
            if (MyFakes.FORCE_RAGDOLL_DEACTIVATION && m_character.IsDead)
            {
                if ((DeactivationCounter <= 0) && Ragdoll.IsSimulationActive)
                {   
                    Ragdoll.ForceDeactivate();
                    DeactivationCounter = RAGDOLL_DEACTIVATION_TIME;
                }
                else
                {
                    DeactivationCounter -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                }
            }

            if (MyFakes.ENABLE_RAGDOLL_DEBUG)
            {
                Debug.WriteLine("MyRagdollMapper.UpdateRagdollAfterSimulation - END");
                MyLog.Default.WriteLine("MyRagdollMapper.UpdateRagdollAfterSimulation - END");
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        internal void UpdateRigidBodiesTransformsSynced(int transformsCount, Matrix worldMatrix, Matrix[] transforms)
        {           
            if (!m_inicialized || !IsActive) return;
            if (Ragdoll == null || !Ragdoll.InWorld) return;

            Debug.Assert(transformsCount == transforms.Length, "Wrong ragdoll transforms sync - transforms don't match!");
            Debug.Assert(transformsCount == Ragdoll.RigidBodies.Count, "The count of ragdoll transform matrices doesn't match the count of rigid bodies!");
            List<Vector3> linearVelocities = new List<Vector3>();
            List<Vector3> angularVelocities = new List<Vector3>();

            if (transformsCount == m_ragdollRigidBodiesAbsoluteTransforms.Length)
            {
                for (int i =0;i<transformsCount;++i)
                {
                    Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(i), "Sync - Ragdoll rigid body index is invalid. Is the ragdoll model correctly built?");
                    Debug.Assert(transforms[i].IsValid(), "Sync - Ragdoll body absolute transform is invalid");
                    Debug.Assert(transforms[i] != Matrix.Zero, "Sync - Ragdoll body absolute transform is zero");
                    linearVelocities.Add(Ragdoll.RigidBodies[i].LinearVelocity);
                    angularVelocities.Add(Ragdoll.RigidBodies[i].AngularVelocity);
                    Ragdoll.SetRigidBodyLocalTransform(i, transforms[i]);
                }
            }

            MatrixD havokWorld = worldMatrix;
            havokWorld.Translation = m_character.Physics.WorldToCluster(worldMatrix.Translation);
            Ragdoll.SetWorldMatrix(havokWorld);
            //Ragdoll.SetTransforms(havokWorld, false);

            foreach (var rigidBodyIndex in m_rigidBodiesToBonesIndices.Keys)
            {
                Ragdoll.RigidBodies[rigidBodyIndex].LinearVelocity = linearVelocities[rigidBodyIndex];
                Ragdoll.RigidBodies[rigidBodyIndex].AngularVelocity = angularVelocities[rigidBodyIndex];
            }            
        }

        public void SyncRigidBodiesTransforms(MatrixD worldTransform)
        {
            bool changed = m_lastSyncedWorldMatrix != worldTransform;
            foreach (var rigidBodyIndex in m_rigidBodiesToBonesIndices.Keys)
            {
                Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(rigidBodyIndex), "Sync - Ragdoll rigid body index is invalid. Is the ragdoll model correctly built?");
                HkRigidBody rigidBody = Ragdoll.RigidBodies[rigidBodyIndex];

               Matrix transform = Ragdoll.GetRigidBodyLocalTransform(rigidBodyIndex);
               changed =  m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex] != transform || changed;
               m_ragdollRigidBodiesAbsoluteTransforms[rigidBodyIndex] = transform;
             
            } 
            if (changed && MyFakes.ENABLE_RAGDOLL_CLIENT_SYNC)
            {
                m_character.SendRagdollTransforms(worldTransform, m_ragdollRigidBodiesAbsoluteTransforms);
                m_lastSyncedWorldMatrix = worldTransform;
            }
        }

        public HkRigidBody GetBodyBindedToBone(MyCharacterBone myCharacterBone)
        {
            if (Ragdoll == null)
            {
                Debug.Fail("Ragdoll is not initialized!");
                return null;
            }

            if (myCharacterBone == null)
            {
                Debug.Fail("Invalid parameter - cannot be null! ");
                return null;
            }

            foreach (var pair in m_ragdollBonesMappings)
            {
                if (pair.Value.Bones.Contains(myCharacterBone.Name))
                {
                    Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(m_rigidBodies[pair.Key]), "Invalid rigid body index!");
                    return Ragdoll.RigidBodies[m_rigidBodies[pair.Key]];
                }
            }

            Debug.Fail("Requested bone was not found in mappings!");
            return null;
        }
    }
}
