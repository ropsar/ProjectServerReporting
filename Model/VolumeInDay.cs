using System;
using System.ComponentModel.DataAnnotations;

namespace Legenda.ProjSpace.Main.Model
{

	public class VolumeInDay
	{
		[Key]
		public Guid Uid { get; set; }

		public int Status { get; set; }

		public DateTime Date { get; set; }

		public DateTime TimeStamp { get; set; }

		public Guid AssnUid { get; set; }

		public double Volume { get; set; }
	}
}