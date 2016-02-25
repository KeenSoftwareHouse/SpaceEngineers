using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX.Direct3D11;

namespace VRageRender
{
    public enum MyVertexInputComponentType
    {
        POSITION_PACKED,
        POSITION2,
        POSITION3,
        POSITION4_H,
        VOXEL_POSITION_MAT,
        VOXEL_NORMAL,

        CUBE_INSTANCE,
        GENERIC_INSTANCE,

        BLEND_INDICES,
        BLEND_WEIGHTS,

        COLOR4,
        
        TEXCOORD0_H,
        TEXCOORD0,

        NORMAL,
        TANGENT_SIGN_OF_BITANGENT,

        CUSTOM_HALF4_0,
        CUSTOM_HALF4_1,
        CUSTOM_HALF4_2,

        CUSTOM_UNORM4_0,
        CUSTOM_UNORM4_1
    }

    enum MyVertexInputComponentFreq
    {
        PER_VERTEX,
        PER_INSTANCE
    }

    struct MyVertexInputComponent
    {
        internal MyVertexInputComponentType Type;
        internal int Slot;
        internal MyVertexInputComponentFreq Freq;

        internal MyVertexInputComponent(MyVertexInputComponentType type)
        {
            Type = type;
            Slot = 0;
            Freq = MyVertexInputComponentFreq.PER_VERTEX;
        }

        internal MyVertexInputComponent(MyVertexInputComponentType type, MyVertexInputComponentFreq freq)
        {
            Type = type;
            Slot = 0;
            Freq = freq;
        }

        internal MyVertexInputComponent(MyVertexInputComponentType type, int slot)
        {
            Type = type;
            Slot = slot;
            Freq = MyVertexInputComponentFreq.PER_VERTEX;
        }

        internal MyVertexInputComponent(MyVertexInputComponentType type, int slot, MyVertexInputComponentFreq freq)
        {
            Type = type;
            Slot = slot;
            Freq = freq;
        }

        public override string ToString()
        {
            return String.Format("<{0}, {1}, {2}>", Type, Slot, Freq);
        }

        public int CompareTo(MyVertexInputComponent item)
        {
            if (Type == item.Type)
            {
                if (Slot == item.Slot)
                {
                    return Freq - item.Freq;
                }
                return Slot - item.Slot;
            }
            else return Type - item.Type;
        }
    }

    struct VertexLayoutId
    {
        internal int Index;

