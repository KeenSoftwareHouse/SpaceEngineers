using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRage.Animations
{
    /// <summary>
    /// Animation controller contains and drives skeletal animations.
    /// It also serves as an abstraction layer, hiding low/level classes.
    /// </summary>
    public class MyAnimationController
    {
        /// <summary>
        /// Simple pool allocator for bone results.
        /// </summary>
        public class MyResultBonesPool
        {
            private int m_boneCount = 0;
            private readonly List<List<MyAnimationClip.BoneState>> m_freeToUse = new List<List<MyAnimationClip.BoneState>>(8);
            private readonly List<List<MyAnimationClip.BoneState>> m_taken = new List<List<MyAnimationClip.BoneState>>(8);
            MyAnimationClip.BoneState[] m_restPose = null;

            /// <summary>
            /// Set the new bone count and default (rest) pose.
            /// </summary>
            public void Reset(MyCharacterBone[] restPoseBones)
            {
                m_freeToUse.Clear();
                m_taken.Clear();
                if (restPoseBones == null)
                {
                    m_boneCount = 0;
                    m_restPose = null;
                    return;
                }

                int boneCount = restPoseBones.Length;
                m_boneCount = boneCount;
                m_restPose = new MyAnimationClip.BoneState[boneCount];
                for (int i = 0; i < boneCount; i++)
                {
                    m_restPose[i] = new MyAnimationClip.BoneState();
                    m_restPose[i].Translation = restPoseBones[i].BindTransform.Translation;
                    m_restPose[i].Rotation = Quaternion.CreateFromRotationMatrix(restPoseBones[i].BindTransform);
                }
            }

            public void FreeAll()
            {
                foreach (var item in m_taken) // transfer all taken to free to use
                    m_freeToUse.Add(item);

                m_taken.Clear(); // clear taken
            }

            /// <summary>
            /// Allocate array of bones from pool. Bones are in the rest (bind) position by default.
            /// </summary>
            /// <returns></returns>
            public List<MyAnimationClip.BoneState> Alloc()
            {
                if (m_freeToUse.Count == 0)
                {
                    var rtn = new List<MyAnimationClip.BoneState>(m_boneCount);
                    rtn.SetSize(m_boneCount);
                    for (int i = 0; i < m_boneCount; i++)
                    {
                        // item not created yet
                        rtn[i] = new MyAnimationClip.BoneState();
                        // fill will default data
                        rtn[i].Translation = m_restPose[i].Translation;
                        rtn[i].Rotation = m_restPose[i].Rotation;
                    }
                    m_taken.Add(rtn);
                    return rtn;
                }
                else
                {
                    var rtn = m_freeToUse[m_freeToUse.Count - 1];
                    m_freeToUse.RemoveAt(m_freeToUse.Count - 1);
                    m_taken.Add(rtn);
                    for (int i = 0; i < m_boneCount; i++)
                    {
                        // item is already created
                        // fill will default data
                        rtn[i].Translation = m_restPose[i].Translation;
                        rtn[i].Rotation = m_restPose[i].Rotation;
                    }
                    return rtn;
                }
            }

            public void Free(List<MyAnimationClip.BoneState> toBeFreed)
            {
                // this is ok, there is going to be about 5 items
                // we're going from the end because we expect that this allocator will be
                // used as "stack"
                int indexInTaken = -1;
                for (int i = m_taken.Count - 1; i >= 0; i--)
                    if (m_taken[i] == toBeFreed)
                    {
                        indexInTaken = i;
                        break;
                    }
                if (indexInTaken == -1)
                {
                    Debug.Fail("Cannot free this bone array result!");
                    return;
                }
                m_freeToUse.Add(m_taken[indexInTaken]);
                m_taken.RemoveAtFast(indexInTaken);
            }
        }
        // ----------------------------------------------------------------------
        // list of layers (state machines)
        private readonly List<MyAnimationStateMachine> m_layers;
        // layer redirection table (layer name -> layer index)
        private readonly Dictionary<string, int> m_tableLayerNameToIndex;
        // allocation pool for results
        public readonly MyResultBonesPool ResultBonesPool;

        // ----------------------------------------------------------------------
        #region Constructor

        // Default constructor.
        public MyAnimationController()
        {
            m_layers = new List<MyAnimationStateMachine>(1);
            m_tableLayerNameToIndex = new Dictionary<string, int>(1);
            Variables = new MyAnimationVariableStorage();
            ResultBonesPool = new MyResultBonesPool();
        }
        #endregion

        // ----------------------------------------------------------------------
        #region Properties

        // variables
        public MyAnimationVariableStorage Variables { get; private set; }

        #endregion

        // ----------------------------------------------------------------------

        // Get layer with matching name. Returns null if the layer having this name is not found.
        public MyAnimationStateMachine GetLayerByName(string layerName)
        {
            int index;
            if (m_tableLayerNameToIndex.TryGetValue(layerName, out index))
            {
                return m_layers[index];
            }
            else
            {
                return null;
            }
        }

        // Get layer by index. Returns null if index is invalid.
        public MyAnimationStateMachine GetLayerByIndex(int index)
        {
            Debug.Assert(index >= 0 && index < m_layers.Count);
            return m_layers[index];
        }

        /// <summary>
        /// Create animation layer with unique name. Parameter insertionIndex can be left -1 to add the layer at the end.
        /// If layer with same name is already present, method fails and returns null.
        /// </summary>
        public MyAnimationStateMachine CreateLayer(string name, int insertionIndex = -1)
        {
            if (GetLayerByName(name) != null)
            {
                Debug.Fail("Cannot create layer: layer with name '" + name + "' already exists.");
                return null;
            }

            MyAnimationStateMachine newLayer = new MyAnimationStateMachine();
            newLayer.Name = name;
            if (insertionIndex != -1)
            {
                m_tableLayerNameToIndex.Add(name, insertionIndex);
                m_layers.Insert(insertionIndex, newLayer);
            }
            else
            {
                m_tableLayerNameToIndex.Add(name, m_layers.Count);
                m_layers.Add(newLayer);
            }

            return newLayer;
        }

        // Delete all animation layers.
        public void DeleteAllLayers()
        {
            m_tableLayerNameToIndex.Clear();
            m_layers.Clear();
        }

        // Get count of layers.
        public int GetLayerCount()
        {
            return m_layers.Count;
        }

        /// <summary>
        /// Update this animation controller.
        /// </summary>
        /// <param name="animationUpdateData">See commentary in MyAnimationUpdateData</param>
        public void Update(ref MyAnimationUpdateData animationUpdateData)
        {
            if (animationUpdateData.CharacterBones == null)
                return; // safety
            if (animationUpdateData.Controller == null)
                animationUpdateData.Controller = this;
            ResultBonesPool.FreeAll(); // free all previous bone allocations, return them to pool
                                       // (this is a safe place to do that, nobody should use them by this time)

            // Set the first layer.
            if (m_layers.Count > 0)
            {
                m_layers[0].Update(ref animationUpdateData);
            }
            // Other layers can replace some transforms or add them
            for (int i = 1; i < m_layers.Count; i++)
            {
                var currentLayer = m_layers[i];
                MyAnimationUpdateData animationUpdateDataLast = animationUpdateData;
                animationUpdateData.LayerBoneMask = null;
                animationUpdateData.BonesResult = null;
                m_layers[i].Update(ref animationUpdateData);
                if (animationUpdateData.BonesResult == null)
                {
                    animationUpdateData = animationUpdateDataLast; // restore backup
                    continue;
                }

                int boneCount = animationUpdateData.BonesResult.Count;
                var bonesLast = animationUpdateDataLast.BonesResult;
                var bones = animationUpdateData.BonesResult;
                var bonesBindPose = animationUpdateDataLast.CharacterBones;

                if (currentLayer.Mode == MyAnimationStateMachine.MyBlendingMode.Replace)
                {
                    for (int j = 0; j < boneCount; j++)
                    {
                        if (animationUpdateData.LayerBoneMask[j] == false)
                        {
                            bones[j].Translation = bonesLast[j].Translation;
                            bones[j].Rotation = bonesLast[j].Rotation;
                        }
                    }
                }
                else if (currentLayer.Mode == MyAnimationStateMachine.MyBlendingMode.Add)
                {
                    for (int j = 0; j < boneCount; j++)
                    {
                        if (animationUpdateData.LayerBoneMask[j])
                        {
                            // add result to current
                            Vector3 addTranslation;
                            Quaternion addRotation;
                            bonesBindPose[j].GetCompleteTransform(ref bones[j].Translation, ref bones[j].Rotation,
                                out addTranslation, out addRotation);
                            bones[j].Translation = bonesLast[j].Translation + addTranslation;
                            bones[j].Rotation = bonesLast[j].Rotation * addRotation;
                            // TODO: Check this math :)
                        }
                        else
                        {
                            // unaffected, copy last result (deep copy because we allocate from pool and bone is currenlty class)
                            bones[j].Translation = bonesLast[j].Translation;
                            bones[j].Rotation = bonesLast[j].Rotation;
                        }
                    }
                }
                ResultBonesPool.Free(animationUpdateDataLast.BonesResult);
            }

        }

        // ----------------------------------------------------------------------

        /// <summary>
        /// Trigger an action in all layers. 
        /// If there is a transition having given (non-null) name, it is followed immediatelly.
        /// Conditions of transition are ignored.
        /// </summary>
        public void TriggerAction(MyStringId actionName)
        {
            foreach (var layer in m_layers)
                layer.TriggerAction(actionName);
        }
    }
}
