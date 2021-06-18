using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using Common;

namespace Server
{
    public partial class Form1 : Form
    {
        List<Connector> clientList;
        public Form1()
        {
            InitializeComponent();
            KeyDown += ( s, e ) => { if( e.KeyCode == Keys.Escape ) Close(); };
            KeyPreview = true;

            string dataPath = Path.Combine( Environment.CurrentDirectory, "Data" );

            ConnectXML xmlConnect = new ConnectXML( "xml" );
            xmlConnect.TransGetter += XmlGetter;

            ConnectBIN binConnect = new ConnectBIN( "bin" );
            binConnect.TransGetter += BinGetter;

            ConnectJSON jsonConnect = new ConnectJSON( "json" );
            jsonConnect.TransGetter += JsonGetter;

            clientList = new List<Connector>() { xmlConnect, binConnect, jsonConnect };

            disconnectClick( null, null );
        }

        private void JsonGetter( Trans trans )
        {
            BeginInvoke( new Action( () =>
            {
                jsonTextBox.Text = $"JSON клиент в {trans.Time.ToLongTimeString()}\n";
                jsonTextBox.Text += trans.Data;
            } ) );
        }

        void BinGetter( Trans trans )
        {
            BeginInvoke( new Action( () =>
            {
                binaryTextBox.Text = $"BIN клиент в {trans.Time.ToLongTimeString()}\n";
                binaryTextBox.Text += trans.Data;
            } ) );
        }
        void XmlGetter( Trans trans )
        {
            BeginInvoke( new Action( () =>
            {
                xmlTextBox.Text = $"XML клиент в {trans.Time.ToLongTimeString()}\n";
                xmlTextBox.Text += trans.Data;
            } ) );
        }

        void SendAll( SignalType signal, string data )
        {
            Trans trans = new Trans( signal ) { Data = data };

            foreach( Connector client in clientList )
                client.SendToClientMap( trans );
        }
        private void sendClick( object sender, EventArgs e )
        {
            string data = sourceTextBox.Text;
            SendAll( SignalType.data, data );
        }

        private void connectClick( object sender, EventArgs e )
        {
            Process.Start( "clientxml.exe" );
            Process.Start( "clientbin.exe" );
            Process.Start( "clientjson.exe" );

            //задержка пока запустятся и перевод формы на верхний план
            Thread.Sleep( 300 );
            TopMost = true;
            Application.DoEvents();
            TopMost = false;
            Focus();

            disconnectButton.Enabled = sendButton.Enabled = true;
            connectButton.Enabled = false;
        }

        private void disconnectClick( object sender, EventArgs e )
        {
            SendAll( SignalType.close, null );

            disconnectButton.Enabled = sendButton.Enabled = false;
            connectButton.Enabled = true;
        }

        private void Form1_FormClosing( object sender, FormClosingEventArgs e )
        {
            disconnectClick( null, null );
            clientList.ForEach( client => client.Dispose() );
        }
    }
}
