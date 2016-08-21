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

            while (!reader.EndOfStream) {
                char chr = (char)reader.Read();

                if ((type == TokenType.Comment && chr != '\r' && chr != '\n') ||
                    (type == TokenType.String && chr != '"' && chr != '\r' && chr != '\n') ||
                    (type == TokenType.Character && chr != '\'' && chr != '\r' && chr != '\n' && sb.Length < 3) ||
                    (type == TokenType.CharacterRemove && sb.Length < 1)) {
                    sb.Append(chr);
                    if (type == TokenType.CharacterRemove)
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '\n' || chr == '\r') {
                    if (type == TokenType.CharacterRemove || type == TokenType.Character) {
                        sb.Append(chr);
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else if (chr == '\r') {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.Break;
                    } else if (chr == '\n') {
                        if (type != TokenType.Break)
                            FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.Break;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else
                        throw new Exception("You shouldn't be here!");
                } else if (chr == ' ' || chr == '\t') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == ';') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.Comment;
                } else if (chr == '\\') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.CharacterRemove;
                } else if (chr == '"') {
                    if (type == TokenType.String) {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.String;
                    }
                } else if (chr == '\'') {
                    if (type == TokenType.Character) {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.Character;
                    }
                } else if (chr == '.') {
                    if (type == TokenType.Empty)
                        type = TokenType.CompilerDirective;
                    else
                        type = TokenType.Error;
                } else if (chr == '#') {
                    if (type == TokenType.Empty)
                        type = TokenType.PreprocessorDirective;
                    else {
                        type = TokenType.Error;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    }
                } else if (chr == '$') {
                    if (type != TokenType.HexadecimalEscape) {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.HexadecimalEscape;
                    } else {
                        type = TokenType.Error;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    }
                } else if (chr == ':' || chr == ',') {
                    if (type != TokenType.Seperator) {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.Seperator;
                    } else {
                        type = TokenType.Break;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    }
                } else if (chr == '!') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.Not;
                } else if (chr == '{') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.ArrayOpen;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '}') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.ArrayClose;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '(') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.ParanthesisOpen;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == ')') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.ParanthesisClose;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '<') {
                    if (type == TokenType.LessThan) {
                        type = TokenType.BitshiftLeft;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.LessThan;
                    }
                } else if (chr == '=') {
                    if (type == TokenType.Assign) {
                        type = TokenType.Equal;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else if (type == TokenType.Not) {
                        type = TokenType.NotEqual;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else if (type == TokenType.GreaterThan) {
                        type = TokenType.GreaterEqual;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else if (type == TokenType.LessThan) {
                        type = TokenType.LessEqual;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.Assign;
                    }
                } else if (chr == '>') {
                    if (type == TokenType.GreaterThan) {
                        type = TokenType.BitshiftRight;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.LessThan;
                    }
                } else if (chr == '&') {
                    if (type == TokenType.BitwiseAnd) {
                        type = TokenType.LogicalAnd;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.BitwiseAnd;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    }
                } else if (chr == '|') {
                    if (type == TokenType.BitwiseOr) {
                        type = TokenType.LogicalOr;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    } else {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.BitwiseOr;
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                    }
                } else if (chr == '+') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.Plus;
                    if (tokens.Count > 0) {
                        if (tokens[tokens.Count - 1].Type == TokenType.Number)
                            FinishToken(tokens, ref type, ref pos, ref line, sb);
                    }
                } else if (chr == '-') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.Minus;
                    if (tokens.Count > 0) {
                        
                        if (tokens[tokens.Count - 1].Type == TokenType.Number)
                            FinishToken(tokens, ref type, ref pos, ref line, sb);
                    }
                } else if (chr == '*') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.Multiply;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '/') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.Divide;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '%') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.Modulo;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '^') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.BitwiseXOr;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr == '~') {
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                    type = TokenType.BitwiseNot;
                    FinishToken(tokens, ref type, ref pos, ref line, sb);
                } else if (chr >= '0' && chr <= '9') {
                    if (type == TokenType.Empty || type == TokenType.Minus || type == TokenType.Plus) {
                        if (type == TokenType.Minus)
                            sb.Append('-');
                        type = TokenType.Number;
                    } else if (type != TokenType.Number && type != TokenType.Word && type != TokenType.HexadecimalEscape) {
                        FinishToken(tokens, ref type, ref pos, ref line, sb);
                        type = TokenType.Number;
                    }
                    sb.Append(chr);
                } else if (((chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z') || chr == '_')
                           && type != TokenType.HexadecimalEscape && type != TokenType.Number) {
                    if (type == TokenType.Empty)
                        type = TokenType.Word;
                    sb.Append(chr);
                } else if (((chr >= 'A' && chr <= 'F') || (chr >= 'a' && chr <= 'f'))
                           && type == TokenType.HexadecimalEscape) {
                    sb.Append(chr);
                } else {
                    type = TokenType.Error;
                    sb.Append(chr);
                }
            }
				
            reader.Dispose();

            FinishToken(tokens, ref type, ref pos, ref line, sb);
            tokens.Add(new Token() { Type = TokenType.Break, Position = pos, Line = line, File = 0 });
            return tokens;
        }

        private static void FinishToken(List<Token> Tokens, ref TokenType Type, ref uint Position, ref uint Line, StringBuilder Builder)
        {
            Position++;

            if (Builder.Length > 0) {
                string content;

                if (Type == TokenType.Empty)
                    Type = TokenType.Error;

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
            } else if (Type != TokenType.Empty) {
                Tokens.Add(new Token() { Type = Type, Position = Position, Line = Line, File = 0 });
                if (Type == TokenType.Break) {
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

