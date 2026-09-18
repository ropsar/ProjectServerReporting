using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class CommentGrid
	{
		public Guid GuidTask { get; set; }

		public List<VolumeCommentsData> ListVolumeCommentsData { get; set; }
	}
}