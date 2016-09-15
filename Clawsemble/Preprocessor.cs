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
        public Dictionary<string, Constant> Defines;
        public Dictionary<string, Constant> PreDefines;
        public List<InstructionSignature> ValidInstructions;

        public Preprocessor()
        {
            Tokens = new List<Token>();
            Files = new List<string>();
            Defines = new Dictionary<string, Constant>();
            PreDefines = new Dictionary<string, Constant>();
            AddPreDefinesRange(DefaultDefinitions.CompileList());
            ValidInstructions = DefaultInstructions.CompileList();
        }

        public void Clear()
        {
            Tokens.Clear();
            Files.Clear();
            Defines.Clear();
        }

        public void DoString(string Code)
        {
            DoString(Code, new Dictionary<string, Constant>());
        }
            
        public void DoString(string Code, Dictionary<string, Constant> Arguments)
        {
            var mstream = new MemoryStream();
            var writer = new StreamWriter(mstream);
            writer.Write(Code);
            writer.Flush();
            mstream.Position = 0;
            DoTokens(Tokenizer.Tokenize(mstream), Arguments);
        }

        public void DoFile(string Filename)
        {
            if (!File.Exists(Filename))
                throw new FileNotFoundException("Included file not found!", Filename);
            Files.Add(Filename);
            DoTokens(Tokenizer.Tokenize(File.OpenRead(Filename)), (uint)Files.Count);
        }

        public void DoTokens(List<Token> Tokens)
        {
            DoTokens(Tokens, new Dictionary<string, Constant>());
        }

        public void DoTokens(List<Token> Tokens, Dictionary<string, Constant> Arguments)
        {
            DoTokens(Tokens, Arguments, 0);
        }

        private void DoTokens(List<Token> Tokens, uint File)
        {
            DoTokens(Tokens, new Dictionary<string, Constant>(), 0);
        }

        private void DoTokens(List<Token> InputTokens, Dictionary<string, Constant> Arguments, uint File)
        {
            string directive;
            var ifStack = new ArbitraryStack<bool>();

            for (int ptr = 0; ptr < InputTokens.Count; ptr++) {
                if (InputTokens[ptr].Type == TokenType.PreprocessorDirective) {
                    if (string.IsNullOrEmpty(InputTokens[ptr].Content))
                        throw new CodeError(CodeErrorType.DirectiveInvalid, "Empty preprocessor directive!", InputTokens[ptr], GetFilename(File));

                    directive = InputTokens[ptr].Content.Trim().ToLower();
                    if (directive == "if" || directive == "elif" || directive == "elseif") {
                        if (directive == "elif" || directive == "elseif") {
                            if (ifStack.Count == 0)
                                throw new CodeError(CodeErrorType.IfMissmatched, "No preceeding opening if!", InputTokens[ptr], GetFilename(File));
                            if (ifStack.Peek()) {
                                try {
                                    SkipAhead(ref ptr, InputTokens, ifStack, true); // we have already successfully handled the if, skip to endif
                                } catch (CodeError error) {
                                    error.Filename = GetFilename(File);
                                    throw error;
                                }
                                continue;
                            }
                        }

                        if (!IsBeforeEOF(ptr, InputTokens.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1].Line, GetFilename(File));

                        if (InputTokens[++ptr].Type == TokenType.ParanthesisOpen) {
                            Constant eval;
                            bool result;
                            try {
                                eval = EvaluateExpression(ref ptr, InputTokens, Arguments);
                            } catch (CodeError error) {
                                error.Filename = GetFilename(File);
                                throw error;
                            }

                            if (eval.Type == ConstantType.Numeric)
                                result = (bool)(eval.Number > 0);
                            else if (eval.Type == ConstantType.String)
                                result = (bool)(!string.IsNullOrEmpty(eval.String));
                            else
                                result = false;

                            if (result) {
                                if (directive == "if") {
                                    ifStack.Push(true); // success, handle what follows
                                } else if (directive == "elif" || directive == "elseif") {
                                    ifStack.Change(true);
                                }
                            } else {
                                if (directive == "if") {
                                    ifStack.Push(false); // no success, keep searching
                                } else if (directive == "elif" || directive == "elseif") {
                                    ifStack.Change(false); // no success, keep searching
                                }

                                try {
                                    SkipAhead(ref ptr, InputTokens, ifStack, false); // skip to the next block
                                } catch (CodeError error) {
                                    error.Filename = GetFilename(File);
                                    throw error;
                                }
                            }
                        } else
                            throw new CodeError(CodeErrorType.ExpectedExpression, InputTokens[ptr], GetFilename(File));
                    } else if (directive == "ifdef" || directive == "ifndef" ||
                               directive == "elifdef" || directive == "elseifdef" || directive == "elifndef" || directive == "elseifndef") {
                        if (directive == "elifdef" || directive == "elseifdef" || directive == "elifndef" || directive == "elseifndef") {
                            if (ifStack.Count == 0)
                                throw new CodeError(CodeErrorType.IfMissmatched, "No preceeding opening if!", InputTokens[ptr], GetFilename(File));
                            if (ifStack.Peek()) {
                                try {
                                    SkipAhead(ref ptr, InputTokens, ifStack, true); // we have already successfully handled the if, skip to endif
                                } catch (CodeError error) {
                                    error.Filename = GetFilename(File);
                                    throw error;
                                }
                                continue;
                            }
                        }

                        if (InputTokens[++ptr].Type == TokenType.Word) {
                            if (!string.IsNullOrEmpty(InputTokens[ptr].Content)) {
                                if (DefineExists(InputTokens[ptr].Content.Trim(), Arguments)) { // the key exists
                                    if (directive == "ifdef") { // key exists AND we have an ifdef = success
                                        ifStack.Push(true); // add that if is solved
                                    } else if (directive == "ifndef") { // key exists AND we have an ifndef = no success
                                        ifStack.Push(false); // add that if is not solved and start search for an else
                                        try {
                                            SkipAhead(ref ptr, InputTokens, ifStack, false); // and let's skip to the next interesting block
                                        } catch (CodeError error) {
                                            error.Filename = GetFilename(File);
                                            throw error;
                                        }
                                    } else if (directive == "elifdef" || directive == "elseifdef") { // we have a key and ELSEifdef
                                        ifStack.Change(true); // change if to solved
                                    } else if (directive == "elifndef" || directive == "elseifndef") {
                                        ifStack.Change(false); // no success; lets search for an else
                                        try {
                                            SkipAhead(ref ptr, InputTokens, ifStack, false); // skip to the next interesting block
                                        } catch (CodeError error) {
                                            error.Filename = GetFilename(File);
                                            throw error;
                                        }
                                    }
                                } else {
                                    if (directive == "ifdef") { // key exists AND we have an ifdef = success
                                        ifStack.Push(false); // add that if is not solved and start search for an else
                                        try {
                                            SkipAhead(ref ptr, InputTokens, ifStack, false); // and let's skip to the next interesting block
                                        } catch (CodeError error) {
                                            error.Filename = GetFilename(File);
                                            throw error;
                                        }
                                    } else if (directive == "ifndef") { // key exists AND we have an ifndef = no success
                                        ifStack.Push(true); // add it, as the if is solved
                                    } else if (directive == "elifdef" || directive == "elseifdef") { // we have a key and ELSEifdef
                                        ifStack.Change(false); // no success; lets search for an else
                                        try {
                                            SkipAhead(ref ptr, InputTokens, ifStack, false); // skip to the next interesting block
                                        } catch (CodeError error) {
                                            error.Filename = GetFilename(File);
                                            throw error;
                                        }
                                    } else if (directive == "elifndef" || directive == "elseifndef") {
                                        ifStack.Change(true); // change if to solved
                                    }
                                }
                            } else
                                throw new CodeError(CodeErrorType.ConstantNotFound, "Constant name is empty!", InputTokens[ptr]);
                        } else
                            throw new CodeError(CodeErrorType.ExpectedWord, InputTokens[ptr]);

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "else") {
                        if (ifStack.Count == 0)
                            throw new CodeError(CodeErrorType.IfMissmatched, "No preceeding opening if!", InputTokens[ptr], GetFilename(File));
                        if (ifStack.Peek()) {
                            try {
                                SkipAhead(ref ptr, InputTokens, ifStack, true); // we have already successfully handled the if, skip to endif
                            } catch (CodeError error) {
                                error.Filename = GetFilename(File);
                                throw error;
                            }
                            continue;
                        }
                        ifStack.Change(true); // we assume nothing comes after the true block so we just jump into it

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "endif") {
                        if (ifStack.Count == 0)
                            throw new CodeError(CodeErrorType.IfMissmatched, "Too many closing if's!", InputTokens[ptr], GetFilename(File));
                        ifStack.Pop(); // remove one if from the stack

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "inc" || directive == "include") {
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[ptr + 1].Type == TokenType.String) {
                                DoFile(InputTokens[++ptr].Content);
                            } else
                                throw new CodeError(CodeErrorType.ExpectedString, InputTokens[ptr], GetFilename(File));
                        } else
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1].Line, GetFilename(File));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "macro") {
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            Constant eval;
                            string expression;

                            if (InputTokens[ptr + 1].Type == TokenType.ParanthesisOpen) {
                                ptr++;
                                try {
                                    eval = EvaluateExpression(ref ptr, InputTokens, Arguments);
                                } catch (CodeError error) {
                                    error.Filename = GetFilename(File);
                                    throw error;
                                }
                            } else if (InputTokens[ptr + 1].Type == TokenType.String || InputTokens[ptr + 1].Type == TokenType.Word) {
                                ptr++;
                                try {
                                    eval = EvaluateExpression(ref ptr, InputTokens, Arguments, 1);
                                } catch (CodeError error) {
                                    error.Filename = GetFilename(File);
                                    throw error;
                                }
                            } else
                                throw new CodeError(CodeErrorType.UnexpectedToken, "Expected string, word, or expression!",
                                    InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                            if (eval.Type != ConstantType.String)
                                throw new CodeError(CodeErrorType.ExpectedString, "Argument needs to resolve to a string!",
                                    InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                            expression = eval.String;
                            var args = new Dictionary<string, Constant>();

                            while (IsBeforeEOF(ptr, InputTokens.Count)) {
                                // check for optional seperator
                                if (IsBeforeEOF(ptr, InputTokens.Count, 2)) {
                                    if (InputTokens[ptr].Type == TokenType.Seperator) {
                                        ptr++;
                                        // Now, that there is a seperator, we require a new operator
                                        if (InputTokens[ptr].Type == TokenType.Break)
                                            throw new CodeError(CodeErrorType.UnexpectedBreak, InputTokens[ptr + 1], GetFilename(InputTokens[ptr + 1].File));
                                    }
                                }

                                if (InputTokens[ptr].Type == TokenType.ParanthesisOpen) {
                                    try {
                                        eval = EvaluateExpression(ref ptr, InputTokens, Arguments, 1);
                                    } catch (CodeError error) {
                                        error.Filename = GetFilename(File);
                                        throw error;
                                    }
                                } else if (InputTokens[ptr].Type == TokenType.Number || InputTokens[ptr].Type == TokenType.Character ||
                                           InputTokens[ptr].Type == TokenType.Hexadecimal || InputTokens[ptr].Type == TokenType.Word) {
                                    try {
                                        eval = EvaluateExpression(ref ptr, InputTokens, Arguments, 1);
                                    } catch (CodeError error) {
                                        error.Filename = GetFilename(File);
                                        throw error;
                                    }
                                } else
                                    throw new CodeError(CodeErrorType.ArgumentInvalid, InputTokens[ptr + 1], GetFilename(InputTokens[ptr + 1].File));

                                args.Add(string.Format("A{0}", args.Count), eval);

                                if (args.Count >= 9)
                                    break;
                                if (IsBeforeEOF(ptr, InputTokens.Count)) {
                                    if (InputTokens[ptr + 1].Type == TokenType.Break)
                                        break;
                                }
                            }

                            // check for break
                            if (IsBeforeEOF(ptr, InputTokens.Count)) {
                                if (InputTokens[++ptr].Type != TokenType.Break)
                                    throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                            }

                            // now evaluate the code
                            DoString(expression, args);
                        } else
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1].Line, GetFilename(File));
                    } else if (directive == "err" || directive == "error") {
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[ptr + 1].Type == TokenType.String) {
                                ptr++;
                                throw new CodeError(CodeErrorType.IntentionalError, InputTokens[ptr].Content, InputTokens[ptr].Line, GetFilename(File));
                            } else if (InputTokens[ptr + 1].Type == TokenType.Number || InputTokens[ptr + 1].Type == TokenType.Character ||
                                       InputTokens[ptr + 1].Type == TokenType.Hexadecimal || InputTokens[ptr + 1].Type == TokenType.Word) {
                                Constant eval;
                                ptr++;
                                try {
                                    eval = EvaluateExpression(ref ptr, InputTokens, Arguments, 1);
                                } catch (CodeError error) {
                                    error.Filename = GetFilename(File);
                                    throw error;
                                }
                                if (eval.Type == ConstantType.Numeric)
                                    throw new CodeError(CodeErrorType.IntentionalError, eval.Number.ToString(), InputTokens[ptr].Line, GetFilename(File));
                                else if (eval.Type == ConstantType.String)
                                    throw new CodeError(CodeErrorType.IntentionalError, eval.String, InputTokens[ptr].Line, GetFilename(File));
                                else
                                    throw new CodeError(CodeErrorType.ExpressionEmpty, InputTokens[ptr].Line, GetFilename(File));
                            } else if (InputTokens[ptr + 1].Type == TokenType.ParanthesisOpen) {
                                Constant eval;
                                ptr++;
                                try {
                                    eval = EvaluateExpression(ref ptr, InputTokens, Arguments);
                                } catch (CodeError error) {
                                    error.Filename = GetFilename(File);
                                    throw error;
                                }
                                if (eval.Type == ConstantType.Numeric)
                                    throw new CodeError(CodeErrorType.IntentionalError, eval.Number.ToString(), InputTokens[ptr].Line, GetFilename(File));
                                else if (eval.Type == ConstantType.String)
                                    throw new CodeError(CodeErrorType.IntentionalError, eval.String, InputTokens[ptr].Line, GetFilename(File));
                                else
                                    throw new CodeError(CodeErrorType.ExpressionEmpty, InputTokens[ptr].Line, GetFilename(File));
                            } else
                                throw new CodeError(CodeErrorType.IntentionalError, InputTokens[ptr].Line, GetFilename(File));
                        } else
                            throw new CodeError(CodeErrorType.IntentionalError, InputTokens[ptr].Line, GetFilename(File));  
                    } else if (directive == "def" || directive == "define") {
                        string key = "";
                        Constant value = new Constant(1);

                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Word)
                                throw new CodeError(CodeErrorType.ExpectedWord, InputTokens[ptr], GetFilename(File));
                            if (string.IsNullOrEmpty(InputTokens[ptr].Content))
                                throw new CodeError(CodeErrorType.ConstantNotFound, "Constant name is empty!", InputTokens[ptr], GetFilename(File));
                            if (IsNameReserved(InputTokens[ptr].Content.Trim(), Arguments))
                                throw new CodeError(CodeErrorType.WordInvalid, "Constant name cannot be used as it is reserved!",
                                    InputTokens[ptr], GetFilename(File));
                            key = InputTokens[ptr].Content.Trim();

                            if (IsBeforeEOF(ptr, InputTokens.Count)) {
                                // check for optional seperator
                                if (IsBeforeEOF(ptr, InputTokens.Count, 2)) {
                                    if (InputTokens[ptr + 1].Type == TokenType.Seperator)
                                        ptr++;
                                }

                                if (InputTokens[ptr + 1].Type == TokenType.String ||
                                    InputTokens[ptr + 1].Type == TokenType.Hexadecimal ||
                                    InputTokens[ptr + 1].Type == TokenType.Character ||
                                    InputTokens[ptr + 1].Type == TokenType.Number ||
                                    InputTokens[ptr + 1].Type == TokenType.Word) {
                                    ptr++;
                                    try {
                                        value = EvaluateExpression(ref ptr, InputTokens, Arguments, 1);
                                    } catch (CodeError error) {
                                        error.Filename = GetFilename(File);
                                        throw error;
                                    }
                                } else if (InputTokens[ptr + 1].Type == TokenType.ParanthesisOpen) {
                                    ptr++;
                                    try {
                                        value = EvaluateExpression(ref ptr, InputTokens, Arguments);
                                    } catch (CodeError error) {
                                        error.Filename = GetFilename(File);
                                        throw error;
                                    }
                                }
                            }
                        } else
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1].Line, GetFilename(File));

                        Defines.Add(key, value);

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "undef" || directive == "undefine") {
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type == TokenType.Word) {
                                if (string.IsNullOrEmpty(InputTokens[ptr].Content))
                                    throw new CodeError(CodeErrorType.ConstantNotFound, "Constant name is empty!", InputTokens[ptr], GetFilename(File));
                                if (IsNameReserved(InputTokens[ptr].Content.Trim(), Arguments))
                                    throw new CodeError(CodeErrorType.WordInvalid, "Constant name cannot be used as it is reserved!",
                                        InputTokens[ptr], GetFilename(File));
                                if (Defines.ContainsKey(InputTokens[ptr].Content.Trim()))
                                    Defines.Remove(InputTokens[ptr].Content.Trim());
                            } else
                                throw new CodeError(CodeErrorType.ExpectedWord, InputTokens[ptr], GetFilename(File));
                        } else
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1].Line, GetFilename(File));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else
                        throw new CodeError(CodeErrorType.DirectiveUnknown, InputTokens[ptr], GetFilename(File));
                } else if (InputTokens[ptr].Type == TokenType.ParanthesisOpen) { // we got an expression, nice
                    int origi = ptr;
                    Constant eval;
                    try {
                        eval = EvaluateExpression(ref ptr, InputTokens, Arguments);
                    } catch (CodeError error) {
                        error.Filename = GetFilename(File);
                        throw error;
                    }
                    if (eval.Type == ConstantType.Numeric) {
                        Tokens.Add(new Token() { Type = TokenType.Number, Content = eval.Number.ToString(),
                            Line = (uint)origi, File = (uint)Files.Count
                        });
                    } else if (eval.Type == ConstantType.String) {
                        Tokens.Add(new Token() { Type = TokenType.String, Content = eval.String,
                            Line = (uint)origi, File = (uint)Files.Count
                        });
                    } else
                        throw new CodeError(CodeErrorType.ExpressionEmpty, InputTokens[ptr].Line, GetFilename(File));
                } else if (InputTokens[ptr].Type == TokenType.Number || InputTokens[ptr].Type == TokenType.Character ||
                           InputTokens[ptr].Type == TokenType.Hexadecimal) {
                    Constant eval;
                    try {
                        eval = new Constant(InputTokens[ptr]);
                    } catch (Exception ex) {
                        throw new CodeError(CodeErrorType.ConstantInvalid, ex.Message, InputTokens[ptr], 0, InputTokens[ptr].Position, GetFilename(File));
                    }

                    Tokens.Add(new Token() { Type = TokenType.Number, Content = eval.Number.ToString(),
                        Position = InputTokens[ptr].Position, Line = InputTokens[ptr].Line, File = (uint)Files.Count
                    });
                } else if (InputTokens[ptr].Type == TokenType.Word) {
                    if (DefineExists(InputTokens[ptr].Content, Arguments)) {
                        Constant eval = GetDefine(InputTokens[ptr].Content, Arguments);

                        if (eval.Type == ConstantType.Numeric) {
                            Tokens.Add(new Token() { Type = TokenType.Number, Content = eval.Number.ToString(),
                                Position = InputTokens[ptr].Position, Line = InputTokens[ptr].Line, File = (uint)Files.Count
                            });
                        } else if (eval.Type == ConstantType.String)
                            Tokens.Add(new Token() { Type = TokenType.String, Content = eval.String,
                                Position = InputTokens[ptr].Position, Line = InputTokens[ptr].Line, File = (uint)Files.Count
                            });
                    } else { // if the word is not resolvable, add it again
                        Tokens.Add(new Token() { Type = InputTokens[ptr].Type, Content = InputTokens[ptr].Content,
                            Position = InputTokens[ptr].Position, Line = InputTokens[ptr].Line, File = (uint)Files.Count
                        });
                    }
                } else if (InputTokens[ptr].Type == TokenType.Break && Tokens.Count > 0) {
                    if (Tokens[Tokens.Count - 1].Type != TokenType.Break) {
                        Tokens.Add(new Token() { Type = InputTokens[ptr].Type, Content = InputTokens[ptr].Content,
                            Position = InputTokens[ptr].Position, Line = InputTokens[ptr].Line, File = (uint)Files.Count
                        });
                    } // else drop it as we don't want newlines following each other
                } else if (IsOp(InputTokens[ptr])) {
                    throw new CodeError(CodeErrorType.UnexpectedOperator, "Expressions need to be surrounded by parantheses!",
                        InputTokens[ptr], GetFilename(File));
                } else if (InputTokens[ptr].Type == TokenType.Invalid || InputTokens[ptr].Type == TokenType.Unexpected) { // catch the errors
                    throw new CodeError(CodeErrorType.TokenError, InputTokens[ptr], GetFilename(File));
                } else if (InputTokens[ptr].Type != TokenType.Comment && InputTokens[ptr].Type != TokenType.Empty) {
                    // we cannot deal with the token just yet
                    Tokens.Add(new Token() { Type = InputTokens[ptr].Type, Content = InputTokens[ptr].Content,
                        Position = InputTokens[ptr].Position, Line = InputTokens[ptr].Line, File = (uint)Files.Count
                    });
                }
            }

            if (ifStack.Count > 0)
                throw new CodeError(CodeErrorType.IfMissmatched, "Unterminated if's remaining!", GetFilename(File));
        }

        /* *
         * I assume this should skip until the next IF control statement of the same hierarchy
         * e.g.     IF      <= we are here and condition is not met
         *              IF
         *              ELIF
         *              ENDIF
         *          ELIF    <= we want to go here
         *          ...
         * */
        private void SkipAhead(ref int Pointer, List<Token> Tokens, ArbitraryStack<bool> IfStack, bool SkipToEnd)
        {
            int startDepth = IfStack.Count;
            string directive;

            for (; IsBeforeEOF(Pointer, Tokens.Count); Pointer++) {
                if (Tokens[Pointer].Type != TokenType.PreprocessorDirective)
                    continue;
                if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                    throw new CodeError(CodeErrorType.DirectiveInvalid, "Empty preprocessor directive!", Tokens[Pointer]);
                
                directive = Tokens[Pointer].Content.Trim().ToLower();
                if (directive == "endif") {
                    if (IfStack.Count == 0)
                        throw new CodeError(CodeErrorType.IfMissmatched, "Too many closing if's!", Tokens[Pointer]);

                    IfStack.Pop(); // pop the placeholders again
                    if (IfStack.Count < startDepth) // as soon as we reach the end of the target level
                        return; // we hit something interesting
                } else if (directive == "if" || directive == "ifdef" || directive == "ifndef") {
                    IfStack.Push(true); // just adding a placeholder onto the if stack; these blocks are not parsed but skipped
                } else if (IfStack.Count == startDepth && !SkipToEnd &&
                           (directive == "elif" || directive == "elseif" || directive == "else" ||
                           directive == "elifdef" || directive == "elseifdef")) {
                    Pointer--;
                    return; // we hit something interesting
                }
            }

            throw new CodeError(CodeErrorType.UnexpectedEOF, Tokens[Pointer - 1].Line);
        }

        private bool IsBeforeEOF(int Pointer, int AvlLength, int ReqLength = 1)
        {
            return (bool)(Pointer + ReqLength < AvlLength);
        }

        private Constant EvaluateExpression(ref int Pointer, List<Token> Tokens, int MaxLength = int.MaxValue)
        {
            return EvaluateExpression(ref Pointer, Tokens, new Dictionary<string, Constant>(), MaxLength);
        }

        private Constant EvaluateExpression(ref int Pointer, List<Token> Tokens, Dictionary<string,Constant> ExtArgs, int MaxLength = int.MaxValue)
        {
            var valstack = new Stack<Constant>();
            var opstack = new Stack<Token>();
            int startPtr = Pointer;

            for (; Pointer - startPtr < MaxLength; Pointer++) {
                if (Tokens[Pointer].Type == TokenType.Break || Tokens[Pointer].Type == TokenType.Seperator ||
                    Tokens[Pointer].Type == TokenType.Comment) {
                    break;
                } else if (Tokens[Pointer].Type == TokenType.Number ||
                           Tokens[Pointer].Type == TokenType.Character ||
                           Tokens[Pointer].Type == TokenType.Hexadecimal ||
                           Tokens[Pointer].Type == TokenType.String) {
                    Constant con;
                    try {
                        con = new Constant(Tokens[Pointer]);
                    } catch (Exception ex) {
                        throw new CodeError(CodeErrorType.ConstantInvalid, ex.Message, Tokens[Pointer]);
                    }
                    valstack.Push(con);
                } else if (Tokens[Pointer].Type == TokenType.Word) {
                    if (DefineExists(Tokens[Pointer].Content.Trim(), ExtArgs)) {
                        Constant cvar = GetDefine(Tokens[Pointer].Content.Trim(), ExtArgs);
                        if (cvar.Type != ConstantType.Empty)
                            valstack.Push(cvar);
                        else
                            throw new CodeError(CodeErrorType.ConstantEmpty, Tokens[Pointer].Line);
                    } else
                        throw new CodeError(CodeErrorType.ConstantNotFound, Tokens[Pointer].Line);
                } else if (Tokens[Pointer].Type == TokenType.ParanthesisOpen) {
                    opstack.Push(Tokens[Pointer]);
                } else if (Tokens[Pointer].Type == TokenType.ParanthesisClose) {
                    while (opstack.Count > 0) {
                        if (opstack.Peek().Type == TokenType.ParanthesisOpen) {
                            opstack.Pop();
                            break;
                        } else {
                            Token optok = opstack.Pop();
                            try {
                                ExecOp(optok, valstack);
                            } catch (CodeError error) {
                                error.Token = optok;
                                throw error;
                            }
                        }
                    }
                } else if (IsOp(Tokens[Pointer])) {
                    Token stackop, currop = Tokens[Pointer];

                    while (opstack.Count > 0) {
                        stackop = opstack.Pop();

                        if (OpAssociativity(currop) == Associativity.LeftToRight && OpPrecedence(currop) <= OpPrecedence(stackop) ||
                            OpAssociativity(currop) == Associativity.RightToLeft && OpPrecedence(currop) < OpPrecedence(stackop)) {
                            try {
                                ExecOp(stackop, valstack);
                            } catch (CodeError error) {
                                error.Token = stackop;
                                throw error;
                            }
                        } else {
                            opstack.Push(stackop);
                            break;
                        }
                    }

                    opstack.Push(currop);
                } else
                    throw new CodeError(CodeErrorType.UnexpectedToken, Tokens[Pointer]);
            }

            if (opstack.Count > 0) {
                while (opstack.Count > 0) {
                    Token optok = opstack.Pop();
                    if (optok.Type == TokenType.ParanthesisOpen)
                        throw new CodeError(CodeErrorType.MissmatchedParantheses, Tokens[Pointer - 1].Line);
                    else if (!IsOp(optok))
                        throw new CodeError(CodeErrorType.ExpectedOperator, optok);
                    else {
                        try {
                            ExecOp(optok, valstack);
                        } catch (CodeError error) {
                            error.Token = optok;
                            throw error;
                        }
                    }
                }
            }
            if (valstack.Count > 1)
                throw new CodeError(CodeErrorType.ExpressionInvalid, Tokens[Pointer - 1].Line);
            else if (valstack.Count == 0)
                valstack.Push(new Constant(0));
            if (valstack.Peek().Type == ConstantType.Empty)
                throw new CodeError(CodeErrorType.ExpressionInvalid, "Expression evaluates to nothing!", Tokens[Pointer - 1].Line);

            return valstack.Pop(); // result can be a number or string
        }

        private void ExecOp(Token Token, Stack<Constant> Stack)
        {
            if (Token.Type == TokenType.BitwiseNot || Token.Type == TokenType.Not) {
                if (Stack.Count < 1)
                    throw new CodeError(CodeErrorType.StackUnderflow, "Too many operators!");
                Constant ct0 = Stack.Pop();

                if (Token.Type == TokenType.BitwiseNot)
                    Stack.Push(new Constant(~ct0.Number));
                else if (Token.Type == TokenType.Not)
                    Stack.Push(new Constant((ct0.Number > 0) ? 0 : 1));
                return;
            } else {
                if (Stack.Count < 2)
                    throw new CodeError(CodeErrorType.StackUnderflow, "Too many operators!");
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
                        throw new CodeError(CodeErrorType.OperationInvalid, "Can't apply arithmetic operation to type of string!");
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
                        if (ct0.Number == 0)
                            throw new CodeError(CodeErrorType.DivisionByZero);
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
                        if (ct0.Number == 0)
                            throw new CodeError(CodeErrorType.DivisionByZero);
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
                    throw new CodeError(CodeErrorType.TypeMissmatch);
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

        private enum Associativity
        {
            None,
            LeftToRight,
            RightToLeft
        }

        private Associativity OpAssociativity(Token Token)
        {
            if (Token.Type == TokenType.Not || Token.Type == TokenType.BitwiseNot) {
                return Associativity.RightToLeft;
            } else if (Token.Type == TokenType.Multiply || Token.Type == TokenType.Divide ||
                       Token.Type == TokenType.Modulo ||
                       Token.Type == TokenType.Plus || Token.Type == TokenType.Minus ||
                       Token.Type == TokenType.BitshiftLeft || Token.Type == TokenType.BitshiftRight ||
                       Token.Type == TokenType.GreaterThan || Token.Type == TokenType.LessThan ||
                       Token.Type == TokenType.GreaterEqual || Token.Type == TokenType.LessEqual ||
                       Token.Type == TokenType.Equal || Token.Type == TokenType.NotEqual ||
                       Token.Type == TokenType.BitwiseAnd || Token.Type == TokenType.BitwiseXOr ||
                       Token.Type == TokenType.BitwiseOr || Token.Type == TokenType.LogicalAnd ||
                       Token.Type == TokenType.LogicalOr ||
                       Token.Type == TokenType.Plus || Token.Type == TokenType.Minus) {
                return Associativity.LeftToRight;
            } else
                return Associativity.None;
        }

        private bool IsNameReserved(string Key, Dictionary<string, Constant> ExtArgs)
        {
            // check external arguments
            if (ExtArgs.ContainsKey(Key))
                return true;

            return IsNameReserved(Key);
        }

        private bool IsNameReserved(string Key)
        {
            // check reserved words
            if (PreDefines.ContainsKey(Key))
                return true;

            // check collisions with instructions
            foreach (InstructionSignature instr in ValidInstructions) {
                if (instr.Mnemonic.ToLower() == Key.ToLower())
                    return true;
            }

            return false;
        }

        private void AddPreDefinesRange(Dictionary<string, Constant> Values)
        {
            foreach (KeyValuePair<string, Constant> kvp in Values)
                PreDefines.Add(kvp.Key, kvp.Value);
        }

        private bool DefineExists(string Key, Dictionary<string,Constant> ExtArgs)
        {
            return (bool)(Defines.ContainsKey(Key) || PreDefines.ContainsKey(Key) || ExtArgs.ContainsKey(Key));
        }

        private bool DefineExists(string Key)
        {
            return (bool)(Defines.ContainsKey(Key) || PreDefines.ContainsKey(Key));
        }

        private Constant GetDefine(string Key, Dictionary<string, Constant> ExtArgs)
        {
            if (ExtArgs.ContainsKey(Key))
                return ExtArgs[Key];
            return GetDefine(Key);
        }

        private Constant GetDefine(string Key)
        {
            if (PreDefines.ContainsKey(Key))
                return PreDefines[Key];
            if (Defines.ContainsKey(Key))
                return Defines[Key];
            throw new ArgumentException("Define does not exist!", "Key");
        }

        private string GetFilename(uint FileID)
        {
            if (FileID == 0 || FileID >= Files.Count)
                return null;

            return Files[(int)FileID - 1];
        }
    }
}
