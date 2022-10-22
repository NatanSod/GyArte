using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    static class Parser
    {
        public static Dictionary<string, Node> Make(string fileName)
        {
            Dictionary<string, Node> nodes = new Dictionary<string, Node>();

            string[] fileLines = File.ReadAllLines($"./{fileName}.yarn");
            for (int i = 0; i < fileLines.Length; i++)
            {
                // Find the title
                Match m = Regex.Match(fileLines[i], @"title:\s*(\w+)");
                if (!m.Success)
                {
                    continue;
                }

                string title = m.Groups[1].Value;

                // Go to where the metadata section ends
                while (fileLines[i].IndexOf("---") != 0)
                {
                    i++;
                }
                i++;
                int nodeLine = 0;

                // Add each line until the beginning of another node 
                List<Line> lines = new List<Line>();

                while (fileLines[i].IndexOf("===") != 0 && i < fileLines.Length)
                {
                    Line? parsedLine = Line.ParseLine(fileLines[i], nodeLine);
                    if (parsedLine != null)
                    {
                        lines.Add(parsedLine);
                    }
                    i++;
                    nodeLine++;
                }

                nodes.Add(title, new Node(lines));
            }
            return nodes;
        }
    }

}