/*
*
*    CouchDBHeartbeat - CouchDB changes API for Silverlight (Chunked HTTP response parser in C#).
*    Copyright (C) 2010 BLITZ Digital Studios LLC d/b/a BLITZ Agency.
*
*    This library is free software; you can redistribute it and/or modify it 
*    under the terms of the GNU Lesser General Public License as published
*    by the Free Software Foundation; either version 2.1 of the License, or 
*    (at your option) any later version.
*
*    This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
*    without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
*    PURPOSE. See the GNU Lesser General Public License for more details.
*
*    You should have received a copy of the GNU Lesser General Public License along 
*    with this library; if not, write to the Free Software Foundation, Inc.,
*    59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
*
*    BLTIZ Digital Studios LLC, 1453 3rd Street Promenade, Ste 420, Santa Monica CA, 90401
*    http://www.blitzagency.com
*    http://labs.blitzagency.com
*
*        Author: Adam Venturella - aventurella@blitzagency.com
*
*/
using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Json;
using System.Text;
using System.Diagnostics;
using System.IO.IsolatedStorage;

namespace BlitzAgency.Net.CouchDB
{
    public class CouchDBHeartbeatEventArgs : EventArgs
    {
        private JsonValue document;

        public CouchDBHeartbeatEventArgs(JsonValue document)
        {
            this.document = document;
        }

        public JsonValue Document
        {
            get { return this.document; }
        }
       
    }

    public class CouchDBHeartbeat
    {
        const int SOCKET_BUFFER_SIZE = 1024;
        const string HTTP_RESPONSE_KEY = "HTTP/1.1 200 OK\r\n";
        const string COUCH_DB_HOST = "yourdomian.com";
        const int COUCH_DB_PORT = 4530;

        byte[] message_buffer;
        int message_buffer_offset = 0;
        
        private string Offset;

        public event CouchDBHeartbeatHandler CouchDBHeartbeatUpdate;
        public delegate void CouchDBHeartbeatHandler(object sender, CouchDBHeartbeatEventArgs e);

        public void Begin(string Offset)
        {
            this.Offset = Offset;
            this.Begin();
        }
            
        public void Begin()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            DnsEndPoint endpoint = new DnsEndPoint(CouchDBHeartbeat.COUCH_DB_HOST, CouchDBHeartbeat.COUCH_DB_PORT);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();

            args.RemoteEndPoint = endpoint;
            args.UserToken = socket;
            args.SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;
            args.Completed += new EventHandler<SocketAsyncEventArgs>(connection_Completed);
            socket.ConnectAsync(args);

        }


        void connection_Completed(object sender, SocketAsyncEventArgs e)
        {

            Socket socket = (Socket) e.UserToken;
            
#if Debug
            Debug.WriteLine("Socket Connected");
            Debug.WriteLine(e.SocketError.ToString());
            Debug.WriteLine(socket.Connected);
#endif

            if (socket.Connected)
            {
                StringBuilder builder = new StringBuilder(String.Empty);
                
                if (Offset != null && int.Parse(Offset) > 0)
                {
                    builder.Append("GET /changes?since="+Offset+" HTTP/1.1\r\n");
                }
                else
                {
                    builder.Append("GET /changes HTTP/1.1\r\n");
                }
               
                builder.Append("Host: "+CouchDBHeartbeat.COUCH_DB_HOST+ ':'+CouchDBHeartbeat.COUCH_DB_PORT.ToString() + "\r\n");
                builder.Append("Connection: Keep-Alive\r\n\r\n");


                byte[] request = Encoding.UTF8.GetBytes(builder.ToString());
                
                e.SetBuffer(request, 0, request.Length);
                e.Completed -= new EventHandler<SocketAsyncEventArgs>(connection_Completed);
                e.Completed += new EventHandler<SocketAsyncEventArgs>(send_Completed);
                
                socket.SendAsync(e);
            }
          
        }

