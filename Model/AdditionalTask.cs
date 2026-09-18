using System;

namespace Legenda.ProjSpace.Main.Model
{
	public class AdditionalTask
	{
		public Guid TaskGuid { get; set; }

		public Guid ParentGuid { get; set; }

		public Guid ProjectGuid { get; set; }

		public Guid AfterGuid { get; set; }

		public string Title { get; set; }

		public string Justification { get; set; }

		public string UserLogin { get; set; }

		public double UnitPrice { get; set; }

		public double AcceptVolume { get; set; }

		public double BaseVolume { get; set; }

		public DateTime TimeStamp { get; set; }

		public bool IsTotal { get; set; }

		public string Measure { get; set; }
	}
}