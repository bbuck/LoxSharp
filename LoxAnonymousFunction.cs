using System;
using System.Collections.Generic;

namespace LoxSharp
{
	class LoxAnonymousFunction : ILoxCallable
	{
		public int Arity
		{
			get
			{
				return _declaration.Parameters.Count;
			}
		}

		private readonly Expr.Function _declaration;
		private readonly Environment _closure;

		public LoxAnonymousFunction(Expr.Function declaration, Environment closure)
		{
			_declaration = declaration;
			_closure = closure;
		}

		public object Call(Interpreter interpreter, List<object> arguments)
		{
			Environment environment = new Environment(_closure);
			for (int i = 0; i < _declaration.Parameters.Count; ++i)
			{
				environment.Define(_declaration.Parameters[i].Lexeme, arguments[i]);
			}

			try
			{
				interpreter.ExecuteBlock(_declaration.Body, environment);
			}
			catch (Interpreter.ReturnException ret)
			{
				return ret.Value;
			}

			return null;
		}

		public override string ToString()
		{
			return "<anonymous fn>";
		}
	}
}
