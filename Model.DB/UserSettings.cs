using System;
using System.ComponentModel.DataAnnotations;

namespace Legenda.ProjSpace.Main.Model.DB
{

	public class UserSettings
	{
		[Key]
		public Guid Uid { get; set; }

		public string Name { get; set; }

		public int Type { get; set; }

		public DateTime TimeStamp { get; set; }

		public string UserLogin { get; set; }

		public string UserName { get; set; }

		public Guid? FilterProjectUid { get; set; }

		public string FilterTaskUids { get; set; }

		public string FilterTypeWork { get; set; }

		public int? FilterBasePlan { get; set; }

		public string TimeScale { get; set; }

		public string RangeStart { get; set; }

		public int Layout { get; set; }

		public string Spreading { get; set; }

		public string Fields { get; set; }

		public string Stages { get; set; }

		public string Bloks { get; set; }

        public string Bloks2 { get; set; }

        public string Capture { get; set; }
    }
}