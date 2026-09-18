using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class VolumeProjectTeamsInDB
	{
		public List<WorkTeamsItem> WorkTeamsItems;

		public Guid ProjectUid { get; set; }

		public string ProjectName { get; set; }

		public string ProjectUrl { get; set; }
	}
}