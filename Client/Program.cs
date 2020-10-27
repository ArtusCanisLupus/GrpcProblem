namespace Client
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Base;
    using Grpc.Core;
    using Helloworld;

    internal class Program
    {
        private const int Port = 40506;

        private static void Main()
        {
            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "ServerIsRunning");

            var processJob = new ProcessJob();
            var process = new Process { StartInfo = { FileName = "Server" } };
            process.Start();
            processJob.AddProcess(process.Handle);

            if (!waitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                Console.WriteLine("Server is not running...");
            }
            else
            {
                var channel = new Channel("127.0.0.1", Port, ChannelCredentials.Insecure);
                var client = new Greeter.GreeterClient(channel);

                var sw = Stopwatch.StartNew();
                Console.WriteLine(client.SayHello(new HelloRequest { Name = "World" }));
                Console.WriteLine($"It took {sw.Elapsed}...");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
