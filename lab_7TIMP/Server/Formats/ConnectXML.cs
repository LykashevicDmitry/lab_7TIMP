using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Server
{
    class ConnectXML : Connector
    {
        public ConnectXML( string ext ) : base(  ext ) { }
        public override void ServerSignalChanged()
        {
            try
            {
                MemoryMappedViewAccessor view = Mapfile.CreateViewAccessor();

                int length;
                // вначале первые 4 байта содержат размер остального массива
                view.Read<int>( 0, out length );
                // выделили память для массива
                byte[] array = new byte[length];
                // прочитали в массив, НАЧИНАЯ (position) с 4 байта весь массив
                view.ReadArray( sizeof( int ), array, 0, length );

                MemoryStream mem = new MemoryStream( array );
                XmlSerializer xml = new XmlSerializer( typeof( Trans ) );
                Trans trans = xml.Deserialize( mem ) as Trans;
                TransGetter?.Invoke( trans ); // передали готовый объект подписчику
            }
            catch( Exception ex ) { }
        }

        public override void SendToClientMap( Trans trans )
        {
            try
            {
                MemoryStream mem = new MemoryStream();

                XmlSerializer xml = new XmlSerializer( typeof( Trans ) );
                xml.Serialize( mem, trans );

                MemoryMappedViewAccessor view = Mapfile.CreateViewAccessor();
                // получили массив байтов
                byte[] array = mem.ToArray();
                // вначале записываем длину массива
                view.Write( 0, array.Length );
                // теперь сам массив, ПОСЛЕ длины (смещение position - 4 байта - размер int)
                view.WriteArray<byte>( sizeof( int ), array, 0, array.Length );
                view.Flush();

                ClientSignal.Set();
            }
            catch( Exception ex ) { }
        }
    }
}
