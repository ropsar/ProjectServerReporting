using Legenda.ProjSpace.Main.Json;
using Legenda.ProjSpace.Main.Logging;
using Legenda.ProjSpace.Main.Model;
//using Legenda.ProjSpace.Core.Services.Logging;
using System;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace Legenda.ProjSpace.Main.Services
{

    public static class PsService
    {
        public static void CallGetWebService(string webUrl, string url)
        {
            WebRequest webRequest = WebRequest.Create(webUrl + url);
            webRequest.Method = "GET";
            webRequest.Credentials = CredentialCache.DefaultCredentials;
            webRequest.GetResponse();
        }

        public static string CallExecuteAcceptDoWork(string webUrl, DataThreadRePublish info)
        {
            Logger.Log("CallExecuteAcceptDoWork(): start");
            if (webUrl == "")
            {
                return "false";
            }
            Uri uri = new Uri($"{webUrl}/_vti_bin/Legenda/ProjSpace/WebService.svc/AcceptDoWorkSvc");
            return CallService(uri.AbsoluteUri, info.ToJson());
        }

        public static string CallExecuteAcceptDoWorkNew(string webUrl, DataThreadRePublish info)
        {
            Logger.Log("CallExecuteAcceptDoWork(): start");
            if (webUrl == "")
            {
                return "false";
            }
            Uri uri = new Uri($"{webUrl}/_vti_bin/Legenda/ProjSpace/WebService.svc/AcceptDoWorkSvcNew");
            return CallService(uri.AbsoluteUri, info.ToJson());
        }

        private static string CallService(string serviceUrl, string json)
        {
            Logger.Log("CallService(): старт запроса по адресу " + serviceUrl);
            using (WindowsImpersonationContext windowsImpersonationContext = WindowsIdentity.Impersonate(IntPtr.Zero))
            {
                try
                {
                    WebClient webClient = new WebClient
                    {
                        UseDefaultCredentials = true
                    };
                    webClient.Headers.Add("Content-Type", "application/json");
                    webClient.Encoding = Encoding.UTF8;
                    webClient.Credentials = CredentialCache.DefaultCredentials;
                    string text = webClient.UploadString(serviceUrl, "POST", json);
                    Logger.Log($"response={text}");
                    return text;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, "Отдельный поток AcceptDoWork");
                    throw new Exception(ex.Message);
                }
            }
        }
    }
}