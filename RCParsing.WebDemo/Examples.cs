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
				$skip : WS ;
				$main : value EOF ;

				NUMBER : /\d+(?:\.\d+)?/ ;
				STRING : /"[^"]*"/ ;
				BOOL : 'true' | 'false' ;
				NULL : 'null' ;

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
				$skip : WS ;
				$main : expression EOF ;

				NUMBER : /\d+/ ;
				ADD_OP : '+' | '-' ;
				MUL_OP : '*' | '/' ;

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
				$skip : SPACES ;
				$main : csv EOF ;

				STRING : /[^,\n\r]+/ ;

				row : STRING % ',' + ;
				csv : row % ('\r'? '\n') + ;
				"""
				,
				"""
				Name,Age,City
				John,25,New York
				Alice,30,London
				Bob,22,Paris
				"""
				)
			};
		}
	}
}