namespace Server
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Base;
    using Grpc.Core;
    using Helloworld;

    internal class GreeterImpl : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
            => Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
    }

    internal class Program
    {
        private const int Port = 40506;

        public static void Main()
        {
            // Setting to false will remove the connection delay
            const bool RedirectStdoutAndStderr = true;

            var processJob = new ProcessJob();
            StartChildren(processJob, RedirectStdoutAndStderr);

            var server = new Server
            {
                Services = { Greeter.BindService(new GreeterImpl()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "ServerIsRunning");
            waitHandle.Set();

            Console.WriteLine("Greeter server listening on port " + Port);

            Thread.Sleep(-1);
        }

        private static void StartChildren(ProcessJob processJob, bool redirectStdoutAndStderr)
        {
            for (int i = 0; i < 10; i++)
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = "Child",
                        RedirectStandardOutput = redirectStdoutAndStderr,
                        RedirectStandardError = redirectStdoutAndStderr,
                        CreateNoWindow = true
                    }
                };
                process.OutputDataReceived += ProcessOnOutputDataReceived;
                process.ErrorDataReceived += ProcessOnOutputDataReceived;
                process.Start();
                if (redirectStdoutAndStderr)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                processJob?.AddProcess(process.Handle);
            }
        }

        private static void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
        }
    }
}
