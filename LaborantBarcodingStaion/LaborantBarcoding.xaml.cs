﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Patholab_Common;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Patholab_DAL_V1;
using Patholab_XmlService;
using System.Diagnostics;
using Timer = System.Windows.Forms.Timer;
namespace LaborantBarcodingStaion
{
    public partial class LaborantBarcoding : UserControl
    {
        private LSSERVICEPROVIDERLib.INautilusServiceProvider sp;
        private LSSERVICEPROVIDERLib.INautilusProcessXML xmlProcessor;
        private LSSERVICEPROVIDERLib.INautilusDBConnection _ntlsCon;
        private LSExtensionWindowLib.IExtensionWindowSite2 _ntlsSite;
        private LSSERVICEPROVIDERLib.INautilusUser _ntlsUser;
        private string _infoTextBlock_text;
        private int barcodeDefaultLength = 12;
        private int locationOfSlash = 7;
        private int indexOfFirstDot = 10;
        private bool inProcess = false;
        //  public List<GridRow> GridRows { get; set; }
        public ObservableCollection<GridRow> GridRows { get; set; }

        public delegate void UpdateTextCallback(string message, Brush color);
        public delegate void UpdateTextCallback2(string message, Brush color, long? SDG_ID, string entityName, OPERATOR oOperator);
        private DataLayer dal;
        //   private System.Windows.Forms.Integration.ElementHost elementHost1;
        private bool debug;
        private Mutex mutex = new Mutex(false);
        Timer _timerFocus = null;
        Timer timer1 = null;
        public int timerInterval = 300000;



        public LaborantBarcoding(LSSERVICEPROVIDERLib.INautilusServiceProvider sp, LSSERVICEPROVIDERLib.INautilusProcessXML xmlProcessor, LSSERVICEPROVIDERLib.INautilusDBConnection _ntlsCon, LSExtensionWindowLib.IExtensionWindowSite2 _ntlsSite, LSSERVICEPROVIDERLib.INautilusUser _ntlsUser, int timerInterval)
        {
            this.DataContext = this;


            GridRows = new ObservableCollection<GridRow>();


            InitializeComponent();
            this.sp = sp;
            this.dal = dal;
            this.xmlProcessor = xmlProcessor;
            this._ntlsCon = _ntlsCon;
            this._ntlsSite = _ntlsSite;
            this._ntlsUser = _ntlsUser;
            this.timerInterval = timerInterval;

            string role = _ntlsUser.GetRoleName();
            debug = (role.ToUpper() == "DEBUG");
            if (debug) Debugger.Launch();

            this.ScanInput.Focus();
            ScanInput.ForceCursor = true;
            //FirstFocus();
        }

        public void InitializeData()
        {
            dal = new DataLayer();
            dal.Connect(_ntlsCon);


            ScanInput.Focus();
            ScanInput.ForceCursor = true;


            //listdata = new ListDatas(dal);
            timer1 = new Timer
            {
                Interval = timerInterval
            };
            timer1.Enabled = true;
            timer1.Tick += new System.EventHandler(OnTimerEvent);
        }
        private void OnTimerEvent(object sender, EventArgs e)
        {
            if (!inProcess)
            {
                txtInProcess.Visibility = Visibility.Visible;
                dal = new DataLayer();
                dal.Connect(_ntlsCon);
                txtInProcess.Visibility = Visibility.Hidden;

                ScanInput.ForceCursor = true;
                ScanInput.Focus();

            }
        }
        //private void FirstFocus()
        //{
        //    //First focus because nautius's bag
        //    _timerFocus = new Timer { Interval = 1000 };
        //    _timerFocus.Tick += timerFocus_Tick;
        //    ScanInput.Focus();
        //}

        //void timerFocus_Tick(object sender, EventArgs e)
        //{
        //    ScanInput.Focus();
        //    _timerFocus.Stop();

