using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class ProjectDb
	{
		public string Name;

		public Guid Id;

		public string OwnerName;

		public CheckOutInfo CheckOutInfo;

		public List<TaskDb> Tasks;
	}
}