using System.Collections.Generic;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    class MySection
    {
        public string DebugName { get; private set; }
        public MyPart[] Parts { get; private set; }

        MyPart FindPart(List<MyPart> parts, string name)
        {
            foreach (var part in parts)
            {
                if (part.Name == name)
                {
                    return part;
                }
            }
            MyRenderProxy.Error("MaterialName in section cannot be found");
            return null;
        }

        public void Init(MyLod parent, MyMeshSectionInfo info, List<MyPart> parts)
        {
            DebugName = info.Name;
            Parts = new MyPart[info.Meshes.Count];
            for (int i = 0; i < Parts.Length; i++)
            {
                MyMeshSectionMeshInfo partInfo = info.Meshes[i];

                string partName = partInfo.MaterialName; 
                MyPart part = FindPart(parts, partName);
                int startIndex = part.StartIndex + partInfo.StartIndex;
                int indicesCount = partInfo.IndexCount;
                MyRenderProxy.Assert(partInfo.StartIndex + partInfo.IndexCount <= part.IndicesCount, "Section indices referencing indices out of the part");
                Parts[i] = new MyPart();
                Parts[i].InitForHighlight(parent, partName, part.Technique, startIndex, indicesCount, 0);
            }
        }
    }
}
