using KeenSoftwareHouse.Library.IO;
#if !XB1 // XB1_NOPROTOBUF
using ProtoBuf.Meta;
#endif // !XB1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using VRage.FileSystem;
using VRage.Library.Collections;
using VRage.Plugins;
using VRage.Serialization;
using VRage.Utils;

namespace VRage.ObjectBuilders
{
    public class MyObjectBuilderSerializer
    {
        // -------------------------
        private static MyObjectFactory<MyObjectBuilderDefinitionAttribute, MyObjectBuilder_Base> m_objectFactory;
        // -------------------------
        private static readonly Dictionary<Type, XmlSerializer> m_serializersByType = new Dictionary<Type, XmlSerializer>();
        private static readonly Dictionary<string, XmlSerializer> m_serializersBySerializedName = new Dictionary<string, XmlSerializer>();
        private static readonly Dictionary<Type, string> m_serializedNameByType = new Dictionary<Type, string>();
        // -------------------------
#if !XB1 // XB1_NOPROTOBUF
        public static RuntimeTypeModel Serializer;
#endif // !XB1
        public static readonly MySerializeInfo Dynamic = new MySerializeInfo(MyObjectFlags.Dynamic, MyPrimitiveFlags.None, 0, SerializeDynamic, null, null);
        // -------------------------
        
        public enum XmlCompression
        {
            Uncompressed = 0,
            Gzip = 1,
        }

        static MyObjectBuilderSerializer()
        {
#if !XB1 // XB1_NOPROTOBUF
            Serializer = TypeModel.Create();
            Serializer.AutoAddMissingTypes = true;
            Serializer.UseImplicitZeroDefaults = false;
#endif // !XB1
            m_objectFactory = new MyObjectFactory<MyObjectBuilderDefinitionAttribute, MyObjectBuilder_Base>();
        }

        public static void RegisterFromAssembly(Assembly assembly)
        {
            m_objectFactory.RegisterFromAssembly(assembly);
        }

        // Are the types already registered?
        public static bool IsReady()
        {
            return m_serializersByType.Count > 0;
        }

        #region Definitions

#if !XB1
        /// <summary>Generates an identifier for the assembly of a specified type</summary>
        /// <remarks>Code copied from the .NET serialization classes - to emulate the same bahavior</remarks>
        /// <param name="type">The type</param>
        /// <returns>String identifying the type's assembly</returns>
        private static string GenerateAssemblyId(Type type)
        {
            Module[] modules = type.Assembly.GetModules();
            List<string> list = new List<string>();
            for (int i = 0; i < modules.Length; i++)
            {
                list.Add(modules[i].ModuleVersionId.ToString());
            }
            list.Sort();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i].ToString());
                sb.Append(",");
            }
            return sb.ToString();
        } // GenerateAssemblyId
