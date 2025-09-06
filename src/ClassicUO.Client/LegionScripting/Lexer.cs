using System;
using System.Collections.Generic;
using System.IO;

namespace LScript
{
    public class SyntaxError : Exception
    {
        public ASTNode Node;
        public string Line;
        public int LineNumber;

        public SyntaxError(ASTNode node, string error) : base(error)
        {
            Node = node;
            Line = null;
            LineNumber = 0;
        }

        public SyntaxError(string line, int lineNumber, ASTNode node, string error) : base(error)
        {
            Line = line;
            LineNumber = lineNumber;
            Node = node;
        }
    }

    public enum ASTNodeType
    {
        // Keywords
        IF,
        ELSEIF,
        ELSE,
        ENDIF,
        WHILE,
        ENDWHILE,
        FOR,
        FOREACH,
        ENDFOR,
        BREAK,
        CONTINUE,
        STOP,
        REPLAY,

        // Operators
        EQUAL,
        NOT_EQUAL,
        LESS_THAN,
        LESS_THAN_OR_EQUAL,
        GREATER_THAN,
        GREATER_THAN_OR_EQUAL,

        // Logical Operators
        NOT,
        AND,
        OR,

        // Value types
        STRING,
        SERIAL,
        INTEGER,
        DOUBLE,
        LIST,

        // Modifiers
        QUIET, // @ symbol
        FORCE, // ! symbol
        
        // Everything else
        SCRIPT,
        STATEMENT,
        COMMAND,
        OPERAND,
        LOGICAL_EXPRESSION,
        UNARY_EXPRESSION,
        BINARY_EXPRESSION,
    }

    // Abstract Syntax Tree Node
    public class ASTNode
    {
        public readonly ASTNodeType Type;
        public readonly string Lexeme;
        public readonly ASTNode Parent;
        public readonly int LineNumber;

        internal LinkedListNode<ASTNode> _node;
        private LinkedList<ASTNode> _children;

        public ASTNode(ASTNodeType type, string lexeme, ASTNode parent, int lineNumber)
        {
            Type = type;
            if (lexeme != null)
                Lexeme = lexeme;
            else
                Lexeme = "";
            Parent = parent;
            LineNumber = lineNumber;
        }

        public ASTNode Push(ASTNodeType type, string lexeme, int lineNumber)
        {
            var node = new ASTNode(type, lexeme, this, lineNumber);

            if (_children == null)
                _children = new LinkedList<ASTNode>();

            node._node = _children.AddLast(node);

            return node;
        }

        public ASTNode FirstChild()
        {
            if (_children == null || _children.First == null)
                return null;

            return _children.First.Value;
        }

        public ASTNode Next()
        {
            if (_node == null || _node.Next == null)
                return null;

            return _node.Next.Value;
        }

        public ASTNode Prev()
        {
            if (_node == null || _node.Previous == null)
                return null;

            return _node.Previous.Value;
        }
    }

    public static class Lexer
    {
        private static int _curLine = 0;

        public static T[] Slice<T>(this T[] src, int start, int end)
        {
            if (end < start)
                return new T[0];

            int len = end - start + 1;

            T[] slice = new T[len];
            for (int i = 0; i < len; i++)
            {
                slice[i] = src[i + start];
            }

            return slice;
        }

        public static ASTNode Lex(string[] lines)
        {
            ASTNode node = new ASTNode(ASTNodeType.SCRIPT, null, null, 0);

            try
            {
                for (_curLine = 0; _curLine < lines.Length; _curLine++)
                {
                    foreach (var l in lines[_curLine].Split(';'))
                    {
                        ParseLine(node, l);
                    }
                }
            }
            catch (SyntaxError e)
            {
                throw new SyntaxError(lines[_curLine], _curLine, e.Node, e.Message);
            }
            catch (Exception e)
            {
                throw new SyntaxError(lines[_curLine], _curLine, null, e.Message);
            }

            return node;
        }

