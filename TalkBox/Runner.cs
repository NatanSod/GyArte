using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    class DialogueRunner
    {
        // When getting all options for choices, "scout ahead" for options with equal level and stop when a line with lower level or equal level without an option is found.
        int line = 0;
        int level = 0;
        bool gettingLine = false;
        bool waitingOptions = false;
        bool waitingCommand = false;
        string nodeName;
        Node currentNode;
        Dictionary<string, Node> nodes;
        CommandManager cm;
        ILineOutput lineOutput;
        IEnumerator lineGetter;

        List<TLine> options = new List<TLine>();

        enum Mode
        {
            Text, // The only mode that doesn't skip lines to find the correct one. Only the top should be in Text mode. (otherwise it's broken)
            Option, // This level is looking for when this level ends, and will go to the lower level, or for a non-option on this level, which will then go into Text mode.
            True, // It is looking for an endif. It will then go back to Text mode.
            False, // It is looking for an elseif or else, which it will try to run. If it instead finds an endif, it will then go back to Text mode.
        }

        private List<Mode> hierarchy = new List<Mode>();

        public DialogueRunner(ILineOutput output, CommandManager commandManager, string dialogueName, string node = "Start")
        {
            cm = commandManager;
            lineOutput = output;
            nodes = Parser.Make(dialogueName);
            nodeName = node;
            currentNode = nodes[nodeName];
            if (currentNode == null)
            {
                throw new ArgumentException($"{node} could not be found in {dialogueName}");
            }
            hierarchy.Add(Mode.Text);
            lineGetter = GetLineE();
        }

        public void Start()
        {
            lineOutput.Start();
        }

        private void GoDown()
        {
            // Each Mode function ends on the first line after the mode ended.
            // If that line is still lower than the level then we need to keep going.
            while (level > currentNode.lines[line].level)
            {
                // Remove the level that is being left.
                hierarchy.RemoveAt(level);
                level--;
                // Go to the end of the current mode.
                switch (hierarchy[level])
                {
                    case Mode.Option:
                        OptionMode();
                        break;
                    case Mode.True:
                        TrueMode();
                        break;
                    case Mode.Text:
                    case Mode.False:
                        throw new InvalidOperationException("Something went wacky with the hierarchy");
                }
                if (line >= currentNode.lines.Length) return;
            }

            if (hierarchy[level] != Mode.Text)
            {
                throw new InvalidOperationException("It should be in text mode right now. Why is it not in text mode?");
            }
        }

        private void OptionMode() // Should only be called through GoDown()
        {
            // CHECK IF THE CURRENT LINE IS VALID AS WELL!
            Line currentLine = currentNode.lines[line];
            while (true)
            {
                // End if the current line's level is lower.
                if (currentLine.level < level)
                {
                    break;
                }
                // or if it's a non option on the same level.
                else if (currentLine.level == level && currentLine.type != Line.LineType.Option)
                {
                    // Since this level continues, also enter text mode.
                    hierarchy[level] = Mode.Text; // I guess I could just make this one if do this in both, but this feels clearer.
                    break;
                }
                line++;

                if (line == currentNode.lines.Length) return; // We don't want it to keep going beyond the current node, you know

                currentLine = currentNode.lines[line];
            }
        }

        private void TrueMode() // Should only be called through GoDown()
        {
            // CHECK IF THE CURRENT LINE IS VALID AS WELL!
            Line currentLine = currentNode.lines[line];
            while (true)
            {
                if (currentLine.level == level && currentLine.type == Line.LineType.If)
                {
                    IfLine fLine = currentLine as IfLine ?? throw new Exception();
                    if (fLine.it == IfLine.IfType.End)
                    {
                        hierarchy[level] = Mode.Text;
                        break;
                    }
                }
                line++;
                if (line == currentNode.lines.Length) throw new InvalidOperationException("TrueMode() couldn't exit before the node ended");
                currentLine = currentNode.lines[line];
            }
            line++;
        }

        private void FalseMode() // Should only be called through GetLineE()
        {
            // CHECK IF THE CURRENT LINE IS VALID AS WELL!
            Line currentLine = currentNode.lines[line];
            while (true)
            {
                if (currentLine.level == level && currentLine.type == Line.LineType.If)
                {
                    IfLine fLine = currentLine as IfLine ?? throw new Exception();
                    if (fLine.it == IfLine.IfType.End)
                    {
                        hierarchy[level] = Mode.Text;
                        break;
                    }
                    else if (fLine.it == IfLine.IfType.If)
                    {
                        throw new InvalidOperationException("While in FalseMode(), something needs to go very wrong to find an If on the same level");
                    }
                    else if (fLine.Evaluate())
                    {
                        hierarchy[level] = Mode.True;
                        hierarchy.Add(Mode.Text);
                        level++;
                        break;
                    }
                }
                line++;
                if (line == currentNode.lines.Length) throw new InvalidOperationException("FalseMode() couldn't exit before the node ended");
                currentLine = currentNode.lines[line];
            }
            // It should not 'line++;' because it will be called in a situation where that will already happen.
        }

        private void ScoutOptions()
        {
            int scoutLine = line;

            Line currentLine = currentNode.lines[scoutLine];
            while (currentLine.level >= level)
            {
                if (currentLine.type == Line.LineType.Option && currentLine.level == level)
                {
                    options.Add(currentLine as TLine ?? throw new Exception());
                }
                scoutLine++;

                // Don't scout too deep
                if (scoutLine == currentNode.lines.Length)
                {
                    break;
                }
                currentLine = currentNode.lines[scoutLine];
            }
        }

        public void NextLine() 
        {
            // This is to make sure that two lines won't be gotten at the same time.
            if (gettingLine) return;

            gettingLine = true;
            lineGetter.MoveNext();
            gettingLine = false;
        }

        public void FinnishCommand() 
        { 
            waitingCommand = false; 
            lineGetter.MoveNext(); 
        }

        private IEnumerator GetLineE()
        {
            while (line < currentNode.lines.Length)
            {
                Line currentLine = currentNode.lines[line];

                if (currentLine.level < level)
                {
                    GoDown();
                    if (line >= currentNode.lines.Length) break;
                    currentLine = currentNode.lines[line];
                }

                switch (currentLine.type)
                {
                    case Line.LineType.Dialogue: // Simply yield return a collection with the line to display.
                        TLine dLine = (TLine)currentLine;
                        lineOutput.DisplayLine(dLine);
                        yield return null;
                        break;

                    case Line.LineType.Option: // Go up a level and yield return a collection with the options to choose.
                        ScoutOptions();
                        waitingOptions = true;
                        lineOutput.DisplayOptions(options.ToArray());
                        while (waitingOptions) yield return null; // Don't do anything unless an option has been selected.
                        hierarchy[level] = Mode.Option;
                        hierarchy.Add(Mode.Text);
                        level++;
                        break;

                    case Line.LineType.If: // If this is being run, then it is in Text mode. 
                        IfLine iLine = currentLine as IfLine ?? throw new Exception();

                        // While in text mode, the only type of IfLine it should find is the If type, nothing else.
                        if (iLine.it != IfLine.IfType.If)
                            throw new InvalidOperationException($"An {iLine.it.ToString()} was encountered while in text mode");

                        if (iLine.Evaluate())
                        {
                            hierarchy[level] = Mode.True;
                            hierarchy.Add(Mode.Text);
                            level++;
                        }
                        else
                        {
                            hierarchy[level] = Mode.False;
                            line++; // We know the current line is an if, FalseMode doesn't need to check it.
                            FalseMode();
                        }
                        break;

                    case Line.LineType.Command:
                        CLine cLine = currentLine as CLine ?? throw new Exception();
                        if (cLine.c == "jump")
                        {
                            // I feel it's safer if jump is handled from in here.
                            Jump(cLine.a[0]);
                            continue; // This is just to avoid the 'line++;' so that it won't skip the first line.
                        }
                        else
                        {
                            waitingCommand = true;
                            cLine.Execute(cm, this);
                            while (waitingCommand) yield return null; // Don't do anything while waiting for an async command to finnish first.
                            break;
                        }
                }
                line++;
            }
            lineOutput.End();
        }

        public void PickOption(int option)
        {
            if (!waitingOptions)
            {
                throw new InvalidOperationException("No answer is expected at the current time");
            }
            else if (option < options.Count && option >= 0)
            {
                line = options[option].line;
                options.Clear();
                waitingOptions = false;
                lineOutput.OptionSelected();
            }
            else
            {
                throw new ArgumentException("An invalid option was picked");
            }
        }

        private void Jump(string node)
        {
            line = 0;
            level = 0;
            hierarchy.Clear();

            nodeName = node;
            currentNode = nodes[nodeName];
            if (currentNode == null)
            {
                throw new ArgumentException($"{node} could not be found jumped to, because it does not exist");
            }
            hierarchy.Add(Mode.Text);
        }

        // DialogueRunner keeps a list of what to do when a level is entered again. When going down a level, it removes everything above that level.
        // DialogueRunner knows what kind of line the current line is.
        // DialogueRunner returns a TLineCollection to be handled by the line's requester.
        // DialogueRunner checks what kind of if an IfLine is and decide weather or not to even evaluate an ifelse.
        // If and Ifelse lines evaluate themselves.
        // DialogueRunner handles the special properties of an ifelse and else.
        // When GetLineE() returns null when you know it isn't waiting for you to pick an option, keep calling it every frame,
        //      because that means it's waiting on an async function and it only continues those when called.
    }
}