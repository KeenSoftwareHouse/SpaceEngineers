using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using VRage;
using VRage.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;

namespace VRage.Components
{
    public abstract class MyRenderComponentBase : MyEntityComponentBase
    {
        public static readonly Vector3 OldRedToHSV = new Vector3(0, 0.0f, 0.05f);
        public static readonly Vector3 OldYellowToHSV = new Vector3(44 / 360f, -0.1f, 0.26f);
        public static readonly Vector3 OldBlueToHSV = new Vector3(207 / 360f, 0, 0);
        public static readonly Vector3 OldGreenToHSV = new Vector3(120 / 360f, -0.48f, -0.25f);
        public static readonly Vector3 OldBlackToHSV = new Vector3(0, -0.96f, -0.5f);
        public static readonly Vector3 OldWhiteToHSV = new Vector3(0, -0.95f, 0.4f);
        public static readonly Vector3 OldGrayToHSV = new Vector3(0, -1f, 0f);

        protected Vector3 m_colorMaskHsv = OldGrayToHSV;
        protected bool m_enableColorMaskHsv = false;

        protected Color m_diffuseColor = Color.White;  //diffuse color multiplier

        /// <summary>
        /// Used by game to store model here. In game this is always of type MyModel.
        /// Implementation should only store and return passed object.
        /// </summary>
        public abstract object ModelStorage { get; set; }

        public bool EnableColorMaskHsv
        {
            get { return m_enableColorMaskHsv; /*|| MyFakes.ENABLE_COLOR_MASK_FOR_EVERYTHING;*/ }
            set
            {
                m_enableColorMaskHsv = value;
                if (EnableColorMaskHsv)
                {
                    UpdateRenderEntity(m_colorMaskHsv);
                    Container.Entity.EnableColorMaskForSubparts(value);
                }
            }
        }

        public Vector3 ColorMaskHsv
        {
            get { return m_colorMaskHsv; }
            set
            {
                m_colorMaskHsv = value;
                if (EnableColorMaskHsv)
                {
                    UpdateRenderEntity(m_colorMaskHsv);
                    Container.Entity.SetColorMaskForSubparts(value);
                }
            }
        }

        public MyPersistentEntityFlags2 PersistentFlags { get; set; }

        public uint[] RenderObjectIDs
        {
            get { return m_renderObjectIDs; }
        }

        public abstract void SetRenderObjectID(int index, uint ID);

        public int GetRenderObjectID()
        {
            if (m_renderObjectIDs.Length > 0)
            {
                return (int)m_renderObjectIDs[0];
            }

            return -1;
        }

        public virtual void RemoveRenderObjects()
        {
            for(int i = 0; i < m_renderObjectIDs.Length; i++)
                ReleaseRenderObjectID(i);
        }

        public abstract void ReleaseRenderObjectID(int index);
    
