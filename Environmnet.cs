using System.Collections.Generic;

namespace LoxSharp
{
	class Environment
	{
		private readonly Dictionary<string, object> Values = new Dictionary<string, object>();
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
			Values[name] = value;
		}

		public void Assign(Token name, object value)
		{
			if (Values.ContainsKey(name.Lexeme))
			{
				Values[name.Lexeme] = value;

				return;
			}

			if (_uninitialized.Contains(name.Lexeme))
			{
				Values[name.Lexeme] = value;
				_uninitialized.Remove(name.Lexeme);

				return;
			}

			if (Enclosing != null)
			{
				Enclosing.Assign(name, value);
				return;
			}

			throw new RuntimeError(name, string.Format("Undefined variable '{0}' assigned to.", name.Lexeme));
		}

		public void AssignAt(int distance, Token name, object value)
		{
			Ancestor(distance).Values[name.Lexeme] = value;
		}

		public object Get(Token name)
		{
			if (Values.ContainsKey(name.Lexeme))
			{
				return Values[name.Lexeme];
			}

			if (_uninitialized.Contains(name.Lexeme))
			{
				throw new RuntimeError(name, string.Format("Variable '{0}' was used before initialized.", name.Lexeme));
			}

			if (Enclosing != null)
			{
				return Enclosing.Get(name);
			}

			throw new RuntimeError(name, string.Format("Undefined variable '{0}' referenced.", name.Lexeme));
		}

		public object GetAt(int distance, string name)
		{
			return Ancestor(distance).Values[name];
		}

		Environment Ancestor(int distance)
		{
			Environment environment = this;
			for (int i = 0; i < distance; ++i)
			{
				environment = environment.Enclosing;
			}

			return environment;
		}
	}
}
