using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRageMath;

namespace Sandbox.ModAPI.Interfaces
{
    /// <summary>
    /// Terminal block property definition
    /// </summary>
    public interface ITerminalProperty
    {
        /// <summary>
        /// Property Id (value name)
        /// </summary>
        string Id { get; }
        /// <summary>
        /// Property type (bool - <see cref="bool"/>, float - <see cref="float"/>, color - <see cref="VRageMath.Color"/>)
        /// </summary>
        string TypeName { get; }
    }

    /// <summary>
    /// Terminal block property access
    /// </summary>
    /// <typeparam name="TValue">Property type (<see cref="ITerminalProperty.TypeName"/>)</typeparam>
    public interface ITerminalProperty<TValue> : ITerminalProperty
    {
        /// <summary>
        /// Retrieve property value
        /// </summary>
        /// <param name="block">block reference</param>
        /// <returns>value of type <see cref="ITerminalProperty.TypeName"/></returns>
        TValue GetValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
        /// <summary>
        /// Set property value
        /// </summary>
        /// <param name="block">block reference</param>
        /// <param name="value">value of type <see cref="ITerminalProperty.TypeName"/></param>
        void SetValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block, TValue value);

        /// <summary>
        /// Default value of property (if value is not set, or value from block definition)
        /// </summary>
        /// <param name="block">block reference</param>
        /// <returns>value of type <see cref="ITerminalProperty.TypeName"/></returns>
        TValue GetDefaultValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
        /// <summary>
        /// Minimum value of property (value from block definition) - this function is obsolete, because it contains typo in name, use <see cref="GetMinimum(Sandbox.ModAPI.Ingame.IMyCubeBlock)"/>
        /// </summary>
        /// <param name="block">block reference</param>
        /// <returns>value of type <see cref="ITerminalProperty.TypeName"/></returns>
        [Obsolete("Use GetMinimum instead")]
        TValue GetMininum(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
        /// <summary>
        /// Minimum value of property (value from block definition)
        /// </summary>
        /// <param name="block">block reference</param>
        /// <returns>value of type <see cref="ITerminalProperty.TypeName"/></returns>
        TValue GetMinimum(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
        /// <summary>
        /// Maximum value of property (value from block definition)
        /// </summary>
        /// <param name="block">block reference</param>
        /// <returns>value of type <see cref="ITerminalProperty.TypeName"/></returns>
        TValue GetMaximum(VRage.Game.ModAPI.Ingame.IMyCubeBlock block);
    }

    /// <summary>
    /// Terminal block extension for property access
    /// </summary>
    public static class TerminalPropertyExtensions
    {
        /// <summary>
        /// Property type cast
        /// </summary>
        /// <typeparam name="TValue">value of type <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="property"><see cref="ITerminalProperty{TValue}"/> reference</param>
        /// <returns>reference to <see cref="ITerminalProperty{TValue}"/> value of specified type</returns>
        public static ITerminalProperty<TValue> As<TValue>(this ITerminalProperty property)
        {
            return property as ITerminalProperty<TValue>;
        }
        /// <summary>
        /// Property type cast
        /// </summary>
        /// <typeparam name="TValue">value of type <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="property"><see cref="ITerminalProperty{TValue}"/> reference</param>
        /// <returns>reference to <see cref="ITerminalProperty{TValue}"/> value of specified type</returns>
        public static ITerminalProperty<TValue> Cast<TValue>(this ITerminalProperty property)
        {
            var prop = property.As<TValue>();
            if (prop == null)
                throw new InvalidOperationException(String.Format("Property {0} is not of type {1}, correct type is {2}", property.Id, typeof(TValue).Name, property.TypeName));
            return prop;
        }

        /// <summary>
        /// Check property type
        /// </summary>
        /// <typeparam name="TValue">value of type <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="property"><see cref="ITerminalProperty{TValue}"/> reference</param>
        /// <returns>true if type matches</returns>
        public static bool Is<TValue>(this ITerminalProperty property)
        {
            return property is ITerminalProperty<TValue>;
        }
        /// <summary>
        /// Property type cast
        /// </summary>
        /// <param name="property"><see cref="ITerminalProperty{TValue}"/> reference</param>
        /// <returns>reference to <see cref="ITerminalProperty{TValue}"/> value of specified type (float)</returns>
        public static ITerminalProperty<float> AsFloat(this ITerminalProperty property)
        {
            return property.As<float>();
        }
        /// <summary>
        /// Property type cast
        /// </summary>
        /// <param name="property"><see cref="ITerminalProperty{TValue}"/> reference</param>
        /// <returns>reference to <see cref="ITerminalProperty{TValue}"/> value of specified type (Color)</returns>
        public static ITerminalProperty<Color> AsColor(this ITerminalProperty property)
        {
            return property.As<Color>();
        }
        /// <summary>
        /// Property type cast
        /// </summary>
        /// <param name="property"><see cref="ITerminalProperty{TValue}"/> reference</param>
        /// <returns>reference to <see cref="ITerminalProperty{TValue}"/> value of specified type (bool)</returns>
        public static ITerminalProperty<bool> AsBool(this ITerminalProperty property)
        {
            return property.As<bool>();
        }
        /// <summary>
        /// Returns value of specified property
        /// </summary>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as float</returns>
        public static float GetValueFloat(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetValue<float>(propertyId);
        }
        /// <summary>
        /// Set float value of property
        /// </summary>
        /// <typeparam name="T"><see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <param name="value">value to set</param>
        public static void SetValueFloat(this Ingame.IMyTerminalBlock block, string propertyId, float value)
        {
            block.SetValue<float>(propertyId, value);
        }
        /// <summary>
        /// Returns value of specified property
        /// </summary>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as bool</returns>
        public static bool GetValueBool(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetValue<bool>(propertyId);
        }
        /// <summary>
        /// Set bool value of property
        /// </summary>
        /// <typeparam name="T"><see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <param name="value">value to set</param>
        public static void SetValueBool(this Ingame.IMyTerminalBlock block, string propertyId, bool value)
        {
            block.SetValue<bool>(propertyId, value);
        }
        /// <summary>
        /// Returns value of specified property
        /// </summary>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as Color</returns>
        public static Color GetValueColor(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetValue<Color>(propertyId);
        }
        /// <summary>
        /// Set bool value of property
        /// </summary>
        /// <typeparam name="T"><see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <param name="value">value to set</param>
        public static void SetValueColor(this Ingame.IMyTerminalBlock block, string propertyId, Color value)
        {
            block.SetValue<Color>(propertyId, value);
        }
        /// <summary>
        /// Returns value of specified property as <see cref="ITerminalProperty.TypeName"/>
        /// </summary>
        /// <typeparam name="T">required value type of <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as <see cref="ITerminalProperty.TypeName"/></returns>
        public static T GetValue<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetValue(block);
        }
        /// <summary>
        /// Returns default value of specified property as <see cref="ITerminalProperty.TypeName"/>
        /// </summary>
        /// <typeparam name="T">required value type of <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as <see cref="ITerminalProperty.TypeName"/></returns>
        public static T GetDefaultValue<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetDefaultValue(block);
        }
        /// <summary>
        /// Returns minimum value of specified property as <see cref="ITerminalProperty.TypeName"/> - this call is obsolete due typo in name, use <see cref="GetMinimum{T}(Ingame.IMyTerminalBlock, string)"/>
        /// </summary>
        /// <typeparam name="T">required value type of <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as <see cref="ITerminalProperty.TypeName"/></returns>
        [Obsolete("Use GetMinimum instead")]
        public static T GetMininum<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetMinimum(block);
        }
        /// <summary>
        /// Returns minimum value of specified property as <see cref="ITerminalProperty.TypeName"/>
        /// </summary>
        /// <typeparam name="T">required value type of <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as <see cref="ITerminalProperty.TypeName"/></returns>
        public static T GetMinimum<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetMinimum(block);
        }
        /// <summary>
        /// Returns maximum value of specified property as <see cref="ITerminalProperty.TypeName"/>
        /// </summary>
        /// <typeparam name="T">required value type of <see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <returns>property value as <see cref="ITerminalProperty.TypeName"/></returns>
        public static T GetMaximum<T>(this Ingame.IMyTerminalBlock block, string propertyId)
        {
            return block.GetProperty(propertyId).Cast<T>().GetMaximum(block);
        }
        /// <summary>
        /// Set value of property with type of <see cref="ITerminalProperty.TypeName"/>
        /// </summary>
        /// <typeparam name="T"><see cref="ITerminalProperty.TypeName"/></typeparam>
        /// <param name="block">block reference</param>
        /// <param name="propertyId">property id (name)</param>
        /// <param name="value">value to set</param>
        public static void SetValue<T>(this Ingame.IMyTerminalBlock block, string propertyId, T value)
        {
            block.GetProperty(propertyId).Cast<T>().SetValue(block, value);
        }
    }
}
