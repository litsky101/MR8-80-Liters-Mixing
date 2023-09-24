using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Drawing;

namespace MR_8_80_Liters_Mixing_System
{
    public enum ConnectionStatus
    {
        Connected,
        NotConnected,
        Error
    }

    public class Connections : IDisposable
    {
        private Component components = new Component();
        private bool disposed = false;

        public ConnectionStatus CheckSocketConnection(int port, string ip)
        {
            ConnectionStatus result;

            TcpClient clientSocket = new TcpClient();

            try
            {
                clientSocket.Connect(ip, port);
                clientSocket.ReceiveTimeout = 4000;

                NetworkStream serverStream = clientSocket.GetStream();

                byte[] outStream = System.Text.Encoding.ASCII.GetBytes("SI" + Convert.ToChar(13) + Convert.ToChar(10));

                serverStream.Write(outStream, 0, outStream.Length);

                Thread.Sleep(1000);

                if (clientSocket.Available > 0)
                {
                    result = ConnectionStatus.Connected;
                }
                else
                {
                    result = ConnectionStatus.NotConnected;
                }
            }
            catch (SocketException)
            {
                result = ConnectionStatus.Error;    //Error
            }
            finally
            {
                clientSocket.Close();
            }

            return result;
        }

        public ConnectionStatus CheckSocketConnectionMain(int port, string ip)
        {
            ConnectionStatus result;

            string command = string.Empty;

            TcpClient clientSocket = new TcpClient();

            try
            {
                clientSocket.Connect(ip, port);
                clientSocket.ReceiveTimeout = 3000;

                NetworkStream serverStream = clientSocket.GetStream();

                byte[] outStream  = System.Text.Encoding.ASCII.GetBytes("@0022" + Convert.ToChar(13) + Convert.ToChar(10));

                serverStream.Write(outStream, 0, outStream.Length);

                Thread.Sleep(1000);

                if (clientSocket.Available > 0)
                {
                    result = ConnectionStatus.Connected;

                }
                else
                {
                    result = ConnectionStatus.NotConnected;
                }
            }
            catch (SocketException)
            {
                result = ConnectionStatus.Error;    //Error
            }
            finally
            {
                clientSocket.Close();
            }

            return result;
        }

        public string ResetWeight(int Port, string IP) // Reset Weighing scale
        {
            Variables.Machine.wait = true;

            string Result = string.Empty;

            TcpClient clientSocket = new TcpClient();


            NetworkStream serverStream = null;

            try
            {
                clientSocket.Connect(IP, Port);

                serverStream = clientSocket.GetStream();

                byte[] outStream = System.Text.Encoding.ASCII.GetBytes("R" + Convert.ToChar(13) + Convert.ToChar(10));

                serverStream.Write(outStream, 0, outStream.Length);

                Thread.Sleep(1000);

                serverStream.Flush();

                Result = "0.00";
            }
            catch (SocketException er)
            {
                if (er.ErrorCode == 10061)
                {
                    throw new System.ArgumentException("Unable to reset weighing scale.\nPlease try again.");
                }
                else
                {
                    throw new System.ArgumentException("Unable to reset weighing scale.\nPlease try again.");
                }
            }
            finally
            {
                if (serverStream != null)
                {
                    serverStream.Close();
                }
                clientSocket.Close();
                Variables.Machine.wait = false;
            }

            return Result;
        }

