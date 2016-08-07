using System;

namespace Clawsemble
{
    public class CodeError : Exception
    {
        public string Error { get; private set; }

        public string Details { get; private set; }

        public CodeErrorType ErrorType { get; private set; }

        public TokenType TokenType { get; private set; }

        public uint Line { get; private set; }

        public string File { get; private set; }

        public new string Message { get; private set; }

        public CodeError()
        {
            this.Message = "An unknown error occurred!";
        }

        public CodeError(String Message)
        {
            this.Message = Message;
        }

        public CodeError(string Error, uint Line, string File)
        {
            Message = string.Format("\"{0}\" at Line {1} in File \"{2}\"", Error, Line, File);
            this.Error = Error;
            this.Line = Line;
            this.File = File;
        }

        public CodeError(string Error, TokenType TokenType, uint Line, string File)
        {
            Message = string.Format("\"{0}\" near Token of type \"{1}\" on Line {2} in File \"{3}\"", Error, TokenType.ToString(), Line, File);
            this.Error = Error;
            this.TokenType = TokenType;
            this.Line = Line;
            this.File = File;
        }

        public CodeError(CodeErrorType ErrorType, uint Line, string File)
        {
            Message = string.Format("{0} on Line {1} in File \"{2}\"", ErrorType.ToString(), Line, File);
            this.ErrorType = ErrorType;
            this.Line = Line;
            this.File = File;
        }

        public CodeError(CodeErrorType ErrorType, string Details, uint Line, string File)
        {
            Message = string.Format("{0} [{1}] on Line {2} in File \"{3}\"", ErrorType.ToString(), Details, Line, File);
            this.ErrorType = ErrorType;
            this.Details = Details;
            this.Line = Line;
            this.File = File;
        }

        public CodeError(CodeErrorType ErrorType, TokenType TokenType, uint Line, string File)
        {
            Message = string.Format("{0} near Token of type \"{1}\" on Line {2} in File \"{3}\"", ErrorType.ToString(), TokenType.ToString(), Line, File);
            this.ErrorType = ErrorType;
            this.TokenType = TokenType;
            this.Line = Line;
            this.File = File;
        }

        public CodeError(CodeErrorType ErrorType, string Details, TokenType TokenType, uint Line, string File)
        {
            Message = string.Format("{0} [{1}] near Token of type \"{2}\" on Line {3} in File \"{4}\"", ErrorType.ToString(), Details, TokenType.ToString(), Line, File);
            this.ErrorType = ErrorType;
            this.TokenType = TokenType;
            this.Details = Details;
            this.Line = Line;
            this.File = File;
        }

        public CodeError(uint Line, string File)
        {
            Message = string.Format("Unknown Error on Line {0} in File \"{1}\"", Line, File);
            this.Line = Line;
            this.File = File;
        }

        public CodeError(uint Line, string File, TokenType TokenType)
        {
            Message = string.Format("Unknown Error near Token of type \"{0}\" on Line {1} in File \"{2}\"", TokenType.ToString(), File, Line);
            this.TokenType = TokenType;
            this.Line = Line;
            this.File = File;
        }
    }
}

