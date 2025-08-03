using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceXmlService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

            var process = new LogicSapInvoice();
            //while (true)
            //{

               process.ProcesFilesFromXML();
            //    //var res = tmp.GetBusinessPartnersByIdV2("CCO-00478", "test");
            //    System.Threading.Thread.Sleep(10000);
            //}

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new HertzInvoiceXmlService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
