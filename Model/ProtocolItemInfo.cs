using System.Collections.Generic;

namespace Legenda.ProjSpace.Main.Model
{

	public class ProtocolItemInfo
	{
		public class Order
		{
			public string Meeting;

			public string QuestionNumber;

			public string ProjectName;

			public string NameOfOrder;

			public string ConstructionObject;

			public string DueDate;

			public string Periodicity;

			public string ExpiredDates;

			public string AssignedTo;

			public List<string> AssignedToList;

			public string Body;
		}

		public string Meeting;

		public string ListName;

		public string ProtocolNumber;

		public string MeetingDate;

		public string RegularMembers;

		public List<string> RegularMembersList;

		public string InvitedMembers;

		public List<string> InvitedMembersList;

		public string Chairman;

		public List<string> ChairmanList;

		public List<string> ChairmanFIOList;

		public string ApprovingMembers;

		public List<string> ApprovingMembersList;

		public List<string> ApprovingMembersFIOList;

		public string Secretary;

		public List<string> SecretaryList;

		public List<string> SecretaryFIOList;

		public List<Order> Orders;

		public ProtocolItemInfo()
		{
			Orders = new List<Order>();
		}
	}
}