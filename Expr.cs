// This file is auto-generated, do not modify

using System.Collections.Generic;

namespace LoxSharp
{
	abstract class Expr
	{
		public abstract R Accept<R>(IVisitor<R> visitor);

		public interface IVisitor<R>
		{
			R VisitBinaryExpr(Binary expr);
			R VisitCallExpr(Call expr);
			R VisitFunctionExpr(Function expr);
			R VisitLogicalExpr(Logical expr);
			R VisitGroupingExpr(Grouping expr);
			R VisitLiteralExpr(Literal expr);
			R VisitUnaryExpr(Unary expr);
			R VisitVariableExpr(Variable expr);
			R VisitAssignExpr(Assign expr);
		}

		public class Binary : Expr
		{
			public Expr Left { get; }
			public Token Op { get; }
			public Expr Right { get; }

			public Binary(Expr left, Token op, Expr right)
			{
				this.Left = left;
				this.Op = op;
				this.Right = right;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitBinaryExpr(this);
			}
		}

		public class Call : Expr
		{
			public Expr Callee { get; }
			public Token Paren { get; }
			public List<Expr> Arguments { get; }

			public Call(Expr callee, Token paren, List<Expr> arguments)
			{
				this.Callee = callee;
				this.Paren = paren;
				this.Arguments = arguments;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitCallExpr(this);
			}
		}

		public class Function : Expr
		{
			public List<Token> Parameters { get; }
			public List<Stmt> Body { get; }

			public Function(List<Token> parameters, List<Stmt> body)
			{
				this.Parameters = parameters;
				this.Body = body;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitFunctionExpr(this);
			}
		}

		public class Logical : Expr
		{
			public Expr Left { get; }
			public Token Op { get; }
			public Expr Right { get; }

			public Logical(Expr left, Token op, Expr right)
			{
				this.Left = left;
				this.Op = op;
				this.Right = right;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitLogicalExpr(this);
			}
		}

		public class Grouping : Expr
		{
			public Expr Expression { get; }

			public Grouping(Expr expression)
			{
				this.Expression = expression;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitGroupingExpr(this);
			}
		}

		public class Literal : Expr
		{
			public object Value { get; }

			public Literal(object value)
			{
				this.Value = value;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitLiteralExpr(this);
			}
		}

		public class Unary : Expr
		{
			public Token Op { get; }
			public Expr Right { get; }

			public Unary(Token op, Expr right)
			{
				this.Op = op;
				this.Right = right;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitUnaryExpr(this);
			}
		}

		public class Variable : Expr
		{
			public Token Name { get; }

			public Variable(Token name)
			{
				this.Name = name;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitVariableExpr(this);
			}
		}

		public class Assign : Expr
		{
			public Token Name { get; }
			public Expr Value { get; }

			public Assign(Token name, Expr value)
			{
				this.Name = name;
				this.Value = value;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitAssignExpr(this);
			}
		}
	}
}
