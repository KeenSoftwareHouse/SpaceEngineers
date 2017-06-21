using VRage.Game.Components.Session;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.VisualScripting.Utils;
using VRage.Library.Utils;

namespace VRage.Game.VisualScripting
{
    [VisualScriptingEvent(new[] { true })]
    public delegate void SingleKeyTriggerEvent(string triggerName, long playerId);

    [VisualScriptingEvent(new[] { true })]
    public delegate void SingleKeyMissionEvent(string missionName);

    [VisualScriptingEvent]
    public delegate void Global_Void();

    public static class MyVisualScriptLogicProvider
    {
        public static SingleKeyMissionEvent MissionStarted;
        public static SingleKeyMissionEvent MissionFinished;

        public static void Init()
        {
            MyVisualScriptingProxy.WhitelistExtensions(typeof(MyVSCollectionExtensions));

            var listType = typeof(List<>);
            MyVisualScriptingProxy.WhitelistMethod(listType.GetMethod("Insert"), true);
            MyVisualScriptingProxy.WhitelistMethod(listType.GetMethod("RemoveAt"), true);
            MyVisualScriptingProxy.WhitelistMethod(listType.GetMethod("Clear"), true);
            MyVisualScriptingProxy.WhitelistMethod(listType.GetMethod("Add"), true);
            MyVisualScriptingProxy.WhitelistMethod(listType.GetMethod("Remove"), true);
            MyVisualScriptingProxy.WhitelistMethod(listType.GetMethod("Contains"), false);
            var stringType = typeof(string);
            MyVisualScriptingProxy.WhitelistMethod(stringType.GetMethod("Substring", new[] {typeof(int), typeof(int)}), true);
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember(true)]
        public static void StoreString(string key, string value)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                MySessionComponentScriptSharedStorage.Instance.Write(key, value);
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember(true)]
        public static void StoreBool(string key, bool value)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                MySessionComponentScriptSharedStorage.Instance.Write(key, value);
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember(true)]
        public static void StoreInteger(string key, int value)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                MySessionComponentScriptSharedStorage.Instance.Write(key, value);
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember(true)]
        public static void StoreLong(string key, long value)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                MySessionComponentScriptSharedStorage.Instance.Write(key, value);
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember(true)]
        public static void StoreFloat(string key, float value)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                MySessionComponentScriptSharedStorage.Instance.Write(key, value);
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember(true)]
        public static void StoreVector(string key, Vector3D value)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                MySessionComponentScriptSharedStorage.Instance.Write(key, value);
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember]
        public static string LoadString(string key)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                return MySessionComponentScriptSharedStorage.Instance.ReadString(key);

            return null;
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember]
        public static bool LoadBool(string key)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                return MySessionComponentScriptSharedStorage.Instance.ReadBool(key);

            return false;
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember]
        public static int LoadInteger(string key)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                return MySessionComponentScriptSharedStorage.Instance.ReadInt(key);

            return 0;
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember]
        public static long LoadLong(string key)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                return MySessionComponentScriptSharedStorage.Instance.ReadLong(key);

            return 0;
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember]
        public static float LoadFloat(string key)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                return MySessionComponentScriptSharedStorage.Instance.ReadFloat(key);

            return 0;
        }

        [VisualScriptingMiscData("Shared Storage")]
        [VisualScriptingMember]
        public static Vector3D LoadVector(string key)
        {
            if (MySessionComponentScriptSharedStorage.Instance != null)
                return MySessionComponentScriptSharedStorage.Instance.ReadVector3D(key);

            return Vector3D.Zero;
        }

        [VisualScriptingMiscData("Input")]
        [VisualScriptingMember(true)]
        public static void SetLocalInputBlacklistState(string controlStringId, bool enabled = false)
        {
            MyInput.Static.SetControlBlock(MyStringId.GetOrCompute(controlStringId), !enabled);
        }

        [VisualScriptingMiscData("Input")]
        [VisualScriptingMember]
        public static bool IsLocalInputBlacklisted(string controlStringId)
        {
            return MyInput.Static.IsControlBlocked(MyStringId.GetOrCompute(controlStringId));
        }

        #region Math functions

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static int Round(float value)
        {
            return (int)Math.Round(value);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static Vector3D DirectionVector(Vector3D speed)
        {
            if (speed == Vector3D.Zero)
                return Vector3D.Forward;
            return Vector3D.Normalize(speed);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static int Ceil(float value)
        {
            return (int)Math.Ceiling(value);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static int Floor(float value)
        {
            return (int)Math.Floor(value);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static float Abs(float value)
        {
            return Math.Abs(value);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static float Min(float value1, float value2)
        {
            return Math.Min(value1, value2);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static float Max(float value1, float value2)
        {
            return Math.Max(value1, value2);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static float Clamp(float value, float min, float max)
        {
            return MathHelper.Clamp(value, min, max);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static float DistanceVector3D(Vector3D posA, Vector3D posB)
        {
            return (float)Vector3D.Distance(posA, posB);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static float DistanceVector3(Vector3 posA, Vector3 posB)
        {
            return Vector3.Distance(posA, posB);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static Vector3D CreateVector3D(float x = 0, float y = 0, float z = 0)
        {
            return new Vector3D(x, y, z);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static void GetVector3DComponents(Vector3D vector, out float x, out float y, out float z)
        {
            x = (float)vector.X;
            y = (float)vector.Y;
            z = (float)vector.Z;
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static float RandomFloat(float min, float max)
        {
            return MyUtils.GetRandomFloat(min, max);
        }

        [VisualScriptingMiscData("Math")]
        [VisualScriptingMember]
        public static int RandomInt(int min, int max)
        {
            return MyUtils.GetRandomInt(min, max);
        }

        #endregion
    }
}
