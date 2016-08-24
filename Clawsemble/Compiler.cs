using System;
using System.Collections.Generic;
using System.Collections;

namespace Clawsemble
{
    public class Compiler
    {
        public List<Token> Tokens { get; set; }

        public List<string> Files { get; set; }

        public List<InstructionSignature> Instructions { get; private set; }

        public List<byte> Binary { get; private set; }

        public List<byte[]> Constants { get; private set; }

        public Dictionary<string, byte[]> Datablocks { get; private set; }

        public Dictionary<string, byte> Symbols { get; private set; }

        public string[] Slots { get; private set; }

        public CompilerBinaryType BinaryType { get; private set; }

        private const int MaxSlotNameLength = 6;

        public Compiler(List<Token>Tokens, List<string>Files, List<InstructionSignature> Instructions)
        {
            this.Tokens = Tokens;
            this.Files = Files;
            this.Instructions = Instructions;
            this.Binary = new List<byte>();
            this.Constants = new List<byte[]>();
            this.Datablocks = new Dictionary<string, byte[]>();
            this.Symbols = new Dictionary<string, byte>();
            this.Slots = new string[16];
            this.BinaryType = 0;
        }

        public Compiler(List<Token>Tokens, List<string>Files)
        {
            this.Tokens = Tokens;
            this.Files = Files;
            this.Binary = new List<byte>();
            this.Constants = new List<byte[]>();
            this.Datablocks = new Dictionary<string, byte[]>();
            this.Symbols = new Dictionary<string, byte>();
            this.Slots = new string[16];
            this.BinaryType = 0;
            Instructions = new List<InstructionSignature>();
            Instructions.AddRange(DefaultInstructions.CompileList());
        }

        public void Cleanup()
        {
            Binary.Clear();
            Constants.Clear();
            Datablocks.Clear();
            Symbols.Clear();
            BinaryType = 0;
            ClearArray(Slots);
        }

        private void ClearArray(Array Array)
        {
            for (int i = 0; i < Array.Length; i++) {
                Array.SetValue(null, i);
            }
        }

        public void Compile()
        {
            Cleanup();
            int ptr = 0;

            // Find the required header and extract the bitness and executable target
            for (; ptr < Tokens.Count; ptr++) {
                if (Tokens[ptr].Type == TokenType.CompilerDirective) {
                    if (string.IsNullOrWhiteSpace(Tokens[ptr].Content))
                        throw new CodeError(CodeErrorType.UnknownDirective, "Empty compiler directive!", Tokens[ptr], GetFilename(Tokens[ptr]));
                    string directive = Tokens[ptr].Content.Trim().ToLower();

                    if (directive != "cwx" && directive != "cwl")
                        throw new CodeError(CodeErrorType.ExpectedHeader, "Expected executable or library header!", Tokens[ptr], GetFilename(Tokens[ptr]));

                    if (directive == "cwx") { // executable
                        BinaryType = CompilerBinaryType.Executable;
                    } else if (directive == "cwl") { // library
                        BinaryType = CompilerBinaryType.Library;
                    }

                    if (!IsBeforeEOF(ptr, Tokens.Count, 2))
                        throw new CodeError(CodeErrorType.UnexpectedEOF,
                            Tokens[Tokens.Count - 1],
                            GetFilename(Tokens[Tokens.Count - 1]));
                    if (Tokens[++ptr].Type != TokenType.Seperator)
                        throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[ptr], GetFilename(Tokens[ptr]));
                    if (Tokens[++ptr].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[ptr], GetFilename(Tokens[ptr]));
                    byte bits;
                    if (!byte.TryParse(Tokens[ptr].Content, out bits))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid bits constant!", Tokens[ptr], GetFilename(Tokens[ptr]));
                    if (bits == 16) {
                        BinaryType |= CompilerBinaryType.Bits16;
                    } else if (bits == 32) {
                        BinaryType |= CompilerBinaryType.Bits32;
                    } else if (bits == 64) {
                        BinaryType |= CompilerBinaryType.Bits64;
                    } else
                        throw new CodeError(CodeErrorType.ArgumentOutOfBounds, "Only 16, 32 and 64 bits are supported!",
                            Tokens[ptr], GetFilename(Tokens[ptr]));
                    ptr++;
                    break;
                } else if (Tokens[ptr].Type != TokenType.Break)
                    throw new CodeError(CodeErrorType.ExpectedHeader, "Expected initial header!", Tokens[ptr], GetFilename(Tokens[ptr]));
            }

