using System;
using System.Collections.Generic;
using System.Collections;

namespace Clawsemble
{
    public class Compiler
    {
        // User provided
        public List<Token> Tokens { get; set; }
        public List<string> Files { get; set; }

        // Automatically filled, but can be user-adjusted
        public List<InstructionSignature> Instructions { get; private set; }

        // Our file pointer is shared accross the stages
        private int Pointer = 0;

        // Stage 0 autofills the following variables
        public BinaryType BinaryType { get; private set; }
        private int EndOfHeaderPointer = 0;

        // Stage 1 autofills the following variables
        public List<Symbol> Symbols { get; private set; }
        public List<byte[]> Constants { get; private set; }
        public Dictionary<byte, ModuleSlot> Slots { get; private set; }
        public MetaHeader Header { get; private set; }

        // Compile autofills the following variables
        public List<byte> Binary { get; private set; }

        // Misc. constant values
        private const int MaxSlotNameLength = 6;
        private const int MaxSlots = 16;
        private const byte MaxNativeInstrs = 0x7f;

        public Compiler(List<Token>Tokens, List<string>Files, List<InstructionSignature> Instructions)
        {
            this.Tokens = Tokens;
            this.Files = Files;
            this.Instructions = Instructions;
            this.Binary = new List<byte>();
            this.Symbols = new List<Symbol>();
            this.Constants = new List<byte[]>();
            this.Slots = new Dictionary<byte, ModuleSlot>();
            this.Header = new MetaHeader();
            this.BinaryType = 0;
            this.Pointer = 0;
        }

        public Compiler(List<Token>Tokens, List<string>Files)
        {
            this.Tokens = Tokens;
            this.Files = Files;
            this.Binary = new List<byte>();
            this.Constants = new List<byte[]>();
            this.Symbols = new List<Symbol>();
            this.Slots = new Dictionary<byte, ModuleSlot>();
            this.Header = new MetaHeader();
            this.BinaryType = 0;
            this.Pointer = 0;
            Instructions = new List<InstructionSignature>();
            Instructions.AddRange(DefaultInstructions.CompileList());
        }

        public void Cleanup()
        {
            Binary.Clear();
            Constants.Clear();
            Instructions.Clear();
            Instructions.AddRange(DefaultInstructions.CompileList());
            // TODO ADD REMAINING CLEARS

            Symbols.Clear();
            Slots.Clear();
            BinaryType = 0;
            Pointer = 0;
        }

        private void ClearArray(Array Array)
        {
            for (int i = 0; i < Array.Length; i++) {
                Array.SetValue(null, i);
            }
        }

