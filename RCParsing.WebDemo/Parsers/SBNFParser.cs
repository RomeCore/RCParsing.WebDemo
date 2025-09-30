using System.Data;
using System.Diagnostics;
using RCParsing.Building;
using RCParsing.Building.ParserRules;
using RCParsing.Building.TokenPatterns;
using RCParsing.ParserRules;
using RCParsing.TokenPatterns;

namespace RCParsing.WebDemo.Parsers
{
	public static class SBNFParser
	{
		static Parser parser;

		private class MainRuleDefinition
		{
			public string? Name { get; set; }
		}

		private class SkipRuleDefinition
		{
			public ParserSkippingStrategy SkipStrategy { get; set; }
		}

		private class RuleDefinition
		{
			public string Name { get; set; } = null!;
		}

		private class Quantifier
		{
			public int Min { get; set; }
			public int Max { get; set; } = -1;
		}

		static SBNFParser()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(b => b.Token("skip"));

			builder.CreateToken("empty")
				.Chars(c => false, 0, 0);

			builder.CreateToken("skip")
				.Choice(
					b => b.Whitespaces(),
					b => b.Literal('#').ZeroOrMoreChars(c => c != '\n' && c != '\r')
				);

			builder.CreateToken("token_name")
				.Identifier(char.IsUpper, c => char.IsLetterOrDigit(c) || c == '_')

				.Transform(v => v.Text);

			builder.CreateToken("rule_name")
				.Identifier(char.IsLower, c => char.IsLetterOrDigit(c) || c == '_')

				.Transform(v => v.Text);

			builder.CreateRule("token_block")
				.Literal(':')
				.Rule("token_expr")
				.Literal(';')
				
				.TransformSelect(index: 1);

			builder.CreateRule("rule_block")
				.Literal(':')
				.Rule("rule_expr")
				.Literal(';')

				.TransformSelect(index: 1);

			var escapes = new Dictionary<string, string>
			{
				["\\n"] = "\n",
				["\\t"] = "\t",
				["\\r"] = "\r",
				["\\\'"] = "\'",
				["\\\\"] = "\\",
			};

			var forbids = new HashSet<string> { "\\", "\'", "\n", "\r" };

			builder.CreateToken("string_literal")
				.Between(
					b => b.Literal('\''),
					b => b.EscapedText(escapes, forbids),
					b => b.Literal('\'')
				);

			builder.CreateRule("quantifier")
				.Choice(
					b => b.Literal("?").Transform(_ => new Quantifier { Min = 0, Max = 1 }),
					b => b.Literal("*").Transform(_ => new Quantifier { Min = 0, Max = -1 }),
					b => b.Literal("+").Transform(_ => new Quantifier { Min = 1, Max = -1 }),
					
					b => b.Literal("{").Number<int>(signed: false).Literal("..").Number<int>(signed: false).Literal("}")
						.Transform(v => new Quantifier { Min = v.GetIntermediateValue<int>(index: 1), Max = v.GetIntermediateValue<int>(index: 3) }),
					
					b => b.Literal("{").Number<int>(signed: false).Literal("..").Literal("}")
						.Transform(v => new Quantifier { Min = v.GetIntermediateValue<int>(index: 1), Max = -1 }),
					
					b => b.Literal("{").Number<int>(signed: false).Literal("}")
						.Transform(v => new Quantifier { Min = v.GetIntermediateValue<int>(index: 1), Max = v.GetIntermediateValue<int>(index: 1) })
				);

			escapes = new Dictionary<string, string>
			{
				["\\/"] = "/",
			};

			forbids = new HashSet<string> { "/", "\n", "\r" };

			builder.CreateToken("regex")
				.Between(
					b => b.Literal('/'),
					b => b.EscapedText(escapes, forbids),
					b => b.Literal('/')
				);

			// Tokens

			builder.CreateRule("token_literal_choice")
				.OneOrMoreSeparated(b => b.Token("string_literal"), s => s.Literal('|'),
					includeSeparatorsInResult: false)

