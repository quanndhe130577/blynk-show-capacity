using Blynk.Tester.Service;
using Blynk.Virtual;
using PowerArgs;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Blynk.Tester
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class Options
    {
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

    }

    class Program
    {
        private static System.Timers.Timer gTimer;
        static Client client;
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
            client = new Client(url, authorization);

            client.Connect();
            var tcs = new TaskCompletionSource<bool>();
            client.OnAuthorized += v => tcs.SetResult(v);
            client.Login();
            var authorized = tcs.Task.Result;
            if (authorized)
            {

                Console.WriteLine("Hardware client is authorized with given token");

                /*
                client.OnVirtualWritePin += (id, value) =>
                {
                   
                    Console.WriteLine($"Write Pin {id} has value {value}");
                };

                client.OnVirtualMultiWritePin += (id, value) =>
                {
                    Console.WriteLine($"Write Pin {id} has value " + string.Join(", ", value));
                };

                client.OnVirtualReadPin += id =>
                {
                    Console.WriteLine($"Read Pin {id}");
                };
                */
                var closeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                Console.CancelKeyPress += (o, e) =>
                {
                    closeTcs.SetResult(true);
                };

                SetTimerGeneration();

                // Schechuler for energy
                StartSchedulerEnergy();


                Console.WriteLine("Test server is active. Press CTRL+C to stop.");

                closeTcs.Task.Wait();
                gTimer.Stop();
                gTimer.Dispose();


                Console.WriteLine("Stopping Test server.");

            }
            else
            {
                Console.WriteLine("Cannot authorize client with given token.");
            }
            client.Disconnect();

        }

        private static void SetTimerGeneration()
        {
            // Create a timer with a two second interval.
            gTimer = new System.Timers.Timer(5000);
            // Hook up the Elapsed event for the timer. 
            gTimer.Elapsed += OnTimedGenerationEvent;
            gTimer.AutoReset = true;
            gTimer.Enabled = true;
        }

        private static void OnTimedGenerationEvent(Object source, ElapsedEventArgs e)
        {
            DataGenerationManage dGM = new DataGenerationManage(client);

            dGM.SetData();
        }
        public static async void StartSchedulerEnergy()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            IJobDetail job = JobBuilder.Create<DataEnergyManage>().Build();
            job.JobDataMap["client"] = client;

            ITrigger trigger = TriggerBuilder.Create()
                .StartNow()
                .WithDailyTimeIntervalSchedule
                  (s =>
                     s.WithIntervalInHours(24)
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(9, 40))
                  )
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
