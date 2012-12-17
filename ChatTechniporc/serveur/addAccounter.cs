using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlServerCe;

namespace serveur
{
    public partial class addAccounter : Form
    {
        SqlCeConnection connection;
        string string_co = "Data Source=accounts.sdf;Persist Security Info=False;";

        public addAccounter()
        {
            InitializeComponent();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            connection = new SqlCeConnection(string_co);
            connection.Open();
            SqlCeCommand cmd = new SqlCeCommand("INSERT INTO accounts (id,username,password) VALUES (1,'"+txbUsername.Text+"', '"+txbPassword.Text+"')", connection);
            cmd.ExecuteNonQuery();
            connection.Close();

        }
    }
}
