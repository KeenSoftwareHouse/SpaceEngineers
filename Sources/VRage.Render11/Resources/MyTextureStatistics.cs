using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Render11.Resources
{
    struct MyTextureStatistics
    {
        public int TexturesTotal { get; private set; }
        public long TotalTextureMemory { get; private set; }
        public int TexturesTotalPeak { get; private set; }
        public long TotalTextureMemoryPeak { get; private set; }

        public void Add(ITexture texture)
        {
            TexturesTotal++;
            TotalTextureMemory += texture.GetTextureByteSize();
            TexturesTotalPeak = Math.Max(TexturesTotalPeak, TexturesTotal);
            TotalTextureMemoryPeak = Math.Max(TotalTextureMemoryPeak, TotalTextureMemory);
        }

        public void Remove(ITexture texture)
        {
            TexturesTotal--;
            TotalTextureMemory -= texture.GetTextureByteSize();
        }
    }
}
