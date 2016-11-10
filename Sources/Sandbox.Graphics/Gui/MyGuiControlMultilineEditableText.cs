using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlMultilineEditableLabel))]
    public class MyGuiControlMultilineEditableText : MyGuiControlMultilineText
    {
        int m_previousTextSize = 0;
        List<int> m_lineInformation = new List<int>();
        List<string> m_undoCache = new List<string>();
        List<string> m_redoCache = new List<string>();

        const int TAB_SIZE = 4;
        const int MAX_UNDO_HISTORY = 50;
        const char NEW_LINE = '\n';
        const char BACKSPACE = '\b';
        const char TAB = '\t';
        const char CTLR_Z = (char)26;
        const char CTLR_Y = (char)25;

        int m_currentCarriageLine = 0;
        int m_previousCarriagePosition = 0;
        float m_fontHeight = 0.0f;

        int m_currentCarriageColumn = 0;

        public MyGuiControlMultilineEditableText(
            Vector2? position = null,
            Vector2? size = null,
            Vector4? backgroundColor = null,
            string font = MyFontEnum.Blue,
            float textScale = MyGuiConstants.DEFAULT_TEXT_SCALE,
            MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
            StringBuilder contents = null,
            bool drawScrollbar = true,
            MyGuiDrawAlignEnum textBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            int? visibleLinesCount = null,
            MyGuiCompositeTexture backgroundTexture = null,
            MyGuiBorderThickness? textPadding = null
        )
            : base(position, size, backgroundColor, font, textScale, textAlign, contents, drawScrollbar, textBoxAlign, visibleLinesCount, true, backgroundTexture: backgroundTexture, textPadding: textPadding)
        {
            m_fontHeight = MyGuiManager.GetFontHeight(Font, TextScaleWithLanguage);
            this.AllowFocusingElements = false;
        }

        override public StringBuilder Text
        {
            get { return m_text; }
            set
            {
                m_lineInformation.Clear();
                m_text.Clear();
                if (value != null)
                    m_text.AppendStringBuilder(value);
                BuildLineInformation();
                RefreshText(false);

            }
        }
        protected override float ComputeRichLabelWidth()
        {
            return float.MaxValue;
        }

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase baseResult = base.HandleInput();

            if (HasFocus && Selectable)
            {
                //Cut
                if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.X) && MyInput.Static.IsAnyCtrlKeyPressed())
                {             
                    AddToUndo(m_text.ToString());
                    m_selection.CutText(this);
                    m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                    m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    return this;
                }

                //Paste
                if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.V) && MyInput.Static.IsAnyCtrlKeyPressed())
                {
                    AddToUndo(m_text.ToString());
                    m_selection.PasteText(this);
                    m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                    m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    return this;
                }
            
                //  Move home
                if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.Home))
                {
                    int startOfLineIndex = GetLineStartIndex(CarriagePositionIndex);
                    int lineIndex = startOfLineIndex;

                    //offset carriage to first letter of the line
                    while (lineIndex < Text.Length && Text[lineIndex]  == ' ')
                    {
                        lineIndex++;
                    }

                    // Alternate between the first letter of the line and the actual start position of the line
                    if (CarriagePositionIndex == lineIndex || lineIndex == Text.Length)
                        CarriagePositionIndex = startOfLineIndex;
                    else
                        CarriagePositionIndex = lineIndex;
                    
                    if (MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        CarriagePositionIndex = 0;
                    }
                    
                    if (MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        m_selection.SetEnd(this);
                    }
                    else
                    {
                        m_selection.Reset(this);
                    }
                    m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    return this;
                }

                //  Move end
                if (m_keyThrottler.IsNewPressAndThrottled(MyKeys.End))
                {
                    int lineIndex = GetLineEndIndex(CarriagePositionIndex);
                    CarriagePositionIndex = lineIndex;
                    if (MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        CarriagePositionIndex = Text.Length;
                    }
                    
                    if (MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        m_selection.SetEnd(this);
                    }
                    else
                    {
                        m_selection.Reset(this);
                    }
                    m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    return this;
                }

                if (MyInput.Static.IsKeyPress(MyKeys.Left) || MyInput.Static.IsKeyPress(MyKeys.Right))
                {
                    m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                }
            }
            HandleTextInputBuffered(ref baseResult);
            return baseResult;
        }

        protected void HandleTextInputBuffered(ref MyGuiControlBase ret)
        {
            bool textChanged = false;
            foreach (var character in MyInput.Static.TextInput)
            {
                if (Char.IsControl(character))
                {
                    if (character == CTLR_Z)
                    {
                        Undo();
                        m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                        m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    }
                    else if (character == CTLR_Y)
                    {
                        Redo();
                        m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                        m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    }
                    else if (character == BACKSPACE)
                    {
                        AddToUndo(m_text.ToString());
                        if (m_selection.Length == 0)
                        {
                            ApplyBackspace();
                        }
                        else
                        {
                            m_selection.EraseText(this);            
                        }
                        m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                        m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                        textChanged = true;
                    }
                    else if (character == '\r')
                    {
                        AddToUndo(m_text.ToString());
                        InsertChar(NEW_LINE);
                        textChanged = true;
                        m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                        m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                    }
                    else if (character == TAB)
                    {
                        m_currentCarriageLine = CalculateNewCarriageLine(CarriagePositionIndex);
                        m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                        AddToUndo(m_text.ToString());
                        var missingChars = TAB_SIZE - (m_currentCarriageColumn % TAB_SIZE);
                        for (int i = 0; i < missingChars; ++i)
                        {
                            InsertChar(' ');
                        }
                        textChanged = missingChars > 0;
                    }
                }
                else
                {
                    AddToUndo(m_text.ToString());
                    if (m_selection.Length > 0)
                    {
                        m_selection.EraseText(this);
                    }

                    InsertChar(character);
  
                    textChanged = true;
                }
            }

            // Unbuffered Delete because it's not delivered as a message through Win32 message loop.
            if (m_keyThrottler.GetKeyStatus(MyKeys.Delete) == ThrottledKeyStatus.PRESSED_AND_READY)
            {
                m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                AddToUndo(m_text.ToString());
                if (m_selection.Length == 0)
                    ApplyDelete();
                else
                    m_selection.EraseText(this);
                textChanged = true;
            }

            if (textChanged)
            {
                OnTextChanged();
                m_currentCarriageColumn = GetCarriageColumn(CarriagePositionIndex);
                ret = this;
            }
        }

        private void OnTextChanged()
        {
            BuildLineInformation();
            m_selection.Reset(this);
            m_label.Clear();
            AppendText(m_text);
        }

        private void InsertChar(char character)
        {
            m_text.Insert(CarriagePositionIndex, character);
            ++CarriagePositionIndex;
        }

        private void ApplyBackspace()
        {
            if (CarriagePositionIndex > 0)
            {
                --CarriagePositionIndex;
                m_text.Remove(CarriagePositionIndex, 1);
                BuildLineInformation();
            }
        }

        private void ApplyDelete()
        {
            if (CarriagePositionIndex < m_text.Length)
            {
                m_text.Remove(CarriagePositionIndex, 1);
            }
        }

        protected override Vector2 GetCarriageOffset(int idx)
        {
            if (m_lineInformation.Count == 0)
            {
                return new Vector2(0, 0);
            }
            else
            {
                int start = m_lineInformation[m_lineInformation.Count - 1];
                Vector2 output = new Vector2(0, -ScrollbarValue);
                int currentLine = 0;
                for (; currentLine < m_lineInformation.Count; ++currentLine)
                {
                    if (idx <= m_lineInformation[currentLine])
                    {
                        currentLine = Math.Max(0, --currentLine);
                        start = m_lineInformation[currentLine];
                        break;
                    }
                }
                if (idx - start > 0)
                {
                    m_tmpOffsetMeasure.Clear();
                    m_tmpOffsetMeasure.AppendSubstring(Text, start, idx - start);
                    output.X = MyGuiManager.MeasureString(Font, m_tmpOffsetMeasure, TextScaleWithLanguage).X;
                }
                output.Y = Math.Min(currentLine, m_lineInformation.Count - 1) * m_fontHeight - ScrollbarValue;  
                return output;
            }
        }

        protected override int GetLineStartIndex(int idx)
        {
            string text = Text.ToString();
            var output = text.Substring(0, idx).LastIndexOf(NEW_LINE) + 1;
            return (output == -1) ? 0 : output;
        }

        public int GetCurrentCarriageLine()
        {
            return m_currentCarriageLine;
        }

        private int CalculateNewCarriageLine(int idx)
        {
            BuildLineInformation();
            for (int currentLine = 1; currentLine < m_lineInformation.Count; ++currentLine)
            {
                if (idx <= m_lineInformation[currentLine])
                {
                    return Math.Max(0, currentLine);
                }
            }
            return m_lineInformation.Count;
        }

        public int MeasureNumLines(string text)
        {
            int numLines = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                if(text[i] == NEW_LINE)
                {
                    numLines++;
                }
            }
            return numLines;
        }

        public bool CarriageMoved()
        {
            if (m_previousCarriagePosition != m_carriagePositionIndex)
            {
                m_previousCarriagePosition = m_carriagePositionIndex;
                return true;
            }
            return false;
        }

        public int GetTotalNumLines()
        {
            return m_lineInformation.Count;
        }

        override protected int GetCarriagePositionFromMouseCursor()
        {
            Vector2 mouseRelative = MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft();
            mouseRelative.Y += this.ScrollbarValue;
            int closestIndex = 0;
            int currentLine = 0;
            for (currentLine = 0; currentLine < m_lineInformation.Count; ++currentLine)            
            {
                float lineMin = m_fontHeight * currentLine;
                if (mouseRelative.Y > lineMin && mouseRelative.Y < lineMin+m_fontHeight)
                {
                    int lenght = currentLine + 1 >= m_lineInformation.Count ? m_text.Length : m_lineInformation[currentLine + 1];
                    lenght -= m_lineInformation[currentLine];
                    int startPos = m_lineInformation[currentLine];
                    float closestDistance = float.MaxValue;
                    for (int j = 0; j < lenght; ++j)
                    {
                        m_tmpOffsetMeasure.Clear();
                        m_tmpOffsetMeasure.AppendSubstring(m_text, startPos, j+1);
                        float currentCharPos = MyGuiManager.MeasureString(Font, m_tmpOffsetMeasure, TextScaleWithLanguage).X;
                        Vector2 charPosition = new Vector2(currentCharPos, mouseRelative.Y);
                        float charDistance = Vector2.Distance(charPosition, mouseRelative);
                        if (charDistance < closestDistance)
                        {
                            closestDistance = charDistance;
                            closestIndex = startPos+j+1;
                        }
                   }
                    break; 
                }
            }
            
            m_currentCarriageColumn = GetCarriageColumn(closestIndex);
            m_currentCarriageLine = currentLine+1;
            return closestIndex;
        }

        void AddToUndo(string text, bool clearRedo = true)
        {
            if (clearRedo)
            {
                m_redoCache.Clear();
            }
            m_undoCache.Add(text);
            if (m_undoCache.Count > MAX_UNDO_HISTORY)
            {
                m_undoCache.RemoveAt(0);
            }
        }

        void AddToRedo(string text)
        {
            
            m_redoCache.Add(text);
            if (m_redoCache.Count > MAX_UNDO_HISTORY)
            {
                m_redoCache.RemoveAt(MAX_UNDO_HISTORY);
            }
        }

        void Undo()
        {
            if (m_undoCache.Count > 0)
            {
                int currentUndoIndex = UpdateCarriage(m_undoCache);
                AddToRedo(m_text.ToString());
                UpdateEditorText(currentUndoIndex,m_undoCache);
            }
        }

        void Redo()
        {
            if (m_redoCache.Count > 0)
            {
                int currentUndoIndex = UpdateCarriage(m_redoCache);
                CarriagePositionIndex -= 1;
                AddToUndo(m_text.ToString(),false);
                UpdateEditorText(currentUndoIndex, m_redoCache);
            }
        }

        int UpdateCarriage(List<string> array)
        {
            int currentIndex = array.Count - 1;
            int comparison = GetFirstDiffIndex(array[currentIndex], m_text.ToString());
            if (array[currentIndex].Length < m_text.Length)
                comparison--;//undo deletes character
            if (array[currentIndex].Length > m_text.Length)
                comparison++;
            CarriagePositionIndex = comparison == -1 ? array[currentIndex].Length : comparison;
            return currentIndex;
        }

        void UpdateEditorText(int currentIndex, List<string> array)
        {
            m_text.Clear();
            m_text.Append(array[currentIndex]);
            OnTextChanged();
            array.RemoveAt(currentIndex);
        }

        int GetFirstDiffIndex(string str1, string str2)
        {
            if (str1 == null || str2 == null) return -1;

            int length = Math.Min(str1.Length, str2.Length);

            for (int index = 0; index < length; index++)
            {
                if (str1[index] != str2[index])
                {
                    return index+1;
                }
            }

            return -1;
        }

        override protected int GetIndexUnderCarriage(int idx)
        {
            int end = GetLineEndIndex(idx);
            int newRowEnd = GetLineEndIndex(Math.Min(Text.Length, end + 1));
            int newRowStart = GetLineStartIndex(Math.Min(Text.Length, end + 1));
            return CalculateNewCarriagePos(newRowEnd, newRowStart);
        }

        private int CalculateNewCarriagePos(int newRowEnd, int newRowStart)
        {
            int newRowIndex = Math.Min(newRowEnd - newRowStart, m_currentCarriageColumn);
            int carriagePosition = newRowStart + newRowIndex;
            m_currentCarriageLine = CalculateNewCarriageLine(carriagePosition);
            return carriagePosition;
        }

        override protected int GetIndexOverCarriage(int idx)
        {
            int start = GetLineStartIndex(idx);
            int newRowEnd = GetLineEndIndex(Math.Max(0, start - 1));
            int newRowStart = GetLineStartIndex(Math.Max(0, start - 1));
            return CalculateNewCarriagePos(newRowEnd, newRowStart);
        }

        int GetCarriageColumn(int idx)
        {
            int start = GetLineStartIndex(idx);
            return idx - start;
        }
        void BuildLineInformation()
        {
            if (m_previousTextSize == m_text.Length)
            {
                return;
            }
            m_previousTextSize = m_text.Length;
            m_currentCarriageLine = 0;
            m_lineInformation.Clear();
            m_lineInformation.Add(0);
            for (int i = 0; i < m_text.Length; ++i)
            {
                if (m_text[i] == NEW_LINE)
                {
                    m_lineInformation.Add(i);
                }
            }
         }
    }
}
