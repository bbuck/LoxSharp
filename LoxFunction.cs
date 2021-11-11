using System;
using System.Collections.Generic;

namespace LoxSharp
{
	class LoxFunction : ILoxCallable
	{
		public int Arity
		{
			get
			{
				return _declaration.Parameters.Count;
			}
		}

		public string Name
		{
			get
			{
				return _declaration.Name.Lexeme;
			}
		}

		private readonly Stmt.Function _declaration;
		private readonly Environment _closure;

		public LoxFunction(Stmt.Function declaration, Environment closure)
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

		public LoxFunction Bind(LoxInstance instance)
		{
			var environment = new Environment(_closure);
			environment.Define("this", instance);

			return new LoxFunction(_declaration, environment);
		}

		public override string ToString()
		{
			return "<fn " + Name + ">";
		}
	}
}
