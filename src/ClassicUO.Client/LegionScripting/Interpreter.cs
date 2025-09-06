using System;
using System.Collections.Generic;
using System.Globalization;
using ClassicUO;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Collections;
using static LScript.Interpreter;

namespace LScript
{
    public class RunTimeError : Exception
    {
        public ASTNode Node;

        public RunTimeError(ASTNode node, string error) : base(error)
        {
            Node = node;
        }
    }

    internal static class TypeConverter
    {
        public static int ToInt(string token)
        {
            int val;

            if (token.StartsWith("0x"))
            {
                if (int.TryParse(token.Substring(2), NumberStyles.HexNumber, Interpreter.Culture, out val))
                    return val;
            }
            else if (int.TryParse(token, out val))
                return val;

            throw new RunTimeError(null, "Cannot convert argument to int");
        }

        public static uint ToUInt(string token)
        {
            uint val;

            if (token.StartsWith("0x"))
            {
                if (uint.TryParse(token.Substring(2), NumberStyles.HexNumber, Interpreter.Culture, out val))
                    return val;
            }
            else if (uint.TryParse(token, out val))
                return val;

            throw new RunTimeError(null, $"Cannot convert argument to uint({token})");
        }

        public static ushort ToUShort(string token)
        {
            ushort val;

            if (token.StartsWith("0x"))
            {
                if (ushort.TryParse(token.Substring(2), NumberStyles.HexNumber, Interpreter.Culture, out val))
                    return val;
            }
            else if (ushort.TryParse(token, out val))
                return val;

            throw new RunTimeError(null, "Cannot convert argument to ushort");
        }

        public static double ToDouble(string token)
        {
            double val;

            if (double.TryParse(token, out val))
                return val;

            throw new RunTimeError(null, "Cannot convert argument to double");
        }

        public static bool ToBool(string token)
        {
            bool val;

            if (bool.TryParse(token, out val))
                return val;

            throw new RunTimeError(null, "Cannot convert argument to bool");
        }
    }

    internal class Scope
    {
        private Dictionary<string, Argument> _namespace = new Dictionary<string, Argument>();

        public readonly ASTNode StartNode;
        public readonly Scope Parent;

        public Scope(Scope parent, ASTNode start)
        {
            Parent = parent;
            StartNode = start;
        }

        public Argument GetVar(string name)
        {
            Argument arg;

            if (_namespace.TryGetValue(name, out arg))
                return arg;

            return null;
        }

        public void SetVar(string name, Argument val)
        {
            _namespace[name] = val;
        }

        public void ClearVar(string name)
        {
            _namespace.Remove(name);
        }
    }

    public class Argument
    {
        private ASTNode _node;
        private Script _script;

        public Argument(Script script, ASTNode node)
        {
            _node = node;
            _script = script;
        }

        public string GetLexeme()
        {
            if(_node.Lexeme == null)
                throw new RunTimeError(_node, "No lexeme found.");

            return _node.Lexeme;
        }

        // Treat the argument as an integer
        public int AsInt()
        {
            if (_node.Lexeme == null)
                throw new RunTimeError(_node, "Cannot convert argument to int");

            // Try to resolve it as a scoped variable first
            var arg = _script.Lookup(_node.Lexeme);
            if (arg != null)
                return arg.AsInt();

            return TypeConverter.ToInt(_node.Lexeme);
        }

        // Treat the argument as an unsigned integer
        public uint AsUInt()
        {
            if (_node.Lexeme == null)
                throw new RunTimeError(_node, "Cannot convert argument to uint");

            // Try to resolve it as a scoped variable first
            var arg = _script.Lookup(_node.Lexeme);
            if (arg != null)
                return arg.AsUInt();

            return TypeConverter.ToUInt(_node.Lexeme);
        }

        public ushort AsUShort()
        {
            if (_node.Lexeme == null)
                throw new RunTimeError(_node, "Cannot convert argument to ushort");

            // Try to resolve it as a scoped variable first
            var arg = _script.Lookup(_node.Lexeme);
            if (arg != null)
                return arg.AsUShort();

            return TypeConverter.ToUShort(_node.Lexeme);
        }

        public bool IsSerial()
        {
            if (_node.Lexeme == null)
                return false;

            var arg = _script.Lookup(_node.Lexeme);
            if (arg != null)
                return arg.IsSerial();

            uint serial = Interpreter.GetAlias(_node.Lexeme);
            if (serial != uint.MaxValue)
                return true;

            return false;
        }
        // Treat the argument as a serial or an alias. Aliases will
        // be automatically resolved to serial numbers.
        public uint AsSerial()
        {
            if (_node.Lexeme == null)
                throw new RunTimeError(_node, "Cannot convert argument to serial");

            // Try to resolve it as a scoped variable first
            var arg = _script.Lookup(_node.Lexeme);
            if (arg != null)
                return arg.AsSerial();

            // Resolve it as a global alias next
            uint serial = Interpreter.GetAlias(_node.Lexeme);
            if (serial != uint.MaxValue)
                return serial;

            return AsUInt();
        }

