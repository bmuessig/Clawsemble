using System;

namespace Clawsemble
{
	public class Constant
	{
		public readonly long Number;
		public readonly string String;
		public readonly ConstantType Type;

		public Constant()
		{
			Type = ConstantType.Empty;
		}

		public Constant(Token Token)
			: this(Token.Type, Token.Content)
		{
		}

		public Constant(TokenType Type, string Content)
		{
			if (Type == TokenType.String) {
				String = Content;
				this.Type = ConstantType.String;
			} else if (Type == TokenType.Number) {
				try {
					Number = Convert.ToInt64(Content);
					this.Type = ConstantType.Numeric;
				} catch (Exception) {
					throw new Exception("Invalid number!");
				}
			} else if (Type == TokenType.HexadecimalEscape) {
				if (Content.Length > 0 && Content.Length <= 8) {
					try {
						Number = Convert.ToInt64(Content, 16);
						this.Type = ConstantType.Numeric;
					} catch (Exception) {
						throw new Exception("Invalid hexadecimal number!");
					}
				} else
					throw new Exception("Hexadecimal escape size missmatch!");
			} else if (Type == TokenType.Character) {
				if (Content.Length == 1) {
					Number = (char)Content[0];
					this.Type = ConstantType.Numeric;
				} else
					throw new Exception("Invalid character size!");
			} else if (Type == TokenType.Empty)
				this.Type = ConstantType.Empty;
			else
				throw new Exception("Invalid token!");
		}

		public static bool TryParse(Token Token, out Constant Output)
		{
			try {
				Output = new Constant(Token);
			} catch (Exception) {
				Output = null;
				return false;
			}
			return true;
		}
	}
}

