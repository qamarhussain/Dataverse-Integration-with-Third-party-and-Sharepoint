using Invoice_Transfer_Action.Models;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Invoice_Transfer_Action
{

    public class CustomerReq
    {
        public string name { get; set; }
        public string organizationNumber { get; set; }
    }

    public static class TripletexHelper
    {

        public static async Task<string> CreateSessionTokenAsync(TripletexProperties settings)
        {
            try
            {
                string expirationDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
                string url = $"{settings.TripletexApiBaseUrl}/token/session/:create?consumerToken={settings.ConsumerToken}&employeeToken={settings.EmployeeToken}&expirationDate={expirationDate}";
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Put, url);
                var response = await client.SendAsync(request);
                var responseMessage = response.EnsureSuccessStatusCode();

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var getCustomerResponse = response.Content.ReadAsStringAsync().Result;
                    JObject customerJson = JObject.Parse(getCustomerResponse);
                    var custObj = customerJson["value"];
                    var token = custObj["token"];
                    var byteArray = Encoding.ASCII.GetBytes($"{0}:{token}");
                    string encodeString = Convert.ToBase64String(byteArray);
                    return encodeString;
                }
                else
                {
                    settings.tracingService.Trace($"Invoice Transfer Action - CreateSessionTokenAsync:  exception internally");
                    throw new Exception($"Invoice Transfer Action - CreateSessionTokenAsync:  exception internally");
                }

            }
            catch (Exception ex)
            {
                settings.tracingService.Trace($"Invoice Transfer Action - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        public static async Task<string> GetCustomerAsync(string BaseUrl, string organizationNumber, string custName, string token, ITracingService tracingService)
        {
            try
            {

                string url = $"{BaseUrl}/customer?organizationNumber={organizationNumber}";

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", "Basic " + token);
                var response = await client.SendAsync(request);
                var responseMessage = response.EnsureSuccessStatusCode();

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var getCustomerResponse = response.Content.ReadAsStringAsync().Result;
                    JObject customerJson = JObject.Parse(getCustomerResponse);
                    var customersCount = ((JArray)customerJson["values"]).Count;
                    tracingService.Trace($"Invoice Transfer Action - customersCount :  {customersCount} ");
                    if (customersCount == 0)
                    {
                       var newCreatedCustId = CreateCustomerAsync(BaseUrl, organizationNumber, custName, token, tracingService).Result;
                        tracingService.Trace($"Invoice Transfer Action - newCreatedCustId :  {newCreatedCustId} ");
                        return newCreatedCustId;
                    }
                    else
                    {
                        var custObj = customerJson["values"][0];
                        var CUSId = custObj["id"];
                        return CUSId.ToString();
                    }
                }
                else
                {
                    tracingService.Trace($"Invoice Transfer Action - Not found:  Customer not exist against Organization number = {organizationNumber}");
                    throw new Exception($"Invoice Transfer Action - Not found:  Customer not exist against Organization number = {organizationNumber}");
                }

            }
            catch (Exception ex)
            {
                tracingService.Trace($"Invoice Transfer Action - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        private static async Task<string> CreateCustomerAsync(string BaseUrl, string organizationNumber, string name, string token, ITracingService tracingService)
        {
            try
            {
                tracingService.Trace($"Invoice Transfer Action - CreateCustomerAsync:  orgNum: {organizationNumber}, name: {name}");
                string url = $"{BaseUrl}/customer";

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", "Basic " + token);
                var req = new CustomerReq
                {
                    name = name,
                    organizationNumber = organizationNumber
                };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                var responseMessage = response.EnsureSuccessStatusCode();

                if (responseMessage.StatusCode == HttpStatusCode.Created)
                {
                    var getCustomerResponse = response.Content.ReadAsStringAsync().Result;
                    JObject customerJson = JObject.Parse(getCustomerResponse);
                    var custObj = customerJson["value"];
                    var CUSId = custObj["id"];
                    return CUSId.ToString();
                }
                else
                {
                    tracingService.Trace($"Invoice Transfer Action - Create customer:  exception internally");
                    throw new Exception($"Invoice Transfer Action - Create customer:  exception internally");
                }

            }
            catch (Exception ex)
            {
                tracingService.Trace($"Invoice Transfer Action - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        public static async Task<string> CreateOrderAsync(string BaseUrl, ITracingService tracingService, Order order, string token)
        {
            try
            {
                string url = $"{BaseUrl}/order";

                tracingService.Trace($"Issue - baseurl: {BaseUrl} ");
                tracingService.Trace($"Issue - order: {Newtonsoft.Json.JsonConvert.SerializeObject(order)} ");
                tracingService.Trace($"Issue - token: {token} ");

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", "Basic " + token);
                var content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                tracingService.Trace($"Issue - response: {Newtonsoft.Json.JsonConvert.SerializeObject(response)} ");
                var responseMessage = response.EnsureSuccessStatusCode();

                tracingService.Trace($"Invoice Transfer Action - CreateOrderAsync - StatusCode :  {responseMessage.StatusCode} ");

                if (responseMessage.StatusCode == HttpStatusCode.Created)
                {
                    var getOrderResponse = response.Content.ReadAsStringAsync().Result;
                    JObject orderJson = JObject.Parse(getOrderResponse);
                    var ordObj = orderJson["value"];
                    return ordObj["id"].ToString();
                }
                else
                {
                    tracingService.Trace($"Invoice Transfer Action - CreateOrderAsync - Order didn't create");
                    throw new Exception($"Invoice Transfer Action - CreateOrderAsync - Order didn't create");
                }

            }
            catch (Exception ex)
            {
                tracingService.Trace($"Invoice Transfer Action - Order Creation Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }


        public static bool SharePointFakturFileProcess(TripletexProperties tripletexProperties)
        {
            try
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - SharePointFakturFileProcess - Started");

                string folderUrl = $"{tripletexProperties.SharePointBaseUrl}/sites/{tripletexProperties.SharePointSiteName}/_api/web/lists/GetByTitle('{tripletexProperties.SharePointFolderName}')/Items?$select=*,EncodedAbsUrl,FileLeafRef&$filter=((ClientId eq '{tripletexProperties.FakturaClientId}') and (Uploaded ne 1))";
                var token = GetSharePointAccessToken(tripletexProperties);
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - Share point list - folderUrl = {folderUrl}");
                if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Error receiving SharePoint access token.");

                tripletexProperties.SharePointToken = token;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(folderUrl);
                request.Method = "GET";
                request.Accept = "application/json;odata=verbose";
                request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
                request.ContentLength = 0;

                using (WebResponse response = request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        var obj = DeserializeFromJsonString<RootObject>(result, tripletexProperties.tracingService);

                        if (obj == null || obj.d == null) return true;
                        tripletexProperties.tracingService.Trace($"Invoice Transfer Action - Share point list count = {obj.d.results.Count}");
                        foreach (var o in obj.d.results)
                        {
                            tripletexProperties.SharePointListItemId = o.Id;
                            DownloadByPathAsync(tripletexProperties, o.FileLeafRef);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - SharePointFakturFileProcess - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }

        }

        private static string GetSharePointAccessToken(TripletexProperties tripletexProperties)
        {
            try
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - GetSharePointAccessToken - Started");

                string access_token = string.Empty;
                WebRequest request = WebRequest.Create("https://accounts.accesscontrol.windows.net/" + tripletexProperties.SharePointTenantId + "/tokens/OAuth/2");
                request.Method = "POST";
                string postData = "grant_type=client_credentials" +
                "&client_id=" + WebUtility.UrlEncode(tripletexProperties.SharePointClientId + "@" + tripletexProperties.SharePointTenantId) +
                "&client_secret=" + WebUtility.UrlEncode(tripletexProperties.SharePointSecretId) +
                "&resource=" + WebUtility.UrlEncode(tripletexProperties.SharePointTokenAudiencePrincipalId + "/" + tripletexProperties.SharePointDomainWithoutHttp + "@" + tripletexProperties.SharePointTenantId);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                using (WebResponse response = request.GetResponse())
                {
                    dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    reader.Close();
                    dataStream.Close();
                    const string accessToken = "access_token\":\"";
                    int clientIndex = responseFromServer.IndexOf(accessToken, StringComparison.Ordinal);
                    int accessTokenIndex = clientIndex + accessToken.Length;
                    access_token = responseFromServer.Substring(accessTokenIndex, (responseFromServer.Length - accessTokenIndex - 2));
                    return access_token;
                }
            }
            catch (Exception ex)
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - GetSharePointAccessToken - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        private static T DeserializeFromJsonString<T>(string jsonString, ITracingService tracingService)
        {
            try
            {
                tracingService.Trace($"Invoice Transfer Action - GetSharePointAccessToken - DeserializeFromJsonString");
                using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //create an instance of generic type object
                    T obj = Activator.CreateInstance<T>();
                    System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
                    obj = (T)serializer.ReadObject(ms);
                    ms.Close();
                    return obj;
                }
            }
            catch(Exception ex)
            {
                tracingService.Trace($"Invoice Transfer Action - DeserializeFromJsonString - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
           
        }

        private static async Task DownloadByPathAsync(TripletexProperties tripletexProperties, string FileLeafRef)
        {
            try
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - DownloadByPathAsync - Started");

                var client = new HttpClient();
                string url = $"{tripletexProperties.SharePointBaseUrl}/sites/{tripletexProperties.SharePointSiteName}/_api/web/getfilebyserverrelativeurl('/sites/{tripletexProperties.SharePointSiteName}/{tripletexProperties.SharePointFolderName}/{FileLeafRef}')/$value?binaryStringResponseBody=true";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", "Bearer " + tripletexProperties.SharePointToken);
                var response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStreamAsync();
                var uploadStatus = UploadOrderAttachmentAsync(tripletexProperties, result, FileLeafRef).Result;
            }
            catch (Exception ex)
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - DownloadByPathAsync - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        private static async Task<bool> UploadOrderAttachmentAsync(TripletexProperties tripletexProperties, Stream stream, string fileName)
        {
            try
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - UploadOrderAttachmentAsync - Started");

                string url = $"{tripletexProperties.TripletexApiBaseUrl}/order/{tripletexProperties.TripletexOrderId}/:attach";
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Add("Accept", "*/*");
                //request.Headers.Add("Authorization", "Basic MDpleUowYjJ0bGJrbGtJam95TkRjNU5qTXdOQ3dpZEc5clpXNGlPaUowWlhOMExUZG1OakJsTldRM0xUbGxNR0l0TkdRMll5MDRaVFV4TFdVd1kyUmtNakppTWprMFpDSjk=");
                request.Headers.Add("Authorization", $"Basic {tripletexProperties.TripletexToken}");
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(stream), "file", fileName);
                request.Content = content;
                var response = await client.SendAsync(request);
                var status = response.EnsureSuccessStatusCode();
                if (status.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    tripletexProperties.tracingService.Trace($"Invoice Transfer Action - UploadOrderAttachmentAsync - Upload completed, now change Uploading status");
                    SetListItemStatusUploadedAsync(tripletexProperties);
                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - UploadOrderAttachmentAsync - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }

        private static async Task SetListItemStatusUploadedAsync(TripletexProperties tripletexProperties)
        {
            try
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - SetListItemStatusUploadedAsync - Started - SharePointListItemId = {tripletexProperties.SharePointListItemId}");
                if (tripletexProperties.SharePointListItemId != null)
                {
                    string url = $"{tripletexProperties.SharePointBaseUrl}/sites/{tripletexProperties.SharePointSiteName}/_api/web/lists/GetByTitle('{tripletexProperties.SharePointFolderName}')/items({tripletexProperties.SharePointListItemId})";
                    var client = new HttpClient();
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                    request.Headers.Add("If-Match", "*");
                    request.Headers.Add("Authorization", "Bearer " + tripletexProperties.SharePointToken);
                    var content = new StringContent("{\r\n    \"Uploaded\" : true\r\n}", null, "application/json");
                    request.Content = content;
                    var response = client.SendAsync(request).Result;
                    var status = response.EnsureSuccessStatusCode();
                    tripletexProperties.tracingService.Trace($"Invoice Transfer Action - SetListItemStatusUploadedAsync - Uploaded request response status = {status.StatusCode}");

                }
            }
            catch (Exception ex)
            {
                tripletexProperties.tracingService.Trace($"Invoice Transfer Action - SetListItemStatusUploadedAsync - Exception:  {ex.Message} ");
                throw new Exception(ex.Message);
            }
        }



    }
}
