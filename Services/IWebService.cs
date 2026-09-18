using Legenda.ProjSpace.Main.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Legenda.ProjSpace.Main.Services
{

	[ServiceContract]
	public interface IWebService
	{
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        string Ping();

  //      [OperationContract]
		//[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
		//Stream ExportWord(int protocolId, string internalNameListMeetings);

		[OperationContract]
		[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
		List<ProjectDb> GetAllProjectsForUser(string loginName);

		[OperationContract]
		[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
		string GetAllAssnForUser(string loginName, Guid projUid);

		[OperationContract]
		[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
		Stream ExportProjectToExcel(CallbackArgs filter, Properties.TypeForm type);

		[OperationContract]
		[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
		void UpdatingProjectVolumeSrv();

		[OperationContract]
		[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
		void AcceptDoWorkSvc(Guid ProjUid, string CurrentUserName, string CurrentUserLogin, string CurrentUserEmail, List<Guid> Accept, List<ApprovalWorkInfo> ListAssnWork, List<PercentTask> PercentTasks, List<ResourceUid> ResourceUids);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        void AcceptDoWorkSvcNew(Guid ProjUid, string CurrentUserName, string CurrentUserLogin, string CurrentUserEmail, List<Guid> Accept, List<ApprovalWorkInfo> ListAssnWork, List<PercentTask> PercentTasks, List<ResourceUid> ResourceUids);
    }
}