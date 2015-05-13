using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;

namespace Sandbox.Game.Entities.Cube
{
    class MyCubeBlockDefinitionWithVariants
    {
        MyCubeBlockDefinition m_baseDefinition;
        int m_variantIndex = -1;

        public void Next()
        {
            if (m_baseDefinition.Variants != null && m_baseDefinition.Variants.Count > 0)
            {
                // To allow -1 as valid part of range
                m_variantIndex++;
                m_variantIndex++;
                m_variantIndex %= (m_baseDefinition.Variants.Count + 1);
                m_variantIndex--;
            }
        }

        public void Prev()
        {
            if (m_baseDefinition.Variants != null && m_baseDefinition.Variants.Count > 0)
            {
                // To allow -1 as valid part of range
                m_variantIndex = (m_variantIndex + m_baseDefinition.Variants.Count + 1) % (m_baseDefinition.Variants.Count + 1);
                m_variantIndex--;
            }
        }

        public void Reset()
        {
            m_variantIndex = -1;
        }

        public int VariantIndex
        {
            get
            {
                return m_variantIndex;
            }
        }

        public MyCubeBlockDefinition Base
        {
            get
            {
                return m_baseDefinition;
            }
        }

        public MyCubeBlockDefinitionWithVariants(MyCubeBlockDefinition definition, int variantIndex)
        {
            m_baseDefinition = definition;
            m_variantIndex = variantIndex;
            if (m_baseDefinition.Variants == null || m_baseDefinition.Variants.Count == 0)
            {
                m_variantIndex = -1;
            }
            else if (m_variantIndex != -1)
            {
                m_variantIndex %= m_baseDefinition.Variants.Count;
            }
        }

        public static implicit operator MyCubeBlockDefinitionWithVariants(MyCubeBlockDefinition definition)
        {
            return new MyCubeBlockDefinitionWithVariants(definition, -1);
        }

        public static implicit operator MyCubeBlockDefinition(MyCubeBlockDefinitionWithVariants definition)
        {
            if (definition == null)
                return null;

            if (definition.m_variantIndex == -1)
            {
                return definition.m_baseDefinition;
            }
            else
            {
                return definition.m_baseDefinition.Variants[definition.m_variantIndex];
            }
        }
    }
}
