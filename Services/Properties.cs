using System.ComponentModel;

namespace Legenda.ProjSpace.Main.Services
{

	public class Properties
	{
		public enum TypeForm
		{
            /// <summary>
            /// Форма ввода объемов
            /// </summary>
            [Description("Форма ввода объемов")]
			InsertValue,
            /// <summary>
            /// Форма согласования работ
            /// </summary>
            [Description("Форма согласования работ")]
			AgreeValue
		}
	}
}