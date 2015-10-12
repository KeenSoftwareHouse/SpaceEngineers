using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRageMath;

namespace Sandbox.ModAPI.Interfaces
{
    public interface ITerminalProperty
    {
        string Id { get; }
        string TypeName { get; }
    }

    public interface ITerminalProperty<TValue> : ITerminalProperty
    {
        TValue GetValue(Sandbox.ModAPI.Ingame.IMyCubeBlock block);
        void SetValue(Sandbox.ModAPI.Ingame.IMyCubeBlock block, TValue value);

        TValue GetDefaultValue(Sandbox.ModAPI.Ingame.IMyCubeBlock block);
        TValue GetMininum(Sandbox.ModAPI.Ingame.IMyCubeBlock block);
        TValue GetMaximum(Sandbox.ModAPI.Ingame.IMyCubeBlock block);
    }

    public static class TerminalPropertyExtensions
    {
        public static ITerminalProperty<TValue> As<TValue>(this ITerminalProperty property)
        {
            return property as ITerminalProperty<TValue>;
        }

        public static ITerminalProperty<TValue> Cast<TValue>(this ITerminalProperty property)
        {
            var prop = property.As<TValue>();
            if (prop == null)
                throw new InvalidOperationException(String.Format("Property {0} is not of type {1}, correct type is {2}", property.Id, typeof(TValue).Name, property.TypeName));
            return prop;
        }

        public static bool Is<TValue>(this ITerminalProperty property)
        {
            return property is ITerminalProperty<TValue>;
        }

        public static ITerminalProperty<float> AsFloat(this ITerminalProperty property)
        {
            return property.As<float>();
        }

        public static ITerminalProperty<Color> AsColor(this ITerminalProperty property)
        {
            return property.As<Color>();
        }

        public static ITerminalProperty<bool> AsBool(this ITerminalProperty property)
        {
            return property.As<bool>();
        }

        public static float GetValueFloat(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetValue<float>(propertyId);
        }

        public static void SetValueFloat(this Ingame.IMyTerminalBlock block, string propertyId, float value)
        {
            block.SetValue<float>(propertyId, value);
        }

        public static bool GetValueBool(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetValue<bool>(propertyId);
        }

        public static void SetValueBool(this Ingame.IMyTerminalBlock block, string propertyId, bool value)
        {
            block.SetValue<bool>(propertyId, value);
        }

        public static void SetValue<T>(this Ingame.IMyTerminalBlock block, string propertyId, T value)
        {
            block.GetProperty(propertyId).Cast<T>().SetValue(block, value);
        }

        public static T GetValue<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetValue(block);
        }

        public static T GetDefaultValue<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetDefaultValue(block);
        }

        public static T GetMininum<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetMininum(block);
        }

        public static T GetMaximum<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetMaximum(block);
        }
    }
}
