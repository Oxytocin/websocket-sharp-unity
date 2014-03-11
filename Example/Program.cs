#if NOTIFY
using Notifications;
#endif
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace Example
{
    public struct NfMessage
    {

        public string Summary;
        public string Body;
        public string Icon;
    }

    public class ThreadState
    {

        public bool           Enabled      { get; set; }

        public AutoResetEvent Notification { get; private set; }

        public ThreadState ()
        {
            Enabled = true;
            Notification = new AutoResetEvent (false);
        }
    }

    public class Program
    {

        private static Queue _msgQ = Queue.Synchronized (new Queue ());

        private static void enNfMessage (string summary, string body, string icon)
        {
            var msg = new NfMessage
      {
        Summary = summary,
        Body = body,
        Icon = icon
      };

            _msgQ.Enqueue (msg);
        }

        public static void Main (string[] args)
        {
            var ts = new ThreadState ();

            WaitCallback notifyMsg = state => 
            {
                while (ts.Enabled || _msgQ.Count > 0) {
                    Thread.Sleep (500);

                    if (_msgQ.Count > 0) {
                        var msg = (NfMessage)_msgQ.Dequeue ();
                        #if NOTIFY
            var nf = new Notification(msg.Summary, msg.Body, msg.Icon);
            nf.AddHint("append", "allowed");
            nf.Show();
                        #else
                        Console.WriteLine ("{0}: {1}", msg.Summary, msg.Body);
                        #endif
                    }
                }

                ts.Notification.Set ();
            };

            ThreadPool.QueueUserWorkItem (notifyMsg);

            using (var ws = new WebSocket("ws://echo.websocket.org")) {
      //using (var ws = new WebSocket("wss://echo.websocket.org"))
      //using (var ws = new WebSocket("ws://localhost:4649"))
      //using (var ws = new WebSocket("ws://localhost:4649/Echo"))
      //using (var ws = new WebSocket("wss://localhost:4649/Echo"))
      //using (var ws = new WebSocket("ws://localhost:4649/Echo?name=nobita"))
      //using (var ws = new WebSocket("ws://localhost:4649/エコー?name=のび太"))
      //using (var ws = new WebSocket("ws://localhost:4649/Chat"))
      //using (var ws = new WebSocket("ws://localhost:4649/Chat?name=nobita"))
      //using (var ws = new WebSocket("ws://localhost:4649/チャット?name=のび太"))
                ws.OnOpen += (sender, e) =>
                {
                    ws.Send ("Hi, all!");
                };

                ws.OnMessage += (sender, e) =>
                {
                    MessageEventArgs _e = (MessageEventArgs)e;
                    if (!String.IsNullOrEmpty (_e.Data)) {
                        enNfMessage ("[WebSocket] Message", _e.Data, "notification-message-im");
                    }
                };

                ws.OnError += (sender, e) =>
                {
                    ErrorEventArgs _e = (ErrorEventArgs)e;
                    enNfMessage ("[WebSocket] Error", _e.Message, "notification-message-im");
                };

                ws.OnClose += (sender, e) =>
                {
                    CloseEventArgs _e = (CloseEventArgs)e;
                    enNfMessage (
            String.Format ("[WebSocket] Close({0})", _e.Code),
            _e.Reason,
            "notification-message-im");
                };

                #if DEBUG
        ws.Log.Level = LogLevel.Trace;
                #endif
                //ws.Compression = CompressionMethod.Deflate;
                //ws.Origin = "http://echo.websocket.org";
                //ws.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                //{
                //  ws.Log.Debug(String.Format("\n{0}\n{1}", certificate.Issuer, certificate.Subject));
                //  return true;
                //};
                //ws.SetCookie(new Cookie("nobita", "\"idiot, gunfighter\""));
                //ws.SetCookie(new Cookie("dora", "tanuki"));
                //ws.SetCredentials ("nobita", "password", false);
                ws.Connect ();
                //ws.ConnectAsync();
                //Console.WriteLine("Compression: {0}", ws.Compression);

                Thread.Sleep (500);
                Console.WriteLine ("\nType \"exit\" to exit.\n");

                string data;
                while (true) {
                    Thread.Sleep (500);

                    Console.Write ("> ");
                    data = Console.ReadLine ();
                    if (data == "exit") {
          //if (data == "exit" || !ws.IsAlive)
                        break;
                    }

                    ws.Send (data);
                }
            }

            ts.Enabled = false;
            ts.Notification.WaitOne ();
        }
    }
}
