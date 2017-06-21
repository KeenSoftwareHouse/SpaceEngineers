#if !XB1
using System;
using System.Reflection;
using System.Reflection.Emit;

#if !UNSHARPER_TMP

namespace VRage
{
    /// <summary>
    /// Helpers for constructor.
    /// </summary>
    /// <typeparam name="T">Type of object</typeparam>
    public static class ConstructorHelper<T>
    {
        /// <summary>
        /// Return delegate through which can be call in place constructor on already created object.
        /// </summary>
        /// <remarks>
        /// Call it like var ctorInPlace = (Action<T, P1, P2 ...>)ConstructorHelper<T>.CreateInPlaceConstructor(typeof(Action<T, P1, P2 ...>);
        /// ctorInPlace(obj, param1, param2 ...)
        /// </remarks>
        /// <param name="constructorType">Type of Action representing constructor, where fist parameter is hidden this value.</remarks></param>
        /// <returns></returns>
        public static Delegate CreateInPlaceConstructor(Type constructorType)
        {
            var type = typeof (T);
            var inplaceCtorMethod = constructorType.GetMethod("Invoke");

            var inplaceCtorParams = Array.ConvertAll(inplaceCtorMethod.GetParameters(), p => p.ParameterType);

            Type[] ctorParams;
            if (inplaceCtorParams.Length > 1)
            {
                ctorParams = new Type[inplaceCtorParams.Length - 1];
                Array.ConstrainedCopy(inplaceCtorParams, 1, ctorParams, 0, inplaceCtorParams.Length - 1);
            }
            else
            {
                ctorParams = Type.EmptyTypes;
            }

            ConstructorInfo constructor = type.GetConstructor(ctorParams);

            if (constructor == null)
            {
                throw new InvalidOperationException(string.Format("No matching constructor for object {0} was found!",
                                                                  type.Name));
            }

            var dm = new DynamicMethod(string.Format("Pool<T>__{0}", Guid.NewGuid().ToString().Replace("-", "")),
                                       typeof (void),
                                       inplaceCtorParams,
                                       typeof(ConstructorHelper<T>),
                                       false);

            var gen = dm.GetILGenerator();
            for (int paramIdx = 0; paramIdx < inplaceCtorParams.Length; paramIdx++)
            {
                if (paramIdx < 4)
                {
                    switch (paramIdx)
                    {
                        case 0:
                            gen.Emit(OpCodes.Ldarg_0);
                            break;
                        case 1:
                            gen.Emit(OpCodes.Ldarg_1);
                            break;
                        case 2:
                            gen.Emit(OpCodes.Ldarg_2);
                            break;
                        case 3:
                            gen.Emit(OpCodes.Ldarg_3);
                            break;
                    }
                }
                else
                {
                    gen.Emit(OpCodes.Ldarg_S, paramIdx);
                }
            }

            gen.Emit(OpCodes.Callvirt, constructor);
            gen.Emit(OpCodes.Ret);

            return dm.CreateDelegate(constructorType);
        }
    }
}

#endif
#endif // !XB1
