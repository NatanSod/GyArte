using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    // Options work in simple contexts.
    // if, ifelse, else, and endif work in simple contexts.
    // Everything directly related to variables probably works.
    // Commands that have been added and work. (Including jump.)

    // Some more thorough testing of flow control might be good.

    // TODO: Add the async commands

    abstract class Line
    {
        public enum LineType
        {
            Dialogue,
            Option,
            If,
            Command,
        }
        public LineType type { get; protected set; }
        public int level { get; protected set; }
        public int line { get; protected set; }

        public static Line? ParseLine(string line, int nr)
        {
            Match m = Regex.Match(line, @"(\s*)(.*)");

            string text = m.Groups[2].Value.Trim();
            if (text == String.Empty) return null;

            int level = m.Groups[1].Value.Length >> 2;

            // If: "<<if [Statement]>>" | "<<elseif [Statement]>>" | "<<else>>" | "<<endif>>"
            string ifPattern = @"^<<(if|elseif|else|endif)\b\s*(.*?)>>$";
            m = Regex.Match(text, ifPattern);
            if (m.Success)
            {
                string ifType = m.Groups[1].Value;
                string statement = m.Groups[2].Value;
                return new IfLine(ifType, statement, level, nr);
            }

            // Command: "<<[Command]>>"
            string commandPattern = @"^<<(\w*)\s*(.*?\s*)>>$";
            m = Regex.Match(line, commandPattern);
            if (m.Success)
            {
                string command = m.Groups[1].Value;
                string arguments = m.Groups[2].Value.Trim();
                return new CLine(command, arguments, level, nr);
            }

            // At this point, it's either text/dialogue or an option.
            // And as such, it might have tags, so get those out.
            string[]? tags = SeparateTags(ref text);


            // Option: "-> [text]"
            string optionPattern = @"^->\s*(.*?)\s*$";
            m = Regex.Match(text, optionPattern);
            if (m.Success)
            {
                text = m.Groups[1].Value;
                // Now that the '->' is removed, get the subject out of there.
                string? subject = SeparateSpeaker(ref text);
                return new TLine(text, subject, true, level, nr, tags);
            }

            // Dialogue: "[text]"
            string? speaker = SeparateSpeaker(ref text);
            return new TLine(text, speaker, false, level, nr, tags);
        }

        static private string[]? SeparateTags(ref string text)
        {
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == '#')
                {
                    // Check the amount of '\' before the '#'
                    int backSlashes = 0;
                    while (i - 1 - backSlashes >= 0 && text[i - 1 - backSlashes] == '\\')
                    {
                        backSlashes++;
                    }

                    // If it's even then it doesn't naturalize the '#'
                    if (backSlashes % 2 == 0)
                    {
                        string tags = text.Substring(i + 1);
                        text = text.Substring(0, i);
                        return Regex.Split(tags, @"\s+\#");
                    }
                }
            }
            return null;
        }

        static private string? SeparateSpeaker(ref string text)
        {
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == ':')
                {
                    // Check the amount of '\' before the ':'
                    int backSlashes = 0;
                    while (i - 1 - backSlashes >= 0 && text[i - 1 - backSlashes] == '\\')
                    {
                        backSlashes++;
                    }

                    // If it's even then it doesn't naturalize the ':'
                    if (backSlashes % 2 == 0)
                    {
                        string speaker = text.Substring(0, i).Trim();
                        text = text.Substring(i + 1).Trim();
                        return speaker;
                    }
                }
            }

            return null;
        }

        // I think this is bad
        static private void UnSlash(ref string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                // This "solution" is stupid but I like it.
                char next = ' ';
                try { next = text[i + 1]; }
                catch (IndexOutOfRangeException e)
                {
                    Console.WriteLine(@"You can't use a '\' to neutralize nothing");
                    throw e;
                }

                // It's actually a line break.
                if (next == 'n') text = text.Substring(0, i) + '\n' + text.Substring(i + 2);
                // It's a neutralized '\'.
                else if (next == '\\') text = text.Substring(0, i) + text.Substring(i + 1);
                // It's invalid.
                else throw new InvalidOperationException(@$"'\{text[i + 1]}' is not valid");
            }
        }
    }

    // A line that contains text in the form of dialogue or an option to be displayed
    class TLine : Line
    {
        /// <summary>
        /// Text to display.
        /// </summary>
        public string t { get; private set; }
        /// <summary>
        /// The subject or speaker. null if there is none
        /// </summary>
        public string? s { get; private set; }
        public string statement { get; private set; } = String.Empty;
            
        public Tag lineTags { get; private set; }

        public TLine(string text, string? speaker, bool isOption, int indent, int nr, string[]? tags)
        {
            type = isOption ? LineType.Option : LineType.Dialogue;
            level = indent;
            line = nr;
            t = text;
            s = speaker;
            
            string state;
            lineTags = new Tag (tags, out state);
            statement = state;
        }

        public TLine(TLine tLine)
        {
            type = tLine.type;
            level = tLine.level;
            line = tLine.line;
            t = tLine.t;
            s = tLine.s;
            lineTags = tLine.lineTags;
            statement = tLine.statement;
        }

        public string GetLine()
        {
            string text = t;

            for (int i = 0; i < text.Length; i++)
            {
                // This "solution" is stupid but I like it.
                char current = text[i];
                if (current == '\\')
                {
                    char next = ' ';
                    try { next = text[i + 1]; }
                    catch (IndexOutOfRangeException e)
                    {
                        Console.WriteLine(@"You can't use a '\' to neutralize nothing");
                        throw e;
                    }

                    string neutralize = @"[]{}\:";

                    // It's actually a line break.
                    if (next == 'n') text = text.Substring(0, i) + '\n' + text.Substring(i + 2);
                    // It's a neutralized symbol.
                    else if (neutralize.Contains(next)) text = text.Substring(0, i) + text.Substring(i + 1);
                    // It's invalid.
                    else throw new InvalidOperationException(@$"'\{text[i + 1]}' is not valid");
                }
                else if (current == '{')
                {
                    string futureText = text.Substring(i);
                    string pattern = @"(?<=[^\\])\{(.*?[^\\])\}";

                    Match m = Regex.Match(futureText, pattern);
                    if (m.Success)
                    {
                        string replacement = Variables.Calculate(m.Groups[1].Value).GetValue<string>();
                        text = text.Substring(i) + replacement;
                    }
                    else
                    {
                        throw new InvalidOperationException("For each '{' there needs to be a closing '}' as well.");
                    }
                }
                // TODO: Add markup support
                else if (current == '[')
                {
                    throw new Exception("Markup support is currently unavailable");
                }
            }
            return text;
        }
    }

    // A iterable collection of text lines for the whatever displays the dialogue to display
    class TLineCollection : IEnumerable
    {
        public TLine[] lines { get; private set; }
        public TLine? line { get; private set; }
        public Line.LineType lineType { get; private set; }

        public TLineCollection(TLine tLine)
        {
            if (tLine.type != Line.LineType.Dialogue)
            {
                throw new ArgumentException("Don't give it a single option like that");
            }
            line = tLine;
            lineType = Line.LineType.Dialogue;
            lines = new TLine[0];
        }

        public TLineCollection(List<TLine> tLines)
        {
            line = null;
            lines = tLines.ToArray();
            lineType = Line.LineType.Option;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lineType != lines[i].type)
                {
                    throw new ArgumentException("A dialogue and option line should not be in the list together");
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            for (int index = 0; index < lines.Length; index++)
            {
                yield return new TLine(lines[index]);
            }
        }
    }

    // A line that contains an if statement or related
    class IfLine : Line
    {
        public enum IfType
        {
            If,
            IfElse,
            Else,
            End
        }

        public IfType it { get; private set; }
        string? s;

        public IfLine(string ifType, string statement, int indent, int nr)
        {
            type = LineType.If;
            level = indent;
            line = nr;
            s = statement != string.Empty ? statement : null;
            switch (ifType)
            {
                case "if":
                    it = IfType.If;
                    if (s == null) throw new ArgumentException("There is no statement here");
                    break;
                case "elseif":
                    it = IfType.IfElse;
                    if (s == null) throw new ArgumentException("There is no statement here");
                    break;
                case "else":
                    it = IfType.Else;
                    s = null;
                    break;
                case "endif":
                    it = IfType.End;
                    s = null;
                    break;
            }
        }


        public bool Evaluate()
        {
            if (s == null)
            {
                return true;
            }
            return Variables.Evaluate(s);
        }
    }

    // A line that contains a command to be executed 
    class CLine : Line
    {
        public string c { get; private set; }
        public string[] a { get; private set; }
        public CLine(string command, string arguments, int indent, int nr)
        {
            type = LineType.Command;
            level = indent;
            line = nr;
            c = command;

            if (command == string.Empty) throw new ArgumentException("There is no command in here");

            a = Regex.Split(arguments, @"\s+");
        }

        public void Execute()
        {
            // The 'jump' command is not here because Dialogue Runner handles that
            if (c == "declare")
            {
                string args = String.Join(' ', a);
                Match m = Regex.Match(args, @"(\$[\w\d]*)\b\s*?(to|[\+\-\*/]?=)\s*?([^\s].*?)\s*?as\s+(\w*)");
                if (!m.Success)
                {
                    throw new ArgumentException("The declare statement could not be parsed correctly");
                }
                string name = m.Groups[1].Value; // First is the name.
                string assign = m.Groups[2].Value == "to" ? "=" : m.Groups[2].Value; // Second it's the assignment type
                string evaluate = m.Groups[3].Value; // After that is the thing that needs to be evaluated

                // And last is the type.
                string t = m.Groups[4].Value[0].ToString().ToUpper() + m.Groups[4].Value.Substring(1).ToLower(); // First letter must be capitalized
                string[] types = Enum.GetNames<Variable.vType>(); // Get the options for what it could be
                int tInt = Array.IndexOf(types, t);
                if (tInt == -1)
                {
                    throw new ArgumentException("There is no type in this declaration");
                }
                Variable.vType type = (Variable.vType)tInt;


                Variable value;
                if (type == Variable.vType.Bool)
                {
                    value = new Variable(Variables.Evaluate(evaluate).ToString(), Variable.vType.Bool);
                }
                else
                {
                    value = Variables.Calculate(evaluate);
                    if (type != value.t)
                    {
                        throw new ArgumentException("The type assigned to this variable doesn't match the value it's defined as");
                    }
                }

                Variables.Define(name, value);
            }
            else if (c == "set")
            {
                string args = String.Join(' ', a);
                Match m = Regex.Match(args, @"(\$[\w\d]*)\b\s*?(to|[\+\-\*/]?=)\s*?([^\s].*?)\s*$");
                if (!m.Success)
                {
                    throw new ArgumentException("The set statement could not be parsed correctly");
                }
                string name = m.Groups[1].Value; // First is the name.
                string assign = m.Groups[2].Value == "to" ? "=" : m.Groups[2].Value; // Second it's the assignment type
                string evaluate = m.Groups[3].Value; // After that is the thing that needs to be evaluated

                // And the value is everything else except the assignment thingy.
                Variable value;
                if (Variables.Get(name).t == Variable.vType.Bool)
                {
                    value = new Variable(Variables.Evaluate(evaluate).ToString(), Variable.vType.Bool);
                }
                else
                {
                    value = Variables.Calculate(evaluate);

                    // If it's a wacky assignment, like +=
                    if (assign[0] != '=')
                    {
                        evaluate = $"{name} {assign[0]} {value.v}";
                        value = Variables.Calculate(evaluate);
                    }
                }
                Variables.Set(name, value);
            }
            else
            {
                throw new ArgumentException($"{c} is not a command");
            }
        }
    }
}
