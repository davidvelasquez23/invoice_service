using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceXmlService.API
{
    public static class Drafts
    {

        public class DraftResponse
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
        }
        public class DraftsModel
        {
            public String DocEntry { get; set; }
            public List<DocumentLines> DocumentLines { get; set; }
        }

        public class DocumentLines
        {
            public string ItemDescription { get; set; }
            public double Quantity { get; set; }
            public string AccountCode { get; set; }
        }

        public static List<DraftsModel> GetAllDrafts(string completeFilename, string locationcode)
        {
            List<DraftsModel> list = null;
            CookieCollection Cookie = null;
            var conexion = new ConexionV2();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += ConexionV2.RemoteSSLTLSCertificateValidate;
                Cookie = new CookieCollection();

                try
                {
                    loginresponse session = conexion.SessionLoginV2("Inglosa");
                    String server = session.Authority;

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strDrafts + "/";

                    Uri UrlSap = new Uri(server);

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, completeFilename);
                    httpWebRequest.Timeout = 5000;
                    httpWebRequest.ServicePoint.ConnectionLeaseTimeout = 5000;
                    httpWebRequest.ServicePoint.MaxIdleTime = 5000;

                    using (var response = httpWebRequest.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                var responseFromServer = reader.ReadToEnd();

                                JObject jObject = JObject.Parse(responseFromServer);
                                list = jObject.SelectToken("value").Select(x => x.ToObject<DraftsModel>()).ToList();
                            }
                        }

                        response.Close();
                    }

                    conexion.SessionLogoutV2(completeFilename, Cookie);

                }
                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        model.Log.WritelogInvoice("GetAllDrafts", message.message.value, ex.StackTrace, completeFilename, "LectorXML API section");
                    }

                }



            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetAllDrafts", ex.Message, ex.StackTrace, completeFilename, locationcode);
                list = null;
            }

            return list;
        }

        public static DraftsModel GetDraftsById(int draftId, string completeFileName, string locationcode)
        {
            DraftsModel res = null;
            CookieCollection Cookie = null;

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += API.ConexionV2.RemoteSSLTLSCertificateValidate;
                Cookie = new CookieCollection();
                var conexion = new ConexionV2();

                try
                {
                    loginresponse session = conexion.SessionLoginV2("Inglosa");
                    String server = session.Authority;

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strDrafts;

                    Uri UrlSap = new Uri(server + "?$filter=DocEntry  eq " + draftId + "");

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, completeFileName);


                    using (var response = httpWebRequest.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                var responseFromServer = reader.ReadToEnd();

                                JObject jObject = JObject.Parse(responseFromServer);
                                res = jObject.SelectToken("value").Select(x => x.ToObject<DraftsModel>()).FirstOrDefault();
                            }
                        }

                        response.Close();
                    }

                }
                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        model.Log.WritelogInvoice("GetDraftsById", message.message.value, "LectorXML API section", completeFileName, locationcode);
                    }

                }


            }
            catch (Exception ex)
            {

                model.Log.WritelogInvoice("GetDraftsById", ex.Message, ex.StackTrace, completeFileName, locationcode);
                res = null;
            }


            return res;
        }

        public static DraftResponse PostDrafts(string jsonDraft, string completeFileName, string locationcode)
        {
            CookieCollection Cookie = null;
            var res = new DraftResponse();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += API.ConexionV2.RemoteSSLTLSCertificateValidate;
                Cookie = new CookieCollection();
                var conexion = new ConexionV2();

                try
                {

                    loginresponse session = conexion.SessionLoginV2("Inglosa");
                    String server = session.Authority;

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strDrafts;

                    Uri UrlSap = new Uri(server);

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, completeFileName, ConstAttributesV2.strPostMethod);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                           | SecurityProtocolType.Tls11
                                                           | SecurityProtocolType.Tls12
                                                           | SecurityProtocolType.Ssl3;

                    httpWebRequest.KeepAlive = false;
                    httpWebRequest.ProtocolVersion = HttpVersion.Version10;
                    httpWebRequest.Method = ConstAttributesV2.strPostMethod;

                    byte[] postBytes = Encoding.UTF8.GetBytes(jsonDraft);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.ContentLength = (jsonDraft != null) ? postBytes.Length : 0;
                    if (jsonDraft.Length > 0 && jsonDraft != null)
                    {
                        using (var requestStream = httpWebRequest.GetRequestStream())
                        {
                            requestStream.Write(postBytes, 0, postBytes.Length);
                            requestStream.Close();

                            using (var response = httpWebRequest.GetResponse())
                            {
                                using (var responseStream = response.GetResponseStream())
                                {
                                    using (var reader = new StreamReader(responseStream))
                                    {
                                        var responseFromServer = reader.ReadToEnd();
                                        if (!string.IsNullOrEmpty(responseFromServer))
                                        {
                                            JObject jObject = JObject.Parse(responseFromServer);
                                            res = jObject.ToObject<DraftResponse>();
                                        }

                                    }
                                }

                                response.Close();
                            }
                        }



                    }

                    conexion.SessionLogoutV2(completeFileName, Cookie);



                    //if (jsonDraft.Length > 0 && jsonDraft != null)
                    //    {
                    //        Stream requestStream = httpWebRequest.GetRequestStream();
                    //        requestStream.Write(postBytes, 0, postBytes.Length);
                    //        requestStream.Close();
                    //    }


                    //    if (httpWebRequest != null)
                    //    {
                    //        string result = "";
                    //        //obtenemos la respuesta del server                
                    //        CreateResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    //        using (var streamReader = new StreamReader(CreateResponse.GetResponseStream()))
                    //        {
                    //            result = streamReader.ReadToEnd();
                    //        }

                    //        int httpCode = (int)CreateResponse.StatusCode;
                    //        JObject jObject = JObject.Parse(result);
                    //        res = jObject.ToObject<DraftResponse>();

                    //        //JObject jObject = JObject.Parse(result);
                    //        //res = jObject.SelectToken("value").Select(x => x.ToObject<DraftResponse>()).FirstOrDefault();
                    //    }
                }

                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        model.Log.WritelogInvoice("PostDrafts", message.message.value, "LectorXML API section", completeFileName, locationcode);
                    }
                    return res;
                }


                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("PostDrafts", ex.InnerException.Message, ex.InnerException.StackTrace, completeFileName, locationcode);
                res = null;
                return res;
            }
        }



    }
}
