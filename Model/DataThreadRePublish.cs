using System;
using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class DataThreadRePublish
	{
		public Guid ProjUid;

		public string CurrentUserName;

		public string CurrentUserLogin;

		public string CurrentUserEmail;

		public List<Guid> Accept;

		public List<ApprovalWorkInfo> ListAssnWork;

		public List<PercentTask> PercentTasks;

		public List<ResourceUid> ResourceUids;
	}
}