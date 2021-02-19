using System;

namespace LoxSharp
{
	class Token
	{
		public TokenType TokenType { get; }
		public String Lexeme { get; }
		public object Literal { get; }
		public int Line { get; }

		public Token(TokenType type, string lexeme, object literal, int line)
		{
			this.TokenType = type;
			this.Lexeme = lexeme;
			this.Literal = literal;
			this.Line = line;
		}

		public override string ToString()
		{
			return string.Format("{0} {1} {2}", TokenType, Lexeme, Literal);
		}
	}
}
