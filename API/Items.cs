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
    public static class Items
    {
        public class ItemsModel
        {
            public string ItemCode { get; set; }
        }

        public static bool createItem(ItemsModel NewItem)
        {
            var res = false;
            try
            {
                return res;
            }
            catch (Exception ex)
            {
                return res;
                throw;
            }
        }
        static public ItemsModel GetItemByIdSWW(String SWW, string completeFileName, string locationcode)
        {
            ItemsModel res = null;
            CookieCollection Cookie = null;

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += API.ConexionV2.RemoteSSLTLSCertificateValidate;

                Cookie = new CookieCollection();
                var conexion = new ConexionV2();
                HttpWebResponse CreateResponse = null;

                try
                {
                    loginresponse session = conexion.SessionLoginV2("Inglosa");
                    String server = session.Authority;
                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strItems;

                    Uri UrlSap = new Uri(server + "?$filter=SWW eq '" + SWW + "'");

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, completeFileName);
                    httpWebRequest.Timeout = 100000;
                    httpWebRequest.ServicePoint.ConnectionLeaseTimeout = 100000;
                    httpWebRequest.ServicePoint.MaxIdleTime = 100000;
                    //httpWebRequest.Timeout = -1;
                    //httpWebRequest.ServicePoint.ConnectionLeaseTimeout = 5000;
                    //httpWebRequest.ServicePoint.MaxIdleTime = 5000;

                    using (var response = httpWebRequest.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                var responseFromServer = reader.ReadToEnd();

                                JObject jObject = JObject.Parse(responseFromServer);
                                res = jObject.SelectToken("value").Select(x => x.ToObject<ItemsModel>()).FirstOrDefault();
                            }
                        }

                        response.Close();
                    }

                    conexion.SessionLogoutV2(completeFileName, Cookie);

                }
                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        model.Log.WritelogInvoice("GetAllDrafts", message.message.value, "LectorXML API section", completeFileName, locationcode);
                    }

                }
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetAllDrafts", ex.InnerException.Message, ex.InnerException.StackTrace, completeFileName, locationcode);
                res = null;
            }


            return res;
        }

    }
}
