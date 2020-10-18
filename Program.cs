using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;



namespace server
{
    public class Category
    {

        public Category(string method, string path, DateTime timeset, string body)
        {

        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            
            var server = new TcpListener(IPAddress.Loopback, 5000);
            server.Start();
            Console.WriteLine("checking...");

            while (true)
            {
                var client = server.AcceptTcpClient();
                Console.WriteLine("Client connected!");
                var stream = client.GetStream();
                sendingToClient(client, "confirmed awaiting commands");
                
                
                WaitingforClientAnswer(client, stream);
                

            }

        }
        static void WaitingforClientAnswer(TcpClient client, NetworkStream stream)
        {
            Console.WriteLine("waiting for client request...");
            var recievingdata = new byte[client.ReceiveBufferSize];
            var cnt = stream.Read(recievingdata);
            var msg = Encoding.UTF8.GetString(recievingdata, 0, cnt);

            Console.WriteLine($"Client sendt back char length {msg.Length}, message :{msg}");

            if ((msg[0..4]).ToUpper() == "READ")
            {
                sendingToClient(client, "Client wants to read table");
            }
            if (msg[0..6].ToUpper() == "UPDATE")
            {
                sendingToClient(client, "Client wants to update table");
            }
            if (msg[0..4].ToUpper() == "DELETE")
            {
                sendingToClient(client, "Client wants to delete table");
            }


            

        }
        static void sendingToClient(TcpClient client, string message)
        {
            var stream = client.GetStream();
            var sendingdata = Encoding.UTF8.GetBytes(message);
            Console.WriteLine($"Sending ( {message} ) to server");
            stream.Write(sendingdata);

        }



    }
}
