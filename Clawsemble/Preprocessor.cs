using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace Clawsemble
{
    public class Preprocessor
    {
        public List<Token> Tokens;
        public List<string> Files;
        public Dictionary<string, string> Meta;
        public Dictionary<string, string> Defines;
        public Dictionary<string, Constant> Constants;

        public Preprocessor()
        {
            Tokens = new List<Token>();
            Files = new List<string>();
            Meta = new Dictionary<string, string>();
            Defines = new Dictionary<string, string>();
            Constants = new Dictionary<string, Constant>();
        }

        public void Clear()
        {
            Tokens.Clear();
            Files.Clear();
            Meta.Clear();
            Defines.Clear();
            Constants.Clear();
        }

        public CompileContext Export()
        {
            // TODO
            return new CompileContext();
        }

        public void DoFile(string Filename)
        {
            if (!File.Exists(Filename))
                throw new FileNotFoundException("Included file not found!", Filename);
            List<Token> ftokens = Tokenizer.Tokenize(File.OpenRead(Filename));
            string directive = "";
            int ifdepth = 0;

            Files.Add(Filename);

            for (int i = 0; i < ftokens.Count; i++) {
                if (ftokens[i].Type == TokenType.PreprocessorDirective) {
                    directive = ftokens[i].Content.Trim().ToLower();
                    if (directive == "inc" || directive == "include") {
                        if (i + 1 < ftokens.Count) {
                            if (ftokens[i + 1].Type == TokenType.String) {
                                DoFile(ftokens[++i].Content);
                            } else
                                throw new Exception("Expected string!");
                        } else
                            throw new Exception("Unexpected end of file!");
                    } else if (directive == "ifdef" || directive == "ifdefined") {
                        // TODO
                    } else if (directive == "ifndef" || directive == "ifnotdefined") {
                        // TODO
                    } else if (directive == "if") {
                        ifdepth++;
                        // TODO
                    } else if (directive == "elif" || directive == "elseif") {
                        // TODO
                    } else if (directive == "endif") {
                        ifdepth--;
                        // TODO
                    } else if (directive == "def" || directive == "define") {
                        string key = "", value = "";
                        TokenType type = TokenType.Empty;
                        if (i + 2 < ftokens.Count) {
                            if (ftokens[i + 1].Type == TokenType.Word)
                                key = ftokens[++i].Content.Trim();
                            else
                                throw new Exception("Expected word!");
                            if (ftokens[i + 1].Type == TokenType.String ||
                                ftokens[i + 1].Type == TokenType.HexadecimalEscape ||
                                ftokens[i + 1].Type == TokenType.Character ||
                                ftokens[i + 1].Type == TokenType.Number) {
                                value = ftokens[++i].Content;
                                type = ftokens[i].Type;
                            }
                        } else if (i + 1 < ftokens.Count) {
                            if (ftokens[i + 1].Type == TokenType.Word)
                                key = ftokens[++i].Content.Trim();
                            else
                                throw new Exception("Expected word!");
                        } else
                            throw new Exception("Unexpected end of file!");
						
                        Constants.Add(key, new Constant(type, value));
                    } else if (directive == "undef" || directive == "undefine") {
                        if (i + 1 < ftokens.Count) {
                            if (ftokens[++i].Type == TokenType.Word) {
                                if (Constants.ContainsKey(ftokens[i].Content.Trim()))
                                    Constants.Remove(ftokens[i].Content.Trim());
                            } else
                                throw new Exception("Expected word!");
                        } else
                            throw new Exception("Unexpected end of file!");
                    } else
                        throw new Exception("Unknown preprocessor directive!");
                } else if (ftokens[i].Type == TokenType.ParanthesisOpen) { // we got an expression, nice
                    Constant eval = EvaluateExpression(ref i, ftokens);
                    if (eval.Type == ConstantType.Numeric) {
                        Tokens.Add(new Token() { Type = TokenType.Number, Content = eval.Number.ToString() });
                    } else if (eval.Type == ConstantType.String) {
                        Tokens.Add(new Token() { Type = TokenType.String, Content = eval.String });
                    } else
                        throw new Exception("Empty expression!");
                } else if (ftokens[i].Type == TokenType.Number || ftokens[i].Type == TokenType.Character ||
                           ftokens[i].Type == TokenType.HexadecimalEscape) {
                    Constant eval;
                    if (Constant.TryParse(ftokens[i], out eval)) {
                        Tokens.Add(new Token() { Type = TokenType.Number, Content = eval.Number.ToString(),
                            Line = ftokens[i].Line, File = (uint)Files.Count
                        });
                    } else
                        throw new Exception("Cannot parse constant!");
                } else if (ftokens[i].Type == TokenType.Word) {
                    if (Constants.ContainsKey(ftokens[i].Content)) {
                        Constant eval = Constants[ftokens[i].Content];

                        if (eval.Type == ConstantType.Numeric) {
                            Tokens.Add(new Token() { Type = TokenType.Number, Content = eval.Number.ToString() });
                        } else if (eval.Type == ConstantType.String)
                            Tokens.Add(new Token() { Type = TokenType.String, Content = eval.String });
                    } else { // if the word is not resolvable, add it again
                        Tokens.Add(new Token() { Type = ftokens[i].Type, Content = ftokens[i].Content,
                            Line = ftokens[i].Line, File = (uint)Files.Count
                        });
                    }
                } else if (ftokens[i].Type == TokenType.Break && Tokens.Count > 0) {
                    if (Tokens[Tokens.Count - 1].Type != TokenType.Break) {
                        Tokens.Add(new Token() { Type = ftokens[i].Type, Content = ftokens[i].Content,
                            Line = ftokens[i].Line, File = (uint)Files.Count
                        });
                    } // else drop it as we don't want newlines following each other
                } else if (ftokens[i].Type != TokenType.Comment && ftokens[i].Type != TokenType.CharacterEscape) {
                    // we cannot deal with the token just yet
                    Tokens.Add(new Token() { Type = ftokens[i].Type, Content = ftokens[i].Content,
                        Line = ftokens[i].Line, File = (uint)Files.Count
                    });
                }
            }
        }

        private Constant EvaluateExpression(ref int Pointer, List<Token> Tokens)
        {
            var valstack = new Stack<Constant>();
            var opstack = new Stack<Token>();

            for (;; Pointer++) {
                if (Tokens[Pointer].Type == TokenType.Break || Tokens[Pointer].Type == TokenType.Seperator) {
                    break;
                } else if (Tokens[Pointer].Type == TokenType.Number ||
                           Tokens[Pointer].Type == TokenType.Character ||
                           Tokens[Pointer].Type == TokenType.HexadecimalEscape) {
                    valstack.Push(new Constant(Tokens[Pointer]));
                } else if (Tokens[Pointer].Type == TokenType.Word) {
                    if (Constants.ContainsKey(Tokens[Pointer].Content.Trim())) {
                        Constant cvar = Constants[Tokens[Pointer].Content.Trim()];
                        if (cvar.Type != ConstantType.Empty)
                            valstack.Push(cvar);
                        else
                            throw new Exception("Constant is empty!");
                    } else
                        throw new Exception("Constant not found!");
                } else if (Tokens[Pointer].Type == TokenType.ParanthesisOpen) {
                    opstack.Push(Tokens[Pointer]);
                } else if (Tokens[Pointer].Type == TokenType.ParanthesisClose) {
                    while (opstack.Count > 0) {
                        if (opstack.Peek().Type == TokenType.ParanthesisOpen) {
                            opstack.Pop();
                            break;
                        } else
                            ExecOp(opstack.Pop(), valstack);
                    }
                } else {
                    // TODO: precedence checking and stuff
                }
            }

            if (opstack.Count > 0) {
                while (opstack.Count > 0) {
                    Token optok = opstack.Pop();
                    if (optok.Type == TokenType.ParanthesisOpen)
                        throw new Exception("Missmatched paranthesis!");
                    else if (!IsOp(optok))
                        throw new Exception("Invalid non-op token in expression!");
                    else
                        ExecOp(optok, valstack);
                }
            }
            if (valstack.Count > 1)
                throw new Exception("Invalid expression!");
            else if (valstack.Count == 0)
                valstack.Push(new Constant(0));
            if (valstack.Peek().Type == ConstantType.Empty)
                throw new Exception("Expression result is invalid!");

            return valstack.Pop(); // result can be a number or string
        }

        private void ExecOp(Token Token, Stack<Constant> Stack)
        {
            if (Token.Type == TokenType.BitwiseNot || Token.Type == TokenType.Not) {
                if (Stack.Count < 1)
                    throw new Exception("Stack underflow!");
                Constant ct0 = Stack.Pop();

                if (Token.Type == TokenType.BitwiseNot)
                    Stack.Push(new Constant(~ct0.Number));
                else if (Token.Type == TokenType.Not)
                    Stack.Push(new Constant((ct0.Number > 0) ? 0 : 1));
                return;
            } else {
                if (Stack.Count < 2)
                    throw new Exception("Stack underflow!");
                Constant ct0 = Stack.Pop(), ct1 = Stack.Pop();
                if (ct0.Type == ConstantType.String || ct1.Type == ConstantType.String) {
                    if (ct0.Type == ConstantType.Numeric)
                        ct0 = new Constant(ct0.Number.ToString());
                    else if (ct1.Type == ConstantType.Numeric)
                        ct1 = new Constant(ct1.Number.ToString());
                    
                    if (Token.Type == TokenType.Equal)
                        Stack.Push(new Constant((ct0.String == ct1.String) ? 1 : 0));
                    else if (Token.Type == TokenType.NotEqual)
                        Stack.Push(new Constant((ct0.String != ct1.String) ? 1 : 0));
                    else if (Token.Type == TokenType.Plus)
                        Stack.Push(new Constant(ct1.String + ct0.String));
                    else if (Token.Type == TokenType.Minus) {
                        if (ct1.String.EndsWith(ct0.String)) {
                            Stack.Push(new Constant(ct1.String.Substring(0, ct1.String.LastIndexOf(ct0.String))));
                        } else
                            Stack.Push(ct1);
                    } else if (Token.Type == TokenType.Divide)
                        Stack.Push(new Constant(ct1.String.Replace(ct0.String, "")));
                    else
                        throw new Exception("Invalid operation on string!");
                } else if (ct0.Type == ConstantType.Numeric && ct1.Type == ConstantType.Numeric) {
                    switch (Token.Type) {
                    case TokenType.BitshiftLeft:
                        Stack.Push(new Constant(ct1.Number << (int)ct0.Number));
                        return;
                    case TokenType.BitshiftRight:
                        Stack.Push(new Constant(ct1.Number >> (int)ct0.Number));
                        return;
                    case TokenType.BitwiseAnd:
                        Stack.Push(new Constant(ct1.Number & ct0.Number));
                        return;
                    case TokenType.BitwiseOr:
                        Stack.Push(new Constant(ct1.Number | ct0.Number));
                        return;
                    case TokenType.BitwiseXOr:
                        Stack.Push(new Constant(ct1.Number ^ ct0.Number));
                        return;
                    case TokenType.Divide:
                        Stack.Push(new Constant(ct1.Number / ct0.Number));
                        return;
                    case TokenType.GreaterEqual:
                        Stack.Push(new Constant((ct1.Number >= ct0.Number) ? 1 : 0));
                        return;
                    case TokenType.GreaterThan:
                        Stack.Push(new Constant((ct1.Number > ct0.Number) ? 1 : 0));
                        return;
                    case TokenType.LessEqual:
                        Stack.Push(new Constant((ct1.Number <= ct0.Number) ? 1 : 0));
                        return;
                    case TokenType.LessThan:
                        Stack.Push(new Constant((ct1.Number < ct0.Number) ? 1 : 0));
                        return;
                    case TokenType.LogicalAnd:
                        Stack.Push(new Constant(((ct1.Number > 0) && (ct0.Number > 0)) ? 1 : 0));
                        return;
                    case TokenType.LogicalOr:
                        Stack.Push(new Constant(((ct1.Number > 0) || (ct0.Number > 0)) ? 1 : 0));
                        return;
                    case TokenType.Minus:
                        Stack.Push(new Constant(ct1.Number - ct0.Number));
                        return;
                    case TokenType.Modulo:
                        Stack.Push(new Constant(ct1.Number % ct0.Number));
                        return;
                    case TokenType.Multiply:
                        Stack.Push(new Constant(ct1.Number * ct0.Number));
                        return;
                    case TokenType.NotEqual:
                        Stack.Push(new Constant((ct1.Number != ct0.Number) ? 1 : 0));
                        return;
                    case TokenType.Plus:
                        Stack.Push(new Constant(ct1.Number + ct0.Number));
                        return;
                    default:
                        return;
                    }
                } else
                    throw new Exception("Type missmatch!");
            }
        }

        private bool IsOp(Token Token)
        {
            return (OpPrecedence(Token) > 0);
        }

        private int OpPrecedence(Token Token)
        {
            if (Token.Type == TokenType.Plus || Token.Type == TokenType.Minus ||
                Token.Type == TokenType.Not || Token.Type == TokenType.BitwiseNot)
                return 1;
            if (Token.Type == TokenType.Multiply || Token.Type == TokenType.Divide ||
                Token.Type == TokenType.Modulo)
                return 2;
            if (Token.Type == TokenType.Plus || Token.Type == TokenType.Minus)
                return 3;
            if (Token.Type == TokenType.BitshiftLeft || Token.Type == TokenType.BitshiftRight)
                return 4;
            if (Token.Type == TokenType.GreaterThan || Token.Type == TokenType.LessThan ||
                Token.Type == TokenType.GreaterEqual || Token.Type == TokenType.LessEqual)
                return 5;
            if (Token.Type == TokenType.Equal || Token.Type == TokenType.NotEqual)
                return 6;
            if (Token.Type == TokenType.BitwiseAnd)
                return 7;
            if (Token.Type == TokenType.BitwiseXOr)
                return 8;
            if (Token.Type == TokenType.BitwiseOr)
                return 9;
            if (Token.Type == TokenType.LogicalAnd)
                return 10;
            if (Token.Type == TokenType.LogicalOr)
                return 11;
            return 0;
        }

    }
}

