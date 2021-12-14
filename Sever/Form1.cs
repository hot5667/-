using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using Xamarin.Forms;

namespace Sever
{
    public partial class Form1 : Form
    {
        TcpClient mTcpClient;
        Byte[] mRx;

        //서버에 연결되어 있는지 확인
        bool bConnected = false;
        private bool Connected { get => mTcpClient == null ? false : mTcpClient.Connected && bConnected; }

        //접속시 사용한 IP PORT
        int iPort;
        IPAddress iPAddress;

        //재접속 타이머
        System.Timers.Timer timer1 = new System.Timers.Timer();

        public Form1()
        {
            InitializeComponent();

            timer1.Interval = 1000;
            timer1.Elapsed += new System.Timers.ElapsedEventHandler(Timer1_Elasped);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (mTcpClient != null && Connected) return;

            IPAddress ipa;
            int nPort;

            try
            {
                if (string.IsNullOrEmpty(tbServerIP.Text) || string.IsNullOrEmpty(tbServerIP.Text))
                    return;

                if(!IPAddress.TryParse(tbServerIP.Text, out ipa))
                {
                    MessageBox.Show("Please sypply an IP Address");
                    return;
                }

                if(!int.TryParse(tbSeverPort.Text, out nPort))
                {
                    nPort = 23000;
                }

                mTcpClient = new TcpClient();
                mTcpClient.BeginConnect(ipa, nPort, onCompleteConnect, mTcpClient);

                iPAddress = ipa;
                iPort = nPort;
                bConnecting = true;
                timer1.Start();
            }
            catch(Exception exc)
            {
                bConnected = false;
                MessageBox.Show(exc.Message);
            }
        }
        bool bFirstConnect = false;
        bool bConnecting = false;
        void onCompleteConnect(IAsyncResult iar)
        {
            TcpClient tcpc;

            try
            {
                tcpc = (TcpClient)iar.AsyncState;
                tcpc.EndConnect(iar);
                mRx = new byte[512];
                tcpc.GetStream().BeginRead(mRx, 0, mRx.Length, onCompleteReadFromServerStream, tcpc);
                bConnected = true; //연결 성공
            }
            catch(Exception exc)
            {
                MessageBox.Show(exc.Message);
                bConnected = false; //연결실패
            }
            bConnecting = false;
        }
        void onCompleteReadFromServerStream(IAsyncResult iar)
        {
            TcpClient tcpc;
            int nCountBytesReceivedFromServer;
            string strReceived;

            try
            {
                tcpc = (TcpClient)iar.AsyncState;
                nCountBytesReceivedFromServer = tcpc.GetStream().EndRead(iar);
                if(nCountBytesReceivedFromServer == 0)
                {
                    MessageBox.Show("Connection broken");
                    return;
                }
                strReceived = Encoding.ASCII.GetString(mRx, 0, nCountBytesReceivedFromServer);
                string s1 = Encoding.ASCII.GetString(mRx, 0, nCountBytesReceivedFromServer - 1);
                if (mRx[nCountBytesReceivedFromServer - 1] != '\n') 
                    return;
                printLine(strReceived);

                mRx = new byte[512];
                tcpc.GetStream().BeginRead(mRx, 0, mRx.Length, onCompleteReadFromServerStream, tcpc);

                bConnected = true; //연결선공
            }
            catch(Exception exc)
            {
                bConnected = false; //연결실패
                MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void printLine(string _strPrint)
        {
            tbConsole.Invoke(new Action<string>(doInvoke), _strPrint);
        }
        public void doInvoke(string _strPrint)
        {
            tbConsole.Text = _strPrint + Environment.NewLine + tbConsole.Text;
        }
        byte[] tx;
        private void Timer1_Elasped(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            bConnected = mTcpClient.Connected;
            if(!mTcpClient.Connected && !bConnecting)
            {
                bConnecting = true;
                mTcpClient.Close();
                mTcpClient = new TcpClient();
                mTcpClient.BeginConnect(iPAddress, iPort, onCompleteConnect, mTcpClient);
            }
            timer1.Enabled = true;
        }

        private void tbSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPayload.Text))
                return;
            try
            {
                if (!Connected) 
                    return;
                tx = Encoding.ASCII.GetBytes(tbPayload.Text + "\r\n");
                if(mTcpClient != null)
                {
                    if (mTcpClient.Client.Connected)
                    {
                        mTcpClient.GetStream().BeginWrite(tx, 0, tx.Length, onCompleteWriteToSever, mTcpClient);
                    }
                }
            }
            catch(Exception exc)
            {
                bConnected = false;
                MessageBox.Show(exc.Message);
            }
        }
        void onCompleteWriteToSever(IAsyncResult iar)
        {
            TcpClient tcpc;

            try
            {
                tcpc = (TcpClient)iar.AsyncState;
                tcpc.GetStream().EndWrite(iar);
            }
            catch(Exception exc)
            {
                //bConnected = false;
                MessageBox.Show(exc.Message);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.Activate(); 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if(this .WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = false;
            }
        }
    }
}
