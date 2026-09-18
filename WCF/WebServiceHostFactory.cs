using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;
using Legenda.ProjSpace.Main.Extensions;
using Microsoft.SharePoint.Client.Services;

namespace Legenda.ProjSpace.Main.WCF
{
    internal class WebServiceHostFactory : MultipleBaseAddressWebServiceHostFactory
    {
        public class ConteqMultipleBaseAddressWebServiceHost : MultipleBaseAddressWebServiceHost
        {
            public ConteqMultipleBaseAddressWebServiceHost(Type serviceType, params Uri[] baseAddresses)
                : base(serviceType, baseAddresses)
            {
            }

            private void SetMaxQuotas(WebHttpBinding webHttpBinding)
            {
                webHttpBinding.MaxReceivedMessageSize = Int32.MaxValue; // 2147483647L;
                webHttpBinding.MaxBufferSize = Int32.MaxValue;
                webHttpBinding.ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxArrayLength = int.MaxValue,
                    MaxBytesPerRead = int.MaxValue,
                    MaxDepth = int.MaxValue,
                    MaxNameTableCharCount = int.MaxValue,
                    MaxStringContentLength = int.MaxValue,
                };
            }

            protected override void OnOpening()
            {
                base.OnOpening();
                base.Description.Endpoints.ForEach(delegate (ServiceEndpoint x)
                {
                    SetMaxQuotas(x.Binding as WebHttpBinding);
                });
            }
        }

        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new ConteqMultipleBaseAddressWebServiceHost(serviceType, baseAddresses);
        }
    }

}
