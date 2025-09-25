using System.Diagnostics;

namespace RCParsing.WebDemo.Parsers
{
	public static class SBNFParser
	{
		static Parser parser;

		static SBNFParser()
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces();

			builder.CreateToken("token_name")
				.Identifier(char.IsLower, c => char.IsLetterOrDigit(c) || c == '_');

			builder.CreateToken("rule_name")
				.Identifier(char.IsUpper, c => char.IsLetterOrDigit(c) || c == '_');

			builder.CreateRule("token_def")
				.Token("token_name")
				.Literal(':')
				.OneOrMore(b => b.Rule("token_expr"))
				.Literal(';');

			builder.CreateRule("rule_def")
				.Token("rule_name")
				.Literal(':')
				.OneOrMore(b => b.Rule("rule_expr"))
				.Literal(';');

			// Tokens

			builder.CreateToken("string_literal")
				.Between(
					b => b.Literal('\''),
					b => b.EscapedTextDoubleChars('\''),
					b => b.Literal('\'')
				);

			builder.CreateRule("token_literal")
				.Token("string_literal");

			builder.CreateRule("token_literal_choice")
				.OneOrMoreSeparated(b => b.Token("string_literal"), s => s.Literal('|'));

			builder.CreateRule("token_identifier")
				.Literal('$')
				.Keyword("identifier");

			builder.CreateRule("token_keyword")
				.Literal('$')
				.Keyword("keyword")
				.Token("string_literal");

			builder.CreateRule("token_keyword_choice")
				.Literal('$')
				.Keyword("keyword")
				.Keyword("choice")
				.OneOrMoreSeparated(b => b.Token("string_literal"), s => s.Literal('|'));

			builder.CreateRule("token_number")
				.Literal('$')
				.Keyword("number")
				.KeywordChoice("int", "float", "double");

			builder.CreateRule("token_regex")
				.Literal('$')
				.Keyword("regex")
				.Token("string_literal");

			builder.CreateRule("token_term")
				.Choice(
					b => b.Rule("token_literal_choice"),
					b => b.Rule("token_literal"),
					b => b.Rule("token_identifier"),
					b => b.Rule("token_keyword_choice"),
					b => b.Rule("token_keyword"),
					b => b.Rule("token_number"),
					b => b.Rule("token_regex")
				);

			builder.CreateRule("token_sequence")
				.OneOrMore(b => b.Rule("token_term"));
		}
	}
}