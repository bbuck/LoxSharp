using System.Collections.Generic;

namespace LoxSharp
{
	interface ILoxCallable
	{
		int Arity { get; }

		object Call(Interpreter interpreter, List<object> arguments);
	}
}
