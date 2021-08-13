// both server and client need a connection state object.
// -> server needs it to keep track of multiple connections
// -> client needs it to safely create a new connection state on every new
//    connect in order to avoid data races where a dieing thread might still
//    modify the current state. can't happen if we create a new state each time!
//    (fixes all the flaky tests)
//
// ... besides, it also allows us to share code!
using System.IO;
using System.Net.Sockets;
using System.Threading;
using WatsonTcp;

namespace Telepathy
{
    public class ConnectionState
    {
        public WatsonTcpClient client;
        public Stream stream;

        // thread safe pipe to send messages from main thread to send thread
        public readonly MagnificentSendPipe sendPipe;

        // ManualResetEvent to wake up the send thread. better than Thread.Sleep
        // -> call Set() if everything was sent
        // -> call Reset() if there is something to send again
        // -> call WaitOne() to block until Reset was called
        public ManualResetEvent sendPending = new ManualResetEvent(false);

        public ConnectionState(WatsonTcpClient client, int MaxMessageSize)
        {
            this.client = client;
            client.Events.StreamReceived += StreamReceived;

            // create send pipe with max message size for pooling
            sendPipe = new MagnificentSendPipe(MaxMessageSize);
        }

        private void StreamReceived(object sender, StreamReceivedEventArgs args)
        {
            stream = args.DataStream;
        }
    }
}
