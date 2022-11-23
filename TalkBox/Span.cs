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
        // The way this works is simpler and (in my opinion) cleaner than the Tag class.
        // However, the Tag class is a lot safer* and everything about a tag, such as what kind of values it accepts, is defined by the tag itself.
        // This class, on the other hand, will let you set the color to big, and color accepting hexadecimal values is hard coded.
        // However, making this brought me way less pain than making the Tag class.

        // *If we ignore the fact that not even I, the person who made it, fully understands how it works.
        [System.Flags]
        public enum Markup : UInt64
        {
            NONE = 0,

            // Non moving ones --------------
            BOLD = 0b_0000_0001,
            ITALIC = 0b_0000_0010,

            SIZE = 0b_0000_1100,

            // Moving -----------------------
            SHAKING = 0b_0000_0001_0000,

            // Color ------------------------
            //         RR GG BB AA
            COLOR = 0x_ff_ff_ff_ff_00_00_00_00,
        }

        [System.Flags]
        public enum MarkValue : uint
        {
            NONE = 0,

            BIG = 1,
            SMALL = 2,

            // Color ------------------------
            // RGBA        RR GG BB AA
            BLACK /**/ = 0x00_00_00_ff,
            RED /*  */ = 0xff_00_00_ff,
            GREEN /**/ = 0x00_ff_00_ff,
            BLUE /* */ = 0x00_00_ff_ff,
            WHITE /**/ = 0xff_ff_ff_ff,
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

        /// <summary>
        /// Get the normalised value of the data under <paramref name="mark"/>.
        /// </summary>
        /// <param name="mark">The area under which the value lies.</param>
        /// <returns></returns>
        public MarkValue GetValue(Markup mark)
        {
            UInt64 area = (UInt64)mark;
            UInt64 value = (UInt64)markup & (UInt64)mark;
            UInt64 move = (area - 1) ^ (area | (area - 1)); // This only leaves the smallest 1 in the binary sequence of the area tag.

            UInt64 movedValue = value / move; // And this moves the value to the position of that last 1.

            return (MarkValue)movedValue;
        }

        // The color values.
        public int red /*  */ { get { return (int)GetValue((Markup)0xff_00_00_00_00_00_00_00); } }
        public int green /**/ { get { return (int)GetValue((Markup)0x00_ff_00_00_00_00_00_00); } }
        public int blue /* */ { get { return (int)GetValue((Markup)0x00_00_ff_00_00_00_00_00); } }
        public int alpha /**/ { get { return (int)GetValue((Markup)0x00_00_00_ff_00_00_00_00); } }
    }

    class SpanCollection : IEnumerable
    {
        private List<MetaSpan> spans = new List<MetaSpan>();
        private string txt = string.Empty;

        public void Open(string input, int index)
        {
            string markName = input.ToUpper();

            Span.Markup markup;
            Span.MarkValue markValue = Span.MarkValue.NONE;

            int split = input.IndexOf('=');
            if (split != -1)
            {
                string extra = markName.Substring(split + 1);
                markName = markName.Substring(0, split);

                if (!Enum.TryParse<Span.Markup>(markName, out markup) || ((UInt64)markup & ((UInt64)markup - 1)) == 0)
                    throw new ArgumentException($"{markName} is not the name of a markup that needs a value");


                if (!Enum.TryParse<Span.MarkValue>(extra, out markValue))
                {
                    // Hard coded way of giving it a color value in hexadecimal.
                    if (markup == Span.Markup.COLOR)
                    {
                        uint color = 0;
                        try { color = Convert.ToUInt32("0x" + extra.ToLower(), 16); }
                        catch { throw new ArgumentException($"{extra} is neither a markup value or a hexadecimal value"); }

                        switch (extra.Length)
                        {
                            case 2: // Grayscale with max alpha.
                                markValue = (Span.MarkValue)(color * 0x01_01_01_00 + 0xff);
                                break;
                            case 4: // Grayscale with decided alpha.
                                markValue = (Span.MarkValue)((color >> 8) * 0x01_01_01_00 + (color & 0x00_ff));
                                break;
                            case 6: // RGB with max alpha.
                                markValue = (Span.MarkValue)(color * 0x00_00_01_00 + 0xff);
                                break;
                            case 8: // RGB with decided alpha.
                                markValue = (Span.MarkValue)color;
                                break;
                            default:
                                throw new ArgumentException($"{extra} is not a valid hexadecimal value");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"{extra} is not a markup value");
                    }
                }
            }
            else
            {
                if (!Enum.TryParse<Span.Markup>(markName, out markup) || ((UInt64)markup & ((UInt64)markup - 1)) != 0)
                    throw new ArgumentException($"{markName} is not the name of a markup that doesn't need a value");
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
            string markName = markup.ToUpper();
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

        Span.Markup MoveValue(Span.Markup markup, Span.MarkValue markValue)
        {
            UInt64 area = (UInt64)markup;
            uint value = (uint)markValue;
            UInt64 move = (area - 1) ^ (area | (area - 1)); // This only leaves the smallest 1 in the binary sequence of the area tag.

            UInt64 movedValue = value * move; // And this moves the value to the position of that last 1.
            return (Span.Markup)movedValue;
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
                Span.Markup type = Span.Markup.NONE;

                bool colored = false;
                foreach (MetaSpan s in spans)
                {
                    if (index >= s.start && index < s.end)
                    {
                        if (s.markup == Span.Markup.COLOR)
                        {
                            colored = true;
                        }

                        type |= MoveValue(s.markup, s.value);
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
                
                // I would really want a better way of having a default colour, but I don't want to do more of this after the Tag class.
                if (!colored)
                {
                    type |= type |= MoveValue(Span.Markup.COLOR, Span.MarkValue.BLACK);
                }
                Span span = new Span(type, txt.Substring(index, end - index));
                yield return span;
                index = end;
            }
        }
    }
}