        public static ASTNode Lex(string fname)
        {
            ASTNode node = new ASTNode(ASTNodeType.SCRIPT, null, null, 0);

            using (var file = new StreamReader(fname))
            {
                _curLine = 0;
                string line = null;

                try
                {
                    while (true)
                    {
                        // Each line in the file is a statement. Statements starting
                        // with a control flow keyword contain an expression.

                        line = file.ReadLine();

                        // End of file
                        if (line == null)
                            break;

                        foreach (var l in line.Split(';'))
                            ParseLine(node, line);

                        _curLine++;
                    }
                }
                catch (SyntaxError e)
                {
                    throw new SyntaxError(line, _curLine, e.Node, e.Message);
                }
                catch (Exception e)
                {
                    throw new SyntaxError(line, _curLine, null, e.Message);
                }
            }

            return node;
        }

        private static TextParser _tfp = new TextParser("", new char[] { ' ' }, new char[] { }, new char[] { '\'', '\'', '"', '"' });
        private static void ParseLine(ASTNode node, string line)
        {
            line = line.Trim();

            if (line.StartsWith("//") || line.StartsWith("#"))
                return;

            // Split the line by spaces (unless the space is in quotes)
            var lexemes = _tfp.GetTokens(line, false);

            if (lexemes.Length == 0)
                return;

            ParseStatement(node, lexemes);
        }

        private static void ParseValue(ASTNode node, string lexeme, ASTNodeType typeDefault)
        {
            if (lexeme.StartsWith("0x"))
                node.Push(ASTNodeType.SERIAL, lexeme, _curLine);
            else if (int.TryParse(lexeme, out _))
                node.Push(ASTNodeType.INTEGER, lexeme, _curLine);
            else if (double.TryParse(lexeme, out _))
                node.Push(ASTNodeType.DOUBLE, lexeme, _curLine);
            else
                node.Push(typeDefault, lexeme, _curLine);
        }

        private static void ParseCommand(ASTNode node, string lexeme)
        {
            // A command may start with an '@' symbol. Pick that
            // off.
            if (lexeme[0] == '@')
            {
                node.Push(ASTNodeType.QUIET, null, _curLine);
                lexeme = lexeme.Substring(1, lexeme.Length - 1);
            }

            // A command may end with a '!' symbol. Pick that
            // off.
            if (lexeme.EndsWith("!"))
            {
                node.Push(ASTNodeType.FORCE, null, _curLine);
                lexeme = lexeme.Substring(0, lexeme.Length - 1);
            }

            node.Push(ASTNodeType.COMMAND, lexeme, _curLine);
        }

        private static void ParseOperand(ASTNode node, string lexeme)
        {
            bool modifier = false;

            // An operand may start with an '@' symbol. Pick that
            // off.
            if (lexeme[0] == '@')
            {
                node.Push(ASTNodeType.QUIET, null, _curLine);
                lexeme = lexeme.Substring(1, lexeme.Length - 1);
                modifier = true;
            }

            // An operand may end with a '!' symbol. Pick that
            // off.
            if (lexeme.EndsWith("!"))
            {
                node.Push(ASTNodeType.FORCE, null, _curLine);
                lexeme = lexeme.Substring(0, lexeme.Length - 1);
                modifier = true;
            }

            if (!modifier)
                ParseValue(node, lexeme, ASTNodeType.OPERAND);
            else
                node.Push(ASTNodeType.OPERAND, lexeme, _curLine);
        }

        private static void ParseOperator(ASTNode node, string lexeme)
        {
            switch (lexeme)
            {
                case "==":
                case "=":
                    node.Push(ASTNodeType.EQUAL, null, _curLine);
                    break;
                case "!=":
                    node.Push(ASTNodeType.NOT_EQUAL, null, _curLine);
                    break;
                case "<":
                    node.Push(ASTNodeType.LESS_THAN, null, _curLine);
                    break;
                case "<=":
                    node.Push(ASTNodeType.LESS_THAN_OR_EQUAL, null, _curLine);
                    break;
                case ">":
                    node.Push(ASTNodeType.GREATER_THAN, null, _curLine);
                    break;
                case ">=":
                    node.Push(ASTNodeType.GREATER_THAN_OR_EQUAL, null, _curLine);
                    break;
                default:
                    throw new SyntaxError(node, "Invalid operator in binary expression");
            }
        }

