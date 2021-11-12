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
		private readonly bool _isInitializer = false;

		public LoxFunction(Stmt.Function declaration, Environment closure, bool isInitializer)
		{
			_isInitializer = isInitializer;
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
				if (_isInitializer)
				{
					return _closure.GetAt(0, "this");
				}

				return ret.Value;
			}

			if (_isInitializer)
			{
				return _closure.GetAt(0, "this");
			}

			return null;
		}

		public LoxFunction Bind(LoxInstance instance)
		{
			var environment = new Environment(_closure);
			environment.Define("this", instance);

			return new LoxFunction(_declaration, environment, _isInitializer);
		}

		public override string ToString()
		{
			return "<fn " + Name + ">";
		}
	}
}
