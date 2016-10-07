using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using VRage.Serialization;

namespace VRage.Sync
{
#if !XB1 // !XB1_SYNC_NOREFLECTION
#if UNSHARPER
	public static class SyncHelpers
	{
        public static SyncType Compose(object obj, int firstId = 0)
		{
			System.Diagnostics.Debug.Assert(false);
			return null;
		}
	}

#else
    using Item = System.Tuple<SyncHelpers.Composer, MySerializeInfo>;

    public static class SyncHelpers
    {
        internal delegate SyncBase Composer(object instance, int id, MySerializeInfo serializeInfo);

        static Dictionary<Type, List<Item>> m_composers = new Dictionary<Type, List<Item>>();
        static FastResourceLock m_composersLock = new FastResourceLock();

        public static SyncType Compose(object obj, int firstId = 0)
        {
            List<SyncBase> result = new List<SyncBase>();
            Compose(obj, firstId, result);
            return new SyncType(result);
        }

        /// <summary>
        /// Takes objects and creates instances of Sync fields.
        /// </summary>
        public static void Compose(object obj, int startingId, List<SyncBase> resultList)
        {
            var type = obj.GetType();
            List<Item> composers;
            using (m_composersLock.AcquireExclusiveUsing())
            {
                if (!m_composers.TryGetValue(type, out composers))
                {
                    composers = CreateComposer(type);
                    m_composers.Add(type, composers);
                }
            }

            foreach (var comp in composers)
            {
                resultList.Add(comp.Item1(obj, startingId++, comp.Item2));
            }
        }

        static List<Item> CreateComposer(Type type)
        {
            List<Item> result = new List<Item>();
            foreach (var field in type.GetDataMembers(true, false, true, true, false, true, true, true).OfType<FieldInfo>())
            {
                if (typeof(SyncBase).IsAssignableFrom(field.FieldType))
                    result.Add(new Item(CreateFieldComposer(field), MyFactory.CreateInfo(field)));
            }
            return result;
        }

        static Composer CreateFieldComposer(FieldInfo field)
        {
            var constructor = field.FieldType.GetConstructor(new Type[] { typeof(int), typeof(MySerializeInfo) });
            Debug.Assert(constructor != null, "Sync fields must have constructor taking ISyncNotify and int");

            var module = Assembly.GetEntryAssembly().GetModules()[0];
            DynamicMethod composerMethod = new DynamicMethod("set" + field.Name, typeof(SyncBase), new Type[] { typeof(object), typeof(int), typeof(MySerializeInfo) }, module, true);
            ILGenerator gen = composerMethod.GetILGenerator();
            var local = gen.DeclareLocal(typeof(SyncBase));
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, field.DeclaringType);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Stloc, local);
            gen.Emit(OpCodes.Stfld, field);
            gen.Emit(OpCodes.Ldloc, local);
            gen.Emit(OpCodes.Ret);
            return (Composer)composerMethod.CreateDelegate(typeof(Composer));
        }
    }
#endif
#else // XB1
    public static class SyncHelpers
    {
    }
#endif // XB1
}
