using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Windows.Forms;
using InvoiceXmlService.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;

using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;


namespace InvoiceXmlService
{
    public  class LogicSapInvoice
    {
        public static string currentfile;
        public static string oldfile;
        public static string locationCodeGlobal = "Aun no se obtiene location code desde archivo";
        //private static string connSSAI = ConfigurationManager.ConnectionStrings["Hertz_Projects_DevEntities"].ToString().Split('"')[1];
        public bool EsDecontado = false;
        public bool tieneISV = false;
        public int DocentryGlobal = 0;
        public string emailGlobal = string.Empty;
        public string Userpos = string.Empty;

        private static string conexionDB = ConfigurationManager.ConnectionStrings["Hertz_Projects_DevEntities"].ToString().Split('"')[1];

        #region logica de archivo
        /// <summary>
        /// obtenemos las ubicaciones donde van los archivos para procesar
        /// </summary>
        /// <returns>ruta de archivo</returns>
        private static string GetFilesLocation()
        {
            var res = string.Empty;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    res = context.configuration.FirstOrDefault().path_file_no_process;
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetFilesLocation", ex.Message, ex.StackTrace, "no hay archivo aun", locationCodeGlobal);
                return res;

            }
        }
        /// <summary>
        /// ubicacion de los archivos ya procesados
        /// </summary>
        /// <returns>ruta de archivo</returns>
        private static string GetFilesProcesLocation()
        {
            var res = string.Empty;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {

                    res = context.configuration.FirstOrDefault().path_file_process;
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetFilesProcesLocation", ex.Message, ex.StackTrace, "no hay archivo aun", locationCodeGlobal);
                return res;

            }
        }
        /// <summary>
        /// ubicacion de los archivos con error
        /// </summary>
        /// <returns>ruta de ubicacion</returns>
        private static string GetFilesNoProcesErrorLocation()
        {
            var res = string.Empty;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {

                    res = context.configuration.FirstOrDefault().path_file_error;
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetFilesNoProcesErrorLocation", ex.Message, ex.StackTrace, "no hay archivo aun", locationCodeGlobal);

                return res;

            }
        }
        /// <summary>
        /// obtiene path de los que ya no tienen correlativo
        /// </summary>
        /// <returns></returns>
        private static string GetFilesNocorrelativoLocation()
        {
            var res = string.Empty;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {

                    res = context.configuration.FirstOrDefault().path_file_nocorrelativo;
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetFilesNocorrelativoLocation", ex.Message, ex.StackTrace, "no hay archivo aun", locationCodeGlobal);

                return res;

            }
        }

