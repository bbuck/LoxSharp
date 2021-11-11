#!/usr/bin/env ruby
# rubocop:disable all
# frozen_string_literal: true

require 'pathname'
require 'pp'

if ARGV.length != 1
  puts 'Usage: generate_ast [output_dir]'
  exit(64)
end

EXPRS = '
Binary   : Expr left, Token op, Expr right
Call     : Expr callee, Token paren, List<Expr> arguments
Function : List<Token> parameters, List<Stmt> body
Logical  : Expr left, Token op, Expr right
Grouping : Expr expression
Literal  : object value
Unary    : Token op, Expr right
Variable : Token name
Assign   : Token name, Expr value
Get      : Expr obj, Token name
'.split("\n").map(&:strip).reject(&:empty?)

STMTS = '
Expression  : Expr expr
Function    : Token name, List<Token> parameters, List<Stmt> body
Class       : Token name, List<Stmt.Function> methods
Return      : Token keyword, Expr value
Print       : Expr expr
Var         : Token name, Expr initializer
Block       : List<Stmt> statements
If          : Expr condition, Stmt thenBranch, Stmt elseBranch
While       : Expr condition, Stmt body
LoopControl : Token token
'.split("\n").map(&:strip).reject(&:empty?)

def capitalize(str)
  str[0].upcase + str[1..-1]
end

def define_type(base_name, class_desc)
  name, fields = class_desc.split(':').map(&:strip)
  type = [
    "public class #{name} : #{base_name}",
    '{',
  ]
  field_descs = fields.split(',').map(&:strip)
  field_descs.each do |field_desc|
    ftype, fname = field_desc.split(' ')
    type << "\tpublic #{ftype} #{capitalize(fname)} { get; }"
  end
  type << ''
  type << "\tpublic #{name}(#{fields})"
  type << "\t{"
  field_descs.each do |field_desc|
    _, fname = field_desc.split(' ')
    type << "\t\tthis.#{capitalize(fname)} = #{fname};"
  end
  type << "\t}"
  type << ''
  type << "\tpublic override R Accept<R>(IVisitor<R> visitor)"
  type << "\t{"
  type << "\t\treturn visitor.Visit#{name}#{base_name}(this);"
  type << "\t}"
  type << '}'
  type << ''

  type.map { |x| "\t\t" + x }
end

def define_visitor(base_name, class_defs)
  visitor = [
    'public interface IVisitor<R>',
    '{',
  ]

  class_defs.each do |class_desc|
    name = class_desc.split(':').first.strip
    visitor << "\tR Visit#{name}#{base_name}(#{name} #{base_name.downcase});"
  end
  visitor << "}"
  visitor << ""
  visitor.map { |x| "\t\t" + x }
end

def gen_ast(base_name, class_defs, output_dir)
  csharp = [
    '// This file is auto-generated, do not modify',
    '',
    'using System.Collections.Generic;',
    '',
    'namespace LoxSharp',
    '{',
    "\tabstract class #{base_name}",
    "\t{",
    "\t\tpublic abstract R Accept<R>(IVisitor<R> visitor);",
    '',
  ]

  csharp += define_visitor(base_name, class_defs)

  class_defs.each do |class_desc|
    csharp += define_type(base_name, class_desc)
  end

  csharp.pop
  csharp << "\t}"
  csharp << '}'
  csharp = csharp.map(&:rstrip).join("\n")

  file_path = output_dir.join("#{base_name}.cs")
  File.open(file_path, 'w') do |file|
    file.puts csharp
  end
end

out = Pathname.new(ARGV.first)

gen_ast('Expr', EXPRS, out)
gen_ast('Stmt', STMTS, out)

puts 'Done'