				.Transform(v =>
				{
					var literals = v.SelectArray<string>();

					foreach (var literal in literals)
						if (!char.IsLetterOrDigit(literal[^1]) && literal[^1] != '_')
						{
							if (literals.Length == 1)
								return new LiteralTokenPattern(literals[0]);
							else
								return new LiteralChoiceTokenPattern(literals);
						}

					if (literals.Length == 1)
						return KeywordTokenPattern.UnicodeKeyword(literals[0]);
					else
						return KeywordChoiceTokenPattern.UnicodeKeywordChoice(literals);
				});

			builder.CreateRule("token_identifier")
				.Literal('$')
				.Keyword("identifier")

				.Transform(v =>
				{
					return IdentifierTokenPattern.UnicodeIdentifier();
				});

			builder.CreateRule("token_keyword")
				.Literal('$')
				.Keyword("keyword")
				.Token("string_literal")

				.Transform(v =>
				{
					var keyword = v.GetValue<string>(index: 2);
					return KeywordTokenPattern.UnicodeKeyword(keyword);
				});

			builder.CreateRule("token_keyword_choice")
				.Literal('$')
				.Keyword("keyword")
				.Keyword("choice")
				.OneOrMoreSeparated(b => b.Token("string_literal"), s => s.Literal('|'),
					includeSeparatorsInResult: false)

				.Transform(v =>
				{
					var keywords = v.SelectValues<string>(index: 3);
					return KeywordChoiceTokenPattern.UnicodeKeywordChoice(keywords);
				});

			builder.CreateRule("token_number")
				.Literal('$')
				.Keyword("number")
				.KeywordChoice("int", "float", "double")
				
				.Transform(v =>
				{
					return v.GetValue<string>(index: 2) switch
					{
						"int" => new NumberTokenPattern(NumberType.Integer, NumberFlags.Integer),
						"float" => new NumberTokenPattern(NumberType.Float, NumberFlags.StrictFloat),
						"double" => new NumberTokenPattern(NumberType.Double, NumberFlags.StrictFloat),
						_ => throw new Exception(),
					};
				});

			builder.CreateRule("token_regex")
				.Token("regex")

				.Transform(v =>
				{
					var regex = v.GetIntermediateValue<string>();
					return new RegexTokenPattern(regex);
				});

			builder.CreateRule("token_term")
				.Choice(
					b => b.Literal('(').Rule("token_expr").Literal(')').TransformSelect(index: 1),
					b => b.Token("token_name"),
					b => b.Rule("token_identifier"),
					b => b.Rule("token_number"),
					b => b.Rule("token_regex"),
					b => b.Rule("token_literal_choice")
				)
				
				.Transform(v =>
				{
					var value = v.GetValue(index: 0);

					if (value is string tokenName)
						return new Utils.Or<string, BuildableTokenPattern>(tokenName);
					if (value is TokenPattern tokenLeaf)
						return new Utils.Or<string, BuildableTokenPattern>(
							new BuildableLeafTokenPattern { TokenPattern = tokenLeaf });
					if (value is Utils.Or<string, BuildableTokenPattern> res)
						return res;

					throw new Exception();
				});

			builder.CreateRule("token_separatedrepeat")
				.Rule("token_term")
				.ZeroOrMore(b => b
					.Literal('%').Rule("token_term").Rule("quantifier"))

				.Transform(v =>
				{
					var token = v.GetValue<Utils.Or<string, BuildableTokenPattern>>(index: 0);
					var sepParent = v[1];
					
					foreach (var sep in sepParent)
					{
						var sepToken = sep.GetValue<Utils.Or<string, BuildableTokenPattern>>(index: 1);
						var quantifier = sep.GetValue<Quantifier>(index: 2);

						token = new BuildableSeparatedRepeatTokenPattern
						{
							Child = token,
							Separator = sepToken,
							MinCount = quantifier.Min,
							MaxCount = quantifier.Max
						};
					}

					return token;
				});

			builder.CreateRule("token_quantifier")
				.Rule("token_separatedrepeat")
				.Optional(b => b.Rule("quantifier"))

				.Transform(v =>
				{
					var value = v.GetValue<Utils.Or<string, BuildableTokenPattern>>(index: 0);
					var quantifier = v.TryGetValue<Quantifier>(index: 1);

					if (quantifier != null)
					{
						if (quantifier.Min == 0 && quantifier.Max == 1)
							value = new BuildableOptionalTokenPattern { Child = value };
						else
							value = new BuildableRepeatTokenPattern { Child = value, MinCount = quantifier.Min, MaxCount = quantifier.Max };
					}

					return value;
				});

