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

    public class BusinessPartnersModel
    {
        public String CardCode { get; set; }
        public string CardName { get; set; }
        public string U_RTN { get; set; }
        public string SalesPersonCode { get; set; }

    }

    public static class BusinessPartners
    {
        public static BusinessPartnersModel GetBusinessPartnersById(string cardcode, string completeFileName)
        {
            BusinessPartnersModel res = null;
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

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strBusinesPartners;

                    Uri UrlSap = new Uri(server + "('" + cardcode + "')");

                    //var httpWebRequest = conexion.GetWebRequest(UrlSap, conexion.Cookies, out Cookie);
                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, completeFileName);
                    httpWebRequest.Timeout = 5000;
                    httpWebRequest.ServicePoint.ConnectionLeaseTimeout = 5000;
                    httpWebRequest.ServicePoint.MaxIdleTime = 5000;
                    httpWebRequest.KeepAlive = false;

                    using (var response = httpWebRequest.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                var responseFromServer = reader.ReadToEnd();

                                JObject jObject = JObject.Parse(responseFromServer);
                                res = jObject.ToObject<BusinessPartnersModel>();
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
                        model.Log.WritelogInvoice("GetBusinessPartnersById", message.message.value, ex.StackTrace, completeFileName, "LectorXML API section");
                    }

                }


            }
            catch (Exception ex)
            {

                model.Log.WritelogInvoice("GetDraftsById", ex.Message, ex.StackTrace, completeFileName, "LectorXML API section");
                res = null;
            }


            return res;
        }

    }
}
