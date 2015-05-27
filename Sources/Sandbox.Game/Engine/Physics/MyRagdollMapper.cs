using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Game.Entities.Character;
using VRageMath;
using System.Diagnostics;
using Sandbox.Engine.Utils;

namespace Sandbox.Engine.Physics
{
    /// <summary>
    /// Maps Ragdoll instance to Character Bones
    /// </summary>
    public class MyRagdollMapper
    {
        Matrix boneToRigidBodyTransform = new Matrix(
                        1, 0, 0, 0,
                        0, 0, -1, 0,
                        0, 1, 0, 0,
                        0, 0, 0, 1);

        Matrix rigidBodyToBoneTransform = new Matrix(
                        1, 0, 0, 0,
                        0, 0, 1, 0,
                        0, -1, 0, 0,
                        0, 0, 0, 1);

        // Not needed since having new Character models
        //static Matrix Identity3DSMax = Matrix.CreateWorld(Vector3.Zero, Vector3.Up, -Vector3.Forward);

        /// <summary>
        /// Dictionary map of rigid body indices to the corresponding list of the bones binded to the specific rigid body
        /// </summary>
        private Dictionary<int, List<int>> m_rigidBodiesToBonesIndices;

        private MyCharacter m_character;

        /// <summary>
        /// Reference to bones which are used for mapping
        /// </summary>
        private List<MyCharacterBone> m_bones;

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
        private Dictionary<string, string[]> m_ragdollBonesMappings;

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
        public MyRagdollMapper(MyCharacter character, List<MyCharacterBone> bones)
        {
            Debug.Assert(character.Physics.Ragdoll != null, "Creating ragdoll mapper without ragdoll?");
            Debug.Assert(bones != null && bones.Count > 0, "Creating ragdoll mapper without mapped bones?");


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
            return m_rigidBodies[bodyName];
        }