			builder.CreateRule("token_sequence")
				.OneOrMore(b => b.Rule("token_quantifier"))

				.Transform(v =>
				{
					var values = v.SelectArray<Utils.Or<string, BuildableTokenPattern>>();

					if (values.Length == 1)
						return values[0];

					var sequence = new BuildableSequenceTokenPattern();
					sequence.Elements.AddRange(values);
					return new Utils.Or<string, BuildableTokenPattern>(sequence);
				});

			builder.CreateRule("token_choice")
				.OneOrMoreSeparated(b => b.Rule("token_sequence"), s => s.Literal('|'),
					includeSeparatorsInResult: false)

				.Transform(v =>
				{
					var values = v.SelectArray<Utils.Or<string, BuildableTokenPattern>>();

					if (values.Length == 1)
						return values[0];

					var sequence = new BuildableChoiceTokenPattern();
					sequence.Choices.AddRange(values);
					return new Utils.Or<string, BuildableTokenPattern>(sequence);
				});

			builder.CreateRule("token_expr")
				.Rule("token_choice");

			// Rules

			builder.CreateRule("rule_term")
				.Choice(
					b => b.Literal('(').Rule("rule_expr").Literal(')').TransformSelect(index: 1),
					b => b.Token("rule_name"),
					b => b.Rule("token_term")
				)

				.Transform(v =>
				{
					var value = v.GetValue(index: 0);

					if (value is string ruleName)
						return new Utils.Or<string, BuildableParserRule>(ruleName);
					if (value is Utils.Or<string, BuildableTokenPattern> token)
						return new Utils.Or<string, BuildableParserRule>(
							new BuildableTokenParserRule { Child = token });
					if (value is Utils.Or<string, BuildableParserRule> res)
						return res;

					throw new Exception();
				});

			builder.CreateRule("rule_separatedrepeat")
				.Rule("rule_term")
				.ZeroOrMore(b => b
					.Literal('%').Rule("rule_term").Rule("quantifier"))

				.Transform(v =>
				{
					var rule = v.GetValue<Utils.Or<string, BuildableParserRule>>(index: 0);
					var sepParent = v[1];

					foreach (var sep in sepParent)
					{
						var sepRule = sep.GetValue<Utils.Or<string, BuildableParserRule>>(index: 1);
						var quantifier = sep.GetValue<Quantifier>(index: 2);

						rule = new BuildableSeparatedRepeatParserRule
						{
							Child = rule,
							Separator = sepRule,
							MinCount = quantifier.Min,
							MaxCount = quantifier.Max
						};
					}

					return rule;
				});

			builder.CreateRule("rule_quantifier")
				.Rule("rule_separatedrepeat")
				.Optional(b => b.Rule("quantifier"))

				.Transform(v =>
				{
					var value = v.GetValue<Utils.Or<string, BuildableParserRule>>(index: 0);
					var quantifier = v.TryGetValue<Quantifier>(index: 1);

					if (quantifier != null)
					{
						if (quantifier.Min == 0 && quantifier.Max == 1)
							value = new BuildableOptionalParserRule { Child = value };
						else
							value = new BuildableRepeatParserRule { Child = value, MinCount = quantifier.Min, MaxCount = quantifier.Max };
					}

					return value;
				});

			builder.CreateRule("rule_sequence")
				.OneOrMore(b => b.Rule("rule_quantifier"))

				.Transform(v =>
				{
					var values = v.SelectArray<Utils.Or<string, BuildableParserRule>>();

					if (values.Length == 1)
						return values[0];

					var sequence = new BuildableSequenceParserRule();
					sequence.Elements.AddRange(values);
					return new Utils.Or<string, BuildableParserRule>(sequence);
				});

			builder.CreateRule("rule_choice")
				.OneOrMoreSeparated(b => b.Rule("rule_sequence"), s => s.Literal('|'),
					includeSeparatorsInResult: false)

				.Transform(v =>
				{
					var values = v.SelectArray<Utils.Or<string, BuildableParserRule>>();

					if (values.Length == 1)
						return values[0];

					var sequence = new BuildableChoiceParserRule();
					sequence.Choices.AddRange(values);
					return new Utils.Or<string, BuildableParserRule>(sequence);
				});

