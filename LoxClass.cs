using System.Collections.Generic;

namespace LoxSharp
{
	class LoxClass : LoxInstance, ILoxCallable
	{
		public readonly string Name;
		private readonly Dictionary<string, LoxFunction> _methods;

		private bool _initializerLoaded = false;
		private LoxFunction _initializer;
		private LoxFunction Initializer
		{
			get
			{
				if (!_initializerLoaded)
				{
					_initializerLoaded = true;
					_initializer = FindMethod("init");
				}

				return _initializer;
			}
		}

		public int Arity
		{
			get
			{
				if (Initializer != null)
				{
					return Initializer.Arity;
				}

				return 0;
			}
		}

		public LoxClass(string name, Dictionary<string, LoxFunction> methods, Dictionary<string, LoxFunction> statics) : base(null)
		{
			Name = name;
			_methods = methods;
			if (statics != null)
			{
				_klass = new LoxClass($"{name} Metaclass", statics, null);
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public object Call(Interpreter interpreter, List<object> arguments)
		{
			var instance = new LoxInstance(this);
			if (Initializer != null)
			{
				Initializer.Bind(instance).Call(interpreter, arguments);
			}

			return instance;
		}

		public LoxFunction FindMethod(string name)
		{
			if (_methods.ContainsKey(name))
			{
				return _methods[name];
			}

			return null;
		}
	}
}