        public string GetWeight(int Port, string IP) //Get values from weighing scale
        {
            Variables.Machine.wait = true;

            string Result = string.Empty;

            TcpClient clientSocket = new TcpClient();
            clientSocket.ReceiveTimeout = 3000;
            try
            {
                clientSocket.Connect(IP, Port);

                NetworkStream serverStream = clientSocket.GetStream();

                byte[] outStream = System.Text.Encoding.ASCII.GetBytes("S" + Convert.ToChar(13) + Convert.ToChar(10));

                serverStream.Write(outStream, 0, outStream.Length);

                serverStream.Flush();

                Thread.Sleep(700);

                if (clientSocket.Available > 0)
                {
                    //16 - 64
                    byte[] inStream = new byte[64 * 1024];
                    serverStream.Read(inStream, 0, (int)clientSocket.ReceiveBufferSize);
                    serverStream.Close();
                    clientSocket.Close();
                    string rep = Regex.Replace(System.Text.Encoding.ASCII.GetString(inStream).ToString(), @"\t|\n|\r|\0", "");
                    string str = rep.Trim();

                    if (str.Length > 0)
                    {
                        string sign = rep.Substring(3, 1);
                        string newstring = Regex.Replace(str, "[^.0-9]", "");
                        double val;
                        bool isDouble = Double.TryParse(newstring, out val);

                        Result = sign.Equals("+") ? val.ToString() : sign + val.ToString();
                    }
                    else
                    {
                        throw new System.ArgumentException("Weighing scale is not yet ready\nPlease try again.");
                    }
                }
                else
                {
                    throw new System.ArgumentException("Weighing scale is not yet ready\nPlease try again.");
                }

                serverStream.Close();
                serverStream.Dispose();
                GC.SuppressFinalize(serverStream);

            }
            catch (SocketException er)
            {
                if (er.ErrorCode == 10061)
                {
                    throw new System.ArgumentException("Unable to get weight from weighing scale.\nPlease try again.");
                }
                else
                {
                    throw new System.ArgumentException("Unable to get weight from weighing scale.\nPlease try again.");
                }
            }
            finally
            {
                clientSocket.Close();
                Variables.Machine.wait = false;
            }

            return Result;
        }

        public string ResetLoadCell(int Port, string IP)
        {
            Variables.Machine.wait = true;

            string Result = string.Empty;

            TcpClient clientSocket = new TcpClient();

            NetworkStream serverStream = null;

            clientSocket.ReceiveTimeout = 3000;

            try
            {
                clientSocket.Connect(IP, Port);

                serverStream = clientSocket.GetStream();

                byte[] outStream = System.Text.Encoding.ASCII.GetBytes("@0054" + Convert.ToChar(13) + Convert.ToChar(10));

                serverStream.Write(outStream, 0, outStream.Length);

                Thread.Sleep(1000);

                serverStream.Flush();

                Result = "0.00";
            }
            catch (SocketException er)
            {
                if (er.ErrorCode == 10061)
                {
                    throw new System.ArgumentException("Unable to reset load cell.\nPlease try again.");
                }
                else
                {
                    throw new System.ArgumentException("Unable to reset load cell.\nPlease try again.");
                }
            }
            finally
            {
                if (serverStream != null)
                {
                    serverStream.Close();
                }
                clientSocket.Close();
                Variables.Machine.wait = false;
            }

            return Result;
        }

