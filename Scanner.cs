using System;
using System.Collections.Generic;

namespace LoxSharp
{
	class Scanner
	{
		private static readonly Dictionary<string, TokenType> _Keywords;

		static Scanner()
		{
			_Keywords = new Dictionary<string, TokenType>
			{
				["and"] = TokenType.And,
				["class"] = TokenType.Class,
				["else"] = TokenType.Else,
				["false"] = TokenType.False,
				["for"] = TokenType.For,
				["fun"] = TokenType.Fun,
				["if"] = TokenType.If,
				["nil"] = TokenType.Nil,
				["or"] = TokenType.Or,
				["print"] = TokenType.Print,
				["return"] = TokenType.Return,
				["super"] = TokenType.Super,
				["this"] = TokenType.This,
				["true"] = TokenType.True,
				["var"] = TokenType.Var,
				["while"] = TokenType.While,
				["break"] = TokenType.Break,
				["continue"] = TokenType.Continue
			};
		}

		private readonly string _source;
		private readonly List<Token> _tokens;

		private int _start = 0;
		private int _current = 0;
		private int _line = 1;

		public Scanner(string source)
		{
			this._source = source;
			this._tokens = new List<Token>();
		}

		public List<Token> ScanTokens()
		{
			while (!IsAtEnd())
			{
				_start = _current;
				ScanToken();
			}

			AddToken(TokenType.EOF);

			return _tokens;
		}

		private bool IsAtEnd()
		{
			return _current >= _source.Length;
		}

		private void ScanToken()
		{
			char c = Advance();

			switch (c)
			{
				case '(':
					AddToken(TokenType.LeftParen);
					break;
				case ')':
					AddToken(TokenType.RightParen);
					break;
				case '{':
					AddToken(TokenType.LeftBrace);
					break;
				case '}':
					AddToken(TokenType.RightBrace);
					break;
				case ',':
					AddToken(TokenType.Comma);
					break;
				case '.':
					AddToken(TokenType.Dot);
					break;
				case '-':
					AddToken(TokenType.Minus);
					break;
				case '+':
					AddToken(TokenType.Plus);
					break;
				case ';':
					AddToken(TokenType.Semicolon);
					break;
				case '*':
					AddToken(TokenType.Star);
					break;
				case '!':
					AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
					break;
				case '=':
					AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
					break;
				case '<':
					AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
					break;
				case '>':
					AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
					break;
				case '/':
					if (Match('/'))
					{
						while (Peek() != '\n' && !IsAtEnd())
						{
							Advance();
						}
					}
					else if (Match('*'))
					{
						BlockComment();
					}
					else
					{
						AddToken(TokenType.Slash);
					}
					break;
				case ' ':
				case '\r':
				case '\t':
					// ignore whitespace
					break;
				case '\n':
					_line++;
					break;
				case '"':
					String();
					break;
				default:
					if (IsDigit(c))
					{
						Number();
					}
					else if (IsAlpha(c))
					{
						Identifier();
					}
					else
					{
						Lox.Error(_line, string.Format("Unexpected character '{0}'.", c));
					}
					break;
			}
		}

		private char Peek()
		{
			if (IsAtEnd())
			{
				return '\0';
			}

			return _source[_current];
		}

		private char PeekNext()
		{
			if (_current + 1 >= _source.Length)
			{
				return '\0';
			}

			return _source[_current + 1];
		}

		private bool Match(char expected)
		{
			if (IsAtEnd())
			{
				return false;
			}

			if (Peek() != expected)
			{
				return false;
			}

			_current++;

			return true;
		}

		private char Advance()
		{
			_current++;
			return _source[_current - 1];
		}

		private void AddToken(TokenType type)
		{
			AddToken(type, null);
		}

		private void AddToken(TokenType type, object literal)
		{
			string text = _source[_start.._current];
			_tokens.Add(new Token(type, text, literal, _line));
		}

		private bool IsDigit(char c)
		{
			return c >= '0' && c <= '9';
		}

		private bool IsAlpha(char c)
		{
			return (c >= 'a' && c <= 'z') ||
				(c >= 'A' && c <= 'Z') ||
				c == '_';
		}

		private bool IsAlphanumeric(char c)
		{
			return IsDigit(c) || IsAlpha(c);
		}

		private void BlockComment()
		{
			int nested = 0;
			while (!IsAtEnd())
			{
				if (Match('*'))
				{
					if (Peek() == '/')
					{
						Advance();
						if (nested == 0)
						{
							break;
						}
						nested--;
					}
				}
				else if (Match('/'))
				{
					if (Peek() == '*')
					{
						nested++;
					}
				}
				else if (Match('\n'))
				{
					_line++;
				}
				Advance();
			}
		}

		private void Identifier()
		{
			while (IsAlphanumeric(Peek()))
			{
				Advance();
			}

			string text = _source[_start.._current];
			TokenType type;
			if (_Keywords.ContainsKey(text))
			{
				type = _Keywords[text];
			}
			else
			{
				type = TokenType.Identifier;
			}
			AddToken(type);
		}

		private void Number()
		{
			while (IsDigit(Peek()))
			{
				Advance();
			}

			// look for fractional part
			if (Peek() == '.' && IsDigit(PeekNext()))
			{
				// consume the dot
				Advance();

				while (IsDigit(Peek()))
				{
					Advance();
				}
			}

			AddToken(TokenType.Number, Double.Parse(_source[_start.._current]));
		}

		private void String()
		{
			while (Peek() != '"' && !IsAtEnd())
			{
				if (Peek() == '\n')
				{
					_line++;
				}
				Advance();
			}

			if (IsAtEnd())
			{
				Lox.Error(_line, "Unterminated string.");
				return;
			}

			// the closing "
			Advance();

			// Trim surrounding quotes
			int len = _current - _start - 2;
			string value = _source.Substring(_start + 1, len);
			AddToken(TokenType.String, value);
		}
	}
}
