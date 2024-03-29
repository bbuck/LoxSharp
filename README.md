# What is LoxSharp?

The book [Crafting Interpreters by Bob Nystrom](http://www.craftinginterpreters.com) details the creation of a toy programming language called "Lox." The full grammar for this toy language can be found [here](http://www.craftinginterpreters.com/appendix-i.html).

This project represents my traversal through this book (yet again). I have started this project bofore and attempted non-Java port such as in Kotlin, actually in Java, in Ruby and one attempt in Go so this is my latest effort, but now in C#. For various reasons I was sidetracked before completing the book by either issues I ran into using the language, disinterest or new shinies (most common). I plan to complete the Tree Walking Interpreter portion of the book in C# (this repo) and then repurpose my Go Tree Walking repo into the Bytecode VM portion (the book has you writing it in C).

**DISCLAIMER: This is not a serious project, this is not a serious language.
This should not be used as a scripting language in a production application.**

# Contributions

Due to the nature of this as a guided project along side a book, at this time I
will not be accepting any contributions. But feel free to file issues and point
out any glaring errors you might have found in the source code as I would love
to learn and address the issues.

# Deviations

At the end of certain chapters the author provides "Challenges," some of these
challenges include adding in some feature to the language that either hasn't
been implemented by that point or will not be addressed directly by the book. So
here are the deviations I made and solutions I took to achieve them.

1. **Chapter 4**
   1. **Challenge 4**: Scanning supports block comments. To achieve this I
      simply look for an asterisk ('\*') character following a forward-slash ('/')
      ('/\*'). Upon detecting this I begin scanning a block comment. To handle
      nested block comments, when I encounter another '/\*' sequence I increment a
      depth counter and when I encounter a closing sequence ('\*/') I decrement
      this depth counter. If I encounter a close and the counter is at zero (`0`) I
      then escape scanning a block comment and continue scanning the rest of the
      program.
1. **Chapter 5**
   1. **Generating AST**: I opted not to implement the AST syntax generator in
      C# as the book has you implement in Java. My familarity with C# project
      structures and building C# applications outside of Visual Studio (this work
      was done on macOS with `dotnet`) prevented me from pursuing this. To avoid
      wasting time I could be using to continue the book learning how to build both
      applications in the same project folder I opted to just throw the AST
      generation into Ruby which made it quick and dirty. The Ruby code in
      `scripts/generate_ast.rb` is not pretty, nor clean, nor production quality
      and likely never will be. But it does the job and that's what matters for
      this project.
   1. **Challenge 3**: Reverse Polish Notation (RPN). I implemented a Reverse
      Polish Notation tree walker that stringifies expressions in the RPN format.
      This tree walker does not support statements as I opted not to continue
      supporting it when I reached that point in the book although for compilation
      reasons and posterity I continue adding the new additions to keep it inline
      with the `Expr.IVisitor` interface. Although now that I'm using version
      control this file may disappear in future commits.
1. **Chapter 7**
   1. **Challenge 2**: Support string + non-string binary operations. I
      implemented a little bit of code to automatically stringify the right-hand
      side of a binary "+" expression if the left-hand side is a string and the
      right is not.
   1. **Challenge 3**: Error on division by zero. I raise an error if you
      attempt division by zero.
1. **Chapter 8**
   1. **Challenge 1**: The REPL has been modified such that if you provide an
      expression the result is printed, if you provide a statement nothing will be
      printed (unless that statement contains `print` of course).
   1. **Challenge 2**: Variables cannot be used before initialized. I opted to
      keep track of which variables appeared in a declaration with no initializer
      (`var a;`) as "uninitialized." If you assign them a value they are removed
      from this uninitialized set. If you attempt to fetch a variable and it has
      not been defined but is in the unitialized list then you will receive an
      error about using it before it's been initialized. This chains up the
      `Environment` tree accordingly as well.
1. **Chapter 9**
   1. **Challenge 3**: Supporting `break` and `continue`. To do this I opted to
      leverage C# Exceptions. I know, Exceptions as control flow is bad, but hear
      me out. I don't _just use them as control flow_. From the beginning, first
      tokens had to be added to support these new keywords, then new keyword
      definitions. Next they had to be parsed so I added a `Stmt.LoopControl` which
      takes a token that can either be a `break` or `continue`. When the
      `Interpreter` visits a `Stmt.LoopControl` node it throws a
      `LoopControlException` with the token claiming that it was used in an invalid
      location. In `Interpreter.VisitWhileStmt` though, I wrap execution of the
      loop body in a `try` and catch this exception where I perform a
      `break`/`continue`. So in most case this error is an "unhandled exception"
      (rightfully so) but can be caught and handled in the correct location. A bit
      cheating, sure. The initial naive approach (just raising) caused an issue
      with the desurgared `for` into `while` where a `continue` would skip the
      increment logic that was tacked on to the end of the loop body in the
      desugaring process. To fix taht I inject (a very ugly solution probably) the
      increment step before any `continue`s using a recursive injection function
      that handles the statement types that matter, such as if's and nested blocks;
      however, I intentionally avoid recursing into `Stmt.While` nodes since a
      `break`/`continue` in there should affect that loop, not the current one.
1. **Chapter 10**
   1. **Challenge 2**: I added support for Anonymous functions. They're
      considered expressions. The program `fun () {};` is "valid" (IMO) but would
      break because the function statement attempts to parse this and fails with no
      name. Since the parser will distinguish whether it's parsing a method or a
      function I opted to allow the statement parse for a function notice no name
      and parse as an anonymous function expression. This seems to allow it all to
      work out correctly.
1. **Chapter 11**
   1. **Challenge 3**: I extended the Resolver to track usage of variables in
      addition to declaration/definition so that when scopes are cleared we can
      report errors for unused variables.
1. **Chapter 12**
   1. **Challenge 1**: I implemented static methods via the `"class" function`
      syntax. Per the challenge in the book, `LoxClass` is a subclass of
      `LoxInstance` and it creates a "meta class" for itself when it's instantiated
      where the static methods are normal methods on the meta class. Being a
      subclass of `LoxInstance` is magically fits in where the instances already
      did (properties/fields and method calls).
   1. **Challenge 2**: I implemented getters along side other functions. Getters
      are special methods with no parameter list and act like normal property get
      statements but will execute the function body. Think computed properties.
1. **Chapter 13**
   1. **Challenge 1**: I opted to add "Mixins," or at least a hasty
      interpretation of them. You can use `<= Class, Class, Class` syntax after
      a possible superclass to "add logic from these classes" into the new class.
      This allows re-use in a variety of ways. This differs slightly from
      the way inheritance is implemented (only slightly) by disallowing the use
      of super in mixin classes (if they have a superclass this is an error).
