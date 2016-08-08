using System;
using System.Text;

namespace Clawsemble
{
    public class CodeError : Exception
    {
        public CodeErrorType ErrorType { get; set; }

        public string Details { get; set; }

        public Token Token {
            set {
                Line = value.Line;
                Position = value.Position;
                TokenContent = value.Content;
                TokenType = value.Type;
            }
        }

        public string TokenContent { get; set ; }

        public TokenType TokenType { get; set; }

        public uint Line { get; set; }

        public uint Position { get; set; }

        public string Filename { get; set; }

        public new string Message {
            get {
                var message = new StringBuilder();
               
                message.Append(ErrorType.ToString());
                if (!string.IsNullOrEmpty(Details))
                    message.AppendFormat(": {0}", Details);
                message.AppendLine();
                if (TokenType != TokenType.Empty) {
                    message.Append(" near Token (");
                    message.AppendFormat("Type: {0}", TokenType.ToString());
                    if (!string.IsNullOrEmpty(TokenContent))
                        message.AppendFormat(", Content: \"{0}\"", TokenContent);

                    message.AppendLine(")");
                }

                if (Line > 0) {
                    message.AppendFormat(" on Line {0}", Line);

                    if (Position > 0)
                        message.AppendFormat(", Symbol {0}", Position);

                    if (!string.IsNullOrEmpty(Filename))
                        message.AppendFormat(", in File \"{0}\"", Filename);
                } else {
                    if (!string.IsNullOrEmpty(Filename))
                        message.AppendFormat(" in File \"{0}\"", Filename);
                }

                return message.ToString();
            }
        }

        public CodeError()
        {
            this.ErrorType = CodeErrorType.UnknownError;
        }

        public CodeError(CodeErrorType ErrorType)
        {
            this.ErrorType = ErrorType;
        }

        public CodeError(CodeErrorType ErrorType, string Details)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
        }

        public CodeError(CodeErrorType ErrorType, uint Line)
        {
            this.ErrorType = ErrorType;
            this.Line = Line;
        }

        public CodeError(CodeErrorType ErrorType, string Details, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, string Details, uint Line)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Line = Line;
        }

        public CodeError(CodeErrorType ErrorType, uint Line, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Line = Line;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, string Details, uint Line, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Line = Line;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, uint Position, uint Line, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Position = Position;
            this.Line = Line;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, uint Position, string Details, uint Line, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Position = Position;
            this.Line = Line;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, string Details, Token Token)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Details = Details;
        }

        public CodeError(CodeErrorType ErrorType, Token Token)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
        }

        public CodeError(CodeErrorType ErrorType, Token Token, uint Line)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Line = Line;
        }

        public CodeError(CodeErrorType ErrorType, Token Token, uint Position, uint Line)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Position = Position;
            this.Line = Line;
        }

        public CodeError(CodeErrorType ErrorType, Token Token, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, Token Token, uint Position, uint Line, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Position = Position;
            this.Line = Line;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, string Details, Token Token, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Details = Details;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, string Details, Token Token, uint Line, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Line = Line;
            this.Details = Details;
            this.Filename = Filename;
        }

        public CodeError(CodeErrorType ErrorType, string Details, Token Token, uint Position, uint Line, string Filename)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Position = Position;
            this.Line = Line;
            this.Details = Details;
            this.Filename = Filename;
        }

        public CodeError(uint Line, string Filename)
        {
            this.Line = Line;
            this.Filename = Filename;
        }

        public CodeError(uint Position, uint Line, string Filename)
        {
            this.Position = Position;
            this.Line = Line;
            this.Filename = Filename;
        }

        public CodeError(Token Token, string Filename)
        {
            this.Token = Token;
            this.Filename = Filename;
        }
    }
}
