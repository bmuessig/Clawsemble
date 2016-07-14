using System;

namespace Clawsemble
{
	public enum TokenType
	{
		Empty,
		Error,
		Comment,
		PreprocessorDirective,
		Assign,
		LessThan,
		Equal,
		GreaterThan,
		BitwiseAnd,
		LogicalAnd,
		BitwiseOr,
		LogicalOr,
		Plus,
		Minus,
		Multiply,
		Divide,
		BitshiftLeft,
		BitshiftRight,
		ParanthesisOpen,
		ParanthesisClose,
		CompilerDirective,
		String,
		Character,
		CharacterEscape,
		HexadecimalEscape,
		Word,
		Number,
		Seperator,
		ArrayOpen,
		ArrayClose,
		Break
	}
}

