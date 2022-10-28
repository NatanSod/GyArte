using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    class MetaSpan
    {
        public Span.Markup markup { get; private set; }
        public Span.MarkValue value { get; private set; }


        public int start { get; private set; }
        public int end { get; private set; }
        public int length { get { return end - start; } }

        public bool closed { get; private set; }

        public MetaSpan(Span.Markup markupType, Span.MarkValue markValue, int index)
        {
            markup = markupType;
            value = markValue;

            start = index;
            closed = false;
        }

        public void End(int index)
        {
            end = index;
            closed = true;
        }
    }

    class Span
    {
        [System.Flags]
        public enum Markup : uint
        {
            None = 0,

            // Non moving ones -----------
            Bold = 0b_0000_0001,
            Italic = 0b_0000_0010,

            Size = 0b_0000_1100,

            Color = 0b_1111_0000_0000,

            // Moving -----------------------
            Shaking = 0b_0000_0001_0000_0000_0000_0000,
        }

        [System.Flags]
        public enum MarkValue : uint
        {
            None = 0,

            Big = 1,
            Small = 2,

            Red = 1,
            Green = 2,
            Blue = 3,
            Rainbow = 15,
        }

        public Markup markup { get; private set; }
        public string contents { get; private set; }

        public Span(Markup markupType, string text)
        {
            markup = markupType;
            contents = text;
        }

        public bool Get(Markup mark)
        {
            return (mark & markup) != 0;
        }

        public MarkValue GetValue(Markup mark)
        {
            uint area = (uint)mark;
            uint value = (uint)markup & (uint)mark;
            uint move = (area - 1) ^ (area | (area - 1)); // This only leaves the smallest 1 in the binary sequence of the area tag.

            uint movedValue = value / move; // And this moves the value to the position of that last 1.

            return (MarkValue) movedValue;
        }
    }

    class SpanCollection : IEnumerable
    {
        private List<MetaSpan> spans = new List<MetaSpan>();
        private string txt = string.Empty;

        public void Open(string input, int index)
        {
            string markName = input.ToUpper();

            Span.Markup markup;
            Span.MarkValue markValue = Span.MarkValue.None;

            int split = input.IndexOf('=');
            if (split != -1)
            {
                string extra = markName[split + 1] + markName.Substring(split + 2).ToLower();
                markName = markName[0] + markName.Substring(1, split - 1).ToLower();

                if (!Enum.TryParse<Span.Markup>(markName, out markup) || ((uint)markup & ((uint)markup - 1)) == 0)
                    throw new ArgumentException($"{markName} is not the name of a markup that needs a value");

                if (!Enum.TryParse<Span.MarkValue>(extra, out markValue))
                    throw new ArgumentException($"{extra} is not a markup value");
            }
            else
            {
                if (!Enum.TryParse<Span.Markup>(markName, out markup) || ((uint)markup & ((uint)markup - 1)) != 0)
                    throw new ArgumentException($"{markName} is not the name of a markup that doesn't need a value");
            }

            Span.Markup type;
            if (!Enum.TryParse<Span.Markup>(markName, out type))
            {
                throw new ArgumentException($"{markName} is not a markup that exists");
            }

            foreach (MetaSpan span in spans)
            {
                if (span.markup == markup && !span.closed)
                {
                    throw new ArgumentException($"Don't open a new {markup.ToString()} span when another still hasn't been closed");
                }
            }

            spans.Add(new MetaSpan(markup, markValue, index));
        }


        public void Close(string markup, int index)
        {
            string markName = markup[1].ToString().ToUpper() + markup.Substring(2).ToLower();
            Span.Markup type;
            if (!Enum.TryParse<Span.Markup>(markName, out type))
            {
                throw new ArgumentException($"{markName} is not a markup that exists");
            }
            Close(type, index);
        }

        public void Close(Span.Markup markup, int index)
        {
            foreach (MetaSpan span in spans)
            {
                if (span.markup == markup && !span.closed)
                {
                    span.End(index);
                    return;
                }
            }
            throw new ArgumentException($"There is no {markup.ToString()} span that can be closed");
        }

        public void Close(int index)
        {
            foreach (MetaSpan span in spans)
            {
                if (!span.closed)
                {
                    span.End(index);
                }
            }
        }

        public void SetString(string text)
        {
            txt = text;
        }

        public IEnumerator GetEnumerator()
        {
            foreach (MetaSpan span in spans)
            {
                if (!span.closed) throw new ArgumentException("You need to close every tag");
            }
            int index = 0;
            while (index < txt.Length)
            {
                int end = txt.Length;
                Span.Markup type = Span.Markup.None;

                foreach (MetaSpan s in spans)
                {
                    if (index >= s.start && index < s.end)
                    {
                        uint area = (uint)s.markup;
                        uint value = (uint)s.value;
                        uint move = (area - 1) ^ (area | (area - 1)); // This only leaves the smallest 1 in the binary sequence of the area tag.

                        uint movedValue = value * move; // And this moves the value to the position of that last 1.

                        type |= (Span.Markup)movedValue;
                        if (end > s.end)
                        {
                            end = s.end;
                        }
                    }
                    else if (index < s.start && end > s.start)
                    {
                        end = s.start;
                    }
                }
                Span span = new Span(type, txt.Substring(index, end - index));
                yield return span;
                index = end;
            }
        }
    }
}