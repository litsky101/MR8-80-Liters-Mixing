using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MR_8_80_Liters_Mixing_System
{
    public class ParameterData
    {
        public string sqlQuery { get; set; }

    }

    public class Query
    {
        public static string ReturnControlMaster(string batchNo)
        {
            return string.Format(@"UPDATE ControlMaster SET MixingStatus = 2 WHERE MixingID = 7 AND BatchNo = '{0}'", batchNo);
        }

        public static string  GetBatchNumber(Mixing m)
        {
            string query = string.Empty;

            query = m == Mixing.MAIN ? "SELECT sp_MainBatch80L150L();" : "SELECT sp_Additives80L150L()";

            return query;
        }

        public static string LoadMixer()
        {
            return "SELECT DisplayName FROM UserDetails WHERE MixingID = 7 AND Mixer = 1 ANd Active = 1";
        }


        public static string LoadChecker()
        {
            return "SELECT DisplayName FROM UserDetails WHERE MixingID = 7 AND Checker = 1 ANd Active = 1";
        }

        public static string GetMixerID(string name)
        {
            return string.Format("SELECT UserID FROM UserDetails WHERE DisplayName = '{0}' AND MixingID = 7 AND Mixer = 1 AND Active = 1", name);
        }

        public static string GetCheckerID(string name)
        {
            return string.Format("SELECT UserID FROM UserDetails WHERE DisplayName = '{0}' AND MixingID = 7 AND Checker = 1 AND Active = 1", name);
        }

        public static string GetAdminAccount(int id)
        {
            return string.Format("SELECT UserID FROM UserDetails WHERE MixingID = 7 AND Admin = 1 AND Active = 1 AND UserID = {0}", id);
        }

        public static string DeleteBatchNumber(Mixing m, string batchno)
        {
            string sql = string.Empty;

            if (m == Mixing.MAIN)
            {
                sql = string.Format("DELETE FROM MainMonomer WHERE MixingID = 7 AND BatchNo = '{0}'", batchno);
            }
            else
            {
                sql = string.Format("DELETE FROM Additives WHERE MixingID = 7 AND BatchNo = '{0}'", batchno);
            }

            return sql;

        }

        public static string CheckMixingStatus(string batchno)
        {
            return string.Format(@"SELECT MixingStatus FROM ControlMaster WHERE MixingID = 7 AND BatchNo = {0}", batchno);
        }

        public static string DeleteBatchNumberProcess(Mixing m, string batchno)
        {
            string sql = string.Empty;

            if(m == Mixing.MAIN)
            {
                sql = string.Format("DELETE FROM ProcessDetail WHERE MixingID = 7 AND BatchNo = '{0}' AND ProcessID = 16 AND MaterialID IN (63,64,65,66);", batchno);
            }
            else
            {
                sql = string.Format("DELETE FROM ProcessDetail WHERE MixingID = 7 AND BatchNo = '{0}' AND ProcessID = 17 AND MaterialID IN (67,68,69,70);", batchno);
            }

            return sql;
        }

        public static string UpdateBatchNumberReturn(string batchno)
        {
            return string.Format("UPDATE ControlMaster SET RecordCount = RecordCount - 1 WHERE MixingID = 7  AND BatchNo = '{0}';", batchno.Substring(0, 8));
        }


        public static string UpdatePresentMatWeightMain(string batchno, string presWeight)
        {
            return string.Format(@"UPDATE MainMonomer SET PresentMatWeight = '{0}' WHERE MixingID = 7 AND BatchNo = '{1}'", presWeight, batchno);
        }

        public static string UpdateBatchNumberReturn(Mixing m,string batchno, int id)
        {
            string query = string.Empty;

            if (m == Mixing.MAIN)
                query = "UPDATE MainMonomer SET ReturnID = '{0}', ReturnTime = CURRENT_TIMESTAMP() WHERE MixingID = 7 AND BatchNo = '{1}'";
            else
                query = "UPDATE Additives SET ReturnID = '{0}', ReturnTime = CURRENT_TIMESTAMP() WHERE MixingID = 7 AND BatchNo = '{1}'";

            return string.Format(query, id, batchno);
        }

        public static string UpdateBatchMainSubLink(string batchno)
        {
            return string.Format(@"UPDATE ControlMaster SET MixingStatus = 2 WHERE MixingID = 7 AND BatchNo = '{0}'", batchno);
        }

        public static string GetCalculations()
        {
            return "SELECT Description,Value FROM `Calculation`  WHERE MixingID = 7;";
        }

        public static string GetAllInjectionTankNo(string batchno)
        {
            return string.Format(@"SELECT InjTankNo FROM Additives WHERE MixingID = 7 AND LEFT(BatchNo, 11) = '{0}'", batchno);
        }

        public static string GetRemainedTankBeforeTransfer(string batchno, string injBatchNo)
        {
            string sql = string.Empty;

            if(injBatchNo.Equals("1"))
            {
                sql = string.Format(@"SELECT PresentMatWeight FROM MainMonomer WHERE MixingID = 7 AND BatchNo = '{0}'", batchno.Substring(0, 11));
            }
            else
            {
                sql = string.Format(@"SELECT InputWeight FROM ProcessDetail WHERE MixingID = 7 AND ProcessID = 17 AND MaterialID = 70 AND LEFT(BatchNo, 11) = '{0}' ORDER BY BatchNo DESC LIMIT 1;", batchno);
            }

            return sql;
        }

        public static string GetLotNo(int lotno, string input)
        {
            string sql = string.Empty;

            if(lotno == 1)
                sql = string.Format(@"SELECT Lot1 FROM LotCode WHERE MixingID = 7 AND Material = '{0}'", input);
            else
                sql = string.Format(@"SELECT Lot2 FROM LotCode WHERE MixingID = 7 AND Material = '{0}'", input);

            return sql;
        }

        //Saving Updating data 
        public static string SaveUpdateData(ProcessM p, params string[] data)
        {
            string sql = string.Empty;

            switch(p)
            {
                case ProcessM.RenewStart:
                    sql = string.Format(@"INSERT INTO MainMonomer (BatchNo, MixingID, MixingQty, RemainedQty, Mixer, Checker, Shift, LiquidTemp,RenewTime)
                                        VALUES ('{0}', 7, '{1}', '{2}', {3}, {4}, '{5}', '{6}', CURRENT_TIMESTAMP())", data[0], data[1], data[2],
                                        Convert.ToInt32(data[3]), Convert.ToInt32(data[4]), data[5],data[6]);
                    break;

                case ProcessM.MR8B2_Reset:
                    sql = string.Format(@"UPDATE MainMonomer SET LiquidTemp2 = '{0}' WHERE MixingID = 7 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

                case ProcessM.MR8A_Reset:
                    sql = string.Format(@"UPDATE MainMonomer SET LiquidTemp3 = '{0}' WHERE MixingID = 7 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

                case ProcessM.MRReleasing_GetAfter:
                    sql = string.Format(@"UPDATE MainMonomer SET PresentMatWeight = '{0}' WHERE MixingID = 7 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

                case ProcessM.Stirring:
                    sql = string.Format(@"UPDATE MainMonomer SET StirSpeed1 = '{0}', StirSpeed1Time = CURRENT_TIMESTAMP() WHERE MixingID = 7 AND BatchNo = '{1}'",
                                        data[0], data[1]);
                    break;

                case ProcessM.NextBatch:
                    sql = string.Format(@"UPDATE ControlMaster SET MixingStatus = 1,ProcessID = 17 WHERE MixingID = 7 AND BatchNo = '{0}'", data[0]);
                    break;

                //case ProcessM.RenewSub:
                //    sql = string.Format(@"UPDATE ControlMaster SET MixingStatus = 2 WHERE MixingID = 7 AND LEFT(BatchNo,8) = '{0}'", data[0]);
                //    break;


                case ProcessM.MixingTankReset:
                    sql = string.Format(@"UPDATE Additives SET MixingQty = '{0}', Mixer = '{1}', Checker = '{2}', Shift = '{3}', MonomerType = '{4}', LiquidTemp = '{5}', InjTankNo = '{6}', RemainedQty = '{7}' 
                                        WHERE MixingID = 7 AND BatchNo = '{8}'",data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8]);
                    break;

                case ProcessM.KemiDyeReset:
                    sql = string.Format(@"UPDATE Additives SET StirrSpeed1 = '{0}', StirrSpeed1Time = CURRENT_TIMESTAMP(), LiquidTemp2 = '{1}' WHERE 
                                        MixingID = 7 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

                case ProcessM.AddFreshYes:
                    sql = string.Format(@"UPDATE Additives SET WTFresh = '{0}', TempFresh = '{1}', FreshInputQty = '{2}' WHERE MixingID = 7 AND BatchNo = '{3}'", data[0], data[1], data[2], data[3]);
                    break;

                case ProcessM.AddExcessYes:
                    sql = string.Format(@"UPDATE Additives SET WTExcess = '{0}', TempExcess = '{1}', ExcessInputQty = '{2}' WHERE MixingID = 7 AND BatchNo = '{3}'", data[0], data[1], data[2], data[3]);
                    break;

                case ProcessM.NextBatchSub:
                    sql = string.Format(@"UPDATE Additives SET LiquidTemp3 = '{0}' WHERE MixingID = 7 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

            }

            return sql;
        }

        //Saving Updating data 
        public static string SaveResetData(ProcessM p, params string[] data)
        {
            string sql = string.Empty;

            switch(p)
            {
                case ProcessM.MR8B1_Reset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, Lot1, Lot2, Standard, Upper, Lower, ResetTime, BeforeInput, BefInputTime)
                               VALUES (7, 16, 63, '{0}', '{1}', '{2}', '{3}', '{4}', '{5}',CURRENT_TIMESTAMP(), '{6}', CURRENT_TIMESTAMP())", data[0], data[1],
                               data[2], data[3], data[4], data[5], data[6]);
                    break;

                case ProcessM.MR8B2_Reset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, Lot1, Lot2, Standard, Upper, Lower, ResetTime, BeforeInput, BefInputTime)
                               VALUES (7, 16, 64, '{0}', '{1}', '{2}', '{3}', '{4}', '{5}',CURRENT_TIMESTAMP(), '{6}', CURRENT_TIMESTAMP())", data[0], data[1],
                               data[2], data[3], data[4], data[5], data[6]);
                    break;

                case ProcessM.MR8A_Reset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, Lot1, Lot2, Standard, Upper, Lower, ResetTime, BeforeInput, BefInputTime)
                               VALUES (7, 16, 65, '{0}', '{1}', '{2}', '{3}', '{4}', '{5}',CURRENT_TIMESTAMP(), '{6}', CURRENT_TIMESTAMP())", data[0], data[1],
                               data[2], data[3], data[4], data[5], data[6]);
                    break;

                case ProcessM.MRReleasing_Reset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, Lot1, Lot2, Standard, Upper, Lower, ResetTime)
                               VALUES (7, 16, 66, '{0}', '{1}', '{2}', '{3}', '{4}', '{5}',CURRENT_TIMESTAMP())", data[0], data[1],
                               data[2], data[3], data[4], data[5]);
                    break;

                case ProcessM.MixingTankReset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, ResetTime, BeforeInput, BefInputTime)
                                VALUES (7, 17, 70, '{0}', CURRENT_TIMESTAMP(), '{1}', CURRENT_TIMESTAMP())", data[0], data[1]);
                    break;

                case ProcessM.KemiReset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, Standard, Upper, Lower, ResetTime, Lot1, Lot2)
                                VALUES (7, 17, 67, '{0}', '{1}', '{2}','{3}',CURRENT_TIMESTAMP(),'{4}','{5}')", data[0], data[1], data[2], data[3], data[4], data[5]);
                    break;

                case ProcessM.DyeReset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, Standard, Upper, Lower, ResetTime, Lot1, Lot2)
                                VALUES (7, 17, 68, '{0}', '{1}', '{2}','{3}',CURRENT_TIMESTAMP(),'{4}','{5}')", data[0], data[1], data[2], data[3], data[4], data[5]);
                    break;

                case ProcessM.MRCatReset:
                    sql = string.Format(@"INSERT INTO ProcessDetail(MixingID, ProcessID, MaterialID, BatchNo, Standard, Upper, Lower, ResetTime, Lot1, Lot2)
                                VALUES (7, 17, 69, '{0}', '{1}', '{2}','{3}',CURRENT_TIMESTAMP(),'{4}','{5}')", data[0], data[1], data[2], data[3], data[4], data[5]);
                    break;
            }

            return sql;
        }

        //Saving Updating data
        public static string SaveWeightData(ProcessM p, params string[] data)
        {
            string sql = string.Empty;

            switch(p)
            {
                case ProcessM.MR8B1_Get:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 63 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

                case ProcessM.MR8B2_Get:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 64 AND BatchNo = '{2}'", data[0], data[1],data[2]);
                    break;

                case ProcessM.MR8A_Get:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 65 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

                case ProcessM.MRReleasing_GetBefore:
                    sql = string.Format(@"UPDATE ProcessDetail SET BeforeInput = '{0}', BefInputTime = CURRENT_TIMESTAMP() WHERE MixingID = 7 
                                       AND MaterialId = 66 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

                case ProcessM.MRReleasing_GetAfter:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 66 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

                case ProcessM.MixingTankGet:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 70 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

                case ProcessM.KemiBefore:
                    sql = string.Format(@"UPDATE ProcessDetail SET BeforeInput = '{0}', BefInputTime = CURRENT_TIMESTAMP() WHERE MixingID = 7 
                                       AND MaterialId = 67 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

                case ProcessM.KemiAfter:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 67 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

                case ProcessM.DyeBefore:
                    sql = string.Format(@"UPDATE ProcessDetail SET BeforeInput = '{0}', BefInputTime = CURRENT_TIMESTAMP() WHERE MixingID = 7 
                                       AND MaterialId = 68 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

                case ProcessM.DyeAfter:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 68 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

                case ProcessM.MRCatBefore:
                    sql = string.Format(@"UPDATE ProcessDetail SET BeforeInput = '{0}', BefInputTime = CURRENT_TIMESTAMP() WHERE MixingID = 7 
                                       AND MaterialId = 69 AND BatchNo = '{1}'", data[0], data[1]);
                    break;

                case ProcessM.MRCatAfter:
                    sql = string.Format(@"UPDATE ProcessDetail SET AfterInput = '{0}', AftInputTime = CURRENT_TIMESTAMP(), InputWeight = '{1}' WHERE MixingID = 7 
                                       AND MaterialId = 69 AND BatchNo = '{2}'", data[0], data[1], data[2]);
                    break;

            }

            return sql;
        }


    }

    public enum ChemicalCalculations
    {
        MR8B1_R, MR8B2_R, MR8A_R, MRREL_R, KEMISORB_R_UVB, BLUING_R_UVB, MRCAT_R_UVB, KEMISORB_R_UV, BLUING_R_UV, MRCAT_R_UV,
        MR8B1_TOL, MR8B2_TOL, MR8A_TOL, MRREL_TOL, KEMISORB_TOL_UVB, BLUING_TOL_UVB, MRCAT_TOL_UVB, KEMISORB_TOL_UV, BLUING_TOL_UV,
        MRCAT_TOL_UV
    }

    public enum ProcessM
    {
        Renew, RenewStart, RenewSubStart, MR8B1_Reset, MR8B1_Get, MR8B2_Reset, MR8B2_Get, MR8A_Reset, MR8A_Get, MRReleasing_Reset, MRReleasing_GetBefore, MRReleasing_GetAfter,
        CalcStirSpeed, Stirring, Stabilizing, Return, NextBatch,

        ForceClosing, NewMainBatch,

        RenewSub, RenewSubInsert, RenewStartSub, MixingTankReset, MixingTankGet, MixingTankStirring, MixingTankStirrSpeed, KemiDyeReset, KemiReset, DyeReset, KemiBefore, DyeBefore, KemiAfter, DyeAfter, KemiDyeBefore, KemiDyeAfter,
        MRCatReset, MRCatBefore, MRCatAfter, MRCatStirring, AddFreshValidation, AddFreshYes, AddExcessValidation, AddExcessYes, Deagassing, NextBatchSub, ReturnSub, End
    }

    
}
