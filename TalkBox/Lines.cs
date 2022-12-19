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

            // Command: "<<[Command] [Arguments?]>>"
            string commandPattern = @"^<<([^\s]*)\s*(.*?)>>$";
            m = Regex.Match(line, commandPattern, RegexOptions.IgnoreCase);
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
    }

    // A line that contains text in the form of dialogue or an option to be displayed
    class TLine : Line
    {
        // TODO: Maybe make a version of GetLine() for the speaker/subject (s).
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

        private SpanCollection? markups = null;

        public TLine(string text, string? speaker, bool isOption, int indent, int nr, string[]? tags)
        {
            type = isOption ? LineType.Option : LineType.Dialogue;
            level = indent;
            line = nr;
            t = text;
            s = speaker;

            string state;
            lineTags = new Tag(tags, out state);
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

        public SpanCollection GetLine()
        {
            if (markups != null) return markups;
            markups = new SpanCollection();
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
                    else throw new InvalidOperationException(@$"'\{next}' is not valid");
                }
                else if (current == '{')
                {
                    string pattern = @"^(.{" + i + @"})\{([^\}]+)\}(.*)$";

                    Match m = Regex.Match(text, pattern);
                    if (m.Success)
                    {
                        string replacement = Variables.Calculate(m.Groups[2].Value).GetValue<string>();
                        text = m.Groups[1].Value + replacement + m.Groups[3].Value;
                    }
                    else
                    {
                        throw new InvalidOperationException("Format your mid-line math properly");
                    }
                }
                else if (current == '[')
                {
                    string pattern = @"^(.{" + i + @"})\[([^\]]+)\](.*)$";
                    Match m = Regex.Match(text, pattern);
                    if (m.Success)
                    {
                        string markup = m.Groups[2].Value;
                        if (markup[0] == '/')
                        {
                            if (markup.Length == 1)
                            {
                                markups.Close(i);
                            }
                            else
                            {
                                markups.Close(markup.Substring(1), i);
                            }
                        }
                        else
                        {
                            markups.Open(markup, i);
                        }
                        text = m.Result("$1$3");
                    }
                    else
                    {
                        throw new InvalidOperationException("Format your markup properly");
                    }
                }
            }
            markups.SetString(text);
            return markups;
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
        // The pattern used to split arguments.
        // It will keep strings and and equations in the same item in the resulting array.
        static string ArgumentPattern
        {
            // This thing is so long and winding that I feel it needs a more in-depth explanation.
            get =>
                // First, a positive lookbehind, meaning the match within must succeed in order for it to split. 
                // It starts either at the beginning of the string, or the previous split point.
                // This means everything since the last split must match the pattern.
                @"(?<=\G" +
                    // A non capturing group. If it was capturing then it would also add what it captured to the array.
                    "(?:" +
                        // For it to succeed it must contain a PAIR of non escaped quotation marks.
                        "(?:\\\"|[^\"])*\"(?:\\\"|[^\"])*[^\\\\]\"" +
                    // And it can succeed any number of times. (Including 0.)
                    ")*" +
                    // Then make sure there are no more quotation marks between here and the splitting point.
                    "(?:\\\"|[^\"])*" +
                // And this is the end of the lookbehind.
                ")" +
                // However, here is another. It makes sure that the symbol in front of the split point isn't a math symbol.
                @"(?<=[^\+\-\*/])" +
                // This is the split point. It needs to be at least one blank space, and each adjacent space will be lost to the aether.
                @"\s+" +
                // A positive look ahead, this one making sure the symbol right after isn't a math symbol either.
                @"(?=[^\+\-\*/])";

            // TLDR; if the pattern was just \s+ then it would split the below string value -
            // first "second third" + fourth fifth
            //      |       |      | |      |
            //      1       2      3 4      5
            // - in positions 1, 2, 3, 4, and 5.
            // That would result in strings being split apart, as well as equations.
            // This pattern would only split it at 1 and 5.
            // The first lookbehind means it doesn't split at position 2.
            // The second lookbehind means it doesn't split at position 4.
            // The lookahead means it doesn't split at position 3.
        }

        public string c { get; private set; } // command
        public string? t { get; private set; } // target
        public string[] a { get; private set; } // arguments
        public CLine(string command, string arguments, int indent, int nr)
        {
            type = LineType.Command;
            level = indent;
            line = nr;
            c = command;

            if (command == string.Empty) throw new ArgumentException("There is no command in here");

            a = Regex.Split(arguments, ArgumentPattern);
        }

        public void Execute(CommandManager commandManager)
        {
            // The async commands need to check the mode of commandManager in case it's skipping or similar.

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
                    if (type != value.Type)
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
                if (Variables.Get(name).Type == Variable.vType.Bool)
                {
                    value = new Variable(Variables.Evaluate(evaluate).ToString(), Variable.vType.Bool);
                }
                else
                {
                    value = Variables.Calculate(evaluate);

                    // If it's a wacky assignment, like +=
                    if (assign[0] != '=')
                    {
                        evaluate = $"{name} {assign[0]} {value.Value}";
                        value = Variables.Calculate(evaluate);
                    }
                }
                Variables.Set(name, value);
            }
            else
            {
                // I have decided to make this work at a later date.
                // It was none of the default commands. Give it to the CommandHandler and hope it knows what to do.
                Variable[] arguments = new Variable[a.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = Variables.Calculate(a[i]);
                }

                // commandManager.Add(c, t, arguments);
            }
        }
    }
}
