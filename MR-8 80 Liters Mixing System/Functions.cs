using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MR_8_80_Liters_Mixing_System
{
    public class Functions
    {
        //Check for null values
        public static bool CheckNotNull(string values)
        {
            string[] splitVal = values.Split('|');
            bool result = true;

            for (int i = 0; i <= splitVal.Length - 1; i++)
            {
                if (splitVal[i] == "" || string.IsNullOrWhiteSpace(splitVal[i]))
                {
                    result = false;
                    break;
                }
            }

            if (result == false) { MessageDisplay.Error("Please fill - out required fields!"); }

            return result;
        }

        public static bool CheckNotNullNoMessage(string val)
        {
            string[] splitVal = val.Split('|');
            bool result = true;

            for (int i = 0; i <= splitVal.Length - 1; i++)
            {
                if (splitVal[i] == "" || string.IsNullOrWhiteSpace(splitVal[i]))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        //Check for values if within tolerance
        public static bool CheckWinthinTolerance(double val, double lowVal, double highVal)
        {
            return (val >= lowVal && val <= highVal) ? true : false;
        }

        //Highlight values
        public void Highlight(System.Windows.Forms.TextBox txtObj)
        {
            txtObj.SelectionStart = 0;
            txtObj.SelectionLength = txtObj.Text.Length;
            txtObj.Focus();
        }

        public void HighlightControl(System.Windows.Forms.Control t)
        {
            if(t is TextBox)
            {
                ((TextBox)t).SelectionStart = 0;
                ((TextBox)t).SelectionLength = ((TextBox)t).Text.Length;
                ((TextBox)t).Focus();
            }
        }

        //Accepts numbers and decimal only. 
        //This method prevents multiple decimal point input
        public void AcceptNumOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if (e.KeyChar == '.' && (sender as TextBox).Text.IndexOf('.') > -1)
            {
                e.Handled = true;
            }
        }

        public void NumberOnlyNoSpecial(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        //Interface functions controls
        public static void ClearData(Control c,Mixing m)
        {
            c.Controls.OfType<Control>().ToList().ForEach(n =>
            {
                if (n is TextBox)
                {
                    TextBox t = (TextBox)n;

                    if (t.Name.StartsWith("txt"))
                    {
                        if (t.Name.EndsWith("LotNo1"))
                        {
                            t.BackColor = Color.White;
                            t.Clear();
                        }
                        else
                        {
                            t.BackColor = Color.White;
                            t.Clear();
                        }
                    }
                    else if (t.Name.StartsWith("lbl"))
                    {
                        t.Clear();
                        t.BackColor = Color.Moccasin;
                    }
                }
                else if (n is Label)
                {
                    Label l = (Label)n;

                    if (l.Name.StartsWith("lbl"))
                    {
                        l.Text = "";
                        l.BackColor = Color.Moccasin;
                    }
                    else if(l.Name.Equals("clblTitle"))
                    {
                        l.Text = "SUBTANK MIXING";
                    }
                    else if(l.Name.Equals("elblErrorMsg") || l.Name.Equals("elblErrorTank"))
                    {
                        l.Text = "";
                        l.BackColor = SystemColors.Control;
                    }
                }
                else if (n is ComboBox)
                {
                    ComboBox cb = (ComboBox)n;

                    if (cb.Name.StartsWith("cbo"))
                    {
                        cb.SelectedIndex = -1;
                    }

                    cb.Text = "";
                }
                else if (n is Button)
                {
                    Button b = (Button)n;

                    if(b.Name.EndsWith("Return") || b.Name.EndsWith("End"))
                    {
                        b.Enabled = true;
                    }

                    if(b.Name.Equals("btnStabilize"))
                    {
                        b.Text = "Stabilize Tank (2 min)";
                    }
                }
            });
        }

        public double ToDouble(string input)
        {
            double temp = 0;

            if (Regex.IsMatch(input, @"\d"))
            {
                temp = Convert.ToDouble(input);
            }

            return temp;
        }


        public static void AlertIndicatorInput(Control c, bool stat)
        {
            if (stat)
            {
                c.BackColor = Color.Red;
            }
            else
            {
                c.BackColor = Color.White;
            }
        }
        public static void AlertIndicatorWeight(Control c, bool stat)
        {
            if (stat)
            {
                c.BackColor = Color.Red;
            }
            else
            {
                c.BackColor = Color.Moccasin;
                c.ForeColor = SystemColors.WindowText;
            }
        }

        public bool CheckResetValidation(ProcessM p, Control[] input, params string[] data)
        {
            bool stat = false;
            string tempData = string.Empty;

            switch (p)
            {
                case ProcessM.MR8B1_Reset:
                    //data[0] = mixing quantity
                    //data[1] = mixer
                    //data[2] = checker
                    //data[3] = shift
                    //data[4] = MR8B1 Temp
                    //data[5] = MR8B1 LotNo1
                    tempData = data[0] + '|' + data[1] + '|' + data[2] + '|' + data[3] + '|' + data[4] + '|' + data[5];

                    if (CheckNotNullNoMessage(tempData))
                    {
                        if (CheckWinthinTolerance(ToDouble(data[0]), 35, 135) == true)
                        {
                            if (string.IsNullOrEmpty(CheckInputValues(data[4])) && CheckWinthinTolerance(ToDouble(data[4]), 20, 27) == true)
                            {
                                AlertIndicatorInput(input[0], false);
                                AlertIndicatorInput(input[1], false);

                                stat = true;
                            }
                            else
                            {
                                MessageDisplay.Error("Temp. error!\nNote: 20 - 27 only acceptable.");
                                HighlightControl(input[4]);
                            }
                        }
                        else
                        {
                            MessageDisplay.Error("Mixing quantity error!\nNote: 35 - 135 only acceptable.");
                            HighlightControl(input[0]);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(data[0].ToString()))
                        {
                            MessageDisplay.Error("Please input mixing quantity.");
                            input[0].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[1].ToString()))
                        {
                            MessageDisplay.Error("Please select mixer.");
                            input[1].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[2].ToString()))
                        {
                            MessageDisplay.Error("Please select checker.");
                            input[2].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[3].ToString()))
                        {
                            MessageDisplay.Error("Please select work shift..");
                            input[3].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[4].ToString()))
                        {
                            MessageDisplay.Error("Please input MR8-B1 Temp.");
                            input[4].Focus();
                        }
                        else if (string.IsNullOrWhiteSpace(data[5].ToString()))
                        {
                            MessageDisplay.Error("Please input lot number 1 for MR8-B1");
                            input[5].Focus();
                        }
                    }

                    break;

                case ProcessM.MR8B2_Reset:
                case ProcessM.MR8A_Reset:
                    //data[0] MR8B2/MR8A Temp
                    //data[1] MR8B2/MR8A Temp
                    tempData = data[0] + '|' + data[1];

                    if (Functions.CheckNotNullNoMessage(tempData))
                    {
                        if (string.IsNullOrEmpty(CheckInputValues(data[0])) && Functions.CheckWinthinTolerance(ToDouble(data[0]), 20, 27))
                        {
                            AlertIndicatorInput(input[0], false);

                            stat = true;
                        }
                        else
                        {
                            if (p.Equals(ProcessM.MR8B2_Reset))
                            {
                                MessageDisplay.Error("Temp. error!\nNote: 20 - 27 only acceptable.");
                                HighlightControl(input[0]);
                            }
                            else
                            {
                                MessageDisplay.Error("Temp. error!\nNote: 20 - 27 only acceptable.");
                                HighlightControl(input[0]);
                            }
                        }
                    }
                    else
                    {
                        if (p.Equals(ProcessM.MR8B2_Reset))
                        {
                            if (string.IsNullOrEmpty(data[0]))
                            {
                                MessageDisplay.Error("Please input MR8-B2 Temp.");
                                input[0].Focus();
                            }
                            else if (string.IsNullOrEmpty(data[1]))
                            {
                                MessageDisplay.Error("Please input lot number 1 for MR8-B2");
                                input[1].Focus();
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(data[0]))
                            {
                                MessageDisplay.Error("Please input MR8-A Temp.");
                                input[0].Focus();
                            }
                            else if (string.IsNullOrWhiteSpace(data[1]))
                            {
                                MessageDisplay.Error("Please input lot number 1 for MR8-A");
                                input[1].Focus();
                            }
                        }
                    }

                    break;

                case ProcessM.MRReleasing_Reset:
                    //data[0] = MR Releasing LotNo1
                    tempData = data[0];

                    if (CheckNotNullNoMessage(tempData))
                    {
                        stat = true;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(data[0]))
                        {
                            MessageDisplay.Error("Please input lot number 1 for MR-Releasing Agent.");
                            input[0].Focus();
                        }
                    }
                    break;

                case ProcessM.MixingTankReset:
                    /* data[0] = Mixing Qty
                     * data[1] = Mixer
                     * data[2] = Checker
                     * data[3] = Shift
                     * data[4] = Monomer Type
                     * data[5] = Tank Temp
                     * data[6] = Injection Tank Unit
                     * data[7] = Remained Mix Qty
                    */
                    tempData = data[0] + '|' + data[1] + '|' + data[2] + '|' + data[3] + '|' + data[4] + '|' + data[5] + '|' + data[6] + '|' + data[7];

                    if (CheckNotNullNoMessage(tempData))
                    {
                        if (CheckWinthinTolerance(ToDouble(data[0]), 5, 20) == true)
                        {
                            if (ToDouble(data[0]) <= ToDouble(data[7]))
                            {
                                if (CheckWinthinTolerance(ToDouble(data[5]), 20, 27) == true)
                                {
                                    AlertIndicatorInput(input[0], false);
                                    AlertIndicatorInput(input[1], false);
                                    AlertIndicatorInput(input[6], false);

                                    stat = true;
                                }
                                else
                                {
                                    MessageDisplay.Error("Injection Tank Temp. error!\nNote: 20 - 27 only acceptable.");
                                    HighlightControl(input[5]);
                                    AlertIndicatorInput(input[5], true);
                                }
                            }
                            else
                            {
                                MessageDisplay.Error("Remained mixed monomer in mixing tank is less than planning distributed quantity.");
                            }
                        }
                        else
                        {
                            MessageDisplay.Error("Mixing quantity error!\nNote: 5 - 20 only acceptable.");
                            HighlightControl(input[0]);
                            AlertIndicatorInput(input[0], true);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(data[0].ToString()))
                        {
                            MessageDisplay.Error("Please input mixing quantity.");
                            input[0].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[6].ToString()))
                        {
                            MessageDisplay.Error("Please input injection tank unit.");
                            input[6].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[1].ToString()))
                        {
                            MessageDisplay.Error("Please select mixer.");
                            input[1].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[2].ToString()))
                        {
                            MessageDisplay.Error("Please select checker.");
                            input[2].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[3].ToString()))
                        {
                            MessageDisplay.Error("Please select work shift..");
                            input[3].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[4].ToString()))
                        {
                            MessageDisplay.Error("Please select monomer type.");
                            input[4].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[5].ToString()))
                        {
                            MessageDisplay.Error("Please input mixing tank temp.");
                            input[5].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[6].ToString()))
                        {
                            MessageDisplay.Error("Please input injection tank number.");
                            input[6].Focus();
                        }
                        
                    }

                    break;

                case ProcessM.KemiDyeReset:

                    tempData = data[0] + '|' + data[1] + '|' + data[2] + '|' + data[3];

                    if (CheckNotNullNoMessage(tempData))
                    {
                        if(CheckWinthinTolerance(ToDouble(data[0]), ToDouble(input[4].Text), ToDouble(input[5].Text)))
                        {
                            if (CheckWinthinTolerance(ToDouble(data[1]), 20, 27) == true)
                            {
                                AlertIndicatorInput(input[0], false);
                                AlertIndicatorInput(input[1], false);
                                stat = true;
                            }
                            else
                            {
                                MessageDisplay.Error("Mixing Tank Temp. error!\nNote: 20 - 27 only acceptable.");
                                HighlightControl(input[1]);
                                AlertIndicatorInput(input[1], true);
                            }
                        }
                        else
                        {
                            MessageDisplay.Error("Stirring Speed Error:\nNote: " + input[4].Text + " - " + input[5].Text + " only acceptable.");
                            HighlightControl(input[0]);
                            AlertIndicatorInput(input[0], true);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(data[0].ToString()))
                        {
                            MessageDisplay.Error("Please input stirring speed.");
                            input[0].Focus();
                        }
                        else if (string.IsNullOrEmpty(data[1].ToString()))
                        {
                            MessageDisplay.Error("Please input injection tank temp.");
                            input[1].Focus();
                        }
                        else if (string.IsNullOrWhiteSpace(data[2].ToString()))
                        {
                            MessageDisplay.Error("Please input kemisorb lot number 1.");
                            input[2].Focus();
                        }
                        else if (string.IsNullOrWhiteSpace(data[3].ToString()))
                        {
                            MessageDisplay.Error("Please input dyestuff lot number 1.");
                            input[3].Focus();
                        }
                    }

                    break;

                case ProcessM.MRCatReset:

                    //data[0] = MR - Cat LotNo1
                    tempData = data[0];

                    if (CheckNotNullNoMessage(tempData))
                    {
                        stat = true;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(data[0]))
                        {
                            MessageDisplay.Error("Please input lot number 1 for MR - Catalyst.");
                            input[0].Focus();
                        }
                    }

                    break;
            }

            return stat;
        }

        public bool CheckActualInputWeight(string net,string gross)
        {
            bool stat = false;

            if(ToDouble(net) > 0 && ToDouble(gross) > 0)
            {
                double lowT = ToDouble(gross) - ToDouble(Variables.setupValue);
                double highT = ToDouble(gross) + ToDouble(Variables.setupValue);

                if ((ToDouble(net) > highT) || (ToDouble(net) < lowT))
                {
                    stat = false;
                }
                else
                {
                    stat = true;
                }
            }

            return stat;
        }

        public string WeightConversionSendValue(string val)
        {
            double weights = ToDouble(val) * 100;

            return weights.ToString("00000");
        }

        public string CheckInputValues(string input)
        {
            double i;
            string val = "";

            if (!double.TryParse(input, out i))
            {
                val = "Error";
            }

            return val;
        }

        public void WriteLogs(string msg)
        {
            try
            {
                string filePath = Application.StartupPath + "\\" + "ErrorLog.txt";

                if (Directory.Exists(filePath))
                {
                    File.AppendAllText(filePath, msg + " -> " + DateTime.Now  +  Environment.NewLine);
                }
                else
                {
                    //DirectoryInfo di = Directory.CreateDirectory(filePath);
                    File.AppendAllText(filePath, msg + " -> " + DateTime.Now  + Environment.NewLine);
                }
            }
            catch 
            {
                throw;
            }
        }

        public static bool IsValidLoadCellWeight(string val)
        {
            bool stat = true;

            if (!string.IsNullOrEmpty(val) && val.Contains(","))
            {
                stat = false;
            }
            else
            {
                stat = true;
            }

            return stat;
        }
    }

}
