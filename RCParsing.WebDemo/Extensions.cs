using System.Text;

namespace RCParsing.WebDemo
{
	public static class Extensions
	{
		public static string Indent(this string str, string indentString = "\t", bool addIndentToFirstLine = true)
		{
			if (string.IsNullOrEmpty(str))
			{
				return str;
			}

			string[] array = str.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
			StringBuilder stringBuilder = new();
			for (int i = 0; i < array.Length; i++)
			{
				if (i > 0 || addIndentToFirstLine)
				{
					stringBuilder.Append(indentString);
				}

				stringBuilder.Append(array[i]);
				if (i < array.Length - 1)
				{
					stringBuilder.AppendLine();
				}
			}

			return stringBuilder.ToString();
		}
	}
}