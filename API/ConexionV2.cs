using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceXmlService.API
{
    static public class ConstAttributesV2
    {
        static public String strHttp = "https://";
        //static public String strController = "/b1s/v1/";
        //static public String strIp = "10.10.2.7";
        static public String strIp = "10.10.2.10";
        static public String strController = "/b1s/v2/";
        static public String strPort = "50000";
        static public String strLogin = "Login";
        static public String strLogout = "Logout";
        static public String strPostMethod = "POST";
        static public String strPatchMethod = "PATCH";
        static public String strGetMethod = "GET";
        static public String strUpdateMethod = "UPDATE";
        static public String strDeleteMethod = "DELETE";
        static public String strJsonContentType = "application/json; charset=utf-8";
        static public String strCookieB1Session = "B1SESSION";
        static public String strCookieCompanyDB = "CompanyDB";
        static public String strCookieROUTEID = "ROUTEID";
        static public String strBusinesPartners = "BusinessPartners";
        static public String strItems = "Items";
        static public String strDrafts = "Drafts";
        static public String strInvoices = "Invoices";
        static public String strAttachments2 = "Attachments2";
        static public String strSalesPersons = "SalesPersons";
        static public String strPaymentTermsTypes = "PaymentTermsTypes";
        static public String strSalesTaxCodes = "SalesTaxCodes";

    }

    public class LoginV2
    {
        public String CompanyDB { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; }
    }

    public class loginresponse
    {
        public string Authority { get; set; }
        public CookieCollection Cookies { get; set; }
    }

    public class SapResponse
    {
        public string ErrorMessage { get; set; }
        public HttpWebResponse Response { get; set; }
        public HttpWebRequest Request { get; set; }
        public CookieCollection CookiesSap { get; set; }

        public SapResponse(int error, String message)
        {
            ErrorMessage = message;
            Response = null;
            Request = null;

        }

    }

    #region Error Sap
    public class error
    {
        public int code { get; set; }
        public Message message { get; set; }
    }

    public class Message
    {
        public string lang { get; set; }
        public string value { get; set; }
    }
    #endregion

    public class ConexionV2
    {

        static public bool RemoteSSLTLSCertificateValidate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
        
        private String Server = $"{ConstAttributesV2.strHttp}{ConstAttributesV2.strIp}:{ConstAttributesV2.strPort}{ConstAttributesV2.strController}"; //"https://10.10.2.7:50000/b1s/v1/";
        public loginresponse SessionLoginV2Old(string completeFileName)
        {
            //HttpWebResponse LoginResponse = null;
            var loginresponse = new loginresponse();
            var cookies = new CookieCollection();
            try
            {
                try
                {
                    /*se crea la peticion para el web service*/
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(Server + ConstAttributesV2.strLogin);

                    //Agregar los headers text/plain
                    httpWebRequest.ContentType = ConstAttributesV2.strJsonContentType;
                    httpWebRequest.Method = ConstAttributesV2.strPostMethod;
                    httpWebRequest.CookieContainer = new CookieContainer();
                    httpWebRequest.KeepAlive = false;

                    var credentials = new model.Hertz_Projects_DevEntities().api_configuration.FirstOrDefault();

                    if (credentials == null)
                    {
                        var ErrorMessage = "no hay credenciales para el API en la tabla api_configuration";
                        model.Log.WritelogInvoice("SessionLogin", ErrorMessage, "", completeFileName, "LectorXML API section");
                        //return response;
                    }

                    LoginV2 login = new LoginV2
                    {
                        CompanyDB = credentials.CompanyDB,
                        UserName = credentials.UserName,
                        Password = credentials.Password
                    };

                    String parametros = JsonConvert.SerializeObject(login);

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(parametros);
                        streamWriter.Flush();
                        streamWriter.Close();

                        using (var responseLogin = (HttpWebResponse)httpWebRequest.GetResponse())
                        {
                            //LoginResponse = responseLogin;
                            loginresponse.Authority = responseLogin.ResponseUri.Authority;

                            foreach (Cookie cookieValue in responseLogin.Cookies)
                            {
                                Cookie cookie = new Cookie();
                                if (cookieValue.Name.Equals("B1SESSION"))
                                {
                                    cookie.Name = cookieValue.Name;
                                    cookie.Value = cookieValue.Value;
                                    cookie.Path = "/b1s/v1";
                                    //cookie.Domain = UrlConnection.Host;
                                    cookies.Add(cookie);
                                }
                                else if (cookieValue.Name.Equals("ROUTEID"))
                                {
                                    cookie.Name = cookieValue.Name;
                                    cookie.Value = cookieValue.Value;
                                    cookie.Path = "/b1s";
                                    //cookie.Domain = UrlConnection.Host;
                                    cookies.Add(cookie);
                                }
                            }

                            responseLogin.Close();

                            if (cookies.Count > 0)
                            {
                                loginresponse.Cookies = cookies;
                            }
                            //LoginResponse.Close();
                        }
                    }
                    return loginresponse;

                }
                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        model.Log.WritelogInvoice("SessionLogin", message.message.value, ex.StackTrace, completeFileName, "LectorXML API section");
                    }
                }

            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("SessionLogin", ex.Message, ex.StackTrace, completeFileName, "LectorXML API section");
            }
            return loginresponse;
        }
        public loginresponse SessionLoginV2(string completeFileName)
        {
            //HttpWebResponse LoginResponse = null;
            ServicePointManager.ServerCertificateValidationCallback += RemoteSSLTLSCertificateValidate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var loginresponse = new loginresponse();
            var cookies = new CookieCollection();
            try
            {
                try
                {
                    /*se crea la peticion para el web service*/
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(Server + ConstAttributesV2.strLogin);

                    //Agregar los headers text/plain
                    httpWebRequest.ServicePoint.Expect100Continue = false;
                    httpWebRequest.ContentType = ConstAttributesV2.strJsonContentType;
                    httpWebRequest.Method = ConstAttributesV2.strPostMethod;
                    httpWebRequest.CookieContainer = new CookieContainer();
                    //httpWebRequest.ContentType = "application/json";
                    //httpWebRequest.Accept = "*/*";                   // Como en Postman


                    var credentials = new model.Hertz_Projects_DevEntities().api_configuration.FirstOrDefault();

                    if (credentials == null)
                    {
                        var ErrorMessage = "no hay credenciales para el API en la tabla api_configuration";
                        model.Log.WritelogInvoice("SessionLogin", ErrorMessage, "", completeFileName, "LectorXML API section");
                        //return response;
                    }

                    LoginV2 login = new LoginV2
                    {
                        CompanyDB = credentials.CompanyDB,
                        UserName = credentials.UserName,
                        Password = credentials.Password
                    };

                    //Login login = new Login
                    //{
                    //    CompanyDB = "SBO_HERTZ_PRUEBAS",
                    //    UserName = "manager",
                    //    Password = "@dmiN123*"
                    //};

                    String parametros = JsonConvert.SerializeObject(login);

                    byte[] byteArray = Encoding.UTF8.GetBytes(parametros);
                    httpWebRequest.ContentLength = byteArray.Length;

                    using (var requestStream = httpWebRequest.GetRequestStream())
                    {
                        requestStream.Write(byteArray, 0, byteArray.Length);
                        //streamWriter.Write(parametros);
                        //streamWriter.Flush();
                        //streamWriter.Close();

                        Console.WriteLine(parametros);

                        using (var responseLogin = (HttpWebResponse)httpWebRequest.GetResponse())
                        {
                            //LoginResponse = responseLogin;
                            loginresponse.Authority = responseLogin.ResponseUri.Authority;

                            foreach (Cookie cookieValue in responseLogin.Cookies)
                            {
                                Cookie cookie = new Cookie();
                                if (cookieValue.Name.Equals("B1SESSION"))
                                {
                                    cookie.Name = cookieValue.Name;
                                    cookie.Value = cookieValue.Value;
                                    cookie.Path = ConstAttributesV2.strController;
                                    //cookie.Domain = UrlConnection.Host;
                                    cookies.Add(cookie);
                                }
                                else if (cookieValue.Name.Equals("ROUTEID"))
                                {
                                    cookie.Name = cookieValue.Name;
                                    cookie.Value = cookieValue.Value;
                                    cookie.Path = "/b1s";
                                    //cookie.Domain = UrlConnection.Host;
                                    cookies.Add(cookie);
                                }
                            }

                            responseLogin.Close();

                            if (cookies.Count > 0)
                            {
                                loginresponse.Cookies = cookies;
                            }
                            //LoginResponse.Close();
                        }
                    }
                    return loginresponse;

                }
                catch (WebException ex)
                {
                    using (HttpWebResponse httpResponse = (HttpWebResponse)ex.Response)
                    {

                        string errorRes = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                        JObject jObject = JObject.Parse(errorRes);
                        var message = jObject.SelectToken("error").ToObject<error>();
                        model.Log.WritelogInvoice("SessionLogin", message.message.value, ex.StackTrace, completeFileName, "LectorXML API section");
                    }
                }

            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("SessionLogin", ex.Message, ex.StackTrace, completeFileName, "LectorXML API section");
            }
            return loginresponse;
        }

        public void SessionLogoutV2Old(string completeFileName, CookieCollection Cookies)
        {
            SapResponse response = new SapResponse(0, "Sap connection successfully.");

            try
            {
                HttpWebResponse LogoutResponse = null;
                //Crear la peticion al web service
                ServicePointManager.ServerCertificateValidationCallback += RemoteSSLTLSCertificateValidate;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(Server + ConstAttributesV2.strLogout);

                //Agregar los headers
                httpWebRequest.ContentType = ConstAttributesV2.strJsonContentType;
                httpWebRequest.Method = ConstAttributesV2.strPostMethod;
                httpWebRequest.CookieContainer = new CookieContainer();
                //httpWebRequest.Timeout = -1;
                httpWebRequest.KeepAlive = false;


                Uri UrlConnection = new Uri(Server);
                if (Cookies != null)
                {

                    foreach (Cookie cookieValue in Cookies)
                    {
                        Cookie cookie = new Cookie();
                        if (cookieValue.Name.Equals("B1SESSION"))
                        {
                            cookie.Name = cookieValue.Name;
                            cookie.Value = cookieValue.Value;
                            cookie.Path = "/b1s/v1";
                            cookie.Domain = UrlConnection.Host;
                            httpWebRequest.CookieContainer.Add(cookie);
                        }
                        else if (cookieValue.Name.Equals("ROUTEID"))
                        {
                            cookie.Name = cookieValue.Name;
                            cookie.Value = cookieValue.Value;
                            cookie.Path = "/b1s";
                            cookie.Domain = UrlConnection.Host;
                            httpWebRequest.CookieContainer.Add(cookie);
                        }
                    }
                }


                response.Request = null;

                using (LogoutResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (var responseStream = LogoutResponse.GetResponseStream())
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            var responseFromServer = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("SessionLogout", ex.InnerException.Message, ex.InnerException.StackTrace, completeFileName, "LectorXML API section");
                response.ErrorMessage = ex.Message;
                response.Response = null;
                response.Request = null;
                //FileHandler.WriteFile($"SAP Logout session error: {ex.Message}");
            }

        }

        public void SessionLogoutV2(string completeFileName, CookieCollection Cookies)
        {

            try
            {
                HttpWebResponse LogoutResponse = null;
                //Crear la peticion al web service
                ServicePointManager.ServerCertificateValidationCallback += RemoteSSLTLSCertificateValidate;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(Server + ConstAttributesV2.strLogout);

                //Agregar los headers
                httpWebRequest.ContentType = ConstAttributesV2.strJsonContentType;
                httpWebRequest.Method = ConstAttributesV2.strPostMethod;
                httpWebRequest.CookieContainer = new CookieContainer();
                //httpWebRequest.Timeout = -1;
                httpWebRequest.KeepAlive = false;


                Uri UrlConnection = new Uri(Server);
                if (Cookies != null)
                {

                    foreach (Cookie cookieValue in Cookies)
                    {
                        Cookie cookie = new Cookie();
                        if (cookieValue.Name.Equals("B1SESSION"))
                        {
                            cookie.Name = cookieValue.Name;
                            cookie.Value = cookieValue.Value;
                            cookie.Path = ConstAttributesV2.strController;
                            cookie.Domain = UrlConnection.Host;
                            httpWebRequest.CookieContainer.Add(cookie);
                        }
                        else if (cookieValue.Name.Equals("ROUTEID"))
                        {
                            cookie.Name = cookieValue.Name;
                            cookie.Value = cookieValue.Value;
                            cookie.Path = "/b1s";
                            cookie.Domain = UrlConnection.Host;
                            httpWebRequest.CookieContainer.Add(cookie);
                        }
                    }
                }


                using (LogoutResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    using (var responseStream = LogoutResponse.GetResponseStream())
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            var responseFromServer = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("SessionLogout", ex.InnerException.Message, ex.InnerException.StackTrace, completeFileName, "LectorXML API section");
            }

        }
        public HttpWebRequest GetWebRequest(Uri uriSap, CookieCollection cookiesSource, out CookieCollection cookiesTarget, string completeFileName, string method = "GET")
        {
            HttpWebRequest request = null;
            cookiesTarget = new CookieCollection();
            try
            {
                request = (HttpWebRequest)WebRequest.Create(uriSap);
                //Agregar los headers
                request.ContentType = ConstAttributesV2.strJsonContentType;
                request.Method = method;
                request.CookieContainer = new CookieContainer();

                request.Timeout = 100000;
                request.ServicePoint.ConnectionLeaseTimeout = 100000;
                request.ServicePoint.MaxIdleTime = 100000;

                if (cookiesSource != null)
                {

                    foreach (Cookie cookieValue in cookiesSource)
                    {
                        Cookie cookie = new Cookie();
                        if (cookieValue.Name.Equals("B1SESSION"))
                        {
                            cookie.Name = cookieValue.Name;
                            cookie.Value = cookieValue.Value;
                            cookie.Path = ConstAttributesV2.strController;
                            cookie.Domain = uriSap.Host;
                            request.CookieContainer.Add(cookie);
                        }
                        else if (cookieValue.Name.Equals("ROUTEID"))
                        {
                            cookie.Name = cookieValue.Name;
                            cookie.Value = cookieValue.Value;
                            cookie.Path = "/b1s";
                            cookie.Domain = uriSap.Host;
                            request.CookieContainer.Add(cookie);
                        }
                        cookiesTarget.Add(cookie);
                    }
                }
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetWebRequest", ex.Message, ex.StackTrace, completeFileName, "LectorXML API section");
            }

            return request;
        }

    }
}
