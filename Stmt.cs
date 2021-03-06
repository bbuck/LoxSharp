// This file is auto-generated, do not modify

using System.Collections.Generic;

namespace LoxSharp
{
	abstract class Stmt
	{
		public abstract R Accept<R>(IVisitor<R> visitor);

		public interface IVisitor<R>
		{
			R VisitExpressionStmt(Expression stmt);
			R VisitFunctionStmt(Function stmt);
			R VisitReturnStmt(Return stmt);
			R VisitPrintStmt(Print stmt);
			R VisitVarStmt(Var stmt);
			R VisitBlockStmt(Block stmt);
			R VisitIfStmt(If stmt);
			R VisitWhileStmt(While stmt);
			R VisitLoopControlStmt(LoopControl stmt);
		}

		public class Expression : Stmt
		{
			public Expr Expr { get; }

			public Expression(Expr expr)
			{
				this.Expr = expr;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitExpressionStmt(this);
			}
		}

		public class Function : Stmt
		{
			public Token Name { get; }
			public List<Token> Parameters { get; }
			public List<Stmt> Body { get; }

			public Function(Token name, List<Token> parameters, List<Stmt> body)
			{
				this.Name = name;
				this.Parameters = parameters;
				this.Body = body;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitFunctionStmt(this);
			}
		}

		public class Return : Stmt
		{
			public Token Keyword { get; }
			public Expr Value { get; }

			public Return(Token keyword, Expr value)
			{
				this.Keyword = keyword;
				this.Value = value;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitReturnStmt(this);
			}
		}

		public class Print : Stmt
		{
			public Expr Expr { get; }

			public Print(Expr expr)
			{
				this.Expr = expr;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitPrintStmt(this);
			}
		}

		public class Var : Stmt
		{
			public Token Name { get; }
			public Expr Initializer { get; }

			public Var(Token name, Expr initializer)
			{
				this.Name = name;
				this.Initializer = initializer;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitVarStmt(this);
			}
		}

		public class Block : Stmt
		{
			public List<Stmt> Statements { get; }

			public Block(List<Stmt> statements)
			{
				this.Statements = statements;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitBlockStmt(this);
			}
		}

		public class If : Stmt
		{
			public Expr Condition { get; }
			public Stmt ThenBranch { get; }
			public Stmt ElseBranch { get; }

			public If(Expr condition, Stmt thenBranch, Stmt elseBranch)
			{
				this.Condition = condition;
				this.ThenBranch = thenBranch;
				this.ElseBranch = elseBranch;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitIfStmt(this);
			}
		}

		public class While : Stmt
		{
			public Expr Condition { get; }
			public Stmt Body { get; }

			public While(Expr condition, Stmt body)
			{
				this.Condition = condition;
				this.Body = body;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitWhileStmt(this);
			}
		}

		public class LoopControl : Stmt
		{
			public Token Token { get; }

			public LoopControl(Token token)
			{
				this.Token = token;
			}

			public override R Accept<R>(IVisitor<R> visitor)
			{
				return visitor.VisitLoopControlStmt(this);
			}
		}
	}
}
