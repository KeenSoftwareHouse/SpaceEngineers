using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Lights;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.TransparentGeometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

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
                var skeletonDescription = new MySkeletonBoneDescription[m_skinnedEntity.Bones.Count];

                for (int i = 0; i < m_skinnedEntity.Bones.Count; i++)
                {
                    skeletonDescription[i].Parent = -1;
                    if (m_skinnedEntity.Bones[i].Parent != null)
                    {
                        for (int j = 0; j < m_skinnedEntity.Bones.Count; j++)
                        {
                            if (m_skinnedEntity.Bones[j].Name == m_skinnedEntity.Bones[i].Parent.Name)
                            {
                                skeletonDescription[i].Parent = j;
                                break;
                            }
                        }
                    }

                    if (m_skinnedEntity.Bones[i].Parent != null)
                    {
                        Debug.Assert(skeletonDescription[i].Parent > -1, "Can't find bone with parent name!");
                    }

                    skeletonDescription[i].SkinTransform = m_skinnedEntity.Bones[i].SkinTransform;
                }

                VRageRender.MyRenderProxy.SetCharacterSkeleton(RenderObjectIDs[0], skeletonDescription, Model.Animations.Skeleton.ToArray());
            }
        }

        public override void Draw()
        {
            base.Draw();

            UpdateCharacterSkeleton();

            VRageRender.MyRenderProxy.SetCharacterTransforms(RenderObjectIDs[0], m_skinnedEntity.BoneRelativeTransforms);
        }

        #endregion   
    }
}
