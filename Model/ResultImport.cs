using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class ResultImport
	{
		public string data { get; set; }

		public int status { get; set; } = 200;

		public string message { get; set; }

		public List<Guid> BedItem { get; set; }
	}
}