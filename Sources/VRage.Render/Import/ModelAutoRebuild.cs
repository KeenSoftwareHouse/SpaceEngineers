using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace VRageRender.Import
{
    public static class ModelAutoRebuild
    {
        static MyModelImporter m_importer = new MyModelImporter();


        /// <summary>
        /// Checks whether that model file was build with current sources files. If current sources of this model - FBX, XML, HKT etc. were changed, this returns false.
        /// </summary>
        /// <param name="modelFile"></param>
        /// <returns>true - if data hashes of source files are valid </returns>
        /// <returns>false - if data has been changed</returns>
        public static bool IsModelActual(string modelFile, string FBXFile, string HKTFile, string XMLFile)
        {

            m_importer.ImportData(modelFile);
            Dictionary<string, object> m_retTagData = m_importer.GetTagData();
            
            // FBX
            if (File.Exists(FBXFile))
            {
                if ((m_retTagData.GetValueOrDefault(MyImporterConstants.TAG_FBXHASHSTRING) != null))
                {
                    VRage.Security.Md5.Hash hash = GetFileHash(FBXFile);
                    VRage.Security.Md5.Hash storedHash = (VRage.Security.Md5.Hash)m_retTagData.GetValueOrDefault(MyImporterConstants.TAG_FBXHASHSTRING);
                    if ((hash.A != storedHash.A) || (hash.B != storedHash.B) || (hash.C != storedHash.C) || (hash.D != storedHash.D)) return false;
                }
                else return false; // if tag is not saved at all, rebuild it, so next time we can use it
            }

            // HKT
            if (File.Exists(HKTFile))
            {
                if ((m_retTagData.GetValueOrDefault(MyImporterConstants.TAG_HKTHASHSTRING) != null))
                {
                    VRage.Security.Md5.Hash hash = GetFileHash(HKTFile);
                    VRage.Security.Md5.Hash storedHash = (VRage.Security.Md5.Hash)m_retTagData.GetValueOrDefault(MyImporterConstants.TAG_HKTHASHSTRING);
                    if ((hash.A != storedHash.A) || (hash.B != storedHash.B) || (hash.C != storedHash.C) || (hash.D != storedHash.D)) return false;
                }
                else return false; // if tag is not saved at all, rebuild it, so next time we can use it
            }

            // XML
            if (File.Exists(XMLFile))
            {
                if ((m_retTagData.GetValueOrDefault(MyImporterConstants.TAG_XMLHASHSTRING) != null) && File.Exists(XMLFile))
                {
                    VRage.Security.Md5.Hash hash = GetFileHash(XMLFile);
                    VRage.Security.Md5.Hash storedHash = (VRage.Security.Md5.Hash)m_retTagData.GetValueOrDefault(MyImporterConstants.TAG_XMLHASHSTRING);
                    if ((hash.A != storedHash.A) || (hash.B != storedHash.B) || (hash.C != storedHash.C) || (hash.D != storedHash.D)) return false;
                }
                else return false; // if tag is not saved at all, rebuild it, so next time we can use it
            }
            return true;
        }

        public static VRage.Security.Md5.Hash GetFileHash(string fileName)
        {
            Debug.Assert( File.Exists( fileName) );

            return VRage.Security.Md5.ComputeHash(File.ReadAllBytes(fileName));
        }
    }
}
