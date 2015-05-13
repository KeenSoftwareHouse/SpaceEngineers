using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharpDX.Direct3D9;
using VRage.Trace;
using VRageMath;

namespace VRageRender.RenderObjects
{
    class MyRenderSkeletonTexture: MyRenderObject
    {
        SharpDX.Vector4[] m_bones;
        Texture m_render, m_update;

        public MyRenderSkeletonTexture(uint id, string debugName, List<Vector3> bones)
            :base(id, debugName, 0)
        {
            CopyBones(bones);
        }

        public Texture SkeletonTexture
        {
            get { return m_render; }
        }

        private void CopyBones(List<Vector3> bones)
        {
            if (m_bones == null || m_bones.Length < bones.Count)
            {
                m_bones = new SharpDX.Vector4[bones.Count];
            }
            for (int i = 0; i < bones.Count; i++)
            {
                m_bones[i] = new SharpDX.Vector4(bones[i].X, bones[i].Y, bones[i].Z, 0);
            }
        }

        int GetSquareDimension()
        {
            int minSize = (int)Math.Ceiling(Math.Sqrt(m_bones.Length));
            return MathHelper.GetNearestBiggerPowerOfTwo(minSize);
        }

        public void Update(List<Vector3> bones)
        {
            CopyBones(bones);
            UpdateBones();
        }

        public override void LoadContent()
        {
            //int size = GetSquareDimension();
            int size = 256;
            m_render = new Texture(MyRender.GraphicsDevice, size, size, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            m_update = new Texture(MyRender.GraphicsDevice, size, size, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            UpdateBones();
        }

        public override void UnloadContent()
        {
            if (m_render != null)
            {
                m_render.Dispose();
                m_render = null;
            }
            if (m_update != null)
            {
                m_update.Dispose();
                m_update = null;
            }
        }

        void UpdateBones()
        {
            const float cubeSize = 2.5f;
            const float maxDeformation = cubeSize / 2;

            using (MyRenderStats.Generic.Measure("Skeleton rebuild", VRage.Stats.MyStatTypeEnum.CurrentValue))
            {
                var tex = m_update.LockRectangle(0, LockFlags.Discard);
                unsafe
                {
                    byte* data = (byte*)tex.DataPointer.ToPointer();
                    for (int i = 0; i < m_bones.Length; i++)
                    {
                        data[i * 4 + 3] = 0;
                        data[i * 4 + 2] = (byte)MathHelper.Clamp(m_bones[i].X / maxDeformation * 128 + 128, 0, 255); // Scale to 0 - 255 (128 = no deformation)
                        data[i * 4 + 1] = (byte)MathHelper.Clamp(m_bones[i].Y / maxDeformation * 128 + 128, 0, 255);
                        data[i * 4 + 0] = (byte)MathHelper.Clamp(m_bones[i].Z / maxDeformation * 128 + 128, 0, 255);
                    }
                }

                m_update.UnlockRectangle(0);
            }

            var tmp = m_update;
            m_update = m_render;
            m_render = tmp;
        }
    }
}