        public void ResizeRenderObjectArray(int newSize)
        {
            var oldSize = m_renderObjectIDs.Length;
            Array.Resize(ref m_renderObjectIDs, newSize);
            for (int i = oldSize; i < newSize; i++)
            {
                m_renderObjectIDs[i] = MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }

        public bool IsRenderObjectAssigned(int index)
        {
            return m_renderObjectIDs[index] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
        }

        protected uint[] m_renderObjectIDs = new uint[] { VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED };

        public virtual void InvalidateRenderObjects(bool sortIntoCullobjects = false)
        {
            var m = Container.Get<MyPositionComponentBase>().WorldMatrix;
            if ((Container.Entity.Visible || Container.Entity.CastShadows) && Container.Entity.InScene && Container.Entity.InvalidateOnMove)
            {
                foreach (uint renderObjectID in m_renderObjectIDs)
                {
                    VRageRender.MyRenderProxy.UpdateRenderObject(renderObjectID, ref m, sortIntoCullobjects);
                }
            }
        }

        virtual public void UpdateRenderEntity(Vector3 colorMaskHSV)
        {
            m_colorMaskHsv = colorMaskHSV;
            MyRenderProxy.UpdateRenderEntity(m_renderObjectIDs[0], m_diffuseColor, m_colorMaskHsv);
        }
      
        public bool Visible
        {
            get
            {
                return (Container.Entity.Flags & EntityFlags.Visible) != 0;
            }

            set
            {
                System.Diagnostics.Debug.Assert(!Container.Entity.Closed, "Cannot change visibility, entity is closed");

                EntityFlags oldValue = Container.Entity.Flags;

                if (value)
                {
                    Container.Entity.Flags = Container.Entity.Flags | EntityFlags.Visible;
                }
                else
                {
                    Container.Entity.Flags = Container.Entity.Flags & (~EntityFlags.Visible);
                }

                if (oldValue != Container.Entity.Flags)
                {
                    UpdateRenderObjectVisibilityIncludingChildren(value);
                }
            }
        }

        protected virtual bool CanBeAddedToRender()
        {
            return true;
        }

        public void UpdateRenderObject(bool visible)
        {
            if (!Container.Entity.InScene && visible)
                return;

            if (visible)
            {
                MyHierarchyComponentBase hierarchyComponent = Container.Get<MyHierarchyComponentBase>();
                if (Visible && (hierarchyComponent.Parent == null || hierarchyComponent.Parent.Container.Entity.Visible))
                {
                    if (CanBeAddedToRender())
                    {
                        if (!IsRenderObjectAssigned(0))
                        {
                            AddRenderObjects();
                        }
                        else
                        {
                            UpdateRenderObjectVisibility(visible);
                        }
                    }
                }
            }
            else
            {
                if (m_renderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    UpdateRenderObjectVisibility(visible);
                }
                RemoveRenderObjects();
            }

            MyHierarchyComponentBase hierarchy = Container.Get<MyHierarchyComponentBase>();
            foreach (var child in hierarchy.Children)
            {
                MyRenderComponentBase renderComponent = null;
                if (child.Container.TryGet(out renderComponent))
                {
                    renderComponent.UpdateRenderObject(visible);
                }
            }
        }

        protected virtual void UpdateRenderObjectVisibility(bool visible)
        {
            foreach (uint id in m_renderObjectIDs)
            {
                VRageRender.MyRenderProxy.UpdateRenderObjectVisibility(id, visible, Container.Entity.NearFlag);
            }
        }

        private void UpdateRenderObjectVisibilityIncludingChildren(bool visible)
        {
            UpdateRenderObjectVisibility(visible);

            MyHierarchyComponentBase hierarchy = Container.Get<MyHierarchyComponentBase>();
            foreach (var child in hierarchy.Children)
            {
                MyRenderComponentBase renderComponent = null;
                if (child.Container.Entity.InScene && child.Container.TryGet(out renderComponent))
                {
                    renderComponent.UpdateRenderObjectVisibilityIncludingChildren(visible);
                }
            }    
        }

        public Color GetDiffuseColor() { return m_diffuseColor; }

        protected void SetDiffuseColor(Color vctColor)
        {
            m_diffuseColor = vctColor;
            VRageRender.MyRenderProxy.UpdateRenderEntity(m_renderObjectIDs[0], m_diffuseColor, m_colorMaskHsv);
        }

        public virtual bool NearFlag
        {
            get
            {
                return (Container.Entity.Flags & EntityFlags.Near) != 0;
            }
            set
            {
                bool hasChanged = value != NearFlag;

                if (value)
                    Container.Entity.Flags |= EntityFlags.Near;
                else
                    Container.Entity.Flags &= ~EntityFlags.Near;


                if (hasChanged)
                {
                    //UpdateRenderObject(false); // Remove (because we need to remove from one group)
                    //UpdateRenderObject(true); // And insert again (...and insert into another)
                    VRageRender.MyRenderProxy.UpdateRenderObjectVisibility(m_renderObjectIDs[0], Visible, NearFlag);
                }

                MyHierarchyComponentBase hierarchy = Container.Get<MyHierarchyComponentBase>();
                foreach (var child in hierarchy.Children)
                {
                    MyRenderComponentBase renderComponent = null;
                    if (child.Container.Entity.InScene && child.Container.TryGet(out renderComponent))
                    {
                        renderComponent.NearFlag = value;
                    }
                }             
            }
        }
     
        public bool NeedsDrawFromParent
        {
            get
            {
                return ((Container.Entity.Flags & EntityFlags.NeedsDrawFromParent) != 0);
            }
            set
            {
                bool hasChanged = value != NeedsDrawFromParent;

                if (hasChanged)
                {
                    Container.Entity.Flags &= ~EntityFlags.NeedsDrawFromParent;

                    if (value)
                        Container.Entity.Flags |= EntityFlags.NeedsDrawFromParent;
                }
            }
        }

        public bool CastShadows
        {
            get
            {
                return (PersistentFlags & MyPersistentEntityFlags2.CastShadows) != 0;
            }
            set
            {
                if (value)
                {
                    PersistentFlags |= MyPersistentEntityFlags2.CastShadows;
                }
                else
                {
                    PersistentFlags &= ~MyPersistentEntityFlags2.CastShadows;
                }
            }
        }

        public bool NeedsResolveCastShadow
        {
            get
            {
                return (Container.Entity.Flags & EntityFlags.NeedsResolveCastShadow) != 0;
            }
            set
            {
                if (value)
                {
                    Container.Entity.Flags |= EntityFlags.NeedsResolveCastShadow;
                }
                else
                {
                    Container.Entity.Flags &= ~EntityFlags.NeedsResolveCastShadow;
                }
            }
        }

        public bool FastCastShadowResolve
        {
            get
            {
                return (Container.Entity.Flags & EntityFlags.FastCastShadowResolve) != 0;
            }
            set
            {
                if (value)
                {
                    Container.Entity.Flags |= EntityFlags.FastCastShadowResolve;
                }
                else
                {
                    Container.Entity.Flags &= ~EntityFlags.FastCastShadowResolve;
                }
            }
        }

        public bool SkipIfTooSmall
        {
            get
            {
                return (Container.Entity.Flags & EntityFlags.SkipIfTooSmall) != 0;
            }
            set
            {
                if (value)
                {
                    Container.Entity.Flags |= EntityFlags.SkipIfTooSmall;
                }
                else
                {
                    Container.Entity.Flags &= ~EntityFlags.SkipIfTooSmall;
                }
            }
        }

        public bool ShadowBoxLod
        {
            get
            {
                return (Container.Entity.Flags & EntityFlags.ShadowBoxLod) != 0;
            }
            set
            {
                if (value)
                {
                    Container.Entity.Flags |= EntityFlags.ShadowBoxLod;
                }
                else
                {
                    Container.Entity.Flags &= ~EntityFlags.ShadowBoxLod;
                }
            }
        }

        public float Transparency;

        public virtual VRageRender.RenderFlags GetRenderFlags()
        {
            VRageRender.RenderFlags renderFlags = 0;
            if (NearFlag)
                renderFlags |= VRageRender.RenderFlags.Near;
            if (CastShadows)
                renderFlags |= VRageRender.RenderFlags.CastShadows;
            if (Visible)
                renderFlags |= VRageRender.RenderFlags.Visible;
            if (NeedsResolveCastShadow)
                renderFlags |= VRageRender.RenderFlags.NeedsResolveCastShadow;
            if (FastCastShadowResolve)
                renderFlags |= VRageRender.RenderFlags.FastCastShadowResolve;
            if (SkipIfTooSmall)
                renderFlags |= VRageRender.RenderFlags.SkipIfTooSmall;
            if (ShadowBoxLod)
                renderFlags |= VRageRender.RenderFlags.ShadowLodBox;
            return renderFlags;
        }

        public virtual VRageRender.CullingOptions GetRenderCullingOptions()
        {
            VRageRender.CullingOptions cullingOptions = VRageRender.CullingOptions.Default;
            return cullingOptions;
        }

        public abstract void AddRenderObjects();

        public abstract void Draw();
        public abstract bool IsVisible();
        
        public virtual bool NeedsDraw
        {
            get
            {
                return ((Container.Entity.Flags & EntityFlags.NeedsDraw) != 0);
            }
            set
            {
                bool hasChanged = value != NeedsDraw;

                if (hasChanged)
                {
                    Container.Entity.Flags &= ~EntityFlags.NeedsDraw;

                    if (value)
                        Container.Entity.Flags |= EntityFlags.NeedsDraw;                  
                }
            }
        }
    }
}
