using System.Collections.Generic;

namespace LoxSharp
{
	class Environment
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
		private readonly HashSet<string> _uninitialized = new HashSet<string>();

		public Environment Enclosing { get; }

		public Environment()
		{
			this.Enclosing = null;
		}

		public Environment(Environment enclosing)
		{
			this.Enclosing = enclosing;
		}

		public void Define(string name)
		{
			_uninitialized.Add(name);
		}

		public void Define(string name, object value)
		{
			_values[name] = value;
		}

		public void Assign(Token name, object value)
		{
			if (_values.ContainsKey(name.Lexeme))
			{
				_values[name.Lexeme] = value;

				return;
			}

			if (_uninitialized.Contains(name.Lexeme))
			{
				_values[name.Lexeme] = value;
				_uninitialized.Remove(name.Lexeme);

				return;
			}

			if (Enclosing != null)
			{
				Enclosing.Assign(name, value);
				return;
			}

			throw new RuntimeError(name, string.Format("Undefined variable '{0}'.", name.Lexeme));
		}

		public object Get(Token name)
		{
			if (_values.ContainsKey(name.Lexeme))
			{
				return _values[name.Lexeme];
			}

			if (_uninitialized.Contains(name.Lexeme))
			{
				throw new RuntimeError(name, string.Format("Variable '{0}' was used before initialized.", name.Lexeme));
			}

			if (Enclosing != null)
			{
				return Enclosing.Get(name);
			}

			throw new RuntimeError(name, string.Format("Undefined variable '{0}'.", name.Lexeme));
		}
	}
}
