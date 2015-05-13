using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace Sandbox.Game
{
    public class MyCreditsDepartment
    {
        public StringBuilder Name;
        public List<MyCreditsPerson> Persons;

        public MyCreditsDepartment(string name)
        {
            Name = new StringBuilder(name);
            Persons = new List<MyCreditsPerson>();
        }
    }

    public class MyCreditsPerson
    {
        public StringBuilder Name;

        public MyCreditsPerson(string name)
        {
            Name = new StringBuilder(name);
        }
    }

    public class MyCreditsNotice
    {
        public string LogoTexture;
        public Vector2? LogoNormalizedSize;
        public float? LogoScale;
        public float LogoOffset = 0.07f;
        public readonly List<StringBuilder> CreditNoticeLines;

        public MyCreditsNotice()
        {
            CreditNoticeLines = new List<StringBuilder>();
        }
    }

    public class MyCredits
    {
        public List<MyCreditsDepartment> Departments = new List<MyCreditsDepartment>();
        public List<MyCreditsNotice> CreditNotices = new List<MyCreditsNotice>();
    }
}
