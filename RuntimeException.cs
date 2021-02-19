using System;

namespace LoxSharp
{
	class RuntimeError : Exception
	{
		public Token Token { get; }

		public RuntimeError(Token token, string message) : base(message)
		{
			this.Token = token;
		}
	}
}
