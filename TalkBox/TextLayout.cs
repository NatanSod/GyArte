using System.Numerics;

namespace TalkBox
{
    /// <summary>
    /// A zero base index of the position a symbol should have in order to minimise the amount of line breaks mid word.
    /// Add the width of each symbol before accessing the positions as adding a symbol may change the result.
    /// To represent a space or line break, use null and -1 respectively.
    /// </summary>
    class TextLayout
    {
        public int maxWidth { get; private set; }
        int spaceWidth = 1; // The width of a single space.
        int symbolMargin = 0; // The distance between two symbols.


        List<int?> widths = new List<int?>(); // The width of each letter in this layout.
        List<Vector2> positions = new List<Vector2>(); // The positions of the words.
        bool compiled = true;

        public TextLayout(int maximumWidth)
        {
            maxWidth = maximumWidth;
        }
        public TextLayout(int maximumWidth, int spaceWidth)
        {
            maxWidth = maximumWidth;
            this.spaceWidth = spaceWidth;
        }
        public TextLayout(int maximumWidth, int spaceWidth, int letterSpacing)
        {
            maxWidth = maximumWidth;
            this.spaceWidth = spaceWidth;
            this.symbolMargin = letterSpacing;
        }
        // I don't think these are very useful currently.
        public TextLayout(int maximumWidth, int?[] widths)
        {
            maxWidth = maximumWidth;
            this.widths = new List<int?>(widths);
            compiled = false;
        }
        public TextLayout(int maximumWidth, List<int?> widths)
        {
            maxWidth = maximumWidth;
            this.widths = new List<int?>(widths);
            compiled = false;
        }


        public void Clear()
        {
            widths.Clear();
            positions.Clear();
            compiled = true;
        }
        /// <summary>
        /// Add the width of a letter to the list of widths.
        /// </summary>
        public void Add(int? width)
        {
            widths.Add(width);
            compiled = false;
        }

        public Vector2 this[int i]
        {
            get
            {
                if (!compiled)
                {
                    Compile();
                }

                if (positions.Count <= i)
                {
                    return -Vector2.One;
                }

                return positions[i];
            }
        }

        // First, add each symbol's width as a multiplier of the width of a space, spaces represented as null and line breaks as -1.
        // Then, call a method that organises all of them into a position.
        private void Compile()
        {
            int column = 0; // The horizontal position of the first letter in the current word.
            int line = 0; // The line that the word is to be added to.
            int width = 0; // The width of the current word.
            List<int> currentWord = new List<int>(); // widths of the current letters.

            void CompileCurrentWord()
            {
                if (maxWidth != 0 && column != 0 && width + column > maxWidth)
                {
                    // Begin a new line if the current word is too wide for this line.
                    column = 0;
                    line++;
                }

                // Add the values in currentWord as correct relative positions.
                foreach (int currentLetter in currentWord)
                {
                    if (maxWidth != 0 && column + currentLetter > maxWidth)
                    {
                        // The word is longer than a single line, it needs to continue on the next line.
                        column = 0;
                        line++;
                    }
                    positions.Add(new Vector2(column, line));
                    column += currentLetter;
                }
                currentWord.Clear();
                width = 0;
            }

            foreach (int? w in widths)
            {
                // If w is null, it's a space.
                // If w is -1, it's a line break.
                // If it's neither of those, it's a letter.
                if (w != null && w != -1)
                {
                    width += symbolMargin + w ?? throw new Exception("This CAN'T ever happen, but it makes the IDE happy.");
                    currentWord.Add(symbolMargin + (int)w);
                }
                else
                {
                    // It's a space or a line break.
                    CompileCurrentWord();
                    if (w == -1)
                    {
                        // Line breaks begin a new line.
                        column = 0;
                        line++;
                    }
                    else if (column != 0)
                    {
                        // Don't add a space if this line is empty.
                        column += symbolMargin + spaceWidth;
                    }
                    positions.Add(-Vector2.One); // the position of "empty" symbols are always (-1, -1).
                }
            }
            // There are probably some widths left in currentWord, so take care of them.
            CompileCurrentWord();
            compiled = true;
        }
    }
}