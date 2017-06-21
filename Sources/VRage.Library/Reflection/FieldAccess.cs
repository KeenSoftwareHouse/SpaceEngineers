using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

#if !UNSHARPER_TMP

namespace System.Reflection
{
    public static class FieldAccess
    {
#if !XB1 // XB1_SYNC_NOREFLECTION
        public static Func<TType, TMember> CreateGetter<TType, TMember>(this FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(TMember), new Type[] { typeof(TType) }, true);
            ILGenerator gen = getterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                if (field.DeclaringType != typeof(TType))
                    gen.Emit(OpCodes.Castclass, field.DeclaringType);
                gen.Emit(OpCodes.Ldfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Func<TType, TMember>)getterMethod.CreateDelegate(typeof(Func<TType, TMember>));
        }

        public static Action<TType, TMember> CreateSetter<TType, TMember>(this FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[] { typeof(TType), typeof(TMember) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                if (typeof(TType).IsValueType)
                    gen.Emit(OpCodes.Ldarga, 0);
                else
                    gen.Emit(OpCodes.Ldarg_0);
                if (field.DeclaringType != typeof(TType))
                    gen.Emit(OpCodes.Castclass, field.DeclaringType);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Action<TType, TMember>)setterMethod.CreateDelegate(typeof(Action<TType, TMember>));
        }
#endif // !XB1

#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public static Getter<TType, TMember> CreateGetterRef<TType, TMember>(this FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod getterMethod = new DynamicMethod(methodName, null, new Type[] { typeof(TType).MakeByRefType(), typeof(TMember).MakeByRefType() }, true);
            ILGenerator gen = getterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                throw new NotImplementedException();
                //gen.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Ldarg_0);
                if (!typeof(TType).IsValueType)
                    gen.Emit(OpCodes.Ldind_Ref);
                if (field.DeclaringType != typeof(TType))
                    gen.Emit(OpCodes.Castclass, field.DeclaringType);
                gen.Emit(OpCodes.Ldfld, field);
                if (field.FieldType != typeof(TMember))
                    gen.Emit(OpCodes.Castclass, typeof(TMember));
                if (!typeof(TMember).IsValueType)
                    gen.Emit(OpCodes.Stind_Ref);
                else
                    gen.Emit(OpCodes.Stobj, typeof(TMember));
            }
            gen.Emit(OpCodes.Ret);
            return (Getter<TType, TMember>)getterMethod.CreateDelegate(typeof(Getter<TType, TMember>));
        }

        public static Setter<TType, TMember> CreateSetterRef<TType, TMember>(this FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[] { typeof(TType).MakeByRefType(), typeof(TMember).MakeByRefType() }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                if (!typeof(TType).IsValueType)
                    gen.Emit(OpCodes.Ldind_Ref);
                if (field.DeclaringType != typeof(TType))
                    gen.Emit(OpCodes.Castclass, field.DeclaringType);

                gen.Emit(OpCodes.Ldarg_1);
                if (!typeof(TMember).IsValueType)
                    gen.Emit(OpCodes.Ldind_Ref);
                else
                    gen.Emit(OpCodes.Ldobj, typeof(TMember));
                if (field.FieldType != typeof(TMember))
                    gen.Emit(OpCodes.Castclass, field.FieldType);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Setter<TType, TMember>)setterMethod.CreateDelegate(typeof(Setter<TType, TMember>));
        }
#endif // !XB1

#if !XB1 // !XB1_SYNC_NOREFLECTION
        public static Action<TMember> CreateSetter<TMember>(this FieldInfo field)
        {
            if (!field.IsStatic)
                throw new InvalidOperationException("Field must be static");

            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[] { typeof(TMember) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Stsfld, field);
            gen.Emit(OpCodes.Ret);
            return (Action<TMember>)setterMethod.CreateDelegate(typeof(Action<TMember>));
        }
#endif // !XB1
    }
}

#endif