        //  }
        public void Enque(object data)
        {
            inProcess = true;
            mutex.WaitOne();
            try
            {
                RunPrint(data);
            }
            catch (Exception ex)
            {
                Log("שגיאה בהדפסה, במידה והתקלה חוזרת יש לדווח למנהל הטכנולוגי", false);
                Logger.WriteLogFile(ex);

            }
            finally
            {
                mutex.ReleaseMutex();
                inProcess = false;
            }

        }

        private void RunPrint(object data)
        {


            string entityNameAndStation = data as string;
            string eventName = "Print Slide Once";



            //בדוק חוקיות ברקוד
            if (entityNameAndStation.Length < 8)
            {
                Log("הברקוד  '" + entityNameAndStation + "' קצר מדי", false);
                return;
            }

            Log("הברקוד '" + entityNameAndStation.Substring(0, entityNameAndStation.Length - 2) + "' נקרא בעמדה מספר "
                + entityNameAndStation.Substring(entityNameAndStation.Length - 2), true);



            //check that the station is defined in the phrase
            string stationCode = entityNameAndStation.Substring(entityNameAndStation.Length - 2);

            PHRASE_ENTRY station =
             dal.FindBy<PHRASE_ENTRY>(
                 pe => pe.PHRASE_HEADER.NAME == "Laborant Position" && pe.PHRASE_NAME == stationCode)
                .FirstOrDefault();


            if (station == null)
            {
                Log("התחנה עבור קורא ברקוד מספר '" + stationCode + "' לא נמצאה", false);
                return;
            }

            //get the "LABORANT_WORK_USER" record
            U_LABORANT_WORK_USER laborantWorkUser =
                dal.FindBy<U_LABORANT_WORK_USER>
                    (lu => lu.U_STATUS == "O" && lu.U_BARCODE_READER == stationCode)
                   .OrderByDescending(lu => lu.U_STARTED_ON)
                   .FirstOrDefault();
            if (laborantWorkUser == null)
            {
                Log("נראה שאינך רשומ//ה לעמדה זו.בכדי לעבוד יש להירשם לעמדה במסך הרשמת לבורנט. \n  (" +
                    station.PHRASE_NAME + ")", false);
                return;
            }

            //get the entityName and respond to it

            string entityName = entityNameAndStation.Substring(0, entityNameAndStation.Length - 2);

            if (entityName == "#(1-)#")
            {
                #region PrintAgain
                //print last printed slides again
                eventName = "Print Slide";
                Log("נקלט קוד הדפסה חוזרת", true, null, "", laborantWorkUser.OPERATOR);


                U_ALL_BARCODE_SCAN_USER barcodeScanUser =
                    dal.FindBy<U_ALL_BARCODE_SCAN_USER>(bsu => bsu.U_LABORANT_POSITION == stationCode
                        && laborantWorkUser.U_LABORANT == bsu.U_LABORANT
                        && bsu.U_CREATED_ON > DateTime.Today)

                       .OrderByDescending(bsu => bsu.U_CREATED_ON).FirstOrDefault();
                if (barcodeScanUser == null)
                {
                    Log(" לא נמצאו הדפסות קודמות מהיום לעמדה " + stationCode + " ולמשתמש", true, null, "", laborantWorkUser.OPERATOR);
                    return;
                }

                entityName = barcodeScanUser.U_ENTITY_NAME;

                Log("מדפיס שנית", true, null, entityName, laborantWorkUser.OPERATOR);

                #endregion PrintAgain
            }
            else if (
                //check for block / sample name + station 2 digit code 
                //blockNameAndStation.Length != barcodeDefaultLength // barcode is a certain length
                entityNameAndStation.IndexOf('.') != indexOfFirstDot // the first dot is always at the same place
                || entityNameAndStation.LastIndexOf('/') != locationOfSlash //the last slashe is alway in the same locations 
                || entityNameAndStation.IndexOf('/') != locationOfSlash //the first slashe is alway in the same locations (only one)
                || entityNameAndStation.LastIndexOfAny(new char[] { 'B', 'P', 'C' }) != 0) //the name sterts with B/P/C
            {
                Log("הברקוד לא נקלט כראוי,  או שקורא הברקוד לא מוגדר. ", false);
                return;
            }


            {

                //check if barcode fits the specs


                //find the slides 

                ALIQUOT[] slides = null;
                long? sdgId = null;
                //find slides if it is a sample of Pap or Cyto
                if (entityName.LastIndexOf('.') ==
                    indexOfFirstDot && entityName.IndexOfAny(new char[] { 'P', 'C' }) == 0)
                {
                    //sample of 'P' or 'C'
                    SAMPLE sample = dal.FindBy<SAMPLE>(s => s.NAME == entityName.ToUpper()).FirstOrDefault();
                    if (sample != null)
                    {

                        string[] parts = dal.FindBy<U_PARTS>(p => p.U_PARTS_USER.U_STAIN != null && p.U_PARTS_USER.U_PART_TYPE.ToUpper() != "O").Select(x => x.U_PARTS_USER.U_STAIN).ToArray();
                        sdgId = sample.SDG_ID;
                        try
                        {
                            slides = sample.ALIQUOTs.
                                            Where(x =>
                                                  x.ALIQUOT_USER.U_GLASS_TYPE == "S"
                                                  && parts.Contains(x.ALIQUOT_USER.U_COLOR_TYPE)).ToArray();

                        }
                        catch (Exception e)
                        {
                            Log("תקלה בחיפוש דגימה \n" + e.ToString(), false, sdgId, entityName, laborantWorkUser.OPERATOR);
                            return;
                        }
                    }

                }

                //if slides not found for sample, check for block
                if (slides == null)
                {
                    ALIQUOT block = dal.FindBy<ALIQUOT>(a => a.NAME == entityName).SingleOrDefault();
                    if (block == null)
                    {
                        Log("הקסטה או הדגימה לא נמצאו", false, sdgId, entityName, laborantWorkUser.OPERATOR);
                        return;
                    }
                    sdgId = block.SAMPLE.SDG_ID;
                    try
                    {
                        slides = block.ALIQ_FORMULATION_CHILD.Select(x => x.PARENT).ToArray();
                    }
                    catch (Exception e)
                    {
                        Log("לא נמצאו סליידים לבלוק ", false, sdgId, entityName, laborantWorkUser.OPERATOR);
                        return;
                    }
                    if (slides.Count() == 0)
                    {
                        Log("לא נמצאו סליידים לבלוק ", false, sdgId, entityName, laborantWorkUser.OPERATOR);
                        return;
                    }
                }
                if (slides.Count() == 0)
                {
                    Log("לא נמצאו סליידים להדפסה לדגימה ", false, sdgId, entityName, laborantWorkUser.OPERATOR);
                    return;
                }
                //update the scan value in laborant position
                laborantWorkUser.U_TOTAL_NUMBER_BARCODE += 1;
                dal.SaveChanges();

                //create an "all Barcode Scan" record
                U_ALL_BARCODE_SCAN barcodeScan = new U_ALL_BARCODE_SCAN();
                barcodeScan.U_ALL_BARCODE_SCAN_ID = (long)dal.GetNewId("SQ_U_ALL_BARCODE_SCAN");
                barcodeScan.NAME = barcodeScan.U_ALL_BARCODE_SCAN_ID.ToString();
                barcodeScan.VERSION = "1";
                barcodeScan.VERSION_STATUS = "A";
                dal.Add(barcodeScan);

                U_ALL_BARCODE_SCAN_USER barcodeScanUser = new U_ALL_BARCODE_SCAN_USER();
                barcodeScanUser.U_ALL_BARCODE_SCAN_ID = barcodeScan.U_ALL_BARCODE_SCAN_ID;
                barcodeScanUser.U_ENTITY_NAME = entityName;
                barcodeScanUser.U_CREATED_ON = DateTime.Now;
                barcodeScanUser.U_LABORANT = laborantWorkUser.U_LABORANT;
                barcodeScanUser.U_LABORANT_POSITION = station.PHRASE_NAME;

                barcodeScanUser.U_NUMBER_OF_SLIDES = (decimal)slides.Count();
                barcodeScanUser.U_PRINTER = station.PHRASE_INFO;
                barcodeScanUser.U_STATUS = "O";
                dal.Add(barcodeScanUser);
                dal.SaveChanges();


                //find all slides
                foreach (ALIQUOT slide in slides)
                {
                    if (slide.STATUS != "X")
                    {
                        //fire "print Slide" Event
                        FireEventXmlHandler fireEvent = new FireEventXmlHandler(sp);
                        fireEvent.CreateFireEventXml("ALIQUOT", slide.ALIQUOT_ID, eventName);
                        var feres = fireEvent.ProcssXml();
                        if (!feres)
                        {

                            Logger.WriteLogFile("U_PRINTED_ON: " + slide.ALIQUOT_USER.U_PRINTED_ON + " eventName: " + eventName);

                            if (slide.ALIQUOT_USER.U_PRINTED_ON == null && eventName == "Print Slide")
                            {
                                Log("שגיאה בהדפסת הסלייד " + slide.NAME, false, sdgId, entityName,
                                    laborantWorkUser.OPERATOR);
                            }
                            else
                            {
                                Log("הסלייד " + slide.NAME + " הודפס בעבר ולא יודפס שנית", false, sdgId, entityName,
                                    laborantWorkUser.OPERATOR);
                            }

                        }
                        else
                        {
                            Log("סלייד " + slide.NAME + "נשלח להדפסה ", true, sdgId, entityName, laborantWorkUser.OPERATOR);
                            slide.ALIQUOT_USER.U_PRINTED_ON = dal.GetSysdate();
                            dal.SaveChanges();
                        }
                    }


                }
            }

        }

