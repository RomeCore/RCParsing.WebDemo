namespace RCParsing.WebDemo
{
	public static class Examples
	{
		public static readonly Dictionary<string, (string, string)> examples;

		static Examples()
		{
			examples = new Dictionary<string, (string, string)>
			{
				["JSON"] = (
				"""
				# Skip the whitespaces
				$skip : WS ;

				# Declare the main rule
				$main : value EOF ;

				# Basic tokens
				NUMBER : /\d+(?:\.\d+)?/ ;
				STRING : /"[^"]*"/ ;
				BOOL : 'true' | 'false' ;
				NULL : 'null' ;

				# Rules
				pair : STRING ':' value ;
				array : '[' value % ',' * ']' ;
				object : '{' pair % ',' * '}' ;
				value : NUMBER | STRING | BOOL | NULL | array | object ;
				"""
				,
				"""
				{
					"name": "John Doe",
					"age": 21,
					"tags": [ "tag1", "tag2", "tag3" ]
				}
				"""
				),

				["Math expressions"] = (
				"""
				# Skip the whitespaces
				$skip : WS ;
				
				# Declare the main rule
				$main : expression EOF ;

				# Basic tokens: numbers and operators
				NUMBER : /\d+/ ;
				ADD_OP : '+' | '-' ;
				MUL_OP : '*' | '/' ;

				# The expression rules
				expression : term % ADD_OP + ;
				term : factor % MUL_OP + ;
				factor : NUMBER | '(' expression ')' ;
				"""
				,
				"""
				2 + 3 * (4 - 1)
				"""
				),

				["Simple CSV"] = (
				"""
				# Skip just spaces, keeping newlines
				$skip : SPACES ;
				
				# Declare the main rule
				$main : csv EOF ;

				# The cell token
				STRING : /[^,\n\r]+/ ;

				# Rules
				row : STRING % ',' + ;
				csv : row % NEWLINE + ;
				"""
				,
				"""
				Name,Age,City
				John,25,New York
				Alice,30,London
				Bob,22,Paris
				"""
				),

				["YAML subset"] = (
				"""
				# Skip whitespaces
				$skip : WS ;

				# Add barrier tokenizer for indentation
				$tokenizer indent:2 INDENT DEDENT NL ;

				# Basic tokens
				BOOL : 'true' | 'false' ;
				NUMBER : /\d+(?:\.\d+)?/ ;
				STRING : /"[^"]*"/ ;
				IDENTIFIER : /[a-zA-Z_][a-zA-Z0-9_]*/ ;

				# Rules for YAML structure
				object_pair : IDENTIFIER ':' value NL ;
				object_child : object_pair | object | NL ;
				object : IDENTIFIER ':' NL+ INDENT object_child+ DEDENT ;

				# Value types
				value : BOOL | NUMBER | STRING ;

				# Main rule
				$main : object_child* EOF ;
				"""
				,
				"""
				a_nested_map:
				  key: true
				  another_key: 0.9
				  another_nested_map:
				    hello: "Hello world!"
				  key_after: 999

				simple_object:
				  name: "John"
				  age: 25
				  active: true
				"""
				)
			};
		}
	}
}