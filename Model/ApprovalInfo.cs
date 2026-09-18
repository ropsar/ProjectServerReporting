using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class ApprovalInfo
	{
		public Guid AssnUid { get; set; }

		public Guid ResUid { get; set; }

		public List<VolumeInDay> VolumeInDays { get; set; }
	}
}