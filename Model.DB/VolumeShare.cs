using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model.DB
{

	public class VolumeShare
	{
		public Guid AssnUid { get; set; }

		public VolumeData VolumeData { get; set; }

		public List<VolumeInDay> VolumeInDays { get; set; }
	}
}