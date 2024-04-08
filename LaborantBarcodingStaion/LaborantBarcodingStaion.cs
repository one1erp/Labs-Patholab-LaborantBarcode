
using System.Drawing;
using System.Threading;
using LSSERVICEPROVIDERLib;
using LSExtensionWindowLib;
using Microsoft.Win32.SafeHandles;



using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;//for debugger :)
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Patholab_Common;
using Patholab_DAL_V1;


namespace LaborantBarcodingStaion
{

    [ComVisible(true)]
    [ProgId("LaborantBarcodingStaion.LaborantBarcodingStaionCls")]

    public partial class LaborantBarcodingStaionCls : UserControl, IExtensionWindow
    {
       #region Private fields
        private INautilusProcessXML xmlProcessor;
        private INautilusUser _ntlsUser;
        private IExtensionWindowSite2 _ntlsSite;
        private INautilusServiceProvider sp;
        private INautilusDBConnection _ntlsCon;

        private DataLayer dal;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        public bool DEBUG;
        public int timerInterval = 300000;

        #endregion
        public LaborantBarcodingStaionCls()
        {
          InitializeComponent();
        }


        public bool CloseQuery()
        {
            w.close();
            return true;
        }

        public WindowRefreshType DataChange()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;

        }

        public WindowButtonsType GetButtons()
        {
            return LSExtensionWindowLib.WindowButtonsType.windowButtonsNone;


        }

        public void Internationalise()
        {

        }
        LaborantBarcoding w;
        public void PreDisplay()
        {
           //if (DEBUG) 
           //   Debugger.Launch();
            xmlProcessor = Utils.GetXmlProcessor(sp);

            _ntlsUser = Utils.GetNautilusUser(sp);

            w = new LaborantBarcoding(sp, xmlProcessor, _ntlsCon, _ntlsSite, _ntlsUser,timerInterval);
            this.elementHost1.Child = w;
            w.InitializeData();
            w.ScanInput.Focus();

        }


        public void RestoreSettings(int hKey)
        {
        }

        public bool SaveData()
        {
            return true;
        }

        public void SaveSettings(int hKey)
        {
        }

        public void SetParameters(string parameters)
        {
            if (parameters != null)
            {
                timerInterval = int.Parse(parameters);
            }
        }

        public void SetServiceProvider(object serviceProvider)
        {
            sp = serviceProvider as NautilusServiceProvider;
            _ntlsCon = Utils.GetNtlsCon(sp);
        }

        public void SetSite(object site)
        {
            _ntlsSite = (IExtensionWindowSite2)site;
            _ntlsSite.SetWindowInternalName("LaborantBarcodingStaion");
            _ntlsSite.SetWindowRegistryName("LaborantBarcodingStaion");
            _ntlsSite.SetWindowTitle("עמדת ברקוד ללבורנט");
        }

        public void Setup()
        {
          
        }
        

       


        public WindowRefreshType ViewRefresh()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;
        }

        public void refresh()
        {
            //throw new NotImplementedException();
        }

   
        private void InitializeData()
        {

          
            //dal = new DataLayer();
            //dal.Connect(_ntlsCon);

            //var sdgn = dal.GetAll<SDG>().Select(x => x.NAME);

            //foreach (var a in sdgn)
            //{
            // //   listBox1.Items.Add(a);
            //}
        }

        private void InitializeComponent()
        {
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 0);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(525, 464);
            //this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = null;
            // 
            // LaborantBarcodingStaionCls
            // 
            this.Controls.Add(this.elementHost1);
            this.Name = "LaborantBarcodingStaionCls";
            this.Size = new System.Drawing.Size(525, 464);
            this.ResumeLayout(false);

        }
    }
}
