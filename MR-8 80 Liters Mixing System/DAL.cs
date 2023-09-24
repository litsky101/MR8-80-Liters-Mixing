using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;
using System.ComponentModel;

namespace MR_8_80_Liters_Mixing_System
{
    public class DAL: IDisposable
    {
        private Component components = new Component();
        private bool disposed = false;

        public Dictionary<ChemicalCalculations, double> GetCalculations(ParameterData pd)
        {
            Dictionary<ChemicalCalculations, double> val = new Dictionary<ChemicalCalculations, double>();

            try
            {
                using (var con = new MySqlConnection(Variables.connString))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(pd.sqlQuery, con))
                    {
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                val.Add((ChemicalCalculations)Enum.Parse(typeof(ChemicalCalculations), dr.GetString(0)), dr.GetDouble(1));
                            }

                            return val;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public DataTable GetDataDT(ParameterData pd)
        {
            try
            {
                using (var con = new MySqlConnection(Variables.connString))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(pd.sqlQuery, con))
                    {
                        using (var dr = cmd.ExecuteReader())
                        {
                            var dt = new DataTable();
                            dt.Load(dr);

                            return dt;
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public string GetSingleData(ParameterData pd)
        {
            string val = string.Empty;

            try
            {
                using (var con = new MySqlConnection(Variables.connString))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(pd.sqlQuery, con))
                    {
                        var result = cmd.ExecuteScalar();

                        return result == null ? string.Empty : result.ToString();
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public string[] GetSingleCollection(ParameterData pd)
        {
            var data = new List<string>();

            try
            {
                using (var con = new MySqlConnection(Variables.connString))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(pd.sqlQuery, con))
                    {
                        using (var dr = cmd.ExecuteReader())
                        {

                            while(dr.Read())
                            {
                                data.Add(dr[0].ToString());
                            }

                            return data.ToArray();
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public void ExecuteQuery(ParameterData pd)
        {
            try
            {
                using (var con = new MySqlConnection(Variables.connString))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(pd.sqlQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        ~DAL()
        {
            Dispose(false);
        }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                components.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    enum Settings
    {
        MIXING_DB
    }
}
