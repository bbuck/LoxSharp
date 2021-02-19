using System;
using System.Collections.Generic;

namespace LoxSharp
{
	static class Lox
	{
		private static readonly Interpreter _Interpreter = new Interpreter();

		static bool _hadError = false;
		static bool _hadRuntimeError = false;

		public static void RunFile(string fileName)
		{
			byte[] bytes = System.IO.File.ReadAllBytes(fileName);
			Run(System.Text.Encoding.Default.GetString(bytes));

			if (_hadError)
			{
				Exit(65);
			}

			if (_hadRuntimeError)
			{
				Exit(70);
			}
		}

		public static void RunPrompt()
		{
			while (true)
			{
				Console.Write("> ");
				string line = Console.ReadLine();
				if (line == null)
				{
					break;
				}
				RunLine(line);
				_hadError = false;
			}
		}

		public static void Error(int line, string message)
		{
			Report(line, message);
		}

		public static void Error(Token token, string message)
		{
			string where = string.Empty;
			if (token.TokenType == TokenType.EOF)
			{
				where = " at end";
			}
			else
			{
				where = string.Format(" at '{0}'", token.Lexeme);
			}
			Report(token.Line, where, message);
		}

		public static void Error(RuntimeError error)
		{
			Report(error.Token.Line, error.Message);
			_hadRuntimeError = true;
		}

		public static void Report(int line, string message)
		{
			Report(line, string.Empty, message);
		}

		public static void Report(int line, string where, string message)
		{
			Console.Error.WriteLine(string.Format("[line {0}] Error{1}: {2}", line, where, message));
		}

		public static void Exit(int code)
		{
			System.Environment.Exit(code);
		}

		static void Run(string code)
		{
			Scanner scanner = new Scanner(code);
			List<Token> tokens = scanner.ScanTokens();

			Parser parser = new Parser(tokens);
			List<Stmt> statements = parser.Parse();

			if (_hadError)
			{
				return;
			}

			_Interpreter.Interpret(statements);
		}

		static void RunLine(string code)
		{
			Scanner scanner = new Scanner(code);
			List<Token> tokens = scanner.ScanTokens();

			Parser parser = new Parser(tokens);
			List<Stmt> statements = parser.Parse();

			if (_hadError)
			{
				return;
			}

			Stmt stmt = statements[0];
			if (stmt is Stmt.Expression)
			{
				Expr expr = (stmt as Stmt.Expression).Expr;
				object result = _Interpreter.Evaluate(expr);

				Console.WriteLine(Inspect(result));
			}
			else
			{
				_Interpreter.Execute(stmt);
			}
		}

		static string Inspect(object value)
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

			if (value is string)
			{
				return string.Format("\"{0}\"", value);
			}

			return value.ToString();
		}
	}
}
