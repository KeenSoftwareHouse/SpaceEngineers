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
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Import;
using Sandbox.Engine.Utils;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.EntityComponents;
using VRageRender.Animations;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Definitions.Animation;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Serialization;
using VRageRender.Import;
using VRageRender.Messages;

#endregion

namespace Sandbox.Game.Entities
{
    #region Structs

    public struct MyAnimationCommand
    {
        [Serialize(MyObjectFlags.Nullable)]
        public string AnimationSubtypeName;
        public MyPlaybackCommand PlaybackCommand;
        public MyBlendOption BlendOption;
        public MyFrameOption FrameOption;
        [Serialize(MyObjectFlags.Nullable)]
        public string Area;
        public float BlendTime;
        public float TimeScale;
        public bool ExcludeLegsWhenMoving;
        public bool KeepContinuingAnimations;
    }

    public struct MyAnimationSetData
    {
        public float BlendTime;
        public string Area;
        public AnimationSet AnimationSet;
    }

    #endregion


    public class MySkinnedEntity : MyEntity
    {
        /// <summary>
        /// VRAGE TODO: THIS IS TEMPORARY! Remove when by the time we use only the new animation system.
        /// </summary>
        public bool UseNewAnimationSystem = false;

        private const int MAX_BONE_DECALS_COUNT = 10;

        #region Fields

        /// <summary>
        /// Shortcut to animation controller component.
        /// </summary>
        private MyAnimationControllerComponent m_compAnimationController;

        // moved to MyAnimationControllerComponent

        // private List<MyCharacterBone> m_bones = new List<MyCharacterBone>(); 
        // Matrix[] m_boneRelativeTransforms;
        // Matrix[] m_boneAbsoluteTransforms;

        public MyAnimationControllerComponent AnimationController { get { return m_compAnimationController; } }        

        public Matrix[] BoneAbsoluteTransforms { get { return m_compAnimationController.BoneAbsoluteTransforms; } }
        public Matrix[] BoneRelativeTransforms { get { return m_compAnimationController.BoneRelativeTransforms; } }

        private Dictionary<int, List<uint>> m_boneDecals = new Dictionary<int, List<uint>>();
        public List<MyBoneDecalUpdate> DecalBoneUpdates { get; private set; }

        protected ulong m_actualUpdateFrame = 0;
		internal ulong ActualUpdateFrame { get { return m_actualUpdateFrame; } }
        protected ulong m_actualDrawFrame = 0;

        protected Dictionary<string, Quaternion> m_additionalRotations = new Dictionary<string, Quaternion>(); // should be moved to MyAnimationControllerComponent

        Dictionary<string, MyAnimationPlayerBlendPair> m_animationPlayers = new Dictionary<string, MyAnimationPlayerBlendPair>();

        Queue<MyAnimationCommand> m_commandQueue = new Queue<MyAnimationCommand>();

        BoundingBoxD m_actualWorldAABB;
        BoundingBoxD m_aabb;

        List<MyAnimationSetData> m_continuingAnimSets = new List<MyAnimationSetData>();

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

            MyEntityTerrainHeightProviderComponent entityTerrainHeightComp = new MyEntityTerrainHeightProviderComponent();
            Components.Add(entityTerrainHeightComp);
            m_compAnimationController = new MyAnimationControllerComponent();
            m_compAnimationController.ReloadBonesNeeded = ObtainBones;
            m_compAnimationController.InverseKinematics.TerrainHeightProvider = entityTerrainHeightComp;
            Components.Add(m_compAnimationController);
            DecalBoneUpdates = new List<MyBoneDecalUpdate>();
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

        public void SetBoneLODs(Dictionary<float, string[]> boneLODs)
        {
            foreach (var animationPlayer in m_animationPlayers)
            {
                animationPlayer.Value.SetBoneLODs(boneLODs);
            }
        }

        public virtual void UpdateAnimation(float distance)
        {
            if (!MyPerGameSettings.AnimateOnlyVisibleCharacters || MySandboxGame.IsDedicated ||
              (Render != null && Render.RenderObjectIDs.Length > 0 && MyRenderProxy.VisibleObjectsRead != null && MyRenderProxy.VisibleObjectsRead.Contains(Render.RenderObjectIDs[0])))
            {
                UpdateContinuingSets();

                bool advanced = AdvanceAnimation();
                bool processed = ProcessCommands();

                UpdateAnimationState();

                if (advanced || processed || UseNewAnimationSystem)
                {
                    CalculateTransforms(distance);
                    UpdateRenderObject();
                }
            }

            UpdateBoneDecals();
        }
      
        void UpdateContinuingSets()
        {
            foreach (var animationSet in m_continuingAnimSets)
            {
                System.Diagnostics.Debug.Assert(animationSet.AnimationSet.Continuous, "Wrong animation set here!");
                PlayAnimationSet(animationSet);
            }
        }

        void UpdateBones(float distance)
        {
            foreach (var animationPlayer in m_animationPlayers)
            {
                animationPlayer.Value.UpdateBones(distance);
            }
        }

