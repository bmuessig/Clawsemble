using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;

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
			int ifdepth = 0;

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
						ifdepth++;
					} else if (directive == "elif" || directive == "elseif") {

					} else if (directive == "endif") {
						ifdepth--;
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
					} else
						throw new Exception("Unknown compiler directive!");
				}
			}
		}

		private long EvaluateExpression(int Pointer, List<Token> Tokens)
		{
			var stack = new Stack<Constant>();

			for (;; Pointer++) {
				if (Tokens[Pointer].Type == TokenType.Break) {
					break;
				} else if (Tokens[Pointer].Type == TokenType.Number ||
				           Tokens[Pointer].Type == TokenType.Character ||
				           Tokens[Pointer].Type == TokenType.HexadecimalEscape) {
					stack.Push(new Constant(Tokens[Pointer]));
				} else if (Tokens[Pointer].Type == TokenType.Word) {
					if (Constants.ContainsKey(Tokens[Pointer].Content.Trim())) {
						Constant cvar = Constants[Tokens[Pointer].Content.Trim()];
						if (cvar.Type != ConstantType.Empty)
							stack.Push(cvar);
						else
							throw new Exception("Constant is empty!");
					} else
						throw new Exception("Constant not found!");
				} else if (Tokens[Pointer].Type == TokenType.ParanthesisOpen) {

				} else {

				}
			}
		}

	}
}

