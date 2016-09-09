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
        public List<Instruction> Instructions { get; private set; }

        // Our file pointer is shared accross the stages
        private int Pointer = 0;

        // Stage 0 autofills the following variables
        public CompilerBinaryType BinaryType { get; private set; }
        private int EndOfHeaderPointer = 0;

        // Stage 1 autofills the following variables
        public List<Symbol> Symbols { get; private set; }
        public List<byte[]> Constants { get; private set; }
        public Dictionary<byte, ModuleSlot> Slots { get; private set; }

        // Compile autofills the following variables
        public List<byte> Binary { get; private set; }

        // Misc. constant values
        private const int MaxSlotNameLength = 6;
        private const int MaxSlots = 16;

        public Compiler(List<Token>Tokens, List<string>Files, List<Instruction> Instructions)
        {
            this.Tokens = Tokens;
            this.Files = Files;
            this.Instructions = Instructions;
            this.Binary = new List<byte>();
            this.Symbols = new List<Symbol>();
            this.Constants = new List<byte[]>();
            this.Slots = new Dictionary<byte, ModuleSlot>();
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
            this.BinaryType = 0;
            this.Pointer = 0;
            Instructions = new List<Instruction>();
            Instructions.AddRange(DefaultInstructions.CompileList());
        }

        public void Cleanup()
        {
            Binary.Clear();
            Constants.Clear();
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
                        throw new CodeError(CodeErrorType.UnknownDirective, "Empty compiler directive!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    string directive = Tokens[Pointer].Content.Trim().ToLower();

                    if (directive != "cwx" && directive != "cwl")
                        throw new CodeError(CodeErrorType.ExpectedHeader, "Expected executable or library header!", Tokens[Pointer], GetFilename(Tokens[Pointer]));

                    if (directive == "cwx") { // executable
                        BinaryType = CompilerBinaryType.Executable;
                    } else if (directive == "cwl") { // library
                        BinaryType = CompilerBinaryType.Library;
                    }

                    if (!IsBeforeEOF(Pointer, Tokens.Count, 2))
                        throw new CodeError(CodeErrorType.UnexpectedEOF,
                            Tokens[Tokens.Count - 1],
                            GetFilename(Tokens[Tokens.Count - 1]));
                    if (Tokens[++Pointer].Type != TokenType.Seperator)
                        throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    if (Tokens[++Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    byte bits;
                    if (!byte.TryParse(Tokens[Pointer].Content, out bits))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid bits constant!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    if (bits == 16) {
                        BinaryType |= CompilerBinaryType.Bits16;
                    } else if (bits == 32) {
                        BinaryType |= CompilerBinaryType.Bits32;
                    } else if (bits == 64) {
                        BinaryType |= CompilerBinaryType.Bits64;
                    } else
                        throw new CodeError(CodeErrorType.ArgumentOutOfBounds, "Only 16, 32 and 64 bits are supported!",
                            Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    Pointer++;
                    break;
                } else if (Tokens[Pointer].Type != TokenType.Break)
                    throw new CodeError(CodeErrorType.ExpectedHeader, "Expected initial header!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
            }
        }

        public void Stage1()
        {
            Symbol sym = new Symbol();

            // Find and add all the .lbl, .sym, .db and .mod to the database
            for (; Pointer < Tokens.Count; Pointer++) {
                if (Tokens[Pointer].Type == TokenType.CompilerDirective) {
                    if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                        throw new CodeError(CodeErrorType.UnknownDirective, "Empty compiler directive!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    string directive = Tokens[Pointer].Content.Trim().ToLower();

                    if (directive == "sym" || directive == "symbol") {
                        if (!IsBeforeEOF(Pointer, Tokens.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1]));
                        if (Tokens[++Pointer].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, Tokens[Pointer], GetFilename(Tokens[Pointer]));

                        string symname;
                        byte symid = 0;
                        bool fixid = false;

                        if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                            throw new CodeError(CodeErrorType.WordInvalid, "Symbol name can't be empty!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        symname = Tokens[Pointer].Content.Trim();

                        // does the symbol already exist?
                        if (SymbolExists(symname, sym))
                            throw new CodeError(CodeErrorType.WordCollision, "Symbol already defined earlier!", Tokens[Pointer], GetFilename(Tokens[Pointer]));

                        if (IsBeforeEOF(Pointer, Tokens.Count, 2)) {
                            // now we want to check if we got an optional fixed function id
                            if (Tokens[Pointer + 1].Type == TokenType.Seperator) {
                                if (Tokens[++Pointer].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                                // we got a fixed id
                                if (!byte.TryParse(Tokens[Pointer].Content, out symid))
                                    throw new CodeError(CodeErrorType.ConstantInvalid, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                                fixid = true;
                            } else if (Tokens[Pointer + 1].Type != TokenType.Break) {
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer + 1], GetFilename(Tokens[Pointer + 1]));
                            } else
                                continue;
                        }

                        // If there already is a symbol, save it
                        if (!string.IsNullOrWhiteSpace(sym.Name))
                            Symbols.Add(sym);

                        // catch fixid collisions
                        if (fixid) {
                            if (symid > 254)
                                throw new CodeError(CodeErrorType.ArgumentOutOfBounds, "Symbols can only use slots 0 to 254!",
                                    Tokens[Pointer], GetFilename(Tokens[Pointer]));
                            if (SymbolIndexExists(symid, sym))
                                throw new CodeError(CodeErrorType.ArgumentInvalid, "Fixed slot already in use by another fixed symbol!",
                                    Tokens[Pointer], GetFilename(Tokens[Pointer]));
                            sym = new Symbol(symname, symid); // add the symbol
                        } else
                            sym = new Symbol(symname); // add the symbol

                        // make sure the line is terminated here
                        if (IsBeforeEOF(Pointer, Tokens.Count)) {
                            if (Tokens[++Pointer].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        }
                    } else if (directive == "dat" || directive == "data" ||
                               directive == "str" || directive == "string" ||
                               directive == "val" || directive == "values") {

                    } else if (directive == "mod" || directive == "module" || directive == "omod" || directive == "optmodule") {
                        if (!IsBeforeEOF(Pointer, Tokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1]));
                        if (Tokens[++Pointer].Type != TokenType.String)
                            throw new CodeError(CodeErrorType.ExpectedString, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                            throw new CodeError(CodeErrorType.ConstantInvalid, "Module name can't be empty!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        string module = Tokens[Pointer].Content.Trim().ToUpper();
                        if (module.Length > MaxSlotNameLength)
                            throw new CodeError(CodeErrorType.ArgumentOutOfBounds,
                                string.Format("The module indentifier can be max. {0} characters long!", MaxSlotNameLength),
                                Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        if (Tokens[++Pointer].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        if (Tokens[++Pointer].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        byte slot;
                        if (!byte.TryParse(Tokens[Pointer].Content, out slot))
                            throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid slot constant!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        if (slot >= MaxSlots)
                            throw new CodeError(CodeErrorType.ArgumentOutOfBounds,
                                string.Format("There are only {0} slots!", MaxSlots),
                                Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        if (Slots.ContainsKey(slot))
                            throw new CodeError(CodeErrorType.OperationInvalid,
                                string.Format("Slot {0} already occupied with \"{1}\"!", slot, Slots[slot]),
                                Tokens[Pointer], GetFilename(Tokens[Pointer]));

                        // add the slot and check whether the module is optional or not
                        Slots.Add(slot, new ModuleSlot(module, (directive == "omod" || directive == "optmodule")));
                        if (IsBeforeEOF(Pointer, Tokens.Count)) {
                            if (Tokens[++Pointer].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                        }
                    } else if (directive == "ttl" || directive == "title") {

                    } else if (directive == "aut" || directive == "author") {

                    } else if (directive == "cpy" || directive == "copyright") {

                    } else if (directive == "ver" || directive == "version") {

                    }
                } else if (Tokens[Pointer].Type == TokenType.Word) {
                    if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                        throw new CodeError(CodeErrorType.WordInvalid, "Empty word!", Tokens[Pointer], GetFilename(Tokens[Pointer]));

                    Instruction instr;
                    if (FindSignature(Tokens[Pointer].Content, out instr)) {
                        DoInstruction(instr, ref Pointer, Binary);
                    } else
                        throw new CodeError(CodeErrorType.WordUnknown, Tokens[Pointer], GetFilename(Tokens[Pointer]));
                } else if (Tokens[Pointer].Type == TokenType.Number || Tokens[Pointer].Type == TokenType.String) {
                    
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
                if (CurrentSymbol.Name == CurrentSymbol.Name)
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

        private void DoInstruction(Instruction Instruction, ref int Pointer, List<byte> Bytes)
        {
            int argnum = 0;
            Bytes.Add(Instruction.Code);

            foreach (InstructionArgumentType arg in Instruction.Arguments) {
                if (!IsBeforeEOF(Pointer++, Tokens.Count))
                    throw new CodeError(CodeErrorType.UnexpectedEOF, Tokens[Pointer - 1].Line, GetFilename(Tokens[Pointer]));
                if (string.IsNullOrWhiteSpace(Tokens[Pointer].Content))
                    throw new CodeError(CodeErrorType.ConstantInvalid, "Constant is empty!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                argnum++;

                if (arg == InstructionArgumentType.Number) {
                    if (Tokens[Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer]));

                    long val;
                    if (!long.TryParse(Tokens[Pointer].Content, out val))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Can't parse numeric constant!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    Bytes.AddRange(BitConverter.GetBytes(val));

                    continue;
                }

                if ((arg & InstructionArgumentType.Label) > 0) {

                }

                if ((arg & InstructionArgumentType.String) > 0) {

                }

                if ((arg & InstructionArgumentType.Byte) > 0) {
                    if (Tokens[Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[Pointer], GetFilename(Tokens[Pointer]));

                    byte val;
                    if (!byte.TryParse(Tokens[Pointer].Content, out val))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Can't parse numeric constant!", Tokens[Pointer], GetFilename(Tokens[Pointer]));
                    Bytes.Add(val);

                    continue;
                }

                throw new CodeError(CodeErrorType.SignatureMissmatch,
                    string.Format("The constant does not match the signature ({1}) of argument #{0} of the instruction \"{2}\"!",
                        argnum, arg.ToString(), Instruction.Mnemonic),
                    Tokens[Pointer], GetFilename(Tokens[Pointer]));
            }
        }

        private bool IsBeforeEOF(int Pointer, int AvlLength, int ReqLength = 1)
        {
            return (bool)(Pointer + ReqLength < AvlLength);
        }

        private bool FindSignature(string Word, out Instruction Signature)
        {
            Word = Word.Trim().ToLower();

            foreach (var sig in Instructions) {
                if (sig.Mnemonic.ToLower() == Word) {
                    Signature = sig;
                    return true;
                }
            }

            Signature = new Instruction();
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
            CompilerBinaryType bits = BinaryType & CompilerBinaryType.Bits;

            switch (bits) {
            case CompilerBinaryType.Bits8:
                return BitConverter.GetBytes((sbyte)(val & sbyte.MaxValue));
            case CompilerBinaryType.Bits16:
                return BitConverter.GetBytes((short)(val & short.MaxValue));
            case CompilerBinaryType.Bits32:
                return BitConverter.GetBytes((int)(val & int.MaxValue));
            case CompilerBinaryType.Bits64:
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

        private string GetFilename(Token Token)
        {
            return Files[(int)(Token.File - 1)];
        }

        private static bool IsValidByte(int val)
        {
            return (bool)(((uint)val) <= 255);
        }
    }
}