        // Treat the argument as a string
        public string AsString()
        {
            if (_node.Lexeme == null)
                throw new RunTimeError(_node, "Cannot convert argument to string");

            // Try to resolve it as a scoped variable first
            var arg = _script.Lookup(_node.Lexeme);
            if (arg != null)
                return arg.AsString();

            return _node.Lexeme;
        }

        public bool AsBool()
        {
            if (_node.Lexeme == null)
                throw new RunTimeError(_node, "Cannot convert argument to bool");

            return TypeConverter.ToBool(_node.Lexeme);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Argument arg = obj as Argument;

            if (arg == null)
                return false;

            return Equals(arg);
        }

        public bool Equals(Argument other)
        {
            if (other == null)
                return false;

            return (other._node.Lexeme == _node.Lexeme);
        }
    }

    public class Script
    {
        public bool IsPlaying = false;

        private ASTNode _statement;

        private Scope _scope;

        private Dictionary<int, ASTNode> lineNodes = new Dictionary<int, ASTNode>();
        private Deque<ASTNode> returnPoints = new Deque<ASTNode>();

        public ExecutionState ExecutionState = ExecutionState.RUNNING;

        public long PauseTimeout = long.MaxValue;

        public bool TargetRequested = false;

        public TimeoutCallback TimeoutCallback = null;

        public HashSet<uint> IgnoreList = new HashSet<uint>();

        public bool IsPaused
        {
            get
            {
                if (ExecutionState != ExecutionState.PAUSED) return false;
                return true;
            }
        }

        private List<JournalEntry> _journalEntries = new List<JournalEntry>();

        public ASTNode Root { get; private set; }

        public int CurrentLine
        {
            get
            {
                return _statement == null ? 0 : _statement.LineNumber;
            }
        }

        public Argument Lookup(string name)
        {
            var scope = _scope;
            Argument result = null;

            while (scope != null)
            {
                result = scope.GetVar(name);
                if (result != null)
                    return result;

                scope = scope.Parent;
            }

            return result;
        }

        public void JournalEntryAdded(JournalEntry e)
        {
            if (_journalEntries.Count >= 50)
            {
                _journalEntries.RemoveAt(0);
            }

            _journalEntries.Add(e);
        }

        public bool SearchJournalEntries(string text)
        {
            foreach (var entry in _journalEntries)
            {
                if (entry.Text.Contains(text)) return true;
            }

            return false;
        }

        public void ClearJournal()
        {
            _journalEntries.Clear();
        }

        private void PushScope(ASTNode node)
        {
            _scope = new Scope(_scope, node);
        }

        private void PopScope()
        {
            _scope = _scope.Parent;
        }

        private Argument[] ConstructArguments(ref ASTNode node)
        {
            List<Argument> args = new List<Argument>();

            node = node.Next();

            while (node != null)
            {
                switch (node.Type)
                {
                    case ASTNodeType.AND:
                    case ASTNodeType.OR:
                    case ASTNodeType.EQUAL:
                    case ASTNodeType.NOT_EQUAL:
                    case ASTNodeType.LESS_THAN:
                    case ASTNodeType.LESS_THAN_OR_EQUAL:
                    case ASTNodeType.GREATER_THAN:
                    case ASTNodeType.GREATER_THAN_OR_EQUAL:
                        return args.ToArray();
                }

                args.Add(new Argument(this, node));

                node = node.Next();
            }

            return args.ToArray();
        }

        // For now, the scripts execute directly from the
        // abstract syntax tree. This is relatively simple.
        // A more robust approach would be to "compile" the
        // scripts to a bytecode. That would allow more errors
        // to be caught with better error messages, as well as
        // make the scripts execute more quickly.
        public Script(ASTNode root)
        {
            // Set current to the first statement
            _statement = root.FirstChild();

            // Create a default scope
            _scope = new Scope(null, _statement);
            Root = root;

            GenlineNodes();
        }

        public void UpdateScript(ASTNode root)
        {
            _statement = root.FirstChild();
            _scope = new Scope(null, _statement);
            Root = root;
            TargetRequested = false;
            returnPoints.Clear();
            GenlineNodes();
        }

