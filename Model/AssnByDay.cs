using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class AssnByDay
	{
		public class DayFact
		{
			public DateTime Date;

			public double Fact;

			public double Plan;
		}

		public Guid AssnUid;

		public List<DayFact> DaysFacts;
	}
}