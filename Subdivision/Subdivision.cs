using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MR_8_80_Liters_Mixing_System;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using User;
using System.Diagnostics;
using System.Threading;

namespace Subdivision
{
    public partial class Subdivision : Form
    {
        #region Declarations
        Dictionary<ChemicalCalculations, double> materialValues = new Dictionary<ChemicalCalculations, double>();
        Dictionary<WeighingScales, string> connPorts = new Dictionary<WeighingScales, string>();
        Dictionary<WeighingScales, string> SocketSettings = new Dictionary<WeighingScales, string>();

        BackgroundWorker bg = new BackgroundWorker();

        public ManualResetEvent thEvent = new ManualResetEvent(false);

        Functions sub = new Functions();
        Tolerance tolerance = new Tolerance();
        Calculations calc = new Calculations();
        WeighingScale scale = new WeighingScale();
        Connections socketConn = new Connections();

        bool closing = false;

        double materialTolerance = 0;

        private ToolTip tip;

        string[] injBatch = null;

        DateTime lastKey = new DateTime(0);
        List<char> barcode = new List<char>(10);
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

        public Subdivision()
        {
            InitializeComponent();
            //Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            Variables.connString = Configuration.GetConnString("Server", "Location");

            LoadMixer();
            LoadChecker();

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
            //        bg.CancelAsync();
            //    }
            //};


            this.Load += (o, e) =>
              {
                  try
                  {
                      this.Text = GetVersion();
                      ControlTagging();
                      InitializeVariables();
                      sub.WriteLogs("System is starting up");
                      bg.RunWorkerAsync();
                  }
                  catch (Exception er)
                  {
                      MessageDisplay.Error(er.Message);
                      closing = true;                         //Exiting the program
                      Application.Exit();
                  }
              };

            this.FormClosing += (o, e) =>
             {
                 if (!closing)
                 {
                     if (!string.IsNullOrEmpty(lblBatchNo.Text))
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
                    string grossVal = scale.GetWeightLoadCell(WeighingScales.TANK, WeightType.GROSS);
                    //test
                    //string grossVal = "010,000,000,000,043.00";
                    //string grossVal = "80.00";

                    lblRemainingMixQty.Text = grossVal;

                    if (!Functions.IsValidLoadCellWeight(grossVal))
                    {
                        MessageDisplay.Error("Load Cell is unstable. Please try again.");
                        return;
                    }


                    if (string.IsNullOrEmpty(CheckWeightValues(lblRemainingMixQty.Text)))
                    {
                        if (sub.ToDouble(lblRemainingMixQty.Text) < 5)
                        {
                            MessageDisplay.Error("Remained mixed monomer is less than 5 KG.");
                            return;
                        }

                        DB(ProcessM.RenewSub);

                        if (lblBatchNo.Text.Equals("-"))
                        {
                            MessageDisplay.Error("No batch number candidate for additives mixing");
                        }
                        else
                        {
                            Variables.batchNo = lblBatchNo.Text + '-' + lblInjBatchNo.Text;

                            EnableDisableControls(Process.Renew, false);
                            EnableDisableControls(Process.RenewStart, true);
                            EnableDisableControls(Process.End, false);
                        }

                        sub.WriteLogs("*************** Start Batch No: " + Variables.batchNo + " ***************");
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }

            };

            cboMonomerType.SelectedIndexChanged += (o, e) =>
            {
                try
                {
                    clblTitle.Text = "SUBTANK MIXING: " + cboMonomerType.Text;

                    if (cboMonomerType.Text.Equals("UV"))
                    {
                        Variables.kemisorb = materialValues[ChemicalCalculations.KEMISORB_R_UV];
                        Variables.dyeStuff = materialValues[ChemicalCalculations.BLUING_R_UV];
                        Variables.mrCatalyst = materialValues[ChemicalCalculations.MRCAT_R_UV];
                    }
                    else
                    {
                        Variables.kemisorb = materialValues[ChemicalCalculations.KEMISORB_R_UVB];
                        Variables.dyeStuff = materialValues[ChemicalCalculations.BLUING_R_UVB];
                        Variables.mrCatalyst = materialValues[ChemicalCalculations.MRCAT_R_UVB];
                    }

                    DisplayMonomerType(cboMonomerType.Text);
                }
                catch(Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }
            };

            cboMixer.SelectedIndexChanged += (o, e) =>
            {
                try
                {
                    string mixer = string.IsNullOrEmpty(cboMixer.Text) ? string.Empty : GetMixerID(cboMixer.Text);

                    if (!string.IsNullOrEmpty(mixer))
                    {
                        Variables.mixerID = Convert.ToInt32(mixer);
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
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
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnTankReset.Click += (o, e) =>
            {
                try
                {
                    Control[] input = { txtPlanningDistQty, cboMixer, cboChecker, cboShift, txtMonomerType, txtTankTemp,  txtInjTankUnit };

                    if (sub.CheckResetValidation(ProcessM.MixingTankReset, input, txtPlanningDistQty.Text, cboMixer.Text, cboChecker.Text, cboShift.Text, txtMonomerType.Text, txtTankTemp.Text, txtInjTankUnit.Text, lblRemainingMixQty.Text))
                    {
                        //if (CheckInjectionTankNoIfExist(txtInjTankUnit.Text) == true)
                        //{
                        //    MessageDisplay.Error("The injection tank number that you inputted is already used for this batch number");
                        //    sub.Highlight(txtInjTankUnit);
                        //    return;
                        //}
                        //else
                        //{

                        //Send 0 Reset to Load Cell
                        if (string.IsNullOrEmpty(scale.ResetWeightLoadCell(WeighingScales.TANK)))
                        {
                            return;
                        }
                        
                        lblTankZeroReset.Text = "0.00";

                        //Send Planning Distributed Qty to Load Cell
                        //string weightVal = sub.WeightConversionSendValue(txtPlanningDistQty.Text);

                        //if (scale.SendStdWeight(WeighingScales.TANK, weightVal) == false)
                        //{
                        //    return;
                        //}

                        //Get Gross Weight from Load Cell
                        //string grossVal = scale.GetWeightLoadCell(WeighingScales.TANK, WeightType.GROSS);

                        //Copy from present remaining weight
                        lblTankBeforeTransfer.Text = lblRemainingMixQty.Text;

                        //Update DB
                        DB(ProcessM.MixingTankReset);

                        EnableDisableControls(Process.RenewStart, false);
                        EnableDisableControls(Process.MixingTankGet, true);

                        //}
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnTankTransferWeight.Click += (o, e) =>
            {
                try
                {
                    //Get net weight from the Load Cell (transferred weight)
                    string net = scale.GetWeightLoadCell(WeighingScales.TANK, WeightType.NET);

                    //Get gross weight from the Load Cell (Weight After Transfer)
                    string gross = scale.GetWeightLoadCell(WeighingScales.TANK, WeightType.GROSS);

                    //Test
                    //string net = "-010,234,000,000,134,123";
                    //string gross = "-2465,56456,54545,5454,5454";
                    //string net = lblTransferWeigt.Text;
                    //string gross = (Convert.ToDouble(lblTankBeforeTransfer.Text) - Convert.ToDouble(net)).ToString();

                    //Removing negative sign due to the NET weight reading is always negative on subtank mixing
                    lblTransferWeigt.Text = net.Replace("-", "");
                    lblTankAfterTransfer.Text = gross;


                    if (!Functions.IsValidLoadCellWeight(net))
                    {
                        MessageDisplay.Error("Load Cell NET WEIGHT is unstable. Please try again.");
                        return;
                    }

                    if (!Functions.IsValidLoadCellWeight(gross))
                    {
                        MessageDisplay.Error("Load Cell GROSS WEIGHT is unstable. Please try again.");
                        return;
                    }

                    if (CheckValidWeightInput(CheckWeightValues(lblTransferWeigt.Text)))
                    {
                        if(sub.ToDouble(lblTankBeforeTransfer.Text) > sub.ToDouble(txtPlanningDistQty.Text))
                        {
                            Functions.AlertIndicatorWeight(lblTransferWeigt, false);

                            if (CheckValidWeightInput(CheckWeightValues(lblTankAfterTransfer.Text)))
                            {
                                if (sub.ToDouble(lblTransferWeigt.Text) > sub.ToDouble(txtPlanningDistQty.Text))
                                {
                                    elblErrorTank.Text = "TRANSFERRED WEIGHT EXCEEDS " + txtPlanningDistQty.Text +" KG";
                                    Functions.AlertIndicatorWeight(lblTransferWeigt, true);
                                }
                                else if (sub.ToDouble(lblTransferWeigt.Text) < sub.ToDouble(txtPlanningDistQty.Text))
                                {
                                    elblErrorTank.Text = "";
                                    DialogResult res = MessageDisplay.Question("Transferred weight does not reach the plannning distributed qty.\nDo you want to get weight again?");

                                    if (res == DialogResult.Yes)
                                    {
                                        btnTankTransferWeight.Focus();
                                    }
                                    else
                                    {
                                        elblErrorTank.Text = "";
                                        Functions.AlertIndicatorWeight(lblTransferWeigt, false);
                                        Functions.AlertIndicatorWeight(lblTankAfterTransfer, false);

                                        DB(ProcessM.MixingTankGet);

                                        EnableDisableControls(Process.Return, false);
                                        EnableDisableControls(Process.MixingTankGet, false);
                                        EnableDisableControls(Process.MixingTankStirring, true);
                                    }
                                }
                                else
                                {
                                    elblErrorTank.Text = "";
                                    Functions.AlertIndicatorWeight(lblTankAfterTransfer, false);

                                    DB(ProcessM.MixingTankGet);

                                    EnableDisableControls(Process.MixingTankGet, false);
                                    EnableDisableControls(Process.MixingTankStirring, true);
                                }
                            }
                            else
                            {
                                elblErrorTank.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                                Functions.AlertIndicatorWeight(lblTankAfterTransfer, true);
                            }
                        }
                        else
                        {
                            elblErrorTank.Text = "REMAINED MONOMER IS LESS THAN PLANNING QTY.";
                            //MessageDisplay.Error("Remained mixed monomer in mixing tank is less than planning distributed quantity.");
                        }
                    }
                    else
                    {
                        elblErrorTank.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                        Functions.AlertIndicatorWeight(lblTransferWeigt, true);
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnStartStirringMain.Click += (o, e) =>
            {
                EnableDisableControls(Process.MixingTankStirring, false);
                EnableDisableControls(Process.MixingTankStirrSpeed, true);
            };

            btnCalcStirSpeed.Click += (o, e) =>
            {
                try
                {
                    lblTargetStirSpeed.Text = calc.CalculateTargetStirringSpeed(Mixing.SUBDIVISION, txtPlanningDistQty.Text);
                    lblMinStirSpeed.Text = calc.CalculateMininumStirringSpeed(Mixing.SUBDIVISION, txtPlanningDistQty.Text, lblMinStirSpeed.Text);

                    EnableDisableControls(Process.MixingTankStirrSpeed, false);
                    EnableDisableControls(Process.KemiDyeReset, true);
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }

            };

            btnKemiDyestuffReset.Click += (o, e) =>
            {
                try
                {
                    Control[] input = { txtStirSpeed, txtInjTankTemp, txtKemiLotNo1, txtDyestuffLotNo1, lblMinStirSpeed, lblTargetStirSpeed, txtStirSpeed  };


                    if (sub.CheckResetValidation(ProcessM.KemiDyeReset, input, txtStirSpeed.Text, txtInjTankTemp.Text, txtKemiLotNo1.Text, txtDyestuffLotNo1.Text))
                    {
                        //Send reset command to scale
                        string kemiReset = scale.WeightReset(WeighingScales.KEMISORB);
                        string dyeReset = scale.WeightReset(WeighingScales.DYESTUFF);

                        //string kemiReset = "0";
                        //string dyeReset = "0";

                        if (string.IsNullOrEmpty(kemiReset) && string.IsNullOrEmpty(dyeReset))
                        {
                            return;
                        }
                        else
                        {
                            CalcKemisorb(lblTransferWeigt.Text, txtMonomerType.Text);
                            CalcDyestuff(lblTransferWeigt.Text, txtMonomerType.Text);

                            DB(ProcessM.KemiDyeReset);
                            DB(ProcessM.KemiReset);
                            DB(ProcessM.DyeReset);

                            EnableDisableControls(Process.MixingTankStirring, false);
                            EnableDisableControls(Process.MixingTankStirrSpeed, false);
                            EnableDisableControls(Process.KemiDyeReset, false);
                            EnableDisableControls(Process.KemiDyeBefore, true);
                        }
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }
            };

            btnKemiDyestuffBeforeInput.Click +=  (o, e) =>
            {
                try
                {
                    sub.WriteLogs("Start Pressing Kemisorb & Dystuff Weight Before Input");

                    string weightKemi = scale.GetWeight(WeighingScales.KEMISORB);
                    string weightDye = scale.GetWeight(WeighingScales.DYESTUFF);

                    //Test
                    //string weightKemi = lblKemiBeforeInput.Text;
                    //string weightDye = lblDyestuffBeforeInput.Text;

                    lblKemiBeforeInput.Text = weightKemi;
                    lblDyestuffBeforeInput.Text = weightDye;

                    tolerance.weightBeforeKemi = lblKemiBeforeInput.Text;
                    tolerance.weightBeforeDyestuff = lblDyestuffBeforeInput.Text;


                    if (CheckValidWeightInput(CheckWeightValues(lblKemiBeforeInput.Text)) && CheckValidWeightInput(CheckWeightValues(lblDyestuffBeforeInput.Text)))
                    {
                        lblKemiBeforeInput.BackColor = tolerance.WeightBeforeOver2Chemicals(Chemicals.KEMISORB) ? Color.Red : Color.Moccasin;
                        lblDyestuffBeforeInput.BackColor = tolerance.WeightBeforeOver2Chemicals(Chemicals.BLUING) ? Color.Red : Color.Moccasin;

                        if (!tolerance.WeightBeforeOver2Chemicals(Chemicals.KEMISORB) && !tolerance.WeightBeforeOver2Chemicals(Chemicals.BLUING))
                        {
                            DB(ProcessM.KemiBefore);
                            DB(ProcessM.DyeBefore);

                            elblErrorMsg.Text = "";

                            EnableDisableControls(Process.KemiDyeBefore, false);
                            EnableDisableControls(Process.KemiDyeAfter, true);
                        }
                        else
                        {
                            elblErrorMsg.Text = "WEIGHT IS OUTSIDE TOLERANCE RANGE.";
                        }
                    }
                    else
                    {
                        lblKemiBeforeInput.BackColor = !CheckValidWeightInput(CheckWeightValues(lblKemiBeforeInput.Text)) ? Color.Red : Color.Moccasin;
                        lblDyestuffBeforeInput.BackColor = !CheckValidWeightInput(CheckWeightValues(lblDyestuffBeforeInput.Text)) ? Color.Red : Color.Moccasin;
                        elblErrorMsg.Text = "INVALID WEIGHT VALUES. PLEASE TRY AGAIN.";
                    }

                    sub.WriteLogs("End Pressing Kemisorb & Dystuff Weight Before Input");

                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }
            };

            btnKemiDyestuffAfterInput.Click += (o, e) =>
            {
                try
                {
                    sub.WriteLogs("Start Pressing Kemisorb & Dystuff Weight After Input");

                    string weightKemi = scale.GetWeight(WeighingScales.KEMISORB);
                    string weightDye = scale.GetWeight(WeighingScales.DYESTUFF);

                    //Test
                    //string weightKemi = lblKemiAfterInput.Text;
                    //string weightDye = lblDyestuffAfterInput.Text;

                    lblKemiAfterInput.Text = weightKemi;
                    lblDyestuffAfterInput.Text = weightDye;

                    if (CheckValidWeightInput(CheckWeightValues(lblKemiAfterInput.Text)) && CheckValidWeightInput(CheckWeightValues(lblDyestuffAfterInput.Text)))
                    {
                        tolerance.weightAfterKemi = lblKemiAfterInput.Text;
                        tolerance.weightAfterDyestuff = lblDyestuffAfterInput.Text;

                        lblKemiAfterInput.BackColor = Color.Moccasin;
                        lblDyestuffAfterInput.BackColor = Color.Moccasin;

                        lblKemiActualInput.Text = tolerance.Weight2Chemicals(Chemicals.KEMISORB).ToString();
                        lblDyestuffActualInput.Text = tolerance.Weight2Chemicals(Chemicals.BLUING).ToString();

                        lblKemiActualInput.BackColor = tolerance.WeightOver2Chemicals(Chemicals.KEMISORB) ? Color.Red : Color.Moccasin;
                        lblDyestuffActualInput.BackColor = tolerance.WeightOver2Chemicals(Chemicals.BLUING) ? Color.Red : Color.Moccasin;

                        if (!tolerance.WeightOver2Chemicals(Chemicals.KEMISORB) && !tolerance.WeightOver2Chemicals(Chemicals.BLUING))
                        {

                            DB(ProcessM.KemiAfter);
                            DB(ProcessM.DyeAfter);

                            elblErrorMsg.Text = "";

                            EnableDisableControls(Process.KemiDyeAfter, false);
                            EnableDisableControls(Process.MRCatReset, true);
                            EnableDisableControls(Process.Return, false);
                        }
                        else
                        {
                            elblErrorMsg.Text = "WEIGHT IS OUTSIDE TOLERANCE RANGE.";
                        }
                    }
                    else
                    {
                        lblKemiAfterInput.BackColor = CheckValidWeightInput(CheckWeightValues(lblKemiBeforeInput.Text)) ? Color.Red : Color.Moccasin;
                        lblDyestuffAfterInput.BackColor = CheckValidWeightInput(CheckWeightValues(lblDyestuffBeforeInput.Text)) ? Color.Red : Color.Moccasin;
                        elblErrorMsg.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                    }

                    sub.WriteLogs("End Pressing Kemisorb & Dystuff Weight After Input");
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                }
            };

            btnMRCatReset.Click += (o, e) =>
            {
                try
                {
                    Control[] input = { txtMRCatLotNo1 };

                    if(sub.CheckResetValidation(ProcessM.MRCatReset, input, txtMRCatLotNo1.Text))
                    {
                        string val = scale.WeightReset(WeighingScales.MRCAT);

                        //Test
                        //string val = "0";

                        if (string.IsNullOrEmpty(val))
                        {
                            return;
                        }
                        else
                        {
                            CalcMRCat(lblTransferWeigt.Text, txtMonomerType.Text);

                            DB(ProcessM.MRCatReset);

                            EnableDisableControls(Process.MRCatReset, false);
                            EnableDisableControls(Process.MRCatBefore, true);
                        }
                    }


                    //if (Functions.CheckNotNull(txtMRCatLotNo1.Text))
                    //{
                    //    //string val = scale.WeightReset(WeighingScales.KEMISORB);

                    //    //Test
                    //    string val = "0";

                    //    if (string.IsNullOrEmpty(val))
                    //    {
                    //        return;
                    //    }
                    //    else
                    //    {
                    //        CalcMRCat(txtPlanningDistQty.Text, txtMonomerType.Text);

                    //        DB(ProcessM.MRCatReset);

                    //        EnableDisableControls(Process.MRCatReset, false);
                    //        EnableDisableControls(Process.MRCatBefore, true);
                    //    }
                    //}
                    //else
                    //{
                    //    sub.Highlight(txtMRCatLotNo1);
                    //}
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnMRCatBeforeInput.Click += (o, e) =>
            {
                try
                {
                    //Test
                    //string val = lblMRCatBeforeInput.Text;

                    //Get value from the weighing scale
                    string val = scale.GetWeight(WeighingScales.MRCAT);

                    lblMRCatBeforeInput.Text = val;

                    if (CheckValidWeightInput(CheckWeightValues(lblMRCatBeforeInput.Text)))
                    {
                        tolerance.weightBefore = lblMRCatBeforeInput.Text;

                        if (!tolerance.WeightBeforeOver)
                        {
                            Functions.AlertIndicatorWeight(lblMRCatBeforeInput, false);

                            DB(ProcessM.MRCatBefore);

                            elblErrorMsg.Text = "";

                            EnableDisableControls(Process.MRCatBefore, false);
                            EnableDisableControls(Process.MRCatAfter, true);
                        }
                        else
                        {
                            elblErrorMsg.Text = "WEIGHT IS OUTSIDE TOLERANCE RANGE.";
                            Functions.AlertIndicatorWeight(lblMRCatBeforeInput, true);
                        }
                    }
                    else
                    {
                        elblErrorMsg.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                        Functions.AlertIndicatorWeight(lblMRCatBeforeInput, true);
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnMRCatAfterInput.Click += (o, e) =>
            {
                try
                {
                    //Test
                    //string val = lblMRCatAfterInput.Text;

                    //Get value from the weighing scale
                    string val = scale.GetWeight(WeighingScales.MRCAT);

                    lblMRCatAfterInput.Text = val;

                    if (CheckValidWeightInput(CheckWeightValues(lblMRCatAfterInput.Text)))
                    {
                        tolerance.weightAfter = lblMRCatAfterInput.Text;

                        lblMRCatActualInput.Text = tolerance.Weight3Decimal.ToString();

                        Functions.AlertIndicatorWeight(lblMRCatAfterInput, false);

                        if (!tolerance.WeightOver3D)
                        {
                            Functions.AlertIndicatorWeight(lblMRCatActualInput, false);

                            DB(ProcessM.MRCatAfter);

                            elblErrorMsg.Text = "";

                            EnableDisableControls(Process.MRCatAfter, false);
                            EnableDisableControls(Process.MRCatStirring, true);
                            EnableDisableControls(Process.End, false);
                        }
                        else
                        {
                            elblErrorMsg.Text = "WEIGHT IS OUTSIDE TOLERANCE RANGE.";
                            Functions.AlertIndicatorWeight(lblMRCatActualInput, true);
                        }
                    }

                    else
                    {
                        elblErrorMsg.Text = "INVALID WEIGHT VALUE. PLEASE TRY AGAIN.";
                        Functions.AlertIndicatorWeight(lblMRCatAfterInput, true);
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnMRCatDissolution.Click += (o, e) =>
            {
                try
                {
                    if (sub.ToDouble(lblTransferWeigt.Text) == 20)
                    {
                        EnableDisableControls(Process.MRCatStirring, false);
                        EnableDisableControls(Process.Deagassing, true);
                    }
                    else
                    {
                        EnableDisableControls(Process.MRCatStirring, false);
                        EnableDisableControls(Process.AddFreshValidation, true);
                    }

                    DB(ProcessM.MRCatStirring);
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnAddFreshYes.Click += (o, e) =>
            {
                try
                {
                    lblFreshMax.Text = ((20 - sub.ToDouble(lblTransferWeigt.Text)) * 1000).ToString("N2");
                    EnableDisableControls(Process.AddFreshYes, true);
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnAddFreshNo.Click += (o, e) =>
            {
                lblFreshMax.Text = "";
                txtFreshTemp.Clear();
                txtFreshWeight.Clear();
                EnableDisableControls(Process.AddFreshValidation, false);
                EnableDisableControls(Process.AddFreshYes, false);
                EnableDisableControls(Process.AddExcessValidation, true);
            };

            btnAddExcessYes.Click += (o, e) =>
            {
                if (string.IsNullOrEmpty(txtFreshWeight.Text))
                {
                    lblExcessMax.Text = ((sub.ToDouble(lblTransferWeigt.Text) * 0.10) * 1000).ToString("N2");
                }
                else
                {
                    double val = (sub.ToDouble(lblTransferWeigt.Text) + (sub.ToDouble(txtFreshWeight.Text)/1000)) * 1.1;

                    if (val <= 20)
                        lblExcessMax.Text = (((sub.ToDouble(lblTransferWeigt.Text) + (sub.ToDouble(txtFreshWeight.Text) / 1000)) * 0.10) * 1000).ToString("N2");
                    else
                        lblExcessMax.Text = ((20 - (sub.ToDouble(lblTransferWeigt.Text) + (sub.ToDouble(txtFreshWeight.Text) / 1000))) * 1000).ToString("N2");
                }

                EnableDisableControls(Process.AddExcessYes, true);
            };

            btnAddExcessNo.Click += (o, e) =>
            {
                lblExcessMax.Text = "";
                txtExcessTemp.Clear();
                txtExcessWeight.Clear();
                EnableDisableControls(Process.AddExcessValidation, false);
                EnableDisableControls(Process.AddExcessYes, false);
                EnableDisableControls(Process.Deagassing, true);
            };

            btnRecordFresh.Click += (o, e) =>
            {
                try
                {
                    string inputs = txtFreshTemp.Text + '|' + txtFreshWeight.Text;

                    if (Functions.CheckNotNullNoMessage(inputs))
                    {
                        if (Functions.CheckWinthinTolerance(sub.ToDouble(txtFreshTemp.Text), 20, 27))
                        {
                            if (Functions.CheckWinthinTolerance(sub.ToDouble(txtFreshWeight.Text), 0.1, sub.ToDouble(lblFreshMax.Text)))
                            {
                                Functions.AlertIndicatorInput(txtFreshTemp, false);
                                Functions.AlertIndicatorInput(txtFreshWeight, false);

                                if (sub.ToDouble(lblTransferWeigt.Text) + (sub.ToDouble(txtFreshWeight.Text) / 1000) == 20)
                                {
                                    EnableDisableControls(Process.AddFreshYes, false);
                                    EnableDisableControls(Process.AddFreshValidation, false);
                                    EnableDisableControls(Process.Deagassing, true);
                                }
                                else
                                {
                                    EnableDisableControls(Process.AddFreshYes, false);
                                    EnableDisableControls(Process.AddFreshValidation, false);
                                    EnableDisableControls(Process.AddExcessValidation, true);
                                }

                                DB(ProcessM.AddFreshYes);
                            }
                            else
                            {
                                MessageDisplay.Error("The maximum input of fresh monomer is " + lblFreshMax.Text + " grams.");
                                sub.Highlight(txtFreshWeight);
                            }
                        }
                        else
                        {
                            MessageDisplay.Error("Temp error!\nNote: 20 - 27 only acceptable.");
                            sub.Highlight(txtFreshTemp);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(txtFreshTemp.Text))
                        {
                            MessageDisplay.Error("Please input fresh temp.");
                            txtFreshTemp.Focus();
                        }
                        else if(string.IsNullOrEmpty(txtFreshWeight.Text))
                        {
                            MessageDisplay.Error("Please input fresh weight.");
                            txtFreshWeight.Focus();
                        }
                    }

                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnRecordExcess.Click += (o, e) =>
            {
                try
                {
                    string inputs = txtExcessTemp.Text + '|' + txtExcessWeight.Text;

                    if (Functions.CheckNotNullNoMessage(inputs))
                    {

                        if (Functions.CheckWinthinTolerance(sub.ToDouble(txtExcessTemp.Text), 19, 24))
                        {
                            if (Functions.CheckWinthinTolerance(sub.ToDouble(txtExcessWeight.Text), 0.1, sub.ToDouble(lblExcessMax.Text)))
                            {
                                DB(ProcessM.AddExcessYes);

                                Functions.AlertIndicatorInput(txtExcessTemp, false);
                                Functions.AlertIndicatorInput(txtExcessWeight, false);
                                EnableDisableControls(Process.AddExcessYes, false);
                                EnableDisableControls(Process.AddExcessValidation, false);
                                EnableDisableControls(Process.Deagassing, true);
                            }
                            else
                            {
                                MessageDisplay.Error("The maximum input of excess monomer is " + lblExcessMax.Text + " grams.");
                                sub.Highlight(txtExcessWeight);
                            }
                        }
                        else
                        {
                            MessageDisplay.Error("Temp error!\nNote: 19 - 24 only acceptable.");
                            sub.Highlight(txtExcessTemp);
                        }
                    }
                    else
                    {
                        if(string.IsNullOrEmpty(txtExcessTemp.Text))
                        {
                            MessageDisplay.Error("Please input excess temp.");
                            txtExcessTemp.Focus();
                        }
                        else if(string.IsNullOrEmpty(txtExcessWeight.Text))
                        {
                            MessageDisplay.Error("Please input excess weight.");
                            txtExcessWeight.Focus();
                        }
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnDegassing.Click += (o, e) =>
            {
                try
                {
                    DB(ProcessM.Deagassing);

                    EnableDisableControls(Process.Deagassing, false);
                    EnableDisableControls(Process.NextBatch, true);
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnNextBatch.Click += (o, e) =>
            {
                try
                {
                    if (Functions.CheckNotNullNoMessage(txtInjTankTempTwo.Text))
                    {
                        if(sub.ToDouble(txtInjTankTempTwo.Text) > 0)
                        {
                            DB(ProcessM.NextBatchSub);

                            sub.WriteLogs("*************** End Batch No: " + Variables.batchNo + " ***************");

                            Functions.AlertIndicatorInput(txtInjTankTempTwo, false);
                            Functions.ClearData(this, Mixing.SUBDIVISION);
                            ClearColor();

                            EnableDisableControls(Process.NextBatch, false);
                            EnableDisableControls(Process.Renew, true);
                        }
                        else
                        {
                            MessageDisplay.Error("Please input injection tank temp.");
                            sub.Highlight(txtInjTankTempTwo);
                        }
                    }
                    else
                    {
                        MessageDisplay.Error("Please input injection tank temp.");
                        sub.Highlight(txtInjTankTempTwo);
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
                    return;
                }
            };

            btnReturn.Click += (o, e) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(lblBatchNo.Text) && lblBatchNo.Text != "-")
                    {
                        if (!string.IsNullOrEmpty(lblTransferWeigt.Text))
                        {
                            UserConfirm frm = new UserConfirm();
                            frm.ShowDialog(this);

                            if (frm.DialogResult == DialogResult.OK)
                            {
                                DB(ProcessM.ReturnSub);

                                Functions.ClearData(this, Mixing.SUBDIVISION);
                                InitializeControls();
                                ClearColor();
                            }
                            else
                            {
                                frm.Close();
                            }
                        }
                        else
                        {
                            DB(ProcessM.ReturnSub);
                            Functions.ClearData(this, Mixing.SUBDIVISION);
                            InitializeControls();
                            ClearColor();
                        }
                    }
                    else
                    {
                        MessageDisplay.Error("Nothing to return");
                    }
                }
                catch (Exception er)
                {
                    sub.WriteLogs(er.Message);
                    MessageDisplay.Error(er.Message);
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

        private void InitializeVariables()
        {
            using (DAL dal = new DAL())
            {
                materialValues = dal.GetCalculations(new ParameterData
                {
                    sqlQuery = Query.GetCalculations()
                });
            }

            SocketSettings = Configuration.GetSocket("Socket");

            Variables.portTANK = SocketSettings[WeighingScales.TANK];
            Variables.portKemisorb = SocketSettings[WeighingScales.KEMISORB];
            Variables.portDyestuff = SocketSettings[WeighingScales.DYESTUFF];
            Variables.portMRCat = SocketSettings[WeighingScales.MRCAT];

            Variables.ip = SocketSettings[WeighingScales.MOXA];

            Variables.setupValue = Configuration.GetSettingData("InitialValueSub", "Port");

            string bc = Configuration.GetSettingData("Monomer", "Port");
            Variables.monomerType = bc.Split(',');
        }

        private void LoadMixer()
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

        private void LoadChecker()
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

        private void ShowBalloonNotif(string text, Control c)
        {
            tip = new ToolTip()
            {
                InitialDelay = 0,
                IsBalloon = false,
                ToolTipTitle = text
            };

            //tip.Show(string.Empty, c);
            tip.Show("UV or UV-BLUE", c, 0);
        }

        private void DisplayMonomerType(string mono)
        {
            if (mono.Equals(Variables.monomerType[1]))
            {
                clblTitle.BackColor = System.Drawing.ColorTranslator.FromHtml("#0e4584");
                clblKemi.BackColor = System.Drawing.ColorTranslator.FromHtml("#0e4584");
                clblDyestuff.BackColor = System.Drawing.ColorTranslator.FromHtml("#0e4584");
                clblMRCat.BackColor = System.Drawing.ColorTranslator.FromHtml("#0e4584");
                clblMixingTank.BackColor = System.Drawing.ColorTranslator.FromHtml("#0e4584");
                clblTitle.ForeColor = Color.White;
                clblKemi.ForeColor = Color.White;
                clblDyestuff.ForeColor = Color.White;
                clblMRCat.ForeColor = Color.White;
                clblMixingTank.ForeColor = Color.White;
            }

            else if (mono.Equals(Variables.monomerType[0]))
            {
                //Color.FromArgb(62, 6, 148);
                clblTitle.BackColor = Color.FromArgb(255, 255, 192);
                clblKemi.BackColor = Color.FromArgb(255, 255, 192);
                clblDyestuff.BackColor = Color.FromArgb(255, 255, 192);
                clblMRCat.BackColor = Color.FromArgb(255, 255, 192);
                clblMixingTank.BackColor = Color.FromArgb(255, 255, 192);
                clblTitle.ForeColor = Color.FromArgb(32, 59, 104);
                clblKemi.ForeColor = Color.FromArgb(32, 59, 104);
                clblDyestuff.ForeColor = Color.FromArgb(32, 59, 104);
                clblMRCat.ForeColor = Color.FromArgb(32, 59, 104);
                clblMixingTank.ForeColor = Color.FromArgb(32, 59, 104);
            }

        }
        
        private bool CheckValidWeightInput(string input)
        {
            bool stat = false;

            if (string.IsNullOrWhiteSpace(input)) { stat = true; }

            return stat;
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

        private bool CheckWeightValidationAfterTransfer(ProcessM p, string weight)
        {
            bool stat = false;

            try
            {
                if (CheckValidWeightInput(CheckWeightValues(weight)))
                {
                    if (txtPlanningDistQty.Text == lblTransferWeigt.Text)
                    {
                        stat = true;
                        Functions.AlertIndicatorWeight(lblTankAfterTransfer, false);
                    }
                    else
                    {
                        Functions.AlertIndicatorWeight(lblTankAfterTransfer, true);
                    }
                }
                else
                {

                }
            }
            catch
            {
                throw;
            }

            return stat;
        }

        private void ControlTagging()
        {
            try
            {
                this.Controls.OfType<Control>().ToList().ForEach(n =>
                {
                    n.GotFocus += (o, e) =>
                    {
                        if (n is ComboBox || n.BackColor == Color.Red || n.Name.Equals("btnReturn") || n.Name.Equals("btnEnd") || n.Name.StartsWith("lbl"))
                        {

                        }
                        else if (n is TextBox || n is Button)
                            n.BackColor = Color.Orange;
                    };

                    n.LostFocus += (o, e) =>
                    {
                        if (n.Name.StartsWith("txt"))
                        {
                            n.BackColor = Color.White;
                        }
                        else if (n.Name.StartsWith("lbl") && n.Name.EndsWith("Input"))
                        {
                            n.BackColor = Color.Moccasin;
                        }
                        else if(n.Name.StartsWith("btn"))
                        {
                            n.BackColor = SystemColors.Control;
                        }

                       
                    };

   

                    n.KeyPress += (o, e) =>
                    {
                        if (n.Name.Equals("txtPlanningDistQty") || n.Name.EndsWith("Unit"))
                        {
                            sub.NumberOnlyNoSpecial(txtPlanningDistQty.Text, e);
                        }

                        if (n.Name.StartsWith("txt") && n.Name.EndsWith("Temp"))
                        {
                            sub.AcceptNumOnly(n, e);
                        }

                        if ((n.Name.StartsWith("txt") && (n.Name.EndsWith("Speed"))) || (n.Name.StartsWith("txt") && n.Name.EndsWith("Weight"))  || n.Name.Equals("txtInjTankTempTwo"))
                        {
                            sub.AcceptNumOnly(n, e);
                        }

                        if(n.Name.Equals("txtMonomerType"))
                        {
                            try
                            {
                                TimeSpan elapsedTime = DateTime.Now - lastKey;

                                if (elapsedTime.TotalMilliseconds > 100)
                                {
                                    barcode.Clear();
                                }

                                barcode.Add(e.KeyChar);
                                lastKey = DateTime.Now;

                                if (e.KeyChar == 13 && barcode.Count > 0)
                                {
                                    string bcc = new string(barcode.ToArray());

                                    if (!string.IsNullOrEmpty(bcc))
                                    {
                                        string newBCMonomer = bcc.Replace("\r", "");

                                        if (CheckMonomerType(newBCMonomer))
                                        {
                                            n.Text = newBCMonomer;
                                            Functions.AlertIndicatorInput(n, false);
                                            GetNextControl(n, true).Focus();
                                            barcode.Clear();

                                            clblTitle.Text = "SUBTANK MIXING: " + n.Text;

                                            if (txtMonomerType.Text.Equals(Variables.monomerType[0]))
                                            {
                                                Variables.kemisorb = materialValues[ChemicalCalculations.KEMISORB_R_UV];
                                                Variables.dyeStuff = materialValues[ChemicalCalculations.BLUING_R_UV];
                                                Variables.mrCatalyst = materialValues[ChemicalCalculations.MRCAT_R_UV];
                                            }
                                            else
                                            {
                                                Variables.kemisorb = materialValues[ChemicalCalculations.KEMISORB_R_UVB];
                                                Variables.dyeStuff = materialValues[ChemicalCalculations.BLUING_R_UVB];
                                                Variables.mrCatalyst = materialValues[ChemicalCalculations.MRCAT_R_UVB];
                                            }

                                            DisplayMonomerType(txtMonomerType.Text);

                                        }
                                        else
                                        {
                                            Functions.AlertIndicatorInput(n, true);
                                            n.Focus();
                                        }
                                    }
                                }
                            }
                            catch(Exception ers)
                            {
                                MessageDisplay.Error(ers.Message);
                                sub.WriteLogs(ers.Message);
                            }
                        }

                        if(n.Name.EndsWith("LotNo1") || n.Name.EndsWith("LotNo2"))
                        {
                            TimeSpan elapsed = DateTime.Now - lastKey;

                            if (elapsed.TotalMilliseconds > 100)
                            {
                                barcode.Clear();
                            }


                            barcode.Add(e.KeyChar);
                            lastKey = DateTime.Now;

                            if (e.KeyChar == 13 && barcode.Count > 0)
                            {
                                string bc = new string(barcode.ToArray()).ToUpper();

                                if (n.Name.EndsWith("LotNo1") & !string.IsNullOrEmpty(bc))
                                {
                                    string newBC = bc.Replace("\r", "");

                                    if (n.Tag.ToString().Split('_').First() == newBC)
                                    {
                                        n.Text = GetLot(1, newBC);
                                        Functions.AlertIndicatorInput(n, false);
                                        GetNextControl(n, true).Focus();
                                        barcode.Clear();
                                    }
                                    else
                                    {
                                        Functions.AlertIndicatorInput(n, true);
                                    }
                                }
                                else if (n.Name.EndsWith("LotNo2") & !string.IsNullOrEmpty(bc))
                                {
                                    string newBC = bc.Replace("\r", "");

                                    if (n.Tag.ToString().Split('_').First() == newBC)
                                    {
                                        n.Text = GetLot(2, newBC);
                                        Functions.AlertIndicatorInput(n, false);
                                        GetNextControl(n, true).Focus();
                                    }
                                    else
                                    {
                                        Functions.AlertIndicatorInput(n, true);
                                    }
                                }
                            }
                        }


                    };

                    n.KeyDown += (o, e) =>
                    {
                        if (n != null)
                        {
                            if (e.KeyCode.Equals(Keys.Enter))
                            {
                                if((n.Name.StartsWith("cbo") && n.Name.EndsWith("er")) || n.Name.Equals("cboShift"))
                                {
                                    GetNextControl(n, true).Focus();
                                }

                                if (n.Name.Equals("txtPlanningDistQty"))
                                {
                                    if (Functions.CheckNotNullNoMessage(txtPlanningDistQty.Text))
                                    {
                                        if (Functions.CheckWinthinTolerance(sub.ToDouble(txtPlanningDistQty.Text), 5, 20) == true)
                                        {
                                            Functions.AlertIndicatorInput(txtPlanningDistQty, false);
                                            GetNextControl(n, true).Focus();
                                        }
                                        else
                                        {
                                            MessageDisplay.Error("Mixing Quantity Error!\n Note: 5 - 20 Kg only acceptable.");
                                            Functions.AlertIndicatorInput(txtPlanningDistQty, true);
                                            sub.Highlight(txtPlanningDistQty);
                                        }
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Please input mixing quantity.");
                                        Functions.AlertIndicatorInput(txtPlanningDistQty, true);
                                        txtPlanningDistQty.Focus();
                                    }
                                }
                                else if (n.Name.Equals("txtMonomerType"))
                                {
                                    txtMonomerType.ReadOnly = true;
                                    //txtMonomerType.Text.Equals(Variables.monomerType[0]) || txtMonomerType.Text.Equals(Variables.monomerType[1])
                                    if (CheckMonomerType(txtMonomerType.Text))
                                    {
                                        clblTitle.Text = "SUBTANK MIXING: " + txtMonomerType.Text;

                                        if (txtMonomerType.Text.Equals(Variables.monomerType[0]))
                                        {
                                            Variables.kemisorb = materialValues[ChemicalCalculations.KEMISORB_R_UV];
                                            Variables.dyeStuff = materialValues[ChemicalCalculations.BLUING_R_UV];
                                            Variables.mrCatalyst = materialValues[ChemicalCalculations.MRCAT_R_UV];
                                        }
                                        else
                                        {
                                            Variables.kemisorb = materialValues[ChemicalCalculations.KEMISORB_R_UVB];
                                            Variables.dyeStuff = materialValues[ChemicalCalculations.BLUING_R_UVB];
                                            Variables.mrCatalyst = materialValues[ChemicalCalculations.MRCAT_R_UVB];
                                        }

                                        DisplayMonomerType(txtMonomerType.Text);
                                        Functions.AlertIndicatorInput(txtMonomerType, false);
                                        GetNextControl(n, true).Focus();
                                    }
                                    else
                                    {
                                        clblTitle.Text = "SUBTANK MIXING";
                                        Functions.AlertIndicatorInput(txtMonomerType, true);
                                        sub.Highlight(txtMonomerType);
                                    }
                                }
                                else if (n.Name.Equals("txtTankTemp"))
                                {

                                    if (Functions.CheckNotNullNoMessage(txtTankTemp.Text))
                                    {
                                        if (Functions.CheckWinthinTolerance(sub.ToDouble(txtTankTemp.Text), 20, 27) == true)
                                        {
                                            Functions.AlertIndicatorInput(txtTankTemp, false);
                                            GetNextControl(n, true).Focus();
                                        }
                                        else
                                        {
                                            MessageDisplay.Error("Temp Error!\n Note: 20°C - 27°C only acceptable.");
                                            sub.Highlight(txtTankTemp);
                                            Functions.AlertIndicatorInput(txtTankTemp, true);
                                        }
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Please input mixing tank temp.");
                                        Functions.AlertIndicatorInput(txtTankTemp, true);
                                        txtTankTemp.Focus();
                                    }
                                }
                                else if (n.Name.Equals("txtStirSpeed"))
                                {
                                    if (!string.IsNullOrEmpty(lblTargetStirSpeed.Text) && !string.IsNullOrEmpty(lblMinStirSpeed.Text))
                                    {
                                        if (Functions.CheckNotNullNoMessage(txtStirSpeed.Text))
                                        {
                                            if (Functions.CheckWinthinTolerance(sub.ToDouble(txtStirSpeed.Text), sub.ToDouble(lblMinStirSpeed.Text), sub.ToDouble(lblTargetStirSpeed.Text)) == true)
                                            {
                                                Functions.AlertIndicatorInput(txtStirSpeed, false);
                                                GetNextControl(n, true).Focus();
                                            }
                                            else
                                            {
                                                MessageDisplay.Error("Stirring speed error!\nNote: " + lblMinStirSpeed.Text + " - " + lblTargetStirSpeed.Text + " only acceptable.");
                                                sub.Highlight(txtStirSpeed);
                                                Functions.AlertIndicatorInput(txtStirSpeed, true);
                                            }
                                        }
                                        else
                                        {
                                            MessageDisplay.Error("Please input stirring speed.");
                                            Functions.AlertIndicatorInput(txtStirSpeed, true);
                                            sub.Highlight(txtStirSpeed);
                                        }
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Please calculate stirring speed before inputting stirring speed.");
                                        btnCalcStirSpeed.Focus();
                                    }

                                }
                                else if (n.Name.Equals("txtInjTankTemp"))
                                {
                                    if (Functions.CheckNotNullNoMessage(txtInjTankTemp.Text))
                                    {
                                        if (Functions.CheckWinthinTolerance(sub.ToDouble(txtInjTankTemp.Text), 20, 27) == true)
                                        {
                                            Functions.AlertIndicatorInput(txtInjTankTemp, false);
                                            GetNextControl(n, true).Focus();
                                        }
                                        else
                                        {
                                            MessageDisplay.Error("Temp Error!\n Note: 20°C - 27°C only acceptable.");
                                            sub.Highlight(txtInjTankTemp);
                                            Functions.AlertIndicatorInput(txtInjTankTemp, true);
                                        }
                                    }
                                    else
                                    {
                                        MessageDisplay.Error("Please input injection tank temp.");
                                        Functions.AlertIndicatorInput(txtInjTankTemp, true);
                                        sub.Highlight(txtInjTankTemp);
                                    }

                                }
                                else if (n.Name.Equals("txtInjTankUnit"))
                                {
                                    if (CheckInjectionTankNoIfExist(txtInjTankUnit.Text) == true)
                                    {
                                        MessageDisplay.Error("This injection tank number is already used for this batch number");
                                        sub.Highlight(txtInjTankUnit);
                                    }
                                    else
                                    {
                                        GetNextControl(n, true).Focus();
                                    }
                                }
                                else if (n.Name.EndsWith("LotNo1") | n.Name.EndsWith("LotNo2"))
                                {
                                    //
                                }
                                else
                                {
                                    GetNextControl(n, true).Focus();
                                }
                            }
                        }
                    };
                });
            }
            catch
            {
                throw;
            }
        }

        private void EnableDisableControls(Process p, bool stat)
        {
            switch (p)
            {
                case Process.Renew:
                    btnRenew.Enabled = stat;
                    if (stat) { btnRenew.Focus(); }
                    break;

                case Process.RenewStart:
                    MultipleEnableDisableControls("RenewStart", stat);
                    if (stat) { txtPlanningDistQty.Focus(); }
                    break;

                case Process.MixingTankReset:
                    btnTankReset.Enabled = stat;
                    if (stat) { btnTankReset.Focus(); }
                    break;

                case Process.MixingTankGet:
                    btnTankTransferWeight.Enabled = stat;
                    if (stat) { btnTankTransferWeight.Focus(); }
                    break;

                case Process.MixingTankStirring:
                    btnStartStirringMain.Enabled = stat;
                    if (stat) { btnStartStirringMain.Focus(); }
                    break;
                case Process.MixingTankStirrSpeed:
                    MultipleEnableDisableControls("CalcStirrSpeedMain", stat);
                    if (stat) { btnCalcStirSpeed.Focus(); }
                    break;

                case Process.KemiDyeReset:
                    MultipleEnableDisableControls("KemiDyeReset", stat);
                    MultipleEnableDisableControls("KEMISORB_KemiDyeReset", stat);
                    MultipleEnableDisableControls("DYESTUFF_KemiDyeReset", stat);
                    if (stat)
                    {
                        txtStirSpeed.Enabled = stat;
                        txtStirSpeed.Focus();
                    }
                    break;

                case Process.KemiDyeBefore:
                    btnKemiDyestuffBeforeInput.Enabled = stat;
                    if (stat) { btnKemiDyestuffBeforeInput.Focus(); }
                    break;

                case Process.KemiDyeAfter:
                    btnKemiDyestuffAfterInput.Enabled = stat;
                    if (stat) { btnKemiDyestuffAfterInput.Focus(); }
                    break;

                case Process.MRCatReset:
                    MultipleEnableDisableControls("MRCatReset", stat);
                    MultipleEnableDisableControls("MR-CATALYST_MRCatReset", stat);
                    if (stat) { txtMRCatLotNo1.Focus(); }
                    break;

                case Process.MRCatBefore:
                    btnMRCatBeforeInput.Enabled = stat;
                    if (stat) { btnMRCatBeforeInput.Focus(); }
                    break;

                case Process.MRCatAfter:
                    btnMRCatAfterInput.Enabled = stat;
                    if (stat) { btnMRCatAfterInput.Focus(); }
                    break;

                case Process.MRCatStirring:
                    btnMRCatDissolution.Enabled = stat;
                    if (stat) { btnMRCatDissolution.Focus(); }
                    break;

                case Process.AddFreshValidation:
                    MultipleEnableDisableControls("AddFreshValidation", stat);
                    if(stat) { btnAddFreshYes.Focus(); }
                    break;

                case Process.AddFreshYes:
                    txtFreshTemp.Enabled = stat;
                    txtFreshTemp.Enabled = stat;

                    MultipleEnableDisableControls("AddFresh", stat);
                    if (stat) { txtFreshTemp.Focus(); }
                    break;

                case Process.AddExcessValidation:
                    MultipleEnableDisableControls("AddExcessValidation", stat);
                    if (stat) { btnAddExcessYes.Focus(); }
                    break;


                case Process.AddExcessYes:
                    txtExcessTemp.Enabled = stat;
                    txtExcessWeight.Enabled = stat;

                    MultipleEnableDisableControls("AddExcess", stat);
                    if (stat) { txtExcessTemp.Focus(); }
                    break;

                case Process.Deagassing:
                    btnDegassing.Enabled = stat;
                    if (stat) { btnDegassing.Focus(); }
                    break;

                case Process.NextBatch:
                    txtInjTankTempTwo.Enabled = stat;
                    btnNextBatch.Enabled = stat;

                    if (stat) { txtInjTankTempTwo.Focus(); }
                    break;

                case Process.Return:
                    btnReturn.Enabled = stat;
                    break;

                case Process.End:
                    btnEnd.Enabled = stat;
                    break;

            }
        }

        private void MultipleEnableDisableControls(string tag, bool stat)
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

        private void ClearColor()
        {
            this.Controls.OfType<Label>().ToList().ForEach(n =>
            {
                if (n.Name.StartsWith("clbl"))
                {
                    n.BackColor = SystemColors.Control;
                    n.ForeColor = Color.Black;
                }
            });
        }

        private void CalcKemisorb(string qty, string mono)
        {
            try
            {
                double std = calc.ComputeStdValues(Chemicals.KEMISORB, sub.ToDouble(qty));
                double upper = 0, lower = 0;

                if (mono.Equals("UV"))
                {
                    materialTolerance = materialValues[ChemicalCalculations.KEMISORB_TOL_UV];

                    upper = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero);
                    lower = Math.Round(std * (1 - materialTolerance), 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    materialTolerance = materialValues[ChemicalCalculations.KEMISORB_TOL_UVB];

                    upper = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero);
                    lower = Math.Round(std * (1 - materialTolerance), 2, MidpointRounding.AwayFromZero);
                }

                lblKemiStandard.Text = std.ToString("N2");
                lblKemiUpper.Text = upper.ToString("N2");
                lblKemiLower.Text = lower.ToString("N2");

                tolerance.upperLimitKemi = upper.ToString("N2");
                tolerance.lowerLimitKemi = lower.ToString("N2");
            }
            catch
            {
                throw;
            }

        }

        private void CalcDyestuff(string qty, string mono)
        {
            try
            {
                double std = calc.ComputeStdValues(Chemicals.BLUING, sub.ToDouble(qty));
                double upper = 0, lower = 0;

                if (mono.Equals("UV"))
                {
                    materialTolerance = materialValues[ChemicalCalculations.BLUING_TOL_UV];

                    upper = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero);
                    lower = Math.Round(std * (1 - materialTolerance), 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    materialTolerance = materialValues[ChemicalCalculations.BLUING_TOL_UVB];

                    upper = Math.Round(std * (1 + materialTolerance), 2, MidpointRounding.AwayFromZero);
                    lower = Math.Round(std * (1 - materialTolerance), 2, MidpointRounding.AwayFromZero);
                }

                lblDyestuffStandard.Text = std.ToString("N2");
                lblDyestuffUpper.Text = upper.ToString("N2");
                lblDyestuffLower.Text = lower.ToString("N2");

                tolerance.upperLimitDyestuff = upper.ToString("N2");
                tolerance.lowerLimitDyestuff = lower.ToString("N2");
            }
            catch
            {
                throw;
            }

        }

        private void CalcMRCat(string qty, string mono)
        {
            try
            {
                double std = calc.ComputeStdValues(Chemicals.MRCATALYST, sub.ToDouble(qty));
                double upper = 0, lower = 0;

                if (mono.Equals("UV"))
                {
                    materialTolerance = materialValues[ChemicalCalculations.MRCAT_TOL_UV];

                    upper = Math.Round(std * (1 + materialTolerance), 3, MidpointRounding.AwayFromZero);
                    lower = Math.Round(std * (1 - materialTolerance), 3, MidpointRounding.AwayFromZero);
                }
                else
                {
                    materialTolerance = materialValues[ChemicalCalculations.MRCAT_TOL_UVB];

                    upper = Math.Round(std * (1 + materialTolerance), 3, MidpointRounding.AwayFromZero);
                    lower = Math.Round(std * (1 - materialTolerance), 3, MidpointRounding.AwayFromZero);
                }

                lblMRCatStandard.Text = std.ToString("N3");
                lblMRCatUpper.Text = upper.ToString("N3");
                lblMRCatLower.Text = lower.ToString("N3");

                SetTolerance(lblMRCatUpper.Text, lblMRCatLower.Text);
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

        private bool CheckInjectionTankNoIfExist(string inj)
        {
            bool stat = false;

            for (int i = 0; i < injBatch.Length - 1; i++)
            {
                if (injBatch[i] == inj)
                {
                    stat = true;
                    break;
                }
            }

            return stat;
        }

        private bool CheckMonomerType(string input)
        {
            bool stat = false;

            for (int i = 0; i < Variables.monomerType.Length; i++)
            {
                if(Variables.monomerType[i] == input)
                {
                    stat = true;
                    break;
                }
            }

            return stat;
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

                    Thread.Sleep(1000);

                    foreach (KeyValuePair<WeighingScales, string> item in SocketSettings)
                    {
                        if (!Variables.Machine.wait)
                        {
                            if (item.Key != WeighingScales.MOXA)
                            {
                                int port = Convert.ToInt32(item.Value);

                                switch (item.Key)
                                {
                                    case WeighingScales.TANK:
                                        lblMixingTankStatus.BackColor = scale.GetColor(CheckLoadCellConnection(port, Variables.ip));
                                        break;
                                    case WeighingScales.KEMISORB:
                                        lblKemiStatus.BackColor = scale.GetColor(CheckConnection.Invoke(port, Variables.ip));
                                        break;

                                    case WeighingScales.DYESTUFF:
                                        lblDyeStatus.BackColor = scale.GetColor(CheckConnection.Invoke(port, Variables.ip));
                                        break;

                                    case WeighingScales.MRCAT:
                                        lblMRCatStatus.BackColor = scale.GetColor(CheckConnection.Invoke(port, Variables.ip));
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
            catch (Exception er)
            {
                MessageDisplay.Error(er.Message);
            }

            return lot;
        }

        private void InitializeControls()
        {
            EnableDisableControls(Process.RenewStart, false);
            EnableDisableControls(Process.MixingTankReset, false);
            EnableDisableControls(Process.MixingTankGet, false);
            EnableDisableControls(Process.MixingTankStirring, false);
            EnableDisableControls(Process.MixingTankStirrSpeed, false);
            EnableDisableControls(Process.KemiDyeReset, false);
            EnableDisableControls(Process.KemiDyeBefore, false);
            EnableDisableControls(Process.KemiDyeAfter, false);
            EnableDisableControls(Process.MRCatReset, false);
            EnableDisableControls(Process.MRCatBefore, false);
            EnableDisableControls(Process.MRCatAfter, false);
            EnableDisableControls(Process.AddExcessValidation, false);
            EnableDisableControls(Process.AddFreshValidation, false);

            EnableDisableControls(Process.AddExcessYes, false);
            EnableDisableControls(Process.AddFreshYes, false);
            EnableDisableControls(Process.MRCatStirring, false);
            EnableDisableControls(Process.Deagassing, false);
            EnableDisableControls(Process.NextBatch, false);
            EnableDisableControls(Process.Return, true);
            EnableDisableControls(Process.End, true);
            EnableDisableControls(Process.Renew, true);
        }

        private string GetVersion()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return v.ToString();
        }

        private void DB(ProcessM p)
        {
            try
            {
                using (DAL dal = new DAL())
                {
                    switch (p)
                    {
                        case ProcessM.RenewSub:
                            string batchno = string.Empty;

                            batchno = dal.GetSingleData(new ParameterData
                            {
                                sqlQuery = Query.GetBatchNumber(Mixing.SUBDIVISION)
                            });

                            if (batchno.Equals("-"))
                            {
                                lblBatchNo.Text = batchno;
                                lblInjBatchNo.Text = batchno;
                            }
                            else
                            {
                                lblBatchNo.Text = batchno.Substring(0, 11);

                                lblInjBatchNo.Text = batchno.Substring(batchno.LastIndexOf('-')).Replace("-", "");

                                //Getting all injection tank number per batch number
                                injBatch = null;

                                injBatch = dal.GetSingleCollection(new ParameterData
                                {
                                    sqlQuery = Query.GetAllInjectionTankNo(lblBatchNo.Text)
                                });

                                //Test
                                //lblRemainingMixQty.Text = dal.GetSingleData(new ParameterData
                                //{
                                //    sqlQuery = Query.GetRemainedTankBeforeTransfer(lblBatchNo.Text, lblInjBatchNo.Text)
                                //});
                            }

                            break;

                        case ProcessM.MixingTankReset:

                            //lblTankBeforeTransfer.Text = dal.GetSingleData(new ParameterData
                            //{
                            //    sqlQuery = Query.GetRemainedTankBeforeTransfer(lblBatchNo.Text, lblInjBatchNo.Text)
                            //});

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveUpdateData(ProcessM.MixingTankReset, txtPlanningDistQty.Text, Variables.mixerID.ToString(), Variables.checkerID.ToString(), cboShift.Text,
                                                                txtMonomerType.Text, txtTankTemp.Text, txtInjTankUnit.Text, lblRemainingMixQty.Text, Variables.batchNo)
                            });

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveResetData(ProcessM.MixingTankReset, Variables.batchNo, lblTankZeroReset.Text)
                            });

                            break;

                        case ProcessM.MixingTankGet:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveWeightData(ProcessM.MixingTankGet, lblTransferWeigt.Text, lblTankAfterTransfer.Text, Variables.batchNo)
                            });

                            //Update control master to proceed main mixing
                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.UpdateBatchMainSubLink(Variables.batchNo.Substring(0, 8))
                            });

                            break;


                        case ProcessM.KemiDyeReset:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveUpdateData(ProcessM.KemiDyeReset, txtStirSpeed.Text, txtInjTankTemp.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.KemiReset:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveResetData(ProcessM.KemiReset, Variables.batchNo, lblKemiStandard.Text, lblKemiUpper.Text, lblKemiLower.Text, txtKemiLotNo1.Text, txtKemiLotNo2.Text)
                            });

                            break;

                        case ProcessM.KemiBefore:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveWeightData(ProcessM.KemiBefore, lblKemiBeforeInput.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.KemiAfter:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveWeightData(ProcessM.KemiAfter, lblKemiAfterInput.Text, lblKemiActualInput.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.DyeReset:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveResetData(ProcessM.DyeReset, Variables.batchNo, lblDyestuffStandard.Text, lblDyestuffUpper.Text, lblDyestuffLower.Text, txtDyestuffLotNo1.Text, txtDyestuffLotNo2.Text)
                            });

                            break;

                        case ProcessM.DyeBefore:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveWeightData(ProcessM.DyeBefore, lblDyestuffBeforeInput.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.DyeAfter:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveWeightData(ProcessM.DyeAfter, lblDyestuffAfterInput.Text, lblDyestuffActualInput.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.MRCatReset:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveResetData(ProcessM.MRCatReset, Variables.batchNo, lblMRCatStandard.Text, lblMRCatUpper.Text, lblMRCatLower.Text, txtMRCatLotNo1.Text, txtMRCatLotNo2.Text)
                            });

                            break;

                        case ProcessM.MRCatBefore:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveWeightData(ProcessM.MRCatBefore, lblMRCatBeforeInput.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.MRCatAfter:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveWeightData(ProcessM.MRCatAfter, lblMRCatAfterInput.Text, lblMRCatActualInput.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.MRCatStirring:

                            break;

                        case ProcessM.AddFreshYes:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveUpdateData(ProcessM.AddFreshYes, lblFreshMax.Text, txtFreshTemp.Text, txtFreshWeight.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.AddExcessYes:

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveUpdateData(ProcessM.AddExcessYes, lblExcessMax.Text, txtExcessTemp.Text, txtExcessWeight.Text, Variables.batchNo)
                            });

                            break;

                        case ProcessM.NextBatchSub:


                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.SaveUpdateData(ProcessM.NextBatchSub, txtInjTankTempTwo.Text, Variables.batchNo)
                            });

                            //dal.ExecuteQuery(new ParameterData
                            //{
                            //    sqlQuery = Query.UpdateBatchMainSubLink(Variables.batchNo.Substring(0, 8))
                            //});

                            break;

                        case ProcessM.ReturnSub:

                            if (string.IsNullOrEmpty(lblTransferWeigt.Text))
                            {
                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.DeleteBatchNumber(Mixing.SUBDIVISION, Variables.batchNo)
                                });

                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.DeleteBatchNumberProcess(Mixing.SUBDIVISION, Variables.batchNo)
                                });

                            }
                            else
                            {
                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.UpdateBatchNumberReturn(Mixing.SUBDIVISION, Variables.batchNo, Variables.returnID)
                                });
                            }

                            dal.ExecuteQuery(new ParameterData
                            {
                                sqlQuery = Query.ReturnControlMaster(Variables.batchNo.Substring(0, 8))
                            });

                            break;

                        case ProcessM.ForceClosing:

                            if (string.IsNullOrEmpty(lblTransferWeigt.Text))
                            {
                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.DeleteBatchNumber(Mixing.SUBDIVISION, Variables.batchNo)
                                });

                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.DeleteBatchNumberProcess(Mixing.SUBDIVISION, Variables.batchNo)
                                });

                            }
                            else if (!string.IsNullOrEmpty(lblBatchNo.Text))
                            {
                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.DeleteBatchNumber(Mixing.SUBDIVISION, Variables.batchNo)
                                });

                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.DeleteBatchNumberProcess(Mixing.SUBDIVISION, Variables.batchNo)
                                });
                            }
                            else
                            {
                                dal.ExecuteQuery(new ParameterData
                                {
                                    sqlQuery = Query.UpdateBatchNumberReturn(Mixing.SUBDIVISION, Variables.batchNo, Variables.returnID)
                                });
                            }

                            break;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

    }

    public enum Process
    {
        Renew, RenewStart, MixingTankReset, MixingTankGet, MixingTankStirring, MixingTankStirrSpeed, KemiDyeReset, KemiDyeBefore, KemiDyeAfter, ForceClosing,
        MRCatReset, MRCatBefore, MRCatAfter, MRCatStirring, AddFreshValidation, AddFreshYes, AddExcessValidation, AddExcessYes, Deagassing, NextBatch, Return,End
    }
}
