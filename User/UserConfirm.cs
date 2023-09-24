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

namespace User
{
    public partial class UserConfirm : Form
    {
        Functions func = new Functions();
        public UserConfirm()
        {
            InitializeComponent();
            ControlTagging();
            

            btnLogin.Click += (o, e) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(txtUserName.Text) || string.IsNullOrEmpty(txtPassWord.Text))
                    {
                        MessageDisplay.Error("Please complete required fields");
                    }
                    else
                    {
                        if(!string.IsNullOrEmpty(CheckAdmin()))
                        {
                            this.DialogResult = DialogResult.OK;


                            Variables.returnID = Convert.ToInt32(txtUserName.Text);
                        }
                        else
                        {
                            MessageDisplay.Error("Incorrect user id or password");
                        }
                    }
                }
                catch(Exception er)
                {
                    MessageDisplay.Error(er.Message);
                }
            };
        }

        private void ControlTagging()
        {
            this.Controls.OfType<TextBox>().ToList().ForEach(n =>
            {
                n.KeyPress += (o, e) =>
                {
                    if (n is TextBox)
                    {
                        func.AcceptNumOnly(o, e);
                }
                };
            });
        }

        private string CheckAdmin()
        {
            string val = string.Empty;

            using (DAL dal = new DAL())
            {
                val = dal.GetSingleData(new ParameterData
                {
                    sqlQuery = Query.GetAdminAccount(Convert.ToInt32(txtUserName.Text))
                });

            }

            return val;
        }
    }
}