        private static void ParseStatement(ASTNode node, string[] lexemes)
        {
            var statement = node.Push(ASTNodeType.STATEMENT, null, _curLine);

            // Examine the first word on the line
            switch (lexemes[0])
            {
                // Ignore comments
                case "#":
                case "//":
                    return;

                // Control flow statements are special
                case "if":
                    {
                        if (lexemes.Length <= 1)
                            throw new SyntaxError(node, "Script compilation error");

                        var t = statement.Push(ASTNodeType.IF, null, _curLine);
                        ParseLogicalExpression(t, lexemes.Slice(1, lexemes.Length - 1));
                        break;
                    }
                case "elseif":
                    {
                        if (lexemes.Length <= 1)
                            throw new SyntaxError(node, "Script compilation error");

                        var t = statement.Push(ASTNodeType.ELSEIF, null, _curLine);
                        ParseLogicalExpression(t, lexemes.Slice(1, lexemes.Length - 1));
                        break;
                    }
                case "else":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.ELSE, null, _curLine);
                    break;
                case "endif":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.ENDIF, null, _curLine);
                    break;
                case "while":
                    {
                        if (lexemes.Length <= 1)
                            throw new SyntaxError(node, "Script compilation error");

                        var t = statement.Push(ASTNodeType.WHILE, null, _curLine);
                        ParseLogicalExpression(t, lexemes.Slice(1, lexemes.Length - 1));
                        break;
                    }
                case "endwhile":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.ENDWHILE, null, _curLine);
                    break;
                case "for":
                    {
                        if (lexemes.Length <= 1)
                            throw new SyntaxError(node, "Script compilation error");

                        ParseForLoop(statement, lexemes.Slice(1, lexemes.Length - 1));
                        break;
                    }
                case "foreach":
                    {
                        if (lexemes.Length != 4)
                            throw new SyntaxError(node, "Script compilation error");

                        ParseForEachLoop(statement, lexemes.Slice(1, lexemes.Length - 1));
                        break;
                    }
                case "endfor":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.ENDFOR, null, _curLine);
                    break;
                case "break":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.BREAK, null, _curLine);
                    break;
                case "continue":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.CONTINUE, null, _curLine);
                    break;
                case "stop":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.STOP, null, _curLine);
                    break;
                case "replay":
                case "loop":
                    if (lexemes.Length > 1)
                        throw new SyntaxError(node, "Script compilation error");

                    statement.Push(ASTNodeType.REPLAY, null, _curLine);
                    break;
                default:
                    // It's a regular statement.
                    ParseCommand(statement, lexemes[0]);

                    foreach (var lexeme in lexemes.Slice(1, lexemes.Length - 1))
                    {
                        ParseValue(statement, lexeme, ASTNodeType.STRING);
                    }
                    break;
            }

        }

        private static bool IsOperator(string lexeme)
        {
            switch (lexeme)
            {
                case "==":
                case "=":
                case "!=":
                case "<":
                case "<=":
                case ">":
                case ">=":
                    return true;
            }

            return false;
        }

        private static void ParseLogicalExpression(ASTNode node, string[] lexemes)
        {
            // The steam language supports logical operators 'and' and 'or'.
            // Catch those and split the expression into pieces first.
            // Fortunately, it does not support parenthesis.
            var expr = node;
            bool logical = false;
            int start = 0;

            for (int i = start; i < lexemes.Length; i++)
            {
                if (lexemes[i] == "and" || lexemes[i] == "or")
                {
                    if (!logical)
                    {
                        expr = node.Push(ASTNodeType.LOGICAL_EXPRESSION, null, _curLine);
                        logical = true;
                    }

                    ParseExpression(expr, lexemes.Slice(start, i - 1));
                    start = i + 1;
                    expr.Push(lexemes[i] == "and" ? ASTNodeType.AND : ASTNodeType.OR, null, _curLine);

                }
            }

            ParseExpression(expr, lexemes.Slice(start, lexemes.Length - 1));
        }

        private static void ParseExpression(ASTNode node, string[] lexemes)
        {

            // The steam language supports both unary and
            // binary expressions. First determine what type
            // we have here.

            bool unary = false;
            bool binary = false;

            foreach (var lexeme in lexemes)
            {
                if (lexeme == "not")
                {
                    // The not lexeme only appears in unary expressions.
                    // Binary expressions would use "!=".
                    unary = true;
                }
                else if (IsOperator(lexeme))
                {
                    // Operators mean it is a binary expression.
                    binary = true;
                }
            }

            // If no operators appeared, it's a unary expression
            if (!unary && !binary)
                unary = true;

            if (unary && binary)
                throw new SyntaxError(node, "Invalid expression");

            if (unary)
                ParseUnaryExpression(node, lexemes);
            else
                ParseBinaryExpression(node, lexemes);
        }

        private static void ParseUnaryExpression(ASTNode node, string[] lexemes)
        {
            var expr = node.Push(ASTNodeType.UNARY_EXPRESSION, null, _curLine);

            int i = 0;

            if (lexemes[i] == "not")
            {
                expr.Push(ASTNodeType.NOT, null, _curLine);
                i++;
            }

            ParseOperand(expr, lexemes[i++]);

            for (; i < lexemes.Length; i++)
            {
                ParseValue(expr, lexemes[i], ASTNodeType.STRING);
            }
        }

        private static void ParseBinaryExpression(ASTNode node, string[] lexemes)
        {
            var expr = node.Push(ASTNodeType.BINARY_EXPRESSION, null, _curLine);

            int i = 0;

            // The expressions on either side of the operator can be values
            // or operands that need to be evaluated.
            ParseOperand(expr, lexemes[i++]);

            for (; i < lexemes.Length; i++)
            {
                if (IsOperator(lexemes[i]))
                    break;

                ParseValue(expr, lexemes[i], ASTNodeType.STRING);
            }

            ParseOperator(expr, lexemes[i++]);

            ParseOperand(expr, lexemes[i++]);

            for (; i < lexemes.Length; i++)
            {
                if (IsOperator(lexemes[i]))
                    break;

                ParseValue(expr, lexemes[i], ASTNodeType.STRING);
            }
        }

        private static void ParseForLoop(ASTNode statement, string[] lexemes)
        {
            // There are 4 variants of for loops in steam. The simplest two just
            // iterate a fixed number of times. The other two iterate
            // parts of lists. We call those second two FOREACH.

            // We're intentionally deprecating two of the variants here.
            // The for X to Y variant, where both X and Y are integers, 
            // is useless. It can be just written as for X.
            // The for X to Y in LIST variant may have some niche uses, but
            // is annoying to implement.

            // The for X loop remains supported as is, while the
            // for X in LIST form is actually transformed into a foreach
            // statement.

            if (lexemes.Length == 1)
            {
                // for X
                var loop = statement.Push(ASTNodeType.FOR, null, _curLine);

                ParseValue(loop, lexemes[0], ASTNodeType.STRING);

            }
            else if (lexemes.Length == 3 && lexemes[1] == "to")
            {
                // for X to LIST
                var loop = statement.Push(ASTNodeType.FOREACH, null, _curLine);

                loop.Push(ASTNodeType.STRING, lexemes[2], _curLine);
                loop.Push(ASTNodeType.LIST, lexemes[2].Substring(0, lexemes[2].Length - 2), _curLine);
            }
            else
            {
                throw new SyntaxError(statement, "Invalid for loop");
            }
        }

        private static void ParseForEachLoop(ASTNode statement, string[] lexemes)
        {
            // foreach X in LIST
            var loop = statement.Push(ASTNodeType.FOREACH, null, _curLine);

            if (lexemes[1] != "in")
                throw new SyntaxError(statement, "Invalid foreach loop");

            // This is the iterator name
            ParseValue(loop, lexemes[0], ASTNodeType.STRING);
            loop.Push(ASTNodeType.LIST, lexemes[2], _curLine);
        }
    }
}
