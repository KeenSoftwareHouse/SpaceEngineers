using Sandbox.Common;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    class MyLoadingScreenQuote
    {
        public readonly MyStringId Text;
        public readonly MyStringId Author;

        public MyLoadingScreenQuote(MyStringId text, MyStringId author)
        {
            Text = text;
            Author = author;
        }

        public override string ToString()
        {
            return string.Format("'{0}' {1}", MyTexts.Get(Text), MyTexts.Get(Author));
        }

        #region Static
        private static MyLoadingScreenQuote[] m_quotes;
        
        static MyLoadingScreenQuote()
        {
            m_quotes = new MyLoadingScreenQuote[MyPerGameSettings.LoadingScreenQuoteCount];

            for (int i = 0; i < MyPerGameSettings.LoadingScreenQuoteCount; ++i)
            {
                var text   = MyStringId.GetOrCompute(string.Format("Quote{0:00}Text", i));
                var author = MyStringId.GetOrCompute(string.Format("Quote{0:00}Author", i));
                m_quotes[i] = new MyLoadingScreenQuote(text, author);
            }
        }

        public static MyLoadingScreenQuote GetQuote(int i)
        {
            i = MyMath.Mod(i, m_quotes.Length);
            return m_quotes[i];
        }

        public static MyLoadingScreenQuote GetRandomQuote()
        {
            return GetQuote(MyUtils.GetRandomInt(m_quotes.Length));
        }

        #endregion
    }
}