        public void Log(string text, bool sucsess)
        {
            Brush color = sucsess ? Brushes.Green : Brushes.Red;
            InfoTextBlock.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { text, color });
        }
        public void Log(string text, bool sucsess, long? sdgId, string entityName, OPERATOR oOperator)
        {
            Brush color = sucsess ? Brushes.Green : Brushes.Red;
            InfoTextBlock.Dispatcher.Invoke(new UpdateTextCallback2(this.UpdateText2), new object[] { text, color, sdgId, entityName, oOperator });


            dal.InsertToSdgLog(sdgId ?? 0, "LB.P" + (sucsess ? "+" : "-"), (long)_ntlsCon.GetSessionId(),
                               entityName + "," + oOperator.NAME);
        }
        private void UpdateText(string text, Brush color)
        {
            InfoTextBlock.Text = text + "\n" + InfoTextBlock.Text;
            InfoTextBlock.Foreground = color;
            var row = new GridRow();
            row.Message = text;
            row.Status = color.ToString();
            row.RowNum = GridRows.Count + 1;
            GridRows.Add(row);
            GridScanHistory.ScrollIntoView(GridRows[GridRows.Count - 1]);
        }
        private void UpdateText2(string text, Brush color, long? SDG_ID, string entityName, OPERATOR oOperator)
        {
            InfoTextBlock.Text = text + "\n" + InfoTextBlock.Text;
            InfoTextBlock.Foreground = color;
            var row = new GridRow();
            row.EntityName = entityName;
            row.OperatorName = oOperator.NAME;
            row.Message = text;
            row.Status = color.ToString();
            row.RowNum = GridRows.Count + 1;
            GridRows.Add(row);
            GridScanHistory.ScrollIntoView(GridRows[GridRows.Count - 1]);
        }
        private void ScanInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TextBox t = sender as TextBox;
                // create instance of thread, and store it in the t-variable:
                Thread tr = new Thread(new ParameterizedThreadStart(Enque));
                // start the thread using the t-variable:
                string input = t.Text;
                t.Text = "";
                tr.Start(input);

            }
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!ScanInput.IsFocused)
            {
                ScanInput.Focus();
                ScanInput.ForceCursor = true;
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

            ScanInput.Focus();

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            ScanInput.Focus();

        }

        private void ScanInput_GotFocus(object sender, RoutedEventArgs e)
        {
            Patholab_Common.zLang.English();

            //  MessageBox.Show("Got");
        }

        private void ScanInput_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        internal void close()
        {
            timer1.Stop();
            dal.Close();
        }
    }

}
