using System;
using System.Collections.Generic;

namespace LoxSharp
{
	class Parser
	{
		private class ParseError : Exception { }

		private readonly List<Token> _tokens;
		private int _current = 0;

		public Parser(List<Token> tokens)
		{
			this._tokens = tokens;
		}

		// program -> declaration* EOF
		public List<Stmt> Parse()
		{
			List<Stmt> statements = new List<Stmt>();
			while (!IsAtEnd())
			{
				statements.Add(Declaration());
			}

			return statements;
		}

		// declaration -> classDecl
		//              | funDecl
		//              | varDecl
		//              | statement
		// funDecl -> "fun" function
		private Stmt Declaration()
		{
			try
			{
				if (Match(TokenType.Class))
				{
					return ClassDeclaration();
				}

				if (Match(TokenType.Fun))
				{
					return Function("function");
				}

				if (Match(TokenType.Var))
				{
					return VarDeclaration();
				}

				return Statement();
			}
			catch (ParseError)
			{
				Synchronize();

				return null;
			}
		}

		// classDecl -> "class" IDENTIFIER
		//              ( "<" IDENTIFIER )?
		//              ( "<=" IDENTIFIER ( "," IDENTIFIER )* )?
		//              "{" ( ( "class" )? function )* "}" ;
		private Stmt ClassDeclaration()
		{
			Token name = Consume(TokenType.Identifier, "Expect class name.");

			// Superclass?
			Expr.Variable superclass = null;
			if (Match(TokenType.Less))
			{
				Consume(TokenType.Identifier, "Expect superclass name.");
				superclass = new Expr.Variable(Previous());
			}

			var mixins = new List<Expr.Variable>();
			if (Match(TokenType.LessEqual))
			{
				while (!Check(TokenType.RightBrace) && !IsAtEnd())
				{
					Consume(TokenType.Identifier, "Expect mixin name.");
					mixins.Add(new Expr.Variable(Previous()));
					if (!Match(TokenType.Comma))
					{
						break;
					}

				}
			}

			Consume(TokenType.LeftBrace, "Expect '{' before class body.");

			var methods = new List<Stmt.Function>();
			var statics = new List<Stmt.Function>();
			while (!Check(TokenType.RightBrace) && !IsAtEnd())
			{
				if (Match(TokenType.Class))
				{
					statics.Add(Function("static_method") as Stmt.Function);
				}
				else
				{
					methods.Add(Function("method") as Stmt.Function);
				}
			}

			Consume(TokenType.RightBrace, "Expect '}' after class body.");

			return new Stmt.Class(name, superclass, mixins, methods, statics);
		}

		// function -> IDENTIFIER ( "(" paramters? ")" )? block
		// paramters -> IDENTIFIER ( "," IDENTIFIER )*
		private Stmt Function(string kind)
		{
			if (kind == "function" && !Check(TokenType.Identifier))
			{
				Expr expr = AnonymousFunction();
				Consume(TokenType.Semicolon, "Expected ';' after expression");

				return new Stmt.Expression(expr);
			}

			Token name = Consume(TokenType.Identifier, $"Expect {kind} name.");

			List<Token> parameters = new List<Token>();

			var hasParamList = Check(TokenType.LeftParen);
			if (hasParamList)
			{
				// Consume the left parenthesis.
				Advance();

				if (!Check(TokenType.RightParen))
				{
					do
					{
						if (parameters.Count >= 255)
						{
							Error(Peek(), "Can't have more than 255 parameters");
						}

						parameters.Add(Consume(TokenType.Identifier, "Expect parameter name."));
					} while (Match(TokenType.Comma));
				}
				Consume(TokenType.RightParen, "Expect ')' after parameters");
			}
			else if (!hasParamList && kind == "function")
			{
				Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");
			}

			Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
			List<Stmt> body = Block();

			return new Stmt.Function(name, parameters, body, !hasParamList);
		}

		// varDecl -> "var" IDENTIFIER ( "=" expression )? ";"
		private Stmt VarDeclaration()
		{
			Token name = Consume(TokenType.Identifier, "Expected variable name.");

			Expr initializer = null;
			if (Match(TokenType.Equal))
			{
				initializer = Expression();
			}

			Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");

			return new Stmt.Var(name, initializer);
		}

