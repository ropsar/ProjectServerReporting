using System;
using System.ComponentModel.DataAnnotations;

namespace Legenda.ProjSpace.Main.Model.DB
{

	public class UserSettingsBasicPlansProject
	{
		[Key]
		public Guid ProjectUid { get; set; }

		public string BasicPlans { get; set; }
	}
}