        private void GenlineNodes()
        {
            ASTNode n = _statement;
            while (n != null)
            {
                lineNodes[n.LineNumber] = n;
                n = n.Next();
            }
        }
        public void Reset()
        {
            _statement = Root.FirstChild();
            _journalEntries.Clear();
            TargetRequested = false;
            IgnoreList.Clear();
            PauseTimeout = long.MaxValue;
            ExecutionState = ExecutionState.RUNNING;
            returnPoints.Clear();
        }

        public bool ExecuteNext()
        {
            if (_statement == null)
                return false;

            if (_statement.Type != ASTNodeType.STATEMENT)
                throw new RunTimeError(_statement, "Invalid script");

            var node = _statement.FirstChild();

            if (node == null)
                throw new RunTimeError(_statement, "Invalid statement");

            int depth = 0;

            if(CUOEnviroment.Debug)
                ClassicUO.Game.GameActions.Print($"Executing: [{CurrentLine}]{node.Lexeme}");

            switch (node.Type)
            {
                case ASTNodeType.IF:
                    {
                        PushScope(node);

                        var expr = node.FirstChild();
                        var result = EvaluateExpression(ref expr);

                        // Advance to next statement
                        Advance();

                        // Evaluated true. Jump right into execution.
                        if (result)
                            break;

                        // The expression evaluated false, so keep advancing until
                        // we hit an elseif, else, or endif statement that matches
                        // and try again.
                        depth = 0;

                        while (_statement != null)
                        {
                            node = _statement.FirstChild();

                            if (node.Type == ASTNodeType.IF)
                            {
                                depth++;
                            }
                            else if (node.Type == ASTNodeType.ELSEIF)
                            {
                                if (depth == 0)
                                {
                                    expr = node.FirstChild();
                                    result = EvaluateExpression(ref expr);

                                    // Evaluated true. Jump right into execution
                                    if (result)
                                    {
                                        Advance();
                                        break;
                                    }
                                }
                            }
                            else if (node.Type == ASTNodeType.ELSE)
                            {
                                if (depth == 0)
                                {
                                    // Jump into the else clause
                                    Advance();
                                    break;
                                }
                            }
                            else if (node.Type == ASTNodeType.ENDIF)
                            {
                                if (depth == 0)
                                    break;

                                depth--;
                            }

                            Advance();
                        }

                        if (_statement == null)
                            throw new RunTimeError(node, "If with no matching endif");

                        break;
                    }
                case ASTNodeType.ELSEIF:
                    // If we hit the elseif statement during normal advancing, skip over it. The only way
                    // to execute an elseif clause is to jump directly in from an if statement.
                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.IF)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.ENDIF)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        Advance();
                    }

                    if (_statement == null)
                        throw new RunTimeError(node, "If with no matching endif");

