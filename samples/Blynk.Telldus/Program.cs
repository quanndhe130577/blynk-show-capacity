using Blynk.Virtual;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Telldus
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class Options
    {
        [ArgShortcut("s")]
        [ArgShortcut("--server")]
        [ArgDefaultValue("tcp://127.0.0.1:8080")]
        [ArgDescription("The url to the Blynk server.")]
        public string Server { get; set; }

        [ArgShortcut("a")]
        [ArgShortcut("--authorization")]
        [ArgDefaultValue("****")]
        [ArgDescription("The device authorization token used to identitify this application.")]
        public string Authorization { get; set; }

        [ArgShortcut("t")]
        [ArgShortcut("--telldus")]
        [ArgDefaultValue("")]
        [ArgDescription("The Telldus websocket device uri.")]
        public string Telldus { get; set; }

        [ArgShortcut("i")]
        [ArgShortcut("--identification")]
        [ArgDefaultValue("")]
        [ArgDescription("The Telldus device id.")]
        public string Identification { get; set; }

        [ArgShortcut("d")]
        [ArgShortcut("--debug")]
        [ArgDescription("Show debug information.")]
        public bool Debug { get; set; }

        [ArgShortcut("b")]
        [ArgShortcut("--broadcast")]
        [ArgDefaultValue("255.255.255.255")]
        [ArgDescription("Show debug information.")]
        public string Broadcast { get; set; }

    }
    class Program
    {
        static void Main(string[] args)
        {
            Options options = null;
            try
            {
                options = Args.Parse<Options>(args);
            }
            catch (ArgException e)
            {
                Console.WriteLine(string.Format("Problems with the command line options: {0}", e.Message));
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Options>());
                return;
            }

            var telldusUri = options.Telldus;
            if (string.IsNullOrEmpty(telldusUri))
            {
                var found = new List<(string Ip, string Id)>();
                string ip = null;
                using (var discover = new Discover(30303, options.Broadcast))
                {
                    discover.OnFoundDevice += (ipEnd, id) =>
                    {
                        found.Add((ipEnd, id));
                        if (options.Debug)
                            Console.WriteLine($"ip {ipEnd}, id {id}");
                    };
                    discover.Start();
                    for (int i = 0; i < 10; i++)
                    {
                        discover.Send();
                    }
                    Task.Delay(2000).Wait();
                    discover.Stop();
                }
                if (string.IsNullOrEmpty(options.Identification))
                {
                    ip = found.Select(v => v.Ip).FirstOrDefault();
                }
                else
                {
                    ip = found.Where(v => v.Item2.Contains(options.Identification)).Select(v => v.Item1).FirstOrDefault();
                }
                if (ip != null)
                {
                    telldusUri = $"ws://{ip}/ws";
                }
                else
                {
                    Console.WriteLine($"Cannot find telldus device with id {options.Identification}");
                    return;
                }
            }
            if (string.IsNullOrEmpty(telldusUri))
            {
                Console.WriteLine("Cannot find a telldus device");
                return;
            }
            // Connect to blynk server
            var url = options.Server; // Blynk server address
            var authorization = options.Authorization; // Authorization token
            using (var client = new Client(url, authorization))
            {
                client.Connect();
                var tcs = new TaskCompletionSource<bool>();
                client.OnAuthorized += v => tcs.SetResult(v);
                client.Login();
                var authorized = tcs.Task.Result;
                if (authorized)
                {
                    Console.WriteLine("Hardware client is authorized with given token");

                    // Connect to Telldus device
                    using (var ws = new TelldusClient(telldusUri))
                    {
                        ws.ConnectAsync().Wait();
                        ws.OnMessage += m =>
                        {
                            var id = (int)m["id"];
                            var type = (string)m["type"];
                            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            var name = (string)m["name"];
                            if (type == "device")
                            {
                                var state = (int)m["state"];
                                switch (state)
                                {
                                    case 1:
                                        client.WriteVirtualPin(id, $"{time} {name} on\r\n");
                                        break;
                                    case 2:
                                        client.WriteVirtualPin(id, $"{time} {name} off\r\n");
                                        break;
                                    default:
                                        client.WriteVirtualPin(id, $"{time} {name} other state {state}\r\n");
                                        break;
                                }
                            }
                            else if (type == "sensor")
                            {
                                var valueType = (int)m["valueType"];
                                if (valueType == 1)
                                {
                                    var value = (double)m["value"];
                                    client.WriteVirtualPin(id, value);
                                }
                            }
                            if (options.Debug)
                                Console.WriteLine(m.ToString());
                        };

                        var closeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                        Console.CancelKeyPress += (o, e) =>
                        {
                            closeTcs.SetResult(true);
                        };
                        ws.StartMessageLoop();
                        Console.WriteLine("Server is active. Press CTRL+C to stop.");
                        closeTcs.Task.Wait();
                        Console.WriteLine("Stopping server.");
                        ws.CloseAsync().Wait();
                    }
                }
                else
                {
                    Console.WriteLine("Cannot authorize client with given token.");
                }
                client.Disconnect();
            }
        }
    }
}
