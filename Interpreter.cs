using System;
using System.Collections.Generic;

namespace LoxSharp
{
	class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
	{
		class LoopControlException : Exception
		{
			public Token Token { get; }

			public LoopControlException(Token token) : base($"'{token.Lexeme} encountered outside of loop body.")
			{
				this.Token = token;
			}
		}

		public class ReturnException : Exception
		{
			public object Value { get; }

			public ReturnException(object value)
			{
				Value = value;
			}
		}

		public Environment Globals { get; } = new Environment();

		private Environment _environment;
		private Dictionary<Expr, int> _locals = new Dictionary<Expr, int>();

		public Interpreter()
		{
			_environment = Globals;
			Globals.Define("clock", new NativeFunction(
				0,
				delegate (Interpreter interpreter, List<object> arguments)
				{
					double seconds = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;

					return seconds;
				}
			));
		}

		public void Interpret(List<Stmt> statements)
		{
			try
			{
				foreach (Stmt statement in statements)
				{
					Execute(statement);
				}
			}
			catch (LoopControlException error)
			{
				Lox.Error(error.Token, error.Message);
			}
			catch (RuntimeError error)
			{
				Lox.Error(error);
			}
		}

		public void Resolve(Expr expr, int depth)
		{
			_locals[expr] = depth;
		}

		public object VisitExpressionStmt(Stmt.Expression stmt)
		{
			Evaluate(stmt.Expr);

			return null;
		}

		public object VisitPrintStmt(Stmt.Print stmt)
		{
			object value = Evaluate(stmt.Expr);
			Console.WriteLine(Stringify(value));

			return null;
		}

		public object VisitFunctionStmt(Stmt.Function stmt)
		{
			LoxFunction function = new LoxFunction(stmt, _environment, LoxFunction.FunctionKind.Function);
			_environment.Define(function.Name, function);

			return null;
		}

		public object VisitClassStmt(Stmt.Class stmt)
		{
			object superclass = null;
			if (stmt.Superclass != null)
			{
				superclass = Evaluate(stmt.Superclass);
				if (!(superclass is LoxClass))
				{
					throw new RuntimeError(stmt.Superclass.Name, "Superclass must be a class.");
				}
			}
			_environment.Define(stmt.Name.Lexeme, null);

			if (stmt.Superclass != null)
			{
				_environment = new Environment(_environment);
				_environment.Define("super", superclass);
			}

			var methods = new Dictionary<string, LoxFunction>();
			foreach (var method in stmt.Methods)
			{
				var kind = LoxFunction.FunctionKind.Function;
				if (method.Getter)
				{
					kind = LoxFunction.FunctionKind.Getter;
				}
				else if (method.Name.Lexeme.Equals("init"))
				{
					kind = LoxFunction.FunctionKind.Initializer;
				}

				var function = new LoxFunction(method, _environment, kind);
				methods[method.Name.Lexeme] = function;
			}

			var statics = new Dictionary<string, LoxFunction>();
			foreach (var staticMethod in stmt.Statics)
			{
				var kind = LoxFunction.FunctionKind.Function;
				if (staticMethod.Getter)
				{
					kind = LoxFunction.FunctionKind.Getter;
				}
				var function = new LoxFunction(staticMethod, _environment, kind);
				statics[staticMethod.Name.Lexeme] = function;
			}

			if (stmt.Superclass != null)
			{
				_environment = _environment.Enclosing;
			}

			LoxClass klass = new LoxClass(stmt.Name.Lexeme, superclass as LoxClass, methods, statics);
			_environment.Assign(stmt.Name, klass);

			return null;
		}

		public object VisitSuperExpr(Expr.Super expr)
		{
			int distance = _locals[expr];
			var superclass = _environment.GetAt(distance, expr.Keyword.Lexeme) as LoxClass;
			var thisObject = _environment.GetAt(distance - 1, "this") as LoxInstance;

			var method = superclass.FindMethod(expr.Method.Lexeme);

			if (method == null)
			{
				throw new RuntimeError(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");
			}

			return method.Bind(thisObject);
		}