#endif // !XB1

        static int f = 0;

        // Load (from dll or create at runtime) all serializers at once.
        public static void LoadSerializers()
        {
            int index = 0;
            foreach (var definition in m_objectFactory.Attributes)
            {
                index++;
                var typeId = (MyRuntimeObjectBuilderId)(MyObjectBuilderType)definition.ProducedType;
#if !XB1 // XB1_NOPROTOBUF
                Serializer.Add(definition.ProducedType.BaseType, true)
                    .AddSubType(typeId.Value * 1000, definition.ProducedType);
#endif // !XB1

            }

            foreach (var definition in m_objectFactory.Attributes)
            {
                var type = definition.ProducedType;

                if (type.Name.Contains("Particle"))
                {
                    f++;
                }

                try
                {
                    XmlSerializer serializer;
                    serializer = new XmlSerializer(type);
                    // register created serializer
                    m_serializersByType.Add(type, serializer);
                    {
                        string serializedName = type.Name;
                        var xmlTypeAttributes = type.GetCustomAttributes(typeof(XmlTypeAttribute), false);
                        if (xmlTypeAttributes.Length != 0)
                        {
                            Debug.Assert(xmlTypeAttributes.Length == 1);
                            serializedName = (xmlTypeAttributes[0] as XmlTypeAttribute).TypeName;
                        }
                        m_serializersBySerializedName.Add(serializedName, serializer);
                        m_serializedNameByType.Add(type, serializedName);
                    }
                    // serializer registered, all went well
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Error creating XML serializer for type " + type.Name, e);
                }
            }
        }

        [Obsolete]
        public static XmlSerializer GetSerializer(Type type)
        {
            return m_serializersByType[type];
        }

        [Obsolete]
        public static string GetSerializedName(Type type)
        {
            return m_serializedNameByType[type];
        }

        [Obsolete]
        public static XmlSerializer GetSerializer(string serializedName)
        {
            return m_serializersBySerializedName[serializedName];
        }

        public static bool IsSerializerAvailable(string serializedName)
        {
            return m_serializersBySerializedName.ContainsKey(serializedName);
        }

        #endregion

        #region Serialization

        private static void SerializeXMLInternal(Stream writeTo, MyObjectBuilder_Base objectBuilder, Type serializeAsType = null)
        {
            XmlSerializer serializer = m_serializersByType[serializeAsType ?? objectBuilder.GetType()];
            serializer.Serialize(writeTo, objectBuilder);
        }

        private static void SerializeGZippedXMLInternal(Stream writeTo, MyObjectBuilder_Base objectBuilder, Type serializeAsType = null)
        {
            using (GZipStream gz = new GZipStream(writeTo, CompressionMode.Compress, true))
            using (BufferedStream buffer = new BufferedStream(gz, 0x8000))
            {
                SerializeXMLInternal(buffer, objectBuilder, serializeAsType);
            }
        }

        public static bool SerializeXML(Stream writeTo, MyObjectBuilder_Base objectBuilder, XmlCompression compress = XmlCompression.Uncompressed, Type serializeAsType = null)
        {
            try
            {
                if (compress == XmlCompression.Gzip)
                    SerializeGZippedXMLInternal(writeTo, objectBuilder, serializeAsType);
                else if (compress == XmlCompression.Uncompressed)
                    SerializeXMLInternal(writeTo, objectBuilder, serializeAsType);
                else
                    Debug.Assert(false, "Unhandled XML compression type during object builder serialization");
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error during serialization.");
                MyLog.Default.WriteLine(e.ToString());
                return false;
            }
            return true;
        }

        public static bool SerializeXML(string path, bool compress, MyObjectBuilder_Base objectBuilder, Type serializeAsType = null)
        {
            ulong sizeInBytes;
            return SerializeXML(path, compress, objectBuilder, out sizeInBytes, serializeAsType);
        }

        public static bool SerializeXML(string path, bool compress, MyObjectBuilder_Base objectBuilder, out ulong sizeInBytes, Type serializeAsType = null)
        {
            try
            {
                using (var fileStream = MyFileSystem.OpenWrite(path))
                using (var writeStream = compress ? fileStream.WrapGZip() : fileStream)
                {
                    long startPos = fileStream.Position;
                    XmlSerializer serializer = m_serializersByType[serializeAsType ?? objectBuilder.GetType()];
                    serializer.Serialize(writeStream, objectBuilder);
                    sizeInBytes = (ulong)(fileStream.Position - startPos); // Length of compressed stream
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error: " + path + " failed to serialize.");
                MyLog.Default.WriteLine(e.ToString());

#if !XB1
#if DEBUG
                var io = e as IOException;
                if (io != null && io.IsFileLocked())
                {
                    MyLog.Default.WriteLine("Files is locked during saving.");
                    MyLog.Default.WriteLine("Xml file locks:");
                    try
                    {
                        foreach (var p in Win32Processes.GetProcessesLockingFile(path))
                        {
                            MyLog.Default.WriteLine(p.ProcessName);
                        }
                    }
                    catch (Exception e2)
                    {
                        MyLog.Default.WriteLine(e2);
                    }
                }
#endif
#endif // !XB1

                sizeInBytes = 0;

                return false;
            }
            return true;
        }

        public static bool DeserializeXML<T>(string path, out T objectBuilder) where T : MyObjectBuilder_Base
        {
            ulong sizeInBytes;
            return DeserializeXML(path, out objectBuilder, out sizeInBytes);
        }

        public static bool DeserializeXML<T>(string path, out T objectBuilder, out ulong fileSize) where T : MyObjectBuilder_Base
        {
            bool result = false;
            fileSize = 0;
            objectBuilder = null;

            using (var fileStream = MyFileSystem.OpenRead(path))
            {
                if (fileStream != null)
                    using (var readStream = fileStream.UnwrapGZip())
                    {
                        if (readStream != null)
                        {
                            fileSize = (ulong)fileStream.Length;
                            result = DeserializeXML(readStream, out objectBuilder);
                        }
                    }
            }

            if (!result)
                MyLog.Default.WriteLine(string.Format("Failed to deserialize file '{0}'", path));

            return result;
        }

        public static bool DeserializeXML<T>(Stream reader, out T objectBuilder) where T : MyObjectBuilder_Base
        {
            MyObjectBuilder_Base obj;
            bool result = DeserializeXML(reader, out obj, typeof(T));
            objectBuilder = (T)obj;
            return result;
        }

        public static bool DeserializeXML(string path, out MyObjectBuilder_Base objectBuilder, Type builderType) 
        {
            bool result = false;
            objectBuilder = null;
            using (var fileStream = MyFileSystem.OpenRead(path))
            {
                if (fileStream != null)
                    using (var readStream = fileStream.UnwrapGZip())
                    {
                        if (readStream != null)
                        {
                            result = DeserializeXML(readStream, out objectBuilder, builderType);
                        }
                    }
            }

            if (!result)
                MyLog.Default.WriteLine(string.Format("Failed to deserialize file '{0}'", path));

            return result;
        }

        public static bool DeserializeXML(Stream reader, out MyObjectBuilder_Base objectBuilder, Type builderType)
        {
            Debug.Assert(typeof(MyObjectBuilder_Base).IsAssignableFrom(builderType));
            Debug.Assert(reader != null);
            Debug.Assert(builderType != null);

            objectBuilder = null;
            try
            {
                XmlSerializer serializer = m_serializersByType[builderType];
                Debug.Assert(serializer != null);
                objectBuilder = (MyObjectBuilder_Base)serializer.Deserialize(XmlReader.Create(reader, new XmlReaderSettings() { CheckCharacters = false }));
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ERROR: Exception during objectbuilder read! (xml): " + builderType.Name);
                MyLog.Default.WriteLine(e);
                if (Debugger.IsAttached)
                    Debugger.Break();
                return false;
            }

            return true;
        }

        public static bool DeserializeGZippedXML<T>(Stream reader, out T objectBuilder) where T : MyObjectBuilder_Base
        {
            objectBuilder = null;
            try
            {
                using (GZipStream gz = new GZipStream(reader, CompressionMode.Decompress))
                using (BufferedStream buffer = new BufferedStream(gz, 0x8000))
                {
                    XmlSerializer serializer = m_serializersByType[typeof(T)];
                    objectBuilder = (T)serializer.Deserialize(buffer);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ERROR: Exception during objectbuilder read! (xml): " + typeof(T).Name);
                MyLog.Default.WriteLine(e);
                if (Debugger.IsAttached)
                    Debugger.Break();
                return false;
            }

            return true;
        }

        public static void SerializeDynamic(BitStream stream, Type baseType, ref Type obj)
        {
            if (stream.Reading)
            {
                var typeId = new MyRuntimeObjectBuilderId(stream.ReadUInt16());
                obj = (MyObjectBuilderType)typeId;
            }
            else
            {
                var typeId = (MyRuntimeObjectBuilderId)(MyObjectBuilderType)obj;
                stream.WriteUInt16(typeId.Value);
            }
        }

        #endregion

        #region Create

        public static MyObjectBuilder_Base CreateNewObject(SerializableDefinitionId id)
        {
            return CreateNewObject(id.TypeId, id.SubtypeId);
        }

        public static MyObjectBuilder_Base CreateNewObject(MyObjectBuilderType type, string subtypeName)
        {
            var ob = CreateNewObject(type);
            ob.SubtypeName = subtypeName;
            return ob;
        }

        public static MyObjectBuilder_Base CreateNewObject(MyObjectBuilderType type)
        {
            return m_objectFactory.CreateInstance(type);
        }

        public static T CreateNewObject<T>(string subtypeName) where T : MyObjectBuilder_Base, new()
        {
            T ob = CreateNewObject<T>();
            ob.SubtypeName = subtypeName;
            return ob;
        }

        public static T CreateNewObject<T>() where T : MyObjectBuilder_Base, new()
        {
            return m_objectFactory.CreateInstance<T>();
        }

        #endregion

        public static MyObjectBuilder_Base Clone(MyObjectBuilder_Base toClone)
        {
            MyObjectBuilder_Base clone = null;
            using (var stream = new MemoryStream())
            {
                SerializeXMLInternal(stream, toClone);

                stream.Position = 0;

                DeserializeXML(stream, out clone, toClone.GetType());
            }
            return clone;
        }

        /// <summary>
        /// Performs shallow copy of data between members of the same name and type from source to target.
        /// This method can be slow and inefficient, so use only when needed.
        /// </summary>
        public static void MemberwiseAssignment(MyObjectBuilder_Base source, MyObjectBuilder_Base target)
        {
            var sourceFields = source.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty);
            var targetFields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty);
            foreach (var sourceField in sourceFields)
            {
                var targetField = targetFields.FirstOrDefault(delegate(FieldInfo fieldInfo)
                {
                    return fieldInfo.FieldType == sourceField.FieldType &&
                        fieldInfo.Name == sourceField.Name;
                });
                if (targetField == default(FieldInfo))
                    continue;

                targetField.SetValue(target, sourceField.GetValue(source));
            }
        }

        public static void UnregisterAssembliesAndSerializers()
        {
            m_objectFactory = new MyObjectFactory<MyObjectBuilderDefinitionAttribute, MyObjectBuilder_Base>();
            // -------------------------
            m_serializersByType.Clear();
            m_serializersBySerializedName.Clear();
            m_serializedNameByType.Clear();
            // -------------------------
#if !XB1 // XB1_NOPROTOBUF
            Serializer = TypeModel.Create(); // create empty protobuf serializer
            Serializer.AutoAddMissingTypes = true;
            Serializer.UseImplicitZeroDefaults = false;
#endif // !XB1
        }
    }
}
