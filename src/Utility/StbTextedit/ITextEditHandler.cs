namespace StbTextEditSharp
{
	public interface ITextEditHandler
	{
		bool InputSet { get; set; }

		string Text { get; set; }

		int Length { get; }

		TextEditRow LayoutRow(int startIndex);
		float GetWidth(int index);

		void AfterInput();
	}
}