        /// <summary>
        /// Initializes the mapper
        /// </summary>
        /// <param name="ragdollBonesMappings">Dictionary containing rigid body names and the corresponding character bones names list</param>
        /// <returns></returns>
        public bool Init(Dictionary<string, string[]> ragdollBonesMappings)
        {
            Debug.Assert(ragdollBonesMappings != null, "Inicializing ragdoll mapper with null data?");
            m_ragdollBonesMappings = ragdollBonesMappings;            

            foreach (var boneSet in ragdollBonesMappings)
            {
                try
                {
                    String rigidBodyName = boneSet.Key;
                    String[] boneNames = boneSet.Value;
                    List<int> boneIndices = new List<int>();
                    int rigidBodyIndex = Ragdoll.FindRigidBodyIndex(rigidBodyName);
                    System.Diagnostics.Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(rigidBodyIndex), "Ragdoll bones mapping error!  Rigid body with name: " + rigidBodyName + " was not found in ragdoll.");
                    foreach (var bone in boneNames)
                    {
                        int boneIndex = m_bones.FindIndex(x => x.Name == bone);
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
            return true;
        }



        /// <summary>
        /// This computes the rig transforms from rigid bodies to ragdoll bones. Must be called before any transforms are made on rigid bodies!
        /// </summary>
        private void InitRigTransforms()
        {
            m_ragdollRigidBodiesAbsoluteTransforms = new Matrix[Ragdoll.RigidBodies.Count];
            m_bodyToBoneRigTransforms = new Matrix[Ragdoll.RigidBodies.Count];
            m_boneToBodyRigTransforms = new Matrix[Ragdoll.RigidBodies.Count];
            BodiesRigTransfoms = new Matrix[Ragdoll.RigidBodies.Count];
            BodiesRigTransfomsInverted = new Matrix[Ragdoll.RigidBodies.Count];
            foreach (var bodyIndex in m_rigidBodiesToBonesIndices.Keys)
            {
                MyCharacterBone bone = m_bones[m_rigidBodiesToBonesIndices[bodyIndex].First()];

                Matrix boneRig = bone.GetAbsoluteRigTransform();

                Matrix rigidBodyTransform = Ragdoll.RigidBodies[bodyIndex].GetRigidBodyMatrix();

                Matrix bodyToBone = boneRig * Matrix.Invert(rigidBodyTransform);

                Matrix boneToBody = rigidBodyTransform * Matrix.Invert(boneRig);

                m_bodyToBoneRigTransforms[bodyIndex] = bodyToBone;

                m_boneToBodyRigTransforms[bodyIndex] = boneToBody;

                BodiesRigTransfoms[bodyIndex] = rigidBodyTransform;

                BodiesRigTransfomsInverted[bodyIndex] = Matrix.Invert(rigidBodyTransform);
            }

            BonesRigTransforms = new Matrix[m_bones.Count];
            BonesRigTransformsInverted = new Matrix[m_bones.Count];
            for (int i = 0; i < BonesRigTransforms.Length; i++)
            {
                BonesRigTransforms[i] = m_bones[i].GetAbsoluteRigTransform();

                BonesRigTransformsInverted[i] = Matrix.Invert(m_bones[i].GetAbsoluteRigTransform());
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
            CalculateRagdollTransformsFromBones();
            UpdateRagdollRigidBodies();
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
            Debug.Assert(Ragdoll.WorldMatrix.IsValid(), "Ragdoll matrix is invalid!");
            foreach (var rigidBodyIndex in m_keyframedBodies)
            {
                Debug.Assert(Ragdoll.RigidBodies.IsValidIndex(rigidBodyIndex), "Ragdoll rigid body index is invalid. Is the ragdoll model correctly built?");
                HkRigidBody rigidBody = Ragdoll.RigidBodies[rigidBodyIndex];

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

        }

        /// <summary>
        /// Sets the character's bones to the ragdoll's pose
        /// </summary>
        /// <param name="weight">transform influence weight on dynamic bodies</param>
        public void UpdateCharacterPose(float dynamicBodiesWeight = 1.0f, float keyframedBodiesWeight = 1.0f)
        {
            Debug.Assert(Ragdoll != null, "Ragdoll mapper ragdoll in not inicialized, can't calculate ragdoll transforms!");
            if (!m_inicialized || !IsActive) return;


            float weight = dynamicBodiesWeight;
            if (m_keyframedBodies.Contains(Ragdoll.m_ragdollTree.m_rigidBodyIndex))
            {
                weight = keyframedBodiesWeight;
            }

            // Instead of blind settings, we need to traverse tree from root to children 
            SetBoneTo(Ragdoll.m_ragdollTree, weight, dynamicBodiesWeight, keyframedBodiesWeight);
        }

        private void SetBoneTo(RagdollBone ragdollBone, float weight, float dynamicChildrenWeight, float keyframedChildrenWeight)
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

            if (weight == 1.0f)
            {
                bone.Rotation = Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)finalTransform.GetOrientation()));

                // NOTE: If enabled, sometimes ragdoll bodies got extra translation which leads to disproporced transfomations on limbs, therefore disabled on all bodies except the firs one                    
                if (MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION)
                {
                    bone.Translation = finalTransform.Translation;
                }
            }
            else
            {
                bone.Rotation = Quaternion.Slerp(bone.Rotation, Quaternion.CreateFromRotationMatrix(Matrix.Normalize((Matrix)finalTransform.GetOrientation())), weight);

                // NOTE: If enabled, sometimes ragdoll bodies got extra translation which leads to disproporced transfomations on limbs, therefore disabled
                if (MyFakes.ENABLE_RAGDOLL_BONES_TRANSLATION)
                {
                    bone.Translation = Vector3.Lerp(bone.Translation, finalTransform.Translation, weight);
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
                SetBoneTo(childBone, childWeight, dynamicChildrenWeight, keyframedChildrenWeight);
            }
        }

        public void Activate()
        {
            if (Ragdoll == null)
            {
                IsActive = false;
                return;
            }
            IsActive = true;
        }

        public void Deactivate()
        {
            if (IsPartiallySimulated) DeactivatePartialSimulation();
            IsActive = false;
        }

        public void SetRagdollToKeyframed()
        {
            if (Ragdoll == null) return;
            Ragdoll.SetToKeyframed();
            m_dynamicBodies.Clear();
            m_keyframedBodies.Clear();
            m_keyframedBodies.AddRange(m_rigidBodies.Values);
            IsPartiallySimulated = false;
        }

        public void SetRagdollToDynamic()
        {
            if (Ragdoll == null) return;
            Ragdoll.SetToDynamic();
            m_keyframedBodies.Clear();
            m_dynamicBodies.Clear();
            m_dynamicBodies.AddRange(m_rigidBodies.Values);
            IsPartiallySimulated = false;
        }


        public List<int> GetBodiesBindedToBones(List<String> bones)
        {
            List<int> bodies = new List<int>();

            foreach (var bone in bones)
            {
                foreach (var pair in m_ragdollBonesMappings)
                {
                    if (pair.Value.Contains(bone))
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

        public void ActivatePartialSimulation(List<int> dynamicRigidBodies)
        {
            if (!m_inicialized || Ragdoll == null || IsPartiallySimulated) return;
                        
            m_dynamicBodies.Clear();
            m_dynamicBodies.AddList(dynamicRigidBodies);
            m_keyframedBodies.Clear();
            m_keyframedBodies.AddRange(m_rigidBodies.Values.Except(dynamicRigidBodies));
            
            foreach (var bodyIndex in m_dynamicBodies)
            {
                Ragdoll.SetToDynamic(bodyIndex);                
                Ragdoll.SwitchRigidBodyToLayer(bodyIndex, MyPhysics.RagdollCollisionLayer);
            }

            // TODO: When we have fully wrapped HkRagdollContraintData update parameters here..
            //foreach (var constraint in Ragdoll.RagdollConstraintsData)
            //{

            //}

            foreach (var bodyIndex in m_keyframedBodies)
            {
                Ragdoll.SetToKeyframed(bodyIndex);                
                Ragdoll.SwitchRigidBodyToLayer(bodyIndex, MyPhysics.RagdollCollisionLayer);
            }

            Ragdoll.EnableConstraints();
            Ragdoll.Activate();

            IsActive = true;
            IsPartiallySimulated = true;
        }

        public void DeactivatePartialSimulation()
        {
            if (!IsPartiallySimulated) return;
            if (Ragdoll == null) return;

            Ragdoll.Deactivate();

            m_keyframedBodies.Clear();
            m_dynamicBodies.Clear();
            m_dynamicBodies.AddRange(m_rigidBodies.Values);

            IsPartiallySimulated = false;
            IsActive = false;
        }

        public void DebugDraw(Matrix worldMatrix)
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
                    VRageRender.MyRenderProxy.DebugDrawSphere(debug.Translation, 0.025f, Color.Purple, 0.8f, false);
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
                    DrawShape(Ragdoll.RigidBodies[bodyIndex].GetShape(), Ragdoll.GetRigidBodyLocalTransform(bodyIndex) * worldMatrix, color, 0.6f);
                }

            }
        }



        /// <summary>
        /// Update Ragdoll position in the Havok world to copy the Physics position
        /// </summary>
        public void UpateHavokWorldPosition()
        {
            if (Ragdoll == null) return;
            if (!m_inicialized || !IsActive) return;
            if (!IsPartiallySimulated && !IsKeyFramed) return;

            Matrix havokWorldMatrix;
            if (m_character.Physics.CharacterProxy != null)
            {
                MatrixD characterTransform = m_character.Physics.CharacterProxy.GetRigidBodyTransform();

                Vector3 transformedCenter = Vector3.TransformNormal(m_character.Physics.Center, characterTransform);
                characterTransform.Translation = m_character.Physics.CharacterProxy.Position - transformedCenter;
                havokWorldMatrix = characterTransform;
            }
            else
            {
                havokWorldMatrix = m_character.Physics.GetWorldMatrix();
                havokWorldMatrix.Translation = m_character.Physics.WorldToCluster(havokWorldMatrix.Translation);
            }

            // If ragdoll is repositioned to a far distance instantly, havok doesn't hadle it properly. Simulation is broken etc.
            // Therefore in that case we need to reposition the ragdoll without breaking the simulation - setting all bodies to new position
            Vector3 distance = havokWorldMatrix.Translation - Ragdoll.WorldMatrix.Translation;
            if (distance.LengthSquared() > 100)
            {
                Ragdoll.SetWorldMatrix(havokWorldMatrix, true);
            }
            else
            {
                Ragdoll.SetWorldMatrix(havokWorldMatrix, !IsPartiallySimulated);
            }
        }

        public static void DrawShape(HkShape shape, Matrix worldMatrix, Color color, float alpha, bool shaded = true)
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
            
            SetAngularVelocity(m_character.Physics.AngularVelocity);
            SetLinearVelocity(m_character.Physics.LinearVelocity);
        }

        public void UpdateRagdollAfterSimulation()
        {
            if (!m_inicialized || !IsActive) return;
            if (Ragdoll == null || !Ragdoll.IsActive) return;

            m_character.Physics.Ragdoll.UpdateWorldMatrixAfterSimulation();
        }
    }
}
