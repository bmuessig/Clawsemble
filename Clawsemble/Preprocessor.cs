using System;
using System.Collections.Generic;
using System.IO;

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
					}
				}
			}
		}
	}
}

