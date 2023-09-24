using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MR_8_80_Liters_Mixing_System;
using System.Windows.Input;
using User;
using System.IO;
using System.Globalization;
using System.Threading;

namespace Main
{
    public partial class Main : Form
    {
        #region Declarations
        Dictionary<ChemicalCalculations, double> materialValues = new Dictionary<ChemicalCalculations, double>();
        Dictionary<WeighingScales, string> SocketSettings = new Dictionary<WeighingScales, string>();
        Dictionary<WeighingScales, string> ports = new Dictionary<WeighingScales, string>();

        BackgroundWorker bg = new BackgroundWorker();

        Functions main = new Functions();
        Tolerance tolerance = new Tolerance();
        Calculations calc = new Calculations();
        WeighingScale scale = new WeighingScale();
        Connections socketConn = new Connections();

        System.Windows.Forms.Timer tmrStirring = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer tmrStabilize = new System.Windows.Forms.Timer();

        bool closing = false;

        decimal materialTolerance = 0;

        DateTime endTime;
        DateTime start;

        DateTime lastKey = new DateTime(0);
        List<char> barcode = new List<char>();

        #endregion

        #region Round Corner Edge
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect, // x-coordinate of upper-left corner
            int nTopRect, // y-coordinate of upper-left corner
            int nRightRect, // x-coordinate of lower-right corner
            int nBottomRect, // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
        );
        #endregion
        public Main()
        {
            InitializeComponent();
            //Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            Variables.connString = Configuration.GetConnString("Server", "Location");
            LoadMixer();
            LoadChecker();

            tmrStirring.Interval = 1000;
            tmrStirring.Tick += new EventHandler(tmrStirring_Tick);

            tmrStabilize.Interval = 1000;
            tmrStabilize.Tick += new EventHandler(tmrStabilize_Tick);

            //Start Checking connections
            new Thread(new ThreadStart(StartCheckingConnection)).Start();

            //bg.DoWork += (o, e) =>
            //{
            //    try
            //    {
            //        StartCheckingConnection();
            //    }
            //    catch (Exception er)
            //    {
            //        MessageDisplay.Error(er.Message);
            //        bg.CancelAsync();
            //    }
            //};

            //bg.RunWorkerCompleted += (o, e) =>
            //{
            //    try
            //    {
            //        bg.RunWorkerAsync();
            //    }
            //    catch (Exception er)
            //    {
            //        MessageDisplay.Error(er.Message);
            //    }
            //};

            this.Load += (o, e) =>
            {
                try
                {
                    this.Text = GetVersion();
                    main.WriteLogs("System is starting up");
                    ControlTagging();
                    InitializeVariables();
                    bg.RunWorkerAsync();
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    closing = true;                         //Exiting the program
                    Application.Exit();
                }
            };

            this.FormClosing += (o, e) =>
            {
                if(!closing)
                {
                    
                    if(!string.IsNullOrEmpty(lblBatchNo.Text))
                    {
                        User.UserConfirm frm = new UserConfirm();
                        frm.ShowDialog(this);

                        if (frm.DialogResult == DialogResult.OK)
                        {
                            DB(ProcessM.ForceClosing);
                            closing = true;
                            //this.Close();
                            Environment.Exit(0);
                        }
                        else
                        {
                            frm.Close();
                            e.Cancel = true;
                        }
                    }
                    else
                    {
                        btnEnd.PerformClick();
                        e.Cancel = true;
                    }
                }
                else
                {
                    Environment.Exit(0);
                }

            };

            btnRenew.Click += (o, e) =>
            {
                try
                {
                    //Get Gross Weight from Load Cell
                    string grossVal = scale.GetWeightLoadCell(WeighingScales.MR8B1, WeightType.GROSS);
                    //Test
                    //string grossVal = "0.00";

                    lblRemainingMixQty.Text = grossVal;
                    lblAbsWeightMixTank.Text = grossVal;

                    if (string.IsNullOrEmpty(CheckWeightValues(grossVal)))
                    {
                        Functions.AlertIndicatorWeight(lblRemainingMixQty, false);
                        Functions.AlertIndicatorWeight(lblAbsWeightMixTank, false);

                        DB(ProcessM.Renew);

                        if(!string.IsNullOrEmpty(lblBatchNo.Text))
                        {
                            EnableDisableControls(ProcessM.Renew, false);
                            EnableDisableControls(ProcessM.RenewStart, true);
                            EnableDisableControls(ProcessM.MR8B1_Reset, true);
                            EnableDisableControls(ProcessM.End, false);
                        }
                        else
                        {
                            MessageDisplay.Error("No batch number candidate for main mixing.");
                        }

                    }
                    else
                    {
                        Functions.AlertIndicatorWeight(lblRemainingMixQty, true);
                        Functions.AlertIndicatorWeight(lblAbsWeightMixTank, true);
                    }
                }
                catch(Exception er)
                {
                    main.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }
            };

            cboMixer.SelectedIndexChanged += (o, e) =>
            {
                try
                {
                    string mixer = string.IsNullOrEmpty(cboMixer.Text)  ? string.Empty : GetMixerID(cboMixer.Text);

                    if (!string.IsNullOrEmpty(mixer))
                    {
                        Variables.mixerID = Convert.ToInt32(mixer);
                    }
                }
                catch(Exception er)
                {
                    main.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }
            };

            cboChecker.SelectedIndexChanged += (o, e) =>
            {
                try
                {
                    string checker = string.IsNullOrEmpty(cboChecker.Text) ? string.Empty : GetCheckerID(cboChecker.Text);

                    if (!string.IsNullOrEmpty(checker))
                    {
                        Variables.checkerID = Convert.ToInt32(checker);
                    }
                }
                catch (Exception er)
                {
                    main.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }
            };

            cboShift.SelectedIndexChanged += (o, e) =>
            {
                txtMR8B1Temp.Focus();
            };

            btnMR8B1Reset.Click += (o, e) =>
            {
                try
                {
                    Control[] input = { txtPlanningMixingQty, cboMixer, cboChecker, cboShift, txtMR8B1Temp, txtMR8B1LotNo1 };

                    if (main.CheckResetValidation(ProcessM.MR8B1_Reset, input, txtPlanningMixingQty.Text, cboMixer.Text, 
                        cboChecker.Text, cboShift.Text, txtMR8B1Temp.Text, txtMR8B1LotNo1.Text) == true)
                    {
                        if (main.ToDouble(lblRemainingMixQty.Text) + main.ToDouble(txtPlanningMixingQty.Text) > 150)
                        {
                            MessageDisplay.Error("Remained mixed monomer + planning mixing quantity should not exceed to 150 KG.");
                            return;
                        }

                        //Reset NET weight load cell
                        string resVal = scale.ResetWeightLoadCell(WeighingScales.MR8B1);
                        //string resVal = "0";

                        if (string.IsNullOrEmpty(resVal))
                        {
                            return;
                        }

                        Calc_MR8B1(txtPlanningMixingQty.Text);
                        SetTolerance(lblMR8B1Upper.Text, lblMR8B1Lower.Text);       //Set upper and lower tolerance
                        lblMR8B1BeforeInput.Text = "0.00";

                        //Sending standard value to load cell
                        //string weightVal = main.WeightConversionSendValue(lblMR8B1Standard.Text);

                        //if (scale.SendStdWeight(WeighingScales.MR8B1, weightVal) == false)
                        //{
                        //    return;
                        //}

                        DB(ProcessM.MR8B1_Reset);                                //update database

                        //Enable and disable controls
                        EnableDisableControls(ProcessM.RenewStart, false);
                        EnableDisableControls(ProcessM.MR8B1_Reset, false);
                        EnableDisableControls(ProcessM.MR8B1_Get, true);
                    }
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }

            };

            btnMR8B1Get.Click += (o, e) =>
            {
                try
                {
                    //Get Gross Weight from Load Cell
                    string val = scale.GetWeightLoadCell(WeighingScales.MR8B1, WeightType.NET);
                    string grossVal = scale.GetWeightLoadCell(WeighingScales.MR8B1, WeightType.GROSS);

                    //string val = lblMR8B1Standard.Text;
                    //string val = lblMR8B1AfterInput.Text;
                    //string grossVal = (Convert.ToDouble(lblRemainingMixQty.Text) + Convert.ToDouble(val)).ToString();

                    lblMR8B1AfterInput.Text = val;
                    lblMR8B1ActualInput.Text = grossVal;

                   if (CheckWeightValidationBefore(ProcessM.MR8B1_Get, lblMR8B1AfterInput.Text))
                   {
                        Functions.AlertIndicatorWeight(lblMR8B1AfterInput, false);

                        if (CheckValidWeightInput(CheckWeightValues(lblMR8B1AfterInput.Text)) && main.ToDouble(lblMR8B1ActualInput.Text) > 0)
                        {
                            string nets = (main.ToDouble(lblMR8B1AfterInput.Text) - main.ToDouble(lblMR8B1BeforeInput.Text)).ToString();
                            string gross = (main.ToDouble(lblMR8B1ActualInput.Text) - main.ToDouble(lblAbsWeightMixTank.Text)).ToString();

                            if (main.CheckActualInputWeight(nets, gross) == true)
                            {
                                elblErrorMsg.Text = "";
                                Functions.AlertIndicatorWeight(lblMR8B1ActualInput, false);

                                DB(ProcessM.MR8B1_Get);

                                EnableDisableControls(ProcessM.MR8B1_Get, false);
                                EnableDisableControls(ProcessM.MR8B2_Reset, true);
                            }
                            else
                            {
                                elblErrorMsg.Text = "NET WEIGHT AND GROSS WEIGHT DIFFERENCE IS NOT EQUAL.";
                                Functions.AlertIndicatorWeight(lblMR8B1ActualInput, true);
                            }
                            
                        }
                        else
                        {
                            elblErrorMsg.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                            Functions.AlertIndicatorWeight(lblMR8B1ActualInput, true);
                        }
                    }
                   else
                   {
                        //lblMR8B1AfterInput.Text = val;
                   }
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnMR8B2Reset.Click += (o, e) =>
             {
                 try
                 {
                     Control[] input = { txtMR8B2Temp, txtMR8B2LotNo1 };

                     if (main.CheckResetValidation(ProcessM.MR8B2_Reset, input, txtMR8B2Temp.Text, txtMR8B2LotNo1.Text))
                     {
                         //Reset NET weight load cell
                         string resVal = scale.ResetWeightLoadCell(WeighingScales.MR8B2);
                         //string resVal = "0";

                         if (string.IsNullOrEmpty(resVal))
                         {
                             return;
                         }

                         Calc_MR8B2(txtPlanningMixingQty.Text);
                         SetTolerance(lblMR8B2Upper.Text, lblMR8B2Lower.Text);
                         lblMR8B2BeforeInput.Text = "0.00";

                         //Sending standard value to load cell
                         //string weightVal = main.WeightConversionSendValue(lblMR8B2Standard.Text);

                         //if (scale.SendStdWeight(WeighingScales.MR8B2, weightVal) == false)
                         //{
                         //    return;
                         //}

                         //update database
                         DB(ProcessM.MR8B2_Reset);

                         //Enable and disable controls
                         EnableDisableControls(ProcessM.MR8B2_Reset, false);
                         EnableDisableControls(ProcessM.MR8B2_Get, true);
                     }
                 }
                 catch(Exception er)
                 {
                     MessageDisplay.Error(er.Message);
                     main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                 }
             };

            btnMR8B2Get.Click += (o, e) =>
            {
                try
                {
                    //Get Gross Weight from Load Cell
                    string val = scale.GetWeightLoadCell(WeighingScales.MR8B2, WeightType.NET);
                    string grossVal = scale.GetWeightLoadCell(WeighingScales.MR8B2, WeightType.GROSS);

                    //string val = lblMR8B2Standard.Text;
                    //string val = lblMR8B2AfterInput.Text;
                    //string grossVal = (Convert.ToDouble(lblMR8B1ActualInput.Text) + Convert.ToDouble(val)).ToString();

                    lblMR8B2AfterInput.Text = val;
                    lblMR8B2ActualInput.Text = grossVal;

                    if (CheckWeightValidationBefore(ProcessM.MR8B2_Get, lblMR8B2AfterInput.Text))
                    {
                        Functions.AlertIndicatorWeight(lblMR8B2AfterInput, false);

                        if (CheckValidWeightInput(CheckWeightValues(lblMR8B2AfterInput.Text)) && main.ToDouble(lblMR8B2ActualInput.Text) > 0)
                        {
                            string nets = (main.ToDouble(lblMR8B2AfterInput.Text) - main.ToDouble(lblMR8B2BeforeInput.Text)).ToString();
                            string gross = (main.ToDouble(lblMR8B2ActualInput.Text) - main.ToDouble(lblMR8B1ActualInput.Text)).ToString();

                            if (main.CheckActualInputWeight(nets, gross) == true)
                            {
                                elblErrorMsg.Text = "";
                                Functions.AlertIndicatorWeight(lblMR8B2ActualInput, false);

                                DB(ProcessM.MR8B2_Get);

                                EnableDisableControls(ProcessM.MR8B2_Get, false);
                                EnableDisableControls(ProcessM.MR8A_Reset, true);
                            }
                            else
                            {
                                elblErrorMsg.Text = "NET WEIGHT AND GROSS WEIGHT DIFFERENCE IS NOT EQUAL.";
                                Functions.AlertIndicatorWeight(lblMR8B2ActualInput, true);
                            }
                        }
                        else
                        {
                            elblErrorMsg.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                            Functions.AlertIndicatorWeight(lblMR8B2ActualInput, true);
                        }                      
                    }
                    else
                    {
                        lblMR8B2AfterInput.Text = val;
                    }

                }
                catch (Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnMR8AReset.Click += (o, e) =>
            {
                try
                {
                    Control[] input = { txtMR8ATemp, txtMR8ALotNo1 };

                    if (main.CheckResetValidation(ProcessM.MR8A_Reset, input, txtMR8ATemp.Text, txtMR8ALotNo1.Text) == true)
                    {
                        //Reset NET weight load cell
                        string resVal = scale.ResetWeightLoadCell(WeighingScales.MR8A);
                        //string resVal = "0";

                        if (string.IsNullOrEmpty(resVal))
                        {
                            return;
                        }

                        Calc_MR8A(txtPlanningMixingQty.Text);
                        SetTolerance(lblMR8AUpper.Text, lblMR8ALower.Text);
                        lblMR8ABeforeInput.Text = "0.00";

                        //Sending standard value to load cell
                        //string weightVal = main.WeightConversionSendValue(lblMR8AStandard.Text);

                        //if (scale.SendStdWeight(WeighingScales.MR8A, weightVal) == false)
                        //{
                        //    return;
                        //}

                        //update database
                        DB(ProcessM.MR8A_Reset);

                        //Enable and disable controls
                        EnableDisableControls(ProcessM.MR8A_Reset, false);
                        EnableDisableControls(ProcessM.MR8A_Get, true);
                    }
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnMR8AGet.Click += (o, e) =>
             {
                 try
                 {
                     //Get Gross Weight from Load Cell
                     string val = scale.GetWeightLoadCell(WeighingScales.MR8B2, WeightType.NET);
                     string grossVal = scale.GetWeightLoadCell(WeighingScales.MR8B2, WeightType.GROSS);

                     //string val = lblMR8AStandard.Text;
                     //string val = lblMR8AAfterInput.Text;
                     //string grossVal = (Convert.ToDouble(lblMR8B2ActualInput.Text) + Convert.ToDouble(val)).ToString();

                     lblMR8AAfterInput.Text = val;
                     lblMR8AActualInput.Text = grossVal;

                     if (CheckWeightValidationBefore(ProcessM.MR8A_Get, lblMR8AAfterInput.Text))
                     {
                         Functions.AlertIndicatorWeight(lblMR8B2AfterInput, false);

                         if (CheckValidWeightInput(CheckWeightValues(grossVal)) && main.ToDouble(lblMR8AActualInput.Text) > 0)
                         {
                             string nets = (main.ToDouble(lblMR8AAfterInput.Text) - main.ToDouble(lblMR8ABeforeInput.Text)).ToString();
                             string gross = (main.ToDouble(lblMR8AActualInput.Text) - main.ToDouble(lblMR8B2ActualInput.Text)).ToString();

                             if (main.CheckActualInputWeight(nets, gross) == true)
                             {
                                 elblErrorMsg.Text = "";
                                 Functions.AlertIndicatorWeight(lblMR8AActualInput, false);

                                 DB(ProcessM.MR8A_Get);

                                 EnableDisableControls(ProcessM.MR8A_Get, false);
                                 EnableDisableControls(ProcessM.MRReleasing_Reset, true);
                             }
                             else
                             {
                                 elblErrorMsg.Text = "NET WEIGHT AND GROSS WEIGHT DIFFERENCE IS NOT EQUAL.";
                                 Functions.AlertIndicatorWeight(lblMR8AActualInput, true);
                             }
                         }
                         else
                         {
                             elblErrorMsg.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                             Functions.AlertIndicatorWeight(lblMR8AActualInput, true);
                         }
                     }
                     
                 }
                 catch (Exception er)
                 {
                     MessageDisplay.Error(er.Message);
                     main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                 }
             };

            btnMRRelReset.Click += (o, e) =>
            {
                try
                {
                    TextBox[] input = { txtMRRelLotNo1 };

                    if (main.CheckResetValidation(ProcessM.MRReleasing_Reset, input, txtMRRelLotNo1.Text) == true)
                    {

                        //string mrRelReset = "0";
                        string mrRelReset = scale.WeightReset(WeighingScales.MR_RELEASING);

                        if (string.IsNullOrEmpty(mrRelReset))
                        {
                            return;
                        }

                        Calc_MRReleasing(txtPlanningMixingQty.Text);
                        SetTolerance(lblMRRelUpper.Text, lblMRRelLower.Text);

                        //update database
                        DB(ProcessM.MRReleasing_Reset);

                        //Enable and disable controls
                        EnableDisableControls(ProcessM.MRReleasing_Reset, false);
                        EnableDisableControls(ProcessM.MRReleasing_GetBefore, true);
                    }
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnMRRelBeforeInput.Click += (o, e) =>
            {
                try
                {
                    //Test
                    //string val = lblMRRelStandard.Text;
                    //string val = lblMRRelBeforeInput.Text;
                    string val = scale.GetWeight(WeighingScales.MR_RELEASING);

                    lblMRRelBeforeInput.Text = val;

                    if (CheckWeightValidationBefore(ProcessM.MRReleasing_GetBefore, lblMRRelBeforeInput.Text))
                    {
                    
                        Functions.AlertIndicatorWeight(lblMRRelBeforeInput, false);

                        DB(ProcessM.MRReleasing_GetBefore);

                        EnableDisableControls(ProcessM.MRReleasing_GetBefore, false);
                        EnableDisableControls(ProcessM.MRReleasing_GetAfter, true);
                    }
                    else
                    {
                        Functions.AlertIndicatorWeight(lblMRRelBeforeInput, true);
                    }
                }
                catch (Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnMRRelAfterInput.Click += (o, e) =>
            {
                try
                {
                    //test
                    //string val = "0";
                    //string val = lblMRRelAfterInput.Text;
                    string val = scale.GetWeight(WeighingScales.MR_RELEASING);

                    lblMRRelAfterInput.Text = val;

                    Functions.AlertIndicatorWeight(lblMRRelAfterInput, false);

                    if (CheckWeightValidationAfter(ProcessM.MRReleasing_GetAfter, lblMRRelAfterInput.Text))
                    {
                        //Get Gross Weight from load cell
                        string grossVal = scale.GetWeightLoadCell(WeighingScales.MR8B1, WeightType.GROSS);

                        //test
                        //string grossVal = (main.ToDouble(lblMR8AActualInput.Text) + (main.ToDouble(lblMRRelActualInput.Text)/1000.00)).ToString("N2");

                        lblPesMatWeight.Text = grossVal;

                        if (CheckValidWeightInput(CheckWeightValues(grossVal)))
                        {
                            Functions.AlertIndicatorWeight(lblPesMatWeight, false);

                            DB(ProcessM.MRReleasing_GetAfter);

                            EnableDisableControls(ProcessM.MRReleasing_GetAfter, false);
                            EnableDisableControls(ProcessM.CalcStirSpeed, true);
                            EnableDisableControls(ProcessM.End, false);
                            EnableDisableControls(ProcessM.Return, false);
                        }
                        else
                        {
                            Functions.AlertIndicatorWeight(lblPesMatWeight, true);
                        }
                        
                    }
                    else
                    {
                        lblMRRelAfterInput.Text = val;
                    }
                }
                catch (Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnCalcStirring.Click += (o, e) =>
            {
                try
                {
                    string[] stirSpeed = calc.GetStirringSpeedMainTank(txtPlanningMixingQty.Text);

                    lblMinStirSpeed.Text = stirSpeed[0];
                    lblTargetStirSpeed.Text = stirSpeed[1];

                    EnableDisableControls(ProcessM.CalcStirSpeed, false);
                    EnableDisableControls(ProcessM.Stirring, true);
                }
                catch (Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnStirringStart.Click += (o, e) =>
            {
                try
                {
                   if(Functions.CheckNotNullNoMessage(txtStirringSpeed.Text))
                    {
                        if (Functions.CheckWinthinTolerance(main.ToDouble(txtStirringSpeed.Text), main.ToDouble(lblMinStirSpeed.Text), main.ToDouble(lblTargetStirSpeed.Text)))
                        {
                            Variables.minutes = 60;
                            Variables.seconds = 0;
                            //DB(ProcessM.Stirring);

                            tmrStirring.Start();

                            EnableDisableControls(ProcessM.Stirring, false);
                        }
                        else
                        {
                            MessageDisplay.Error("Stirring speed error!\nNote: " + lblMinStirSpeed.Text + " - " + lblTargetStirSpeed.Text + " only acceptable.");
                            //Functions.AlertIndicatorInput(txtStirringSpeed, true);
                            main.Highlight(txtStirringSpeed);
                        }
                    }
                   else
                    {
                        MessageDisplay.Error("Please input stirring speed.");
                        //Functions.AlertIndicatorInput(txtStirringSpeed, true);
                        main.Highlight(txtStirringSpeed);
                    }
                }
                catch (Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - timer stirring" + lblBatchNo.Text);
                    EnableDisableControls(ProcessM.Stirring, true);
                }
            };

            btnStabilize.Click += (o, e) =>
            {
                try
                {
                    start = DateTime.UtcNow;
                    endTime = start.AddMinutes(2.01);
                    btnStabilize.Text = "02:00";
                    EnableDisableControls(ProcessM.Stabilizing, false);
                    tmrStabilize.Start();
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                    EnableDisableControls(ProcessM.Stabilizing, true);
                }
            };

            btnNextBatch.Click += (o, e) =>
            {
                try
                {
                    //DB(ProcessM.NextBatch);
                    if(CheckMixingStatus(lblBatchNo.Text) == "2")
                    {

                        DialogResult confirm = MessageDisplay.Question(@"THIS BATCH NUMBER WILL CUT THE TRANSACTION IN SUBDIVISION. ARE YOU SURE TO PROCEED NEXT BATCH?");

                        if(confirm == DialogResult.Yes)
                        {
                            Functions.ClearData(this, Mixing.MAIN);
                            EnableDisableControls(ProcessM.NextBatch, false);
                            EnableDisableControls(ProcessM.Renew, true);
                            EnableDisableControls(ProcessM.End, true);
                        }

                    }
                    else
                    {
                        MessageDisplay.Error("This batch number is not yet finished in subdivision.");
                    }
                    
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnReturn.Click += (o, e) =>
            {
                try
                {
                    if(!string.IsNullOrEmpty(lblBatchNo.Text))
                    {
                        if (string.IsNullOrEmpty(lblMR8B1AfterInput.Text))
                        {
                            DB(ProcessM.Return);
                            Functions.ClearData(this, Mixing.MAIN);

                            InitializeControls();
                        }
                        else
                        {
                            User.UserConfirm frm = new UserConfirm();
                            frm.ShowDialog(this);

                            if (frm.DialogResult == DialogResult.OK)
                            {
                                DB(ProcessM.Return);
                                Functions.ClearData(this, Mixing.MAIN);

                                InitializeControls();
                            }
                            else
                            {
                                frm.Close();
                            }
                        }
                    }
                    else
                    {
                        MessageDisplay.Error("Nothing to return");
                    }
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                    main.WriteLogs(er.Message + " - " + lblBatchNo.Text);
                }
            };

            btnEnd.Click += (o, e) =>
            {
                if (MessageDisplay.Question("Are you sure you want to close the program?") == DialogResult.Yes)
                {
                    closing = true;
                    this.Close();
                }
            };

        }

        public void InitializeVariables()
        {
            try
            {
                using (DAL dal = new DAL())
                {
                    materialValues = dal.GetCalculations(new ParameterData
                    {
                        sqlQuery = Query.GetCalculations()
                    });

                    Variables.mr8B1 = materialValues[ChemicalCalculations.MR8B1_R];
                    Variables.mr8B2 = materialValues[ChemicalCalculations.MR8B2_R];
                    Variables.mr8A = materialValues[ChemicalCalculations.MR8A_R];
                    Variables.mrReleasing = materialValues[ChemicalCalculations.MRREL_R];

                    SocketSettings = Configuration.GetSocket("Socket");

                    Variables.portMR8B1 = SocketSettings[WeighingScales.MR8B1];
                    Variables.portMR8B2 = SocketSettings[WeighingScales.MR8B2];
                    Variables.portMR8A = SocketSettings[WeighingScales.MR8A];
                    Variables.portMRReleasing = SocketSettings[WeighingScales.MR_RELEASING];

                    Variables.ip = SocketSettings[WeighingScales.MOXA];

                    Variables.setupValue = Configuration.GetSettingData("InitialValue", "Port");
                }
            }
            catch
            {
                throw;
            }
        }

        private void LoadMixer()
        {
            try
            {
                using (DAL dal = new DAL())
                {
                    cboMixer.DataSource = dal.GetDataDT(new ParameterData
                    {
                        sqlQuery = Query.LoadMixer()
                    });
                }

                cboMixer.DisplayMember = "DisplayName";
                cboMixer.SelectedIndex = -1;
            }
            catch
            {
                throw;
            }
        }

        private void LoadChecker()
        {
            try
            {
                using (DAL dal = new DAL())
                {
                    cboChecker.DataSource = dal.GetDataDT(new ParameterData
                    {
                        sqlQuery = Query.LoadChecker()
                    });
                }

                cboChecker.DisplayMember = "DisplayName";
                cboChecker.SelectedIndex = -1;
            }
            catch
            {
                throw;
            }
        }

        private string GetMixerID(string mixer)
        {
            try
            {
                string id = string.Empty;

                using (DAL dal = new DAL())
                {
                    id = dal.GetSingleData(new ParameterData
                    {
                        sqlQuery = Query.GetMixerID(cboMixer.Text)
                    });
                }

                return id;
            }
            catch
            {
                throw;
            }
        }

        private string GetCheckerID(string checker)
        {
            try
            {
                string id = string.Empty;

                using (DAL dal = new DAL())
                {
                    id = dal.GetSingleData(new ParameterData
                    {
                        sqlQuery = Query.GetCheckerID(cboChecker.Text)
                    });
                }

                return id;
            }
            catch
            {
                throw;
            }
        }

        private bool CheckWeightValidationBefore(ProcessM p, string weight)
        {
            bool stat = false;

            if (CheckValidWeightInput(CheckWeightValues(weight)))
            {
                tolerance.weightBefore = weight;

                if (!tolerance.WeightBeforeOver)
                {
                    elblErrorMsg.Text = "";
                    switch (p)
                    {
                        case ProcessM.MR8B1_Get:
                            Functions.AlertIndicatorWeight(lblMR8B1AfterInput, false);
                            break;
                        case ProcessM.MR8B2_Get:
                            Functions.AlertIndicatorWeight(lblMR8B2AfterInput, false);
                            break;
                        case ProcessM.MR8A_Get:
                            Functions.AlertIndicatorWeight(lblMR8AAfterInput, false);
                            break;
                        case ProcessM.MRReleasing_GetBefore:
                            Functions.AlertIndicatorWeight(lblMRRelBeforeInput, false);
                            break;
                    }

                    stat = true;

                }
                else
                {
                    elblErrorMsg.Text = "WEIGHT IS OUTSIDE TOLERANCE RANGE.";

                    switch (p)
                    {
                        case ProcessM.MR8B1_Get:
                            Functions.AlertIndicatorWeight(lblMR8B1AfterInput, true);
                            break;
                        case ProcessM.MR8B2_Get:
                            Functions.AlertIndicatorWeight(lblMR8B2AfterInput, true);
                            break;
                        case ProcessM.MR8A_Get:
                            Functions.AlertIndicatorWeight(lblMR8AAfterInput, true);
                            break;
                        case ProcessM.MRReleasing_GetBefore:
                            Functions.AlertIndicatorWeight(lblMRRelBeforeInput, true);
                            break;
                    }
                }
            }
            else
            {
                elblErrorMsg.Text = "INVALID WEIGHT VALUES. PLEASE TRY AGAIN.";

                switch (p)
                {
                    case ProcessM.MR8B1_Get:
                        Functions.AlertIndicatorWeight(lblMR8B1AfterInput, true);
                        break;
                    case ProcessM.MR8B2_Get:
                        Functions.AlertIndicatorWeight(lblMR8B2AfterInput, true);
                        break;
                    case ProcessM.MR8A_Get:
                        Functions.AlertIndicatorWeight(lblMR8AAfterInput, true);
                        break;
                    case ProcessM.MRReleasing_GetBefore:
                        Functions.AlertIndicatorWeight(lblMRRelBeforeInput, true);
                        break;
                }
            }

            return stat;
        }

        private bool CheckWeightValidationAfter(ProcessM p, string weight)
        {
            bool stat = false;

            if (CheckValidWeightInput(CheckWeightValues(weight)))
            {
                tolerance.weightAfter = weight;

                lblMRRelActualInput.Text = tolerance.Weight.ToString();

                if (!tolerance.WeightOver)
                {
                    elblErrorMsg.Text = "";
                    Functions.AlertIndicatorWeight(lblMRRelAfterInput, false);
                    Functions.AlertIndicatorWeight(lblMRRelActualInput, false);

                    stat = true;
                }
                else
                {
                    elblErrorMsg.Text = "WEIGHT IS OUTSIDE TOLERANCE RANGE.";
                    Functions.AlertIndicatorWeight(lblMRRelActualInput, true);
                }
            }
            else
            {
                elblErrorMsg.Text = "INVALID WEIGHT VALUES. PLEASE TRY AGAIN.";
                Functions.AlertIndicatorWeight(lblMRRelAfterInput, true);
            }

            return stat;
        }

        private void tmrStirring_Tick(object sender, EventArgs e)
        {
            try
            {
                CountDownTimer();

                //TimeSpan remainingTime = endTime - DateTime.UtcNow;
                //if(remainingTime < TimeSpan.Zero)
                //{

                //}
                //else
                //{
                //    lblStirCountDown.Text = string.Format(@"{0:D2}:{1:D2}", remainingTime.Minutes, remainingTime.Seconds);
                //}
            }
            catch (Exception er)
            {
                tmrStirring.Stop();
                MessageDisplay.Error(er.Message);
                main.WriteLogs(er.Message + " - timer stirring");
                EnableDisableControls(ProcessM.Stirring, true);
            }

        }

        private void tmrStabilize_Tick(object sender, EventArgs e)
        {
            try
            {
                TimeSpan remainingTime = endTime - DateTime.UtcNow;
                if(remainingTime < TimeSpan.Zero)
                {
                    tmrStabilize.Stop();
                    DB(ProcessM.NextBatch);
                    EnableDisableControls(ProcessM.NextBatch, true);
                }
                else
                {
                    btnStabilize.Text = string.Format("{0:mm\\:ss}", remainingTime);
                }
            }
            catch(Exception er)
            {
                tmrStabilize.Stop();
                MessageDisplay.Error(er.Message);
                main.WriteLogs(er.Message + " - timer stabilize");
                EnableDisableControls(ProcessM.Stabilizing, true);
            }

        }

        private void CountDownTimer()
        {
            try
            {
                if (Variables.minutes == 0 && Variables.seconds == 0)
                {
                    tmrStirring.Stop();

                     DB(ProcessM.Stirring);
                    //Update
                    //DB(ProcessM.NextBatch);
                    //EnableDisable controls
                    //EnableDisableControls(ProcessM.NextBatch, true);
                    EnableDisableControls(ProcessM.Stabilizing, true);
                }
                else
                {
                    if (Variables.seconds == 0)
                    {
                        Variables.seconds = 59;
                        Variables.minutes -= 1;
                    }
                    else
                    {
                        Variables.seconds -= 1;
                    }
                }

                lblStirCountDown.Text = Variables.minutes.ToString("00") + ":" + Variables.seconds.ToString("00");
            }
            catch
            {
                throw;
            }
        }

        private void SetTolerance(string upperLimit, string lowerLimit)
        {
            tolerance.upperLimit = upperLimit;
            tolerance.lowerLimit = lowerLimit;
        }

        private string CheckWeightValues(string weight)
        {
            double i;
            string val = "";

            if (!double.TryParse(weight, out i))
            {
                val = "Error";
            }

            return val;
        }

        private bool CheckValidWeightInput(string input)
        {
            bool stat = false;

            if(string.IsNullOrWhiteSpace(input)) { stat = true; }

            return stat;
        }


        //Controls system flow of the system
        private void ControlTagging()
        {
            this.Controls.OfType<Control>().ToList().ForEach(c =>
            {

                c.KeyPress += (o, e) =>
                {
                    if(c.Name.Equals("txtPlanningMixingQty"))
                    {
                        main.NumberOnlyNoSpecial(txtPlanningMixingQty.Text, e);
                    }

                    else if ((c.Name.Contains("txt") && c.Name.Contains("Temp")) || (c.Name.Equals("txtStirringSpeed")))
                    {
                        main.AcceptNumOnly(c, e);
                    }


                    if(c.Name.EndsWith("LotNo1") || c.Name.EndsWith("LotNo2"))
                    {

                        TimeSpan elapsed = DateTime.Now - lastKey;

                        if(elapsed.TotalMilliseconds > 100)
                        {
                            barcode.Clear();
                        }


                        barcode.Add(e.KeyChar);
                        lastKey = DateTime.Now;

                        if(e.KeyChar == 13 && barcode.Count > 0)
                        {
                            string bc = new string(barcode.ToArray()).ToUpper();

                            if(c.Name.EndsWith("LotNo1") & !string.IsNullOrEmpty(bc))
                            {
                                string newBC = bc.Replace("\r", "");

                                if (c.Tag.ToString().Split('_').First() == newBC)
                                {
                                    c.Text = GetLot(1, newBC);
                                    Functions.AlertIndicatorInput(c, false);
                                    GetNextControl(c, true).Focus();
                                }
                                else
                                {
                                    Functions.AlertIndicatorInput(c, true);
                                }
                            } 
                            else if (c.Name.EndsWith("LotNo2") & !string.IsNullOrEmpty(bc))
                            {
                                string newBC = bc.Replace("\r", "");

                                if (c.Tag.ToString().Split('_').First() == newBC)
                                {
                                    c.Text = GetLot(2, newBC);
                                    Functions.AlertIndicatorInput(c, false);
                                    GetNextControl(c, true).Focus();
                                }
                                else
                                {
                                    Functions.AlertIndicatorInput(c, true);
                                }
                            }
                        }
                        
                    }
                };


                c.LostFocus += (o, e) =>
                {
                    if(c.Name.StartsWith("txt"))
                    {
                        c.BackColor = Color.White;

                        if (c.Name.Equals("txtTankTemp"))
                        {
                            double val = 0;

                            if (double.TryParse(c.Text, out val))
                            {
                                c.Text = string.Format("N2");
                            }
                            else
                            {
                                c.Text = val.ToString("N2");
                            }
                        }
                    }
                    else if (c.Name.StartsWith("lbl") && c.Name.EndsWith("Input"))
                    {
                        c.BackColor = Color.Moccasin;
                    }
                    else
                    {
                        c.BackColor = SystemColors.Control;
                    }

                };

                c.GotFocus += (o, e) =>
                {
                    if(c is ComboBox || c.BackColor == Color.Red || c.Name.Equals("btnReturn") || c.Name.Equals("btnEnd") || c.Name.StartsWith("lbl"))
                    {
                        
                    }
                    else if (c is TextBox || c is Button)
                        c.BackColor = Color.Orange;

                };

                c.KeyDown += (o, e) =>
                {
                    if(e.KeyCode.Equals(Keys.Enter))
                    {
                        
                        if(c != null)
                        {
                            if ((c.Name.StartsWith("cbo") && c.Name.EndsWith("er")) || c.Name.Equals("cboShift"))
                            {
                                GetNextControl(c, true).Focus();
                            }

                            if (c.Name.Equals("txtPlanningMixingQty"))
                            {
                                if (Functions.CheckNotNullNoMessage(txtPlanningMixingQty.Text))
                                {
                                    if (Functions.CheckWinthinTolerance(Convert.ToDouble(txtPlanningMixingQty.Text), 35, 135) == true)
                                    {
                                        Functions.AlertIndicatorInput(txtPlanningMixingQty, true);
                                        GetNextControl(c, true).Focus();
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Mixing quantity error!\nNote 35 - 135 only accepttable.");
                                        Functions.AlertIndicatorInput(txtPlanningMixingQty, true);
                                        main.Highlight(txtPlanningMixingQty);
                                    }
                                }
                                else
                                {
                                    MessageDisplay.Error("Please input mixing quantity.");
                                    txtPlanningMixingQty.Focus();
                                }
                            }
                            else if (c.Name.Equals("txtMR8B1Temp"))
                            {
                                if (Functions.CheckNotNullNoMessage(txtMR8B1Temp.Text))
                                {
                                    if (!string.IsNullOrEmpty(main.CheckInputValues(txtMR8B1Temp.Text)))
                                    {
                                        Functions.AlertIndicatorInput(txtMR8B1Temp, true);
                                    }
                                    else if (Functions.CheckWinthinTolerance(Convert.ToDouble(txtMR8B1Temp.Text), 20, 27) == true)
                                    {
                                        Functions.AlertIndicatorInput(txtMR8B1Temp, false);
                                        GetNextControl(c, true).Focus();
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Temp error!\nNote 20 - 27 only accepttable.");
                                        Functions.AlertIndicatorInput(txtMR8B1Temp, true);
                                        main.Highlight(txtMR8B1Temp);
                                    }
                                }
                                else
                                {
                                    MessageDisplay.Error("Please input MR-8 B1 temp.");
                                    Functions.AlertIndicatorInput(txtMR8B1Temp, true);
                                    txtMR8B1Temp.Focus();
                                }
                            }
                            else if (c.Name.Equals("txtMR8B2Temp"))
                            {
                                if (Functions.CheckNotNullNoMessage(txtMR8B2Temp.Text))
                                {
                                    if (!string.IsNullOrEmpty(main.CheckInputValues(txtMR8B2Temp.Text)))
                                    {
                                        Functions.AlertIndicatorInput(txtMR8B2Temp, true);
                                    }
                                    else if (Functions.CheckWinthinTolerance(Convert.ToDouble(txtMR8B2Temp.Text), 20, 27) == true)
                                    {
                                        Functions.AlertIndicatorInput(txtMR8B2Temp, false);
                                        GetNextControl(c, true).Focus();
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Temp error!\nNote 20 - 27 only accepttable.");
                                        Functions.AlertIndicatorInput(txtMR8B2Temp, true);
                                        main.Highlight(txtMR8B2Temp);
                                    }
                                }
                                else
                                {
                                    MessageDisplay.Error("Please input MR-8 B2 temp.");
                                    Functions.AlertIndicatorInput(txtMR8B2Temp, true);
                                    txtMR8B2Temp.Focus();
                                }
                            }
                            else if (c.Name.Equals("txtMR8ATemp"))
                            {
                                if (Functions.CheckNotNullNoMessage(txtMR8ATemp.Text))
                                {
                                    if (!string.IsNullOrEmpty(main.CheckInputValues(txtMR8ATemp.Text)))
                                    {
                                        Functions.AlertIndicatorInput(txtMR8ATemp, true);
                                    }
                                    else if (Functions.CheckWinthinTolerance(Convert.ToDouble(txtMR8ATemp.Text), 20, 27) == true)
                                    {
                                        Functions.AlertIndicatorInput(txtMR8ATemp, false);
                                        GetNextControl(c, true).Focus();
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Temp error!\nNote 20 - 27 only accepttable.");
                                        Functions.AlertIndicatorInput(txtMR8ATemp, true);
                                        main.Highlight(txtMR8ATemp);
                                    }
                                }
                                else
                                {
                                    MessageDisplay.Error("Please input MR-8 A temp.");
                                    Functions.AlertIndicatorInput(txtMR8ATemp, true);
                                    txtMR8ATemp.Focus();
                                }
                            }
                            else if (c.Name.EndsWith("LotNo1") | c.Name.EndsWith("LotNo2"))
                            {
                                //
                            }
                            else
                            {
                                GetNextControl(c, true).Focus();
                            }

                        }

                    }

                };
            });
        }

        //Enable/Disable controls via Tag property
        private void MultipleEnableDisableControls(string tag,bool stat)
        {
            foreach (Control c in this.Controls)
            {
                if (c.Tag != null)
                {

                    if (c.Tag.Equals(tag))
                    {

                        c.Enabled = stat;
                    }
                }
            }
        }

        private void EnableDisableControls(ProcessM p, bool stat)
        {
            switch (p)
            {
                case ProcessM.Renew:
                    btnRenew.Enabled = stat;
                    break;

                case ProcessM.RenewStart:
                    MultipleEnableDisableControls("RenewStart", stat);

                    if (stat) { txtPlanningMixingQty.Focus(); }
                    break;

                case ProcessM.MR8B1_Reset:
                    MultipleEnableDisableControls("MR8B1Reset", stat);
                    MultipleEnableDisableControls("MR8-B1_MR8B1Reset", stat);
                    break;

                case ProcessM.MR8B1_Get:
                    btnMR8B1Get.Enabled = stat;

                    if(stat) { btnMR8B1Get.Focus(); }
                    break;

                case ProcessM.MR8B2_Reset:
                    MultipleEnableDisableControls("MR8B2Reset", stat);
                    MultipleEnableDisableControls("MR8-B2_MR8B2Reset", stat);

                    if (stat) { txtMR8B2Temp.Focus(); }
                    break;

                case ProcessM.MR8B2_Get:
                    btnMR8B2Get.Enabled = stat;

                    if (stat) { btnMR8B2Get.Focus(); }
                    break;

                case ProcessM.MR8A_Reset:
                    MultipleEnableDisableControls("MR8AReset", stat);
                    MultipleEnableDisableControls("MR8-A_MR8AReset", stat);

                    if (stat) { txtMR8ATemp.Focus(); }
                    break;

                case ProcessM.MR8A_Get:
                    btnMR8AGet.Enabled = stat;

                    if(stat) { btnMR8AGet.Focus(); }
                    break;

                case ProcessM.MRReleasing_Reset:
                    MultipleEnableDisableControls("MRRelReset", stat);
                    MultipleEnableDisableControls("MR-RELEASING_MRRelReset", stat);

                    if (stat) { txtMRRelLotNo1.Focus(); }
                    break;

                case ProcessM.MRReleasing_GetBefore:
                    btnMRRelBeforeInput.Enabled = stat;

                    if(stat) { btnMRRelBeforeInput.Focus(); }
                    break;

                case ProcessM.MRReleasing_GetAfter:
                    btnMRRelAfterInput.Enabled = stat;

                    if(stat) { btnMRRelAfterInput.Focus(); }
                    break;

                case ProcessM.CalcStirSpeed:
                    btnCalcStirring.Enabled = stat;

                    if(stat) { btnCalcStirring.Focus(); }
                    break;

                case ProcessM.Stirring:
                    MultipleEnableDisableControls("Stirring", stat);
                    if (stat) { txtStirringSpeed.Focus();  }
                    break;

                case ProcessM.Stabilizing:
                    btnStabilize.Enabled = stat;
                    if (stat) { btnStabilize.Focus(); }
                    break;

                case ProcessM.Return:
                    btnReturn.Enabled = stat;
                    break;

                case ProcessM.End:
                    btnEnd.Enabled = stat;
                    break;

                case ProcessM.NextBatch:
                    btnNextBatch.Enabled = stat;
                    if(stat) { btnNextBatch.Focus(); }
                    break;
            }
        }

        private void  Calc_MR8B1(string qty)
        {
            decimal std = (decimal)calc.ComputeStdValues(Chemicals.MR8B1, main.ToDouble(qty));
            materialTolerance = (decimal)materialValues[ChemicalCalculations.MR8B1_TOL];

            lblMR8B1Standard.Text = Math.Round(std, 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMR8B1Upper.Text = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMR8B1Lower.Text = Math.Round((std * (1 - materialTolerance)), 2, MidpointRounding.AwayFromZero).ToString("N2");

        }

        private void Calc_MR8B2(string qty)
        {
            decimal std = (decimal)calc.ComputeStdValues(Chemicals.MR8B2, main.ToDouble(qty));
            materialTolerance = (decimal)materialValues[ChemicalCalculations.MR8B2_TOL];

            lblMR8B2Standard.Text = Math.Round(std, 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMR8B2Upper.Text = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMR8B2Lower.Text = Math.Round((std * (1 - materialTolerance)), 2, MidpointRounding.AwayFromZero).ToString("N2");
        }

        private void Calc_MR8A(string qty)
        {
            decimal std = (decimal)calc.ComputeStdValues(Chemicals.MR8A, main.ToDouble(qty));
            materialTolerance = (decimal)materialValues[ChemicalCalculations.MR8A_TOL];

            lblMR8AStandard.Text = Math.Round(std, 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMR8AUpper.Text = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMR8ALower.Text = Math.Round((std * (1 - materialTolerance)), 2, MidpointRounding.AwayFromZero).ToString("N2");
        }

        private void Calc_MRReleasing(string qty)
        {
            decimal std = (decimal)calc.ComputeStdValues(Chemicals.MR_RELEASING, main.ToDouble(qty));
            materialTolerance = (decimal)materialValues[ChemicalCalculations.MRREL_TOL];

            lblMRRelStandard.Text = Math.Round(std, 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMRRelUpper.Text = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero).ToString("N2");

            lblMRRelLower.Text = Math.Round((std * (1 - materialTolerance)), 2, MidpointRounding.AwayFromZero).ToString("N2");
        }

        private string GetLot(int lotno, string input)
        {
            string lot = string.Empty;

            try
            {
                using (DAL dal = new DAL())
                {
                    lot = dal.GetSingleData(new ParameterData
                    {
                        sqlQuery = Query.GetLotNo(lotno, input)
                    });
                }

            }
            catch(Exception er)
            {
                MessageDisplay.Error(er.Message);
            }

            return lot;
        }

        protected void StartCheckingConnection()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(1500);

                    WeighingScale scale = new WeighingScale();

                    Func<int, string, ConnectionStatus> CheckConnection = socketConn.CheckSocketConnection;
                    Func<int, string, ConnectionStatus> CheckLoadCellConnection = socketConn.CheckSocketConnectionMain;

                    foreach (KeyValuePair<WeighingScales, string> item in SocketSettings)
                    {
                        if (!Variables.Machine.wait)
                        {
                            if (item.Key != WeighingScales.MOXA)
                            {
                                int port = Convert.ToInt32(item.Value);

                                switch (item.Key)
                                {
                                    case WeighingScales.MR8B1:
                                    case WeighingScales.MR8B2:
                                    case WeighingScales.MR8A:
                                        lblStatusMixingTank.BackColor = scale.GetColor(CheckLoadCellConnection.Invoke(port, Variables.ip));
                                        break;
                                    case WeighingScales.MR_RELEASING:
                                        lblStatusMRReleasingAgent.BackColor = scale.GetColor(CheckConnection.Invoke(port, Variables.ip));
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        private void InitializeControls()
        {
            EnableDisableControls(ProcessM.RenewStart, false);
            EnableDisableControls(ProcessM.MR8B1_Reset, false);
            EnableDisableControls(ProcessM.MR8B1_Get, false);
            EnableDisableControls(ProcessM.MR8B2_Reset, false);
            EnableDisableControls(ProcessM.MR8B2_Get, false);
            EnableDisableControls(ProcessM.MR8A_Reset, false);
            EnableDisableControls(ProcessM.MR8A_Get, false);
            EnableDisableControls(ProcessM.CalcStirSpeed, false);
            EnableDisableControls(ProcessM.Stirring, false);
            EnableDisableControls(ProcessM.NextBatch, false);
            EnableDisableControls(ProcessM.Return, true);
            EnableDisableControls(ProcessM.End, true);
            EnableDisableControls(ProcessM.Renew, true);

        }

        public string CheckMixingStatus(string batchno)
        {
            try
            {
                using (DAL dal = new DAL())
                {
                    string val = dal.GetSingleData(new ParameterData
                    {
                        sqlQuery = Query.CheckMixingStatus(lblBatchNo.Text.Substring(0,8))
                    });

                    return val;
                }
            }
            catch
            {
                throw;
            }
        }

        private string GetVersion()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return v.ToString();
        }

        private void DB(ProcessM p)
        {
            using (DAL dac = new DAL())
            {
                switch (p)
                {
                    case ProcessM.Renew:

                        lblBatchNo.Text = dac.GetSingleData(new ParameterData
                        {
                            sqlQuery = Query.GetBatchNumber(Mixing.MAIN)
                        });

                        break;

                    case ProcessM.RenewStart:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveUpdateData(ProcessM.RenewStart)
                        });

                        break;

                    case ProcessM.MR8B1_Reset:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveUpdateData(ProcessM.RenewStart, lblBatchNo.Text, txtPlanningMixingQty.Text, lblRemainingMixQty.Text, Variables.mixerID.ToString(),
                                                     Variables.checkerID.ToString(), cboShift.Text, txtMR8B1Temp.Text)
                        });


                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveResetData(ProcessM.MR8B1_Reset, lblBatchNo.Text, txtMR8B1LotNo1.Text,
                                txtMR8B1LotNo2.Text, lblMR8B1Standard.Text, lblMR8B1Upper.Text, lblMR8B1Lower.Text, lblMR8B1BeforeInput.Text)
                        });

                        break;

                    case ProcessM.MR8B1_Get:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveWeightData(ProcessM.MR8B1_Get, lblMR8B1AfterInput.Text, lblMR8B1ActualInput.Text, lblBatchNo.Text)
                        });

                        break;

                    case ProcessM.MR8B2_Reset:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveUpdateData(ProcessM.MR8B2_Reset, txtMR8B2Temp.Text, lblBatchNo.Text)
                        });


                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveResetData(ProcessM.MR8B2_Reset, lblBatchNo.Text, txtMR8B2LotNo1.Text,
                                txtMR8B2LotNo2.Text, lblMR8B2Standard.Text, lblMR8B2Upper.Text, lblMR8B2Lower.Text, lblMR8B2BeforeInput.Text)
                        });

                        break;

                    case ProcessM.MR8B2_Get:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveWeightData(ProcessM.MR8B2_Get, lblMR8B2AfterInput.Text, lblMR8B2ActualInput.Text, lblBatchNo.Text)
                        });

                        break;

                    case ProcessM.MR8A_Reset:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveUpdateData(ProcessM.MR8A_Reset, txtMR8ATemp.Text, lblBatchNo.Text)
                        });

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveResetData(ProcessM.MR8A_Reset, lblBatchNo.Text, txtMR8ALotNo1.Text,
                                txtMR8ALotNo2.Text, lblMR8AStandard.Text, lblMR8AUpper.Text, lblMR8ALower.Text, lblMR8ABeforeInput.Text)
                        });

                        break;

                    case ProcessM.MR8A_Get:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveWeightData(ProcessM.MR8A_Get, lblMR8AAfterInput.Text, lblMR8AActualInput.Text, lblBatchNo.Text)
                        });

                        break;

                    case ProcessM.MRReleasing_Reset:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveResetData(ProcessM.MRReleasing_Reset, lblBatchNo.Text, txtMRRelLotNo1.Text,
                                txtMRRelLotNo2.Text, lblMRRelStandard.Text, lblMRRelUpper.Text, lblMRRelLower.Text)
                        });

                        break;

                    case ProcessM.MRReleasing_GetBefore:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveWeightData(ProcessM.MRReleasing_GetBefore, lblMRRelBeforeInput.Text, lblBatchNo.Text)
                        });

                        break;

                    case ProcessM.MRReleasing_GetAfter:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveWeightData(ProcessM.MRReleasing_GetAfter, lblMRRelAfterInput.Text, lblMRRelActualInput.Text, lblBatchNo.Text)
                        });

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.UpdatePresentMatWeightMain(lblBatchNo.Text, lblPesMatWeight.Text)
                        });

                        break;

                    case ProcessM.ForceClosing:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.UpdateBatchNumberReturn(Mixing.MAIN, lblBatchNo.Text, Variables.returnID)
                        });

                        break;

                    case ProcessM.Return:

                        string batchno = lblBatchNo.Text;

                        if (string.IsNullOrEmpty(lblMR8B1AfterInput.Text))
                        {
                            dac.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.DeleteBatchNumber(Mixing.MAIN, batchno)
                            });

                            dac.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.DeleteBatchNumberProcess(Mixing.MAIN, batchno)
                            });

                            dac.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.UpdateBatchNumberReturn(batchno)
                            });
                        }
                        else
                        {
                            dac.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.UpdateBatchNumberReturn(Mixing.MAIN, batchno, Variables.returnID)
                            });
                        }

                        break;

                    case ProcessM.Stirring:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveUpdateData(ProcessM.Stirring, txtStirringSpeed.Text, lblBatchNo.Text)
                        });

                        break;

                    case ProcessM.NextBatch:

                        dac.ExecuteQuery(new ParameterData
                        {
                            sqlQuery = Query.SaveUpdateData(ProcessM.NextBatch, lblBatchNo.Text.Substring(0, 8))
                        });

                        break;
                }
            }
        }

    }

    

}
