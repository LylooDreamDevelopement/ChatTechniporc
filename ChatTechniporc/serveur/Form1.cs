using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Data.SqlServerCe;

namespace serveur
{
    public partial class Form1 : Form
    {
        UdpClient serveur;
        Thread listener;
        SqlCeConnection connection;
        string string_co = "Data Source=accounts.sdf;Persist Security Info=False;";
        private bool conti = true;
        const int PORT = 5053;

        public Form1()
        {
            InitializeComponent();
        }

        private class CommunicationData
        {
            public IPEndPoint Client { get; private set; }

            public byte[] Data { get; private set; }

            public CommunicationData(IPEndPoint client, byte[] data)
            {
                Client = client;
                Data = data;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            connection = new SqlCeConnection(string_co);
            connection.Open();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == "Start")
            {
                serveur = new UdpClient();
                serveur.EnableBroadcast = true;
                serveur.Connect(new IPEndPoint(IPAddress.Broadcast, PORT));
                listener = new Thread(new ThreadStart(Listen));
                listener.Start();
                lsbLog.Items.Add("Server up on port : " + PORT.ToString());
                toolStatus.Text = "On";
                btnStart.Text = "Stop";
                conti = true;
            }
            else
            {
                stop(false);
                serveur = null;
                btnStart.Text = "Start";
                toolStatus.Text = "Off";
                lsbLog.Items.Add("Server shutting down...");
            }
        }

        private void Listen()
        {
            //Création d'un Socket qui servira de serveur de manière sécurisée.
            UdpClient serveur2 = null;
            bool erreur = false;
            int attempts = 0;

            //J'essaie 3 fois car je veux éviter un plantage au serveur juste pour une question de millisecondes.
            do
            {
                try
                {
                    serveur2 = new UdpClient(1523);
                }
                catch
                {
                    erreur = true;
                    attempts++;
                    Thread.Sleep(400);
                }
            } while (erreur && attempts < 4);

            //Si c'est vraiment impossible de se lier, on en informe le serveur et on quitte le thread.
            if (serveur == null)
            {
                this.Invoke(new Action<string>(log), "Il est impossible de se lier au port 1523. Vérifiez votre configuration réseau.");
                this.Invoke(new Action<bool>(stop), false);
                return;
            }

            serveur2.Client.ReceiveTimeout = 1000;

            //Boucle infinie d'écoute du réseau.
            while (conti)
            {
                try
                {
                    IPEndPoint ip = null;
                    byte[] data = serveur2.Receive(ref ip);

                    //Préparation des données à l'aide de la classe interne.
                    CommunicationData cd = new CommunicationData(ip, data);
                    //On lance un nouveau thread avec les données en paramètre.
                    new Thread(new ParameterizedThreadStart(TraiterMessage)).Start(cd);
                }
                catch
                {
                }
            }

            serveur2.Close();
        }

        private void log(string message)
        {
            lsbLog.Items.Add(DateTime.Now.ToUniversalTime() + " => " + message);
        }

        private void stop(bool attendre)
        {
            log("Arrêt du serveur...");
            conti = false;
            //On attend le thread d'écoute seulement si on le demande et si ce dernier était réellement en train de fonctionner.
            if (attendre && listener != null && listener.ThreadState == ThreadState.Running)
                listener.Join();
        }

        private void TraiterMessage(object messageArgs)
        {
            try
            {
                //On récupère les données entrantes et on les formatte comme il faut.
                CommunicationData data = messageArgs as CommunicationData;
                string bad = Encoding.Default.GetString(data.Data);
                string message;
                if (bad.StartsWith("/login"))
                {
                    bad = bad.Remove(0, 6);
                    bad = bad.Trim();
                    string[] id = bad.Split(';');
                    id[0] = id[0].Substring(id[0].LastIndexOf(' '), (id[0].Length - id[0].LastIndexOf(' '))).Trim();
                    id[1] = id[1].Substring(id[1].LastIndexOf(' '), (id[1].Length - id[1].LastIndexOf(' '))).Trim();
                    SqlCeCommand cmd = new SqlCeCommand("SELECT COUNT(*) AS Count FROM accounts WHERE username LIKE '"+id[0]+"' AND password LIKE '"+id[1]+"'",connection);

                    SqlCeDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader["Count"].ToString() == "1")
                        {
                            message = "OK "+id[0];
                        }else
                        {
                            message = "NOK "+id[0];
                        }
                            byte[] donnees = Encoding.Default.GetBytes(message);
                            serveur.Send(donnees, donnees.Length);
                            this.Invoke(new Action<string>(log), message);
                        
                    }
                }
                else
                {
                    message = string.Format("{0}:{1} > {2}", data.Client.Address.ToString(), data.Client.Port, Encoding.Default.GetString(data.Data));
                    //On renvoie le message formatté à travers le réseau.
                    byte[] donnees = Encoding.Default.GetBytes(message);
                    serveur.Send(donnees, donnees.Length);
                    this.Invoke(new Action<string>(log), message);
                }
                
            }
            catch { }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addAccounter frm = new addAccounter();
            frm.ShowDialog();
        }
    }
}
