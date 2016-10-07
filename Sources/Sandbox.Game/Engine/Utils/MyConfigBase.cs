using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VRage;
using VRage.FileSystem;
using VRage.Profiler;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;

//  This class encapsulated read/write access to our config file - xxx.cfg - stored in user's local files
//  It assumes that config file may be non existing, or that some values may be missing or in wrong format - this class can handle it
//  and in such case will offer default values -> BUT YOU HAVE TO HELP IT... HOW? -> when writing getter from a new property,
//  you have to return default value in case it's null or empty or invalid!!
//  IMPORTANT: Never call get/set on this class properties from real-time code (during gameplay), e.g. don't do AddCue2D(cueEnum, MyConfig.VolumeMusic)
//  IMPORTANT: Only from loading and initialization methods.

namespace Sandbox.Engine.Utils
{
    public class MyConfigBase
    {
        //  Here we store parameter name (dictionary key) in its value (dictionary value)
        protected readonly SerializableDictionary<string, object> m_values = new SerializableDictionary<string, object>();

        string m_path;

        public MyConfigBase(string fileName)
        {
            m_path = Path.Combine(MyFileSystem.UserDataPath, fileName);
        }

        //  Return parameter value from memory. If not found, empty string is returned.
        protected string GetParameterValue(string parameterName)
        {
            object outObject;
            string outValue;
            if (m_values.Dictionary.TryGetValue(parameterName, out outObject) == false)
            {
                outValue = "";
            }
            else
                outValue = (string)outObject;

            return outValue;
        }

        protected SerializableDictionary<string, object> GetParameterValueDictionary(string parameterName)
        {
            object outObject;
            SerializableDictionary<string, object> outValue;
            if (m_values.Dictionary.TryGetValue(parameterName, out outObject) == false)
            {
                outValue = null;
            }
            else
                outValue = (SerializableDictionary<string, object>)outObject;

            return outValue;
        }

        protected T GetParameterValueT<T>(string parameterName)
        {
            object outObject;
            T outValue;
            if (m_values.Dictionary.TryGetValue(parameterName, out outObject) == false)
            {
                outValue = default(T);
            }
            else
                outValue = (T)outObject;

            return outValue;
        }

        protected Vector3I GetParameterValueVector3I(string parameterName)
        {
            var parts = GetParameterValue(parameterName).Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int x, y, z;
            if (parts.Length == 3 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y) && int.TryParse(parts[2], out z))
            {
                return new Vector3I(x, y, z);
            }
            else
            {
                return new Vector3I(0, 0, 0);
            }
        }

        //  Change parameter's value in memory. It doesn't matter if this parameter was loaded. If was, it will be overwritten. If wasn't loaded, we will just set it.
        protected void SetParameterValue(string parameterName, string value)
        {
            m_values.Dictionary[parameterName] = value;
        }

        //  Change parameter's value in memory. It doesn't matter if this parameter was loaded. If was, it will be overwritten. If wasn't loaded, we will just set it.
        protected void SetParameterValue(string parameterName, float value)
        {
            m_values.Dictionary[parameterName] = value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        //  Change parameter's value in memory. It doesn't matter if this parameter was loaded. If was, it will be overwritten. If wasn't loaded, we will just set it.
        protected void SetParameterValue(string parameterName, bool? value)
        {
            m_values.Dictionary[parameterName] =  value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture.NumberFormat) : "";
        }

