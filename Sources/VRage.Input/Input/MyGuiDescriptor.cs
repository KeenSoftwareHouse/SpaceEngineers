using System.Text;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Library;

namespace VRage.Input
{
    public class MyGuiDescriptor
    {
        private const string LINE_SPLIT_SEPARATOR = " | ";
        private static readonly string[] LINE_SEPARATOR = new string[] { MyEnvironment.NewLine };

        private bool m_isDirty = true;
        protected StringBuilder m_name;
        protected StringBuilder m_description;

        private MyStringId? m_descriptionEnum;
        public MyStringId? DescriptionEnum
        {
            get { return m_descriptionEnum; }
            set
            {
                if (value != m_descriptionEnum)
                {
                    m_descriptionEnum = value;
                    m_isDirty = true;
                }
            }
        }

        private MyStringId m_nameEnum;
        public MyStringId NameEnum
        {
            get { return m_nameEnum; }
            set
            {
                if (value != m_nameEnum)
                {
                    m_nameEnum = value;
                    m_isDirty = true;
                }
            }
        }

        public StringBuilder Name
        {
            get
            {
                UpdateDirty();
                return m_name;
            }
        }

        public StringBuilder Description
        {
            get
            {
                UpdateDirty();
                return m_description;
            }
        }

        public MyGuiDescriptor(MyStringId name, MyStringId? description = null)
        {
            m_nameEnum = name;
            DescriptionEnum = description;
        }

        private void UpdateDirty()
        {
            if (m_isDirty)
            {
                m_name = MyTexts.Get(m_nameEnum);

                m_description.Clear();
                if (m_descriptionEnum.HasValue)
                {
                    MyUtils.SplitStringBuilder(m_description, MyTexts.Get(m_descriptionEnum.Value), LINE_SPLIT_SEPARATOR);
                }

                m_isDirty = false;
            }
        }
    }
}