		public object VisitThisExpr(Expr.This expr)
		{
			return LookUpVariable(expr.Keyword, expr);
		}

		public object VisitReturnStmt(Stmt.Return stmt)
		{
			object value = null;
			if (stmt.Value != null)
			{
				value = Evaluate(stmt.Value);
			}

			throw new ReturnException(value);
		}

		public object VisitVarStmt(Stmt.Var stmt)
		{
			object value = null;
			if (stmt.Initializer != null)
			{
				value = Evaluate(stmt.Initializer);
				_environment.Define(stmt.Name.Lexeme, value);
			}
			else
			{
				_environment.Define(stmt.Name.Lexeme);
			}

			return null;
		}

		public object VisitBlockStmt(Stmt.Block stmt)
		{
			ExecuteBlock(stmt.Statements, new Environment(_environment));

			return null;
		}

		public object VisitIfStmt(Stmt.If stmt)
		{
			if (IsTruthy(Evaluate(stmt.Condition)))
			{
				Execute(stmt.ThenBranch);
			}
			else
			{
				Execute(stmt.ElseBranch);
			}

			return null;
		}

		public object VisitWhileStmt(Stmt.While stmt)
		{
			while (IsTruthy(Evaluate(stmt.Condition)))
			{
				try
				{
					Execute(stmt.Body);
				}
				catch (LoopControlException exception)
				{
					if (exception.Token.TokenType == TokenType.Break)
					{
						break;
					}
					else
					{
						continue;
					}
				}
			}

			return null;
		}

		public object VisitLoopControlStmt(Stmt.LoopControl stmt)
		{
			throw new LoopControlException(stmt.Token);
		}

		public object VisitLiteralExpr(Expr.Literal expr)
		{
			return expr.Value;
		}

		public object VisitGroupingExpr(Expr.Grouping expr)
		{
			return Evaluate(expr.Expression);
		}

		public object VisitUnaryExpr(Expr.Unary expr)
		{
			object right = Evaluate(expr.Right);

			switch (expr.Op.TokenType)
			{
				case TokenType.Minus:
					CheckNumberOperand(expr.Op, right);
					return -(double)right;
				case TokenType.Bang:
					return !IsTruthy(right);
			}

			return null;
		}

		public object VisitBinaryExpr(Expr.Binary expr)
		{
			object left = Evaluate(expr.Left);
			object right = Evaluate(expr.Right);

			switch (expr.Op.TokenType)
			{
				case TokenType.Plus:
					if (left is double && right is double)
					{
						return (double)left + (double)right;
					}

					if (left is string)
					{
						string rightStr = Stringify(right);

						return (string)left + rightStr;
					}

					throw new RuntimeError(expr.Op, "Operands must be two numbers or two strings");
				case TokenType.Minus:
					CheckNumberOperands(expr.Op, left, right);
					return (double)left - (double)right;
				case TokenType.Slash:
					CheckNumberOperands(expr.Op, left, right);
					if ((double)right == 0)
					{
						throw new RuntimeError(expr.Op, "Cannot divide by zero.");
					}
					return (double)left / (double)right;
				case TokenType.Star:
					CheckNumberOperands(expr.Op, left, right);
					return (double)left * (double)right;
				case TokenType.Greater:
					CheckNumberOperands(expr.Op, left, right);
					return (double)left > (double)right;
				case TokenType.GreaterEqual:
					CheckNumberOperands(expr.Op, left, right);
					return (double)left >= (double)right;
				case TokenType.Less:
					CheckNumberOperands(expr.Op, left, right);
					return (double)left < (double)right;
				case TokenType.LessEqual:
					CheckNumberOperands(expr.Op, left, right);
					return (double)left <= (double)right;
				case TokenType.BangEqual:
					return !IsEqual(left, right);
				case TokenType.EqualEqual:
					return IsEqual(left, right);
			}

			return null;
		}

		public object VisitCallExpr(Expr.Call expr)
		{
			object callee = Evaluate(expr.Callee);

			List<object> arguments = new List<object>();
			foreach (Expr argument in expr.Arguments)
			{
				arguments.Add(Evaluate(argument));
			}