        //  Change parameter's value in memory. It doesn't matter if this parameter was loaded. If was, it will be overwritten. If wasn't loaded, we will just set it.
        protected void SetParameterValue(string parameterName, int value)
        {
            m_values.Dictionary[parameterName] = value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        //  Change parameter's value in memory. It doesn't matter if this parameter was loaded. If was, it will be overwritten. If wasn't loaded, we will just set it.
        protected void SetParameterValue(string parameterName, int? value)
        {
            m_values.Dictionary[parameterName] = value == null ? "" : value.Value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        protected void SetParameterValue(string parameterName, Vector3I value)
        {
            SetParameterValue(parameterName, String.Format("{0}, {1}, {2}", value.X, value.Y, value.Z));
        }

        protected void RemoveParameterValue(string parameterName)
        {
            m_values.Dictionary.Remove(parameterName);
        }

        protected T? GetOptionalEnum<T>(string name) where T : struct, IComparable, IFormattable, IConvertible
        {
            int? retInt = MyUtils.GetIntFromString(GetParameterValue(name));
            if (retInt.HasValue && Enum.IsDefined(typeof(T), retInt.Value))
            {
                unsafe
                {
                    int tmp = retInt.Value;
                    int* pTmp = &tmp;
                    T result = default(T);
                    SharpDX.Utilities.Read(new IntPtr(pTmp), ref result);
                    return result;
                }
            }
            else
            {
                return null;
            }
        }

        protected void SetOptionalEnum<T>(string name, T? value) where T : struct, IComparable, IFormattable, IConvertible
        {
            if (value.HasValue)
            {
                T trueValue = value.Value;
                int tmp = 0;
                unsafe
                {
                    int* pTmp = &tmp;
                    SharpDX.Utilities.Write(new IntPtr(pTmp), ref trueValue);
                }
                SetParameterValue(name, tmp);
            }
            else
            {
                RemoveParameterValue(name);
            }
        }

        //  Save all values from config file
        public void Save()
        {
            if (MySandboxGame.IsDedicated)
                return;

            MySandboxGame.Log.WriteLine("MyConfig.Save() - START");
            MySandboxGame.Log.IncreaseIndent();
            ProfilerShort.Begin("MyConfig.Save");
            try
            {
                MySandboxGame.Log.WriteLine("Path: " + m_path, LoggingOptions.CONFIG_ACCESS);

                try
                {
                    using (var stream = MyFileSystem.OpenWrite(m_path))
                    {
                        XmlWriterSettings settings = new XmlWriterSettings()
                        {
                            Indent = true,
                            NewLineHandling = NewLineHandling.None
                        };

                        using (XmlWriter xmlWriter = XmlWriter.Create(stream, settings))
                        {
                            XmlSerializer xmlSerializer = new XmlSerializer(m_values.GetType(), new Type[] { typeof(SerializableDictionary<string, string>), typeof(List<string>), typeof(SerializableDictionary<string, MyConfig.MyDebugInputData>), typeof(MyConfig.MyDebugInputData) });
                            xmlSerializer.Serialize(xmlWriter, m_values);
                        }
                    }
                }
                catch (Exception exc)
                {
                    //  Write exception to log, but continue as if nothing wrong happened
                    MySandboxGame.Log.WriteLine("Exception occured, but application is continuing. Exception: " + exc);
                }
            }
            finally
            {
                ProfilerShort.End();
                MySandboxGame.Log.DecreaseIndent();
                MySandboxGame.Log.WriteLine("MyConfig.Save() - END");
            }
        }


        public void Load()
        {
            if (MySandboxGame.IsDedicated)
                return;

            MySandboxGame.Log.WriteLine("MyConfig.Load() - START");
            using (MySandboxGame.Log.IndentUsing(LoggingOptions.CONFIG_ACCESS))
            {
                MySandboxGame.Log.WriteLine("Path: " + m_path, LoggingOptions.CONFIG_ACCESS);

                //  If anything fails during loading, we ignore log it, but continue - because damaged cfg file must not stop this game
                string xmlTextOriginal = "";
                try
                {
                    if (!File.Exists(m_path))
                    {
                        MySandboxGame.Log.WriteLine("Config file not found! " + m_path);
                    }
                    else
                    {
                        using (var stream = MyFileSystem.OpenRead(m_path))
                        using (XmlReader xmlReader = XmlReader.Create(stream))
                        {
                            XmlSerializer xmlSerializer = new XmlSerializer(m_values.GetType(), new Type[] { typeof(SerializableDictionary<string, string>), typeof(List<string>), typeof(SerializableDictionary<string, MyConfig.MyDebugInputData>), typeof(MyConfig.MyDebugInputData) });

                            SerializableDictionary<string, object> newValues = (SerializableDictionary<string, object>)xmlSerializer.Deserialize(xmlReader);

                            m_values.Dictionary = newValues.Dictionary;
                        }
                    }
                }
                catch (Exception exc)
                {
                    //  Write exception to log, but continue as if nothing wrong happened
                    MySandboxGame.Log.WriteLine("Exception occured, but application is continuing. Exception: " + exc);
                    MySandboxGame.Log.WriteLine("Config:");
                    MySandboxGame.Log.WriteLine(xmlTextOriginal);
                }

                foreach (KeyValuePair<string, object> kvp in m_values.Dictionary)
                {
                    MySandboxGame.Log.WriteLine(kvp.Key + ": " + kvp.Value.ToString(), LoggingOptions.CONFIG_ACCESS);
                }
            }
            MySandboxGame.Log.WriteLine("MyConfig.Load() - END");
        }
    }
}
