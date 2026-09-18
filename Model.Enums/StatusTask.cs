using System.ComponentModel;

namespace Legenda.ProjSpace.Main.Model.Enums
{

	public enum StatusTask
	{
		[Description("Новый")]
		New,
		[Description("Cохранён")]
		Saved,
		[Description("На утверждении")]
		Send,
		[Description("Отклонено")]
		Rejected,
		[Description("На публикации")]
		Publish,
		[Description("Принято")]
		Approved
	}
}