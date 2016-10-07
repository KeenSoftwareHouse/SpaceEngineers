using System.Diagnostics;

namespace VRageRender.Utils
{
    public partial class MyPolygonBoolOps
    {
        private enum ClassificationIndex : byte
        {
            LEFT_SUBJECT_AND_LEFT_SUBJECT = 0,
            LEFT_SUBJECT_AND_LEFT_CLIP = 1,
            LEFT_SUBJECT_AND_RIGHT_SUBJECT = 2,
            LEFT_SUBJECT_AND_RIGHT_CLIP = 3,

            LEFT_CLIP_AND_LEFT_SUBJECT = 4,
            LEFT_CLIP_AND_LEFT_CLIP = 5,
            LEFT_CLIP_AND_RIGHT_SUBJECT = 6,
            LEFT_CLIP_AND_RIGHT_CLIP = 7,

            RIGHT_SUBJECT_AND_LEFT_SUBJECT = 8,
            RIGHT_SUBJECT_AND_LEFT_CLIP = 9,
            RIGHT_SUBJECT_AND_RIGHT_SUBJECT = 10,
            RIGHT_SUBJECT_AND_RIGHT_CLIP = 11,

            RIGHT_CLIP_AND_LEFT_SUBJECT = 12,
            RIGHT_CLIP_AND_LEFT_CLIP = 13,
            RIGHT_CLIP_AND_RIGHT_SUBJECT = 14,
            RIGHT_CLIP_AND_RIGHT_CLIP = 15,
        }

        private enum IntersectionClassification : byte
        {
            INVALID,
            LIKE_INTERSECTION,
            LOCAL_MINIMUM,
            LOCAL_MAXIMUM,
            LEFT_E1_INTERMEDIATE,
            RIGHT_E1_INTERMEDIATE,
            LEFT_E2_INTERMEDIATE,
            RIGHT_E2_INTERMEDIATE,
        }

        private class Operation
        {
            private IntersectionClassification[] m_classificationTable;
            private bool m_sParityForContribution;
            private bool m_cParityForContribution;
            public bool SubjectInvert { get; private set; }
            public bool ClipInvert { get; private set; }

            public Operation(IntersectionClassification[] classificationTable, bool subjectContributingParity, bool clipContributingParity, bool invertSubjectSides, bool invertClipSides)
            {
                m_classificationTable = classificationTable;
                m_sParityForContribution = subjectContributingParity;
                m_cParityForContribution = clipContributingParity;
                SubjectInvert = invertSubjectSides;
                ClipInvert = invertClipSides;
            }

            public IntersectionClassification ClassifyIntersection(Side side1, PolygonType type1, Side side2, PolygonType type2)
            {
                return m_classificationTable[EncodeClassificationIndex(side1, type1, side2, type2)];
            }

            public bool InitializeContributing(bool parity, PolygonType type)
            {
                if (type == PolygonType.SUBJECT)
                {
                    if (parity == true)
                    {
                        return m_sParityForContribution;
                    }
                    else
                    {
                        return !m_sParityForContribution;
                    }
                }
                else
                {
                    if (parity == true)
                    {
                        return m_cParityForContribution;
                    }
                    else
                    {
                        return !m_cParityForContribution;
                    }
                }
            }
        }

        private static Operation m_operationIntersection;
        private static Operation m_operationUnion;
        private static Operation m_operationDifference;

