using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InvoiceXmlService.model
{
    public static class Log
    {
        //public static void Writelog2(string methodName, string errorDescription, string traceError, string CompleteFileName, string locationcode, string projectName = "LectorXml")
        //{
        //    try
        //    {
        //        var CompleteFileNameWhithoutIp = CompleteFileName.Replace("10.10.1.39", "");
        //        var contractnumber = Regex.Match(CompleteFileNameWhithoutIp, @"\d+").Value;
        //        //var Path.GetFileName(file);
        //        using (var context = new Hertz_Projects_DevEntities())
        //        {
        //            var row = new error_log
        //            {
        //                project = projectName,
        //                method_name = methodName,
        //                trace_error = traceError,
        //                error_description = errorDescription,
        //                file_name = CompleteFileName,
        //                contract_number = contractnumber,
        //                date_error = DateTime.Now,
        //                location_code = locationcode
        //            };

        //            context.error_log.Add(row);
        //            context.SaveChanges();
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        public static void WritelogInvoice(string methodName, string errorDescription, string traceError, string CompleteFileName, string locationcode, string projectName = "Lector xml facturacion")
        {
            try
            {
                var CompleteFileNameWhithoutIp = CompleteFileName.Replace("10.10.1.39", "");
                var contractnumber = Regex.Match(CompleteFileNameWhithoutIp, @"\d+").Value;
                //var Path.GetFileName(file);
                using (var context = new Hertz_Projects_DevEntities())
                {
                    var row = new invoice_error_log
                    {
                        project = projectName,
                        method_name = methodName,
                        trace_error = traceError,
                        error_description = errorDescription,
                        file_name = CompleteFileName,
                        contract_number = contractnumber,
                        date_error = DateTime.Now,
                        location_code = locationcode
                    };

                    context.invoice_error_log.Add(row);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
