using System;

namespace LoxSharp
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length > 1)
			{
				Console.WriteLine("Usage: cslox [script]");
				Lox.Exit(64);
			}
			else if (args.Length == 1)
			{
				Lox.RunFile(args[0]);
			}
			else
			{
				Lox.RunPrompt();
			}
		}

		// static void Main(string[] args)
		// {
		// 	// (1 + 2) * (4 - 3)

		// 	Expr expression = new Expr.Binary(
		// 		new Expr.Grouping(
		// 			new Expr.Binary(
		// 				new Expr.Literal(1),
		// 				new Token(TokenType.Plus, "+", null, 1),
		// 				new Expr.Literal(2))),
		// 		new Token(TokenType.Star, "*", null, 1),
		// 		new Expr.Grouping(
		// 			new Expr.Binary(
		// 				new Expr.Literal(4),
		// 				new Token(TokenType.Minus, "-", null, 1),
		// 				new Expr.Literal(3))));
		// 	var printer = new ReversePolishASTPrinter();
		// 	Console.WriteLine(printer.Print(expression));
		// }
	}
}

