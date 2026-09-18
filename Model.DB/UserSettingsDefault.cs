using System;
using System.ComponentModel.DataAnnotations;

namespace Legenda.ProjSpace.Main.Model.DB
{

	public class UserSettingsDefault
	{
		[Key]
		public Guid Uid { get; set; }

		public string UserLogin { get; set; }

		public Guid UserSettingsUid { get; set; }
	}
}