using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    //-----------------JSON-------------------------
    class ConnectJSON : Connector
    {
        public ConnectJSON( string ext ) : base( ext ) { }

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

                JsonSerializerSettings jset = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
                string jsontext = Encoding.UTF8.GetString( array );
                Trans trans = JsonConvert.DeserializeObject<Trans>( jsontext, jset );

                TransGetter?.Invoke( trans ); // передали готовый объект подписчику
            }
            catch( Exception ex ) { }
        }

        public override void SendToClientMap( Trans trans )
        {
            try
            {
                JsonSerializerSettings jset = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
                string jsontext = JsonConvert.SerializeObject( trans, Formatting.Indented, jset );
                byte[] array = Encoding.UTF8.GetBytes( jsontext );

                MemoryMappedViewAccessor view = Mapfile.CreateViewAccessor();

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