        public void Stage0()
        {
            Cleanup();

            // Find the required header and extract the bitness and executable target
            for (; Pointer < Tokens.Count; Pointer++) {
                if (Tokens[Pointer].Type == TokenType.CompilerDirective) {
                    if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                        throw new CodeError(CodeErrorType.UnknownDirective, "Empty compiler directive!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    string directive = Tokens[Pointer].Content.Trim().ToLower();

                    if (directive != "cwx" && directive != "cwl")
                        throw new CodeError(CodeErrorType.ExpectedHeader, "Expected executable or library header!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));

                    if (directive == "cwx") { // executable
                        BinaryType = BinaryType.Executable;
                    } else if (directive == "cwl") { // library
                        BinaryType = BinaryType.Library;
                    }

                    if (!IsBeforeEOF(Pointer, Tokens.Count, 2))
                        throw new CodeError(CodeErrorType.UnexpectedEOF,
                            Tokens[Tokens.Count - 1],
                            GetFilename(Tokens[Tokens.Count - 1].File));
                    if (Tokens[++Pointer].Type != TokenType.Seperator)
                        throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    if (Tokens[++Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    byte bits;
                    if (!byte.TryParse(Tokens[Pointer].Content, out bits))
                        throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid bits argument!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    if (bits == 16) {
                        BinaryType |= BinaryType.Bits16;
                    } else if (bits == 32) {
                        BinaryType |= BinaryType.Bits32;
                    } else if (bits == 64) {
                        BinaryType |= BinaryType.Bits64;
                    } else
                        throw new CodeError(CodeErrorType.ConstantRange, "Only 16, 32 and 64 bits are supported!",
                            Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    EndOfHeaderPointer = ++Pointer;
                    break;
                } else if (Tokens[Pointer].Type != TokenType.Break)
                    throw new CodeError(CodeErrorType.ExpectedHeader, "Expected initial header!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
            }
        }

        public void Stage1()
        {
            Symbol sym = new Symbol();

            // Find and add all the .lbl, .sym, .dat, .val, .exi and .mod to the database
            for (; Pointer < Tokens.Count; Pointer++) {
                if (Tokens[Pointer].Type == TokenType.CompilerDirective) {
                    if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                        throw new CodeError(CodeErrorType.UnknownDirective, "Empty compiler directive!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    string directive = Tokens[Pointer].Content.Trim().ToLower();

                    if (directive == "sym" || directive == "symbol") {
                        if (!IsBeforeEOF(Pointer, Tokens.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1].File));
                        if (Tokens[++Pointer].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, Tokens[Pointer], GetFilename(Tokens[Pointer].File));

                        string symname;
                        byte symid = 0;
                        bool fixid = false;

                        if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Symbol name can't be empty!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        symname = Tokens[Pointer].Content.Trim();

                        // does the symbol already exist?
                        if (SymbolExists(symname, sym))
                            throw new CodeError(CodeErrorType.WordCollision, "Symbol already defined earlier!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));

                        if (IsBeforeEOF(Pointer, Tokens.Count, 2)) {
                            // now we want to check if we got an optional fixed function id
                            if (Tokens[Pointer + 1].Type == TokenType.Seperator) {
                                if (Tokens[++Pointer].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                                // we got a fixed id
                                if (!byte.TryParse(Tokens[Pointer].Content, out symid))
                                    throw new CodeError(CodeErrorType.ArgumentInvalid, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                                fixid = true;
                            } else if (Tokens[Pointer + 1].Type != TokenType.Break) {
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer + 1], GetFilename(Tokens[Pointer + 1].File));
                            } else
                                continue;
                        }

                        // If there already is a symbol, save it
                        if (!string.IsNullOrWhiteSpace(sym.Name))
                            Symbols.Add(sym);

                        // catch fixid collisions
                        if (fixid) {
                            if (symid > 254)
                                throw new CodeError(CodeErrorType.ConstantRange, "Symbols can only use slots 0 to 254!",
                                    Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                            if (SymbolIndexExists(symid, sym))
                                throw new CodeError(CodeErrorType.ArgumentInvalid, "Fixed slot already in use by another fixed symbol!",
                                    Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                            sym = new Symbol(symname, symid); // add the symbol
                        } else
                            sym = new Symbol(symname); // add the symbol

                        // make sure the line is terminated here
                        if (IsBeforeEOF(Pointer, Tokens.Count)) {
                            if (Tokens[++Pointer].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        }
                    } else if (directive == "dat" || directive == "data" ||
                               directive == "str" || directive == "string" ||
                               directive == "val" || directive == "values") {
                        // e.g.:  .dat test 123,123,123,1232,23
                        if (!IsBeforeEOF(Pointer, Tokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1].File));
                        if (Tokens[++Pointer].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        
                    } else if (directive == "mod" || directive == "module" || directive == "omod" || directive == "optmodule") {
                        if (!IsBeforeEOF(Pointer, Tokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1].File));
                        if (Tokens[++Pointer].Type != TokenType.String)
                            throw new CodeError(CodeErrorType.ExpectedString, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Module name can't be empty!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        string module = Tokens[Pointer].Content.Trim().ToUpper();
                        if (module.Length > MaxSlotNameLength)
                            throw new CodeError(CodeErrorType.ConstantRange,
                                string.Format("The module indentifier can be max. {0} characters long!", MaxSlotNameLength),
                                Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        if (Tokens[++Pointer].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        if (Tokens[++Pointer].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        byte slot;
                        if (!byte.TryParse(Tokens[Pointer].Content, out slot))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid slot constant!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        if (slot >= MaxSlots)
                            throw new CodeError(CodeErrorType.ConstantRange,
                                string.Format("There are only {0} slots!", MaxSlots),
                                Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        if (Slots.ContainsKey(slot))
                            throw new CodeError(CodeErrorType.OperationInvalid,
                                string.Format("Slot {0} already occupied with \"{1}\"!", slot, Slots[slot]),
                                Tokens[Pointer], GetFilename(Tokens[Pointer].File));

                        // add the slot and check whether the module is optional or not
                        Slots.Add(slot, new ModuleSlot(module, (directive == "omd" || directive == "optmodule")));
                        // check for break
                        if (IsBeforeEOF(Pointer, Tokens.Count)) {
                            if (Tokens[++Pointer].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        }
                    } else if (directive == "exi" || directive == "extinstr") {
                        if (!IsBeforeEOF(Pointer, Tokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, Tokens[Tokens.Count - 1], GetFilename(Tokens[Tokens.Count - 1].File));
                        if (Tokens[++Pointer].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Instruction name can't be empty!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        string mnemonic = Tokens[Pointer].Content.Trim().ToLower();
                        if (FindSignature(mnemonic)) // is the instruction already defined
                                throw new CodeError(CodeErrorType.OperationInvalid,
                                string.Format("Instruction {0} already defined!", mnemonic),
                                Tokens[Pointer],
                                GetFilename(Tokens[Pointer].File));
                        // check for seperator (not that it is really ever needed but it looks nice)
                        if (Tokens[++Pointer].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        // check for the id
                        if (Tokens[++Pointer].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        byte code;
                        if (!byte.TryParse(Tokens[Pointer].Content, out code))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid custom instruction code!",
                                Tokens[Pointer],
                                GetFilename(Tokens[Pointer].File));
                        if (code <= MaxNativeInstrs)
                            throw new CodeError(CodeErrorType.ArgumentRange,
                                string.Format("Custom instruction code outside of extended instruction range ({0}-255)!", MaxNativeInstrs + 1),
                                Tokens[Pointer],
                                GetFilename(Tokens[Pointer].File));
                        var args = new List<InstructionArgumentType>();
                        // Now go for args until eof or break
                        while (IsBeforeEOF(Pointer, Tokens.Count, 2)) { // 2 because seperator, then another signature
                            if (Tokens[Pointer + 1].Type != TokenType.Seperator)
                                break; // only continue while there are seperators
                            if (Tokens[Pointer + 2].Type != TokenType.String)
                                break;
                            Pointer += 2; // now we know the args match, so advance the pointer
                            if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                                throw new CodeError(CodeErrorType.WordInvalid, "Custom instruction signature argument type can't be empty!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                            InstructionArgumentType type;
                            if (!Enum.TryParse(Tokens[Pointer].Content.Trim(), true, out type))
                                throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid custom instruction signature argument!",
                                    Tokens[Pointer],
                                    GetFilename(Tokens[Pointer].File));
                            args.Add(type);
                        }

                        Instructions.Add(new InstructionSignature(mnemonic, code, true, args.ToArray()));

                        // check for break
                        if (IsBeforeEOF(Pointer, Tokens.Count)) {
                            if (Tokens[++Pointer].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        }
                    } else if (directive == "title" || directive == "author" ||
                               directive == "copyr" || directive == "copyright" ||
                               directive == "descr" || directive == "description") {
                        if (!IsBeforeEOF(Pointer, Tokens.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1].File));
                        if (Tokens[++Pointer].Type != TokenType.String)
                            throw new CodeError(CodeErrorType.ExpectedString, Tokens[Pointer], GetFilename(Tokens[Pointer].File));

                        string content = (string.IsNullOrWhiteSpace(Tokens[Pointer].Content) ? "" : Tokens[Pointer].Content);

                        if (directive == "title") {
                            Header.Title = content.Trim();
                        } else if (directive == "author") {
                            Header.Author = content.Trim();
                        } else if (directive == "copyr" || directive == "copyright") {
                            Header.Copyright = content;
                        } else if (directive == "descr" || directive == "description") {
                            Header.Description = content;
                        }
                    } else if (directive == "ver" || directive == "version") {
                        if (!IsBeforeEOF(Pointer, Tokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF, Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1].File));
                        if (Tokens[++Pointer].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        byte major;
                        if (!byte.TryParse(Tokens[Pointer].Content, out major))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid major version number!",
                                Tokens[Pointer],
                                GetFilename(Tokens[Pointer].File));

                        if (Tokens[++Pointer].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        
                        byte minor;
                        if (Tokens[++Pointer].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        if (!byte.TryParse(Tokens[Pointer].Content, out minor))
                            throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid minor version number!",
                                Tokens[Pointer],
                                GetFilename(Tokens[Pointer].File));
                        
                        byte revision = 0;
                        // check for optional revision
                        if (!IsBeforeEOF(Pointer, Tokens.Count, 2)) {
                            if (Tokens[Pointer + 1].Type == TokenType.Seperator) {
                                Pointer++;
                                if (Tokens[++Pointer].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                                if (!byte.TryParse(Tokens[Pointer].Content, out revision))
                                    throw new CodeError(CodeErrorType.ArgumentInvalid, "Invalid revision version number!",
                                        Tokens[Pointer],
                                        GetFilename(Tokens[Pointer].File));
                            }
                        }

                        // write the data
                        Header.Version.Major = major;
                        Header.Version.Minor = minor;
                        Header.Version.Revision = revision;

                        // check for break
                        if (IsBeforeEOF(Pointer, Tokens.Count)) {
                            if (Tokens[++Pointer].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                        }
                    }
                } else if (Tokens[Pointer].Type != TokenType.Break) {
                    throw new CodeError(CodeErrorType.UnexpectedToken, Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                }
            }
        }

        /*
         } else if (Tokens[ptr].Type == TokenType.Word) {
                    if (string.IsNullOrWhiteSpace(Tokens[ptr].Content))
                        throw new CodeError(CodeErrorType.WordInvalid, "Empty instruction!", Tokens[ptr], GetFilename(Tokens[ptr]));

                    InstructionSignature instr;
                    if (FindSignature(Tokens[ptr].Content, out instr)) {
                        DoInstruction(instr, ref ptr, Binary);
                    } else
                        throw new CodeError(CodeErrorType.WordUnknown, "Unknown instruction!", Tokens[ptr], GetFilename(Tokens[ptr]));
                } else if (Tokens[ptr].Type == TokenType.Number) {
                    for (; Tokens[ptr].Type != TokenType.Break && Tokens[ptr].Type != TokenType.Seperator; ptr++) {
                        if (Tokens[ptr].Type == TokenType.String) {
                            
                        } else if (Tokens[ptr].Type == TokenType.Number) {

                        }
                    }
                }
           */

        public void Compile()
        {

        }

        private bool SymbolExists(string Name, Symbol CurrentSymbol)
        {
            if (!string.IsNullOrWhiteSpace(CurrentSymbol.Name)) {
                if (Name == CurrentSymbol.Name)
                    return true;
            }

            foreach (Symbol sym in Symbols) {
                if (sym.Name == Name)
                    return true;
            }

            return false;
        }

        private bool SymbolIndexExists(byte Index, Symbol CurrentSymbol)
        {
            if (CurrentSymbol.Index == Index && !string.IsNullOrWhiteSpace(CurrentSymbol.Name))
                return true;

            foreach (Symbol sym in Symbols) {
                if (sym.Index == Index)
                    return true;
            }

            return false;
        }

        private void DoInstruction(InstructionSignature Instruction, ref int Pointer, List<byte> Bytes)
        {
            int argnum = 0;
            Bytes.Add(Instruction.Code);

            foreach (InstructionArgumentType arg in Instruction.Arguments) {
                if (!IsBeforeEOF(Pointer++, Tokens.Count))
                    throw new CodeError(CodeErrorType.UnexpectedEOF, Tokens[Pointer - 1].Line, GetFilename(Tokens[Pointer].File));
                if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                    throw new CodeError(CodeErrorType.ConstantInvalid, "Constant is empty!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                argnum++;

                if (arg == InstructionArgumentType.Number) {
                    if (Tokens[Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));

                    long val;
                    if (!long.TryParse(Tokens[Pointer].Content, out val))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Can't parse numeric constant!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    Bytes.AddRange(BitConverter.GetBytes(val));

                    continue;
                }

                if ((arg & InstructionArgumentType.Label) > 0) {

                }

                if ((arg & InstructionArgumentType.String) > 0) {

                }

                if ((arg & InstructionArgumentType.Byte) > 0) {
                    if (Tokens[Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer].File));

                    byte val;
                    if (!byte.TryParse(Tokens[Pointer].Content, out val))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Can't parse numeric constant!", Tokens[Pointer], GetFilename(Tokens[Pointer].File));
                    Bytes.Add(val);

                    continue;
                }

                throw new CodeError(CodeErrorType.SignatureMissmatch,
                    string.Format("The constant does not match the signature ({1}) of argument #{0} of the instruction \"{2}\"!",
                        argnum, arg.ToString(), Instruction.Mnemonic),
                    Tokens[Pointer], GetFilename(Tokens[Pointer].File));
            }
        }

        private bool IsBeforeEOF(int Pointer, int AvlLength, int ReqLength = 1)
        {
            return (bool)(Pointer + ReqLength < AvlLength);
        }

        private bool FindSignature(string Word, out InstructionSignature Signature)
        {
            Word = Word.Trim().ToLower();

            foreach (var sig in Instructions) {
                if (sig.Mnemonic.ToLower() == Word) {
                    Signature = sig;
                    return true;
                }
            }

            Signature = new InstructionSignature();
            return false;
        }

        private bool FindSignature(string Word, bool QueryExtended = true, bool QueryDefault = true)
        {
            Word = Word.Trim().ToLower();

            foreach (var sig in Instructions) {
                if (sig.Mnemonic.ToLower() == Word)
                    return true;
            }

            return false;
        }

        private bool FindSignature(byte Code)
        {
            foreach (var sig in Instructions) {
                if (sig.Code == Code)
                    return true;
            }

            return false;
        }

        private bool FindSignature(byte Code, out InstructionSignature Signature)
        {
            foreach (var sig in Instructions) {
                if (sig.Code == Code) {
                    Signature = sig;
                    return true;
                }
            }

            Signature = new InstructionSignature();
            return false;
        }

        private int RegisterConstant(byte[] Constant)
        {
            if (Constants.Contains(Constant)) {
                int ptr = 0;
                foreach (byte[] entry in Constants) {
                    if (entry == Constant)
                        return ptr;
                    ptr++;
                }

                return -1;
            } else {
                Constants.Add(Constant);
                return Constants.Count - 1;
            }
        }

        private byte[] NumberToBytes(long val)
        {
            BinaryType bits = BinaryType & BinaryType.Bits;

            switch (bits) {
            case BinaryType.Bits8:
                return BitConverter.GetBytes((sbyte)(val & sbyte.MaxValue));
            case BinaryType.Bits16:
                return BitConverter.GetBytes((short)(val & short.MaxValue));
            case BinaryType.Bits32:
                return BitConverter.GetBytes((int)(val & int.MaxValue));
            case BinaryType.Bits64:
                return BitConverter.GetBytes((long)(val & long.MaxValue));
            }

            return null;
        }

        private int RegisterConstant(long[] Constant)
        {
            var bytes = new List<byte>();

            foreach (long val in Constant) {
                bytes.AddRange(NumberToBytes(val)); 
            }

            return RegisterConstant(bytes.ToArray());
        }

        private int RegisterConstant(string Constant)
        {
            return RegisterConstant(System.Text.ASCIIEncoding.ASCII.GetBytes(Constant));
        }

        private string GetFilename(uint File)
        {
            return Files[(int)(File - 1)];
        }

        private static bool IsValidByte(int val)
        {
            return (bool)(((uint)val) <= 255);
        }
    }
}

