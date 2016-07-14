using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace Clawsemble
{
    public class Preprocessor
    {
        public List<Token> Tokens;
        public Dictionary<string, string> Meta;
        public Dictionary<string, string> Defines;
        public Dictionary<string, Constant> Constants;

        public Preprocessor()
        {
            Tokens = new List<Token>();
            Meta = new Dictionary<string, string>();
            Defines = new Dictionary<string, string>();
            Constants = new Dictionary<string, Constant>();
        }

        public void Clear()
        {
            Tokens.Clear();
            Meta.Clear();
            Defines.Clear();
            Constants.Clear();
        }

        public void DoFile(string Filename)
        {
            List<Token> ftokens = Tokenizer.Tokenize(File.OpenRead(Filename));
            string directive = "";
            int ifdepth = 0;

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

                    } else if (directive == "ifndef" || directive == "ifnotdefined") {
						
                    } else if (directive == "if") {
                        ifdepth++;
                    } else if (directive == "elif" || directive == "elseif") {

                    } else if (directive == "endif") {
                        ifdepth--;
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
                        throw new Exception("Unknown compiler directive!");
                }
            }
        }

        private long EvaluateExpression(int Pointer, List<Token> Tokens)
        {
            var nbstack = new Stack<Constant>();
            var opstack = new Stack<Token>();

            for (;; Pointer++) {
                if (Tokens[Pointer].Type == TokenType.Break) {
                    break;
                } else if (Tokens[Pointer].Type == TokenType.Number ||
                           Tokens[Pointer].Type == TokenType.Character ||
                           Tokens[Pointer].Type == TokenType.HexadecimalEscape) {
                    nbstack.Push(new Constant(Tokens[Pointer]));
                } else if (Tokens[Pointer].Type == TokenType.Word) {
                    if (Constants.ContainsKey(Tokens[Pointer].Content.Trim())) {
                        Constant cvar = Constants[Tokens[Pointer].Content.Trim()];
                        if (cvar.Type != ConstantType.Empty)
                            nbstack.Push(cvar);
                        else
                            throw new Exception("Constant is empty!");
                    } else
                        throw new Exception("Constant not found!");
                } else if (Tokens[Pointer].Type == TokenType.ParanthesisOpen) {
                    opstack.Push(Token[Pointer]);
                } else if (Tokens[Pointer].Type == TokenType.ParanthesisClose) {
                    while (opstack.Count > 0) {
                        Token optok = opstack.Pop();
                        if (optok.Type == TokenType.ParanthesisClose) {
                            break;
                        }
                    }
                } else {

                }
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

