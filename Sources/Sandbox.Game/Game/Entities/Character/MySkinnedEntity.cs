#region Using

using Havok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Components;
using VRage.ModAPI;
using VRage.Import;
using Sandbox.Engine.Utils;
using Sandbox.Definitions;
using Sandbox.Game.Components;

#endregion

namespace Sandbox.Game.Entities
{
    #region Structs

    public struct MyAnimationCommand
    {
        public string AnimationSubtypeName;
        public MyPlaybackCommand PlaybackCommand;
        public MyBlendOption BlendOption;
        public MyFrameOption FrameOption;
        public string Area;
        public float BlendTime;
        public float TimeScale;
        public bool ExcludeLegsWhenMoving;
    }

    #endregion


    public class MySkinnedEntity : MyEntity
    {

        #region Fields

        private List<MyCharacterBone> m_bones = new List<MyCharacterBone>();
        Matrix[] m_boneRelativeTransforms;
        Matrix[] m_boneAbsoluteTransforms;

        
        public List<MyCharacterBone> Bones { get { return m_bones; } }

        public Matrix[] BoneAbsoluteTransforms { get { return m_boneAbsoluteTransforms; } } 
        public Matrix[] BoneRelativeTransforms { get { return m_boneRelativeTransforms; } }


        protected ulong m_actualUpdateFrame = 0;
        protected ulong m_actualDrawFrame = 0;
        protected bool m_characterBonesReady = false;

        List<Matrix> m_simulatedBones = new List<Matrix>();

        protected Dictionary<string, Quaternion> m_additionalRotations = new Dictionary<string, Quaternion>();

        Dictionary<string, MyAnimationPlayerBlendPair> m_animationPlayers = new Dictionary<string, MyAnimationPlayerBlendPair>();

        Queue<MyAnimationCommand> m_commandQueue = new Queue<MyAnimationCommand>();

        BoundingBoxD m_actualWorldAABB;
        BoundingBoxD m_aabb;


        #endregion

        #region Init


        public MySkinnedEntity()
        {
            this.Render = new MyRenderComponentSkinnedEntity();
            Render.EnableColorMaskHsv = true;
            Render.NeedsDraw = true;
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;
            Render.SkipIfTooSmall = false;
        }

        public override void Init(StringBuilder displayName,
                         string model,
                         MyEntity parentObject,
                         float? scale,
                         string modelCollision = null)
        {
            base.Init(displayName, model, parentObject, scale, modelCollision);
            InitBones();
        }


        protected void InitBones()
        {
            ObtainBones();
            
            m_animationPlayers.Clear();

            AddAnimationPlayer("", null);
        }

        public virtual void UpdateAnimation()
        {
            //if (Render.RenderObjectIDs.Length > 0 && MyRenderProxy.VisibleObjectsRead.Contains(Render.RenderObjectIDs[0]))
            {
                AdvanceAnimation();

                ProcessCommands();

                UpdateAnimationState();

                CalculateTransforms();

                UpdateRenderObject();
                    
            }
        }

        void UpdateBones()
        {
            foreach (var animationPlayer in m_animationPlayers)
            {
                animationPlayer.Value.UpdateBones();
            }
        }

        void AdvanceAnimation()
        {
            foreach (var animationPlayer in m_animationPlayers)
            {
                animationPlayer.Value.Advance();
            }
        }

        void UpdateAnimationState()
        {
            foreach (var animationPlayer in m_animationPlayers)
            {
                animationPlayer.Value.UpdateAnimationState();
            }
        }

        /// <summary>
        /// Get the bones from the model and create a bone class object for
        /// each bone. We use our bone class to do the real animated bone work.
        /// </summary>
        protected virtual void ObtainBones()
        {
            m_bones.Clear();

            foreach (MyModelBone bone in Model.Bones)
            {
                Matrix boneTransform = bone.Transform;

                // Create the bone object and add to the heirarchy
                MyCharacterBone newBone = new MyCharacterBone(bone.Name, boneTransform, bone.Parent != -1 ? m_bones[bone.Parent] : null);

                // Add to the bones for this model
                m_bones.Add(newBone);
            }

            m_boneRelativeTransforms = new Matrix[m_bones.Count];
            m_boneAbsoluteTransforms = new Matrix[m_bones.Count];
        }

        public Quaternion GetAdditionalRotation(string bone)
        {
            Quaternion res = Quaternion.Identity;

            if (string.IsNullOrEmpty(bone))
                return res;

            if (m_additionalRotations.TryGetValue(bone, out res))
                return res;

            return Quaternion.Identity;
        }

          
        #endregion

        #region Bones

        public MyCharacterBone FindBone(string name, out int index)
        {
            index = -1;
            if (name == null) return null;
            foreach (MyCharacterBone bone in m_bones)
            {
                index++;

                if (bone.Name == name)
                    return bone;
            }

            if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
            {
                Debug.Fail("Warning! Bone with name: " + name + " was not found in the skeleton of model name: " + this.Model.AssetName + ". Pleace check your bone definitions in SBC file.");
            }

            return null;
        }

        internal void AddAnimationPlayer(string name, string[] bones)
        {
            m_animationPlayers.Add(name, new MyAnimationPlayerBlendPair(this, bones, name));
        }

        internal bool TryGetAnimationPlayer(string name, out MyAnimationPlayerBlendPair player)
        {
            if (name == null)
                name = "";
            if (name == "Body")
                name = "";

            return m_animationPlayers.TryGetValue(name, out player);
        }

