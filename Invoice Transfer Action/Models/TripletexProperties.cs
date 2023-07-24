using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Invoice_Transfer_Action.Models
{
    public class TripletexProperties
    {
        public string FakturaGuid { get; set; }
        public string TripletexApiBaseUrl { get; set; }
        public int TripletexOrderId { get; set; }
        public string TripletexToken { get; set; }
        public string FakturaClientId { get; set; }
        public string SharePointToken { get; set; }
        public string SharePointBaseUrl { get; set; }
        public string SharePointSiteName { get; set; }
        public string SharePointFolderName { get; set; }
        public string SharePointTenantId { get; set; }
        public string SharePointClientId { get; set; }
        public string SharePointSecretId { get; set; }
        public string SharePointDomainWithoutHttp { get; set; }
        public string SharePointTokenAudiencePrincipalId { get; set; }

        public ITracingService tracingService { get; set; }
        public string ConsumerToken { get; set; }
        public string EmployeeToken { get; set; }
        public int? SharePointListItemId { get; set; }


    }
}
