namespace RCParsing.WebDemo.Pages
{
	public partial class Home
	{
		private string gv =
		"""
		flowchart TD
			Root["- "] --> Multiply["*"]
			Root --> VariableC["c"]

			Multiply --> VariableA["a"]
			Multiply --> Plus["+"]

			Plus --> VariableB["b"]
			Plus --> Literal5["5"]
		""";
	}
}