		// statement -> exprStmt
		//            | ifStmt
		//            | printStmt
		//            | whileStmt
		//            | forStmt
		//            | returnStmt
		//            | loopControl
		//            | block
		private Stmt Statement()
		{
			if (Match(TokenType.If))
			{
				return IfStatement();
			}

			if (Match(TokenType.Print))
			{
				return PrintStatement();
			}

			if (Match(TokenType.While))
			{
				return While();
			}

			if (Match(TokenType.For))
			{
				return ForStatement();
			}

			if (Match(TokenType.Return))
			{
				return ReturnStatement();
			}

			if (Match(TokenType.LeftBrace))
			{
				return new Stmt.Block(Block());
			}

			if (Match(TokenType.Break, TokenType.Continue))
			{
				return LoopControl();
			}

			return ExpressionStatement();
		}

		// returnStmt -> "return" expression? ";"
		private Stmt ReturnStatement()
		{
			Token keyword = Previous();
			Expr value = null;
			if (!Check(TokenType.Semicolon))
			{
				value = Expression();
			}

			Consume(TokenType.Semicolon, "Expected ';' after return statement");

			return new Stmt.Return(keyword, value);
		}

		// loopControlStmt -> "break" ";"
		//                  | "continue" ";"
		private Stmt LoopControl()
		{
			Token token = Previous();
			Consume(TokenType.Semicolon, string.Format("Expected ';' after '{0}'.", token.Lexeme));

			return new Stmt.LoopControl(token);
		}

		// ifStmt -> "if" "(" expression ")" statement ( "else" statement )?
		private Stmt IfStatement()
		{
			Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
			Expr condition = Expression();
			Consume(TokenType.RightParen, "Expect ')' after if condition.");

			Stmt thenBranch = Statement();
			Stmt elseBranch = null;
			if (Match(TokenType.Else))
			{
				elseBranch = Statement();
			}

			return new Stmt.If(condition, thenBranch, elseBranch);
		}

		// whileStmt -> "while" "(" expression ")" statement
		private Stmt While()
		{
			Consume(TokenType.LeftParen, "Expected '(' after 'while'.");
			Expr condition = Expression();
			Consume(TokenType.RightParen, "Expected ')' after while condition.");

			Stmt body = Statement();

			return new Stmt.While(condition, body);
		}

		// forStmt -> "for" "(" varDecl ";" expression ";" expression ")" statement
		private Stmt ForStatement()
		{
			Consume(TokenType.LeftParen, "Expected '(' after 'for'.");

			Stmt initializer;
			if (Match(TokenType.Semicolon))
			{
				initializer = null;
			}
			else if (Match(TokenType.Var))
			{
				initializer = VarDeclaration();
			}
			else
			{
				initializer = ExpressionStatement();
			}

			Expr condition = null;
			if (!Check(TokenType.Semicolon))
			{
				condition = Expression();
			}
			Consume(TokenType.Semicolon, "Expected ';' after 'for' condition.");

			Expr increment = null;
			if (!Check(TokenType.RightParen))
			{
				increment = Expression();
			}
			Consume(TokenType.RightParen, "Expected ')' after 'for' clauses.");

			Stmt body = Statement();

			if (increment != null)
			{
				Stmt incr = new Stmt.Expression(increment);
				body = InjectIncrement(body, incr);
				body = new Stmt.Block(new List<Stmt>
				{
					body,
					incr
				});
			}

			if (condition == null)
			{
				condition = new Expr.Literal(true);
			}
			body = new Stmt.While(condition, body);

			if (initializer != null)
			{
				body = new Stmt.Block(new List<Stmt>
				{
					initializer,
					body
				});
			}

			return body;
		}

		// block -> "{" declaration* "}"
		private List<Stmt> Block()
		{
			List<Stmt> statements = new List<Stmt>();

			while (!Check(TokenType.RightBrace) && !IsAtEnd())
			{
				statements.Add(Declaration());
			}

			Consume(TokenType.RightBrace, "Expect '}' after block.");

			return statements;
		}

		// printStmt -> "print" expression ";"
		private Stmt PrintStatement()
		{
			Expr value = Expression();
			Consume(TokenType.Semicolon, "Expect ';' after value.");

			return new Stmt.Print(value);
		}

		// exprStmt -> expression ";"
		private Stmt ExpressionStatement()
		{
			Expr value = Expression();
			Consume(TokenType.Semicolon, "Expect ';' after expression.");

			return new Stmt.Expression(value);
		}

