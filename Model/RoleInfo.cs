using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class RoleInfo
	{
		public Guid ProjectUid { get; set; }

		public string ProjectName { get; set; }

		public string ProjectUrl { get; set; }

		public Dictionary<string, List<string>> UserAndRole { get; set; }

		public Dictionary<string, List<string>> UserAndEmail { get; set; }
	}
}