			builder.CreateRule("rule_expr")
				.Rule("rule_choice");

			// Main rules

			builder.CreateRule("skip_strategy")
				.Choice(
					b => b.Keywords("before", "greedy")	.Transform(_ => ParserSkippingStrategy.SkipBeforeParsingGreedy),
					b => b.Keywords("before", "lazy")	.Transform(_ => ParserSkippingStrategy.SkipBeforeParsingLazy),
					b => b.Keywords("before")			.Transform(_ => ParserSkippingStrategy.SkipBeforeParsing),
					b => b.Keywords("after", "greedy")	.Transform(_ => ParserSkippingStrategy.TryParseThenSkipGreedy),
					b => b.Keywords("after", "lazy")	.Transform(_ => ParserSkippingStrategy.TryParseThenSkipLazy),
					b => b.Keywords("after")			.Transform(_ => ParserSkippingStrategy.TryParseThenSkip),
					b => b.Token("empty")				.Transform(_ => ParserSkippingStrategy.SkipBeforeParsing)
				);

			builder.CreateRule("rule_def")
				.Choice(
					b => b.Literal("$").Keyword("main").Optional(b => b.Token("rule_name"))
						.Transform(v => new MainRuleDefinition { Name = v.TryGetValue<string>(index: 2) }),
					b => b.Literal("$").Keyword("skip").Rule("skip_strategy")
						.Transform(v => new SkipRuleDefinition { SkipStrategy = v.GetValue<ParserSkippingStrategy>(index: 2) }),
					b => b.Token("rule_name")
						.Transform(v => new RuleDefinition { Name = v.Text })
				)
				.Rule("rule_block")

				.Transform(v =>
				{
					var builder = v.GetParsingParameter<ParserBuilder>();
					var def = v.GetValue(index: 0);
					var rule = v.GetValue<Utils.Or<string, BuildableParserRule>>(index: 1);

					if (def is MainRuleDefinition mainDef)
					{
						var ruleBuilder = mainDef.Name != null
							? builder.CreateMainRule(mainDef.Name)
							: builder.CreateMainRule();
						ruleBuilder.BuildingRule = rule;
					}

					if (def is SkipRuleDefinition skipDef)
					{
						builder.Settings.Skip(b => b.BuildingRule = rule, skipDef.SkipStrategy);
					}

					if (def is RuleDefinition ruleDef)
					{
						var ruleBuilder = builder.CreateRule(ruleDef.Name);
						ruleBuilder.BuildingRule = rule;
					}

					return null;
				});

			builder.CreateRule("token_def")
				.Choice(
					b => b.Token("token_name")
				)
				.Rule("token_block")
				
				.Transform(v =>
				{
					var builder = v.GetParsingParameter<ParserBuilder>();
					var name = v.GetValue<string>(index: 0);
					var token = v.GetValue<Utils.Or<string, BuildableTokenPattern>>(index: 1);

					var tokenBuilder = builder.CreateToken(name);
					tokenBuilder.BuildingPattern = token;

					return null;
				});

			builder.CreateMainRule()
				.ZeroOrMore(b => b.Choice(
					b => b.Rule("token_def"),
					b => b.Rule("rule_def")
				)
				.Recovery(r => r.SkipAfter(b => b.Literal(';')))
				)
				.EOF()
				.RecoveryLast(r => r.FindNext())
				
				.TransformSelect(index: 0);

			parser = builder.Build();
		}

		public static void ImportFrom(string grammar, ParserBuilder builder)
		{
			var ast = parser.Parse(grammar, parameter: builder);
			var exception = new ParsingException(ast.Context);
			if (exception.Groups.RelevantGroups.Count > 0)
				throw exception;

			_ = ast.Value; // Trigger the transformation of the AST to a concrete object.
			
			builder.CreateToken("EOF")
				.EOF();
			builder.CreateToken("WS")
				.Whitespaces();
			builder.CreateToken("SPACES")
				.Spaces();
			builder.CreateToken("NEWLINE")
				.Newline();
		}

		public static Parser ParseGrammar(string grammar)
		{
			var builder = new ParserBuilder();
			ImportFrom(grammar, builder);
			return builder.Build();
		}
	}
}