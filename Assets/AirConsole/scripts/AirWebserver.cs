using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Text;
using UnityEngine;

namespace AirConsole {

    public class AirWebserver {

        private static HttpListener listener = new HttpListener();
        private static string startUpPath;
        private static int port;
        private static StartMode startMode;
        private static DebugLevel debug;

        public AirWebserver(int pPort, DebugLevel pDebug, StartMode pStartMode, string pStartUpPath) {

            port = pPort;
            debug = pDebug;
            startMode = pStartMode;
            startUpPath = pStartUpPath;  
        }

        public void Start() {

            if (!listener.IsListening) {

                listener.Start();
                listener.Prefixes.Add(string.Format("http://{0}:{1}/", GetIPAddress(), port.ToString()));

                Thread t = new Thread(new ThreadStart(ClientListener));
                t.Start();
            }

            if (startMode != StartMode.NoBrowserStart) {
                Application.OpenURL(GetUrl(startMode)+"http://" + GetIPAddress() + ":" + port + "/");
            }
        }

        public string GetUrl(StartMode mode) {

            switch (mode) {
                case StartMode.VirtualControllers:
                    return AirController.AIRCONSOLE_NORMAL_URL;
                case StartMode.Debug:
                    return AirController.AIRCONSOLE_DEBUG_URL;
                case StartMode.Normal:
                    return AirController.AIRCONSOLE_URL;
                default:
                    return "";
            }
        }

        public static void ClientListener() {

            while (true) {

                try {

                    HttpListenerContext request = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(ProcessRequest, request);
                }

                catch (Exception e) {

                    if (debug.error) {
                        Debug.Log(e.Message); 
                    }
                }
            }
        }

        public static void ProcessRequest(object listenerContext) {

            try {
               
                var context = (HttpListenerContext)listenerContext;
                string filename = Path.GetFileName(context.Request.RawUrl);

                string path = startUpPath + context.Request.RawUrl;

                byte[] msg;

                if (!File.Exists(path)) {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    msg = Encoding.UTF8.GetBytes("<html><head><title>AirConsole Error</title></head><body><h1>AirWebserver can't find resources!</h1></body></html>");

                } else {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    msg = File.ReadAllBytes(path);
                    context.Response.ContentType = ReturnMIMEType(Path.GetExtension(filename));
                }

                context.Response.ContentLength64 = msg.Length;

                using (Stream s = context.Response.OutputStream) {
                    s.Write(msg, 0, msg.Length);
                }
            }

            catch (Exception e) {

                if (debug.error) {
                    Debug.LogError(e.Message);
                }
            }
        }

        public static string ReturnMIMEType(string filename) {

            switch (filename) {
                case ".txt":
                    return "text/plain";
                case ".gif":
                    return "image/gif";
                case ".png":
                    return "image/png";
                case ".jpg":
                case "jpeg":
                    return "image/jpeg";
                case ".bmp":
                    return "image/bmp";
                case ".wav":
                    return "audio/wav";
                case ".html":
                    return "text/html";
                case ".htm":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
                default:
                    return "application/octet-stream";
            }
        }

        public static string GetIPAddress() {
            #if UNITY_EDITOR
            return Network.player.ipAddress;
            #else
            return "localhost";
            #endif
        }
        
    }

}