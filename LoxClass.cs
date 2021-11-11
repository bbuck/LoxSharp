using System.Collections.Generic;

namespace LoxSharp
{
	class LoxClass : ILoxCallable
	{
		public readonly string Name;

		public int Arity
		{
			get
			{
				return 0;
			}
		}

		public LoxClass(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}

		public object Call(Interpreter interpreter, List<object> arguments)
		{
			var instance = new LoxInstance(this);

			return instance;
		}
	}
}
