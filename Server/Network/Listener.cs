#region Header
// **************************************\
//     _  _   _   __  ___  _   _   ___   |
//    |# |#  |#  |## |### |#  |#  |###   |
//    |# |#  |# |#    |#  |#  |# |#  |#  |
//    |# |#  |#  |#   |#  |#  |# |#  |#  |
//   _|# |#__|#  _|#  |#  |#__|# |#__|#  |
//  |##   |##   |##   |#   |##    |###   |
//        [http://www.playuo.org]        |
// **************************************/
//  [2014] Listener.cs
// ************************************/
#endregion

#region References
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
#endregion

namespace Server.Network
{
	public class Listener : IDisposable
	{
		private Socket m_Listener;

		private readonly Queue<Socket> m_Accepted;
		private readonly object m_AcceptedSyncRoot;

#if NewAsyncSockets
		private SocketAsyncEventArgs m_EventArgs;
        #else
		private readonly AsyncCallback m_OnAccept;
#endif

		private static readonly Socket[] m_EmptySockets = new Socket[0];

		public static IPEndPoint[] EndPoints { get; set; }

		public Listener(IPEndPoint ipep)
		{
			m_Accepted = new Queue<Socket>();
			m_AcceptedSyncRoot = ((ICollection)m_Accepted).SyncRoot;

			m_Listener = Bind(ipep);

			if (m_Listener == null)
			{
				return;
			}

			DisplayListener();

#if NewAsyncSockets
			m_EventArgs = new SocketAsyncEventArgs();
			m_EventArgs.Completed += new EventHandler<SocketAsyncEventArgs>( Accept_Completion );
			Accept_Start();
            #else
			m_OnAccept = OnAccept;
			try
			{
				IAsyncResult res = m_Listener.BeginAccept(m_OnAccept, m_Listener);
			}
			catch (SocketException ex)
			{
				NetState.TraceException(ex);
			}
			catch (ObjectDisposedException)
			{ }
#endif
		}

		private Socket Bind(IPEndPoint ipep)
		{
			var s = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try
			{
				s.LingerState.Enabled = false;
#if !MONO
				s.ExclusiveAddressUse = false;
#endif
				s.Bind(ipep);
				s.Listen(8);

				return s;
			}
			catch (Exception e)
			{
				if (e is SocketException)
				{
					var se = (SocketException)e;

					if (se.ErrorCode == 10048)
					{
						// WSAEADDRINUSE
						Utility.PushColor(ConsoleColor.Red);
						Console.WriteLine("Listener Failed: {0}:{1} (In Use)", ipep.Address, ipep.Port);
						Utility.PopColor();
					}
					else if (se.ErrorCode == 10049)
					{
						// WSAEADDRNOTAVAIL
						Utility.PushColor(ConsoleColor.Red);
						Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", ipep.Address, ipep.Port);
						Utility.PopColor();
					}
					else
					{
						Utility.PushColor(ConsoleColor.Red);
						Console.WriteLine("Listener Exception:");
						Console.WriteLine(e);
						Utility.PopColor();
					}
				}

				return null;
			}
		}

		private void DisplayListener()
		{
			var ipep = m_Listener.LocalEndPoint as IPEndPoint;

			if (ipep == null)
			{
				return;
			}

			if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
			{
				NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface adapter in adapters)
				{
					IPInterfaceProperties properties = adapter.GetIPProperties();
					foreach (IPAddressInformation unicast in properties.UnicastAddresses)
					{
						if (ipep.AddressFamily == unicast.Address.AddressFamily)
						{
							Utility.PushColor(ConsoleColor.Green);
							Console.WriteLine("Listening: {0}:{1}", unicast.Address, ipep.Port);
							Utility.PopColor();
						}
					}
				}
				/*
                try {
                Console.WriteLine( "Listening: {0}:{1}", IPAddress.Loopback, ipep.Port );
                IPHostEntry iphe = Dns.GetHostEntry( Dns.GetHostName() );
                IPAddress[] ip = iphe.AddressList;
                for ( int i = 0; i < ip.Length; ++i )
                Console.WriteLine( "Listening: {0}:{1}", ip[i], ipep.Port );
                }
                catch { }
                */
			}
			else
			{
				Utility.PushColor(ConsoleColor.Green);
				Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
				Utility.PopColor();
			}

			Utility.PushColor(ConsoleColor.DarkGreen);
			Console.WriteLine(@"----------------------------------------------------------------------");
			Utility.PopColor();
		}

#if NewAsyncSockets
		private void Accept_Start()
		{
			bool result = false;

			do {
				try {
					result = !m_Listener.AcceptAsync( m_EventArgs );
				} catch ( SocketException ex ) {
					NetState.TraceException( ex );
					break;
				} catch ( ObjectDisposedException ) {
					break;
				}

				if ( result )
					Accept_Process( m_EventArgs );
			} while ( result );
		}

		private void Accept_Completion( object sender, SocketAsyncEventArgs e )
		{
			Accept_Process( e );

			Accept_Start();
		}

		private void Accept_Process( SocketAsyncEventArgs e )
		{
			if ( e.SocketError == SocketError.Success && VerifySocket( e.AcceptSocket ) ) {
				Enqueue( e.AcceptSocket );
			} else {
				Release( e.AcceptSocket );
			}

			e.AcceptSocket = null;
		}

        #else

		private void OnAccept(IAsyncResult asyncResult)
		{
			var listener = (Socket)asyncResult.AsyncState;

			Socket accepted = null;

			try
			{
				accepted = listener.EndAccept(asyncResult);
			}
			catch (SocketException ex)
			{
				NetState.TraceException(ex);
			}
			catch (ObjectDisposedException)
			{
				return;
			}

			if (accepted != null)
			{
				if (VerifySocket(accepted))
				{
					Enqueue(accepted);
				}
				else
				{
					Release(accepted);
				}
			}

			try
			{
				listener.BeginAccept(m_OnAccept, listener);
			}
			catch (SocketException ex)
			{
				NetState.TraceException(ex);
			}
			catch (ObjectDisposedException)
			{ }
		}
#endif

		private bool VerifySocket(Socket socket)
		{
			try
			{
				var args = new SocketConnectEventArgs(socket);

				EventSink.InvokeSocketConnect(args);

				return args.AllowConnection;
			}
			catch (Exception ex)
			{
				NetState.TraceException(ex);

				return false;
			}
		}

		private void Enqueue(Socket socket)
		{
			lock (m_AcceptedSyncRoot)
			{
				m_Accepted.Enqueue(socket);
			}

			Core.Set();
		}

		private void Release(Socket socket)
		{
			try
			{
				socket.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException ex)
			{
				NetState.TraceException(ex);
			}

			try
			{
				socket.Close();
			}
			catch (SocketException ex)
			{
				NetState.TraceException(ex);
			}
		}

		public Socket[] Slice()
		{
			Socket[] array;

			lock (m_AcceptedSyncRoot)
			{
				if (m_Accepted.Count == 0)
				{
					return m_EmptySockets;
				}

				array = m_Accepted.ToArray();
				m_Accepted.Clear();
			}

			return array;
		}

		public void Dispose()
		{
			Socket socket = Interlocked.Exchange(ref m_Listener, null);

			if (socket != null)
			{
				socket.Close();
			}
		}
	}
}