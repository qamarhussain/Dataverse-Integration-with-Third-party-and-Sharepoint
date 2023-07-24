using Invoice_Transfer_Action.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invoice_Transfer_Action
{
    public static class DataverseHelper
    {
        public static void SetFakturaOrderId(IOrganizationService service, ITracingService tracingService, Guid fakturaId, string orderId)
        {
            try
            {
                var fakturaEntity = new Entity("cr200_faktura", fakturaId);
                fakturaEntity["cr200_orderid"] = orderId;
                service.Update(fakturaEntity);
                tracingService.Trace($"Invoice Transfer Action - SetFakturaOrderId success");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Invoice Transfer Action - SetFakturaOrderId Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }
        public static TripletexProperties GetSettings(IOrganizationService service, ITracingService tracingService)
        {
            try
            {
                string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='cr200_setting'>
    <attribute name='cr200_name' />
    <attribute name='cr200_value' />
    <filter type='and'>
      <condition attribute='cr200_name' operator='like' value='TT%' />
    </filter>
  </entity>
</fetch>";
                EntityCollection settings = service.RetrieveMultiple(new FetchExpression(fetchXml));
                if (!settings.Entities.Any())
                    return null;
                tracingService.Trace($"Invoice Transfer Action - GetSettings - settings.Entities:  {settings.Entities.Count} ");
                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                foreach(var item in settings.Entities)
                {
                    keyValuePairs.Add(item.GetAttributeValue<string>("cr200_name"), item.GetAttributeValue<string>("cr200_value"));
                }

                var tripletexProp = new TripletexProperties
                {
                    FakturaClientId = "",
                    TripletexApiBaseUrl = keyValuePairs.Where(x=>x.Key == "TTTripletexApiBaseUrl").First().Value,
                    TripletexOrderId = 2383203,
                    TripletexToken = "",
                    SharePointBaseUrl = keyValuePairs.Where(x => x.Key == "TTSharePointBaseUrl").First().Value,
                    SharePointSiteName = keyValuePairs.Where(x => x.Key == "TTSharePointSiteName").First().Value,
                    SharePointFolderName = keyValuePairs.Where(x => x.Key == "TTSharePointFolderName").First().Value,
                    SharePointTenantId = keyValuePairs.Where(x => x.Key == "TTSharePointTenantId").First().Value,
                    SharePointClientId = keyValuePairs.Where(x => x.Key == "TTSharePointClientId").First().Value,
                    SharePointSecretId = keyValuePairs.Where(x => x.Key == "TTSharePointSecretId").First().Value,
                    SharePointDomainWithoutHttp = keyValuePairs.Where(x => x.Key == "TTSharePointDomainWithoutHttp").First().Value,
                    SharePointTokenAudiencePrincipalId = keyValuePairs.Where(x => x.Key == "TTSharePointTokenAudiencePrincipalId").First().Value,
                    ConsumerToken = keyValuePairs.Where(x => x.Key == "TTconsumerToken").First().Value,
                    EmployeeToken = keyValuePairs.Where(x => x.Key == "TTemployeeToken").First().Value,
                };

                tracingService.Trace($"Invoice Transfer Action - GetSettings - tripletexProp obj:  {Newtonsoft.Json.JsonConvert.SerializeObject(tripletexProp)} ");

                return tripletexProp;

            }
            catch(Exception ex)
            {
                tracingService.Trace($"Invoice Transfer Action - GetSettings - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        public static Dictionary<int, Order> PrepareOrder(IOrganizationService service, TripletexProperties settings, out string kundeId)
        {
            try
            {
                kundeId = string.Empty;
                string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='cr200_ordrelinje'>
                        <attribute name='cr200_ordrelinjeid' />
                        <attribute name='cr200_name' />
                        <attribute name='createdon' />
                        <attribute name='cr200_vasktype' />
                        <attribute name='cr200_trallenummer' />
                        <attribute name='cr200_ordre' />
                        <attribute name='cr200_kjrety' />
                        <order attribute='cr200_name' descending='false' />
                        <filter type='and'>
                          <condition attribute='cr200_faktura' operator='eq' uiname='Faktura: August-Luksus Bilutleie' uitype='cr200_faktura' value='" + settings.FakturaGuid + @"' />
                        </filter>
                        <link-entity name='cr200_ordre' from='cr200_ordreid' to='cr200_ordre' link-type='inner' alias='order'>
                          <attribute name='cr200_dato' />
                          <link-entity name='cr200_kunde' from='cr200_kundeid' to='cr200_kunde' link-type='inner' alias='customer'>
                           <attribute name='cr200_organisasjonsnummer' />
                           <attribute name='cr200_name' />
                          </link-entity>
                        </link-entity>
                        <link-entity name='cr200_faktura' from='cr200_fakturaid' to='cr200_faktura' visible='false' link-type='outer' alias='invoice'>
                          <attribute name='cr200_kunde' />
                          <attribute name='cr200_orderid' />
                        </link-entity>
                     <link-entity name='cr200_vasktype' from='cr200_vasktypeid' to='cr200_vasktype' visible='false' link-type='outer' alias='vasktype'>
                          <attribute name='cr200_productid' />
                          <attribute name='cr200_pris' />
                        </link-entity>
                      </entity>
                    </fetch>";


                EntityCollection invoiceData = service.RetrieveMultiple(new FetchExpression(fetchXml));
                settings.tracingService.Trace($"Issue - fetchxml output: {Newtonsoft.Json.JsonConvert.SerializeObject(invoiceData)}");
                if (!invoiceData.Entities.Any())
                    return null;
                else
                {
                    string TriOrderId = string.Empty;
                    settings.tracingService.Trace($"Issue - line 124");
                    settings.tracingService.Trace($"Issue - invoiceData.Entities count = {invoiceData.Entities.Count}");
                    var invoice = invoiceData.Entities[0];
                    settings.tracingService.Trace($"Issue - invoice data =  {Newtonsoft.Json.JsonConvert.SerializeObject(invoice)}");

                    var customerGuid = ((EntityReference)invoice.GetAttributeValue<AliasedValue>("invoice.cr200_kunde").Value).Id;
                    kundeId = customerGuid.ToString();

                    if (invoice.Attributes.Contains("invoice.cr200_orderid"))
                    {
                        var invoiceOrderIdObj = (AliasedValue)invoice.Attributes["invoice.cr200_orderid"];
                        if (invoiceOrderIdObj != null)
                            TriOrderId = !string.IsNullOrEmpty(Convert.ToString(invoiceOrderIdObj.Value)) ? invoiceOrderIdObj.Value.ToString().Replace(",", string.Empty) : string.Empty;
                    }

                    settings.tracingService.Trace($"Issue - calculated attribute value: {TriOrderId}");
                    if (string.IsNullOrEmpty(TriOrderId))
                    {
                        Entity customerEntity = service.Retrieve("cr200_kunde", customerGuid, new ColumnSet("cr200_organisasjonsnummer", "cr200_name"));
                        var custOrgNumber = customerEntity["cr200_organisasjonsnummer"].ToString();
                        var custName = customerEntity["cr200_name"].ToString();

                        var CustomerId = TripletexHelper.GetCustomerAsync(settings.TripletexApiBaseUrl, custOrgNumber, custName, settings.TripletexToken, settings.tracingService).Result;

                        if (string.IsNullOrEmpty(CustomerId))
                            throw new Exception("Customer data not found!");

                        Order order = new Order();
                        List<OrderLine> orderLines = new List<OrderLine>();

                        order.number = NewNumber().ToString();
                        order.orderDate = DateTime.Now.ToString("yyyy-MM-dd");
                        order.deliveryDate = DateTime.Now.ToString("yyyy-MM-dd");
                        order.customer = new Customer()
                        {
                            id = CustomerId,
                            name = custName
                        };

                        foreach (var orderLine in invoiceData.Entities)
                        {
                            var productIdObj = (AliasedValue)invoice.Attributes["vasktype.cr200_productid"];
                            OrderLine line = new OrderLine();
                            line.description = orderLine.GetAttributeValue<string>("cr200_name");
                            line.product = new Product()
                            {
                                id = ((AliasedValue)orderLine.Attributes["vasktype.cr200_productid"]).Value.ToString()
                            };
                            line.count = 1;

                            orderLines.Add(line);
                        }

                        order.orderLines.AddRange(orderLines);
                        settings.tracingService.Trace($"Invoice Transfer Action - PrepareOrder - Order json:  {Newtonsoft.Json.JsonConvert.SerializeObject(order)} ");
                        var output = new Dictionary<int, Order>();
                        output.Add(0, order);
                        return output;
                    }
                    else
                    {
                        var output = new Dictionary<int, Order>();
                        output.Add(int.Parse(TriOrderId), null);
                        return output;
                    }

                }

            }
            catch(Exception ex)
            {
                settings.tracingService.Trace($"Invoice Transfer Action - PrepareOrder - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        private static int NewNumber()
        {
            Random a = new Random(Guid.NewGuid().GetHashCode());
            return a.Next();
        }

    }
}
