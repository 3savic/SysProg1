using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SysProg1
{
    internal class HttpServer
    {
        public static readonly byte[] notFoundRequestBody = Encoding.ASCII.GetBytes("<h1>Not found.</h1>");

        private string serverAddress;
        private uint portNumber;
        private string rootDir;
        private LRUCache<string, byte[]> cache;

        public HttpServer(string serverAddress, uint portNumber, string rootDir, LRUCache<string, byte[]> cache = null)
        {
            this.serverAddress = serverAddress;
            this.portNumber = portNumber;
            this.rootDir = rootDir;
            this.cache = cache ?? new LRUCache<string, byte[]>(10);
        }

        private void SendResponse(HttpListenerContext context, byte[] responseBody, string contentType, HttpStatusCode statusCode = HttpStatusCode.OK, Boolean att = false)
        {
            string logString = string.Format(
                "REQUEST:\n{0} {1} HTTP/{2}\nHost: {3}\nUser-agent: {4}\n-------------------\nRESPONSE:\nStatus: {5}\nDate: {6}\nContent-Type: {7}\nContent-Length: {8}\n",
                context.Request.HttpMethod,
                context.Request.RawUrl,
                context.Request.ProtocolVersion,
                context.Request.UserHostName,
                context.Request.UserAgent,
                statusCode,
                DateTime.Now,
                contentType,
                responseBody.Length
            );
            if(att == true)
            {
                context.Response.AppendHeader("Content-Disposition", "attachment");
            }
            context.Response.ContentType = contentType;
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentLength64 = responseBody.Length;

            using (Stream outputStream = context.Response.OutputStream)
            {
                outputStream.Write(responseBody, 0, responseBody.Length);
            }
            Console.WriteLine(logString);
        }

        private string BuildPage()
        {
            FileInfo[] files = new DirectoryInfo(rootDir).GetFiles();
            string elements = string.Empty;
            foreach(FileInfo file in files)
            {
                elements += $"<li><a href=\"http://localhost:8080/{file.Name}\" target=\"_blank\">{file.Name}</a></li>";
            }
            string responseBody = "<HTML>" +
                                  "<BODY>" +
                                  "<ul>" +
                                  elements+
                                  "</ul>" +
                                  "</BODY>" +
                                  "</HTML>";

            return responseBody;
        }

        private void DownloadFile(string fileName, HttpListenerContext context)
        {
            string filePath = Path.GetFullPath(rootDir) + fileName;
            byte[] responseBody;
            if (!cache.TryGetValue(fileName, out responseBody))
            { 
                if (!File.Exists(filePath))
                {
                    SendResponse(context, notFoundRequestBody, "text/html", HttpStatusCode.NotFound);
                    Console.WriteLine("File does not exist.");
                }
                else
                {
                    Console.WriteLine($"Requested download for {filePath}");
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        responseBody = new byte[fs.Length];
                        fs.Read(responseBody, 0, responseBody.Length);
                        cache.Add(fileName, responseBody);
                        SendResponse(context, responseBody, "attachment", HttpStatusCode.OK, true);
                        Console.WriteLine($"\"{filePath}\" has been downloaded.");
                    }        
                }
            }
            else
            {
                SendResponse(context, responseBody, "attachment", HttpStatusCode.OK, true);
                Console.WriteLine($"\"{filePath}\" has been downloaded from the cache.");
            }
        }
        public void Launch()
        {

            using (HttpListener httpListener = new HttpListener())
            {
                string baseUrl = $"http://{serverAddress}:{portNumber}/";
                httpListener.Prefixes.Add(baseUrl);
                httpListener.Start();
                Console.WriteLine($"Server is listening {baseUrl}...\n");

                while (httpListener.IsListening)
                {
                    HttpListenerContext context = httpListener.GetContext();

                    ThreadPool.QueueUserWorkItem((object httpListenerContext) =>
                    {
                        try
                        {
                            HttpListenerContext localContext = httpListenerContext as HttpListenerContext;

                            string fileName = Path.GetFileName(localContext.Request.RawUrl);
                            
                            if (fileName == string.Empty)
                            {
                                byte[] responseBody = Encoding.ASCII.GetBytes(BuildPage());
                                SendResponse(context, responseBody, "text/html", HttpStatusCode.OK);
                                return;
                            }
                            else
                            {
                                DownloadFile(fileName, context);
                            }

                        }
                        catch (Exception ex)
                        {
                            if (context.Response.OutputStream.CanWrite)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                context.Response.OutputStream.Close();
                            }
                            Console.WriteLine(ex.Message);
                        }
                    }, context);
                }
            }

        }
    }
}