        public string GetWeightLoadCell(WeightType w, int Port, string IP)
        {
            Variables.Machine.wait = true;

            string Result = string.Empty;
            string command = string.Empty;

            TcpClient clientSocket = new TcpClient();

            command = w == WeightType.NET ? "@0022" : "@0021";

            clientSocket.ReceiveTimeout = 3000;

            try
            {
                clientSocket.Connect(IP, Port);

                NetworkStream serverStream = clientSocket.GetStream();

                byte[] outStream = System.Text.Encoding.ASCII.GetBytes(command + Convert.ToChar(13) + Convert.ToChar(10));

                serverStream.Write(outStream, 0, outStream.Length);

                serverStream.Flush();

                Thread.Sleep(900);

                if (clientSocket.Available > 0)
                {
                    //16 - 64
                    byte[] inStream = new byte[64 * 1024];
                    serverStream.Read(inStream, 0, (int)clientSocket.ReceiveBufferSize);
                    serverStream.Close();
                    clientSocket.Close();
                    string rep = Regex.Replace(System.Text.Encoding.ASCII.GetString(inStream).ToString(), @"\t|\n|\r|\0", "");
                    string str = rep.Trim();

                    if (command.Equals("@0022"))
                    {
                        str = str.Replace("@0022", "");
                    }
                    else if(command.Equals("@0021"))
                    {
                        str = str.Replace("@0021", "");
                    }
                    else
                    {
                        str = string.Empty;
                    }

                    if (!str.Contains("@00ER"))
                    {
                        if (str.Length > 0)
                        {
                            string sign = rep.Substring(5, 1);
                            string newstring = Regex.Replace(str, "[^.0-9]", "");

                            //double val;
                            //bool isDouble = Double.TryParse(newstring, out val);
                            //Result = sign.Equals("+") ? val.ToString() : sign + val.ToString();
                            double val;
                            if (Double.TryParse(newstring, out val))
                            {
                                Result = sign.Equals("+") ? val.ToString("N2") : sign + val.ToString("N2");
                            }
                            else
                            {
                                Result = sign.Equals("+") ? val.ToString() : sign + val.ToString();
                            }
                        }
                        else
                        {
                            throw new System.ArgumentException("Load cell not ready!\nPlease try again.");
                        }
                    }
                    else
                    {
                        throw new System.ArgumentException("Load cell not ready!\nPlease try again.");
                    }

                    //if (str.Length > 0)
                    //{
                    //    string sign = rep.Substring(5, 1);
                    //    string newstring = Regex.Replace(str, "[^.0-9]", "");

                    //    //double val;
                    //    //bool isDouble = Double.TryParse(newstring, out val);
                    //    //Result = sign.Equals("+") ? val.ToString() : sign + val.ToString();
                    //    double val;
                    //    if (Double.TryParse(newstring, out val))
                    //    {
                    //        Result = sign.Equals("+") ? val.ToString("N2") : sign + val.ToString("N2");
                    //    }
                    //    else
                    //    {
                    //        Result = sign.Equals("+") ? val.ToString() : sign + val.ToString();
                    //    }
                    //}
                    //else
                    //{
                    //    throw new System.ArgumentException("Load cell not ready!\nPlease try again.");
                    //}
                }
                else
                {
                    throw new System.ArgumentException("Load cell not ready!\nPlease try again.");
                }

                serverStream.Close();
                serverStream.Dispose();
                GC.SuppressFinalize(serverStream);

            }
            catch (SocketException er)
            {
                if (er.ErrorCode == 10061)
                {
                    throw new System.ArgumentException("Unable to connect load cell.\n Please try again.");
                }
                else
                {
                    throw new System.ArgumentException("Unable to connect load cell.\n Please try again.");
                }
            }
            finally
            {
                clientSocket.Close();
            }

            return Result;
        }

        public bool SendLoadCellValue(int Port, string IP, string weight)
        {
            Variables.Machine.wait = true;

            bool result = false;

            TcpClient clientSocket = new TcpClient();


            NetworkStream serverStream = null;

            try
            {
                clientSocket.Connect(IP, Port);

                serverStream = clientSocket.GetStream();

                byte[] outStream = System.Text.Encoding.ASCII.GetBytes("US1W12" + weight + Convert.ToChar(13) + Convert.ToChar(10));

                serverStream.Write(outStream, 0, outStream.Length);

                Thread.Sleep(1000);

                serverStream.Flush();

                result = true;
            }
            catch (SocketException er)
            {
                if (er.ErrorCode == 10061)
                {
                    throw new System.ArgumentException("Unable to send standard weight to load cell.\nPlease try again");
                }
                else
                {
                    throw new System.ArgumentException("Unable to send standard weight to load cell.\nPlease try again");
                }
            }
            finally
            {
                if (serverStream != null)
                {
                    serverStream.Close();
                }
                clientSocket.Close();
                Variables.Machine.wait = false;
            }

            return result;
        }

        ~Connections()
        {
            Dispose(false);
        }

        #region  IDisposable Members
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

    public class WeighingScale
    {
        static Connections con;

        public Color GetColor(ConnectionStatus con)
        {
            Color clr;

            if(con.Equals(ConnectionStatus.Connected))
            {
                clr = Color.Green;
            }
            else if(con.Equals(ConnectionStatus.NotConnected))
            {
                clr = Color.Red;
            }
            else
            {
                clr = Color.Black;
            }

            return clr;
        }

        public Func<WeighingScales, string> WeightReset = (x) =>
          {
              con = new Connections();
              string val = string.Empty;

              try
              {
                  switch (x)
                  {
                      case WeighingScales.MR_RELEASING:
                          val = con.ResetWeight(Convert.ToInt32(Variables.portMRReleasing), Variables.ip);
                          break;

                      case WeighingScales.KEMISORB:
                          val = con.ResetWeight(Convert.ToInt32(Variables.portKemisorb), Variables.ip);
                          break;

                      case WeighingScales.DYESTUFF:
                          val = con.ResetWeight(Convert.ToInt32(Variables.portDyestuff), Variables.ip);
                          break;

                      case WeighingScales.MRCAT:
                          val = con.ResetWeight(Convert.ToInt32(Variables.portMRCat), Variables.ip);
                          break;
                  }
              }
              catch
              {
                  throw;
              }

              return val;
          };