		// expression -> assignment
		private Expr Expression()
		{
			return Assignment();
		}

		// assignment -> ( call "." )? IDENTIFIER "=" assignment
		//             | logicOr
		private Expr Assignment()
		{
			Expr expr = Or();

			if (Match(TokenType.Equal))
			{
				Token equals = Previous();
				Expr value = Assignment();

				if (expr is Expr.Variable)
				{
					Token name = ((Expr.Variable)expr).Name;

					return new Expr.Assign(name, value);
				}
				else if (expr is Expr.Get)
				{
					var get = (Expr.Get)expr;

					return new Expr.Set(get.Obj, get.Name, value);
				}

				Error(equals, "Invalid assignment target.");
			}

			return expr;
		}

		// logicOr -> logicAnd ( "or" logicAnd )*
		public Expr Or()
		{
			Expr expr = And();

			while (Match(TokenType.Or))
			{
				Token op = Previous();
				Expr right = And();
				expr = new Expr.Logical(expr, op, right);
			}

			return expr;
		}

		// logicAnd -> equality ( "and" equality )*
		public Expr And()
		{
			Expr expr = Equality();

			while (Match(TokenType.And))
			{
				Token op = Previous();
				Expr right = Equality();
				expr = new Expr.Logical(expr, op, right);
			}

			return expr;
		}

		// equality       → comparison ( ( "!=" | "==" ) comparison )* ;
		private Expr Equality()
		{
			Expr expr = Comparison();

			while (Match(TokenType.BangEqual, TokenType.EqualEqual))
			{
				Token op = Previous();
				Expr right = Comparison();
				expr = new Expr.Binary(expr, op, right);
			}

			return expr;
		}

		// comparison     → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
		private Expr Comparison()
		{
			Expr expr = Term();

			while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
			{
				Token op = Previous();
				Expr right = Term();
				expr = new Expr.Binary(expr, op, right);
			}

			return expr;
		}

		// term -> factor ( ( "+" | "-" ) factor )* ;
		private Expr Term()
		{
			Expr expr = Factor();

			while (Match(TokenType.Plus, TokenType.Minus))
			{
				Token op = Previous();
				Expr right = Factor();
				expr = new Expr.Binary(expr, op, right);
			}

			return expr;
		}

		// factor  -> unary ( ( "*" | "/" ) unary )* ;
		private Expr Factor()
		{
			Expr expr = Unary();

			while (Match(TokenType.Star, TokenType.Slash))
			{
				Token op = Previous();
				Expr right = Unary();
				expr = new Expr.Binary(expr, op, right);
			}

			return expr;
		}

		// unary -> ( "!" | "-" ) unary
		//        | call
		private Expr Unary()
		{
			if (Match(TokenType.Bang, TokenType.Minus))
			{
				Token op = Previous();
				Expr right = Unary();

				return new Expr.Unary(op, right);
			}

			return Call();
		}

		// call -> primary ( "(" arguments? ")" | "." IDENTIFIER )*
		private Expr Call()
		{
			Expr expr = Primary();

			while (true)
			{
				if (Match(TokenType.LeftParen))
				{
					expr = FinishCall(expr);
				}
				else if (Match(TokenType.Dot))
				{
					Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
					expr = new Expr.Get(expr, name);
				}
				else
				{
					break;
				}
			}

			return expr;
		}

		private Expr FinishCall(Expr callee)
		{
			List<Expr> arguments = new List<Expr>();
			if (!Check(TokenType.RightParen))
			{
				do
				{
					if (arguments.Count >= 255)
					{
						Error(Peek(), "Can't have more than 255 arguments.");
					}
					arguments.Add(Expression());
				} while (Match(TokenType.Comma));
			}

			Token paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");

			return new Expr.Call(callee, paren, arguments);
		}

		// arguments -> expression ( "," expression )*