        bool AdvanceAnimation()
        {
            bool animationAdvanced = false;
            foreach (var animationPlayer in m_animationPlayers)
            {
                animationAdvanced = animationPlayer.Value.Advance() || animationAdvanced;
            }
            return animationAdvanced;
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
        public virtual void ObtainBones()
        {
            MyCharacterBone[] characterBones = new MyCharacterBone[Model.Bones.Length];
            Matrix[] relativeTransforms = new Matrix[Model.Bones.Length];
            Matrix[] absoluteTransforms = new Matrix[Model.Bones.Length];
            for (int i = 0; i < Model.Bones.Length; i++)
            {
                MyModelBone bone = Model.Bones[i];
                Matrix boneTransform = bone.Transform;
                // Create the bone object and add to the heirarchy
                MyCharacterBone parent = bone.Parent != -1 ? characterBones[bone.Parent] : null;
                MyCharacterBone newBone = new MyCharacterBone(
                    bone.Name, parent, boneTransform, i, relativeTransforms, absoluteTransforms);
                // Add to the bone array for this model
                characterBones[i] = newBone;
            }

            // pass array of bones to animation controller
            m_compAnimationController.SetCharacterBones(characterBones, relativeTransforms, absoluteTransforms);
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

        internal void AddAnimationPlayer(string name, string[] bones)
        {
            m_animationPlayers.Add(name, new MyAnimationPlayerBlendPair(this, bones, null, name));
        }

        internal bool TryGetAnimationPlayer(string name, out MyAnimationPlayerBlendPair player)
        {
            if (name == null)
                name = "";
            if (name == "Body")
                name = "";

            return m_animationPlayers.TryGetValue(name, out player);
        }

        internal DictionaryReader<string, MyAnimationPlayerBlendPair> GetAllAnimationPlayers()
        {
            return m_animationPlayers;
        }

        void PlayAnimationSet(MyAnimationSetData animationSetData)
        {
            if (MyRandom.Instance.NextFloat(0, 1) < animationSetData.AnimationSet.Probability)
            {
                float total = animationSetData.AnimationSet.AnimationItems.Sum(x => x.Ratio);
                if (total > 0)
                {
                    float r = MyRandom.Instance.NextFloat(0, 1);
                    float rel = 0;
                    foreach (var animationItem in animationSetData.AnimationSet.AnimationItems)
                    {
                        rel += animationItem.Ratio / total;

                        if (r < rel)
                        {
                            var command = new MyAnimationCommand()
                            {
                                AnimationSubtypeName = animationItem.Animation,
                                PlaybackCommand = MyPlaybackCommand.Play,
                                Area = animationSetData.Area,
                                BlendTime = animationSetData.BlendTime,
                                TimeScale = 1,
                                KeepContinuingAnimations = true
                            };

                            ProcessCommand(ref command);
                            break;
                        }
                    }
                }
            }
        }

        internal void PlayersPlay(string bonesArea, MyAnimationDefinition animDefinition, bool firstPerson, MyFrameOption frameOption, float blendTime, float timeScale)
        {
            string[] players = bonesArea.Split(' ');

            if (animDefinition.AnimationSets != null)
            {
                foreach (var animationSet in animDefinition.AnimationSets)
                {
                    var animationSetData = new MyAnimationSetData()
                        {
                            BlendTime = blendTime,
                            Area = bonesArea,
                            AnimationSet = animationSet
                        };

                    if (animationSet.Continuous)
                    {
                        m_continuingAnimSets.Add(animationSetData);
                        continue;
                    }

                    PlayAnimationSet(animationSetData);
                }

                return;
            }

            foreach (var player in players)
            {
                PlayerPlay(player, animDefinition, firstPerson, frameOption, blendTime, timeScale);
            }
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

        protected virtual void CalculateTransforms(float distance)
        {
            ProfilerShort.Begin("MySkinnedEntity.CalculateTransforms");

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Clear bones");
            //foreach (var bone in Bones)
            //{
            //    bone.Translation = Vector3.Zero;
            //    bone.Rotation = Quaternion.Identity;
            //}
            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Update bones");

            if (!UseNewAnimationSystem)
            {
                UpdateBones(distance);
            }

            MyRenderProxy.GetRenderProfiler().StartNextBlock("UpdateTransformations");

            AnimationController.UpdateTransformations();

            MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            ProfilerShort.End();
        }

        /// <summary>
        /// Try getting animation definition matching given subtype name.
        /// VRage TODO: dependency on MyDefinitionManager, do we really need it here?
        ///             backward compatibility is for modders?
        ///             move backward compatibility to MyDefinitionManager.TryGetAnimationDefinition? then we do not need this method
        ///             
        ///             marked as obsolete, needs to be resolved
        /// </summary>
        [Obsolete]
        protected bool TryGetAnimationDefinition(string animationSubtypeName, out MyAnimationDefinition animDefinition)
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

        /// <summary>
        /// Process all commands in the animation queue at once. 
        /// If any command is generated during flushing, it will be processed later.
        /// </summary>
        protected bool ProcessCommands()
        {
            if (m_commandQueue.Count > 0)
            {
                MyAnimationCommand command = m_commandQueue.Dequeue();
                ProcessCommand(ref command);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <param name="position">Position of the decal in the binding pose</param>
        protected void AddBoneDecal(uint decalId, int boneIndex)
        {
            List<uint> decals;
            bool found = m_boneDecals.TryGetValue(boneIndex, out decals);
            if (!found)
            {
                decals = new List<uint>(MAX_BONE_DECALS_COUNT);
                m_boneDecals.Add(boneIndex, decals);
            }

            if (decals.Count == decals.Capacity)
            {
                MyDecals.RemoveDecal(decals[0]);
                decals.RemoveAt(0);
            }

            decals.Add(decalId);
        }

        private void UpdateBoneDecals()
        {
            DecalBoneUpdates.Clear();
            foreach (var pair in m_boneDecals)
            {
                foreach (uint decalId in pair.Value)
                    DecalBoneUpdates.Add(new MyBoneDecalUpdate() { BoneID = pair.Key, DecalID = decalId });
            }
        }

        /// <summary>
        /// Process all commands in the animation queue at once. If any command is generated during flushing, it is processed as well.
        /// </summary>
        protected void FlushAnimationQueue()
        {
            while (m_commandQueue.Count > 0)
                ProcessCommands();
        }

        /// <summary>
        /// Process single animation command.
        /// </summary>
        void ProcessCommand(ref MyAnimationCommand command)
        {
            if (command.PlaybackCommand == MyPlaybackCommand.Play)
            {
                MyAnimationDefinition animDefinition;
                if (!TryGetAnimationDefinition(command.AnimationSubtypeName, out animDefinition))
                    return;

                string bonesArea = animDefinition.InfluenceArea;
                var frameOption = command.FrameOption;

                if (frameOption == MyFrameOption.Default)
                {
                    frameOption = animDefinition.Loop ? MyFrameOption.Loop : MyFrameOption.PlayOnce;
                }

                bool useFirstPersonVersion = false;

                OnAnimationPlay(animDefinition, command, ref bonesArea, ref frameOption, ref useFirstPersonVersion);

                //override bones area if required
                if (!string.IsNullOrEmpty(command.Area))
                    bonesArea = command.Area;

                if (bonesArea == null)
                    bonesArea = "";

                if (!command.KeepContinuingAnimations)
                    m_continuingAnimSets.Clear();

                if (UseNewAnimationSystem)
                {
                    // these commands are now completely ignored.
                    //var animationLayer = AnimationController.Controller.GetLayerByName(bonesArea);
                    //animationLayer.SetState(command.AnimationSubtypeName);
                }
                else
                {
                    PlayersPlay(bonesArea, animDefinition, useFirstPersonVersion, frameOption, command.BlendTime, command.TimeScale);
                }
            }
            else if (command.PlaybackCommand == MyPlaybackCommand.Stop)
            {
                string bonesArea = command.Area == null ? "" : command.Area;
                string[] boneAreas = bonesArea.Split(' ');

                if (UseNewAnimationSystem)
                {

                }
                else
                {
                    foreach (var boneArea in boneAreas)
                    {
                        PlayerStop(boneArea, command.BlendTime);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Fail("Unknown playback command");
            }
        }

        /// <summary>
        /// Enqueue animation command. Parameter sync is used in child classes.
        /// </summary>
        public virtual void AddCommand(MyAnimationCommand command, bool sync = false)
        {
            //if (command.PlaybackCommand == MyPlaybackCommand.Play && command.BlendOption == MyBlendOption.Immediate)
            //{
            //    m_commandQueue.Clear();
            //}

            m_commandQueue.Enqueue(command);
        }

        /// <summary>
        /// Virtual method called when animation is started, used in MyCharacter.
        /// </summary>
        protected virtual void OnAnimationPlay(MyAnimationDefinition animDefinition, MyAnimationCommand command, ref string bonesArea, ref MyFrameOption frameOption, ref bool useFirstPersonVersion)
        {
        }

        protected void UpdateRenderObject()
        {
            m_actualWorldAABB = BoundingBoxD.CreateInvalid();

            if (AnimationController.CharacterBones != null)
            for (int i = 1; i < Model.Bones.Length; i++)
            {
                Vector3D p1 = Vector3D.Transform(AnimationController.CharacterBones[i].Parent.AbsoluteTransform.Translation, WorldMatrix);
                Vector3D p2 = Vector3D.Transform(AnimationController.CharacterBones[i].AbsoluteTransform.Translation, WorldMatrix);

                m_actualWorldAABB.Include(ref p1);
                m_actualWorldAABB.Include(ref p2);
            }

            ContainmentType containmentType;
            m_aabb.Contains(ref m_actualWorldAABB, out containmentType);
            if (containmentType != ContainmentType.Contains)
            {
                m_actualWorldAABB.Inflate(0.5f);
                if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    MatrixD worldMatrix = WorldMatrix;
                    VRageRender.MyRenderProxy.UpdateRenderObject(Render.RenderObjectIDs[0], ref worldMatrix, false, m_actualWorldAABB);
                }
                m_aabb = m_actualWorldAABB;
            }
        }


        #endregion
    }
}
