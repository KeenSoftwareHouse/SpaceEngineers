using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Profiler
{
    public partial class MyProfiler
    {
        public struct MyProfilerBlockKey
        {
            public readonly string File;
            public readonly string Member;
            public readonly string Name;
            public readonly int Line;
            public readonly int ParentId;
            public readonly int HashCode;

            public MyProfilerBlockKey(string file, string member, string name, int line, int parentId)
            {
                File = file;
                Member = member;
                Name = name;
                Line = line;
                ParentId = parentId;
                unchecked
                {
                    HashCode = file.GetHashCode();
                    HashCode = (397 * HashCode) ^ member.GetHashCode();
                    HashCode = (397 * HashCode) ^ (name ?? String.Empty).GetHashCode();
                    HashCode = (397 * HashCode) ^ parentId.GetHashCode();
                }
            }

            public override bool Equals(object obj)
            {
                throw new InvalidBranchException("Equals is not supposed to be called, use comparer!");
            }

            public override int GetHashCode()
            {
                throw new InvalidBranchException("Get hash code is not supposed to be called, use comparer!");
            }
        }

        public class MyProfilerBlockKeyComparer : IEqualityComparer<MyProfilerBlockKey>
        {
            public bool Equals(MyProfilerBlockKey x, MyProfilerBlockKey y)
            {
                return x.ParentId == y.ParentId && x.Name == y.Name && x.Member == y.Member && x.File == y.File && x.Line == y.Line;
            }

            public int GetHashCode(MyProfilerBlockKey obj)
            {
                return obj.HashCode;
            }
        }
    }
}