        void send_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.SetBuffer(new byte[SOCKET_BUFFER_SIZE], 0, SOCKET_BUFFER_SIZE);
            e.Completed -= new EventHandler<SocketAsyncEventArgs>(send_Completed);
            e.Completed += new EventHandler<SocketAsyncEventArgs>(receive_Completed);
            Socket socket = (Socket)e.UserToken;
            socket.ReceiveAsync(e);
        }

        void receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket socket = (Socket)e.UserToken;
            MemoryStream stream = new MemoryStream(e.Buffer);


            if (e.Buffer.Length >= HTTP_RESPONSE_KEY.Length)
            {
                byte[] httpResponseTest = new byte[HTTP_RESPONSE_KEY.Length];
                stream.Read(httpResponseTest, 0, HTTP_RESPONSE_KEY.Length);

                if (Encoding.UTF8.GetString(httpResponseTest, 0, httpResponseTest.Length) == HTTP_RESPONSE_KEY)
                {
                    byte[] bytes = new byte[stream.Length - 198];
                    stream.Seek(198, SeekOrigin.Begin);
                }
                else
                {
                    stream.Position = 0;
                }
            }


            while (stream.Position < stream.Length)
            {
                if (message_buffer == null)
                {

                    byte[] lengthBytes = null;

                    int chunkSizeBegin = (int)stream.Position;
                    int chunckSize = 0;

                    while (stream.CanRead)
                    {
                        char data = (char)stream.ReadByte();

                        if (data == '\r')
                        {

                            chunckSize = ((int)stream.Position - 1) - chunkSizeBegin;
                            if (chunckSize > 0)
                            {
                                lengthBytes = new byte[chunckSize];
                                stream.Position = chunkSizeBegin;
                                stream.Read(lengthBytes, 0, chunckSize);
                                break;
                            }
                        }
                        else if (data == '\0')
                        {
                            stream.Close();
                            stream.Dispose();

                            e.SetBuffer(new byte[SOCKET_BUFFER_SIZE], 0, SOCKET_BUFFER_SIZE);
                            socket.ReceiveAsync(e);
                            return;
                        }
                    }


                    int message_buffer_length = Convert.ToInt32(Encoding.UTF8.GetString(lengthBytes, 0, chunckSize), 16);

                    // chunked encoding include the \r\n in the total data for the end of the line, so we trim those laste 2 bytes
                    message_buffer_length = message_buffer_length - 1;

                    // move past the \r\n for the chunk size
                    stream.Seek(2, SeekOrigin.Current);

                    message_buffer = new byte[message_buffer_length];

                    if (message_buffer_length > 0 && (stream.Position + message_buffer_length) < stream.Length)
                    {
                        stream.Read(message_buffer, 0, message_buffer_length);
                        receivedDocument(message_buffer);

                        // move past the \r\n for the end of the chunk which will be followed by 
                        // another \r\n designating the beginning of the next chunck
                        stream.Seek(3, SeekOrigin.Current);
                    }
                    else if (message_buffer_length > 0)
                    {
                        int bytes_to_read = (int)(stream.Length - stream.Position);
                        stream.Read(message_buffer, 0, bytes_to_read);
                        message_buffer_offset = bytes_to_read;
                    }
                    else
                    {
                        // _changes is set to have a heartbeat every 15 
                        // seconds to keep the connection alive. 
                        // we catch that heartbeat here
                        message_buffer = null;
                        message_buffer_offset = 0;

                        stream.Close();
                        stream.Dispose();

                        e.SetBuffer(new byte[SOCKET_BUFFER_SIZE], 0, SOCKET_BUFFER_SIZE);
                        socket.ReceiveAsync(e);
                        return;
                    }
                }
                else
                {
                    int bytes_to_read = (int)(message_buffer.Length - message_buffer_offset);

                    if (stream.Length < bytes_to_read)
                    {
                        bytes_to_read = (int)stream.Length;
                    }

                    stream.Read(message_buffer, message_buffer_offset, bytes_to_read);
                    message_buffer_offset = message_buffer_offset + bytes_to_read;

                    if (message_buffer_offset == (int)message_buffer.Length)
                    {
                        receivedDocument(message_buffer);

                        // move past the \r\n for the end of the chunk which will be followed by 
                        // another \r\n designating the beginning of the next chunck
                        //Debug.WriteLine(String.Format("Length: {0} | Position: {1}", stream.Length, stream.Position));
                        stream.Seek(3, SeekOrigin.Current);

                    }
                }
            }

            stream.Close();
            stream.Dispose();

            e.SetBuffer(new byte[SOCKET_BUFFER_SIZE], 0, SOCKET_BUFFER_SIZE);
            socket.ReceiveAsync(e);
        }

        void receivedDocument(byte[] document)
        {
            string json = Encoding.UTF8.GetString(document, 0, document.Length);
            try
            {
                OnCouchDBHeartbeatUpdate(new CouchDBHeartbeatEventArgs(JsonObject.Parse(json)));

            }
            catch
            {
#if Debug
                Debug.WriteLine(json);
#endif
            }

            document = null;
            message_buffer = null;
            message_buffer_offset = 0;
        }

        protected virtual void OnCouchDBHeartbeatUpdate(CouchDBHeartbeatEventArgs e)
        {
            Debug.WriteLine(e.Document.ToString());
            if (CouchDBHeartbeatUpdate != null) { CouchDBHeartbeatUpdate(this, e); }
        }
    }
}
