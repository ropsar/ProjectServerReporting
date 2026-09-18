using System;

namespace Legenda.ProjSpace.Main.Model
{

	public class VolumeUpdate
	{
		public double ReportedValue { get; set; }

		public int Status { get; set; } = 1;

		public DateTime? Date { get; set; }

		public Guid GuidTask { get; set; }
	}
}