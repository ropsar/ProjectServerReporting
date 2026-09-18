using System;

namespace Legenda.ProjSpace.Main.Model
{

	public class AssnNewFinish
	{
		public Guid TaskUid;

		public Guid AssnUid;

		public Guid ResUid;

		public DateTime Finish;

		public DateTime NewFinish;

		public double Duration;

		public int Type;

		public AssnByDay.DayFact ByDays;
	}
}