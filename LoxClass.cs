using System.Collections.Generic;

namespace LoxSharp
{
	class LoxClass : LoxInstance, ILoxCallable
	{
		public readonly string Name;
		public readonly Dictionary<string, LoxFunction> Methods;

		private readonly LoxClass _superclass;
		private readonly List<LoxClass> _mixins;

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

		public bool IsSubclass
		{
			get
			{
				return _superclass != null;
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

		public LoxClass(
			string name,
			LoxClass superclass,
			List<LoxClass> mixins,
			Dictionary<string, LoxFunction> methods,
			Dictionary<string, LoxFunction> statics) : base(null)
		{
			Name = name;
			_superclass = superclass;
			Methods = methods;
			_mixins = mixins;
			if (statics != null)
			{
				_klass = new LoxClass($"{name} Metaclass", null, mixins, statics, null);
			}
		}

		public LoxClass(
			string name,
			LoxClass superclass,
			Dictionary<string, LoxFunction> methods,
			Dictionary<string, LoxFunction> statics)
				: this(name, superclass, null, methods, statics)
		{ }

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
			if (Methods.ContainsKey(name))
			{
				return Methods[name];
			}

			if (_superclass != null)
			{
				return _superclass.FindMethod(name);
			}

			if (_mixins != null && _mixins.Count > 0)
			{
				foreach (var mixin in _mixins)
				{
					var method = mixin.FindMethod(name);
					if (method != null)
					{
						return method;
					}
				}
			}

			return null;
		}
	}
}