                    break;
                case ASTNodeType.ENDIF:
                    PopScope();
                    Advance();
                    break;
                case ASTNodeType.ELSE:
                    // If we hit the else statement during normal advancing, skip over it. The only way
                    // to execute an else clause is to jump directly in from an if statement.
                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.IF)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.ENDIF)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        Advance();
                    }

                    if (_statement == null)
                        throw new RunTimeError(node, "If with no matching endif");

                    break;
                case ASTNodeType.WHILE:
                    {
                        // When we first enter the loop, push a new scope
                        if (_scope.StartNode != node)
                        {
                            PushScope(node);
                        }

                        var expr = node.FirstChild();
                        var result = EvaluateExpression(ref expr);

                        // Advance to next statement
                        Advance();

                        // The expression evaluated false, so keep advancing until
                        // we hit an endwhile statement.
                        if (!result)
                        {
                            depth = 0;

                            while (_statement != null)
                            {
                                node = _statement.FirstChild();

                                if (node.Type == ASTNodeType.WHILE)
                                {
                                    depth++;
                                }
                                else if (node.Type == ASTNodeType.ENDWHILE)
                                {
                                    if (depth == 0)
                                    {
                                        PopScope();
                                        // Go one past the endwhile so the loop doesn't repeat
                                        Advance();
                                        break;
                                    }

                                    depth--;
                                }

                                Advance();
                            }
                        }
                        break;
                    }
                case ASTNodeType.ENDWHILE:
                    // Walk backward to the while statement
                    _statement = _statement.Prev();

                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.ENDWHILE)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.WHILE)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        _statement = _statement.Prev();
                    }

                    if (_statement == null)
                        throw new RunTimeError(node, "Unexpected endwhile");

                    break;
                case ASTNodeType.FOR:
                    {
                        // The iterator variable's name is the hash code of the for loop's ASTNode.
                        var iterName = node.GetHashCode().ToString();

                        // When we first enter the loop, push a new scope
                        if (_scope.StartNode != node)
                        {
                            PushScope(node);

                            // Grab the arguments
                            var max = node.FirstChild();

                            if (max.Type != ASTNodeType.INTEGER)
                                throw new RunTimeError(max, "Invalid for loop syntax");

                            // Create a dummy argument that acts as our loop variable
                            var iter = new ASTNode(ASTNodeType.INTEGER, "0", node, 0);

                            _scope.SetVar(iterName, new Argument(this, iter));
                        }
                        else
                        {
                            // Increment the iterator argument
                            var arg = _scope.GetVar(iterName);

                            var iter = new ASTNode(ASTNodeType.INTEGER, (arg.AsUInt() + 1).ToString(), node, 0);

                            _scope.SetVar(iterName, new Argument(this, iter));
                        }

                        // Check loop condition
                        var i = _scope.GetVar(iterName);

                        // Grab the max value to iterate to
                        node = node.FirstChild();
                        var end = new Argument(this, node);

                        if (i.AsUInt() < end.AsUInt())
                        {
                            // enter the loop
                            Advance();
                        }
                        else
                        {
                            // Walk until the end of the loop
                            Advance();

                            depth = 0;

                            while (_statement != null)
                            {
                                node = _statement.FirstChild();

                                if (node.Type == ASTNodeType.FOR ||
                                    node.Type == ASTNodeType.FOREACH)
                                {
                                    depth++;
                                }
                                else if (node.Type == ASTNodeType.ENDFOR)
                                {
                                    if (depth == 0)
                                    {
                                        PopScope();
                                        // Go one past the end so the loop doesn't repeat
                                        Advance();
                                        break;
                                    }

                                    depth--;
                                }

                                Advance();
                            }
                        }
                    }
                    break;
                case ASTNodeType.FOREACH:
                    {
                        // foreach VAR in LIST
                        // The iterator's name is the hash code of the for loop's ASTNode.
                        var varName = node.FirstChild().Lexeme;
                        var listName = node.FirstChild().Next().Lexeme;
                        var iterName = node.GetHashCode().ToString();

                        // When we first enter the loop, push a new scope
                        if (_scope.StartNode != node)
                        {
                            PushScope(node);

                            // Create a dummy argument that acts as our iterator object
                            var iter = new ASTNode(ASTNodeType.INTEGER, "0", node, 0);
                            _scope.SetVar(iterName, new Argument(this, iter));

                            // Make the user-chosen variable have the value for the front of the list
                            var arg = Interpreter.GetListValue(listName, 0);

                            if (arg != null)
                                _scope.SetVar(varName, arg);
                            else
                                _scope.ClearVar(varName);
                        }
                        else
                        {
                            // Increment the iterator argument
                            var idx = _scope.GetVar(iterName).AsInt() + 1;
                            var iter = new ASTNode(ASTNodeType.INTEGER, idx.ToString(), node, 0);
                            _scope.SetVar(iterName, new Argument(this, iter));

                            // Update the user-chosen variable
                            var arg = Interpreter.GetListValue(listName, idx);

                            if (arg != null)
                                _scope.SetVar(varName, arg);
                            else
                                _scope.ClearVar(varName);
                        }

                        // Check loop condition
                        var i = _scope.GetVar(varName);

                        if (i != null)
                        {
                            // enter the loop
                            Advance();
                        }
                        else
                        {
                            // Walk until the end of the loop
                            Advance();

                            depth = 0;

                            while (_statement != null)
                            {
                                node = _statement.FirstChild();

                                if (node.Type == ASTNodeType.FOR ||
                                    node.Type == ASTNodeType.FOREACH)
                                {
                                    depth++;
                                }
                                else if (node.Type == ASTNodeType.ENDFOR)
                                {
                                    if (depth == 0)
                                    {
                                        PopScope();
                                        // Go one past the end so the loop doesn't repeat
                                        Advance();
                                        break;
                                    }

                                    depth--;
                                }

                                Advance();
                            }
                        }
                        break;
                    }
                case ASTNodeType.ENDFOR:
                    // Walk backward to the for statement
                    _statement = _statement.Prev();

                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.ENDFOR)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.FOR ||
                                 node.Type == ASTNodeType.FOREACH)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        _statement = _statement.Prev();
                    }

                    if (_statement == null)
                        throw new RunTimeError(node, "Unexpected endfor");

                    break;
                case ASTNodeType.BREAK:
                    // Walk until the end of the loop
                    Advance();

                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.WHILE ||
                            node.Type == ASTNodeType.FOR ||
                            node.Type == ASTNodeType.FOREACH)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.ENDWHILE ||
                            node.Type == ASTNodeType.ENDFOR)
                        {
                            if (depth == 0)
                            {
                                // Go one past the end so the loop doesn't repeat
                                Advance();
                                break;
                            }

                            depth--;
                        }

                        Advance();
                    }

                    PopScope();
                    break;
                case ASTNodeType.CONTINUE:
                    // Walk backward to the loop statement
                    _statement = _statement.Prev();

                    depth = 0;

                    while (_statement != null)
                    {
                        node = _statement.FirstChild();

                        if (node.Type == ASTNodeType.ENDWHILE ||
                            node.Type == ASTNodeType.ENDFOR)
                        {
                            depth++;
                        }
                        else if (node.Type == ASTNodeType.WHILE ||
                                 node.Type == ASTNodeType.FOR ||
                                 node.Type == ASTNodeType.FOREACH)
                        {
                            if (depth == 0)
                                break;

                            depth--;
                        }

                        _statement = _statement.Prev();
                    }

                    if (_statement == null)
                        throw new RunTimeError(node, "Unexpected continue");
                    break;
                case ASTNodeType.STOP:
                    _statement = null;
                    break;
                case ASTNodeType.REPLAY:
                    _statement = _statement.Parent.FirstChild();
                    break;
                case ASTNodeType.QUIET:
                case ASTNodeType.FORCE:
                case ASTNodeType.COMMAND:
                    if (ExecuteCommand(node))
                        Advance();

                    break;
            }

            return (_statement != null) ? true : false;
        }

        public void Advance()
        {
            Interpreter.ClearTimeout();
            _statement = _statement.Next();
        }

        public bool GotoLine(int line)
        {
            if (lineNodes.ContainsKey(line))
            {
                returnPoints.AddToBack(_statement);
                _statement = lineNodes[line];
                return true;
            }
            return false;
        }

        public void ReturnFromGoto()
        {
            if (returnPoints.Count > 0)
                _statement = returnPoints.RemoveFromFront();
        }

        private ASTNode EvaluateModifiers(ASTNode node, out bool quiet, out bool force, out bool not)
        {
            quiet = false;
            force = false;
            not = false;

            while (true)
            {
                switch (node.Type)
                {
                    case ASTNodeType.QUIET:
                        quiet = true;
                        break;
                    case ASTNodeType.FORCE:
                        force = true;
                        break;
                    case ASTNodeType.NOT:
                        not = true;
                        break;
                    default:
                        return node;
                }

                node = node.Next();
            }
        }

        private bool ExecuteCommand(ASTNode node)
        {
            node = EvaluateModifiers(node, out bool quiet, out bool force, out _);

            var handler = Interpreter.GetCommandHandler(node.Lexeme);

            if (handler == null)
                throw new RunTimeError(node, "Unknown command");

            var cont = handler(node.Lexeme, ConstructArguments(ref node), quiet, force);

            if (node != null)
                throw new RunTimeError(node, "Command did not consume all available arguments");

            return cont;
        }

        private bool EvaluateExpression(ref ASTNode expr)
        {
            if (expr == null || (expr.Type != ASTNodeType.UNARY_EXPRESSION && expr.Type != ASTNodeType.BINARY_EXPRESSION && expr.Type != ASTNodeType.LOGICAL_EXPRESSION))
                throw new RunTimeError(expr, "No expression following control statement");

            var node = expr.FirstChild();

            if (node == null)
                throw new RunTimeError(expr, "Empty expression following control statement");

            switch (expr.Type)
            {
                case ASTNodeType.UNARY_EXPRESSION:
                    return EvaluateUnaryExpression(ref node);
                case ASTNodeType.BINARY_EXPRESSION:
                    return EvaluateBinaryExpression(ref node);
            }

            bool lhs = EvaluateExpression(ref node);

            node = node.Next();

            while (node != null)
            {
                // Capture the operator
                var op = node.Type;
                node = node.Next();

                if (node == null)
                    throw new RunTimeError(node, "Invalid logical expression");

                bool rhs;

                var e = node.FirstChild();

                switch (node.Type)
                {
                    case ASTNodeType.UNARY_EXPRESSION:
                        rhs = EvaluateUnaryExpression(ref e);
                        break;
                    case ASTNodeType.BINARY_EXPRESSION:
                        rhs = EvaluateBinaryExpression(ref e);
                        break;
                    default:
                        throw new RunTimeError(node, "Nested logical expressions are not possible");
                }

                switch (op)
                {
                    case ASTNodeType.AND:
                        lhs = lhs && rhs;
                        break;
                    case ASTNodeType.OR:
                        lhs = lhs || rhs;
                        break;
                    default:
                        throw new RunTimeError(node, "Invalid logical operator");
                }

                node = node.Next();
            }

            return lhs;
        }

        private bool CompareOperands(ASTNodeType op, IComparable lhs, IComparable rhs)
        {
            if (lhs.GetType() != rhs.GetType())
            {
                // Different types. Try to convert one to match the other.

                if (rhs is double)
                {
                    // Special case for rhs doubles because we don't want to lose precision.
                    lhs = (double)lhs;
                }
                else if (rhs is bool)
                {
                    // Special case for rhs bools because we want to down-convert the lhs.
                    var tmp = Convert.ChangeType(lhs, typeof(bool));
                    lhs = (IComparable)tmp;
                }
                else
                {
                    var tmp = Convert.ChangeType(rhs, lhs.GetType());
                    rhs = (IComparable)tmp;
                }
            }

            try
            {
                // Evaluate the whole expression
                switch (op)
                {
                    case ASTNodeType.EQUAL:
                        return lhs.CompareTo(rhs) == 0;
                    case ASTNodeType.NOT_EQUAL:
                        return lhs.CompareTo(rhs) != 0;
                    case ASTNodeType.LESS_THAN:
                        return lhs.CompareTo(rhs) < 0;
                    case ASTNodeType.LESS_THAN_OR_EQUAL:
                        return lhs.CompareTo(rhs) <= 0;
                    case ASTNodeType.GREATER_THAN:
                        return lhs.CompareTo(rhs) > 0;
                    case ASTNodeType.GREATER_THAN_OR_EQUAL:
                        return lhs.CompareTo(rhs) >= 0;
                }
            }
            catch (ArgumentException e)
            {
                throw new RunTimeError(null, e.Message);
            }

            throw new RunTimeError(null, "Unknown operator in expression");

        }

        private bool EvaluateUnaryExpression(ref ASTNode node)
        {
            node = EvaluateModifiers(node, out bool quiet, out _, out bool not);

            var handler = Interpreter.GetExpressionHandler(node.Lexeme);

            if (handler == null)
                throw new RunTimeError(node, "Unknown expression");

            var result = handler(node.Lexeme, ConstructArguments(ref node), quiet);

            if (not)
                return CompareOperands(ASTNodeType.EQUAL, result, false);
            else
                return CompareOperands(ASTNodeType.EQUAL, result, true);
        }

        private bool EvaluateBinaryExpression(ref ASTNode node)
        {
            // Evaluate the left hand side
            var lhs = EvaluateBinaryOperand(ref node);

            // Capture the operator
            var op = node.Type;
            node = node.Next();

            // Evaluate the right hand side
            var rhs = EvaluateBinaryOperand(ref node);

            return CompareOperands(op, lhs, rhs);
        }

        private IComparable EvaluateBinaryOperand(ref ASTNode node)
        {
            IComparable val;

            node = EvaluateModifiers(node, out bool quiet, out _, out _);
            switch (node.Type)
            {
                case ASTNodeType.INTEGER:
                    val = TypeConverter.ToInt(node.Lexeme);
                    break;
                case ASTNodeType.SERIAL:
                    val = TypeConverter.ToUInt(node.Lexeme);
                    break;
                case ASTNodeType.STRING:
                    val = node.Lexeme;
                    break;
                case ASTNodeType.DOUBLE:
                    val = TypeConverter.ToDouble(node.Lexeme);
                    break;
                case ASTNodeType.OPERAND:
                    {
                        // This might be a registered keyword, so do a lookup
                        var handler = Interpreter.GetExpressionHandler(node.Lexeme);

                        if (handler == null)
                        {
                            // It's just a string
                            val = node.Lexeme;
                        }
                        else
                        {
                            val = handler(node.Lexeme, ConstructArguments(ref node), quiet);
                        }
                        break;
                    }
                default:
                    throw new RunTimeError(node, "Invalid type found in expression");
            }

            return val;
        }
    }

    public enum ExecutionState
    {
        RUNNING,
        PAUSED,
        TIMING_OUT
    };

    public static class Interpreter
    {
        // Aliases only hold serial numbers
        private static Dictionary<string, uint> _aliases = new Dictionary<string, uint>();

        // Lists
        private static Dictionary<string, List<Argument>> _lists = new Dictionary<string, List<Argument>>();

        // Timers
        private static Dictionary<string, DateTime> _timers = new Dictionary<string, DateTime>();

        // Expressions
        public delegate IComparable ExpressionHandler(string expression, Argument[] args, bool quiet);
        public delegate T ExpressionHandler<T>(string expression, Argument[] args, bool quiet) where T : IComparable;

        private static Dictionary<string, ExpressionHandler> _exprHandlers = new Dictionary<string, ExpressionHandler>();

        public delegate bool CommandHandler(string command, Argument[] args, bool quiet, bool force);

        private static Dictionary<string, CommandHandler> _commandHandlers = new Dictionary<string, CommandHandler>();

        public delegate uint AliasHandler(string alias);

        private static Dictionary<string, AliasHandler> _aliasHandlers = new Dictionary<string, AliasHandler>();

        private static Script _activeScript = null;

        public static Script ActiveScript { get { return _activeScript; } }

        public delegate bool TimeoutCallback();

        public static CultureInfo Culture;

        static Interpreter()
        {
            Culture = new CultureInfo(CultureInfo.CurrentCulture.LCID, false);
            Culture.NumberFormat.NumberDecimalSeparator = ".";
            Culture.NumberFormat.NumberGroupSeparator = ",";
        }

        public static void RegisterExpressionHandler<T>(string keyword, ExpressionHandler<T> handler) where T : IComparable
        {
            _exprHandlers[keyword] = (expression, args, quiet) => handler(expression, args, quiet);
        }

        public static ExpressionHandler GetExpressionHandler(string keyword)
        {
            _exprHandlers.TryGetValue(keyword, out var expression);

            return expression;
        }
        public static bool GotoLine(int line)
        {
            if (ActiveScript == null) return false;
            return ActiveScript.GotoLine(line);
        }
        public static void ReturnFromGoto()
        {
            if (ActiveScript == null) return;
            ActiveScript.ReturnFromGoto();
        }
        public static bool InIgnoreList(uint serial)
        {
            if (ActiveScript == null) return false;

            return ActiveScript.IgnoreList.Contains(serial);
        }
        public static void IgnoreSerial(uint serial)
        {
            if (ActiveScript == null) return;

            ActiveScript.IgnoreList.Add(serial);
        }
        public static void ClearIgnoreList()
        {
            if (ActiveScript == null) return;

            ActiveScript.IgnoreList.Clear();
        }
        public static void ClearJournal()
        {
            if (ActiveScript == null)
                return;

            ActiveScript.ClearJournal();
        }

        public static bool IsTargetRequested()
        {
            if (ActiveScript != null)
                return ActiveScript.TargetRequested;

            return false;
        }

        public static void SetTargetRequested(bool targetRequested)
        {
            if (ActiveScript == null)
                return;

            ActiveScript.TargetRequested = targetRequested;
        }

        public static void RegisterCommandHandler(string keyword, CommandHandler handler)
        {
            _commandHandlers[keyword] = handler;
        }

        public static CommandHandler GetCommandHandler(string keyword)
        {
            _commandHandlers.TryGetValue(keyword, out CommandHandler handler);

            return handler;
        }

        public static void RegisterAliasHandler(string keyword, AliasHandler handler)
        {
            _aliasHandlers[keyword] = handler;
        }

        public static void UnregisterAliasHandler(string keyword)
        {
            _aliasHandlers.Remove(keyword);
        }

        public static uint GetAlias(string alias)
        {
            // If a handler is explicitly registered, call that.
            if (_aliasHandlers.TryGetValue(alias, out AliasHandler handler))
                return handler(alias);

            uint value;
            if (_aliases.TryGetValue(alias, out value))
                return value;

            return uint.MaxValue;
        }

        public static void SetAlias(string alias, uint serial)
        {
            _aliases[alias] = serial;
        }

        public static void RemoveAlias(string alias)
        {
            if (_aliases.ContainsKey(alias))
                _aliases.Remove(alias);
        }

        public static void CreateList(string name)
        {
            if (_lists.ContainsKey(name))
                return;

            _lists[name] = new List<Argument>();
        }

        public static void DestroyList(string name)
        {
            _lists.Remove(name);
        }

        public static void ClearAllLists()
        {
            _lists.Clear();
        }

        public static void ClearList(string name)
        {
            if (!_lists.ContainsKey(name))
                return;

            _lists[name].Clear();
        }

        public static List<Argument> GetList(string name)
        {
            if (_lists.ContainsKey(name))
                return _lists[name];

            return null;
        }

        public static bool ListExists(string name)
        {
            return _lists.ContainsKey(name);
        }

        public static bool ListContains(string name, Argument arg)
        {
            if (!_lists.ContainsKey(name))
                return false;

            return _lists[name].Contains(arg);
        }

        public static int ListLength(string name)
        {
            if (!_lists.ContainsKey(name))
                return 0;

            return _lists[name].Count;
        }

        public static void PushList(string name, Argument arg, bool front, bool unique)
        {
            if (!_lists.ContainsKey(name))
                throw new RunTimeError(null, "List does not exist");

            if (unique && _lists[name].Contains(arg))
                return;

            if (front)
                _lists[name].Insert(0, arg);
            else
                _lists[name].Add(arg);
        }

        public static bool PopList(string name, Argument arg)
        {
            if (!_lists.ContainsKey(name))
                return true;

            return _lists[name].Remove(arg);
        }

        public static bool PopList(string name, bool front)
        {
            if (!_lists.ContainsKey(name))
                throw new RunTimeError(null, "List does not exist");

            var idx = front ? 0 : _lists[name].Count - 1;

            _lists[name].RemoveAt(idx);

            return _lists[name].Count > 0;
        }

        public static Argument GetListValue(string name, int idx)
        {
            if (!_lists.ContainsKey(name))
                throw new RunTimeError(null, "List does not exist");

            var list = _lists[name];

            if (idx < list.Count)
                return list[idx];

            return null;
        }

        public static void SetTimer(string name, int msDuration)
        {
            if (_timers.ContainsKey(name) && !TimerExpired(name)) //Don't update if timer exists
            {
                return;
            }

            _timers[name] = DateTime.UtcNow + TimeSpan.FromMilliseconds(msDuration);
        }

        public static bool TimerExpired(string name)
        {
            if (!_timers.ContainsKey(name))
                return true; //Timer doesn't exist

            if (_timers[name] <= DateTime.UtcNow)
            {
                //Timer expired
                _timers.Remove(name);
                return true;
            }

            return false;
        }

        public static void RemoveTimer(string name)
        {
            if (!_timers.ContainsKey(name))
                return;

            _timers.Remove(name);
        }

        public static bool TimerExists(string name)
        {
            return _timers.ContainsKey(name);
        }

        public static void StopScript()
        {
            if (_activeScript != null)
                _activeScript.ExecutionState = ExecutionState.RUNNING;
            _activeScript = null;
        }

        public static bool ExecuteScript(Script script)
        {
            if (script == null)
                return false;

            _activeScript = script;


            if (_activeScript.ExecutionState == ExecutionState.PAUSED)
            {
                if (_activeScript.PauseTimeout < DateTime.UtcNow.Ticks)
                    _activeScript.ExecutionState = ExecutionState.RUNNING;
                else
                    return true;
            }
            else if (_activeScript.ExecutionState == ExecutionState.TIMING_OUT)
            {
                if (_activeScript.PauseTimeout < DateTime.UtcNow.Ticks)
                {
                    if (_activeScript.TimeoutCallback != null)
                    {
                        if (_activeScript.TimeoutCallback())
                        {
                            _activeScript.Advance();
                            ClearTimeout();
                        }

                        _activeScript.TimeoutCallback = null;
                    }

                    /* If the callback changed the state to running, continue
                     * on. Otherwise, exit.
                     */
                    if (_activeScript.ExecutionState != ExecutionState.RUNNING)
                    {
                        _activeScript = null;
                        return false;
                    }
                }
            }

            if (!_activeScript.ExecuteNext())
            {
                _activeScript = null;
                return false;
            }

            return true;
        }

        // Pause execution for the given number of milliseconds
        public static void Pause(long duration)
        {
            // Already paused or timing out
            if (_activeScript.ExecutionState != ExecutionState.RUNNING)
                return;

            _activeScript.PauseTimeout = DateTime.UtcNow.Ticks + (duration * 10000);
            _activeScript.ExecutionState = ExecutionState.PAUSED;
        }

        // Unpause execution
        public static void Unpause()
        {
            if (_activeScript.ExecutionState != ExecutionState.PAUSED)
                return;

            _activeScript.PauseTimeout = 0;
            _activeScript.ExecutionState = ExecutionState.RUNNING;
        }

        // If forward progress on the script isn't made within this
        // amount of time (milliseconds), bail
        public static void Timeout(long duration, TimeoutCallback callback)
        {
            // Don't change an existing timeout
            if (_activeScript.ExecutionState != ExecutionState.RUNNING)
                return;

            _activeScript.PauseTimeout = DateTime.UtcNow.Ticks + (duration * 10000);
            _activeScript.ExecutionState = ExecutionState.TIMING_OUT;
            _activeScript.TimeoutCallback = callback;
        }

        // Clears any previously set timeout. Automatically
        // called any time the script advances a statement.
        public static void ClearTimeout()
        {
            if (_activeScript.ExecutionState != ExecutionState.TIMING_OUT)
                return;

            _activeScript.ExecutionState = ExecutionState.RUNNING;
            _activeScript.PauseTimeout = 0;
        }

        public static void Reset()
        {
            if (_activeScript != null)
                _activeScript = null;

            ClearTimeout();

        }
    }
}