        /// <summary>
        /// obtenemos el encabezado y detalle de la factura en Rentworks
        /// </summary>
        /// <param name="PathFile">ruta de archivo</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>una estructura en borrador</returns>
        public  Draft GetcontentFile(string PathFile, string completeFileName)
        {
            var draft = new Draft();

            try
            {
                var name = Path.GetFileName(PathFile);

                XmlDocument doc = new XmlDocument();
                doc.Load(PathFile);

                draft = GetContentHeaderFile(doc, completeFileName);

                if (draft != null)
                {
                    draft.Detail = GetContentDetailFile(draft, doc, completeFileName);
                }
                else
                {
                    return null;
                }


                return draft;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetcontentFile", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return null;
            }
        }
        /// <summary>
        /// obtenemos el encabezado de la factura generada en Rentworks
        /// </summary>
        /// <param name="doc">documento XML</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns></returns>
        /// 
        public Draft GetContentHeaderFile(XmlDocument doc, string completeFileName)
        {
            var res = new Draft();
            res.Rtn = string.Empty;
            var dateIn = new DateTime();
            var dateOut = new DateTime();
            var haveCompany = false;
            int SalesPersonCodeContado = -1;
            var NamePartGlobal = string.Empty;
            var enteretRenterRAAlredy = false;

            var fechaRPTdate = string.Empty;
            var fechaDateIn = string.Empty;
            EsDecontado = false;

            try
            {
                foreach (XmlNode node in doc)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        var childnodeName = childNode.Name;

                        if (childnodeName == "Company")
                        {
                            haveCompany = true;
                            res.CardCode = childNode["Code"].InnerText;
                            res.State = 1;
                            continue;
                        }

                        if (childnodeName == "etPayments")
                        {
                            var tempContractmp = childNode["InvoiceNumber"].InnerText;

                            if (!string.IsNullOrEmpty(tempContractmp) && tempContractmp.Length > 3)
                            {
                                res.Contract = tempContractmp;
                            }

                        }

                        if (childnodeName == "etInvTrx")
                        {
                            if (string.IsNullOrEmpty(childNode["DateIn"].InnerText))
                            {
                                res.date = Convert.ToDateTime(fechaRPTdate);
                            }


                            var itemProject = API.Items.GetItemByIdSWW(childNode["UnitNumber"].InnerText, completeFileName, locationCodeGlobal);

                            if (itemProject != null)
                            {
                                var projectTmp = itemProject.ItemCode;

                                if (!string.IsNullOrEmpty(projectTmp))
                                {
                                    res.project = projectTmp;
                                }
                            }
                        }

                        if (childnodeName == "etRRM")
                        {
                            //res.Contract = childNode["RANumber"].InnerText;
                            res.Contract = completeFileName;
                            var rpdateIn = childNode["DateIn"].InnerText;

                            fechaRPTdate = childNode["RptDate"].InnerText;
                            fechaDateIn = childNode["DateIn"].InnerText;

                            res.TotalCharges = childNode["TotalCharges"].InnerText;
                            if (!string.IsNullOrEmpty(rpdateIn))
                            {
                                res.date = Convert.ToDateTime(rpdateIn);
                                //obtenemos los dias de renta
                                dateIn = Convert.ToDateTime(childNode["DateIn"].InnerText);
                                dateOut = Convert.ToDateTime(childNode["DateOut"].InnerText);
                                res.daysRent = (dateIn - dateOut).TotalDays == 0 ? 1 : (dateIn - dateOut).TotalDays;
                            }
                            else
                            {
                                res.date = Convert.ToDateTime(childNode["DateDue"].InnerText);
                                //obtenemos los dias de renta
                                dateIn = Convert.ToDateTime(childNode["DateDueOriginal"].InnerText);
                                dateOut = Convert.ToDateTime(childNode["DateDue"].InnerText); res.daysRent = (dateIn - dateOut).TotalDays == 0 ? 1 : (dateIn - dateOut).TotalDays;
                                res.daysRent = (dateOut - dateIn).TotalDays == 0 ? 1 : (dateOut - dateIn).TotalDays;
                            }

                            //

                            res.Client = childNode["CustomerFirstname"].InnerText + " " + childNode["CustomerLastname"].InnerText;

                        }

                        if (childnodeName == "etRenterRA")
                        {

                            //if(enteretRenterRAAlredy)
                            //{
                            //    continue;
                            //}

                            //enteretRenterRAAlredy = true;

                            var email = childNode["EMail"].InnerText.ToString();

                            if (email.Length > 5)
                            {
                                emailGlobal = email;
                            }

                            var tmprtn = childNode["InsuranceCo"].InnerText;

                            if (!string.IsNullOrEmpty(tmprtn))
                            {
                                res.Rtn = tmprtn;
                            }

                            var SalesCodeRentwork = childNode["PolicyNumber"].InnerText.ToString();


                            var NamePart = childNode["LocalAddress"].InnerText.ToString();

                            if (NamePart.Length > 2)
                            {
                                if (Regex.IsMatch(NamePart, @"^[a-zA-Z,.() ]+$"))
                                {
                                    //NamePart = string.Empty;
                                    NamePartGlobal = " " + NamePart;
                                    //res.Client = res.Client + NamePart;
                                }
                            }

                            //LocalAddress

                            if (!string.IsNullOrEmpty(SalesCodeRentwork))
                            {
                                var tempcode = ObtenerCodigoVendedor(SalesCodeRentwork);

                                if (tempcode > 0)
                                {
                                    SalesPersonCodeContado = tempcode;
                                }

                            }

                            continue;
                        }

                        if (childnodeName == "etRRM")
                        {
                            //res.Serie = GetSerieIdLocation(childNode["LocationCodeDue"].InnerText, completeFileName);
                            res.Serie = GetSerieIdLocation(childNode["LocationCodeOut"].InnerText, completeFileName);
                            if (res.Serie == null)
                            {
                                return null;
                            }

                            res.CCLocation = childNode["LocationCodeOut"].InnerText;

                        }

                    }

                    if (!haveCompany)
                    {
                        res.CardCode = "CCN-99999";
                        res.CCBussinesLine = "RNT";
                        res.SellerIdCode = SalesPersonCodeContado.ToString();
                        EsDecontado = true;

                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(res.CardCode))
                        {
                            var companyFromApi = API.BusinessPartners.GetBusinessPartnersById(res.CardCode, completeFileName);
                            if (companyFromApi == null)
                            {
                                res.CardCode = "CCN-99999";
                                res.CCBussinesLine = "RNT";
                                res.SellerIdCode = SalesPersonCodeContado.ToString();
                                EsDecontado = true;
                                continue;
                            }

                            if (companyFromApi != null)
                            {
                                //res.CardCode = res.CardCode;
                                res.Client = companyFromApi.CardName;
                                res.CCBussinesLine = "RNT";
                                res.SellerIdCode = companyFromApi.SalesPersonCode;
                                res.Rtn = companyFromApi.U_RTN;
                            }
                        }
                        else
                        {
                            res.CardCode = "CCN-99999";
                            res.CCBussinesLine = "RNT";
                            res.SellerIdCode = res.SellerIdCode = SalesPersonCodeContado.ToString();
                            EsDecontado = true;
                        }
                    }
                }

                res.Client = res.Client + NamePartGlobal;
                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetContentHeaderFile", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// obtenemos las lineas del detalle de la factura
        /// </summary>
        /// <param name="draftHeader">linea de encabezado del borrador de Rentworks</param>
        /// <param name="doc">documento xml</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns></returns>
        public List<DraftDetail> GetContentDetailFile(Draft draftHeader, XmlDocument doc, string completeFileName)
        {
            var resdetail = new List<DraftDetail>();
            var cont = 0;
            var ChgDays = 0.0;
            var totalTPM = 0.0;
            //inicializamos variables globales
            Userpos = string.Empty;
            bool haveetBookedRev = false;
            bool Agregarcombustible = true;
            tieneISV = false;

            var rowExceptionCombustible = new model.Hertz_Projects_DevEntities().combustible_excepciones.FirstOrDefault(x => x.cod_cliente == draftHeader.CardCode);

            if (rowExceptionCombustible != null)
            {
                Agregarcombustible = false;
            }

            try
            {
                foreach (XmlNode node in doc)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        var childnodeName = childNode.Name;

                        //primera linea
                        if (childnodeName == "etRentalDB")
                        {
                            cont++;
                            var row = new DraftDetail();
                            row.Code = draftHeader.project;
                            row.Order = cont;//orden de linea
                            row.Quantity = draftHeader.daysRent;
                            row.Price = Math.Round(Convert.ToDouble(childNode["TotalTPM"].InnerText) / draftHeader.daysRent, 2);
                            totalTPM = Convert.ToDouble(childNode["TotalTPM"].InnerText);
                            resdetail.Add(row);

                            var textRenatlDiscopunt = childNode["RentalDiscountAmt"].InnerText;

                            Userpos = childNode["EmployeeNumberIn"].InnerText.ToString();

                            if (!string.IsNullOrEmpty(textRenatlDiscopunt))
                            {
                                var discountRental = Convert.ToDouble(childNode["RentalDiscountAmt"].InnerText);

                                if (discountRental > 0)
                                {
                                    cont++;
                                    var row2 = new DraftDetail();
                                    row2.Code = "DESC RATE";
                                    row2.Order = cont;//orden de linea
                                    row2.Quantity = -1;
                                    //var price= Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / draftHeader.daysRent, 2);
                                    var price = discountRental;
                                    row2.Price = price < 0 ? price * -1 : price;
                                    resdetail.Add(row2);
                                }
                            }




                        }

                        if (childnodeName == "etBookedRev")
                        {
                            haveetBookedRev = true;
                        }

                        if (childnodeName == "etRCMIFT")
                        {
                            var code = childNode["Code"].InnerText;

                            if (!string.IsNullOrEmpty(code))
                            {
                                if (EsSeguro(code))
                                {
                                    var tmpChgDays = Convert.ToDouble(childNode["ChgDays"].InnerText);

                                    if (tmpChgDays > ChgDays)
                                    {
                                        ChgDays = tmpChgDays;
                                    }
                                }
                            }

                            if (code == "ISV")
                            {
                                tieneISV = true;
                            }

                            if (code == "PERMISO")
                            {
                                //var tmpChgDays = Convert.ToDouble(childNode["ChgDays"].InnerText);

                                //if (tmpChgDays > ChgDays)
                                //{
                                //    ChgDays = tmpChgDays;
                                //}

                                cont++;
                                var row = new DraftDetail();
                                row.Code = code;
                                row.Order = cont;//orden de linea
                                row.Quantity = 1;
                                //var price= Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / draftHeader.daysRent, 2);
                                var price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / 1, 2);
                                row.Price = price < 0 ? price * -1 : price;
                                resdetail.Add(row);
                            }

                            if (code == "AF")
                            {
                                //var tmpChgDays = Convert.ToDouble(childNode["ChgDays"].InnerText);

                                //if (tmpChgDays > ChgDays)
                                //{
                                //    ChgDays = tmpChgDays;
                                //}

                                cont++;
                                var row = new DraftDetail();
                                row.Code = code;
                                row.Order = cont;//orden de linea
                                row.Quantity = 1;
                                var price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / 1, 2);
                                row.Price = price < 0 ? price * -1 : price;
                                resdetail.Add(row);
                            }

                            if    (code == "DROP"
                                ||(code.ToUpper().Trim() == "ASPS")
                                || (code.ToUpper().Trim() == "OPSPS")
                                || (code.ToUpper().Trim() == "OPT")
                                || (code.ToUpper().Trim() == "DSAP")
                                || (code.ToUpper().Trim() == "PIA HERTZ")
                                || (code.ToUpper().Trim() == "PIA DOLLAR"))
                            {
                                var uniquecode = "DROP";
                                //var tmpChgDays = Convert.ToDouble(childNode["ChgDays"].InnerText);
                                var ChgUnits = Convert.ToDouble(childNode["ChgUnits"].InnerText);

                                //if (tmpChgDays > ChgDays)
                                //{
                                //    ChgDays = tmpChgDays;
                                //}

                                cont++;
                                var row = new DraftDetail();
                                //row.Code = code;
                                row.Code = uniquecode;
                                row.Order = cont;//orden de linea
                                row.Quantity = 1;
                                var price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / ChgUnits, 2);
                                row.Price = price < 0 ? price * -1 : price;
                                resdetail.Add(row);
                            }


                            if (code == "DESC COVER" || code.ToUpper() == "DESC RATE")
                            {


                                //cont++;
                                //var row = new DraftDetail();
                                //row.Code = code;
                                //row.Order = cont;//orden de linea
                                //row.Quantity = -1;
                                //var price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / 1, 2);
                                //row.Price = price < 0 ? price * -1 : price;
                                //resdetail.Add(row);

                                //var tmpChgDays = Convert.ToDouble(childNode["ChgDays"].InnerText);

                                //if (tmpChgDays > ChgDays)
                                //{
                                //    ChgDays = tmpChgDays;
                                //}

                                //tomar dias
                                //2*-1
                                var tmpChgDays = Convert.ToDouble(childNode["ChgUnits"].InnerText);
                                var price = 0.0;

                                cont++;
                                var row = new DraftDetail();
                                row.Code = code;
                                row.Order = cont;//orden de linea
                                if (tmpChgDays > 0)
                                {
                                    row.Quantity = tmpChgDays * -1;
                                }
                                else
                                {
                                    row.Quantity = tmpChgDays;
                                }

                                if (tmpChgDays > 0)
                                {
                                    price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / tmpChgDays, 2);
                                }
                                else
                                {
                                    price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / 1, 2);
                                }


                                row.Price = price < 0 ? price * -1 : price;
                                resdetail.Add(row);
                            }

                            if (code != "TTX"
                                && code != "ISV"
                                && code != "FUEL"
                                && code != "DIESEL"
                                && code.ToUpper() != "DESC COVER"
                                && code.ToUpper() != "DESC RATE"
                                && code != "ADRV"
                                && code != "DRIVER"
                                && code != "BABYSEAT"
                                && code != "AF"
                                && code != "DROP"
                                && code != "PERMISO"
                                && (code.ToUpper().Trim() != "ASPS")
                                && (code.ToUpper().Trim() != "OPSPS")
                                && (code.ToUpper().Trim() != "OPT")
                                && (code.ToUpper().Trim() != "DSAP")
                                && (code.ToUpper().Trim() != "PIA HERTZ")
                                && (code.ToUpper().Trim() != "PIA DOLLAR"))
                            {
                                var tmpChgDays = Convert.ToDouble(childNode["ChgDays"].InnerText);

                                //if (tmpChgDays > ChgDays)
                                //{
                                //    ChgDays = tmpChgDays;
                                //}

                                cont++;
                                var row = new DraftDetail();
                                row.Code = code;
                                row.Order = cont;//orden de linea
                                //row.Quantity = draftHeader.daysRent;
                                row.Quantity = tmpChgDays;
                                //row.Price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / draftHeader.daysRent, 2);
                                row.Price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / tmpChgDays, 2);
                                resdetail.Add(row);
                            }

                            if (code == "ADRV" || code == "DRIVER" || code == "BABYSEAT")
                            {
                                var tmpChgDays = Convert.ToDouble(childNode["ChgDays"].InnerText);

                                //if (tmpChgDays > ChgDays)
                                //{
                                //    ChgDays = tmpChgDays;
                                //}

                                var Total = Convert.ToDouble(childNode["Total"].InnerText);

                                if (Total <= 0)
                                {
                                    continue;
                                }




                                cont++;
                                var row = new DraftDetail();
                                row.Code = code;
                                row.Order = cont;//orden de linea
                                row.Quantity = tmpChgDays;//el valor parametrizable
                                //row.Price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / draftHeader.daysRent, 2);
                                row.Price = (Total / tmpChgDays);
                                resdetail.Add(row);
                            }

                            //crear una tabla parametrizable
                            if (code == "FUEL" || code == "DIESEL" || code == "PEAJE")
                            {
                                if (!Agregarcombustible)
                                {
                                    continue;
                                }

                                cont++;
                                var row = new DraftDetail();
                                row.Code = code;
                                row.Order = cont;//orden de linea
                                row.Quantity = 1;//el valor parametrizable
                                //row.Price = Math.Round(Convert.ToDouble(childNode["Total"].InnerText) / draftHeader.daysRent, 2);
                                row.Price = Convert.ToDouble(childNode["Total"].InnerText);
                                resdetail.Add(row);
                            }

                        }
                    }
                }

                foreach (var row in resdetail)
                {
                    if (row.Order == 1)
                    {
                        if (ChgDays <= 0)
                        {
                            row.Quantity = draftHeader.daysRent;
                            row.Price = Math.Round(totalTPM / draftHeader.daysRent, 2);
                        }
                        else
                        {
                            row.Quantity = ChgDays;
                            row.Price = Math.Round(totalTPM / ChgDays, 2);

                            //if(draftHeader.daysRent< ChgDays)
                            //{
                            //    row.Quantity = ChgDays;
                            //    row.Price = Math.Round(totalTPM / ChgDays, 2);
                            //}
                            //else
                            //{
                            //    row.Quantity = draftHeader.daysRent;
                            //    row.Price = Math.Round(totalTPM / draftHeader.daysRent, 2);
                            //}


                        }
                    }
                }

                if (haveetBookedRev)
                {
                    foreach (var row in resdetail)
                    {

                        row.Quantity = draftHeader.daysRent;
                        row.Price = Math.Round(totalTPM / draftHeader.daysRent, 2);

                    }
                }


                return resdetail;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetContentDetailFile", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return resdetail;

            }
        }

        #endregion

        #region procesamiento
        /// <summary>
        /// funcion principal de procesamiento de archivo
        /// </summary>
        public void ProcesFilesFromXML()
        {
            //escribir("entramos al servicio");
            var filesInvoices = new List<Draft>();
            var fileName = "aun no hay archivos para procesar";
            var userCredentials = GetCredentials(fileName);
            
            if (userCredentials == null)
            {
                return;
            }

            //escribir("obtenemos credenciales: "+ userCredentials.user_credential);

            try
            {
                var path = GetFilesLocation();
                var pathrprocessed = GetFilesProcesLocation();
                var errorProcessed = GetFilesNoProcesErrorLocation();
                var NocorrelativoPath = GetFilesNocorrelativoLocation();

                //var path = @"C:\\RentWorks";
                //var pathrprocessed = @"C:\\RentWorks\XML_PROCESADOS";
                //var errorProcessed = @"C:\\RentWorks\XML_ERRORES";
                //var NocorrelativoPath = @"C:\\RentWorks\XML_SIN_CORRELATIVO";


                string[] fileEntries = Directory.GetFiles(path);
                //escribir("lectura de archivos");
                foreach (string file in fileEntries)
                {
                    //escribir("leyendo archivo: "+file);
                    var res = false;
                    fileName = file;
                    var CurrentDraft = new Draft();

                    var name = Path.GetFileName(file);
                    string[] contractsParts = name.Split('_');

                    if (contractsParts.Count() < 3)
                    {
                        return;
                    }

                    
                    var contractnumber = contractsParts[3].Replace(".xml", "");
                    currentfile = contractnumber;
                    //escribir("numero de contrato: "+ currentfile);
                    //factura existe
                    if (contractExist(contractnumber, name))
                    {
                        if (System.IO.File.Exists(fileName))
                        {

                            File.Delete(file);
                            Thread.Sleep(1000);
                            model.Log.WritelogInvoice("CONTRATO YA EXISTE", "numero de contrato: " + contractnumber, "", fileName, locationCodeGlobal);

                        }
                        return;
                    }


                    //factura existe en sap
                    if (contractExistSAP(contractnumber, name))
                    {
                        if (System.IO.File.Exists(fileName))
                        {

                            File.Delete(file);
                            model.Log.WritelogInvoice("CONTRATO YA EXISTE", "numero de contrato: " + contractnumber, "", fileName, locationCodeGlobal);

                        }
                        return;
                    }

                    //escribir("numero de contrato: " + currentfile);

                    //borrador existe
                    if (contractExistDraft(contractnumber, name))
                    {
                        if (System.IO.File.Exists(fileName))
                        {

                            File.Delete(file);
                            Thread.Sleep(1000);
                            model.Log.WritelogInvoice("BORRADOR YA EXISTE", "numero de contrato: " + contractnumber, "", fileName, locationCodeGlobal);

                        }
                        return;
                    }


                    


                    if (System.IO.File.Exists(fileName))
                    {
                        CurrentDraft = GetcontentFile(file, contractnumber);

                        if (CurrentDraft != null)
                        {
                            var serieInt = CurrentDraft.Serie;

                            var rowExceptionDraft = GetcustomerExceptionForDraft(CurrentDraft.CardCode, fileName);

                            if (rowExceptionDraft.customer_code != null || (EsDecontado && !tieneISV))
                            {
                                res = ProcessIndividualDrafts(CurrentDraft, fileName);
                            }
                            else
                            {
                                if (serieInt > 0)
                                {

                                    var correltivoVencido = CorrelativoVencido(serieInt.ToString(), name);

                                    if (correltivoVencido)
                                    {
                                        var fileNameCopy = System.IO.Path.GetFileName(file);
                                        var destFile = System.IO.Path.Combine(NocorrelativoPath, fileNameCopy);
                                        System.IO.File.Copy(fileName, destFile, true);
                                        File.Delete(file);

                                        model.Log.WritelogInvoice("CORRELATIVO VENCIDO", "locacion: " + CurrentDraft.CCLocation + " serie: " + CurrentDraft.Serie + " para el archivo: " + name, "", fileName, locationCodeGlobal);

                                        continue;
                                    }

                                    var tieneCorreltivo = TieneCorrelativo(serieInt.ToString(), name);

                                    if (!tieneCorreltivo)
                                    {
                                        
                                        var fileNameCopy = System.IO.Path.GetFileName(file);
                                        var destFile = System.IO.Path.Combine(NocorrelativoPath, fileNameCopy);
                                        System.IO.File.Copy(fileName, destFile, true);
                                        Thread.Sleep(2000);
                                        File.Delete(file);

                                        model.Log.WritelogInvoice("SIN CORRELATIVO o CORRELATIVO REPETIDO", "locacion: " + CurrentDraft.CCLocation + " serie: " + CurrentDraft.Serie + " para el archivo: " + name, "", fileName, locationCodeGlobal);

                                        continue;
                                    }
                                }

                                res = ProcessIndividualInvoices(CurrentDraft, fileName);
                            }

                            if (res)
                            {
                                
                                var fileNameCopy = System.IO.Path.GetFileName(file);
                                var destFile = System.IO.Path.Combine(pathrprocessed, fileNameCopy);
                                System.IO.File.Copy(fileName, destFile, true);
                                Thread.Sleep(2000);
                                File.Delete(file);

                                //generatePdfInvoice(68641);
                                //if (DocentryGlobal > 0)
                                //{
                                //    generatePdfInvoice(DocentryGlobal);
                                //}

                                //DocentryGlobal = 0;
                            }
                            else
                            {
                                var fileNameCopy = System.IO.Path.GetFileName(file);
                                var destFile = System.IO.Path.Combine(errorProcessed, fileNameCopy);
                                System.IO.File.Copy(fileName, destFile, true);
                                File.Delete(file);
                            }
                        }
                        else
                        {
                            var fileNameCopy = System.IO.Path.GetFileName(file);
                            var destFile = System.IO.Path.Combine(errorProcessed, fileNameCopy);
                            System.IO.File.Copy(fileName, destFile, true);
                            File.Delete(file);
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("ProcesFilesFromXML", ex.Message, ex.StackTrace, fileName, locationCodeGlobal);
            }
        }
        /// <summary>
        /// funcion que procesa archivos individuales
        /// </summary>
        /// <param name="Drafts">borrador de la factura a procesar</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>true si proceso bien, false si hubo error</returns>
        public bool ProcessIndividualInvoices(Draft CurrentDraft, string completeFileName)
        {
           
            var res = false;
            model.correlativosSar correlativo = null;
            string grantotal = string.Empty;
            try
            {
                if (CurrentDraft != null)
                {
                    var str = string.Empty;
                    grantotal = CurrentDraft.TotalCharges;

                    var contractNumber = Regex.Match(CurrentDraft.Contract, @"\d+").Value;

                    var locationcodeContract = CurrentDraft.CCLocation;

                    if (CurrentDraft.CCLocation.Length > 2)
                    {
                        using (var context = new Hertz_Projects_DevEntities())
                        {
                            var row = context.correlatives.FirstOrDefault(x => x.location_code == CurrentDraft.CCLocation);

                            if (row != null)
                            {
                                locationcodeContract = row.cost_center;
                            }
                        }
                    }



                    var MainInvoice = new API.Invoices.Invoice_header_create
                    {

                        CardCode = CurrentDraft.CardCode,
                        CardName = CurrentDraft.Client,
                        DocDate = CurrentDraft.date.ToString("yyyy-MM-dd"),
                        Series = CurrentDraft.Serie,
                        U_CONTRATO = locationcodeContract + "_" + contractNumber,
                        U_RTN = CurrentDraft.Rtn,
                        DocObjectCode = "oInvoices",
                        SalesPersonCode = Convert.ToInt16(CurrentDraft.SellerIdCode),

                        U_ORDENCOMPRA = str,
                        U_UsuarioPos = Userpos,
                        U_OCEXONERADA = str,
                        U_CONSREGEXO = str,
                        U_REGSAG = str,
                        Comments = str

                    };

                    MainInvoice.DocumentLines = new List<API.Invoices.Invoice_detail_create>();



                    var detailOrdered = CurrentDraft.Detail.OrderBy(x => x.Order).ToList();

                    if (detailOrdered.Count > 0)
                    {
                        foreach (var rowDetail in detailOrdered)
                        {

                            var detailInvoices = new API.Invoices.Invoice_detail_create();
                            detailInvoices.ItemCode = rowDetail.Code;
                            detailInvoices.Quantity = rowDetail.Quantity;
                            detailInvoices.UnitPrice = rowDetail.Price;
                            detailInvoices.ProjectCode = CurrentDraft.project;
                            detailInvoices.CostingCode2 = "RNT";

                            var cost_center = new model.Hertz_Projects_DevEntities()
                                .correlatives.FirstOrDefault(x => x.location_code == CurrentDraft.CCLocation);
                            if (cost_center != null)
                            {
                                detailInvoices.CostingCode3 = cost_center.cost_center;
                            }
                            else
                            {
                                detailInvoices.CostingCode3 = "";
                            }

                            //grantotal = grantotal + (rowDetail.Quantity* rowDetail.Price);

                            MainInvoice.DocumentLines.Add(detailInvoices);

                        }

                        var settings = new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto,

                        };

                        var json = JsonConvert.SerializeObject(MainInvoice, settings);

                        var rowException = GetcustomerException(MainInvoice.CardCode, completeFileName, locationCodeGlobal);
                        //var rowExceptionDraft = GetcustomerExceptionForDraft(MainInvoice.CardCode, completeFileName);

                        if (rowException.customer_code != null)
                        {
                            if (rowException.exchange_rate > 0)
                            {
                                model.Log.WritelogInvoice("antes de procesar", json, "linea 1016", completeFileName, locationCodeGlobal);
                                var resapi = API.Invoices.PostInvoices(json,completeFileName);

                                //var resapi = new API.Invoices.InvoiceResponse
                                //{
                                //    DocEntry = 1,
                                //    DocNum = 2

                                //};

                                if (resapi.DocEntry != 0)
                                {
                                    model.Log.WritelogInvoice("despues de procesar", "docEntry: "+ resapi.DocEntry,"linea 1028", completeFileName, locationCodeGlobal);
                                   
                                    correlativo = ObtenerCorrelativo(MainInvoice.Series.ToString(), completeFileName);
                                    var invoice = ObtenerFacturadesdeSap(resapi.DocEntry,completeFileName);
                                    var docEntry = resapi.DocEntry;

                                    var actFactura = ActualizarFacturaSap(correlativo, resapi.DocEntry, completeFileName, invoice.TransNum.ToString());
                                    if (actFactura)
                                    {
                                        var resInsert = SalvarFactura(json, resapi, correlativo.CorrelativoSiguiente, MainInvoice.U_CONTRATO, grantotal, correlativo.CorrelativoReferencia, str, completeFileName);
                                        res = true;
                                    }
                                }
                                else
                                {
                                    res = false;
                                }
                            }
                            else
                            {
                                //insertar factura
                                //res = InsertDraftInDatabasewithoutPost(MainInvoice, Drafts.Contract, completeFileName, json);
                            }
                        }
                        else
                        {

                            //var resapi = API.Drafts.PostDrafts(json, completeFileName, locationCodeGlobal);
                            model.Log.WritelogInvoice("antes de procesar", json, "linea 1056", completeFileName, locationCodeGlobal);
                            var resapi = API.Invoices.PostInvoices(json,completeFileName);

                            if (resapi.DocEntry != 0)
                            {
                                model.Log.WritelogInvoice("despues de procesar", "docEntry: " + resapi.DocEntry, "linea 1061", completeFileName, locationCodeGlobal);
                                var invoice = ObtenerFacturadesdeSap(resapi.DocEntry,completeFileName);
                                var docEntry = resapi.DocEntry;

                                if (docEntry != 0 && invoice != null)
                                {
                                    correlativo = ObtenerCorrelativo(MainInvoice.Series.ToString(), completeFileName);
                                    
                                    var actFactura = ActualizarFacturaSap(correlativo, resapi.DocEntry, completeFileName, invoice.TransNum.ToString());
                                    if (actFactura)
                                    {
                                        var resInsert = SalvarFactura(json, resapi, correlativo.CorrelativoSiguiente, MainInvoice.U_CONTRATO, grantotal, correlativo.CorrelativoReferencia, str, completeFileName);
                                        res = true;
                                    }
                                }
                                else
                                {
                                    model.Log.WritelogInvoice("sin numero de factura", "contrato sin docentry o invoice.TransNum, DocEntry:" + docEntry.ToString(), json, completeFileName, locationCodeGlobal);
                                }
                                
                            }
                            else
                            {
                                res = false;
                            }
                        }
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("ProcessIndividualInvoices", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// funcion que procesa archivos indiviuales a draft
        /// </summary>
        /// <param name="Drafts"></param>
        /// <param name="completeFileName"></param>
        /// <returns></returns>
        public static bool ProcessIndividualDrafts(Draft Drafts, string completeFileName)
        {
            var res = false;
            try
            {
                if (Drafts != null)
                {
                    var contract = Regex.Match(Drafts.Contract, @"\d+").Value;

                    var MainDraft = new draft_header_create();

                    MainDraft.CardCode = Drafts.CardCode;
                    MainDraft.CardName = Drafts.Client;
                    MainDraft.DocObjectCode = "13";
                    MainDraft.DocDate = Drafts.date.ToString("yyyy-MM-dd");
                    MainDraft.Series = Drafts.Serie;
                    MainDraft.U_RTN = Drafts.Rtn;
                    MainDraft.U_CONTRATO = Drafts.CCLocation + "_" + contract;
                    MainDraft.DocumentLines = new List<draft_detail_create>();

                    var detailOrdered = Drafts.Detail.OrderBy(x => x.Order).ToList();

                    if (detailOrdered.Count > 0)
                    {
                        foreach (var rowDetail in detailOrdered)
                        {
                            var detail = new draft_detail_create();

                            detail.ItemCode = rowDetail.Code;
                            detail.Quantity = rowDetail.Quantity.ToString();
                            detail.UnitPrice = rowDetail.Price.ToString();
                            detail.ProjectCode = Drafts.project;
                            detail.CostingCode2 = "RNT";

                            var cost_center = new model.Hertz_Projects_DevEntities()
                                .correlatives.FirstOrDefault(x => x.location_code == Drafts.CCLocation);
                            if (cost_center != null)
                            {
                                detail.CostingCode3 = cost_center.cost_center;
                            }
                            else
                            {
                                detail.CostingCode3 = "";
                            }



                            MainDraft.DocumentLines.Add(detail);

                        }

                        var settings = new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto,

                        };

                        var json = JsonConvert.SerializeObject(MainDraft, settings);

                        var rowException = GetcustomerException(MainDraft.CardCode, completeFileName, locationCodeGlobal);

                        if (rowException.customer_code != null)
                        {
                            if (rowException.exchange_rate > 0)
                            {
                                var resapi = API.Drafts.PostDrafts(json, completeFileName, locationCodeGlobal);

                                if (resapi.DocEntry != 0)
                                {
                                    res = InsertDraftInDatabase(MainDraft, Drafts.Contract, completeFileName, json, resapi);


                                }
                                else
                                {
                                    res = false;
                                }
                            }
                            else
                            {
                                res = InsertDraftInDatabasewithoutPost(MainDraft, Drafts.Contract, completeFileName, json);
                            }
                        }
                        else
                        {

                            var resapi = API.Drafts.PostDrafts(json, completeFileName, locationCodeGlobal);

                            if (resapi.DocEntry != 0)
                            {
                                res = InsertDraftInDatabase(MainDraft, Drafts.Contract, completeFileName, json, resapi);

                            }
                            else
                            {
                                res = false;
                            }
                        }
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("ProcessDrafts", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        public static bool InsertDraftInDatabase(draft_header_create draft, string contractnumber, string completeFileName, string json, API.Drafts.DraftResponse draftInfo)
        {
            var res = false;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    var header = new model.draft_header_post();

                    header.CardCode = draft.CardCode;
                    header.U_RTN = draft.U_RTN;
                    header.U_CONTRATO = draft.U_CONTRATO;
                    header.docDate = draft.DocDate;
                    header.contract_number = contractnumber;
                    header.docObjectCode = draft.DocObjectCode;
                    header.series = draft.Series;
                    header.json_to_sap = json;
                    header.docnum = draftInfo.DocNum;
                    header.docEntry = draftInfo.DocEntry;

                    context.draft_header_post.Add(header);
                    context.SaveChanges();

                    int id = header.draftId; // Yes it's here

                    foreach (var rowdetail in draft.DocumentLines)
                    {
                        var detail = new model.draft_detail_post();

                        detail.draftId = header.draftId;
                        detail.ItemCode = rowdetail.ItemCode;
                        detail.Quantity = rowdetail.Quantity;
                        detail.UnitPrice = rowdetail.UnitPrice;
                        detail.ProjectCode = rowdetail.ProjectCode;
                        detail.CostingCode2 = rowdetail.CostingCode2;
                        detail.CostingCode3 = rowdetail.CostingCode3;

                        context.draft_detail_post.Add(detail);
                        context.SaveChanges();
                        oldfile = contractnumber;
                        res = true;
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("InsertDraftInDatabase", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// ingresa un draft sin procesarlo a SAP
        /// </summary>
        /// <param name="draft">draft completo</param>
        /// <param name="contractnumber">numero de contrato</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <param name="json">json del draft</param>
        /// <returns></returns>
        public static bool InsertDraftInDatabasewithoutPost(draft_header_create draft, string contractnumber, string completeFileName, string json)
        {
            var res = false;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    var header = new model.draft_header_post();

                    header.CardCode = draft.CardCode;
                    header.U_RTN = draft.U_RTN;
                    header.U_CONTRATO = draft.U_CONTRATO;
                    header.docDate = draft.DocDate;
                    header.contract_number = contractnumber;
                    header.docObjectCode = draft.DocObjectCode;
                    header.series = draft.Series;
                    header.json_to_sap = json;

                    context.draft_header_post.Add(header);
                    context.SaveChanges();

                    int id = header.draftId; // Yes it's here

                    foreach (var rowdetail in draft.DocumentLines)
                    {
                        var detail = new model.draft_detail_post();

                        detail.draftId = header.draftId;
                        detail.ItemCode = rowdetail.ItemCode;
                        detail.Quantity = rowdetail.Quantity;
                        detail.UnitPrice = rowdetail.UnitPrice;
                        detail.ProjectCode = rowdetail.ProjectCode;
                        detail.CostingCode2 = rowdetail.CostingCode2;
                        detail.CostingCode3 = rowdetail.CostingCode3;

                        context.draft_detail_post.Add(detail);
                        context.SaveChanges();
                        res = true;
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("InsertDraftInDatabasewithoutPost", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// funcion que salva factura ne base de datos
        /// </summary>
        /// <param name="json"></param>
        /// <param name="InvoicetInfo">respuesta al procesar la factura</param>
        /// <param name="correlativo">correlativo del SAR</param>
        /// <param name="contrato">numero de contrato</param>
        /// <param name="grantotal">gran total de la factura</param>
        /// <param name="correlativoReferencia">referencia de la tabla de correlativos</param>
        /// <param name="tipodoc">tipo de documento: "factura"</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>true si salvo factura, false si uno un error</returns>
        private static bool SalvarFactura(string json, API.Invoices.InvoiceResponse InvoicetInfo, int? correlativo, string contrato, string grantotal, string correlativoReferencia, string tipodoc, string completeFileName)
        {
            var res = false;
            try
            {

                JObject jObject = JObject.Parse(json);
                var infoInvoice = jObject.ToObject<API.Invoices.Invoice_header_create>();

                if (infoInvoice.DocumentLines.Count > 0)
                {
                    var split_oficina = infoInvoice.U_CONTRATO.Split('_');
                    var oficina = split_oficina[0];

                    res = InsertInvoiceInDatabase(infoInvoice, json, InvoicetInfo, oficina, correlativo, contrato, tipodoc, grantotal, correlativoReferencia, completeFileName);
                }
                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("SalvarFactura", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }

        /// <summary>
        /// inserta datos en tabla de factura
        /// </summary>
        /// <param name="invoice">informacion completa de la factura</param>
        /// <param name="json">json enviado a SAP</param>
        /// <param name="InvoiceInfo">informacion de respuesta de la factura</param>
        /// <param name="oficina">oficina</param>
        /// <param name="correlativo">correlativo de la SAR</param>
        /// <param name="NumeroContrato">numero de contrato</param>
        /// <param name="tipodoc">tipo de documento "factura"</param>
        /// <param name="grantotal">gran total de la factura</param>
        /// <param name="correlativoReferencia">correlativo referencia en SAR</param>
        /// <param name="completeFileName"></param>
        /// <returns>true si salvo la factura en base de datos, falso si hubo un error</returns>
        private static bool InsertInvoiceInDatabase(API.Invoices.Invoice_header_create invoice, string json, API.Invoices.InvoiceResponse InvoiceInfo, string oficina, int? correlativo, string NumeroContrato, string tipodoc, string grantotal, string correlativoReferencia, string completeFileName)
        {
            var res = false;
            try
            {
                var contrato = string.Empty;
                var numFacturaCompleto = string.Empty;

                if (correlativoReferencia.Length > 2)
                {
                    numFacturaCompleto = correlativoReferencia + correlativo.ToString().Substring(1, correlativo.ToString().Length - 1);
                }

                if (!string.IsNullOrEmpty(tipodoc))
                {
                    contrato = oficina + "_" + tipodoc + "_" + NumeroContrato;
                }
                else
                {
                    string[] contractsParts = invoice.U_CONTRATO.Split('_');
                    if (contractsParts.Length > 1)
                    {
                        contrato = contractsParts[1];
                    }
                    else
                    {
                        contrato = invoice.U_CONTRATO;
                    }

                }

                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    var header = new model.invoice_header_post();


                    header.user_SSAI = "fac_automatica";
                    header.CardCode = invoice.CardCode;
                    header.U_RTN = invoice.U_RTN;
                    header.U_CONTRATO = invoice.U_CONTRATO; //invoice.U_CONTRATO;
                    header.docDate = invoice.DocDate;
                    header.contract_number = contrato; //Regex.Match(invoice.U_CONTRATO, @"\d+").Value;
                    header.docObjectCode = "oInvoices";
                    header.series = invoice.Series;
                    header.json_to_sap = json;
                    header.docnum = InvoiceInfo.DocNum;
                    header.docEntry = InvoiceInfo.DocEntry;
                    header.date_processed = DateTime.Now;
                    header.correlativoSar = correlativo;



                    var str = string.Empty;
                    //otros datos
                    header.n_factura = numFacturaCompleto;
                    header.cai = str;
                    header.correlativoConstanciaRegistro = str;
                    header.correlativoOrdenCompraExenta = str;
                    header.identificativoRegistroSag = str;
                    header.granTotal = Convert.ToDecimal(grantotal);
                    header.impreso = false;

                    context.invoice_header_post.Add(header);

                    context.SaveChanges();

                    int id = header.InvoiceId;

                    foreach (var rowdetail in invoice.DocumentLines)
                    {
                        var detail = new model.invoice_detail_post();

                        detail.invoiceId = header.InvoiceId;
                        detail.ItemCode = rowdetail.ItemCode;
                        detail.Quantity = rowdetail.Quantity.ToString();
                        detail.UnitPrice = rowdetail.UnitPrice.ToString();
                        detail.ProjectCode = rowdetail.ProjectCode;
                        detail.CostingCode2 = "RNT";
                        detail.CostingCode3 = oficina;

                        context.invoice_detail_post.Add(detail);
                        context.SaveChanges();

                    }

                    res = true;
                }



                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("InsertInvoiceInDatabase", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// obtiene codigo de vendedor por codigo de rentworks
        /// </summary>
        /// <returns>codigo de vendedor</returns>
        private int ObtenerCodigoVendedor(string codigoRentworks)
        {
            int res = 0;
            try
            {
                using (var connSec = new SqlConnection(conexionDB))
                {
                    if (connSec.State == ConnectionState.Open)
                        connSec.Close();
                    connSec.Open();
                    var dt = new DataTable();
                    var comm = "BUSCAR_VENDEDOR_POR_ID";
                    var sqlc = new SqlCommand(comm, connSec);
                    sqlc.CommandType = CommandType.StoredProcedure;
                    sqlc.Parameters.AddWithValue("@FindParm", codigoRentworks);
                    var da = new SqlDataAdapter(sqlc);
                    da.Fill(dt);
                    connSec.Close();

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        res = Convert.ToInt16(dt.Rows[i]["Codigo"]);
                    }

                }

                return res;
            }
            catch (Exception ex)
            {
                return res;
            }
        }
        private API.Invoices.InvoiceModel ObtenerFacturadesdeSap(int docEntry,string filename)
        {
            API.Invoices.InvoiceModel InvoicesSap = new API.Invoices.InvoiceModel();

            try
            {

                InvoicesSap = API.Invoices.GetInvoiceById(docEntry, filename, "");

                if (InvoicesSap != null)
                {
                    return InvoicesSap;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("ObtenerFacturadesdeSap", "contrato sin docentry o invoice.TransNum, DocEntry:" + docEntry.ToString()+" "+ex.InnerException,ex.StackTrace.ToString(), "", locationCodeGlobal);
                return InvoicesSap;
            }

        }
        /// <summary>
        /// Actualiza un factura procesada con un numero de la SAR
        /// </summary>
        /// <param name="correlativo">correlativo de la SAR</param>
        /// <param name="docentry">numero proporcionado por SAP al procesar factura</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>true si actualizo factura en SAP, false si hubo un error</returns>
        private  bool ActualizarFacturaSap(model.correlativosSar correlativo, int docentry, string completeFileName, string reference2)
        {
            var res = false;
            try
            {
                var json = ObtenerJsonParaPatchFactura(correlativo, completeFileName, reference2);

                if (!string.IsNullOrEmpty(json))
                {
                    res = API.Invoices.PatchInvoices(docentry, json);
                }
                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("ActualizarFacturaSap", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }

        }

        #endregion

        #region Utileria
        /// <summary>
        /// arma el json patch para actualizar la factura procesada 
        /// </summary>
        /// <param name="correlativoRow">row con informacion de correlativo que pertenece a la factura</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>json para actualizar factura</returns>
        private  string ObtenerJsonParaPatchFactura(model.correlativosSar correlativoRow, string completeFileName, string reference2)
        {
            var json = string.Empty; 
           

            try
            {
                if (correlativoRow != null)
                {
                    var correlativosiguiente = correlativoRow.CorrelativoSiguiente.ToString();
                    var correlativoreferencia = correlativoRow.CorrelativoReferencia.ToString();
                    var correlativoInicial = correlativoRow.CorrelativoInicial.ToString();
                    var correlativoFinal = correlativoRow.CorrelativoFinal.ToString();
                    var fechavenceCai = correlativoRow.FechaVenceCAI;
                    var CAI = correlativoRow.CAI;

                    var patchdata = new API.Invoices.PatchInvoice
                    {
                        U_numfacsar = correlativoreferencia + correlativosiguiente.Substring(1, correlativosiguiente.Length - 1),
                        NumAtCard = correlativoreferencia + correlativosiguiente.Substring(1, correlativosiguiente.Length - 1),
                        U_FECHALIMITE = fechavenceCai.Value.Date.ToString("yyyy-MM-dd"),
                        U_PREFIJO = correlativoreferencia,
                        U_NUMINICIAL = correlativoreferencia + correlativoInicial.Substring(1, correlativoInicial.Length - 1),
                        U_NUMFINAL = correlativoreferencia + correlativoFinal.Substring(1, correlativoFinal.Length - 1),
                        U_CAI = CAI,
                        Printed = "psNo",
                        Reference2 = reference2,
                        U_Email=emailGlobal
                    };



                    json = JsonConvert.SerializeObject(patchdata);
                }

                model.Log.WritelogInvoice("datos actualizar", json, "linea 1604", completeFileName, locationCodeGlobal);
                return json;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("ObtenerJsonParaPatchFactura", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return json;
            }
        }
        /// <summary>
        /// obtenemos el id de la serie de la ubicacion
        /// </summary>
        /// <param name="locationcode">codigo de la ubicacion eje: (OPSPS)</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>id de la ubicacion</returns>
        private static int? GetSerieIdLocation(string locationcode, string completeFileName)
        {
            int? res = 0;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {

                    var row = context.correlatives.FirstOrDefault(x => x.location_code == locationcode);
                    if (row != null)
                    {
                        locationCodeGlobal = row.location_code;
                        res = row.serie;
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetSerieIdLocation", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// obtiene credenciales de Windows
        /// </summary>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>modelo de credenciales de windows</returns>
        private static model.user_credential_windows GetCredentials(string completeFileName)
        {
            var res = new model.user_credential_windows();
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    res = context.user_credential_windows.FirstOrDefault();

                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetCredentials", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// verifica si el contrato existe y no esta nulado
        /// </summary>
        /// <param name="contractnumber">numero de contrato</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>true si existe, false sino existe</returns>
        private static bool contractExist(string contract, string completeFileName)
        {
            var res = false;
            try
            {
                var contractnumber = Regex.Match(contract, @"\d+").Value;
                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    if (!string.IsNullOrEmpty(contractnumber))
                    {
                        var rowInvoice = context.invoice_header_post.Where(x => x.contract_number == contractnumber).FirstOrDefault(y => y.CANCELED == null);

                        if (rowInvoice != null)
                        {
                            res = true;
                        }
                    }
                    //var row = context.draft_header_post.FirstOrDefault(x => x.contract_number == contractnumber);

                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("contractExist", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }

        /// <summary>
        /// verifica si el contrato existe y no esta nulado en SAP
        /// </summary>
        /// <param name="contractnumber">numero de contrato</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>true si existe, false sino existe</returns>
        private static bool contractExistSAP(string contract, string completeFileName)
        {
            var res = false;
            try
            {
                //var contractnumber = Regex.Match(contract, @"\d+").Value;
                //var builder = new StringBuilder();

                //bool haveSeperator = false;

                //foreach (var c in contract)
                //{


                //    if (Char.IsNumber(c) && !haveSeperator)
                //    {
                //        builder.Append('_');
                //        builder.Append(c);
                //        haveSeperator = true;
                //    }
                //    else
                //    {
                //        builder.Append(c);
                //    }
                //}

                //var contractnumber = builder.ToString();

                var contractnumber = Regex.Match(contract, @"\d+").Value;

                res = LogicSapInvoice.ExisteFacturaEnSap(contractnumber);

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("contractExistSAP", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }

        /// <summary>
        /// obtiene codigo de vendedor por codigo de rentworks
        /// </summary>
        /// <returns>codigo de vendedor</returns>
        private static bool ExisteFacturaEnSap(string contract)
        {
            string existe = string.Empty;
            var res = false;
            try
            {
                using (var connSec = new SqlConnection(conexionDB))
                {
                    if (connSec.State == ConnectionState.Open)
                        connSec.Close();
                    connSec.Open();
                    var dt = new DataTable();
                    var comm = "BUSCAR_FACTURAS_SAP_POR_ID";
                    var sqlc = new SqlCommand(comm, connSec);
                    sqlc.CommandType = CommandType.StoredProcedure;
                    sqlc.Parameters.AddWithValue("@FindParm", contract);
                    var da = new SqlDataAdapter(sqlc);
                    da.Fill(dt);
                    connSec.Close();

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        existe = Convert.ToString(dt.Rows[i]["docEntry"]);
                        res = true;
                        break;
                    }

                }

                return res;
            }
            catch (Exception ex)
            {
                return res;
            }
        }
        /// <summary>
        /// obtiene todas los clientes con excepcion para facturacion
        /// </summary>
        /// <param name="customerCode">codigo de cliente</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <param name="locationcode">codigo de locacion</param>
        /// <returns>modelo de excepciones de clientes</returns>
        public static model.customer_exception GetcustomerException(string customerCode, string completeFileName, string locationcode)
        {
            var res = new model.customer_exception();
            try
            {
                var row = new model.Hertz_Projects_DevEntities().customer_exception.FirstOrDefault(x => x.customer_code == customerCode);

                if (row != null)
                {
                    res = row;
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetcustomerException", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }

        /// <summary>
        /// obtiene las excepciones de contratos que se mandaran a Drafts
        /// </summary>
        /// <param name="customerCode">codigo del cliente</param>
        /// <param name="completeFileName">nombre del archivo</param>
        /// <param name="locationcode"></param>
        /// <returns></returns>
        public static model.customer_exception_for_draft GetcustomerExceptionForDraft(string customerCode, string completeFileName)
        {
            var res = new model.customer_exception_for_draft();
            try
            {
                var row = new model.Hertz_Projects_DevEntities().customer_exception_for_draft.FirstOrDefault(x => x.customer_code == customerCode);

                if (row != null)
                {
                    res = row;
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("GetcustomerExceptionForDraft", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        /// <summary>
        /// obtiene correlativo de la SAR
        /// </summary>
        /// <param name="codigoSerie">serie de donde vamos obtener correlativo</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>modelo de correlativo SAR</returns>
        public static model.correlativosSar ObtenerCorrelativo(string codigoSerie, string completeFileName)
        {
            model.correlativosSar correlativo = new model.correlativosSar { CorrelativoSiguiente = -1 };

            int codSerie = Convert.ToInt32(codigoSerie);
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    var locationCode = context.correlatives.FirstOrDefault(x => x.serie == codSerie);

                    var rowLocation = context.locations.FirstOrDefault(x => x.code == locationCode.cost_center);

                    if (rowLocation != null)
                    {
                        var row = context.correlativosSar.FirstOrDefault(x => x.locationId == rowLocation.location_id);
                        correlativo = row;
                        if (row != null)
                        {
                            if (row.CorrelativoSiguiente > row.CorrelativoFinal)
                            {
                                correlativo.CorrelativoSiguiente = -1;
                            }
                            else
                            {
                                var correlativoAUsar = correlativo.CorrelativoSiguiente;

                                row.CorrelativoSiguiente = correlativo.CorrelativoSiguiente + 1;
                                context.correlativosSar.AddOrUpdate(row);
                                context.SaveChanges();
                                correlativo.CorrelativoSiguiente = correlativoAUsar;
                            }
                        }
                    }
                }

                return correlativo;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("ObtenerCorrelativo", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return correlativo;
            }
        }
        /// <summary>
        /// verifica si hay correlativo para esa serie
        /// </summary>
        /// <param name="codigoSerie">codigo de la serie</param>
        /// <param name="completeFileName"></param>
        /// <returns>true si aun tiene disponible, false si ya se acabaron</returns>
        private static bool TieneCorrelativo(string codigoSerie, string completeFileName)
        {
            var res = true;
            try
            {

                //validar que no se repita correlativo

                int codSerie = Convert.ToInt32(codigoSerie);

                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    var locationCode = context.correlatives.FirstOrDefault(x => x.serie == codSerie);
                    var rowLocation = context.locations.FirstOrDefault(x => x.code == locationCode.location_code);

                    if (rowLocation != null)
                    {
                        var row = context.correlativosSar.FirstOrDefault(x => x.locationId == rowLocation.location_id);

                        if (row != null)
                        {
                            var correlativosDisponibles = row.CorrelativoFinal - row.CorrelativoSiguiente;

                            if (correlativosDisponibles <= row.notificacion)
                            {
                                model.Log.WritelogInvoice("TieneCorrelativo", "no tiene correlativos", "no tiene correlativos", completeFileName, locationCodeGlobal);
                                return false;
                            }

                            //verificar si existe correlativo
                            var correlativoRow = context.correlativosSar.FirstOrDefault(x => x.locationId == rowLocation.location_id);

                            if (correlativoRow != null)
                            {
                                var correlativoExiste = context.invoice_header_post.Where(x => x.correlativoSar == correlativoRow.CorrelativoSiguiente).Where(y => y.series == codSerie).FirstOrDefault();

                                if (correlativoExiste != null)
                                {
                                    model.Log.WritelogInvoice("TieneCorrelativo", "correlativo ya existe", correlativoRow.CorrelativoSiguiente.ToString(), completeFileName, locationCodeGlobal);
                                    //row.CorrelativoSiguiente = correlativoRow.CorrelativoSiguiente + 1;
                                    //context.correlativosSar.AddOrUpdate(row);
                                    //context.SaveChanges();
                                    return false;
                                }

                            }
                        }
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("TieneCorrelativo", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }

        /// <summary>
        /// verifica si vencio el correlativo de la serie
        /// </summary>
        /// <param name="codigoSerie">codigo de la serie</param>
        /// <param name="completeFileName"></param>
        /// <returns></returns>
        private static bool CorrelativoVencido(string codigoSerie, string completeFileName)
        {
            var res = true;
            try
            {

                //validar que no este vencido el correlativo

                int codSerie = Convert.ToInt32(codigoSerie);

                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    var locationCode = context.correlatives.FirstOrDefault(x => x.serie == codSerie);
                    var rowLocation = context.locations.FirstOrDefault(x => x.code == locationCode.cost_center);

                    if (rowLocation != null)
                    {
                        var row = context.correlativosSar.FirstOrDefault(x => x.locationId == rowLocation.location_id);

                        if (row != null)
                        {

                            if (DateTime.Now.Date >= row.FechaEmision.Value.Date && DateTime.Now.Date <= row.FechaVenceCAI.Value.Date)
                            {
                                res = false;
                            }
                            else
                            {
                                res = true;
                            }
                        }
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("CorrelativoVencido", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }

        private void escribir(string text)
        {
            try
            {
                //var path = "C:\\test\\test.txt";
                //var path = "C:\\testInvoiceService\\test.txt";
                //var path = "C:\\Users\\alexa\\OneDrive\\Desktop\\test";

                //string[] lines = { "old falcon", "deep forest", "golden ring" };
                //string text = "hola " + DateTime.Now.ToString() + "\n";

                //File.WriteAllText(path, text);

                //using (StreamWriter writetext = new StreamWriter("C:\\testInvoiceService\\test.txt"))
                //{
                //    writetext.WriteLine(text);

                //}

                File.AppendAllText("C:\\testInvoiceService\\test.txt", "\n" + text);

                //MessageBox.Show("hola" + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {

            }
        }

        public static bool EsSeguro(string codigo)
        {
            bool res = false;

            try
            {
                using (var connSec = new SqlConnection(conexionDB))
                {
                    if (connSec.State == ConnectionState.Open)
                        connSec.Close();
                    connSec.Open();
                    var dt = new DataTable();
                    var comm = "BUSCAR_INVENTARIO_POR_124";
                    var sqlc = new SqlCommand(comm, connSec);
                    sqlc.CommandType = CommandType.StoredProcedure;
                    sqlc.Parameters.AddWithValue("@FindParm", codigo);
                    sqlc.Parameters.Add("@Datos", SqlDbType.Int, 2);
                    sqlc.Parameters["@Datos"].Direction = ParameterDirection.Output;

                    var da = new SqlDataAdapter(sqlc);
                    sqlc.ExecuteNonQuery();
                    res = Convert.ToBoolean(sqlc.Parameters["@Datos"].Value);

                    connSec.Close();

                    return res;
                }
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("contratoAnulado", ex.Message, ex.StackTrace, "", "facturacion SSAI");
                return res;
            }
        }

        #endregion

        #region PDF y correo
        public static void sendEmail()
        {
            try
            {
                List<string> lstArchivos = new List<string>();
                lstArchivos.Add(@"C:\facturas\factura68641.pdf");

                //creamos nuestro objeto de la clase que hicimos
                //Mail oMail = new Mail("alexander.v211111@gmail.com", "alexander.v211111@gmail.com",
                //                    "Factura", "gracias por su compra", lstArchivos);

                Mail oMail = new Mail("alexander.v211111@gmail.com", "vpena@inglosa.hn",
                                    "Factura", "gracias por su compra", lstArchivos);


                //y enviamos
                if (oMail.enviaMail())
                {
                    Console.Write("se envio el mail");

                }
                else
                {
                    Console.Write("no se envio el mail: " + oMail.error);

                }

            }
            catch (Exception ex)
            {

            }
        }

        public static void generatePdfInvoice(int docentry)
        {
            try
            {
                ReportDocument cryRpt = new ReportDocument();
                CrystalReportViewer crystalReportViewer1 = new CrystalReportViewer();
                var db = "SBO_HERTZ_PRUEBAS";

                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    var row = context.api_configuration.FirstOrDefault();

                    if (row != null)
                    {
                        db = row.CompanyDB;
                    }
                }
                var sAppPath = Environment.CurrentDirectory + @"\reportes\FacturadeVentaHERTZ.rpt";

                //cryRpt.Load(@"C:\Users\alexa\OneDrive\Desktop\SAP\proyecto xml invoice\LectorFacturasSap 2.0\LectorFacturasSap\reportes\FacturadeVentaHERTZ.rpt");
                cryRpt.Load(sAppPath);
                //cryRpt.Load(@"C:\Users\alexa\OneDrive\Desktop\SAP\proyecto xml invoice\LectorFacturasSap 2.0\LectorFacturasSap\reportes\FacturadeVentaHERTZ.rpt");
                cryRpt.DataSourceConnections[0].SetConnection("10.10.2.10", db, "System", "Sap5erver");

                //cryRpt.Load(@"D:\C# Demos\Crystal Reports\CrystalReportDemo\CrystalReportDemo\CrystalReport1.rpt");
                cryRpt.SetParameterValue("UserCode@", "dvelasquez");
                cryRpt.SetParameterValue("Schema@", db);

                //68641
                cryRpt.SetParameterValue("DocKey@", docentry);

                TextObject txt;
                txt = (TextObject)cryRpt.ReportDefinition.ReportObjects["Text43"];
                txt.Text = "ORIGINAL";
                crystalReportViewer1.ReportSource = cryRpt;
                crystalReportViewer1.Refresh();
                cryRpt.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, @"C:\facturas\factura" + docentry.ToString() + ".pdf");

            }
            catch (Exception ex)
            {


            }
        }

        /// <summary>
        /// verifica si el contrato existe el borrador
        /// </summary>
        /// <param name="contractnumber">numero de contrato</param>
        /// <param name="completeFileName">nombre de archivo</param>
        /// <returns>true si existe, false sino existe</returns>
        private static bool contractExistDraft(string contractnumber, string completeFileName)
        {
            var res = false;
            try
            {
                using (var context = new model.Hertz_Projects_DevEntities())
                {
                    //var row = context.draft_header_post.FirstOrDefault(x => x.contract_number == contractnumber);
                    var rowDraft = context.draft_header_post.Where(x => x.contract_number == contractnumber).FirstOrDefault();

                    if (rowDraft != null)
                    {
                        res = true;
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                model.Log.WritelogInvoice("contractExist", ex.Message, ex.StackTrace, completeFileName, locationCodeGlobal);
                return res;
            }
        }
        #endregion

        #region clases
        public class Draft
        {
            //si
            public int DraftId { get; set; }
            //si
            public DateTime date { get; set; }
            //si
            public string CardCode { get; set; }
            //si
            public string Client { get; set; }
            //si
            public string Rtn { get; set; }
            //si para saber si se proceso o no
            public int State { get; set; }
            //si
            public string Contract { get; set; }
            //si
            public int? Serie { get; set; }
            //(Datein-DateOut)/ mismo valor de cantidad del detalle menos para FUEL,DIESEL y DRIVER
            public double daysRent { get; set; }
            //linea de negocio
            public string CCBussinesLine { get; set; }
            //ubicacion de la factura
            public string CCLocation { get; set; }
            //proyecto
            public string project { get; set; }
            //id del vendedor
            public string SellerIdCode { get; set; }
            public string TotalCharges { get; set; }

            public List<DraftDetail> Detail { get; set; }
        }

        public class DraftDetail
        {
            //id autogenerado
            public string DraftDetailId { get; set; }
            //id que amarra el header de la factura
            public int DraftId { get; set; }
            //codigo del tipo de producto de la linea (EA-02385,ECONOMY)
            public string Code { get; set; }
            //formula->totalTPM/(datein-dateout)
            public double Price { get; set; }
            //(datein-dateout)
            public double Quantity { get; set; }
            public int Order { get; set; }

        }
        #endregion

        #region Draft post class for json
        public class draft_header_create
        {
            public string CardCode { get; set; }
            public string DocObjectCode { get; set; }
            public string DocDate { get; set; }
            public Nullable<int> Series { get; set; }
            public string U_RTN { get; set; }
            public string U_CONTRATO { get; set; }
            public string CardName { get; set; }
            public List<draft_detail_create> DocumentLines { get; set; }
        }

        public class draft_detail_create
        {
            public string ItemCode { get; set; }
            public string Quantity { get; set; }
            public string UnitPrice { get; set; }
            public string ProjectCode { get; set; }
            public string CostingCode2 { get; set; }
            public string CostingCode3 { get; set; }
        }
        #endregion
    }
}
