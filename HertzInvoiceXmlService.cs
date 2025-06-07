using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceXmlService
{
    public partial class HertzInvoiceXmlService : ServiceBase
    {
        #region declaracion de variables
        public Thread worker = null;
        #endregion
        public HertzInvoiceXmlService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ThreadStart start = new ThreadStart(InicioServicio);
            worker = new Thread(start);
            worker.Start();
        }

        protected override void OnStop()
        {
        }

        #region metodos del servicio
        private void InicioServicio()
        {

            var process = new LogicSapInvoice();

            while (true)
            {
                process.ProcesFilesFromXML();
                //EventoTemporizador();
                Thread.Sleep(5000);
            }

        }


        private void EventoTemporizador()
        {
            try
            {
                //var path = "C:\\test\\test.txt";
                var path = "C:\\testInvoiceService\\test.txt";
                //var path = "C:\\Users\\alexa\\OneDrive\\Desktop\\test";

                //string[] lines = { "old falcon", "deep forest", "golden ring" };
                string text = "hola " + DateTime.Now.ToString() + "\n";

                File.WriteAllText(path, text);

                //MessageBox.Show("hola" + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {

            }
        }


        #endregion
    }
}
