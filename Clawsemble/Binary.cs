using System;
using System.Collections.Generic;
using System.IO;

namespace Clawsemble
{
    public class Binary
    {
        public BinaryType Type;
        public MetaHeader Meta;
        public List<byte[]> Constants;
        public Dictionary<byte, ModuleSlot> Slots;
        public Dictionary<byte, byte[]> Symbols;

        public Binary()
        {
            Meta = new MetaHeader();
            Constants = new List<byte[]>();
            Slots = new Dictionary<byte, ModuleSlot>();
            Symbols = new Dictionary<byte, byte[]>();
        }

        public byte[] Bake()
        {
            var memst = new MemoryStream();
            byte[] bytes;

            try {
                this.Bake(memst);
            } catch (Exception ex) {
                throw ex;
            }

            bytes = memst.ToArray();
            memst.Close();

            return bytes;
        }

        public void Bake(Stream Stream)
        {
            // Start writing the magic numbers CAFE + (CWX || CWL)
            Stream.WriteByte(0xca);
            Stream.WriteByte(0xfe);

            // Write the type
            Stream.WriteByte((byte)'C');
            Stream.WriteByte((byte)'W');
            if ((Type & BinaryType.Library) > 0)
                Stream.WriteByte((byte)'L'); // we got a library
            else
                Stream.WriteByte((byte)'X'); // we got an executable

            // Write the bitness
            Stream.WriteByte((byte)(Type & BinaryType.Bits));

            // Let's write the meta information now
            Meta.Bake(Stream);

            // Check number of slots
            if (Slots.Count > GlobalConstants.MaxSlots)
                throw new BitbakeError(BitbakeErrorType.TooManySlots);

            // Write the number of slots used
            Stream.WriteByte((byte)Slots.Count);

            // Now add the module slots themselves
            foreach (KeyValuePair<byte, ModuleSlot> slot in Slots) {
                if (slot.Key > GlobalConstants.MaxSlot)
                    throw new BitbakeError(BitbakeErrorType.SlotOutOfBounds, slot.Key);

                // Write the module id and or the isoptional flag to the fifth bit of the id
                Stream.WriteByte((byte)((slot.Key & GlobalConstants.MaxSlot) | (slot.Value.IsOptional ? (0x1 << 5) : 0)));

                // Write the module indentifier
                WriteString(slot.Value.Name, Stream);
            }

            // Check the number of constants
            if (Constants.Count > GlobalConstants.MaxConstants)
                throw new BitbakeError(BitbakeErrorType.TooManyConstants);
            
            // Write the number of constants used
            Stream.WriteByte((byte)Constants.Count);

            // Now add the constants
            int consti = 0;
            foreach (byte[] constant in Constants) {
                if ((ulong)constant.LongLength > MaxUNumber())
                    throw new BitbakeError(BitbakeErrorType.ConstantLength, consti);

                // Write the size first
                WriteUNumber((ulong)constant.LongLength, Stream);

                // Now write the constant
                Stream.Write(constant, 0, constant.Length);

                consti++;
            }

            // Check the number of symbols
            if (Symbols.Count > GlobalConstants.MaxSymbols)
                throw new BitbakeError(BitbakeErrorType.TooManySymbols);

            // Now write the number of symbols
            Stream.WriteByte((byte)Symbols.Count);

            // Now add the symbols
            foreach (KeyValuePair<byte,byte[]> symbol in Symbols) {
                if (symbol.Key > GlobalConstants.MaxSymbol)
                    throw new BitbakeError(BitbakeErrorType.SymbolOutOfBounds, symbol.Key);

                // Write the id of the symbol
                Stream.WriteByte(symbol.Key);

                if ((ulong)symbol.Value.LongLength > MaxUNumber())
                    throw new BitbakeError(BitbakeErrorType.SymbolLength, symbol.Key);

                // Write the length of the symbol
                WriteUNumber((ulong)symbol.Value.LongLength, Stream);

                // Write the symbol data
                Stream.Write(symbol.Value, 0, symbol.Value.Length);
            }
        }

        private void WriteString(string String, Stream Stream)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(String);
            Stream.WriteByte((byte)bytes.Length);
            Stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteUNumber(ulong Value, Stream Stream)
        {
            BinaryType bits = Type & BinaryType.Bits;
            byte[] bytes;

            switch (bits) {
            case BinaryType.Bits8:
                bytes = BitConverter.GetBytes((byte)(Value & byte.MaxValue));
                break;
            case BinaryType.Bits16:
                bytes = BitConverter.GetBytes((ushort)(Value & ushort.MaxValue));
                break;
            case BinaryType.Bits32:
                bytes = BitConverter.GetBytes((uint)(Value & uint.MaxValue));
                break;
            case BinaryType.Bits64:
                bytes = BitConverter.GetBytes((ulong)(Value & ulong.MaxValue));
                break;
            default:
                return;
            }

            Stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteNumber(long Value, Stream Stream)
        {
            BinaryType bits = Type & BinaryType.Bits;
            byte[] bytes;

            switch (bits) {
            case BinaryType.Bits8:
                bytes = BitConverter.GetBytes((sbyte)(Value & sbyte.MaxValue));
                break;
            case BinaryType.Bits16:
                bytes = BitConverter.GetBytes((short)(Value & short.MaxValue));
                break;
            case BinaryType.Bits32:
                bytes = BitConverter.GetBytes((int)(Value & int.MaxValue));
                break;
            case BinaryType.Bits64:
                bytes = BitConverter.GetBytes((long)(Value & long.MaxValue));
                break;
            default:
                return;
            }

            Stream.Write(bytes, 0, bytes.Length);
        }

        private ulong MaxUNumber()
        {
            BinaryType bits = Type & BinaryType.Bits;

            switch (bits) {
            case BinaryType.Bits8:
                return byte.MaxValue;
            case BinaryType.Bits16:
                return ushort.MaxValue;
            case BinaryType.Bits32:
                return uint.MaxValue;
            case BinaryType.Bits64:
                return ulong.MaxValue;
            }

            return 0;
        }

        private long MaxNumber()
        {
            BinaryType bits = Type & BinaryType.Bits;

            switch (bits) {
            case BinaryType.Bits8:
                return (long)sbyte.MaxValue;
            case BinaryType.Bits16:
                return (long)short.MaxValue;
            case BinaryType.Bits32:
                return (long)int.MaxValue;
            case BinaryType.Bits64:
                return long.MaxValue;
            }
                
            return 0;
        }
    }
}

