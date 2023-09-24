using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MR_8_80_Liters_Mixing_System
{
    public class Calculations
    {
        public double ComputeStdValues(Chemicals c, double qty)
        {
            double val = 0;

            switch(c)
            {
                case Chemicals.MR8B1:
                    val = Variables.mr8B1 * qty;
                    break;

                case Chemicals.MR8B2:
                    val = Variables.mr8B2 * qty;
                    break;

                case Chemicals.MR8A:
                    val = Variables.mr8A * qty;
                    break;

                case Chemicals.MR_RELEASING:
                    val = (Variables.mrReleasing * qty) * 1000;
                    break;

                case Chemicals.KEMISORB:
                    val = (Variables.kemisorb * qty) * 1000;
                    break;

                case Chemicals.BLUING:
                    val = (Variables.dyeStuff * qty) * 1000;
                    break;

                case Chemicals.MRCATALYST:
                    val = (Variables.mrCatalyst * qty) * 1000;
                    break;
            }

            return val;
        }



        public string CalculateTargetStirringSpeed(Mixing m, string qty)
        {
            double val = 0;

            if (m == Mixing.MAIN)
            {
                if (Convert.ToInt32(qty) >= 61 && Convert.ToInt32(qty) <= 80)
                {
                    val = 230;
                }
                else
                {
                    val = Math.Round(((3.2 * Convert.ToInt32(qty)) + 30));
                }

            }
            else
            {
                if (Convert.ToInt32(qty) >= 10)
                {
                    val = 300;
                }
                else
                {
                    val = 250;
                }
            }

            return val.ToString();
        }

        public string CalculateMininumStirringSpeed(Mixing m, string qty, string tagetSpeed)
        {
            double min = 0;

            if (m == Mixing.MAIN)
            {
                if(Convert.ToInt32(qty) >= 61 && Convert.ToInt32(qty) <= 80)
                {
                    min = 220;
                }
                else
                {
                    min = Math.Round((Convert.ToInt32(tagetSpeed) - 10) / 5.0) * 5;
                }
            }
            else
            {
                if (Convert.ToInt32(qty) >= 10)
                {
                    min = 260;
                }
                else
                {
                    min = 150;
                }
            }

            return min.ToString();
        }

        public string[] GetStirringSpeedMainTank(string qty)
        {
            string[] result = new string[2];

            if (Convert.ToInt32(qty) <= 150 && Convert.ToInt32(qty) >= 101)
            {
                result[0] = "200";
                result[1] = "250";
            }
            else if (Convert.ToInt32(qty) <= 100 && Convert.ToInt32(qty) >= 71)
            {
                result[0] = "160";
                result[1] = "200";
            }
            else 
            {
                result[0] = "120";
                result[1] = "150";
            }

            return result;
        }

        public string CalculateExcessMaxWeight(bool freshWeightInput,double freshWeight,double plandistQty)
        {
            double val = 0;



            return val.ToString();
        }
    }

    public struct Tolerance
    {
        public string upperLimit { get; set; }
        public string lowerLimit { get; set; }
        public string weightBefore { get; set; }
        public string weightAfter { get; set; }

        public string upperLimitKemi { get; set; }
        public string lowerLimitKemi { get; set; }
        public string upperLimitDyestuff { get; set; }
        public string lowerLimitDyestuff { get; set; }

        public string weightBeforeKemi { get; set; }
        public string weightAfterKemi { get; set; }

        public string weightBeforeDyestuff { get; set; }
        public string weightAfterDyestuff { get; set; }


        public string excessWeight { get; set; }
        public string freshWeight { get; set; }

        public bool retVal { get; set; }

        public String Weight
        {
            get
            {
                if(!string.IsNullOrEmpty(weightBefore) & !string.IsNullOrEmpty(weightAfter))
                {
                    return (ToDouble(weightBefore) - ToDouble(weightAfter)).ToString("N2");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public String Weight3Decimal
        {
            get
            {
                if (!string.IsNullOrEmpty(weightBefore) && !string.IsNullOrEmpty(weightAfter))
                {

                    return (ToDouble(weightBefore) - ToDouble(weightAfter)).ToString("N3");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string Weight2Chemicals(Chemicals c)
        {
            string val = string.Empty;

            if(c.Equals(Chemicals.KEMISORB))
            {
                if(!string.IsNullOrEmpty(weightBeforeKemi) && !string.IsNullOrEmpty(weightAfterKemi))
                {
                    val = (ToDouble(weightBeforeKemi) - ToDouble(weightAfterKemi)).ToString("N2");
                }
                else
                {
                    val = string.Empty;
                }
            }
            else if ( c.Equals(Chemicals.BLUING))
            {
                if(!string.IsNullOrEmpty(weightBeforeDyestuff) && !string.IsNullOrEmpty(weightAfterDyestuff))
                {
                    val = (ToDouble(weightBeforeDyestuff) - ToDouble(weightAfterDyestuff)).ToString("N2");
                }
                else
                {
                    val = string.Empty;
                }
            }

            return val;
        }

        public bool WeightBeforeOver2Chemicals(Chemicals c)
        {

            if (c.Equals(Chemicals.KEMISORB))
            {
                if (ToDouble(weightBeforeKemi) <= ToDouble(upperLimitKemi) && ToDouble(weightBeforeKemi) >= ToDouble(lowerLimitKemi))
                {
                    retVal = false;
                }
                else
                {
                    retVal = true;
                }
            }
            else if (c.Equals(Chemicals.BLUING))
            {
                if (ToDouble(weightBeforeDyestuff) <= ToDouble(upperLimitDyestuff) && ToDouble(weightBeforeDyestuff) >= ToDouble(lowerLimitDyestuff))
                {
                    retVal = false;
                }
                else
                {
                    retVal = true;
                }

            }

            return retVal;
        }

        public bool WeightBeforeOver
        {
            get
            {
                if (ToDouble(weightBefore) <= ToDouble(upperLimit) && ToDouble(weightBefore) >= ToDouble(lowerLimit))
                {
                    retVal = false;
                }
                else
                {
                    retVal = true;
                }

                return retVal;
            }
        }

        //For 2 decimals
        public bool WeightOver
        {
            get
            {
                if((ToDouble(Weight) <= ToDouble(upperLimit)) & (ToDouble(Weight) >= ToDouble(lowerLimit)))
                {
                    retVal = false;
                }
                else
                {
                    retVal = true;
                }

                return retVal;
            }
        }

        //For 3 decimals
        public bool WeightOver3D
        {
            get
            {
                if (ToDouble(Weight3Decimal) <= ToDouble(upperLimit) && ToDouble(Weight3Decimal) >= ToDouble(lowerLimit))
                {
                    retVal = false;
                }
                else
                {
                    retVal = true;
                }

                return retVal;
            }
        }

        public bool WeightOver2Chemicals(Chemicals c)
        {
            if (c.Equals(Chemicals.KEMISORB))
            {
                if((ToDouble(Weight2Chemicals(c)) <= ToDouble(upperLimitKemi)) & (ToDouble(Weight2Chemicals(c)) >= ToDouble(lowerLimitKemi)))
                {
                    retVal = false;
                }
                else
                {
                    retVal = true;
                }
            }
            else if (c.Equals(Chemicals.BLUING))
            {
                if ((ToDouble(Weight2Chemicals(c)) <= ToDouble(upperLimitDyestuff)) & (ToDouble(Weight2Chemicals(c)) >= ToDouble(lowerLimitDyestuff)))
                {
                    retVal = false;
                }
                else
                {
                    retVal = true;
                }
            }

            return retVal;
        }

        private double ToDouble(string val)
        {
            return Convert.ToDouble(val);
        }

    }

    public enum Chemicals
    {
        MR8B1,MR8B2,MR8A,MR_RELEASING, KEMISORB, BLUING,  MRCATALYST, TANK
    }

    public enum Mixing
    {
        MAIN,SUBDIVISION
    }
}
