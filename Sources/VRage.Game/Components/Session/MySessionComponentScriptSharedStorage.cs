using System.Diagnostics;
using VRage.Game.ObjectBuilders.Components;
using VRageMath;

namespace VRage.Game.Components.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 1000, typeof(MyObjectBuilder_SharedStorageComponent))]
    public class MySessionComponentScriptSharedStorage : MySessionComponentBase
    {
        private MyObjectBuilder_SharedStorageComponent m_objectBuilder;
        private static MySessionComponentScriptSharedStorage m_instance;

        public static MySessionComponentScriptSharedStorage Instance { get { return m_instance; } }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            var ob = sessionComponent as MyObjectBuilder_SharedStorageComponent;
            Debug.Assert(ob != null);
            m_objectBuilder = new MyObjectBuilder_SharedStorageComponent
            {
                BoolStorage = ob.BoolStorage,
                FloatStorage = ob.FloatStorage,
                StringStorage = ob.StringStorage,
                IntStorage = ob.IntStorage,
                Vector3DStorage = ob.Vector3DStorage,
                ExistingFieldsAndStaticAttribute = ob.ExistingFieldsAndStaticAttribute
            };

            m_instance = this;
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            return m_objectBuilder;
        }

        public bool Write(string variableName, int value, bool @static = false)
        {
            if (m_objectBuilder == null) return false;
            if (!m_objectBuilder.IntStorage.Dictionary.ContainsKey(variableName))
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.ContainsKey(variableName))
                    return false;
                else
                {
                    m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.Add(variableName, @static);
                    m_objectBuilder.IntStorage.Dictionary.Add(variableName, value);
                }

            else
            {
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary[variableName])
                    return false;

                m_objectBuilder.IntStorage.Dictionary[variableName] = value;
            }

            return true;
        }

        public bool Write(string variableName, long value, bool @static = false)
        {
            if (m_objectBuilder == null) return false;
            if (!m_objectBuilder.LongStorage.Dictionary.ContainsKey(variableName))
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.ContainsKey(variableName))
                    return false;
                else
                {
                    m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.Add(variableName, @static);
                    m_objectBuilder.LongStorage.Dictionary.Add(variableName, value);
                }

            else
            {
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary[variableName])
                    return false;

                m_objectBuilder.LongStorage.Dictionary[variableName] = value;
            }

            return true;
        }

        public bool Write(string variableName, bool value, bool @static = false)
        {
            if (m_objectBuilder == null) return false;
            if (!m_objectBuilder.BoolStorage.Dictionary.ContainsKey(variableName))
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.ContainsKey(variableName))
                    return false;
                else
                {
                    m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.Add(variableName, @static);
                    m_objectBuilder.BoolStorage.Dictionary.Add(variableName, value);
                }
                    
            else
            {
                if(m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary[variableName])
                    return false;

                m_objectBuilder.BoolStorage.Dictionary[variableName] = value;
            }

            return true;
        }

        public bool Write(string variableName, float value, bool @static = false)
        {
            if (m_objectBuilder == null) return false;
            if (!m_objectBuilder.FloatStorage.Dictionary.ContainsKey(variableName))
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.ContainsKey(variableName))
                    return false;
                else
                {
                    m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.Add(variableName, @static);
                    m_objectBuilder.FloatStorage.Dictionary.Add(variableName, value);
                }

            else
            {
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary[variableName])
                    return false;

                m_objectBuilder.FloatStorage.Dictionary[variableName] = value;
            }

            return true;
        }

        public bool Write(string variableName, string value, bool @static = false)
        {
            if (m_objectBuilder == null) return false;
            if (!m_objectBuilder.StringStorage.Dictionary.ContainsKey(variableName))
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.ContainsKey(variableName))
                    return false;
                else
                {
                    m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.Add(variableName, @static);
                    m_objectBuilder.StringStorage.Dictionary.Add(variableName, value);
                }

            else
            {
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary[variableName])
                    return false;

                m_objectBuilder.StringStorage.Dictionary[variableName] = value;
            }

            return true;
        }

        public bool Write(string variableName, Vector3D value, bool @static = false)
        {
            if (m_objectBuilder == null) return false;
            if (!m_objectBuilder.Vector3DStorage.Dictionary.ContainsKey(variableName))
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.ContainsKey(variableName))
                    return false;
                else
                {
                    m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary.Add(variableName, @static);
                    m_objectBuilder.Vector3DStorage.Dictionary.Add(variableName, value);
                }

            else
            {
                if (m_objectBuilder.ExistingFieldsAndStaticAttribute.Dictionary[variableName])
                    return false;

                m_objectBuilder.Vector3DStorage.Dictionary[variableName] = value;
            }

            return true;
        }

        public int ReadInt(string variableName)
        {
            if (m_objectBuilder == null) return -1;
            int val;
            if (m_objectBuilder.IntStorage.Dictionary.TryGetValue(variableName, out val))
                return val;

            return -1;
        }

        public long ReadLong(string variableName)
        {
            if (m_objectBuilder == null) return -1;
            long val;
            if (m_objectBuilder.LongStorage.Dictionary.TryGetValue(variableName, out val))
                return val;

            return -1;
        }

        public float ReadFloat(string variableName)
        {
            if (m_objectBuilder == null) return 0;
            float val;
            if (m_objectBuilder.FloatStorage.Dictionary.TryGetValue(variableName, out val))
                return val;

            return 0;
        }

        public string ReadString(string variableName)
        {
            if (m_objectBuilder == null) return null;
            string val;
            if (m_objectBuilder.StringStorage.Dictionary.TryGetValue(variableName, out val))
                return val;

            return null;
        }

        public Vector3D ReadVector3D(string variableName)
        {
            if (m_objectBuilder == null) return Vector3D.Zero;
            SerializableVector3D val;
            if (m_objectBuilder.Vector3DStorage.Dictionary.TryGetValue(variableName, out val))
                return val;

            return Vector3D.Zero;
        }

        public bool ReadBool(string variableName)
        {
            if (m_objectBuilder == null) return false;
            bool val;
            if (m_objectBuilder.BoolStorage.Dictionary.TryGetValue(variableName, out val))
                return val;

            return false;
        }
    }
}
