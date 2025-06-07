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
    class Invoices
    {


        public class InvoiceResponse
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
        }
        public class InvoiceModel
        {
            public String DocEntry { get; set; }
            public DateTime DocDate { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string U_RTN { get; set; }
            public int Series { get; set; }
            public string U_CONTRATO { get; set; }
            public int SalesPersonCode { get; set; }
            public int PaymentGroupCode { get; set; }
            public string U_numfacsar { get; set; }
            public string U_CAI { get; set; }
            public string U_ORDENCOMPRA { get; set; }
            public string U_OCEXONERADA { get; set; }
            public string U_CONSREGEXO { get; set; }
            public string U_REGSAG { get; set; }
            public string Comments { get; set; }
            public string U_UsuarioPos { get; set; }
            public int TransNum { get; set; }
            public List<DocumentLines> DocumentLines { get; set; }

        }

        public class DocumentLines
        {
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public string TaxCode { get; set; }
            public double UnitPrice { get; set; }
            public double Quantity { get; set; }
            public string ProjectCode { get; set; }
            public double LineTotal { get; set; }
            public double TaxTotal { get; set; }
            public double GrossTotal { get; set; }
            public string DiscountPercent { get; set; }

        }

        public class PatchInvoice
        {
            public string U_numfacsar { get; set; }
            public string NumAtCard { get; set; }
            public string U_FECHALIMITE { get; set; }
            public string U_PREFIJO { get; set; }
            public string U_NUMINICIAL { get; set; }
            public string U_NUMFINAL { get; set; }
            public string U_CAI { get; set; }
            public string Printed { get; set; }
            public string Reference2 { get; set; }
            public string U_Email { get; set; }

        }

        public class Invoice_header_create
        {
            public string CardCode { get; set; }
            public string DocObjectCode { get; set; }
            public string DocDate { get; set; }
            public Nullable<int> Series { get; set; }
            public string U_RTN { get; set; }
            public string U_CONTRATO { get; set; }
            public string CardName { get; set; }

            public int SalesPersonCode { get; set; }

            public string U_ORDENCOMPRA { get; set; }
            public string U_UsuarioPos { get; set; }
            public string U_OCEXONERADA { get; set; }
            public string U_CONSREGEXO { get; set; }
            public string U_REGSAG { get; set; }
            public string Comments { get; set; }


            public List<Invoice_detail_create> DocumentLines { get; set; }
        }

        public class Invoice_detail_create
        {
            public string ItemCode { get; set; }
            //public string ItemDescription { get; set; }
            //public string TaxCode { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            //public double DiscountPercent { get; set; }
            public string ProjectCode { get; set; }
            public string CostingCode2 { get; set; }
            public string CostingCode3 { get; set; }
        }

        public class PatchInvoiceProbarConexion
        {
            public string Comments { get; set; }
            //public List<Lines> DocumentLines { get; set; }

        }

        public class Lines
        {
            public string StgDesc { get; set; }
        }

        public static InvoiceResponse PostInvoices(string jsonInvoice,string completefilename)
        {
            CookieCollection Cookie = null;
            var res = new InvoiceResponse();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback += API.ConexionV2.RemoteSSLTLSCertificateValidate;
                Cookie = new CookieCollection();
                var conexion = new ConexionV2();

                try
                {

                    loginresponse session = conexion.SessionLoginV2("Inglosa");
                    String server = session.Authority;

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strInvoices;

                    Uri UrlSap = new Uri(server);

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, "", ConstAttributesV2.strPostMethod);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                           | SecurityProtocolType.Tls11
                                                           | SecurityProtocolType.Tls12
                                                           | SecurityProtocolType.Ssl3;

                    httpWebRequest.KeepAlive = false;
                    httpWebRequest.ProtocolVersion = HttpVersion.Version10;
                    httpWebRequest.Method = ConstAttributesV2.strPostMethod;

                    byte[] postBytes = Encoding.UTF8.GetBytes(jsonInvoice);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.ContentLength = (jsonInvoice != null) ? postBytes.Length : 0;
                    if (jsonInvoice.Length > 0 && jsonInvoice != null)
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
                                            res = jObject.ToObject<InvoiceResponse>();
                                        }

                                    }
                                }

                                response.Close();
                            }
                        }



                    }

                    conexion.SessionLogoutV2("", Cookie);

                }

                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        //model.Log.Writelog("PostDrafts", message.message.value, "LectorXML API section", "", "");
                        model.Log.WritelogInvoice("ProcessIndividualInvoices", message.message.value, ex.StackTrace, completefilename, string.Empty);
                    }
                    return res;
                }


                return res;
            }
            catch (Exception ex)
            {
                //model.Log.Writelog("PostDrafts", ex.InnerException.Message, ex.InnerException.StackTrace, "", "");
                model.Log.WritelogInvoice("ProcessIndividualInvoices", ex.InnerException.Message, ex.StackTrace, completefilename, string.Empty);
                res = null;
                return res;
            }
        }

        public static bool PatchInvoices(int Docentry, string jsonInvoicePatch)
        {
            CookieCollection Cookie = null;
            var res = false;


            try
            {
                ServicePointManager.ServerCertificateValidationCallback += API.ConexionV2.RemoteSSLTLSCertificateValidate;
                Cookie = new CookieCollection();
                var conexion = new ConexionV2();

                try
                {

                    loginresponse session = conexion.SessionLoginV2("Inglosa");
                    String server = session.Authority;

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strInvoices;

                    Uri UrlSap = new Uri(server + "(" + Docentry + ")");

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, "", ConstAttributesV2.strPatchMethod);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                           | SecurityProtocolType.Tls11
                                                           | SecurityProtocolType.Tls12
                                                           | SecurityProtocolType.Ssl3;

                    httpWebRequest.KeepAlive = false;
                    httpWebRequest.ProtocolVersion = HttpVersion.Version10;
                    httpWebRequest.Method = ConstAttributesV2.strPatchMethod;

                    byte[] patchBytes = Encoding.UTF8.GetBytes(jsonInvoicePatch);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.ContentLength = (jsonInvoicePatch != null) ? patchBytes.Length : 0;


                    if (jsonInvoicePatch.Length > 0 && jsonInvoicePatch != null)
                    {
                        using (var requestStream = httpWebRequest.GetRequestStream())
                        {
                            requestStream.Write(patchBytes, 0, patchBytes.Length);
                            requestStream.Flush();
                            requestStream.Close();

                            using (var response = httpWebRequest.GetResponse())
                            {

                                using (var responseStream = response.GetResponseStream())
                                {
                                    using (var reader = new StreamReader(responseStream))
                                    {
                                        var responseFromServer = reader.ReadToEnd();

                                        HttpWebResponse webresponse = (HttpWebResponse)response;
                                        int httpCode = (int)webresponse.StatusCode;

                                        if (httpCode == 204)
                                        {
                                            res = true;
                                        }

                                    }
                                }

                                response.Close();
                            }
                        }



                    }

                    conexion.SessionLogoutV2("", Cookie);

                }

                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        model.Log.WritelogInvoice("PatchInvoices", message.message.value, "LectorXML API section", "", "");
                    }
                    return res;
                }


                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("PatchInvoices", ex.InnerException.Message, ex.InnerException.StackTrace, "", "");
                res = false;
                return res;
            }
        }

        public static bool Probarconexion(int Docentry, string jsonInvoicePatch)
        {
            CookieCollection Cookie = null;
            var res = false;


            try
            {
                ServicePointManager.ServerCertificateValidationCallback += API.ConexionV2.RemoteSSLTLSCertificateValidate;
                Cookie = new CookieCollection();
                var conexion = new ConexionV2();

                try
                {

                    loginresponse session = conexion.SessionLoginV2("Inglosa");
                    String server = session.Authority;

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strInvoices;

                    Uri UrlSap = new Uri(server + "(" + Docentry + ")");

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, "", ConstAttributesV2.strPatchMethod);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                           | SecurityProtocolType.Tls11
                                                           | SecurityProtocolType.Tls12
                                                           | SecurityProtocolType.Ssl3;

                    httpWebRequest.KeepAlive = false;
                    httpWebRequest.ProtocolVersion = HttpVersion.Version10;
                    httpWebRequest.Method = ConstAttributesV2.strPatchMethod;

                    byte[] patchBytes = Encoding.UTF8.GetBytes(jsonInvoicePatch);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.ContentLength = (jsonInvoicePatch != null) ? patchBytes.Length : 0;


                    if (jsonInvoicePatch.Length > 0 && jsonInvoicePatch != null)
                    {
                        using (var requestStream = httpWebRequest.GetRequestStream())
                        {
                            requestStream.Write(patchBytes, 0, patchBytes.Length);
                            requestStream.Flush();
                            requestStream.Close();

                            using (var response = httpWebRequest.GetResponse())
                            {

                                using (var responseStream = response.GetResponseStream())
                                {
                                    using (var reader = new StreamReader(responseStream))
                                    {
                                        var responseFromServer = reader.ReadToEnd();

                                        HttpWebResponse webresponse = (HttpWebResponse)response;
                                        int httpCode = (int)webresponse.StatusCode;

                                        if (httpCode == 204)
                                        {
                                            res = true;
                                        }
                                        else
                                        {
                                            res = true;
                                        }

                                    }
                                }

                                response.Close();
                            }
                        }



                    }

                    conexion.SessionLogoutV2("", Cookie);

                }

                catch (WebException ex)
                {
                    //using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    //{

                    //    string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                    //    JObject jObject = JObject.Parse(errorRes);
                    //    var message = jObject.SelectToken("error").ToObject<error>();
                    //    model.Log.Writelog("PostDrafts", message.message.value, "LectorXML API section", "", "");
                    //}
                    return res;
                }


                return res;
            }
            catch (Exception ex)
            {
                //model.Log.Writelog("PostDrafts", ex.InnerException.Message, ex.InnerException.StackTrace, "", "");
                //res = false;
                return res;
            }
        }

        public static InvoiceModel GetInvoiceById(int Docentry, string completeFileName, string locationcode)
        {
            InvoiceModel res = null;
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

                    server = ConstAttributesV2.strHttp + server + ConstAttributesV2.strController + ConstAttributesV2.strInvoices;

                    //Uri UrlSap = new Uri(server + "?$filter=DocEntry  eq " + draftId + "");
                    Uri UrlSap = new Uri(server + "(" + Docentry + ")");

                    var httpWebRequest = conexion.GetWebRequest(UrlSap, session.Cookies, out Cookie, completeFileName);


                    using (var response = httpWebRequest.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                var responseFromServer = reader.ReadToEnd();

                                JObject jObject = JObject.Parse(responseFromServer);
                                res = jObject.ToObject<InvoiceModel>();


                                //res = jObject.SelectToken("value").Select(x => x.ToObject<DraftsModel>()).FirstOrDefault();
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
                        model.Log.WritelogInvoice("GetInvoiceById", message.message.value+" Docentry:"+ Docentry.ToString(), "LectorXML API section", completeFileName, locationcode);
                    }

                }


            }
            catch (Exception ex)
            {

                model.Log.WritelogInvoice("GetInvoiceById", ex.Message + " Docentry:" + Docentry.ToString(), ex.StackTrace, completeFileName, locationcode);
                res = null;
            }


            return res;
        }



    }
}
