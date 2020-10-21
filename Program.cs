using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.IO;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace server
{
    
        public class Request
        {
            public string Method { get; set; }
            public string Path  { get; set; }
            public string Date { get; set; }
            public string Body { get; set; }

        }
    public class ServerResponse
    {
        public string Status { get; set; }
        public string Body { get; set; }
        public void addToStatusBody(String addBodyElement)
        {
            Status = Status + ", " + addBodyElement;
        }
    }
    public class Category
    {
        [JsonPropertyName("cid")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }


    public static class Program
    {
        
        static void Main(string[] args)
        {
            string localpath = "C:/Users/Amos/Desktop";
            var server = new TcpListener(IPAddress.Loopback, 5000);
            server.Start();
            Console.WriteLine("checking...");

            while (true)
            {
                var client = server.AcceptTcpClient();
                Console.WriteLine("------------New Client connected!---------------");
              
                WaitingforClientAnswer(client, localpath);

               // sendingToClient(client, "confirmed awaiting commands");
            }

        }

        static void WaitingforClientAnswer(TcpClient client, string localpath)
        {
            var requestStatus = "";
            var stream = client.GetStream();
            var recievingdata = new byte[client.ReceiveBufferSize];
            Console.WriteLine("Client buffer size: " + client.ReceiveBufferSize);
            Console.WriteLine("waiting for answer...");

            int cnt = 0;
            try
            {
                cnt = stream.Read(recievingdata);
            }
            catch (IOException)
            {
                Console.WriteLine("Connection closed...");
                return;
            };


            string msg = Encoding.UTF8.GetString(recievingdata, 0, cnt);



            
            Console.WriteLine("Translating from Json");

            Console.WriteLine();
            Request requestFromJson = Util.FromJson<Request>(msg);
            Console.WriteLine("Ready to treat request...");
            Console.WriteLine("Client request" + msg);
            Console.WriteLine($"Method: {requestFromJson.Method}");
            Console.WriteLine($"Path:   {requestFromJson.Path}");
            Console.WriteLine($"Date:   {requestFromJson.Date}");
            Console.WriteLine($"Body:   {requestFromJson.Body}");

            ServerResponse response = new ServerResponse();

            if (requestFromJson.Method != null && requestFromJson.Method.ToUpper() == "ECHO")
            {
                response.Body = requestFromJson.Body;

            }
            CheckClientDate(requestFromJson, response, ref requestStatus);
            CheckingMethodBodyAndPath(requestFromJson,  response, ref requestStatus, localpath);
            if (response.Status == null) {
                requestStatus = "1 Ok";
            }
            else if (response.Status != null &&
                response.Status.Contains("missing method") ||
                response.Status.Contains("illegal method") ||
                response.Status.Contains("missing resource") ||
                response.Status.Contains("Illegal body") ||
                response.Status.Contains("Illegal body") ||
                response.Status.Contains("missing date") ||
                response.Status.Contains("Illegal date")
                )
            {
                requestStatus = "6 ERROR";
            }else if (response.Status.Contains("bad request"))
            {
                requestStatus = "4 Bad Request";
            }else if (response.Status.Contains("file not found !"))
            {
                requestStatus = "5 not found";
            }
            if (requestStatus.ToLower() == "4 bad request" || requestStatus == "6 ERROR")
            {
                sendingToClient(client, response, requestStatus);
                return; 
            }  
            
            
            if (requestFromJson.Method != null && requestFromJson.Method.ToUpper() == "ECHO")
            {
                sendingToClient(client, response, requestStatus);
                return;
            }
            
            if(requestStatus == "1 Ok")
            {
                
                if (requestFromJson.Method.ToUpper() == "UPDATE") requestStatus = "3 updated";
                
            }
            
            if (requestStatus == "1 Ok" || requestStatus == "3 updated" || requestStatus == "4 deleted" ) {
                
                fileHandler(requestFromJson, response, localpath, ref requestStatus);
                
                sendingToClient(client, response, requestStatus);
            }
            else sendingToClient(client, response, requestStatus);

            Console.WriteLine("-----------------------------------------------");



        }

        

        private static void CheckClientDate(Request requestFromJson, ServerResponse response, ref string requestStatus)
        {
            try { 
                var time = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                if (requestFromJson.Date == null) response.addToStatusBody("missing date");
                else if(int.Parse(time)+500 >= int.Parse(requestFromJson.Date) && int.Parse(time) - 500 >= int.Parse(requestFromJson.Date))
                {
                    response.addToStatusBody("Illegal date");
                    
                }
            }
            catch
            {
                response.addToStatusBody("Illegal date");
                
            }
        }
        

        private static void CheckingMethodBodyAndPath(Request requestFromJson, ServerResponse  response, ref string requestStatus, string localpath)
        {
            if (requestFromJson.Method != null)
            {
                
                if(requestFromJson.Method.ToUpper() == "UPDATE" ||
                    requestFromJson.Method.ToUpper() == "CREATE" ||
                    requestFromJson.Method.ToUpper() == "DELETE" ||
                    requestFromJson.Method.ToUpper() == "ECHO" ||
                    requestFromJson.Method.ToUpper() == "READ" )
                {
                    if (requestFromJson.Body == null && requestFromJson.Method.ToUpper() != "READ" && requestFromJson.Method.ToUpper() != "DELETE")
                    {
                        response.addToStatusBody("missing body");
                    }
                    else if (requestFromJson.Body != null && !IsJson(requestFromJson.Body) &&
                        requestFromJson.Method.ToUpper() != "READ" &&
                        requestFromJson.Method.ToUpper() != "DELETE")
                    {
                        response.addToStatusBody("Illegal body");
                    }
                    if (requestFromJson.Path != null
                        && requestFromJson.Method.ToUpper() != "ECHO"
                        && requestFromJson.Method.ToUpper() != "CREATE"
                        && requestFromJson.Method.ToUpper() != "READ")
                    {
                        if(requestFromJson.Method.ToUpper() == "UPDATE" ||
                            requestFromJson.Method.ToUpper() == "DELETE" 
                            )
                        {
                            if(requestFromJson.Path == "/api/categories")
                            {
                                response.addToStatusBody("bad request");
                                Console.WriteLine("found bad boi");
                                return;
                            }
                            
                        }
                        try
                        {
                            StreamReader sr = File.OpenText(localpath + requestFromJson.Path);
                            Console.WriteLine("found file");
                            sr.Close();
                            Console.WriteLine("FOUND FILE GOOD REQUEST !");
                         
                        }
                        catch
                        {
                            
                            if(requestFromJson.Method.ToUpper() == "UPDATE" || requestFromJson.Method.ToUpper() == "DELETE")
                            {
                                response.addToStatusBody("file not found !");
                            }
                            else
                            {
                                
                                response.addToStatusBody("bad request");
                                return;
                            }
                            
                        };

                    }
                    else if (requestFromJson.Method.ToUpper() == "CREATE")
                    {
                        if (localpath + requestFromJson.Path == localpath + "/api/categories")
                        {
                            Category que = Util.FromJson<Category>(requestFromJson.Body);
                            que.Id = Directory.GetFiles(localpath + "/api/categories", "*", SearchOption.AllDirectories).Length + 1;
                            requestFromJson.Body = que.ToJson();
                            requestFromJson.Path = requestFromJson.Path + "/" + que.Id;
                            Console.WriteLine("Giving ID to Request : " + que.Id);
                        }
                        else
                        {
                            response.addToStatusBody("bad request"); 


                        }
                    }
                    
                    if (requestFromJson.Path == null && requestFromJson.Method.ToUpper() != "ECHO")
                    {
                        response.addToStatusBody("missing resource");
                    }
                    if (requestFromJson.Method.ToUpper() == "READ")
                    {
                        
                        if(requestFromJson.Path == null)
                        {
                            response.addToStatusBody("missing resource");
                        }
                        else if (!requestFromJson.Path.Contains("/api/categories"))
                        {
                            response.addToStatusBody("bad request");
                            return;
                        }
                        //Check wether input cid or id or path is in int value. 
                        else if (requestFromJson.Path != "/api/categories" && !isInteger(requestFromJson.Path[16..])  )
                        {
                            response.addToStatusBody("bad request");
                            Console.WriteLine("found bad request : " + requestFromJson.Path[16..]);
                            return;
                        }else
                        {
                            try
                            {
                                // Single file
                                StreamReader sr = File.OpenText(localpath + requestFromJson.Path) ;
                                Console.WriteLine("found file");
                                sr.Close();
                            
                            }
                            catch
                            {
                                try
                                {
                                    //directory
                                    int fCount = Directory.GetFiles(localpath + requestFromJson.Path, "*", SearchOption.AllDirectories).Length;
                                    if (fCount > 0)
                                    {
                                        Console.WriteLine($"Found Directory with : {fCount} Files!");
                                    
                                    }else response.addToStatusBody("file not found !"); 
                                }
                                catch
                                {
                                    Console.WriteLine("filenotfound");
                                    response.addToStatusBody("file not found !");

                                }
                            }
                        }
                    }
                    
                }
                else
                {
                    response.addToStatusBody("Illegal method");
                    requestStatus = "6 ERROR"; 


                }

            }
            else
            {
                response.addToStatusBody("missing method");
                requestStatus = "6 ERROR";
            }

           
        }

     

        static void sendingToClient(TcpClient client, ServerResponse message, string requestStatus )
        {
            if (requestStatus == "4 Bad Request")
            {
                message.Status = requestStatus;
            }
            else
            {
                message.Status = requestStatus + message.Status;
            }
            Console.WriteLine("Request processed");
            Console.WriteLine("Status: " + message.Status);
            Console.WriteLine("Body  : " + message.Body);
            string msg = message.ToJson();
            var stream = client.GetStream();
            var sendingdata = Encoding.UTF8.GetBytes(msg);

            Console.WriteLine("Sending as Json to client");
            stream.Write(sendingdata, 0, sendingdata.Length);

        }

        public static bool IsJson(this string input)
        {
            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}")
                   || input.StartsWith("[") && input.EndsWith("]");
        }


        public static bool isInteger(String input)
        {
            try
            {
                var que = int.Parse(input);
                return true; 
            }
            catch
            {
                return false;
            }
        }
        public static void fileHandler( Request request, ServerResponse response, string localpath, ref string requestStatus) {
        try
        {
                if (request.Method.ToUpper() == "DELETE")
                {
                    File.Delete(localpath + request.Path);
                    Console.WriteLine("Deleted file..." );
                }
            else if (request.Method.ToUpper() != "READ") {
                    var path = "";
                    int fCount = 0;
                    try
                    {
                       fCount = Directory.GetFiles(localpath + request.Path, "*", SearchOption.AllDirectories).Length + 1;
                       path = localpath + request.Path + "/" + fCount;
                        Console.WriteLine("Trying to create file at: " + path);
                    }
                    catch
                    { path = localpath + request.Path;}

                    // Create the file, or overwrite if the file exists.
                    Console.WriteLine("Trying to open file...");
                    using (FileStream fs = File.Create(path))
                    {  
                        byte[] info = new UTF8Encoding(true).GetBytes(request.Body);
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                        Console.WriteLine("creating file...");
                        fs.Close();
                        Console.WriteLine("Closing file...");
                    }

                    try
                    {
                        //Reading the created or updated file back to client..

                        using (StreamReader sr = File.OpenText(path))
                        {
                            string s = "";
                            while ((s = sr.ReadLine()) != null)
                            {
                                response.Body = response.Body + s;
                            }
                            Console.WriteLine(response.Body);
                            sr.Close();
                            Console.WriteLine("Closing file...");
                        }
                    }
                    catch
                    {
                        Console.WriteLine("filenotfound");
                        requestStatus = "bad request ";
                        return;
                    }

                }
            else if (request.Method.ToUpper() == "READ")
            {// Open the stream and read it back.
                    if(request.Path == "/api/categories")
                    {
                        var tempfileholder ="["; 
                        //var  tempfileholder = new List<String>();
                        foreach (string file in Directory.EnumerateFiles(localpath + request.Path))
                        {
                            string contents = File.ReadAllText(file);

                            //tempfileholder.Add(contents);
                            if(tempfileholder == "[") tempfileholder = tempfileholder  + contents;
                            else tempfileholder =  tempfileholder  +","+ contents;
                            
                        }
                        tempfileholder = tempfileholder  + "]";
                        
                        response.Body = tempfileholder;
                       

                    }
                    else
                    {
                        using (StreamReader sr = File.OpenText(localpath + request.Path))
                        {
                            string s = "";
                            while ((s = sr.ReadLine()) != null)
                            {
                                response.Body = response.Body + s;
                            }
                            sr.Close();
                            Console.WriteLine("Closing file...");

                        }
                    }

                    
                    
            }
        }
        catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }





    }
   

}
public static class Util
{
    public static T FromJson<T>(this string element)
    {
        return JsonSerializer.Deserialize<T>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
    public static string ToJson(this object data)
    {
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
 


}