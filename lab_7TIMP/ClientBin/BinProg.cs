using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Common;


namespace ClientBin
{
    class BinProg
    {
        static Random rnd = new Random();

        static MemoryMappedFile Mapfile;
        static EventWaitHandle ServerSignal;
        static EventWaitHandle ClientSignal;

        static void Main( string[] args )
        {
            Mapfile = MemoryMappedFile.CreateOrOpen( $"bin.format", 32000, MemoryMappedFileAccess.ReadWrite );
            ServerSignal = new EventWaitHandle( false, EventResetMode.AutoReset, $"server.bin" );
            ClientSignal = new EventWaitHandle( false, EventResetMode.AutoReset, $"client.bin" );

            SendToServer( "BINARY Client подключен" );
            ClientSignal.Reset();

            while( true )
            {
                ClientSignal.WaitOne();
                Console.WriteLine( "ClientSignal Set" );
                if( !ReadMap() )
                    break;
            }
            SendToServer( "BINARY Client отключен" );
        }

        static bool ReadMap()
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
                IFormatter binary = new BinaryFormatter();
                Trans trans = binary.Deserialize( mem ) as Trans;

                return TransFromServer( trans );
            }
            catch( Exception ex ) { }
            return false;
        }

        static bool TransFromServer( Trans trans )
        {
            Console.WriteLine( $"Получено сообщение '{trans.Signal}' в {trans.Time}" );
            switch( trans.Signal )
            {
                case SignalType.close:
                    return false;
                case SignalType.data:
                    DataWork( trans );
                    break;
            }
            return true;
        }


        static void DataWork( Trans serverTrans )
        {
            Console.WriteLine( $"Начата обработка Binary сообщения: реверс слов" );
            SendToServer( "Начата обработка..." );

            string data = "РЕВЕРС СЛОВ\r\n\r\n";
            string[] words = serverTrans.Data.Split( new char[] { ' ', '.', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
            List<string> revers = new List<string>();
            foreach( string word in words )
                revers.Add( new string( word.Reverse().ToArray() ) );

            data += string.Join( "\n", revers );

            Thread.Sleep( rnd.Next( 1500, 2200 ) );//задержка для имитации длительной операции

            Console.WriteLine( $"Обработка Binary сообщения завершена: отправка на Сервер." );

            SendToServer( data );
        }

        static void SendToServer( string data )
        {
            Trans trans = new Trans( SignalType.data, data );
            try
            {
                MemoryStream mem = new MemoryStream();

                IFormatter binary = new BinaryFormatter();
                binary.Serialize( mem, trans );

                MemoryMappedViewAccessor view = Mapfile.CreateViewAccessor();
                // получили массив байтов
                byte[] array = mem.ToArray();
                // вначале записываем длину массива
                view.Write( 0, array.Length );
                // теперь сам массив, ПОСЛЕ длины (смещение position - 4 байта - размер int)
                view.WriteArray<byte>( sizeof( int ), array, 0, array.Length );
                view.Flush();
                ServerSignal.Set();
            }
            catch( Exception ex )
            {
                Console.WriteLine( ex.Message );
            }
        }
    }
}
