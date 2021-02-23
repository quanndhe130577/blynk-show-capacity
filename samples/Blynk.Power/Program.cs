using Blynk.Virtual;
using PowerArgs;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Blynk.Power
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class Options
    {
        [ArgShortcut("p")]
        [ArgShortcut("--pin")]
        [ArgDefaultValue(18)]
        [ArgDescription("Listening pin connected to power led.")]
        public int Pin { get; set; }

        [ArgShortcut("s")]
        [ArgShortcut("--server")]
        [ArgDefaultValue("tcp://192.168.68.115:8080")]
        [ArgDescription("The url to the Blynk server.")]
        public string Server { get; set; }

        [ArgShortcut("a")]
        [ArgShortcut("--authorization")]
        [ArgDefaultValue("P7KUVYCfqmnfZQxy26ZupNH5wrCH3y6V")]
        [ArgDescription("The device authorization token used to identitify the repeater.")]
        public string Authorization { get; set; }

        [ArgShortcut("d")]
        [ArgShortcut("--debug")]
        [ArgDescription("Show debug information.")]
        public bool Debug { get; set; }

    }
    public enum State
    {
        None,
        Up,
        Down,
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

                    var pin = options.Pin;
                    var debug = options.Debug;
                    Console.WriteLine("Read event on a pin");
                    using (var controller = new GpioController())
                    {
                        controller.OpenPin(pin, PinMode.Input);
                        Console.WriteLine($"GPIO pin enabled for use: {pin}");

                        var powerWatch = new Stopwatch();
                        var blinkWatch = new Stopwatch();
                        var state = State.None;
                        var previousOk = false;
                        var blinkTime = 12; // [ms]
                        var risingEventHandler = new PinChangeEventHandler((@object, a) =>
                        {
                            if (state == State.Up)
                            {
                                if (debug)
                                    Console.WriteLine($"Invalid. Rising event. State {state}");
                                state = State.None;
                                previousOk = false;
                            }
                            else
                            {
                                state = State.Up;
                                blinkWatch.Restart();
                                if (debug)
                                    Console.WriteLine($"Valid. Rising event.");
                            }
                        });
                        var fallingEventHandler = new PinChangeEventHandler((@object, a) =>
                        {
                            if (state == State.Up)
                            {
                                var elapsedBlinkTime = blinkWatch.ElapsedMilliseconds;
                                var validBlink = elapsedBlinkTime < blinkTime;
                                if (validBlink)
                                {
                                    if (previousOk)
                                    {
                                        var elapsed = powerWatch.ElapsedMilliseconds / 1000.0; // elapsed time in seconds
                                        if (elapsed != 0)
                                        {
                                            var power = Math.Round(3600.0f / elapsed);
                                            if (debug)
                                                Console.WriteLine($"Power consumption {power}");
                                            client.WriteVirtualPin(pin, power);
                                        }
                                    }
                                    state = State.Down;
                                    previousOk = true;
                                    powerWatch.Restart();
                                    if (debug)
                                        Console.WriteLine("Valid. Falling event.");
                                }
                                else
                                {
                                    state = State.None;
                                    previousOk = false;
                                    if (debug)
                                        Console.WriteLine($"Invalid. Falling event was too slow. Time {elapsedBlinkTime}");
                                }
                            }
                            else
                            {
                                if (debug)
                                    Console.WriteLine($"Invalid. Falling event. State {state}");
                                state = State.None;
                                previousOk = false;
                            }



                        });

                        var source = new TaskCompletionSource<bool>();
                        Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs eventArgs) =>
                        {
                            controller.UnregisterCallbackForPinValueChangedEvent(pin, risingEventHandler);
                            controller.UnregisterCallbackForPinValueChangedEvent(pin, fallingEventHandler);
                            source.SetResult(true);
                        };

                        controller.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Rising, risingEventHandler);
                        controller.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Falling, fallingEventHandler);
                        Console.WriteLine("Press CTRL+C to stop.");
                        source.Task.Wait();
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
