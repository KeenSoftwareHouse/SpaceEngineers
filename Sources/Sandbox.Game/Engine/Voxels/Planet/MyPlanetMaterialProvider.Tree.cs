using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public partial class MyPlanetMaterialProvider
    {
        /*********************************************************************
         * 
         * Member types
         * 
         */

        public class VoxelMaterial
        {
            public MyVoxelMaterialDefinition Material;
            public float Depth;
            public byte Value;

            public virtual bool IsRule { get { return false; } }

            public override string ToString()
            {
                if (Material != null)
                    return String.Format("({0}:{1})", Material.Id.SubtypeName, Depth);
                else
                    return "null";
            }
        }

        public class PlanetMaterial : VoxelMaterial
        {
            public VoxelMaterial[] Layers;

            public bool HasLayers { get { return Layers != null && Layers.Length > 0; } }

            public MyVoxelMaterialDefinition FirstOrDefault { get { return HasLayers ? Layers[0].Material : Material; } }

            public PlanetMaterial(MyPlanetMaterialDefinition def)
            {
                Depth = def.MaxDepth;
                if (def.Material != null)
                    Material = GetMaterial(def.Material);

                Value = def.Value;

                if (def.HasLayers)
                {
                    Layers = new VoxelMaterial[def.Layers.Length];

                    for (int i = 0; i < Layers.Length; i++)
                    {
                        Layers[i] = new VoxelMaterial();
                        Layers[i].Material = GetMaterial(def.Layers[i].Material);
                        Layers[i].Depth = def.Layers[i].Depth;
                    }
                }
            }

            private string FormatLayers(int padding)
            {
                StringBuilder sb = new StringBuilder();

                string pad = new string(' ', padding);

                sb.Append('[');
                if (Layers.Length > 0)
                {
                    sb.Append('\n');

                    for (int i = 0; i < Layers.Length; i++)
                    {
                        sb.Append(pad);
                        sb.Append("\t\t");
                        sb.Append(Layers[i]);
                        sb.Append('\n');
                    }
                }
                sb.Append(pad);
                sb.Append(']');

                return sb.ToString();
            }

            public override string ToString()
            {
                return ToString(0);
            }

            public string ToString(int padding)
            {
                if (HasLayers)
                    return String.Format("LayeredMaterial({0})", FormatLayers(padding));
                else return "SimpleMaterial" + base.ToString();
            }
        }

        public class PlanetMaterialRule : PlanetMaterial
        {
            public override bool IsRule { get { return true; } }

            public SerializableRange Height;

            public SymetricSerializableRange Latitude;

            public SerializableRange Longitude;

            public SerializableRange Slope;

            /**
             * Check that a rule matches terrain properties.
             * 
             * @param height Height ration to the height map.
             * @param latitude Latitude cosine
             * @param slope Surface dominant angle sine.
             */
            public bool Check(float height, float latitude, float longitude, float slope)
            {
                return Height.ValueBetween(height) && Latitude.ValueBetween(latitude) && Longitude.ValueBetween(longitude)
                             && Slope.ValueBetween(slope);
            }

            public PlanetMaterialRule(MyPlanetMaterialPlacementRule def)
                : base(def)
            {
                Height = def.Height;
                Latitude = def.Latitude;
                Longitude = def.Longitude;
                Slope = def.Slope;
            }

            public override string ToString()
            {
                return String.Format("MaterialRule(\n\tHeight: {0};\n\tSlope: {1};\n\tLatitude: {2};\n\tLongitude: {3};\n\tMaterials: {4})", Height.ToString(), Slope.ToStringAcos(), Latitude.ToStringAsin(), Longitude.ToStringLongitude(), base.ToString(4));
            }
        }

        public class PlanetBiome
        {
            public MyDynamicAABBTree MateriaTree;

            public byte Value;

            public string Name;

            public List<PlanetMaterialRule> Rules;

            public bool IsValid { get { return Rules.Count > 0; } }

            public PlanetBiome(MyPlanetMaterialGroup group)
            {
                Value = group.Value;

                Name = group.Name;

                Rules = new List<PlanetMaterialRule>(group.MaterialRules.Length);

                for (int i = 0; i < group.MaterialRules.Length; i++)
                {
                    Rules.Add(new PlanetMaterialRule(group.MaterialRules[i]));
                }

                MateriaTree = new MyDynamicAABBTree(Vector3.Zero);

                foreach (var rule in Rules)
                {
                    BoundingBox bb = new BoundingBox(new Vector3(rule.Height.Min, rule.Latitude.Min, rule.Longitude.Min), new Vector3(rule.Height.Max, rule.Latitude.Max, rule.Longitude.Max));
                    MateriaTree.AddProxy(ref bb, rule, 0);
                    if (rule.Latitude.Mirror)
                    {
                        float min = -bb.Max.Y;
                        bb.Max.Y = -bb.Min.Y;
                        bb.Min.Y = min;
                        MateriaTree.AddProxy(ref bb, rule, 0);
                    }
                }
            }
        }
    }
}
