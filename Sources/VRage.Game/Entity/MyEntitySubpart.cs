using System.Diagnostics;
using System.IO;
using VRage.Import;
using VRageMath;
using VRageRender.Import;

namespace VRage.Game.Entity
{
    public class MyEntitySubpart : MyEntity
    {
        public struct Data
        {
            public string Name;
            public string File;
            public Matrix InitialTransform;
        };

        public static bool GetSubpartFromDummy(string modelPath, string dummyName, MyModelDummy dummy, ref Data outData)
        {
            if (!dummyName.Contains(MyModelDummy.SUBPART_PREFIX))
                return false;

            Debug.Assert(dummyName.Substring(0, MyModelDummy.SUBPART_PREFIX.Length).Equals(MyModelDummy.SUBPART_PREFIX), string.Format("Subpart name should start with prefix '{0}'", MyModelDummy.SUBPART_PREFIX));
            Debug.Assert(dummy.CustomData.ContainsKey(MyModelDummy.ATTRIBUTE_FILE), "Subpart dummy must have 'file' attribute specified.");

            string subpartPath = Path.Combine(Path.GetDirectoryName(modelPath), (string)dummy.CustomData[MyModelDummy.ATTRIBUTE_FILE]);
            subpartPath += ".mwm"; // Temporary fix
            outData = new Data()
            {
                Name = dummyName.Substring(MyModelDummy.SUBPART_PREFIX.Length),
                File = subpartPath,
                InitialTransform = Matrix.Normalize(dummy.Matrix)
            };
            return true;
        }

        public MyEntitySubpart()
        {
            this.Save = false;
        }
    }
}
