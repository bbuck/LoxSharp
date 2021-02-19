using System.Text;

namespace LoxSharp
{
	class ASTPrinter : Expr.IVisitor<string>
	{
		public string Print(Expr expr)
		{
			if (expr == null)
			{
				return string.Empty;
			}

			return expr.Accept(this);
		}

		public string VisitBinaryExpr(Expr.Binary expr)
		{
			return Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
		}

		public string VisitLogicalExpr(Expr.Logical expr)
		{
			return Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
		}

		public string VisitGroupingExpr(Expr.Grouping expr)
		{
			return Parenthesize("group", expr.Expression);
		}

		public string VisitLiteralExpr(Expr.Literal expr)
		{
			if (expr.Value == null)
			{
				return "nil";
			}

			if (expr.Value is bool)
			{
				bool value = (bool)expr.Value;

				if (value)
				{
					return "true";
				}

				return "false";
			}

			return expr.Value.ToString();
		}

		public string VisitUnaryExpr(Expr.Unary expr)
		{
			return Parenthesize(expr.Op.Lexeme, expr.Right);
		}

		public string VisitVariableExpr(Expr.Variable expr)
		{
			return Parenthesize(expr.Name.Lexeme);
		}

		public string VisitAssignExpr(Expr.Assign expr)
		{
			return Parenthesize("=", new Expr.Variable(expr.Name), expr.Value);
		}

		private string Parenthesize(string name, params Expr[] exprs)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append('(').Append(name);
			foreach (Expr expr in exprs)
			{
				builder.Append(' ').Append(expr.Accept(this));
			}
			builder.Append(')');

			return builder.ToString();
		}
	}
}