		// primary        → NUMBER | STRING | "true" | "false" | "nil"
		//                | "(" expression ")"
		//                | "super" "." IDENTIFIER ;
		private Expr Primary()
		{
			if (Match(TokenType.False))
			{
				return new Expr.Literal(false);
			}

			if (Match(TokenType.True))
			{
				return new Expr.Literal(true);
			}

			if (Match(TokenType.Nil))
			{
				return new Expr.Literal(null);
			}

			if (Match(TokenType.Number, TokenType.String))
			{
				return new Expr.Literal(Previous().Literal);
			}

			if (Match(TokenType.LeftParen))
			{
				Expr expr = Expression();
				Consume(TokenType.RightParen, "Expect ')' after expression.");

				return new Expr.Grouping(expr);
			}

			if (Match(TokenType.Super))
			{
				Token keyword = Previous();
				Consume(TokenType.Dot, "Expect '.' after 'super'.");
				Token method = Consume(TokenType.Identifier, "Expect superclass method name.");

				return new Expr.Super(keyword, method);
			}

			if (Match(TokenType.This))
			{
				return new Expr.This(Previous());
			}

			if (Match(TokenType.Identifier))
			{
				return new Expr.Variable(Previous());
			}

			if (Match(TokenType.Fun))
			{
				return AnonymousFunction();
			}

			throw Error(Peek(), "Expect expression.");
		}

		// anonymous_function -> "fun" "(" parameters? ")" block
		private Expr AnonymousFunction()
		{
			Consume(TokenType.LeftParen, "Expected '(' after fun keyword");

			List<Token> parameters = new List<Token>();
			if (!Check(TokenType.RightParen))
			{
				do
				{
					if (parameters.Count >= 255)
					{
						Error(Peek(), "A function can't have more than 255 parameters.");
					}

					parameters.Add(Consume(TokenType.Identifier, "Expected parameter name."));
				} while (Match(TokenType.Comma));
			}
			Consume(TokenType.RightParen, "Expect ')' after parenthesis");

			Consume(TokenType.LeftBrace, "Expect '{' before function body");
			List<Stmt> body = Block();

			return new Expr.Function(parameters, body);
		}

		private void Synchronize()
		{
			Advance();

			while (!IsAtEnd())
			{
				if (Previous().TokenType == TokenType.Semicolon)
				{
					return;
				}

				switch (Peek().TokenType)
				{
					case TokenType.Class:
					case TokenType.Fun:
					case TokenType.Var:
					case TokenType.For:
					case TokenType.If:
					case TokenType.While:
					case TokenType.Print:
					case TokenType.Return:
						return;
				}

				Advance();
			}
		}

		private Token Consume(TokenType type, string message)
		{
			if (Check(type))
			{
				return Advance();
			}

			throw Error(Peek(), message);
		}

		private ParseError Error(Token token, string message)
		{
			Lox.Error(token, message);

			throw new ParseError();
		}

		private bool Match(params TokenType[] types)
		{
			foreach (TokenType type in types)
			{
				if (Check(type))
				{
					Advance();
					return true;
				}
			}

			return false;
		}

		private bool Check(TokenType type)
		{
			if (IsAtEnd())
			{
				return false;
			}

			return Peek().TokenType == type;
		}

		private Token Advance()
		{
			if (!IsAtEnd())
			{
				_current++;
			}

			return Previous();
		}

		private bool IsAtEnd()
		{
			return Peek().TokenType == TokenType.EOF;
		}

		private Token Peek()
		{
			return _tokens[_current];
		}

		private Token Previous()
		{
			return _tokens[_current - 1];
		}

		private Stmt InjectIncrement(Stmt stmt, Stmt increment)
		{
			if (stmt is Stmt.Block)
			{
				Stmt.Block block = stmt as Stmt.Block;
				List<Stmt> statements = block.Statements;
				List<Stmt> newStatements = new List<Stmt>();

				foreach (Stmt statement in statements)
				{
					newStatements.Add(InjectIncrement(statement, increment));
				}

				return new Stmt.Block(newStatements);
			}
			else if (stmt is Stmt.LoopControl)
			{
				if ((stmt as Stmt.LoopControl).Token.TokenType == TokenType.Break)
				{
					return stmt;
				}

				return new Stmt.Block(new List<Stmt>
				{
					increment,
					stmt
				});
			}
			else if (stmt is Stmt.If)
			{
				Stmt.If ifStmt = stmt as Stmt.If;
				Stmt thenBranch = InjectIncrement(ifStmt.ThenBranch, increment);
				Stmt elseBranch = ifStmt.ElseBranch;
				if (ifStmt.ElseBranch != null)
				{
					elseBranch = InjectIncrement(ifStmt.ElseBranch, increment);
				}

				return new Stmt.If(ifStmt.Condition, thenBranch, elseBranch);
			}

			return stmt;
		}
	}
}
