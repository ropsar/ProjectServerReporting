using System.ComponentModel;

namespace Legenda.ProjSpace.Main.Model.Enums
{

	public enum PeriodType
	{
		[Description("Текущая неделя")]
		CurrentWeek,
		[Description("Текущий месяц")]
		CurrentMonth,
		[Description("Текущий квартал")]
		CurrentQuarter,
		[Description("Выбрать период")]
		Custom,
		[Description("Весь период")]
		All
	}
}