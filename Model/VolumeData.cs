using System;
using System.ComponentModel.DataAnnotations;

namespace Legenda.ProjSpace.Main.Model
{

	public class VolumeData
	{
		[Key]
		public Guid AssnUid { get; set; }

		public Guid TaskUid { get; set; }

		public Guid ProjectUid { get; set; }

		public DateTime StartDate { get; set; }

		public DateTime FinishDate { get; set; }

		public int Status { get; set; }

		public string Comments { get; set; }

		public DateTime TimeStamp { get; set; }

		public string UserLogin { get; set; }

		public string UserName { get; set; }

		public double PercentCompleted { get; set; }

        public string SenderEmail { get; set; }
    }
}