        [Conditional("DEBUG")]
        private static void CheckClassificationIndices()
        {
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.SUBJECT, Side.LEFT, PolygonType.SUBJECT) == (int)ClassificationIndex.LEFT_SUBJECT_AND_LEFT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.SUBJECT, Side.LEFT, PolygonType.CLIP) == (int)ClassificationIndex.LEFT_SUBJECT_AND_LEFT_CLIP);
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.SUBJECT, Side.RIGHT, PolygonType.SUBJECT) == (int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.SUBJECT, Side.RIGHT, PolygonType.CLIP) == (int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_CLIP);
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.CLIP, Side.LEFT, PolygonType.SUBJECT) == (int)ClassificationIndex.LEFT_CLIP_AND_LEFT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.CLIP, Side.LEFT, PolygonType.CLIP) == (int)ClassificationIndex.LEFT_CLIP_AND_LEFT_CLIP);
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.CLIP, Side.RIGHT, PolygonType.SUBJECT) == (int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.LEFT, PolygonType.CLIP, Side.RIGHT, PolygonType.CLIP) == (int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_CLIP);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.SUBJECT, Side.LEFT, PolygonType.SUBJECT) == (int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.SUBJECT, Side.LEFT, PolygonType.CLIP) == (int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_CLIP);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.SUBJECT, Side.RIGHT, PolygonType.SUBJECT) == (int)ClassificationIndex.RIGHT_SUBJECT_AND_RIGHT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.SUBJECT, Side.RIGHT, PolygonType.CLIP) == (int)ClassificationIndex.RIGHT_SUBJECT_AND_RIGHT_CLIP);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.CLIP, Side.LEFT, PolygonType.SUBJECT) == (int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.CLIP, Side.LEFT, PolygonType.CLIP) == (int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_CLIP);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.CLIP, Side.RIGHT, PolygonType.SUBJECT) == (int)ClassificationIndex.RIGHT_CLIP_AND_RIGHT_SUBJECT);
            Debug.Assert(EncodeClassificationIndex(Side.RIGHT, PolygonType.CLIP, Side.RIGHT, PolygonType.CLIP) == (int)ClassificationIndex.RIGHT_CLIP_AND_RIGHT_CLIP);
        }

        private static int EncodeClassificationIndex(Side side1, PolygonType type1, Side side2, PolygonType type2)
        {
            return (((int)side1 + (int)type1) << 2) + (int)side2 + (int)type2;
        }

        private static void InitializeOperations()
        {
            CheckClassificationIndices();

            IntersectionClassification[] table = new IntersectionClassification[16];

            table = new IntersectionClassification[16];
            InitClassificationTable(table);
            table[(int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_CLIP] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_CLIP] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_SUBJECT] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_SUBJECT] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.LEFT_CLIP_AND_LEFT_SUBJECT] = IntersectionClassification.LEFT_E2_INTERMEDIATE;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_LEFT_CLIP] = IntersectionClassification.LEFT_E2_INTERMEDIATE;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_RIGHT_SUBJECT] = IntersectionClassification.RIGHT_E1_INTERMEDIATE;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_RIGHT_CLIP] = IntersectionClassification.RIGHT_E1_INTERMEDIATE;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_CLIP] = IntersectionClassification.LOCAL_MAXIMUM;
            table[(int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_SUBJECT] = IntersectionClassification.LOCAL_MAXIMUM;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_CLIP] = IntersectionClassification.LOCAL_MINIMUM;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_SUBJECT] = IntersectionClassification.LOCAL_MINIMUM;

            m_operationIntersection = new Operation(table, true, true, false, false);

            table = new IntersectionClassification[16];
            InitClassificationTable(table);
            table[(int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_CLIP] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_CLIP] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_SUBJECT] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_SUBJECT] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.LEFT_CLIP_AND_LEFT_SUBJECT] = IntersectionClassification.LEFT_E1_INTERMEDIATE;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_LEFT_CLIP] = IntersectionClassification.LEFT_E1_INTERMEDIATE;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_RIGHT_SUBJECT] = IntersectionClassification.RIGHT_E2_INTERMEDIATE;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_RIGHT_CLIP] = IntersectionClassification.RIGHT_E2_INTERMEDIATE;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_CLIP] = IntersectionClassification.LOCAL_MINIMUM;
            table[(int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_SUBJECT] = IntersectionClassification.LOCAL_MINIMUM;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_CLIP] = IntersectionClassification.LOCAL_MAXIMUM;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_SUBJECT] = IntersectionClassification.LOCAL_MAXIMUM;

            m_operationUnion = new Operation(table, false, false, false, false);

            table = new IntersectionClassification[16];
            InitClassificationTable(table);
            table[(int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_CLIP] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_CLIP] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_SUBJECT] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_SUBJECT] = IntersectionClassification.LIKE_INTERSECTION;
            table[(int)ClassificationIndex.LEFT_CLIP_AND_LEFT_SUBJECT] = IntersectionClassification.LEFT_E2_INTERMEDIATE;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_LEFT_CLIP] = IntersectionClassification.LEFT_E2_INTERMEDIATE;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_RIGHT_SUBJECT] = IntersectionClassification.RIGHT_E1_INTERMEDIATE;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_RIGHT_CLIP] = IntersectionClassification.RIGHT_E1_INTERMEDIATE;
            table[(int)ClassificationIndex.LEFT_SUBJECT_AND_RIGHT_CLIP] = IntersectionClassification.LOCAL_MAXIMUM;
            table[(int)ClassificationIndex.LEFT_CLIP_AND_RIGHT_SUBJECT] = IntersectionClassification.LOCAL_MAXIMUM;
            table[(int)ClassificationIndex.RIGHT_SUBJECT_AND_LEFT_CLIP] = IntersectionClassification.LOCAL_MINIMUM;
            table[(int)ClassificationIndex.RIGHT_CLIP_AND_LEFT_SUBJECT] = IntersectionClassification.LOCAL_MINIMUM;

            m_operationDifference = new Operation(table, false, true, false, true);
        }

        private static void InitClassificationTable(IntersectionClassification[] m_classificationTable)
        {
            for (int i = 0; i < 16; ++i)
            {
                m_classificationTable[i] = IntersectionClassification.INVALID;
            }
        }
    }
}
