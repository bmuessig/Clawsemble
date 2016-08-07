using System;
using System.Text;

namespace Clawsemble
{
    public class CodeError : Exception
    {
        public CodeErrorType ErrorType { get; private set; }

        public string Details { get; set; }

        public Token Token { get; set; }

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
                if (Token.Type != TokenType.Empty) {
                    message.Append(" near Token (");
                    message.AppendFormat("Type: {0}", Token.Type.ToString());
                    if (!string.IsNullOrEmpty(Token.Content))
                        message.AppendFormat(", Content: \"{0}\"", Token.Content);

                    message.AppendLine(")");

                    if (Token.Line > 0 || Line > 0) {
                        if (Line > 0)
                            message.AppendFormat(" on Line {0}", Line);
                        else
                            message.AppendFormat(" on Line {0}", Token.Line);
                        
                        if (Position > 0)
                            message.AppendFormat(", Symbol {0}", Position);
                        else if (Token.Position > 0)
                            message.AppendFormat(", Symbol {0}", Token.Position);

                        if (!string.IsNullOrEmpty(Filename))
                            message.AppendFormat(", in File \"{0}\"", Filename);
                    } else {
                        if (!string.IsNullOrEmpty(Filename))
                            message.AppendFormat(" in File \"{0}\"", Filename);
                    }
                } else if (Line > 0) {
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

        public CodeError(CodeErrorType ErrorType, string Details, uint Line)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Line = Line;
        }

        public CodeError(CodeErrorType ErrorType, uint Line, string File)
        {
            this.ErrorType = ErrorType;
            this.Line = Line;
            this.Filename = File;
        }

        public CodeError(CodeErrorType ErrorType, string Details, uint Line, string File)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Line = Line;
            this.Filename = File;
        }

        public CodeError(CodeErrorType ErrorType, uint Position, uint Line, string File)
        {
            this.ErrorType = ErrorType;
            this.Position = Position;
            this.Line = Line;
            this.Filename = File;
        }

        public CodeError(CodeErrorType ErrorType, uint Position, string Details, uint Line, string File)
        {
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Position = Position;
            this.Line = Line;
            this.Filename = File;
        }

        public CodeError(CodeErrorType ErrorType, Token Token)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Position = Token.Position;
            this.Line = Token.Line;
        }

        public CodeError(CodeErrorType ErrorType, Token Token, string File)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Position = Token.Position;
            this.Line = Token.Line;
            this.Filename = File;
        }

        public CodeError(CodeErrorType ErrorType, string Details, Token Token, string File)
        {
            this.ErrorType = ErrorType;
            this.Token = Token;
            this.Details = Details;
            this.Position = Token.Position;
            this.Line = Token.Line;
            this.Filename = File;
        }

        public CodeError(uint Line, string File)
        {
            this.Line = Line;
            this.Filename = File;
        }

        public CodeError(uint Position, uint Line, string File)
        {
            this.Position = Position;
            this.Line = Line;
            this.Filename = File;
        }

        public CodeError(Token Token, string File)
        {
            this.Token = Token;
            this.Position = Token.Position;
            this.Line = Token.Line;
            this.Filename = File;
        }
    }
}

