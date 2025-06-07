
namespace InvoiceXmlService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.InvoiceXmlProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.InvoiceXmlServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // InvoiceXmlProcessInstaller
            // 
            this.InvoiceXmlProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.InvoiceXmlProcessInstaller.Password = null;
            this.InvoiceXmlProcessInstaller.Username = null;
            // 
            // InvoiceXmlServiceInstaller
            // 
            this.InvoiceXmlServiceInstaller.ServiceName = "Hertz Invoice Xml Service";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.InvoiceXmlProcessInstaller,
            this.InvoiceXmlServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller InvoiceXmlProcessInstaller;
        private System.ServiceProcess.ServiceInstaller InvoiceXmlServiceInstaller;
    }
}