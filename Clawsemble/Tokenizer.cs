using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Clawsemble
{
    public static class Tokenizer
    {
        public static List<Token> Tokenize(Stream Stream)
        {
            var reader = new StreamReader(Stream);
            var tokens = new List<Token>();

            var sb = new StringBuilder();
            var type = TokenType.Empty;
            uint line = 1, pos = 1;
            bool multiline = false, inEscape = false, hardBreak = false;

            while (!reader.EndOfStream) {
                char chr = (char)reader.Read();

                if ((type == TokenType.Comment && chr != '\r' && chr != '\n') ||
                    (type == TokenType.String && (chr != '"' || inEscape)) ||
                    (type == TokenType.Character && chr != '\'' && chr != '\r' && chr != '\n' && sb.Length < 3)) {
                    if (type == TokenType.String) {
                        if (chr == '\\')
                            inEscape = !inEscape;
                        else
                            inEscape = false;
                    }
                    sb.Append(chr);
                } else if (chr == '\n' || chr == '\r') {
                    if (type == TokenType.Character) {
                        sb.Append(chr);
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else if (multiline) {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else if (chr == '\r') {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.Break;
                        hardBreak = true;
                    } else if (chr == '\n') {
                        if (type != TokenType.Break)
                            FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.Break;
                        hardBreak = true;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == ' ' || chr == '\t') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr == ';') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.Comment;
                } else if (chr == '"') {
                    if (type == TokenType.String) {
                        inEscape = false;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.String;
                    }
                } else if (chr == '\'') {
                    if (type == TokenType.Character) {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.Character;
                    }
                } else if (chr == '.') {
                    if (type == TokenType.Empty)
                        type = TokenType.CompilerDirective;
                    else
                        type = TokenType.Invalid;
                } else if (chr == '#') {
                    if (type == TokenType.Empty)
                        type = TokenType.PreprocessorDirective;
                    else {
                        type = TokenType.Invalid;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == '$') {
                    if (type != TokenType.Hexadecimal) {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.Hexadecimal;
                    } else {
                        type = TokenType.Invalid;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == ':' || chr == ',') {
                    if (type != TokenType.Seperator) {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.Seperator;
                    } else {
                        type = TokenType.Break;
                        hardBreak = false;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == '!') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.Not;
                } else if (chr == '{') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    if (multiline) {
                        type = TokenType.Unexpected;
                        sb.Append(chr);
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else
                        multiline = true;
                } else if (chr == '}') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    if (!multiline) {
                        type = TokenType.Unexpected;
                        sb.Append(chr);
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else
                        multiline = false;
                } else if (chr == '(') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.ParanthesisOpen;
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr == ')') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.ParanthesisClose;
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr == '<') {
                    if (type == TokenType.LessThan) {
                        type = TokenType.BitshiftLeft;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.LessThan;
                    }
                } else if (chr == '=') {
                    if (type == TokenType.Assign) {
                        type = TokenType.Equal;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else if (type == TokenType.Not) {
                        type = TokenType.NotEqual;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else if (type == TokenType.GreaterThan) {
                        type = TokenType.GreaterEqual;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else if (type == TokenType.LessThan) {
                        type = TokenType.LessEqual;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.Assign;
                    }
                } else if (chr == '>') {
                    if (type == TokenType.GreaterThan) {
                        type = TokenType.BitshiftRight;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.LessThan;
                    }
                } else if (chr == '&') {
                    if (type == TokenType.BitwiseAnd) {
                        type = TokenType.LogicalAnd;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.BitwiseAnd;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == '|') {
                    if (type == TokenType.BitwiseOr) {
                        type = TokenType.LogicalOr;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.BitwiseOr;
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == '+') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.Plus;
                    if (tokens.Count > 0) {
                        if (tokens[tokens.Count - 1].Type == TokenType.Number)
                            FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == '-') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.Minus;
                    if (tokens.Count > 0) {
                        
                        if (tokens[tokens.Count - 1].Type == TokenType.Number)
                            FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    }
                } else if (chr == '*') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.Multiply;
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr == '/') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.Divide;
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr == '%') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.Modulo;
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr == '^') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.BitwiseXOr;
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr == '~') {
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    type = TokenType.BitwiseNot;
                    FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                } else if (chr >= '0' && chr <= '9') {
                    if (type == TokenType.Empty || type == TokenType.Minus || type == TokenType.Plus) {
                        if (type == TokenType.Minus)
                            sb.Append('-');
                        type = TokenType.Number;
                    } else if (type != TokenType.Number && type != TokenType.Word && type != TokenType.Hexadecimal) {
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                        type = TokenType.Number;
                    }
                    sb.Append(chr);
                } else if (((chr >= 'A' && chr <= 'F') || (chr >= 'a' && chr <= 'f'))
                           && type == TokenType.Hexadecimal) {
                    sb.Append(chr);
                } else if (((chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z') || chr == '_')
                           && type != TokenType.Hexadecimal && type != TokenType.Number) {
                    if (type == TokenType.Empty)
                        type = TokenType.Word;
                    if (type != TokenType.CompilerDirective && type != TokenType.PreprocessorDirective && type != TokenType.Word)
                        FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
                    sb.Append(chr);
                } else {
                    type = TokenType.Invalid;
                    sb.Append(chr);
                }
            }
				
            reader.Dispose();

            FinishToken(tokens, ref type, ref pos, ref line, ref hardBreak, sb);
            tokens.Add(new Token() { Type = TokenType.Break, Position = pos, Line = line, File = 0 });
            return tokens;
        }

        private static void FinishToken(List<Token> Tokens, ref TokenType Type, ref uint Position, ref uint Line, ref bool HardBreak, StringBuilder Builder)
        {
            Position++;

            if (Builder.Length > 0) {
                string content;

                if (Type == TokenType.Empty || Type == TokenType.Break)
                    Type = TokenType.Invalid;

                content = Builder.ToString();
                Builder.Clear();

                if (Type == TokenType.String || Type == TokenType.Character)
                    content = EscapeString(content);

                Tokens.Add(new Token() {
                    Type = Type,
                    Content = content,
                    Position = Position,
                    Line = Line,
                    File = 0
                });

                if (Logger.Priority == LogPriority.Debug)
                    Logger.Debug(string.Format("Adding token (Type: {0}, Content: '{1}', Position: {2}, Line: {3})",
                        Type.ToString(), content, Position, Line));
            } else if (Type != TokenType.Empty) {
                Tokens.Add(new Token() { Type = Type, Position = Position, Line = Line, File = 0 });

                if (Logger.Priority == LogPriority.Debug)
                    Logger.Debug(string.Format("Adding token (Type: {0}, Position: {1}, Line: {2})",
                        Type.ToString(), Position, Line));

                if (Type == TokenType.Break && HardBreak) {
                    HardBreak = false;
                    Position = 1;
                    Line++;
                }
            }
            Type = TokenType.Empty;
        }

        private static string EscapeString(string Input)
        {
            var regex = new Regex(@"\\(?:([tnrb])|(25[0-5]|2[0-4][0-9]|[01]?[0-9]?[0-9]))");
            return regex.Replace(Input, delegate(Match match) {
                if (match.Groups.Count == 3) {
                    string escape;

                    if (match.Groups[1].Length > 0) {
                        escape = match.Groups[1].Value.ToLower();

                        if (escape == "t") {
                            return "\t";
                        } else if (escape == "n") {
                            return "\n";
                        } else if (escape == "r") {
                            return "\r";
                        } else if (escape == "b") {
                            return "\b";
                        } else if (escape == @"\") {
                            return @"\";
                        } else if (escape == "\"") {
                            return "\"";
                        }
                    } else if (match.Groups[2].Length > 0) {
                        byte val;
                        escape = match.Groups[2].Value.ToLower();

                        if (byte.TryParse(escape, out val))
                            return ((char)val).ToString();
                    }
                }

                return match.Value;
            });
        }
    }
}

