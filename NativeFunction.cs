using System;
using System.Collections.Generic;

namespace LoxSharp
{
	delegate object NativeFunctionBody(Interpreter interpreter, List<object> arguments);

	class NativeFunction : ILoxCallable
	{
		private NativeFunctionBody _body;
		private int _arity;

		public int Arity
		{
			get
			{
				return _arity;
			}
		}
		public NativeFunction(int arity, NativeFunctionBody body)
		{
			_body = body;
			_arity = arity;
		}

		public object Call(Interpreter interpreter, List<object> arguments)
		{
			return _body.Invoke(interpreter, arguments);
		}

		public override string ToString()
		{
			return "<native fn>";
		}
	}
}
