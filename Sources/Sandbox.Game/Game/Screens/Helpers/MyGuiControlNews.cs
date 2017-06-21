using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using Sandbox.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using System.Xml.Serialization;
using Sandbox.Engine.Utils;
using VRage;
using VRage.Game;
using VRage.Game.News;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlNews : MyGuiControlBase
    {
        public enum StateEnum
        {
            Entries,
            Loading,
            Error,
        }

        private static StringBuilder m_stringCache = new StringBuilder(100);

        private List<MyNewsEntry> m_news;
        private int m_currentEntryIndex;
        private StateEnum m_state;

        private MyGuiControlLabel m_labelTitle;
        private MyGuiControlLabel m_labelDate;
        private MyGuiControlSeparatorList m_separator;
        private MyGuiControlMultilineText m_textNewsEntry;
        private MyGuiControlPanel m_backgroundPanel;
        private MyGuiControlPanel m_bottomPanel;
        private MyGuiControlLabel m_labelPages;
        private MyGuiControlButton m_buttonNext;
        private MyGuiControlButton m_buttonPrev;

        private MyGuiControlMultilineText m_textError;

        private MyGuiControlRotatingWheel m_wheelLoading;

        #region Downloading process

        Task m_downloadNewsTask;
        MyNews m_downloadedNews;
        XmlSerializer m_newsSerializer;
        bool m_downloadedNewsOK = false;
        bool m_downloadedNewsFinished = false;
        bool m_pauseGame = false;
        private static readonly char[] m_trimArray = new char[] {' ', (char) 13, '\r', '\n'};
        private static readonly char[] m_splitArray = new char[] {'\r', '\n'};

        #endregion


        public StateEnum State
        {
            get { return m_state; }
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    RefreshState();
                }
            }
        }

        public MyGuiControlNews():
            base(isActiveControl: true,
            canHaveFocus: false,
            allowFocusingElements: true)
        {
            m_news = new List<MyNewsEntry>();

            m_labelTitle = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP) {
                Name = "Title"
            };
            m_labelDate = new MyGuiControlLabel(
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP) {
                Name = "Date"
            };
            m_separator = new MyGuiControlSeparatorList() {
                Name = "Separator",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER
            };
            m_textNewsEntry = new MyGuiControlMultilineText(
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f,
                drawScrollbar: true)
            {
                Name = "NewsEntry",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
            };
            m_textNewsEntry.OnLinkClicked += OnLinkClicked;
            m_bottomPanel = new MyGuiControlPanel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                BackgroundTexture = MyGuiConstants.TEXTURE_NEWS_PAGING_BACKGROUND,
                Name = "BottomPanel",
            };
            m_labelPages = new MyGuiControlLabel(
                text: new StringBuilder("{0}/{1}  ").ToString(),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM) {
                Name = "Pages"
            };
            m_buttonPrev = new MyGuiControlButton(
                visualStyle: MyGuiControlButtonStyleEnum.ArrowLeft,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                onButtonClick: (b) => UpdateCurrentEntryIndex(-1)) {
                Name = "Previous"
            };
            m_buttonNext = new MyGuiControlButton(
                visualStyle: MyGuiControlButtonStyleEnum.ArrowRight,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
                onButtonClick: (b) => UpdateCurrentEntryIndex(+1)) {
                Name = "Next"
            };
            m_textError = new MyGuiControlMultilineText(
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                font: MyFontEnum.Red) {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                Name = "Error"
            };
            m_backgroundPanel = new MyGuiControlCompositePanel()
            {
                ColorMask = new Vector4(1f, 1f, 1f, 0.69f),
                BackgroundTexture = MyGuiConstants.TEXTURE_NEWS_BACKGROUND
            };

            m_wheelLoading = new MyGuiControlRotatingWheel(multipleSpinningWheels: MyPerGameSettings.GUI.MultipleSpinningWheels);

            Elements.Add(m_backgroundPanel);
            Elements.Add(m_labelTitle);
            Elements.Add(m_labelDate);
            Elements.Add(m_separator);
            Elements.Add(m_textNewsEntry);
            Elements.Add(m_bottomPanel);
            Elements.Add(m_labelPages);
            Elements.Add(m_buttonPrev);
            Elements.Add(m_buttonNext);
            Elements.Add(m_textError);
            Elements.Add(m_wheelLoading);

            if (false)
            {
                m_textNewsEntry.BorderEnabled = true;
                m_labelPages.BorderEnabled = true;
                m_bottomPanel.BorderEnabled = true;
                m_buttonPrev.BorderEnabled = true;
                m_buttonNext.BorderEnabled = true;
                m_textError.BorderEnabled = true;
                m_wheelLoading.BorderEnabled = true;
            }

            RefreshState();
            UpdatePositionsAndSizes();
            RefreshShownEntry();

            try
            {
                m_newsSerializer = new XmlSerializer(typeof(MyNews));
            }
            finally
            {
                DownloadNews();   
            }
        }

        void OnLinkClicked(MyGuiControlBase sender, string url)
        {
            Debug.Assert(sender == m_textNewsEntry);
            m_stringCache.Clear();
            m_stringCache.AppendFormat(MyTexts.GetString(MyCommonTexts.MessageBoxTextOpenBrowser), url);
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                messageText: m_stringCache,
                callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                {
                    if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        if (!MyBrowserHelper.OpenInternetBrowser(url))
                        {
                            StringBuilder sbMessage = new StringBuilder();
                            sbMessage.AppendFormat(MyTexts.GetString(MyCommonTexts.TitleFailedToStartInternetBrowser), url);
                            StringBuilder sbTitle = MyTexts.Get(MyCommonTexts.TitleFailedToStartInternetBrowser);
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageText: sbMessage,
                                messageCaption: sbTitle));
                        }
                }));
        }

        protected override void OnSizeChanged()
        {
            UpdatePositionsAndSizes();
            base.OnSizeChanged();
        }

        public override MyGuiControlBase HandleInput()
        {
            base.HandleInput();
            return HandleInputElements();
        }

        private void UpdatePositionsAndSizes()
        {
            float padding = 0.01f;
            float spacing = 0.005f;
            float posY = -0.5f * Size.Y + padding;
            float posXLeft = -0.5f * Size.X + padding;
            float posXRight = 0.5f * Size.X - padding;

            m_labelTitle.Position = new Vector2(posXLeft, posY);
            m_labelDate.Position = new Vector2(posXRight, posY);
            posY += Math.Max(m_labelTitle.Size.Y, m_labelDate.Size.Y) + spacing;

            m_separator.Size = Size;
            m_separator.Clear();
            m_separator.AddHorizontal(new Vector2(posXLeft, posY), posXRight - posXLeft);
            posY += spacing;

            m_textNewsEntry.Position = new Vector2(posXLeft, posY);

            m_buttonNext.Position = new Vector2(posXRight, 0.5f * Size.Y - padding);
            m_buttonPrev.Position = m_buttonNext.Position - new Vector2(m_buttonNext.Size.X + spacing, 0f);
            m_bottomPanel.Position = m_buttonNext.Position + new Vector2(0.009f, 0.008f);
            m_labelPages.Position = new Vector2(m_bottomPanel.Position.X - m_bottomPanel.Size.X + 2f*spacing,
                                                m_buttonPrev.Position.Y);

            m_textNewsEntry.Size = new Vector2(posXRight - posXLeft, (m_buttonNext.Position.Y - m_textNewsEntry.Position.Y));

            m_textError.Size = Size - 2f * padding;
            m_bottomPanel.Size = new Vector2(0.125f, m_buttonPrev.Size.Y + 0.015f);
            m_backgroundPanel.Size = Size;
        }

        internal void Show(MyNews news)
        {
            m_news.Clear();
            m_news.AddRange(news.Entry);
            m_currentEntryIndex = 0; // showing the newest entry by default
            RefreshShownEntry();
        }

        private void UpdateCurrentEntryIndex(int delta)
        {
            m_currentEntryIndex += delta;
            if (m_currentEntryIndex < 0)
                m_currentEntryIndex = 0;
            if (m_currentEntryIndex >= m_news.Count)
                m_currentEntryIndex = m_news.Count - 1;
            RefreshShownEntry();
        }

        private void RefreshShownEntry()
        {
            m_textNewsEntry.Clear();
            if (m_news.IsValidIndex(m_currentEntryIndex))
            {
                var entry = m_news[m_currentEntryIndex];
                m_labelTitle.Text = entry.Title;
                m_labelDate.Text = entry.Date;
                MyWikiMarkupParser.ParseText(entry.Text, ref m_textNewsEntry);
                //We need append some empty space because of pages label overlap
                m_textNewsEntry.AppendLine();
                m_labelPages.UpdateFormatParams(m_currentEntryIndex + 1, m_news.Count);
            }
            else
            {
                m_labelTitle.Text = null;
                m_labelDate.Text = null;
                m_labelPages.UpdateFormatParams(0, 0);
            }
        }

        private void RefreshState()
        {
            bool showingEntries = m_state == StateEnum.Entries;
            bool showingError   = m_state == StateEnum.Error;
            bool showingLoading = m_state == StateEnum.Loading;

            m_labelTitle.Visible    = showingEntries;
            m_labelDate.Visible     = showingEntries;
            m_separator.Visible     = showingEntries;
            m_textNewsEntry.Visible = showingEntries;
            m_labelPages.Visible    = showingEntries;
            m_bottomPanel.Visible   = showingEntries;
            m_buttonPrev.Visible    = showingEntries;
            m_buttonNext.Visible    = showingEntries;

            m_textError.Visible = showingError;

            m_wheelLoading.Visible = showingLoading;
        }

        public StringBuilder ErrorText
        {
            get { return m_textError.Text; }
            set { m_textError.Text = value; }
        }

        public void DownloadNews()
        {
            if (m_downloadNewsTask != null && !m_downloadNewsTask.IsCompleted)
            {
                Debug.Fail("Trying to download news before the previous download action was completed.");
                return;
            }
            State = StateEnum.Loading;

            m_downloadNewsTask = Task.Run(() => DownloadNewsAsync()).ContinueWith(task => DownloadNewsCompleted());
        }

        void DownloadNewsCompleted()
        {
            CheckVersion();

            if (m_downloadedNewsOK)
            {
                State = StateEnum.Entries;
                Show(m_downloadedNews);
            }
            else
            {
                State = StateEnum.Error;
                ErrorText = MyTexts.Get(MyCommonTexts.NewsDownloadingFailed);
            }
        }

        void DownloadNewsAsync()
        {
            try
            {
                var newsDownloadClient = new WebClient();
                newsDownloadClient.Proxy = null;

                Uri changeLogUri;
                if (MyFinalBuildConstants.IS_STABLE)
                    changeLogUri = new Uri(MyPerGameSettings.ChangeLogUrl);
                else
                    changeLogUri = new Uri(MyPerGameSettings.ChangeLogUrlDevelop);

                var downloadedNews = newsDownloadClient.DownloadString(changeLogUri);

                using (StringReader stream = new StringReader(downloadedNews))
                {
                    m_downloadedNews = (MyNews)m_newsSerializer.Deserialize(stream);

                    StringBuilder text = new StringBuilder();
                    for (int i = 0; i < m_downloadedNews.Entry.Count; i++)
                    {
                        var newsItem = m_downloadedNews.Entry[i];

                        string itemText = newsItem.Text.Trim(m_trimArray);
                        string[] lines = itemText.Split(m_splitArray);

                        text.Clear();
                        foreach (string lineItem in lines)
                        {
                            string line = lineItem.Trim();
                            text.AppendLine(line);
                        }

                        m_downloadedNews.Entry[i] = new MyNewsEntry()
                        {
                            Title = newsItem.Title,
                            Version = newsItem.Version,
                            Date = newsItem.Date,
                            Text = text.ToString(),
                        };
                    }

                    if (MyFakes.TEST_NEWS)
                    {
                        var entry = m_downloadedNews.Entry[m_downloadedNews.Entry.Count - 1];
                        entry.Title = "Test";
                        entry.Text = "ASDF\nASDF\n[www.spaceengineersgame.com Space engineers web]\n[[File:Textures\\GUI\\MouseCursor.dds|64x64px]]\n";
                        m_downloadedNews.Entry.Add(entry);
                    }
                    m_downloadedNewsOK = true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while downloading news: " + e.ToString());
            }
            finally
            {
                m_downloadedNewsFinished = true;
            }
        }

        void CheckVersion()
        {
            int latestVersion = 0;
            if (m_downloadedNews != null && m_downloadedNews.Entry.Count > 0)
            {
                if (int.TryParse(m_downloadedNews.Entry[0].Version, out latestVersion))
                {
                    if (latestVersion > MyFinalBuildConstants.APP_VERSION)
                    {
                        if (MySandboxGame.Config.LastCheckedVersion != MyFinalBuildConstants.APP_VERSION)
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageText: MyTexts.Get(MySpaceTexts.NewVersionAvailable),
                                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionInfo),
                                styleEnum: MyMessageBoxStyleEnum.Info));

                            MySandboxGame.Config.LastCheckedVersion = MyFinalBuildConstants.APP_VERSION;
                            MySandboxGame.Config.Save();
                        }
                    }
                }
            }
        }
    }
}