			if (!(callee is ILoxCallable))
			{
				throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
			}

			ILoxCallable function = callee as ILoxCallable;
			if (arguments.Count != function.Arity)
			{
				throw new RuntimeError(expr.Paren, "Expected " + function.Arity + "arguments but got " + arguments.Count + ".");
			}

			return function.Call(this, arguments);
		}

		public object VisitGetExpr(Expr.Get expr)
		{
			object obj = Evaluate(expr.Obj);
			if (obj is LoxInstance)
			{
				object result = ((LoxInstance)obj).Get(expr.Name);
				if (result is LoxFunction)
				{
					var function = (LoxFunction)result;
					if (function.Kind == LoxFunction.FunctionKind.Getter)
					{
						return function.Call(this, new List<object>());
					}
				}

				return result;
			}

			throw new RuntimeError(expr.Name, "Only instances have properties.");
		}

		public object VisitSetExpr(Expr.Set expr)
		{
			object obj = Evaluate(expr.Obj);

			if (!(obj is LoxInstance))
			{
				throw new RuntimeError(expr.Name, "Only instances have fields.");
			}

			object value = Evaluate(expr.Value);
			((LoxInstance)obj).Set(expr.Name, value);

			return value;
		}

		public object VisitLogicalExpr(Expr.Logical expr)
		{
			object left = Evaluate(expr.Left);

			if (expr.Op.TokenType == TokenType.Or)
			{
				if (IsTruthy(left))
				{
					return left;
				}
			}
			else
			{
				if (!IsTruthy(left))
				{
					return left;
				}
			}

			return Evaluate(expr.Right);
		}

		public object VisitVariableExpr(Expr.Variable expr)
		{
			return LookUpVariable(expr.Name, expr);
		}

		public object VisitAssignExpr(Expr.Assign expr)
		{
			object value = Evaluate(expr.Value);

			if (_locals.ContainsKey(expr))
			{
				int distance = _locals[expr];
				_environment.AssignAt(distance, expr.Name, value);
			}
			else
			{
				Globals.Assign(expr.Name, value);
			}

			return value;
		}

		public object VisitFunctionExpr(Expr.Function expr)
		{
			LoxAnonymousFunction function = new LoxAnonymousFunction(expr, _environment);

			return function;
		}

		private string Stringify(object value)
		{
			if (value == null)
			{
				return "nil";
			}

			if (value is double)
			{
				string text = value.ToString();
				if (text.EndsWith(".0"))
				{
					int len = text.Length - 2;
					text = text.Substring(0, len);
				}
				return text;
			}

			if (value is bool)
			{
				return value.ToString().ToLower();
			}

			return value.ToString();
		}

		private void CheckNumberOperand(Token op, object operand)
		{
			if (operand is double)
			{
				return;
			}

			throw new RuntimeError(op, "Operand must be a number");
		}

		private void CheckNumberOperands(Token op, object left, object right)
		{
			if (left is double && right is double)
			{
				return;
			}

			throw new RuntimeError(op, "Operands must be numbers");
		}

		private bool IsEqual(object a, object b)
		{
			if (a == null && b == null)
			{
				return true;
			}

			if (a == null)
			{
				return false;
			}

			return a.Equals(b);
		}

		private bool IsTruthy(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is bool)
			{
				return (bool)obj;
			}

			return true;
		}

		public object Evaluate(Expr expr)
		{
			return expr.Accept(this);
		}

		public void Execute(Stmt stmt)
		{
			if (stmt == null)
			{
				return;
			}

			stmt.Accept(this);
		}

		public void ExecuteBlock(List<Stmt> statements, Environment environment)
		{
			Environment previous = _environment;
			_environment = environment;
			try
			{
				foreach (Stmt stmt in statements)
				{
					Execute(stmt);
				}
			}
			finally
			{
				_environment = previous;
			}
		}

		object LookUpVariable(Token name, Expr expr)
		{
			if (_locals.ContainsKey(expr))
			{
				int distance = _locals[expr];

				return _environment.GetAt(distance, name.Lexeme);
			}
			else
			{
				return Globals.Get(name);
			}
		}
	}
}
