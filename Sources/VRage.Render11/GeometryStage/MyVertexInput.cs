using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX.Direct3D11;
using System.Diagnostics;
using SharpDX.Direct3D;

namespace VRageRender
{
    public enum MyVertexInputComponentType
    {
        POSITION_PACKED,
        POSITION2,
        POSITION3,
        POSITION4,
        POSITION4_H,
        VOXEL_POSITION_MAT,
        VOXEL_NORMAL,

        CUBE_INSTANCE,
        GENERIC_INSTANCE,
        SIMPLE_INSTANCE,
        SIMPLE_INSTANCE_COLORING,

        BLEND_INDICES,
        BLEND_WEIGHTS,

        COLOR4,
        
        TEXCOORD0_H,
        TEXCOORD0,
        TEXINDICES,

        NORMAL,
        TANGENT_SIGN_OF_BITANGENT,

        CUSTOM_HALF4_0,
        CUSTOM_HALF4_1,
        CUSTOM_HALF4_2,

        CUSTOM_UNORM4_0,
        CUSTOM_UNORM4_1,

        CUSTOM4_0
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
        internal ShaderMacro[] Macros;
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
                Macros = new ShaderMacro[0],
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

        internal static VertexLayoutId GetLayout(VertexLayoutId firstLayout, VertexLayoutId secondLayout)
        {
            VertexLayoutId combinedLayout = VertexLayoutId.NULL;
            List<MyVertexInputComponent> firstComponents = new List<MyVertexInputComponent>(firstLayout.Info.Components);
            MyVertexInputComponent[] secondComponents = secondLayout.Info.Components;
           
            firstComponents.AddArray(secondComponents);

            Debug.Assert(firstComponents.Count == firstComponents.Capacity);
            firstComponents.Capacity = firstComponents.Count;

            combinedLayout = GetLayout(firstComponents.GetInternalArray());
            return combinedLayout;
        }

        internal static VertexLayoutId GetLayout(params MyVertexInputComponent[] components)
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
            var semanticDict = new Dictionary<string, int>();

            // Might save some allocations when each AddComponent only adds one element as then we can use GetInternalArray and Capacity set does nothing
            var elementsList = new List<InputElement>(components.Length);

            foreach (var component in components)
            {
                MyVertexInputLayout.MapComponent[component.Type].AddComponent(component, elementsList, semanticDict, declarationBuilder, sourceBuilder);
            }

            elementsList.Capacity = elementsList.Count;
            Debug.Assert(elementsList.Count == elementsList.Capacity);

            Layouts.Data[id.Index] = new MyVertexLayoutInfo {
                Components = components,
                Elements = elementsList.GetInternalArray(),
                Macros = MyComponent.GetComponentMacros(declarationBuilder.ToString(), sourceBuilder.ToString(), components),
                HasBonesInfo = components.Any(x => x.Type == MyVertexInputComponentType.BLEND_INDICES)
            };

            return id;
        }
    }

    // kind of proxy to cached data
    internal partial class MyVertexInputLayout
    {
        // input
        MyVertexInputComponent [] m_components = new MyVertexInputComponent[0];
        int m_hash;
        int m_id;

        private InputElement[] m_elements;
        private ShaderMacro[] m_macros;

        static readonly Dictionary<int, MyVertexInputLayout> m_cached = new Dictionary<int, MyVertexInputLayout>();

        static MyVertexInputLayout()
        {
            var empty = new MyVertexInputLayout();
            empty.Build();
            m_cached[0] = empty;

            InitComponentsMap();
        }

        internal static MyVertexInputLayout Empty { get { return m_cached[0]; } }

        private MyVertexInputLayout Append(MyVertexInputComponent component)
        {
            return Append(component.Type, component.Slot, component.Freq);
        }

        internal MyVertexInputLayout Append(MyVertexInputComponentType type, int slot = 0, MyVertexInputComponentFreq freq = MyVertexInputComponentFreq.PER_VERTEX)
        {
            MyVertexInputComponent component = new MyVertexInputComponent
            {
                Type = type,
                Slot = slot,
                Freq = freq
            };

            int nextHash = MyHashHelper.Combine(m_hash, component.GetHashCode());

            MyVertexInputLayout next;
            if(m_cached.TryGetValue(nextHash, out next))
            {
                return next;
            }

            next = new MyVertexInputLayout
            {
                m_hash = nextHash,
                m_id = m_cached.Count,
                m_components = m_components.Concat(component.Yield_()).ToArray()
            };
            next.Build();

            m_cached[nextHash] = next;
            return next;
        }

        private void Build()
        {
            var declarationBuilder = new StringBuilder();
            var sourceBuilder = new StringBuilder();
            var elementsList = new List<InputElement>();
            var semanticDict = new Dictionary<string, int>();

            foreach (var component in m_components)
            {
                MapComponent[component.Type].AddComponent(component, elementsList, semanticDict, declarationBuilder, sourceBuilder);
            }

            m_elements = elementsList.ToArray();
            m_macros = MyComponent.GetComponentMacros(declarationBuilder.ToString(), sourceBuilder.ToString(), m_components);
        }
    }
}
