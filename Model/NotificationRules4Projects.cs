using System.Collections.Generic;
using System.Linq;

namespace Legenda.ProjSpace.Main.Model
{

	public class NotificationRules4Projects
	{
		public string Title;

		public string guid;

		public List<string> Notifications;

		protected List<NotificationSettings> NotificationsList { get; }

		public NotificationRules4Projects()
		{
		}

		public NotificationRules4Projects(string title, string guidnew, List<string> notifications, List<NotificationSettings> notificationslist)
		{
			Title = title;
			guid = guidnew;
			Notifications = notifications;
			NotificationsList = notificationslist;
		}

		public NotificationSettings GetThisNotificationSettings(string typeevent)
		{
			return NotificationsList.FirstOrDefault((NotificationSettings p) => p.TypeEvent == typeevent);
		}

		public NotificationSettings GetThisNotificationSettings(string stageName, string typeevent)
		{
			return NotificationsList.FirstOrDefault((NotificationSettings p) => p.Stage == stageName && p.TypeEvent == typeevent);
		}
	}
}