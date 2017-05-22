﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Transport.Serialization;

namespace Cake.Scripting.Transport.Tcp.Server
{
    public class ScriptGenerationServer : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ScriptGenerationServer(IScriptGenerationService service, int port)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            RunAsync(service, port, _cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private static async Task RunAsync(IScriptGenerationService service, int port, CancellationToken cancellationToken)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, port);

                using (var reader = new BinaryReader(client.GetStream()))
                using (var writer = new BinaryWriter(client.GetStream()))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Request
                        var fileChange = FileChangeSerializer.Deserialize(reader);
                        var cakeScript = service.Generate(fileChange);

                        // Response
                        CakeScriptSerializer.Serialize(writer, cakeScript);
                        writer.Flush();
                    }
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel(false);
        }
    }
}