        public static bool operator ==(VertexLayoutId x, VertexLayoutId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(VertexLayoutId x, VertexLayoutId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly VertexLayoutId NULL = new VertexLayoutId { Index = -1 };

        public override int GetHashCode()
        {
            return Index;
        }

        internal InputElement[] Elements { get { return MyVertexLayouts.GetElements(this); } }
        internal MyVertexLayoutInfo Info { get { return MyVertexLayouts.Layouts.Data[Index]; } }
        internal bool HasBonesInfo { get { return MyVertexLayouts.Layouts.Data[Index].HasBonesInfo; } }
    }

    struct MyVertexLayoutInfo
    {
        internal MyVertexInputComponent[] Components;
        internal InputElement[] Elements;
        internal string SourceDeclarations;
        internal string SourceDataMove;
        internal bool HasBonesInfo;
    }

    static class MyVertexLayouts
    {
        static Dictionary<int, VertexLayoutId> HashIndex = new Dictionary<int, VertexLayoutId>();
        internal static MyFreelist<MyVertexLayoutInfo> Layouts = new MyFreelist<MyVertexLayoutInfo>(64);

        internal static VertexLayoutId Empty;

        internal static InputElement[] GetElements(VertexLayoutId id)
        {
            return Layouts.Data[id.Index].Elements;
        }

        static MyVertexLayouts()
        {
            var id = new VertexLayoutId { Index = Layouts.Allocate() };
            HashIndex[0] = id;
            Layouts.Data[id.Index] = new MyVertexLayoutInfo 
            { 
                Elements = new InputElement[0], 
                SourceDeclarations = "struct __VertexInput { \n \n \n };",
                SourceDataMove = ""
            };

            Empty = id;
        }

        internal static void Init()
        {
        }

        internal static VertexLayoutId GetLayout(params MyVertexInputComponentType[] components)
        {
            return GetLayout(components.Select(x => new MyVertexInputComponent(x)).ToArray());
        }

        internal static VertexLayoutId GetLayout(VertexLayoutId a, VertexLayoutId b)
        {
            return GetLayout(a.Info.Components.Concat(b.Info.Components).ToArray());
        }

        internal static VertexLayoutId GetLayout(params MyVertexInputComponent [] components)
        {
            if(components == null || components.Length == 0)
            {
                return Empty;
            }

            var hash = 0;
            foreach(var c in components)
            {
                MyHashHelper.Combine(ref hash, c.GetHashCode());
            }

            if(HashIndex.ContainsKey(hash))
            {
                return HashIndex[hash];
            }

            var id = new VertexLayoutId { Index = Layouts.Allocate() };
            HashIndex[hash] = id;

            var declarationBuilder = new StringBuilder();
            var sourceBuilder = new StringBuilder();
            var elementsList = new List<InputElement>();
            var semanticDict = new Dictionary<string, int>();

            foreach (var component in components)
            {
                MyVertexInputLayout.m_mapComponent[component.Type].AddComponent(component, elementsList, semanticDict, declarationBuilder, sourceBuilder);
            }

            Layouts.Data[id.Index] = new MyVertexLayoutInfo {
                Components = components,
                Elements = elementsList.ToArray(),
                SourceDataMove = sourceBuilder.ToString(),
                SourceDeclarations = new StringBuilder().AppendFormat("struct __VertexInput {{ \n {0} \n }};", declarationBuilder.ToString()).ToString(),
                HasBonesInfo = components.Any(x => x.Type == MyVertexInputComponentType.BLEND_INDICES)
            };

            return id;
        }
    }

    // kind of proxy to cached data
    partial class MyVertexInputLayout
    {
        // input
        MyVertexInputComponent [] m_components = new MyVertexInputComponent[0];
        int m_hash;
        int m_id;

        internal InputElement[] m_elements;
        internal string m_declarationsSrc;
        internal string m_transferSrc;

        internal int Hash { get { return m_hash; } }
        internal int ID { get { return m_id; } }

        static Dictionary<int, MyVertexInputLayout> m_cached = new Dictionary<int, MyVertexInputLayout>();

        static MyVertexInputLayout()
        {
            var empty = new MyVertexInputLayout();
            empty.Build();
            m_cached[0] = empty;

            InitComponentsMap();
        }

        private MyVertexInputLayout()
        {

        }

        internal static MyVertexInputLayout Empty { get { return m_cached[0]; } }

        internal MyVertexInputLayout Append(MyVertexInputLayout other)
        {
            var current = this;
            foreach(var component in other.m_components)
            {
                current = current.Append(component);
            }
            return current;
        }

        internal MyVertexInputLayout Append(MyVertexInputComponent component)
        {
            return Append(component.Type, component.Slot, component.Freq);
        }

        internal MyVertexInputLayout Append(MyVertexInputComponentType type, int slot = 0, MyVertexInputComponentFreq freq = MyVertexInputComponentFreq.PER_VERTEX)
        {
            MyVertexInputComponent component = new MyVertexInputComponent();
            component.Type = type;
            component.Slot = slot;
            component.Freq = freq;

            int nextHash = MyHashHelper.Combine(m_hash, component.GetHashCode());

            MyVertexInputLayout next;
            if(m_cached.TryGetValue(nextHash, out next))
            {
                return next;
            }

            next = new MyVertexInputLayout();
            next.m_hash = nextHash;
            next.m_id = m_cached.Count;
            next.m_components = m_components.Concat(component.Yield()).ToArray();
            next.Build();

            m_cached[nextHash] = next;
            return next;
        }

        internal static string DeclarationsSrc(int hash)
        {
            MyVertexInputLayout cached;
            if (m_cached.TryGetValue(hash, out cached))
            {
                return cached.m_declarationsSrc;
            }
            return null;
        }

        internal static string TransferSrc(int hash)
        {
            MyVertexInputLayout cached;
            if (m_cached.TryGetValue(hash, out cached))
            {
                return cached.m_transferSrc;
            }
            return null;
        }

        internal static InputLayout CreateLayout(MyVertexInputLayout layout, byte[] bytecode)
        {
            if(layout.m_elements.Length > 0)
            {
                return new InputLayout(MyRender11.Device, bytecode, layout.m_elements);
            }
            return null;
        }

        private void Build()
        {
            var declarationBuilder = new StringBuilder();
            var sourceBuilder = new StringBuilder();
            var elementsList = new List<InputElement>();
            var semanticDict = new Dictionary<string, int>();

            foreach (var component in m_components)
            {
                m_mapComponent[component.Type].AddComponent(component, elementsList, semanticDict, declarationBuilder, sourceBuilder);
            }

            m_declarationsSrc = new StringBuilder().AppendFormat("struct __VertexInput {{ \n {0} \n }};", declarationBuilder.ToString()).ToString();
            m_transferSrc = sourceBuilder.ToString();
            m_elements = elementsList.ToArray();
        }
    }
}
