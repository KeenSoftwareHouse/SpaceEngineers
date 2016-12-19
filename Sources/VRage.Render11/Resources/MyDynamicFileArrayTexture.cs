using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Text;
using SharpDX.DXGI;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRageMath;
using VRageRender;
using Resource = SharpDX.Direct3D11.Resource;


namespace VRage.Render11.Resources
{
    internal interface IDynamicFileArrayTexture : ISrvBindable
    {
        int SlicesCount { get; }
        int GetOrAddSlice (string filepath);
        void Clear();
        void Update();
    }

    namespace Internal
    {
        class MyDynamicFileArrayTexture : IDynamicFileArrayTexture
        {
            IFileArrayTexture m_arrayTexture;
            int m_numRefs = 0;

            string m_name;
            MyFileTextureEnum m_type;

            bool m_dirtyFlag;
            Format m_formatBytePattern;
            byte[] m_errorBytePattern;
            List<string> m_filepaths = new List<string>();
            Dictionary<string, int> m_lookupTable = new Dictionary<string, int>();

            public int SlicesCount
            {
                get
                {
                    if (m_arrayTexture != null)
                        return m_arrayTexture.SubtexturesCount;
                    else
                        return 0;
                }
            }

            public string Name
            {
                get { return m_name; }
            }

            public Resource Resource
            {
                get
                {
                    if (m_arrayTexture != null)
                        return m_arrayTexture.Resource;
                    else
                        return null;
                }
            }

            public Vector3I Size3
            {
                get { return new Vector3I(Size.X, Size.Y, SlicesCount); }
            }

            public Vector2I Size
            {
                get
                {
                    if (m_arrayTexture != null)
                        return m_arrayTexture.Size;
                    else
                        return Vector2I.Zero;
                }
            }

            public ShaderResourceView Srv 
            { 
                get
                {
                    if (m_arrayTexture != null)
                        return m_arrayTexture.Srv;
                    else
                        return null;
                } 
            }

            public int GetOrAddSlice(string filepath)
            {
                filepath = MyResourceUtils.GetTextureFullPath(filepath);

                if (m_lookupTable.ContainsKey(filepath))
                    return m_lookupTable[filepath];

                int sliceNum = m_filepaths.Count;
                m_lookupTable.Add(filepath, sliceNum);
                m_filepaths.Add(filepath);
                m_dirtyFlag = true;
                return sliceNum;
            }

            public void Init(string name, MyFileTextureEnum type, byte[] errorBytePattern, Format formatBytePattern)
            {
                m_name = name;
                m_type = type;
                m_arrayTexture = null;
                m_dirtyFlag = false;
                m_numRefs = 1;
                m_errorBytePattern = errorBytePattern;
                m_formatBytePattern = formatBytePattern;
            }

            public void Update()
            {
                if (!m_dirtyFlag)
                    return;

                m_dirtyFlag = false;

                var previousArrayTexture = m_arrayTexture;
                if (m_filepaths.Count == 0)
                    m_arrayTexture = null;
                else
                    m_arrayTexture = MyManagers.FileArrayTextures.CreateFromFiles(m_name, m_filepaths.ToArray(), m_type, m_errorBytePattern, m_formatBytePattern, false, previousArrayTexture as MyFileArrayTexture);

                MyManagers.FileArrayTextures.DisposeTex(ref previousArrayTexture);
            }

            public int AddRef()
            {
                return ++m_numRefs;
            }

            public int Release()
            {
                MyRenderProxy.Assert(m_numRefs>0);
                if (--m_numRefs == 0)
                {
                    if (m_arrayTexture != null)
                        MyManagers.FileArrayTextures.DisposeTex(ref m_arrayTexture);

                    m_filepaths.Clear();
                    m_lookupTable.Clear();
                }
                return m_numRefs;
            }

            public void Clear()
            {
                m_lookupTable.Clear();
                m_filepaths.Clear();
                m_dirtyFlag = true;
            }

            public void SetDirtyFlag()
            {
                m_dirtyFlag = true;
            }
        }

    }

    class MyDynamicFileArrayTextureManager : IManager, IManagerUpdate, IManagerUnloadData
    {
        MyObjectsPool<MyDynamicFileArrayTexture> m_objectsPool = new MyObjectsPool<MyDynamicFileArrayTexture>(1);
        //HashSet<MyDynamicFileArrayTexture> m_texarraysAutodestroyed = new HashSet<MyDynamicFileArrayTexture>();

        public void ReloadAll()
        {
            foreach (MyDynamicFileArrayTexture tex in m_objectsPool.Active)
                tex.SetDirtyFlag();
        }
        
        public IDynamicFileArrayTexture CreateTexture(string name, MyFileTextureEnum type, byte[] bytePattern, Format bytePatternFormat)
        {
            MyDynamicFileArrayTexture tex;
            m_objectsPool.AllocateOrCreate(out tex);
            tex.Init(name, type, bytePattern, bytePatternFormat);

            //if (destroyOnUnloadSession)
            //    m_texarraysAutodestroyed.Add(tex);
            return tex;
        }

        public void DisposeTex(ref IDynamicFileArrayTexture inTex)
        {
            if (inTex == null)
                return;

            MyDynamicFileArrayTexture tex = (MyDynamicFileArrayTexture)inTex;
            //if (m_texarraysAutodestroyed.Contains(tex))
            //    m_texarraysAutodestroyed.Remove(tex);
            tex.Release();
            m_objectsPool.Deallocate(tex);
            inTex = null;
        }

        void IManagerUpdate.OnUpdate()
        {
            foreach(MyDynamicFileArrayTexture tex in m_objectsPool.Active)
                tex.Update();
        }

        void IManagerUnloadData.OnUnloadData()
        {
            //while (m_texarraysAutodestroyed.Count != 0)
            //{
            //    IDynamicFileArrayTexture tex = m_texarraysAutodestroyed.FirstElement();
            //    DisposeTex(ref tex);
            //}
            ReloadAll();
        }
    }
}
