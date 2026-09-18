using System;

namespace Legenda.ProjSpace.Main.Model
{

	public class ExportExcelKS2
	{
		public Guid GuidTask { get; set; }

		public Guid ParentGUID { get; set; }

		public int Outlevel { get; set; }

		public int TaskIndex { get; set; }

		public string Codifier { get; set; }

		public string TaskName { get; set; }

		public string Measure { get; set; }

		public bool TaskIsSummary { get; set; }

		public double Price { get; set; }

		public double Volume { get; set; }

		public double ApprovedThisMonth { get; set; }
	}
}