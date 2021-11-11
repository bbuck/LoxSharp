using System.Collections.Generic;

namespace LoxSharp
{
	enum FunctionType
	{
		None,
		Function,
		Method,
	}

	enum VariableStatus
	{
		Declared,
		Defined,
		Used,
	}

	class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object>
	{
		class Variable
		{
			public VariableStatus VariableStatus { get; set; }
			public Token Token { get; set; }
			public string Name { get; set; }
		}

		public static readonly string ThisVariableName = "this";

		private readonly Interpreter _interpreter;
		private readonly List<Dictionary<string, Variable>> _scopes;
		private FunctionType _currentFunction = FunctionType.None;

		public Resolver(Interpreter interpreter)
		{
			_interpreter = interpreter;
			_scopes = new List<Dictionary<string, Variable>>();
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
				var scope = _scopes.Last();
				if (scope.ContainsKey(expr.Name.Lexeme) && scope[expr.Name.Lexeme].VariableStatus == VariableStatus.Declared)
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
			UseVariable(stmt.Name);

			ResolveFunction(stmt, FunctionType.Function);

			return null;
		}

		public object VisitClassStmt(Stmt.Class stmt)
		{
			Declare(stmt.Name);
			Define(stmt.Name);

			BeginScope();
			Declare(ThisVariableName);
			Define(ThisVariableName);
			UseVariable(ThisVariableName);

			foreach (var method in stmt.Methods)
			{
				FunctionType declaration = FunctionType.Method;
				ResolveFunction(method, declaration);
			}

			EndScope();

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

		public object VisitThisExpr(Expr.This expr)
		{
			ResolveLocal(expr, expr.Keyword);

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

		public object VisitGetExpr(Expr.Get expr)
		{
			Resolve(expr.Obj);

			return null;
		}

		public object VisitSetExpr(Expr.Set expr)
		{
			Resolve(expr.Value);
			Resolve(expr.Obj);

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
					_scopes[i][name.Lexeme].VariableStatus = VariableStatus.Used;

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
				UseVariable(param);
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
				UseVariable(param);
			}
			Resolve(function.Body);
			EndScope();

			_currentFunction = enclosing;
		}

		void BeginScope()
		{
			_scopes.Add(new Dictionary<string, Variable>());
		}

		void EndScope()
		{
			var scope = _scopes.Last();

			foreach (var entry in scope)
			{
				if (entry.Value.Token != null && entry.Value.VariableStatus != VariableStatus.Used)
				{
					Lox.Error(entry.Value.Token, "Unused variable");
				}
			}

			_scopes.RemoveAt(_scopes.Count - 1);
		}

		void Declare(Token name)
		{
			if (_scopes.Count == 0)
			{
				return;
			}

			var scope = _scopes.Last();

			if (scope.ContainsKey(name.Lexeme))
			{
				Lox.Error(name, "Already variable with this name in this scope.");
			}

			scope[name.Lexeme] = new Variable
			{
				Token = name,
				VariableStatus = VariableStatus.Declared,
			};
		}

		void Declare(string name)
		{
			if (_scopes.Count == 0)
			{
				return;
			}

			var scope = _scopes.Last();

			scope[name] = new Variable
			{
				Name = name,
				VariableStatus = VariableStatus.Declared,
			};
		}

		void Define(string name)
		{
			if (_scopes.Count == 0)
			{
				return;
			}

			var scope = _scopes.Last();
			scope[name].VariableStatus = VariableStatus.Defined;
		}

		void Define(Token name)
		{
			Define(name.Lexeme);
		}

		void UseVariable(Token name)
		{
			UseVariable(name.Lexeme);
		}

		void UseVariable(string name)
		{
			if (_scopes.Count == 0)
			{
				return;
			}

			var scope = _scopes.Last();
			scope[name].VariableStatus = VariableStatus.Used;
		}
	}
}
