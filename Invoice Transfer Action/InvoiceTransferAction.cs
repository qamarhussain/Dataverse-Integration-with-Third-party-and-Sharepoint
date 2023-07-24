using Invoice_Transfer_Action.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Invoice_Transfer_Action
{
    public class InvoiceTransferAction : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            OrganizationServiceContext orgContext = new OrganizationServiceContext(service);

            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                tracingService.Trace($"Invoice Transfer Action - Started");

               var settings = DataverseHelper.GetSettings(service, tracingService);
                if (settings == null)
                    throw new Exception("Invalid setting parameters in Setting table!");

                tracingService.Trace($"Invoice Transfer Action - Faktura Guid - {context.InputParameters["InvoiceGuid"].ToString()}");

                settings.FakturaGuid = context.InputParameters["InvoiceGuid"].ToString();
                settings.tracingService = tracingService;


                var token = TripletexHelper.CreateSessionTokenAsync(settings).Result;
                if (!string.IsNullOrEmpty(token))
                {
                    string kundeId = string.Empty;
                    settings.TripletexToken = token;
                    var orderKeyPair = DataverseHelper.PrepareOrder(service, settings, out kundeId);
                    tracingService.Trace($"Invoice Transfer Action - Order File kundeId - {kundeId}");
                    if (orderKeyPair == null)
                    {
                        tracingService.Trace($"Invoice Transfer Action - Faktura not found");
                    }
                    else
                    {
                        tracingService.Trace($"Invoice Transfer Action - Order File Upload - started ");
                        tracingService.Trace($"Invoice Transfer Action - orderKeyPair:  {Newtonsoft.Json.JsonConvert.SerializeObject(orderKeyPair)} ");
                        var orderInfo = orderKeyPair.First();
                        if (orderInfo.Key != 0)
                        {
                            tracingService.Trace($"Invoice Transfer Action - Order File Upload - Order already exist with this Faktura. ");

                            settings.TripletexOrderId = orderInfo.Key;
                            settings.FakturaClientId = kundeId;
                            TripletexHelper.SharePointFakturFileProcess(settings);
                            tracingService.Trace($"Invoice Transfer Action - Order File Upload - Completed ");
                        }
                        else
                        {
                            tracingService.Trace($"Invoice Transfer Action - Order File Upload - Order not exist with this Faktura, proceeding to create new order. ");
                            var createOrderResponseStatus = TripletexHelper.CreateOrderAsync(settings.TripletexApiBaseUrl, tracingService, orderInfo.Value, token);
                            tracingService.Trace($"Invoice Transfer Action - createOrderResponseStatus:  {createOrderResponseStatus.Result} ");

                            if (!string.IsNullOrEmpty(createOrderResponseStatus.Result))
                            {
                                DataverseHelper.SetFakturaOrderId(service, tracingService, Guid.Parse(settings.FakturaGuid), createOrderResponseStatus.Result);

                                if (!string.IsNullOrEmpty(kundeId))
                                {
                                    settings.TripletexOrderId = int.Parse(createOrderResponseStatus.Result);
                                    settings.FakturaClientId = kundeId.ToString();
                                    TripletexHelper.SharePointFakturFileProcess(settings);
                                    tracingService.Trace($"Invoice Transfer Action - Order File Upload - Completed ");
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                tracingService.Trace($"Invoice Transfer Action - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }

        }
       

    }
}