        public Func<WeighingScales, string> GetWeight = (x) =>
          {
              con = new Connections();
              string val = string.Empty;

              try
              {
                  switch (x)
                  {
                      case WeighingScales.MR_RELEASING:
                          val = con.GetWeight(Convert.ToInt32(Variables.portMRReleasing), Variables.ip);
                          break;

                      case WeighingScales.KEMISORB:
                          val = con.GetWeight(Convert.ToInt32(Variables.portKemisorb), Variables.ip);
                          break;

                      case WeighingScales.DYESTUFF:
                          val = con.GetWeight(Convert.ToInt32(Variables.portDyestuff), Variables.ip);
                          break;

                      case WeighingScales.MRCAT:
                          val = con.GetWeight(Convert.ToInt32(Variables.portMRCat), Variables.ip);
                          break;
                  }
              }
              catch
              {
                  throw;
              }

              return val;
          };

        public Func<WeighingScales, WeightType, string> GetWeightLoadCell = (x, y) =>
          {
              con = new Connections();
              string val = string.Empty;

              try
              {
                  switch (x)
                  {
                      case WeighingScales.MR8B1:
                          val = con.GetWeightLoadCell(y, Convert.ToInt32(Variables.portMR8B1), Variables.ip);
                          break;
                      case WeighingScales.MR8B2:
                          val = con.GetWeightLoadCell(y, Convert.ToInt32(Variables.portMR8B2), Variables.ip);
                          break;
                      case WeighingScales.MR8A:
                          val = con.GetWeightLoadCell(y, Convert.ToInt32(Variables.portMR8A), Variables.ip);
                          break;
                      case WeighingScales.TANK:
                          val = con.GetWeightLoadCell(y, Convert.ToInt32(Variables.portTANK), Variables.ip);
                          break;
                  }
              }
              catch
              {
                  throw;
              }

              return val;
          };

        public Func<WeighingScales, string> ResetWeightLoadCell = (x) =>
        {
            string stat = string.Empty;

            try
            {
                switch (x)
                {
                    case WeighingScales.MR8B1:
                        stat = con.ResetLoadCell(Convert.ToInt32(Variables.portMR8B1), Variables.ip);
                        break;
                    case WeighingScales.MR8B2:
                        stat = con.ResetLoadCell(Convert.ToInt32(Variables.portMR8B2), Variables.ip);
                        break;
                    case WeighingScales.MR8A:
                        stat = con.ResetLoadCell(Convert.ToInt32(Variables.portMR8A), Variables.ip);
                        break;
                    case WeighingScales.TANK:
                        stat = con.ResetLoadCell(Convert.ToInt32(Variables.portTANK), Variables.ip);
                        break;
                }
            }
            catch
            {
                throw;
            }

            return stat;
        };

        public Func<WeighingScales, string, bool> SendStdWeight = (scale, weight) =>
        {
            bool stat =  false;

            try
            {
                switch (scale)
                {
                    case WeighingScales.MR8B1:
                        stat = con.SendLoadCellValue(Convert.ToInt32(Variables.portMR8B1), Variables.ip, weight);
                        break;
                    case WeighingScales.MR8B2:
                        stat = con.SendLoadCellValue(Convert.ToInt32(Variables.portMR8B2), Variables.ip, weight);
                        break;
                    case WeighingScales.MR8A:
                        stat = con.SendLoadCellValue(Convert.ToInt32(Variables.portMR8A), Variables.ip, weight);
                        break;
                    case WeighingScales.TANK:
                        stat = con.SendLoadCellValue(Convert.ToInt32(Variables.portTANK), Variables.ip, weight);
                        break;
                }
            }
            catch
            {
                throw;
            }

            return stat;
        };

    }

    public enum WeightType
    {
        NET,
        GROSS
    }

}
