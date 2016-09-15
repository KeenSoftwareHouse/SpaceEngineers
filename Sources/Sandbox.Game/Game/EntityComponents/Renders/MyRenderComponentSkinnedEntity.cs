using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Lights;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Sandbox.Game.Components
{
    class MyRenderComponentSkinnedEntity : MyRenderComponent
    {

        #region Skinned entity properties

        bool m_sentSkeletonMessage = false;
        protected MySkinnedEntity m_skinnedEntity = null;

        #endregion

        #region overrides

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_skinnedEntity = Container.Entity as MySkinnedEntity;
        }

        public override void AddRenderObjects()
        {
            if (m_model == null)
                return;

            if (IsRenderObjectAssigned(0))
                return;

            System.Diagnostics.Debug.Assert(m_model == null || !string.IsNullOrEmpty(m_model.AssetName));

            SetRenderObjectID(0, VRageRender.MyRenderProxy.CreateRenderCharacter
                (
                 Container.Entity.GetFriendlyName() + " " + Container.Entity.EntityId.ToString(),
                 m_model.AssetName,
                 Container.Entity.PositionComp.WorldMatrix,
                 m_diffuseColor,
                 ColorMaskHsv,
                 GetRenderFlags()
                ));
            m_sentSkeletonMessage = false;

            UpdateCharacterSkeleton();
        }


        private void UpdateCharacterSkeleton()
        {
            if (!m_sentSkeletonMessage)
            {
                m_sentSkeletonMessage = true;
                var characterBones = m_skinnedEntity.AnimationController.CharacterBones;
                var skeletonDescription = new MySkeletonBoneDescription[characterBones.Length];

                for (int i = 0; i < characterBones.Length; i++)
                {
                    if (characterBones[i].Parent == null)
                        skeletonDescription[i].Parent = -1;
                    else
                        skeletonDescription[i].Parent = characterBones[i].Parent.Index;

                    skeletonDescription[i].SkinTransform = characterBones[i].SkinTransform;
                }

                MyRenderProxy.SetCharacterSkeleton(RenderObjectIDs[0], skeletonDescription, Model.Animations.Skeleton.ToArray());
            }
        }

        public override void Draw()
        {
            base.Draw();

            UpdateCharacterSkeleton();

            MyRenderProxy.SetCharacterTransforms(RenderObjectIDs[0], m_skinnedEntity.BoneAbsoluteTransforms, m_skinnedEntity.DecalBoneUpdates);
        }

        #endregion   
    }
}
