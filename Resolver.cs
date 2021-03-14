using System.Collections.Generic;

namespace LoxSharp
{
	enum FunctionType
	{
		None,
		Function,
	}

	class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object>
	{
		private readonly Interpreter _interpreter;
		private readonly List<Dictionary<string, bool>> _scopes;
		private FunctionType _currentFunction = FunctionType.None;

		public Resolver(Interpreter interpreter)
		{
			_interpreter = interpreter;
			_scopes = new List<Dictionary<string, bool>>();
		}

		public object VisitBlockStmt(Stmt.Block stmt)
		{
			BeginScope();
			Resolve(stmt.Statements);
			EndScope();

			return null;
		}

		public object VisitVarStmt(Stmt.Var stmt)
		{
			Declare(stmt.Name);
			if (stmt.Initializer != null)
			{
				Resolve(stmt.Initializer);
			}
			Define(stmt.Name);

			return null;
		}

		public object VisitVariableExpr(Expr.Variable expr)
		{
			if (_scopes.Count > 0)
			{
				var scope = _scopes[_scopes.Count - 1];
				if (scope.ContainsKey(expr.Name.Lexeme) && scope[expr.Name.Lexeme] == false)
				{
					Lox.Error(expr.Name, "Can't read local variable in its own initializer.");
				}
			}

			ResolveLocal(expr, expr.Name);

			return null;
		}

		public object VisitFunctionStmt(Stmt.Function stmt)
		{
			Declare(stmt.Name);
			Define(stmt.Name);

			ResolveFunction(stmt, FunctionType.Function);

			return null;
		}

		public object VisitFunctionExpr(Expr.Function expr)
		{
			ResolveFunction(expr, FunctionType.Function);

			return null;
		}

		public object VisitAssignExpr(Expr.Assign expr)
		{
			Resolve(expr.Value);
			ResolveLocal(expr, expr.Name);

			return null;
		}

		public object VisitExpressionStmt(Stmt.Expression stmt)
		{
			Resolve(stmt.Expr);

			return null;
		}

		public object VisitIfStmt(Stmt.If stmt)
		{
			Resolve(stmt.Condition);
			Resolve(stmt.ThenBranch);
			Resolve(stmt.ElseBranch);

			return null;
		}

		public object VisitPrintStmt(Stmt.Print stmt)
		{
			Resolve(stmt.Expr);

			return null;
		}

		public object VisitReturnStmt(Stmt.Return stmt)
		{
			if (_currentFunction == FunctionType.None)
			{
				Lox.Error(stmt.Keyword, "Can't return from top-level code.");
			}

			if (stmt.Value != null)
			{
				Resolve(stmt.Value);
			}

			return null;
		}

		public object VisitWhileStmt(Stmt.While stmt)
		{
			Resolve(stmt.Condition);
			Resolve(stmt.Body);

			return null;
		}

		public object VisitLoopControlStmt(Stmt.LoopControl stmt)
		{
			return null;
		}

		public object VisitBinaryExpr(Expr.Binary expr)
		{
			Resolve(expr.Left);
			Resolve(expr.Right);

			return null;
		}

		public object VisitCallExpr(Expr.Call expr)
		{
			Resolve(expr.Callee);

			foreach (Expr argument in expr.Arguments)
			{
				Resolve(argument);
			}

			return null;
		}

		public object VisitGroupingExpr(Expr.Grouping expr)
		{
			Resolve(expr.Expression);

			return null;
		}

		public object VisitLogicalExpr(Expr.Logical expr)
		{
			Resolve(expr.Left);
			Resolve(expr.Right);

			return null;
		}

		public object VisitUnaryExpr(Expr.Unary expr)
		{
			Resolve(expr.Right);

			return null;
		}

		public object VisitLiteralExpr(Expr.Literal expr)
		{
			return null;
		}

		public void Resolve(List<Stmt> statements)
		{
			foreach (Stmt statement in statements)
			{
				Resolve(statement);
			}
		}

		public void Resolve(Stmt stmt)
		{
			stmt.Accept(this);
		}

		public void Resolve(Expr expr)
		{
			expr.Accept(this);
		}

		void ResolveLocal(Expr expr, Token name)
		{
			for (int i = _scopes.Count - 1; i >= 0; --i)
			{
				if (_scopes[i].ContainsKey(name.Lexeme))
				{
					_interpreter.Resolve(expr, _scopes.Count - 1 - i);

					return;
				}
			}
		}

		void ResolveFunction(Stmt.Function function, FunctionType functionType)
		{
			var enclosing = _currentFunction;
			_currentFunction = functionType;

			BeginScope();
			foreach (Token param in function.Parameters)
			{
				Declare(param);
				Define(param);
			}
			Resolve(function.Body);
			EndScope();

			_currentFunction = enclosing;
		}

		void ResolveFunction(Expr.Function function, FunctionType functionType)
		{
			var enclosing = _currentFunction;
			_currentFunction = functionType;

			BeginScope();
			foreach (Token param in function.Parameters)
			{
				Declare(param);
				Define(param);
			}
			Resolve(function.Body);
			EndScope();

			_currentFunction = enclosing;
		}

		void BeginScope()
		{
			_scopes.Add(new Dictionary<string, bool>());
		}

		void EndScope()
		{
			_scopes.RemoveAt(_scopes.Count - 1);
		}

		void Declare(Token name)
		{
			if (_scopes.Count == 0)
			{
				return;
			}

			var scope = _scopes[_scopes.Count - 1];

			if (scope.ContainsKey(name.Lexeme))
			{
				Lox.Error(name, "Already variable with this name in this scope.");
			}

			scope[name.Lexeme] = false;
		}

		void Define(Token name)
		{
			if (_scopes.Count == 0)
			{
				return;
			}

			var scope = _scopes[_scopes.Count - 1];
			scope[name.Lexeme] = true;
		}
	}
}
