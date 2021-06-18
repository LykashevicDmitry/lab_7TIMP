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

namespace Server
{
    abstract class Connector
    {
        public Action<Trans> TransGetter; //посылаем готовый Trans принятый от клиента

        protected MemoryMappedFile Mapfile;
        protected EventWaitHandle ServerSignal;
        protected EventWaitHandle ClientSignal;
        protected bool IsRunning;

        public Connector( string type )
        {
            Mapfile = MemoryMappedFile.CreateOrOpen( $"{type}.format", 32000, MemoryMappedFileAccess.ReadWrite );
            ServerSignal = new EventWaitHandle( false, EventResetMode.AutoReset, $"server.{type}" );
            ClientSignal = new EventWaitHandle( false, EventResetMode.AutoReset, $"client.{type}" );
            Task.Run( Listener );
        }
        protected void Listener()
        {
            IsRunning = true;
            while( true )
            {
                ServerSignal.WaitOne();
                if( IsRunning )
                    ServerSignalChanged();
                else break;
            }
        }
        public void Dispose() { IsRunning = false; ServerSignal.Set(); Mapfile.Dispose(); }

        // фактический код определен через перегрузку в каждом классе     
        public virtual void ServerSignalChanged() { }
        public virtual void SendToClientMap( Trans trans ) { }
    }
}
