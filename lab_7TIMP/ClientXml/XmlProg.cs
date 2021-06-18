using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Common;


namespace ClientXml
{
    class XmlProg
    {
        static Random rnd = new Random();

        // системный именованый фалй в памяти. Здесь имя "xml.format"
        static MemoryMappedFile Mapfile;

        // системный именованый канал сигналов. здесь имя server.xml
        // посылает системный сигнал Windows, блокировщику ServerSignal.WaitOne()
        static EventWaitHandle ServerSignal;

        // именованый канал сигналов для этого клиента. Он ждет сигнал в методе ClientSignal.WaitOne
        // такой сигнал: ClientSignal.Set() посылает сервер когда записал данные в MapFile
        static EventWaitHandle ClientSignal;        

        static void Main( string[] args )
        {
            Mapfile = MemoryMappedFile.CreateOrOpen( $"xml.format", 32000, MemoryMappedFileAccess.ReadWrite );
            ServerSignal = new EventWaitHandle( false, EventResetMode.AutoReset, $"server.xml" );
            ClientSignal = new EventWaitHandle( false, EventResetMode.AutoReset, $"client.xml" );

            SendToServer( "XML Client подключен" );
            ClientSignal.Reset();

            while( true )
            {
                ClientSignal.WaitOne();
                Console.WriteLine("ClientSignal Set");
                if( !ReadMap() )
                    break;
            }
            SendToServer( "XMLClient отключен" );
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
                XmlSerializer xml = new XmlSerializer( typeof( Trans ) );
                Trans trans = xml.Deserialize( mem ) as Trans;
                return TransFromServer( trans );
            }
            catch(Exception ex) { }
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
            Console.WriteLine( $"Начата обработка XML сообщения: перевод букв в верхний регистр" );
            SendToServer( "Начата обработка..." );

            string data = "Перевод в верхний регистр\r\n\r\n";
            data += serverTrans.Data.ToUpper();

            Thread.Sleep( rnd.Next( 1500, 2200 ) );//задержка для имитации длительной операции

            Console.WriteLine( $"Обработка XML сообщения завершена: отправка на Сервер." );

            SendToServer( data );
        }

        static void SendToServer( string data )
        {
            Trans trans = new Trans( SignalType.data, data );
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
                ServerSignal.Set();
            }
            catch( Exception ex ) 
            {
                Console.WriteLine( ex.Message );
            }
        }
    }
}
