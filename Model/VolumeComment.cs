using System;

namespace Legenda.ProjSpace.Main.Model
{

	public class VolumeComment
	{
		public Guid GuidTask { get; set; }

		public Guid GuidProject { get; set; }

		public DateTime Date { get; set; }

		public string UserLogin { get; set; }

		public string UserName { get; set; }

		public DateTime Created { get; set; }

		public string Comment { get; set; }

		public string StageCreated { get; set; }

		public DateTime TimeStamp { get; set; }
	}
}