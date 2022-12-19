using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TalkBox
{
    static class Variables
    {
        private static Dictionary<string, Variable> variables = new Dictionary<string, Variable>();

        public static void Define(string name, string value, Variable.vType type)
        {
            Variable variable = new Variable(value, type);
            variables[name] = variable;
        }

        public static void Define(string name, Variable value)
        {
            Variable variable = new Variable(value);
            variables[name] = variable;
        }

        public static void Set(string name, string value)
        {
            Variable? variable;
            if (!variables.TryGetValue(name, out variable)) throw new ArgumentException("Ay, that is not a variable that exists");

            variable.Set(value);
        }
        public static void Set(string name, Variable value)
        {
            Variable? variable;
            if (!variables.TryGetValue(name, out variable)) throw new ArgumentException("Ay, that is not a variable that exists");

            variable.Set(value);
        }

        public static Variable Get(string name)
        {
            Variable? variable;
            if (!variables.TryGetValue(name, out variable)) throw new ArgumentException("Ay, that is not a variable that exists");
            return new Variable(variable);
        }

        public static Variable Calculate(string s)
        {
            if (s == string.Empty) throw new ArgumentException("How do you expect us to calculate nothing?");
            string[] statement = Regex.Split(s, @"(?<=\G" +
                                                    "(?:" +
                                                        "(?:\\\"|[^\"])*" +
                                                        "\"" +
                                                        "(?:\\\"|[^\"])*" +
                                                        "[^\\\\]\"" +
                                                    ")*" +
                                                    "(?:\\\"|[^\"])*" +
                                                ")" +
                                                @"\s*([\+\-\*/])\s*");
            List<Variable> variables = new List<Variable>();
            List<char> operators = new List<char>();
            Variable.vType? statementType = null;

            for (int i = 0; i < statement.Length; i += 2)
            {
                string item = statement[i];
                char c = i + 1 < statement.Length ? statement[i + 1][0] : ')';

                // There should be an operator
                if (c != ')' && statement[i + 1].Length > 1)
                {
                    throw new InvalidOperationException("There must be an operator between each value");
                }

                bool success = false;

                operators.Add(c);
                // Get the variable
                Variable.vType type = Variable.vType.Number;
                if (item[0] == '$')
                {
                    Variable v = Variables.Get(item);
                    type = v.Type;
                    variables.Add(v);
                }
                else
                {
                    // Try to convert it until it works
                    for (int j = 0; j < Enum.GetNames<Variable.vType>().Length; j++)
                    {
                        type = (Variable.vType)j;
                        try
                        {
                            Variable v = new Variable(item, type);
                            variables.Add(v);
                            success = true;
                            break;
                        }
                        catch (ArgumentException e) when (e.Message.Contains("Ay, that is not a "))
                        {
                            continue;
                        }
                    }
                    if (success == false)
                    {
                        throw new InvalidOperationException("That is not a valid value");
                    }
                }


                // It works like this so that if a string is involved it becomes a string 
                // and to make sure that something that there isn't a bool, a number, and no string in one statement
                if (statementType == null || (int)type > (int)statementType)
                {
                    statementType = type;
                }
                else if (statementType != type && statementType != Variable.vType.String)
                {
                    throw new InvalidOperationException("That is not a valid statement you idiot. Don't mismatch value types like that");
                }
            }
            // It has been parsed, time to calculate;
            if (statementType == Variable.vType.String)
            {
                // Add all of it as if it was a string.

                string r = "";
                for (int i = 0; i < variables.Count; i++)
                {
                    Variable v = variables[i];
                    char c = operators[i];
                    // '+' and ')' (end of evaluation) is the only valid operators for a string
                    if (c != '+' && c != ')')
                    {
                        throw new InvalidOperationException("That operator is not valid for strings");
                    }
                    r += v.GetValue<string>();
                }
                return new Variable($"\"{r}\"", Variable.vType.String);
            }
            else if (statementType == Variable.vType.Bool)
            {
                // Complain, because if it's a Bool then it isn't a String and, you shouldn't do math with true and false.
                if (variables.Count > 1)
                {
                    throw new InvalidOperationException("No operator is valid for a bool");
                }
                // Just give the bool value back.
                return variables[0];
            }
            else
            {
                // Math time!
                int GetOrder(int index)
                {
                    switch (operators[index])
                    {
                        case '*':
                        case '/':
                            return 3;
                        case '+':
                        case '-':
                            return 2;
                    }
                    return 0;
                }
                int i = 1;

                float Ma(int current, bool mergeOne = true)
                {
                    while (i < variables.Count)
                    {
                        int next = i++;
                        while (GetOrder(current) < GetOrder(next))
                        {
                            Ma(next);
                        }
                        float cv = variables[current].GetValue<float>();
                        float nv = variables[next].GetValue<float>();
                        switch (operators[current])
                        {
                            case '*':
                                cv *= nv;
                                break;
                            case '/':
                                cv /= nv;
                                break;
                            case '+':
                                cv += nv;
                                break;
                            case '-':
                                cv -= nv;
                                break;
                        }
                        variables[current].Set(cv.ToString());
                        operators[current] = operators[next];
                        if (mergeOne)
                        {
                            return cv;
                        }
                    }
                    return variables[current].GetValue<float>();
                }

                // Put this in a variable and return it
                return new Variable(Ma(0, false).ToString(), Variable.vType.Number);
            }
            throw new Exception("It's not done yet you big dumb");
        }

        public static bool Evaluate(string statement)
        {
            bool previous = true;
            // int count = 0;
            foreach (Match m in Regex.Matches(statement, @"\G\s*(.+?)\s*(&&|\|\||$)"))
            {
                bool isOr = m.Groups[2].Value == "||";
                if (!previous)
                {
                    if (isOr)
                    {
                        previous = true;
                    }
                    // Console.WriteLine("{0}, number {1}, was skipped", m.Groups[1], count++);
                    continue;
                }

                bool result = false;

                Match ma = Regex.Match(m.Groups[1].Value, @"(.*?)\s*([<>]\=?|[!=]\=)\s*([^=<>]*)");

                if (ma.Groups[1].Value != string.Empty)
                {
                    Variable left = Variables.Calculate(ma.Groups[1].Value);
                    string comparator = ma.Groups[2].Value;
                    Variable right = Variables.Calculate(ma.Groups[3].Value);

                    if (left.Type != right.Type)
                    {
                        throw new InvalidOperationException("Can't compare values of different types");
                    }

                    // Make sure not happens first.
                    if (comparator == "!=")
                    {
                        result = left.Value != right.Value;
                    }
                    // So that if it wasn't != then if it contains a '=' it's either ==, <=, or >=. 
                    // In all those cases, if the values are equal it should become true.
                    else if (comparator.Contains('=') && left.Value == right.Value)
                    {
                        result = true;
                    }
                    // Only check < and > if numbers are being compared
                    else if (left.Type == Variable.vType.Number && comparator.Contains('<'))
                    {
                        result = left.GetValue<float>() < right.GetValue<float>();
                    }
                    else if (left.Type == Variable.vType.Number && comparator.Contains('>'))
                    {
                        result = left.GetValue<float>() > right.GetValue<float>();
                    }
                }
                else
                {
                    result = Variables.Calculate(m.Groups[1].Value).GetValue<bool>();
                }
                // Console.WriteLine("{0}, number {1}, evaluated to {2}", m.Groups[1], count++, result);

                // If the previous result needed to be true but was false then it would've skipped evaluating this.
                // So it doesn't need to check the previous result.
                if (isOr)
                {
                    // If the next thing is || and this is true, then anything beyond that doesn't need to be checked and this can just return true.
                    if (result)
                    {
                        return true;
                    }
                    // Even if the result wasn't true, since the next thing is a || this result doesn't matter, so it'll just say it was true
                    previous = true;
                }
                else
                {
                    // It needs to keep evaluating and this result needs to be remembered
                    previous = result;
                }
            }
            return previous;
        }
    }

    class Variable
    {
        public enum vType
        {
            Number,
            Bool,
            String,
        }
        public vType Type { get; private set; }
        string v;
        public string Value { get => Type == vType.String ? $"\"{v}\"" : v; }

        public Variable(string value, vType type)
        {
            Type = type;
            v = "null";
            if (IsValidValue(value))
                v = value;
        }

        public Variable(Variable value)
        {
            Type = value.Type;
            v = value.v;
        }

        public void Set(string value)
        {
            if (IsValidValue(value))
            {
                v = value;
            }

        }
        public void Set(Variable value)
        {
            if (Type == value.Type)
            {
                v = value.v;
            }
        }

        public bool IsValidValue(string value)
        {
            switch (Type)
            {
                case vType.Bool:
                    {
                        bool ignoreMe;
                        if (!Boolean.TryParse(value, out ignoreMe)) throw new ArgumentException("Ay, that is not a bool");
                    }
                    break;
                case vType.Number:
                    {
                        float ignoreMe;
                        if (!float.TryParse(value, out ignoreMe)) throw new ArgumentException("Ay, that is not a number");
                    }
                    break;
                // case vType.String:
                //     if (value[0] != '"' || !value.EndsWith('"')) throw new ArgumentException("Ay, that is not a string");
                //     break;
            }
            return true;
        }

        public TBoolFloatString GetValue<TBoolFloatString>()
        {
            if (typeof(TBoolFloatString) == typeof(string)
                || (Type == vType.Number && typeof(TBoolFloatString) == typeof(float))
                || (Type == vType.Bool && typeof(TBoolFloatString) == typeof(bool)))
            {
                return (TBoolFloatString)Convert.ChangeType(v, typeof(TBoolFloatString));
            }
            throw new InvalidOperationException("You tried to get the wrong type, idiot");
        }
    }
}