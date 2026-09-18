using System;
using System.ComponentModel.DataAnnotations;

namespace Legenda.ProjSpace.Main.Model.DB
{

	public class UserScale
	{
		[Key]
		public Guid Uid { get; set; }

		public string Login { get; set; }

		public string Height { get; set; }

		public DateTime TimeStamp { get; set; }
	}
}