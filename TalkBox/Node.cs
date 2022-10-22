using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    // This class' existence is kind of stupid. Might get rid of it.
    class Node
    {
        public Line[] lines;

        public Node(Line[] lineArray)
        {
            lines = lineArray;
        }
        public Node(List<Line> lineList)
        {
            lines = lineList.ToArray();
        }
    }
}