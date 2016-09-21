using System;
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
                    } else if (directive == "data" || directive == "str" || directive == "vals") {
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

                        byte constid = 0;
                        ReferenceType type = 0;

                        if (directive == "str") {
                            if (InputTokens[++ptr].Type != TokenType.Seperator)
                                throw new CodeError(CodeErrorType.ExpectedSeperator, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                            if (InputTokens[++ptr].Type != TokenType.String)
                                throw new CodeError(CodeErrorType.ExpectedString, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                            if (string.IsNullOrEmpty(InputTokens[ptr].Content))
                                throw new CodeError(CodeErrorType.ArgumentInvalid, "String can't be empty!", InputTokens[ptr],
                                    GetFilename(InputTokens[ptr].File));

                            if (!RegisterConstant(InputTokens[ptr].Content, out constid))
                                throw new CodeError(CodeErrorType.StackOverflow, "Too many constants (max. 255 per file)!",
                                    InputTokens[ptr], GetFilename(InputTokens[ptr].File));
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

                            if (!RegisterConstant(data.ToArray(), out constid))
                                throw new CodeError(CodeErrorType.StackOverflow, "Too many constants (max. 255 per file)!",
                                    InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                            type = ReferenceType.Data;
                        } else if (directive == "vals") {
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

                            if (!RegisterConstant(data.ToArray(), out constid))
                                throw new CodeError(CodeErrorType.StackOverflow, "Too many constants (max. 255 per file)!",
                                    InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                            type = ReferenceType.Values;
                        }

                        // add the reference
                        References.Add(new NamedReference(name, type, constid));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "mod" || directive == "optmod") {
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
                        Slots.Add(slot, new ModuleSlot(module, (directive == "optmod")));

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "extinstr") {
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
                        var args = new List<InstructionArgument>();
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
                            InstructionArgument arg;
                            if (!InstructionArgument.TryParse(InputTokens[ptr].Content.Trim(), out arg))
                                throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid custom instruction signature argument!",
                                    InputTokens[ptr],
                                    GetFilename(InputTokens[ptr].File));
                            args.Add(arg);

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
                    } else if (directive == "extsym" || directive == "extsymbol") {
                        // collect the name and id of the symbol
                        if (!IsBeforeEOF(ptr, tokBuf.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                tokBuf[tokBuf.Count - 1],
                                GetFilename(tokBuf[tokBuf.Count - 1].File));
                        if (tokBuf[++ptr].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, tokBuf[ptr], GetFilename(tokBuf[ptr].File));

                        string name;
                        byte id = 0;

                        if (string.IsNullOrWhiteSpace(tokBuf[ptr].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Symbol name can't be empty!", tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        name = tokBuf[ptr].Content.Trim();

                        // does the extern symbol name already exist?
                        if (!IsNameAvailable(name))
                            throw new CodeError(CodeErrorType.WordCollision,
                                string.Format("Name '{0}' already in use!", name),
                                tokBuf[ptr], GetFilename(tokBuf[ptr].File));

                        // now we want to get the function id
                        if (tokBuf[++ptr].Type != TokenType.Seperator)
                            throw new CodeError();
                        if (tokBuf[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                        if (!byte.TryParse(tokBuf[ptr].Content, out id))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, tokBuf[ptr], GetFilename(tokBuf[ptr].File));

                        // check range
                        if (id > 254)
                            throw new CodeError(CodeErrorType.ConstantRange, "Symbols can only use slots 0 to 254!",
                                tokBuf[ptr], GetFilename(tokBuf[ptr].File));

                        // Save the extern symbol
                        References.Add(new NamedReference(name, ReferenceType.ExternSymbol, id));

                        // check for break
                        if (IsBeforeEOF(ptr, tokBuf.Count)) {
                            if (tokBuf[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, tokBuf[ptr], GetFilename(tokBuf[ptr].File));
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
                    } else if (directive == "minvstack" || directive == "mincstack" || directive == "minpoolsz") {
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
                        } else if (directive == "minpoolsz") {
                            Header.MinPoolSize = value;
                        }

                        // check for break
                        if (IsBeforeEOF(ptr, InputTokens.Count)) {
                            if (InputTokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, InputTokens[ptr], GetFilename(InputTokens[ptr].File));
                        }
                    } else if (directive == "version" || directive == "minrt") {
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
                        if (directive == "version") {
                            Header.Version.Major = major;
                            Header.Version.Minor = minor;
                            Header.Version.Revision = revision;
                        } else if (directive == "minrt") {
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

            NamedReference currRef = new NamedReference(ReferenceType.InternSymbol);
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
                            if (IntSymbolIdExists(id, out collName)) {
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
                        currRef = new NamedReference(name, ReferenceType.InternSymbol);
                    } else
                        throw new CodeError(CodeErrorType.DirectiveUnknown, "Unknown compiler directive!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                } else if (tokBuf[ptr].Type == TokenType.String) {
                    byte constid;
                    if (!RegisterConstant(tokBuf[ptr].Content, out constid))
                        throw new CodeError(CodeErrorType.StackOverflow, "Too many constants (max. 255 per file)!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));
                    if (string.IsNullOrEmpty(currInstr.Signature.Mnemonic)) // no instruction encountered yet
                        throw new CodeError(CodeErrorType.ExpectedInstruction, "String without previous instruction!",
                            tokBuf[ptr], GetFilename(tokBuf[ptr].File));

                    currInstr.Arguments.Add(new ArgumentToken(constid, ReferenceType.String, tokBuf[ptr].Position, tokBuf[ptr].Line, tokBuf[ptr].File));
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
                if (refr.Type != ReferenceType.InternSymbol)
                    throw new CodeError(CodeErrorType.ExpectedSymbol, "Missing an explicit main symbol!");
            } else if (!foundMain && Symbols.Count == 1) // explicitly set the only function as main
                Symbols[0].SetIndex(0);

            // Now auto-id the remaining symbols
            foreach (Symbol sym in Symbols) {
                if (!sym.IsIndexSet) {
                    int autoindex = IntSymbolFreeId();
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
                            int targeti = FindLabel(arg.String, sym);
                            if (targeti < 0) // check if the label was found
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

                            // offset to the token we want to jump to the beginning of
                            // if we jump back, don't subtract, otherwise subtract the current token from it
                            // TODO: confirm that this actually works the way it is supposed to
                            int tokoffset = ((targeti <= insti) ? (targeti - insti) : (targeti - insti - 2));
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
                            arg.Set((long)binoffset, ReferenceType.Label);
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
                        InstructionArgument sigarg = instr.Signature.Arguments[argi];
                        ArgumentToken tokarg = instr.Arguments[argi];

                        // Check if the type matches and convert the argtoken to the correct output format
                        if (!sigarg.NormalizeArg(ref tokarg))
                            throw new CodeError(CodeErrorType.ArgumentInvalid,
                                string.Format("Argument #{0} ({1}), in instruction {2}, invalid. Expected argument of type {3}!",
                                    argi, tokarg.Type.ToString(), instr.Signature.Mnemonic, sigarg.ToString()),
                                tokarg.Position, tokarg.Line, GetFilename(tokarg.File));

                        // write arguments to bytecode
                        InstructionArgumentTarget sigtarget = sigarg.Target;
                        if (sigtarget == InstructionArgumentTarget.Byte) { // write byte
                            symbytes.Add(instr.Arguments[argi].Byte);
                        } else if (sigtarget == InstructionArgumentTarget.UnsignedNumber) { // write unsigmed number
                            bool clip;
                            symbytes.AddRange(UNumberToBytes(instr.Arguments[argi].Number, out clip));

                            if (clip)
                                throw new CodeError(CodeErrorType.ConstantRange, "Constant too large or too small for the selected target bitness!",
                                    instr.Arguments[argi].Position, instr.Arguments[argi].Line, GetFilename(instr.Arguments[argi].File));
                        } else { // write signed number
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

        /*private bool ArgumentTokenToSignature(ArgumentToken Token, out InstructionArgument Output)
        {



            switch (Token.Type) {
            case ArgumentTokenType.ByteValue:
                Output = InstructionArgumentFlags.Byte;
                return true;
            case ArgumentTokenType.NumberValue:
                Output = InstructionArgumentFlags.Number;
                return true;
            case ArgumentTokenType.ReferenceNumber:
                switch (Token.Target) {
                case ReferenceType.Label:
                    Output = InstructionArgumentFlags.Label;
                    return true;
                }
                break;
            case ArgumentTokenType.ReferenceByte:
                switch (Token.Target) {
                case ReferenceType.Data:
                    Output = InstructionArgumentFlags.Data;
                    return true;
                case ReferenceType.Label:
                    if (Token.Number > 0)
                        Output = InstructionArgumentFlags.ShortLabelFw;
                    else
                        Output = InstructionArgumentFlags.ShortLabelBw;
                    return true;
                case ReferenceType.String:
                    Output = InstructionArgumentFlags.String;
                    return true;
                case ReferenceType.Symbol:
                    Output = InstructionArgumentFlags.Symbol;
                    return true;
                case ReferenceType.Values:
                    Output = InstructionArgumentFlags.Values;
                    return true;
                }
                break;
            }

            Output = 0;
            return false;
        }*/

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

        private bool IntSymbolIdExists(byte Id, out string Name)
        {
            foreach (NamedReference refr in References) {
                if (refr.Type == ReferenceType.InternSymbol && refr.Value == Id) {
                    Name = refr.Name;
                    return true;
                }
            }

            Name = null;
            return false;
        }

        private bool IntSymbolIdExists(byte Id)
        {
            foreach (NamedReference refr in References) {
                if (refr.Type == ReferenceType.InternSymbol && refr.Value == Id)
                    return true;
            }

            return false;
        }

        private int IntSymbolFreeId()
        {
            byte id = 0;
            for (; id <= 254; id++) {
                if (!IntSymbolIdExists(id))
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

        private bool RegisterConstant(byte[] Constant, out byte ID)
        {
            if (Constant.Length < 1) {
                ID = byte.MaxValue;
                return true;
            }
            
            byte ptr = 0;
            foreach (byte[] entry in Constants) {
                if (entry == Constant) {
                    ID = ptr;
                    return true;
                }
                ptr++;
            }

            if (Constants.Count >= 255) {
                ID = 0;
                return false;
            }
            Constants.Add(Constant);
            ID = (byte)(Constants.Count - 1);
            return true;
        }

        private bool RegisterConstant(string Constant, out byte ID)
        {
            if (string.IsNullOrEmpty(Constant)) {
                ID = byte.MaxValue;
                return true;
            }
            return RegisterConstant(System.Text.ASCIIEncoding.ASCII.GetBytes(Constant), out ID);
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

        private byte[] UNumberToBytes(long val, out bool Clip)
        {
            BinaryType bits = BinaryType & BinaryType.Bits;

            switch (bits) {
            case BinaryType.Bits8:
                Clip = (val < byte.MinValue || val > byte.MaxValue);
                return BitConverter.GetBytes((byte)val);
            case BinaryType.Bits16:
                Clip = (val < ushort.MinValue || val > ushort.MaxValue);
                return BitConverter.GetBytes((ushort)val);
            case BinaryType.Bits32:
                Clip = (val < uint.MinValue || val > uint.MaxValue);
                return BitConverter.GetBytes((uint)val);
            case BinaryType.Bits64:
                Clip = (val < (long)ulong.MinValue);
                return BitConverter.GetBytes((ulong)val);
            }

            Clip = false;
            return null;
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

