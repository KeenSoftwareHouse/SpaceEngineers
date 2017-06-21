using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Game.ObjectBuilders
{
    public interface IMyObjectBuilder_GunObject<out T> where T : MyObjectBuilder_DeviceBase
    {
        MyObjectBuilder_DeviceBase DeviceBase { get; set; }
    }

    public static class GunObjectBuilderExtensions
    {
        public static void InitializeDeviceBase<T>(this IMyObjectBuilder_GunObject<T> gunObjectBuilder, MyObjectBuilder_DeviceBase newBuilder)
            where T : MyObjectBuilder_DeviceBase
        {
            Debug.Assert(newBuilder.TypeId == typeof(T));
            if (newBuilder.TypeId != typeof(T))
            {
                return;
            }

            gunObjectBuilder.DeviceBase = newBuilder;
        }

        public static T GetDevice<T>(this IMyObjectBuilder_GunObject<T> gunObjectBuilder) where T : MyObjectBuilder_DeviceBase
        {
            return gunObjectBuilder.DeviceBase as T;
        }
    }
}
