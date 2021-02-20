using System;
using System.Collections.Generic;

namespace LoxSharp
{
	class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
	{
		class LoopControlException : Exception
		{
			public Token Token { get; }

			public LoopControlException(Token token) : base(string.Format("'{0}' encountered outside of loop body.", token.Lexeme))
			{
				this.Token = token;
			}
		}

		private Environment _environment = new Environment();

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
			return _environment.Get(expr.Name);
		}

		public object VisitAssignExpr(Expr.Assign expr)
		{
			object value = Evaluate(expr.Value);
			_environment.Assign(expr.Name, value);

			return value;
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

		private void ExecuteBlock(List<Stmt> statements, Environment environment)
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
	}
}