            // Perform the actual compilation
            for (; ptr < Tokens.Count; ptr++) {
                if (Tokens[ptr].Type == TokenType.CompilerDirective) {
                    if (string.IsNullOrWhiteSpace(Tokens[ptr].Content))
                        throw new CodeError(CodeErrorType.UnknownDirective, "Empty compiler directive!", Tokens[ptr], GetFilename(Tokens[ptr]));
                    string directive = Tokens[ptr].Content.Trim().ToLower();

                    if (directive == "sym" || directive == "symbol") {
                        if (!IsBeforeEOF(ptr, Tokens.Count))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1]));
                        if (Tokens[++ptr].Type != TokenType.Word)
                            throw new CodeError(CodeErrorType.ExpectedWord, Tokens[ptr], GetFilename(Tokens[ptr]));
                        
                        string symname;
                        byte symid;

                        if (string.IsNullOrWhiteSpace(Tokens[ptr].Content))
                            throw new CodeError(CodeErrorType.ConstantInvalid, "Symbol name can't be empty!", Tokens[ptr], GetFilename(Tokens[ptr]));
                        symname = Tokens[ptr].Content.Trim();

                        if (IsBeforeEOF(ptr, Tokens.Count, 2)) {
                            // now we want to check if we got an optional fixed function id
                            if (Tokens[ptr + 1].Type == TokenType.Seperator) {
                                if (Tokens[++ptr].Type != TokenType.Number)
                                    throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[ptr], GetFilename(Tokens[ptr]));
                                // we got a fixed id
                                if (!byte.TryParse(Tokens[ptr].Content, out symid))
                                    throw new CodeError(CodeErrorType.ConstantInvalid, Tokens[ptr], GetFilename(Tokens[ptr]));
                            } else if (Tokens[ptr + 1].Type != TokenType.Break) {
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[ptr + 1], GetFilename(Tokens[ptr + 1]));
                            } else
                                continue;
                        }

                        // process the gathered information

                        // make sure the line is terminated here
                        if (IsBeforeEOF(ptr, Tokens.Count)) {
                            if (Tokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[ptr], GetFilename(Tokens[ptr]));
                        }
                    } else if (directive == "db" || directive == "data") {

                    } else if (directive == "mod" || directive == "module") {
                        if (!IsBeforeEOF(ptr, Tokens.Count, 3))
                            throw new CodeError(CodeErrorType.UnexpectedEOF,
                                Tokens[Tokens.Count - 1],
                                GetFilename(Tokens[Tokens.Count - 1]));
                        if (Tokens[++ptr].Type != TokenType.String)
                            throw new CodeError(CodeErrorType.ExpectedString, Tokens[ptr], GetFilename(Tokens[ptr]));
                        if (string.IsNullOrWhiteSpace(Tokens[ptr].Content))
                            throw new CodeError(CodeErrorType.ConstantInvalid, "Module name can't be empty!", Tokens[ptr], GetFilename(Tokens[ptr]));
                        string module = Tokens[ptr].Content.Trim().ToUpper();
                        if (module.Length > MaxSlotNameLength)
                            throw new CodeError(CodeErrorType.ArgumentOutOfBounds,
                                string.Format("The module indentifier can be max. {0} characters long!", MaxSlotNameLength),
                                Tokens[ptr], GetFilename(Tokens[ptr]));
                        if (Tokens[++ptr].Type != TokenType.Seperator)
                            throw new CodeError(CodeErrorType.ExpectedSeperator, Tokens[ptr], GetFilename(Tokens[ptr]));
                        if (Tokens[++ptr].Type != TokenType.Number)
                            throw new CodeError(CodeErrorType.ExpectedNumber, Tokens[ptr], GetFilename(Tokens[ptr]));
                        byte slot;
                        if (!byte.TryParse(Tokens[ptr].Content, out slot))
                            throw new CodeError(CodeErrorType.ConstantInvalid, "Invalid slot constant!", Tokens[ptr], GetFilename(Tokens[ptr]));
                        if (slot >= Slots.Length)
                            throw new CodeError(CodeErrorType.ArgumentOutOfBounds,
                                string.Format("There are only {0} slots!", Slots.Length),
                                Tokens[ptr], GetFilename(Tokens[ptr]));
                        if (!string.IsNullOrEmpty(Slots[slot]))
                            throw new CodeError(CodeErrorType.OperationInvalid,
                                string.Format("Slot {0} already occupied with \"{1}\"!", slot, Slots[slot]),
                                Tokens[ptr], GetFilename(Tokens[ptr]));
                        Slots[slot] = module;
                        if (IsBeforeEOF(ptr, Tokens.Count)) {
                            if (Tokens[++ptr].Type != TokenType.Break)
                                throw new CodeError(CodeErrorType.ExpectedBreak, Tokens[ptr], GetFilename(Tokens[ptr]));
                        }
                    } else if (directive == "ttl" || directive == "title") {

                    } else if (directive == "aut" || directive == "author") {

                    } else if (directive == "cpy" || directive == "copyright") {

                    } else if (directive == "ver" || directive == "version") {

                    }
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
            }
        }

        private void DoInstruction(InstructionSignature Instruction, ref int Pointer, List<byte> Bytes)
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

