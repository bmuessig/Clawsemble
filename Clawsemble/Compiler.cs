﻿using System;
using System.Collections.Generic;
using System.Collections;

namespace Clawsemble
{
    public class Compiler
    {
        // Files
        public List<string> Filenames;

        // Automatically filled, but can be user-adjusted
        public List<InstructionSignature> Instructions { get; private set; }

        // Shared accross Stage 1 and Stage 2
        public List<NamedReference> References { get; private set; }

        // Stage 1 autofills the following variables
        public BinaryType BinaryType { get; private set; }
        public MetaHeader Header { get; private set; }
        public List<byte[]> Constants { get; private set; }
        public Dictionary<byte, ModuleSlot> Slots { get; private set; }

        // Stage 2 autofills the following variables
        public List<Symbol> Symbols { get; private set; }

        // Misc. vars
        public bool IsPrecompiled { get; private set; }

        public Compiler()
        {
            Instructions = new List<InstructionSignature>();

            References = new List<NamedReference>();

            BinaryType = 0;
            IsPrecompiled = false;
            Header = new MetaHeader();
            Constants = new List<byte[]>();
            Slots = new Dictionary<byte, ModuleSlot>();

            Symbols = new List<Symbol>();

            Filenames = new List<string>();
        }

        public void Precompile(List<Token>InputTokens, List<string>Filenames = null)
        {
            var tokBuf = new List<Token>();
            bool foundHeader = false, foundMain = false;
            int ptr;

            // Save the list of filenames
            this.Filenames = Filenames;

            // Clear out the arrays
            Instructions.Clear();
            Instructions.AddRange(DefaultInstructions.CompileList());

            References.Clear();

            BinaryType = 0;
            IsPrecompiled = false;
            Header = new MetaHeader();
            Constants.Clear();
            Slots.Clear();

            Symbols.Clear();

            // Find the header, then find and add all the meta, .dat, .val, .exi and .mod to the database
            for (ptr = 0; ptr < InputTokens.Count; ptr++) {
                if (InputTokens[ptr].Type == TokenType.CompilerDirective) {
                    if (string.IsNullOrWhiteSpace(InputTokens[ptr].Content))
                        throw new CodeError(CodeErrorType.DirectiveInvalid, "Empty compiler directive!", InputTokens[ptr],
                            GetFilename(InputTokens[ptr].File));
                    string directive = InputTokens[ptr].Content.Trim().ToLower();

                    if (directive == "cwx" || directive == "executable" || directive == "cwl" || directive == "library") {
                        if (!foundHeader)
                            foundHeader = true;
                        else
                            throw new CodeError(CodeErrorType.UnexpectedDirective, "Header already given previously!",
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                        if (directive == "cwx" || directive == "executable") { // executable
                            BinaryType = BinaryType.Executable;
                        } else if (directive == "cwl" || directive == "library") { // library; elseif just kept for future expandability
                            BinaryType = BinaryType.Library;
                        }

                        if (!IsBeforeEOF(ptr, InputTokens.Count, 2))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                InputTokens[InputTokens.Count - 1],
                                GetFilename(InputTokens[InputTokens.Count - 1].File));
                        if (InputTokens[++ptr].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (InputTokens[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        byte bits;
                        if (!byte.TryParse(InputTokens[ptr].Content, out bits))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid bits argument!", InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        if (bits == 16) {
                            BinaryType |= BinaryType.Bits16;
                        } else if (bits == 32) {
                            BinaryType |= BinaryType.Bits32;
                        } else if (bits == 64) {
                            BinaryType |= BinaryType.Bits64;
                        } else
                            throw new CodeError(CodeErrorType.ConstantRange, "Only 16, 32 and 64 bits are supported!",
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (!foundHeader) {
                        throw new CodeError(CodeErrorType.ExpectedHeader, "Expected executable or library header!", InputTokens[ptr],
                            GetFilename(InputTokens[ptr].File));
                    } else if (directive == "data" ||
                               directive == "str" || directive == "string" ||
                               directive == "vals" || directive == "values") {
                        // e.g.:  .dat test,123,123,123,1232,23
                        if (!IsBeforeEOF(ptr, InputTokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                InputTokens[InputTokens.Count - 1],
                                GetFilename(InputTokens[InputTokens.Count - 1].File));
                        if (InputTokens[++ptr].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (string.IsNullOrWhiteSpace(InputTokens[ptr].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Constant name can't be empty!", InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        string name = InputTokens[ptr].Content.Trim();
                        if (!IsNameAvailable(name)) // is the array name already used
                            throw new CodeError(CodeErrorType.WordCollision,
                                string.Format("Name '{0}' already in use!", name),
                                InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));

                        int constid = -1;
                        ReferenceType type = 0;

                        if (directive == "str" || directive == "string") {
                            if (InputTokens[++ptr].Type != TokenType.Seperator)
                                throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                            if (InputTokens[++ptr].Type != TokenType.String)
                                throw new CodeError(CodeErrorType.ExpectedString, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                            if (string.IsNullOrEmpty(InputTokens[ptr].Content))
                                throw new CodeError(CodeErrorType.ArgumentInvalid, "String can't be empty!", InputTokens[ptr],
                                    GetFilename(InputTokens[ptr].File));
                            
                            constid = RegisterConstant(InputTokens[ptr].Content);
                            type = ReferenceType.String;
                        } else if (directive == "data") {
                            var data = new List<byte>();

                            // Now go for args until eof or break
                            while (IsBeforeEOF(ptr, InputTokens.Count, 2)) { // 2 because seperator, then another signature
                                if (InputTokens[++ptr].Type != TokenType.Seperator)
                                    throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));    
                                if (InputTokens[++ptr].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));    
                                byte val;
                                if (!byte.TryParse(InputTokens[ptr].Content, out val))
                                    throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid byte value!",
                                        InputTokens[ptr], GetFilename(InputTokens[ptr].File)); 
                                data.Add(val);
                                if (InputTokens[ptr + 1].Type == TokenType.Break)
                                    break;
                            }

                            constid = RegisterConstant(data.ToArray());
                            type = ReferenceType.Data;
                        } else if (directive == "vals" || directive == "values") {
                            var data = new List<byte>();

                            // Now go for args until eof or break
                            while (IsBeforeEOF(ptr, InputTokens.Count, 2)) { // 2 because seperator, then another signature
                                if (InputTokens[++ptr].Type != TokenType.Seperator)
                                    throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));    
                                if (InputTokens[++ptr].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));    
                                long val;
                                if (!long.TryParse(InputTokens[ptr].Content, out val))
                                    throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid value!",
                                        InputTokens[ptr], GetFilename(InputTokens[ptr].File)); 
                                bool clip;
                                data.AddRange(NumberToBytes(val, out clip));

                                if (clip)
                                    throw new CodeError(CodeErrorType.ConstantRange, "Constant too large for the selected target bitness!",
                                        InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                                if (IsBeforeEOF(ptr, InputTokens.Count)) {
                                    if (InputTokens[ptr + 1].Type == TokenType.Break)
                                        break;
                                }
                            }

                            constid = RegisterConstant(data.ToArray());
                            type = ReferenceType.Values;
                        }

                        // check constid for errors
                        if (constid == -1) {
                            throw new CodeError(CodeErrorType.StackUnderflow, "The array is empty!",
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        } else if (constid == -2) {
                            throw new CodeError(CodeErrorType.StackOverflow, "Too many constants (max. 255 per file)!",
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }

                        // add the reference
                        References.Add(new NamedReference(name, type, (byte)constid));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "mod" || directive == "module" || directive == "omod" || directive == "optmodule") {
                        if (!IsBeforeEOF(ptr, InputTokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                InputTokens[InputTokens.Count - 1],
                                GetFilename(InputTokens[InputTokens.Count - 1].File));
                        if (InputTokens[++ptr].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (string.IsNullOrWhiteSpace(InputTokens[ptr].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Module name can't be empty!", InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        string module = InputTokens[ptr].Content.Trim().ToUpper();
                        if (module.Length > GlobalConstants.MaxSlotNameLength)
                            throw new CodeError(CodeErrorType.ConstantRange,
                                string.Format("The module indentifier can be max. {0} characters long!", GlobalConstants.MaxSlotNameLength),
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (InputTokens[++ptr].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (InputTokens[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        byte slot;
                        if (!byte.TryParse(InputTokens[ptr].Content, out slot))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid slot constant!", InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        if (slot > GlobalConstants.MaxSlot)
                            throw new CodeError(CodeErrorType.ConstantRange,
                                string.Format("There are only {0} slots!", GlobalConstants.MaxSlot + 1),
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (Slots.ContainsKey(slot))
                            throw new CodeError(CodeErrorType.OperationInvalid,
                                string.Format("Slot {0} already occupied with \"{1}\"!", slot, Slots[slot]),
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                        // add the slot and check whether the module is optional or not
                        Slots.Add(slot, new ModuleSlot(module, (directive == "omd" || directive == "optmodule")));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "exi" || directive == "extinstr") {
                        if (!IsBeforeEOF(ptr, InputTokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1],
                                GetFilename(InputTokens[InputTokens.Count - 1].File));
                        if (InputTokens[++ptr].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (string.IsNullOrWhiteSpace(InputTokens[ptr].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Instruction name can't be empty!",
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        string mnemonic = InputTokens[ptr].Content.Trim().ToLower();
                        if (!IsNameAvailable(mnemonic, false)) // is the instruction already defined
                            throw new CodeError(CodeErrorType.WordCollision,
                                string.Format("Name '{0}' already in use!", mnemonic),
                                InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        // check for seperator (not that it is really ever needed but it looks nice)
                        if (InputTokens[++ptr].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        // check for the id
                        if (InputTokens[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        byte code;
                        if (!byte.TryParse(InputTokens[ptr].Content, out code))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid custom instruction code!",
                                InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        if (code <= GlobalConstants.MaxNativeInstrs)
                            throw new CodeError(CodeErrorType.ArgumentRange,
                                string.Format("Custom instruction code outside of extended instruction range ({0}-255)!", GlobalConstants.MaxNativeInstrs + 1),
                                InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        var args = new List<InstructionArgumentType>();
                        // Now go for args until eof or break
                        while (IsBeforeEOF(ptr, InputTokens.Count, 2)) { // 2 because seperator, then another signature
                            if (InputTokens[++ptr].Type != TokenType.Seperator)
                                throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));    
                            if (InputTokens[++ptr].Type != TokenType.String)
                                throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File)); 
                            if (string.IsNullOrWhiteSpace(InputTokens[ptr].Content))
                                throw new CodeError(CodeErrorType.WordInvalid,
                                    "Custom instruction signature argument type can't be empty!", InputTokens[ptr],
                                    GetFilename(InputTokens[ptr].File));
                            InstructionArgumentType type;
                            if (!Enum.TryParse(InputTokens[ptr].Content.Trim(), true, out type))
                                throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid custom instruction signature argument!",
                                    InputTokens[ptr],
                                    GetFilename(InputTokens[ptr].File));
                            args.Add(type);

                            if (IsBeforeEOF(ptr, InputTokens.Count)) {
                                if (InputTokens[ptr + 1].Type == TokenType.Break)
                                    break;
                            }
                        }

                        Instructions.Add(new InstructionSignature(mnemonic, code, true, args.ToArray()));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "title" || directive == "author" || directive == "description") {
                        if (!IsBeforeEOF(ptr, InputTokens.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1],
                                GetFilename(InputTokens[InputTokens.Count - 1].File));
                        if (InputTokens[++ptr].Type != TokenType.String)
                            throw new CodeError(CodeErrorType.ExpectedString, InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                        string content = (string.IsNullOrWhiteSpace(InputTokens[ptr].Content) ? "" : InputTokens[ptr].Content);

                        if (content.Length > byte.MaxValue)
                            throw new CodeError(CodeErrorType.StackOverflow, "Metastring can't be longer than 256 characters!",
                                InputTokens[ptr], GetFilename(InputTokens[ptr].File));

                        if (directive == "title") {
                            Header.Title = content.Trim();
                        } else if (directive == "author") {
                            Header.Author = content.Trim();
                        } else if (directive == "description") {
                            Header.Description = content;
                        }

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "minvstack" || directive == "mincstack" || directive == "minpool") {
                        if (!IsBeforeEOF(ptr, InputTokens.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1],
                                GetFilename(InputTokens[InputTokens.Count - 1].File));
                        if (InputTokens[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        byte value;
                        if (!byte.TryParse(InputTokens[ptr].Content, out value))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid byte number!",
                                InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));

                        // write the data
                        if (directive == "minvstack") {
                            Header.MinVarstackSize = value;
                        } else if (directive == "mincstack") {
                            Header.MinCallstackSize = value;
                        } else if (directive == "minpool") {
                            Header.MinPoolSize = value;
                        }

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "ver" || directive == "version" || directive == "minrt" || directive == "minruntime") {
                        if (!IsBeforeEOF(ptr, InputTokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[InputTokens.Count - 1],
                                GetFilename(InputTokens[InputTokens.Count - 1].File));
                        if (InputTokens[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        byte major;
                        if (!byte.TryParse(InputTokens[ptr].Content, out major))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid major version number!",
                                InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));

                        if (InputTokens[++ptr].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        
                        byte minor;
                        if (InputTokens[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        if (!byte.TryParse(InputTokens[ptr].Content, out minor))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid minor version number!",
                                InputTokens[ptr],
                                GetFilename(InputTokens[ptr].File));
                        
                        byte revision = 0;
                        // check for optional revision
                        if (!IsBeforeEOF(ptr, InputTokens.Count, 2)) {
                            if (InputTokens[ptr + 1].Type == TokenType.Seperator) {
                                ptr++;
                                if (InputTokens[++ptr].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                                if (!byte.TryParse(InputTokens[ptr].Content, out revision))
                                    throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid revision version number!",
                                        InputTokens[ptr],
                                        GetFilename(InputTokens[ptr].File));
                            }
                        }
                            
                        // write the data
                        if (directive == "version" || directive == "ver") {
                            Header.Version.Major = major;
                            Header.Version.Minor = minor;
                            Header.Version.Revision = revision;
                        } else if (directive == "minrt" || directive == "minruntime") {
                            Header.MinRuntimeVersion.Major = major;
                            Header.MinRuntimeVersion.Minor = minor;
                            Header.MinRuntimeVersion.Revision = revision;
                        }

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else {
                        // pass the compiler directive to the next pass
                        tokBuf.Add(new Token() { Type = TokenType.CompilerDirective, Content = directive,
                            Line = InputTokens[ptr].Line, File = InputTokens[ptr].File, Position = InputTokens[ptr].Position
                        });
                    }
                } else if (!foundHeader && InputTokens[ptr].Type != TokenType.Break &&
                           InputTokens[ptr].Type != TokenType.Empty && InputTokens[ptr].Type != TokenType.Comment) {
                    throw new CodeError(CodeErrorType.ExpectedHeader, "Expected initial header!", InputTokens[ptr],
                        GetFilename(InputTokens[ptr].File));
                } else if (InputTokens[ptr].Type == TokenType.Word || InputTokens[ptr].Type == TokenType.Number ||
                           InputTokens[ptr].Type == TokenType.Seperator || InputTokens[ptr].Type == TokenType.Break ||
                           InputTokens[ptr].Type == TokenType.String) {

                    // Prevent double breaks and breaks at the beginning of the buffer
                    if (InputTokens[ptr].Type == TokenType.Break) {
                        if (tokBuf.Count > 0) {
                            if (tokBuf[tokBuf.Count - 1].Type == TokenType.Break)
                                continue;
                        } else
                            continue;
                    }

                    // Schedule the token for the next pass
                    tokBuf.Add(InputTokens[ptr]);
                } else if (InputTokens[ptr].Type == TokenType.Unexpected || InputTokens[ptr].Type == TokenType.Invalid) {
                    throw new CodeError(CodeErrorType.TokenError, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                } else if (InputTokens[ptr].Type != TokenType.Comment && InputTokens[ptr].Type != TokenType.Empty)
                    throw new CodeError(CodeErrorType.UnexpectedToken, "Unsupported token!",
                        InputTokens[ptr], GetFilename(InputTokens[ptr].File));
            }

            NamedReference currRef = new NamedReference(ReferenceType.Symbol);
            Symbol currSym = new Symbol();
            Instruction currInstr = new Instruction();

            for (ptr = 0; ptr < tokBuf.Count; ptr++) {
                if (tokBuf[ptr].Type == TokenType.CompilerDirective) {
                    if (string.IsNullOrWhiteSpace(tokBuf[ptr].Content))
                        throw new CodeError(CodeErrorType.DirectiveInvalid, "Empty compiler directive!", tokBuf[ptr],
                            GetFilename(tokBuf[ptr].File));
                    string directive = tokBuf[ptr].Content.Trim().ToLower();

                    if (directive == "lbl" || directive == "label") {
                        if (!IsBeforeEOF(ptr, tokBuf.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, tokBuf[tokBuf.Count - 1],
                                GetFilename(tokBuf[tokBuf.Count - 1].File));
                        if (tokBuf[++ptr].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        if (string.IsNullOrEmpty(tokBuf[ptr].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Label name can't be empty!", tokBuf[ptr],
                                GetFilename(tokBuf[ptr].File));
                        string name = tokBuf[ptr].Content.Trim();
                        if (!IsNameAvailable(name)) // is the label name already used
                            throw new CodeError(CodeErrorType.WordCollision,
                                string.Format("Name '{0}' already in use!", name),
                                tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        currInstr.Label = name;
                    } else if (directive == "sym" || directive == "symbol") {
                        if (!string.IsNullOrEmpty(currRef.Name)) {
                            // save the last symbol
                            // also append a ret to the end of symbols if there is none already
                            if (currSym.Instructions.Count > 0) {
                                string lastMnemoric = currSym.Instructions[currSym.Instructions.Count - 1].Signature.Mnemonic.ToLower();
                                if (lastMnemoric != "ret" && lastMnemoric != "eop") {
                                    currInstr.Signature = GetSignature("ret");
                                    currSym.Instructions.Add(currInstr);
                                }
                            } else {
                                currInstr.Signature = GetSignature("ret");
                                currSym.Instructions.Add(currInstr);
                            }
                            currInstr = new Instruction();
                            currRef.Value = (byte)Symbols.Count;
                            Symbols.Add(currSym);
                            References.Add(currRef);
                        }
                        // collect the name and optional id of the symbol
                        if (!IsBeforeEOF(ptr, tokBuf.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                tokBuf[tokBuf.Count - 1],
                                GetFilename(tokBuf[tokBuf.Count - 1].File));
                        if (tokBuf[++ptr].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, tokBuf[ptr], GetFilename(tokBuf[ptr].File));

                        string name;
                        byte id = 0;
                        bool fixid = false;

                        if (string.IsNullOrWhiteSpace(tokBuf[ptr].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Symbol name can't be empty!", tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        name = tokBuf[ptr].Content.Trim();

                        // does the symbol already exist?
                        if (!IsNameAvailable(name))
                            throw new CodeError(CodeErrorType.WordCollision,
                                string.Format("Name '{0}' already in use!", name),
                                tokBuf[ptr], GetFilename(tokBuf[ptr].File));

                        // check for fixid
                        if (IsBeforeEOF(ptr, tokBuf.Count, 2)) {
                            // now we want to check if we got an optional fixed function id
                            if (tokBuf[ptr + 1].Type == TokenType.Seperator) {
                                ptr += 2;
                                if (tokBuf[ptr].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedNumber, tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                                // we got a fixed id
                                if (!byte.TryParse(tokBuf[ptr].Content, out id))
                                    throw new CodeError(CodeErrorType.ArgumentInvalid, tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                                fixid = true;
                            } else if (tokBuf[ptr + 1].Type != TokenType.Break) {
                                throw new CodeError(CodeErrorType.ExpectedBreak, tokBuf[ptr + 1], GetFilename(tokBuf[ptr + 1].File));
                            } else
                                continue;
                        }

                        // catch fixid collisions
                        if (fixid) {
                            if (id > 254)
                                throw new CodeError(CodeErrorType.ConstantRange, "Symbols can only use slots 0 to 254!",
                                    tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                            string collName;
                            if (SymbolIdExists(id, out collName)) {
                                if (string.IsNullOrEmpty(collName))
                                    throw new CodeError(CodeErrorType.ArgumentInvalid,
                                        string.Format("Fixed slot {0} already in use by another fixed symbol!", fixid),
                                        tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                                else
                                    throw new CodeError(CodeErrorType.ArgumentInvalid,
                                        string.Format("Fixed slot {0} already in use by fixed symbol ({1})!", fixid, collName),
                                        tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                            }
                            if (id == 0)
                                foundMain = true;
                        }

                        // check for break
                        if (IsBeforeEOF(ptr, tokBuf.Count)) {
                            if (tokBuf[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        }

                        // make space for the new symbol and save the information
                        if (fixid)
                            currSym = new Symbol(id);
                        else
                            currSym = new Symbol();
                        currRef = new NamedReference(name, ReferenceType.Symbol);
                    } else
                        throw new CodeError(CodeErrorType.DirectiveUnknown, "Unknown compiler directive!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                } else if (tokBuf[ptr].Type == TokenType.String) {
                    if (string.IsNullOrEmpty(tokBuf[ptr].Content))
                        throw new CodeError(CodeErrorType.ArgumentInvalid, "String can't be empty!", tokBuf[ptr],
                            GetFilename(tokBuf[ptr].File));

                    int constid = RegisterConstant(tokBuf[ptr].Content);
                    if (constid == -2)
                        throw new CodeError(CodeErrorType.StackOverflow, "Too many constants (max. 255 per file)!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                    
                    if (string.IsNullOrEmpty(currInstr.Signature.Mnemonic)) // no instruction encountered yet
                        throw new CodeError(CodeErrorType.ExpectedInstruction, "String without previous instruction!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));


                } else if (tokBuf[ptr].Type == TokenType.Word) {
                    if (string.IsNullOrEmpty(currRef.Name)) // no symbol encountered yet
                        throw new CodeError(CodeErrorType.ExpectedSymbol, "No symbol previously defined!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                    if (string.IsNullOrWhiteSpace(tokBuf[ptr].Content))
                        throw new CodeError(CodeErrorType.WordInvalid, "Empty word!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                    string name = tokBuf[ptr].Content.Trim();

                    if (string.IsNullOrEmpty(currInstr.Signature.Mnemonic)) { // this is an instruction
                        InstructionSignature sig;

                        if (!FindSignature(name, out sig))
                            throw new CodeError(CodeErrorType.InstructionUnknown,
                                string.Format("The instruction '{0}' is not known!", name),
                                tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        currInstr.Signature = sig;
                        currInstr.Position = tokBuf[ptr].Position;
                        currInstr.Line = tokBuf[ptr].Line;
                        currInstr.File = tokBuf[ptr].File;
                    } else { // this is an argument
                        NamedReference refr;

                        if (string.IsNullOrEmpty(currInstr.Signature.Mnemonic)) // no instruction open
                            throw new CodeError(CodeErrorType.ExpectedInstruction, "Argument without previous instruction!",
                                tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        if (FindReference(name, out refr)) {
                            currInstr.Arguments.Add(new ArgumentToken(refr.Value, refr.Type,
                                tokBuf[ptr].Position, tokBuf[ptr].Line, tokBuf[ptr].File));
                        } else // no reference found, just save it for later
                            currInstr.Arguments.Add(new ArgumentToken(name,
                                tokBuf[ptr].Position, tokBuf[ptr].Line, tokBuf[ptr].File));
                    }
                } else if (tokBuf[ptr].Type == TokenType.Number) {
                    if (string.IsNullOrEmpty(currInstr.Signature.Mnemonic)) // no instruction open
                        throw new CodeError(CodeErrorType.ExpectedInstruction, "Argument without previous instruction!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                    long value;
                    if (!long.TryParse(tokBuf[ptr].Content, out value))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "The constant is invalid!", tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                    if (value > byte.MaxValue)
                        currInstr.Arguments.Add(new ArgumentToken(value, tokBuf[ptr].Position, tokBuf[ptr].Line, tokBuf[ptr].File));
                    else
                        currInstr.Arguments.Add(new ArgumentToken((byte)value, tokBuf[ptr].Position, tokBuf[ptr].Line, tokBuf[ptr].File));
                } else if (tokBuf[ptr].Type == TokenType.Break) {
                    // Finish instruction
                    if (!string.IsNullOrEmpty(currInstr.Signature.Mnemonic)) {
                        currSym.Instructions.Add(currInstr);
                        currInstr = new Instruction();
                    }
                } else
                    throw new CodeError(CodeErrorType.UnexpectedToken, tokBuf[ptr], GetFilename(tokBuf[ptr].File));
            }

            if (!string.IsNullOrEmpty(currRef.Name)) {
                // save the last symbol
                if (currSym.Instructions.Count > 0) {
                    string lastMnemoric = currSym.Instructions[currSym.Instructions.Count - 1].Signature.Mnemonic.ToLower();
                    if (lastMnemoric != "ret" && lastMnemoric != "eop") {
                        currInstr.Signature = GetSignature("ret");
                        currSym.Instructions.Add(currInstr);
                    }
                } else {
                    currInstr.Signature = GetSignature("ret");
                    currSym.Instructions.Add(currInstr);
                }
                currRef.Value = (byte)Symbols.Count;
                Symbols.Add(currSym);
                References.Add(currRef);
            } else
                throw new CodeError(CodeErrorType.ExpectedSymbol, "No symbols defined!");

            // Check if we got a valid main symbol
            if (!foundMain && Symbols.Count > 1) {
                NamedReference refr;
                if (!FindReference("main", out refr, false))
                    throw new CodeError(CodeErrorType.ExpectedSymbol, "Missing an explicit main symbol!");
                if (refr.Type != ReferenceType.Symbol)
                    throw new CodeError(CodeErrorType.ExpectedSymbol, "Missing an explicit main symbol!");
            } else if (!foundMain && Symbols.Count == 1) // explicitly set the only function as main
                Symbols[0].SetIndex(0);

            // Now auto-id the remaining symbols
            foreach (Symbol sym in Symbols) {
                if (!sym.IsIndexSet) {
                    int autoindex = SymbolFreeId();
                    if (autoindex < 0)
                        throw new CodeError(CodeErrorType.StackOverflow, "Too many symbols (max. 255 per file)!");
                    sym.SetIndex((byte)autoindex);
                }
            }

            IsPrecompiled = true;
        }

        public Binary Compile()
        {
            if (!IsPrecompiled)
                throw new InvalidOperationException("You can't compile until you ran the precompiler!");

            // calculate bytes per long
            byte numbytes = 0;
            BinaryType bits = BinaryType & BinaryType.Bits;

            // Determine bits for the long type
            switch (bits) {
            case BinaryType.Bits8:
                numbytes = 1;
                break;
            case BinaryType.Bits16:
                numbytes = 2;
                break;
            case BinaryType.Bits32:
                numbytes = 4;
                break;
            case BinaryType.Bits64:
                numbytes = 8;
                break;
            }

            foreach (Symbol sym in Symbols) {
                for (int insti = 0; insti < sym.Instructions.Count; insti++) {
                    Instruction instr = sym.Instructions[insti];

                    for (byte argi = 0; argi < instr.Arguments.Count; argi++) {
                        ArgumentToken arg = instr.Arguments[argi];

                        if (arg.Type == ArgumentTokenType.ReferenceString) {
                            // first check if we can resolve it from the reference table
                            NamedReference refr;
                            if (FindReference(arg.String, out refr)) {
                                arg.Set(refr.Value, refr.Type);
                                continue;
                            }

                            // now check if it's a label
                            int lblid = FindLabel(arg.String, sym);
                            if (lblid < 0) // check if the label was found
                                throw new CodeError(CodeErrorType.ArgumentInvalid,
                                    string.Format("Can't resolve the reference to '{0}'!", arg.String),
                                    arg.Position, arg.Line, GetFilename(arg.File));

                            // Little drawing of how the jump algorithm is going to work:
                            /* *
                             * ...
                             * dp
                             *  <-------------------------------------------------------------------------+ - size(ld 1)
                             * ld 1 (lbl1)                                                                | - size(add)
                             * add                                                                        | - size(jp lbl1)
                             * jp lbl1   /// we are at the end of the instruction and need to go to the begin of ld 1
                             * --------------------------------------------^ 
                             * subc 5
                             * ...
                             * */

                            // FIXME; does not work

                            // offset to the token we want to jump to the beginning of
                            int tokoffset = lblid - insti;
                            int binoffset = 0;

                            if (tokoffset >= 0) {
                                // we count to the current token by subtracting from it
                                for (; tokoffset >= 0; tokoffset--)
                                    binoffset += sym.Instructions[insti + tokoffset].GetSize(numbytes);
                            } else { // tokoffset < 0
                                // we count to the current token by adding to it
                                for (; tokoffset <= 0; tokoffset++)
                                    binoffset -= sym.Instructions[insti + tokoffset].GetSize(numbytes);
                            }

                            // now lets save the binary offset as our new argument
                            arg.Set(tokoffset, ReferenceType.Label);
                        }
                    }
                }
            }

            var binary = new Binary();
            var symbytes = new List<byte>();

            // compile and check the instructions
            foreach (Symbol sym in Symbols) {
                foreach (Instruction instr in sym.Instructions) {
                    // check argument count, otherwise error out
                    if (instr.Arguments.Count < instr.Signature.Arguments.Length) {
                        throw new CodeError(CodeErrorType.ArgumentCount,
                            string.Format("Too few arguments ({0}) for instruction {1} which requires {2} argument(s)!",
                                instr.Arguments.Count, instr.Signature.Mnemonic, instr.Signature.Arguments.Length),
                            instr.Position, instr.Line, GetFilename(instr.File));
                    } else if (instr.Arguments.Count > instr.Signature.Arguments.Length) {
                        throw new CodeError(CodeErrorType.ArgumentCount,
                            string.Format("Too many arguments ({0}) for instruction {1} which requires {2} argument(s)!",
                                instr.Arguments.Count, instr.Signature.Mnemonic, instr.Signature.Arguments.Length),
                            instr.Position, instr.Line, GetFilename(instr.File));
                    }

                    // add instruction code to bytecode
                    symbytes.Add(instr.Signature.Code);

                    for (int argi = 0; argi < instr.Arguments.Count; argi++) {
                        InstructionArgumentType sigarg = instr.Signature.Arguments[argi], tokarg;

                        if (!ArgumentTokenToSignature(instr.Arguments[argi], out tokarg))
                            throw new CodeError(CodeErrorType.ArgumentInvalid,
                                string.Format("Unknown type ({0}, {1}) of argument #{2} in instruction {3}!", 
                                    instr.Arguments[argi].Type.ToString(), instr.Arguments[argi].Target.ToString(),
                                    argi, instr.Signature.Mnemonic),
                                instr.Arguments[argi].Position, instr.Arguments[argi].Line, GetFilename(instr.Arguments[argi].File));

                        // Check if we can do type conversion
                        if (tokarg == InstructionArgumentType.Byte && sigarg == InstructionArgumentType.Number) {
                            tokarg = InstructionArgumentType.Number;
                            instr.Arguments[argi].Set((long)instr.Arguments[argi].Byte);
                        }

                        // Fix backwards jumping labels and their type
                        if (tokarg == InstructionArgumentType.ShortLabelBw || tokarg == InstructionArgumentType.ShortLabelFw)
                            instr.Arguments[argi].Set((byte)Math.Abs(instr.Arguments[argi].Number), ReferenceType.Label);

                        // Check if any of the valid argument types match
                        if ((sigarg & tokarg) == 0 && sigarg != tokarg)
                            throw new CodeError(CodeErrorType.ArgumentInvalid,
                                string.Format("Type of argument #{0} ({1}), in instruction {2}, invalid. Expected argument of type {3}!",
                                    argi, tokarg.ToString(), instr.Signature.Mnemonic, sigarg.ToString()),
                                instr.Arguments[argi].Position, instr.Arguments[argi].Line, GetFilename(instr.Arguments[argi].File));

                        // write arguments to bytecode
                        if ((sigarg & InstructionArgumentType.Byte) > 0) { // write byte
                            symbytes.Add(instr.Arguments[argi].Byte);
                        } else { // write number
                            bool clip;
                            symbytes.AddRange(NumberToBytes(instr.Arguments[argi].Number, out clip));

                            if (clip)
                                throw new CodeError(CodeErrorType.ConstantRange, "Constant too large for the selected target bitness!",
                                    instr.Arguments[argi].Position, instr.Arguments[argi].Line, GetFilename(instr.Arguments[argi].File));
                        }
                    }
                }

                // Add symbol to binary
                binary.Symbols.Add(sym.Index, symbytes.ToArray());
                // Create new symbol
                symbytes = new List<byte>();
            }

            // Assign the final properties
            binary.Type = BinaryType;
            binary.Meta = Header;
            binary.Constants = Constants;
            binary.Slots = Slots;

            return binary;
        }

        private bool ArgumentTokenToSignature(ArgumentToken Token, out InstructionArgumentType Output)
        {
            switch (Token.Type) {
            case ArgumentTokenType.ByteValue:
                Output = InstructionArgumentType.Byte;
                return true;
            case ArgumentTokenType.NumberValue:
                Output = InstructionArgumentType.Number;
                return true;
            case ArgumentTokenType.ReferenceNumber:
                switch (Token.Target) {
                case ReferenceType.Label:
                    Output = InstructionArgumentType.Label;
                    return true;
                }
                break;
            case ArgumentTokenType.ReferenceByte:
                switch (Token.Target) {
                case ReferenceType.Data:
                    Output = InstructionArgumentType.Data;
                    return true;
                case ReferenceType.Label:
                    if (Token.Number > 0)
                        Output = InstructionArgumentType.ShortLabelFw;
                    else
                        Output = InstructionArgumentType.ShortLabelBw;
                    return true;
                case ReferenceType.String:
                    Output = InstructionArgumentType.String;
                    return true;
                case ReferenceType.Symbol:
                    Output = InstructionArgumentType.Symbol;
                    return true;
                case ReferenceType.Values:
                    Output = InstructionArgumentType.Values;
                    return true;
                }
                break;
            }

            Output = 0;
            return false;
        }

        private int FindLabel(string Name, Symbol Symbol)
        {
            for (int i = 0; i < Symbol.Instructions.Count; i++) {
                if (Symbol.Instructions[i].Label == Name)
                    return i;
            }

            return -1;
        }

        private bool IsBeforeEOF(int Pointer, int AvlLength, int ReqLength = 1)
        {
            return (bool)(Pointer + ReqLength < AvlLength);
        }

        private InstructionSignature GetSignature(string Name)
        {
            InstructionSignature sig;
            if (FindSignature(Name, out sig))
                return sig;
            else if (Name != "nop")
                return GetSignature("nop");
            else
                return new InstructionSignature();
        }

        private bool SymbolIdExists(byte Id, out string Name)
        {
            foreach (NamedReference refr in References) {
                if (refr.Type == ReferenceType.Symbol && refr.Value == Id) {
                    Name = refr.Name;
                    return true;
                }
            }

            Name = null;
            return false;
        }

        private bool SymbolIdExists(byte Id)
        {
            foreach (NamedReference refr in References) {
                if (refr.Type == ReferenceType.Symbol && refr.Value == Id)
                    return true;
            }

            return false;
        }

        private int SymbolFreeId()
        {
            byte id = 0;
            for (; id <= 254; id++) {
                if (!SymbolIdExists(id))
                    return id;
            }

            return -1;
        }

        private bool IsNameAvailable(string Name, bool CaseSensitive = true)
        {
            if (FindReference(Name, CaseSensitive))
                return false;
            if (FindSignature(Name))
                return false;

            return true;
        }

        private bool FindReference(string Name, out NamedReference Reference, bool CaseSensitive = true)
        {
            if (!CaseSensitive)
                Name = Name.Trim().ToLower();

            foreach (var reference in References) {
                if (CaseSensitive) {
                    if (reference.Name == Name) {
                        Reference = reference;
                        return true;
                    }
                } else {
                    if (reference.Name.ToLower() == Name) {
                        Reference = reference;
                        return true;
                    }
                }
            }

            Reference = null;
            return false;
        }

        private bool FindReference(string Name, bool CaseSensitive = true)
        {
            if (!CaseSensitive)
                Name = Name.Trim().ToLower();

            foreach (var reference in References) {
                if (CaseSensitive) {
                    if (reference.Name == Name)
                        return true;
                } else {
                    if (reference.Name.ToLower() == Name)
                        return true;
                }
            }

            return false;
        }

        private bool FindSignature(string Name, out InstructionSignature Signature)
        {
            Name = Name.Trim().ToLower();

            foreach (var sig in Instructions) {
                if (sig.Mnemonic.ToLower() == Name) {
                    Signature = sig;
                    return true;
                }
            }

            Signature = new InstructionSignature();
            return false;
        }

        private bool FindSignature(string Name)
        {
            Name = Name.Trim().ToLower();

            foreach (var sig in Instructions) {
                if (sig.Mnemonic.ToLower() == Name)
                    return true;
            }

            return false;
        }

        private int RegisterConstant(byte[] Constant)
        {
            /* *
             * Returns:
             * >= 0: All ok
             * -1:   Size error
             * -2:   No more constants left
             * */
             
            if (Constant.Length < 1)
                return -1;
            if (Constants.Contains(Constant)) {
                int ptr = 0;
                foreach (byte[] entry in Constants) {
                    if (entry == Constant)
                        return ptr;
                    ptr++;
                }
                return -1; // just to make the compiler happy
            } else {
                if (Constants.Count >= 255)
                    return -2;
                Constants.Add(Constant);
                return Constants.Count - 1;
            }
        }

        private byte[] NumberToBytes(long val, out bool Clip)
        {
            BinaryType bits = BinaryType & BinaryType.Bits;

            switch (bits) {
            case BinaryType.Bits8:
                Clip = (val > sbyte.MaxValue);
                return BitConverter.GetBytes((sbyte)(val & sbyte.MaxValue));
            case BinaryType.Bits16:
                Clip = (val > short.MaxValue);
                return BitConverter.GetBytes((short)(val & short.MaxValue));
            case BinaryType.Bits32:
                Clip = (val > int.MaxValue);
                return BitConverter.GetBytes((int)(val & int.MaxValue));
            case BinaryType.Bits64:
                Clip = (val > long.MaxValue);
                return BitConverter.GetBytes((long)(val & long.MaxValue));
            }

            Clip = false;
            return null;
        }

        private int RegisterConstant(string Constant)
        {
            return RegisterConstant(System.Text.ASCIIEncoding.ASCII.GetBytes(Constant));
        }

        private string GetFilename(uint FileID)
        {
            if (Filenames == null)
                return null;
            if (Filenames.Count < FileID)
                return null;
        
            return Filenames[(int)(FileID - 1)];
        }

        private static bool IsValidByte(int val)
        {
            return (bool)(((uint)val) <= 255);
        }
    }
}