        internal void PlayerPlay(string playerName, MyAnimationDefinition animDefinition, bool firstPerson, MyFrameOption frameOption, float blendTime, float timeScale)
        {
            MyAnimationPlayerBlendPair player;
            if (TryGetAnimationPlayer(playerName, out player))
            {
                player.Play(animDefinition, firstPerson, frameOption, blendTime, timeScale);
            }
       //     else
         //       Debug.Fail("Non existing animation set");
        }

        internal void PlayerStop(string playerName, float blendTime)
        {
            MyAnimationPlayerBlendPair player;
            if (TryGetAnimationPlayer(playerName, out player))
            {
                player.Stop(blendTime);
            }
            // Commented out - asserts when there is no fingers animation
            //else
            //    Debug.Fail("Non existing animation set");
        }


        #endregion

        #region Simulation

        protected virtual void CalculateTransforms()
        {
            ProfilerShort.Begin("MySkinnedEntity.CalculateTransforms");

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Clear bones");
            foreach (var bone in Bones)
            {
                bone.Translation = Vector3.Zero;
                bone.Rotation = Quaternion.Identity;
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Update bones");

            UpdateBones();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("ComputeAbsoluteTransforms");
            for (int i = 0; i < Bones.Count; i++)
            {
                MyCharacterBone bone = Bones[i];
                bone.ComputeAbsoluteTransform();
                m_boneRelativeTransforms[i] = bone.ComputeBoneTransform();                
                BoneAbsoluteTransforms[i] = bone.AbsoluteTransform;
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            ProfilerShort.End();
        }

        bool TryGetAnimationDefinition(string animationSubtypeName, out MyAnimationDefinition animDefinition)
        {
            if (animationSubtypeName == null)
            {
                animDefinition = null;
                return false;
            }

            animDefinition = MyDefinitionManager.Static.TryGetAnimationDefinition(animationSubtypeName);
            if (animDefinition == null)
            {
                //Try backward compatibility
                //Backward compatibility
                string oldPath = System.IO.Path.Combine(MyFileSystem.ContentPath, animationSubtypeName);
                if (MyFileSystem.FileExists(oldPath))
                {
                    animDefinition = new MyAnimationDefinition()
                    {
                        AnimationModel = oldPath,
                        ClipIndex = 0,
                    };
                    return true;
                }

                animDefinition = null;
                return false;
            }

            return true;
        }

        protected void ProcessCommands()
        {
            if (m_commandQueue.Count > 0)
            {
                MyAnimationCommand command = m_commandQueue.Peek();

                if (command.PlaybackCommand == MyPlaybackCommand.Play)
                {
                    m_commandQueue.Dequeue();

                    MyAnimationDefinition animDefinition;
                    if (!TryGetAnimationDefinition(command.AnimationSubtypeName, out animDefinition))
                        return;


                    string bonesArea = animDefinition.InfluenceArea;
                    var frameOption = command.FrameOption;
                    bool useFirstPersonVersion = false;

                    OnAnimationPlay(animDefinition, command, ref bonesArea, ref frameOption, ref useFirstPersonVersion);

                    //override bones area if required
                    if (!string.IsNullOrEmpty(command.Area))
                        bonesArea = command.Area;

                    if (bonesArea == null)
                        bonesArea = "";

                    string[] boneAreas = bonesArea.Split(' ');
                    foreach (var boneArea in boneAreas)
                    {
                        PlayerPlay(boneArea, animDefinition, useFirstPersonVersion, frameOption, command.BlendTime, command.TimeScale);
                    }
                }

                else
                {
                    System.Diagnostics.Debug.Assert(command.PlaybackCommand == MyPlaybackCommand.Stop, "Unknown playback command");

                    m_commandQueue.Dequeue();

                    string bonesArea = command.Area == null ? "" : command.Area;
                    string[] boneAreas = bonesArea.Split(' ');

                    foreach (var boneArea in boneAreas)
                    {
                        PlayerStop(boneArea, command.BlendTime);
                    }
                }
            }
        }


        protected void FlushAnimationQueue()
        {
            while (m_commandQueue.Count > 0)
                ProcessCommands();
        }

        public virtual void AddCommand(MyAnimationCommand command, bool sync = false)
        {
            if (command.PlaybackCommand == MyPlaybackCommand.Play && command.BlendOption == MyBlendOption.Immediate)
            {
                m_commandQueue.Clear();
            }

            m_commandQueue.Enqueue(command);
        }


        protected virtual void OnAnimationPlay(MyAnimationDefinition animDefinition, MyAnimationCommand command, ref string bonesArea, ref MyFrameOption frameOption, ref bool useFirstPersonVersion)
        {
        }

        protected void UpdateRenderObject()
        {
            m_actualWorldAABB = BoundingBoxD.CreateInvalid();

            for (int i = 1; i < Model.Bones.Length; i++)
            {
                Vector3D p1 = Vector3D.Transform(Bones[i].Parent.AbsoluteTransform.Translation, WorldMatrix);
                Vector3D p2 = Vector3D.Transform(Bones[i].AbsoluteTransform.Translation, WorldMatrix);

                m_actualWorldAABB.Include(ref p1);
                m_actualWorldAABB.Include(ref p2);
            }

            ContainmentType containmentType;
            m_aabb.Contains(ref m_actualWorldAABB, out containmentType);
            if (containmentType != ContainmentType.Contains)
            {
                m_actualWorldAABB.Inflate(0.5f);
                MatrixD worldMatrix = WorldMatrix;
                VRageRender.MyRenderProxy.UpdateRenderObject(Render.RenderObjectIDs[0], ref worldMatrix, false, m_actualWorldAABB);
                m_aabb = m_actualWorldAABB;
            }
        }


        #endregion

    }
}
