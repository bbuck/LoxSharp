using System.Text;

namespace LoxSharp
{
	class ReversePolishASTPrinter : Expr.IVisitor<string>
	{
		public string Print(Expr expr)
		{
			return expr.Accept(this);
		}

		public string VisitBinaryExpr(Expr.Binary expr)
		{
			return Reverse(expr.Op.Lexeme, expr.Left, expr.Right);
		}

		public string VisitLogicalExpr(Expr.Logical expr)
		{
			return Reverse(expr.Op.Lexeme, expr.Left, expr.Right);
		}

		public string VisitGroupingExpr(Expr.Grouping expr)
		{
			return expr.Expression.Accept(this);
		}

		public string VisitLiteralExpr(Expr.Literal expr)
		{
			if (expr.Value == null)
			{
				return "nil";
			}

			return expr.Value.ToString();
		}

		public string VisitUnaryExpr(Expr.Unary expr)
		{
			return Reverse(expr.Op.Lexeme, expr.Right);
		}

		public string VisitVariableExpr(Expr.Variable expr)
		{
			return expr.Name.Lexeme;
		}

		public string VisitAssignExpr(Expr.Assign expr)
		{
			return Reverse("=", new Expr.Variable(expr.Name), expr.Value);
		}

		private string Reverse(string name, params Expr[] exprs)
		{
			StringBuilder builder = new StringBuilder();
			foreach (Expr expr in exprs)
			{
				builder.Append(expr.Accept(this)).Append(' ');
			}
			builder.Append(name);

			return builder.ToString();
		}
	}
}
