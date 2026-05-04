#nullable enable
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;

namespace EWAExtenderCommunication
{
    public class ServerMessagePipe : IDisposable
    {
        Thread mServerCommThread;
        DateTime? mLastPing;

        public Action<string>? Log { get; set; }
        public string PipeName { get; }
        public Action<object>? Callback { get; set; }
        public bool Exit { get; private set; }
        public bool Connected { get; private set; }

        public ServerMessagePipe(string aPipeName)
        {
            PipeName = aPipeName;
            mServerCommThread = new Thread(ServerCommunicationLoop) { IsBackground = true };
            mServerCommThread.Start();
        }

        private void ServerCommunicationLoop()
        {
            var shownErrors = new List<string>();
            while (!Exit)
            {
                try
                {
                    ExecServerCommunication();
                }
                catch (ThreadAbortException) { return; }
                catch (Exception error)
                {
                    if (!shownErrors.Contains(error.Message))
                    {
                        shownErrors.Add(error.Message);
                        Log?.Invoke($"Failed ExecServerCommunication. {PipeName} Reason: " + error);
                    }
                    if (!Exit) Thread.Sleep(1000);
                }
            }
        }

        private void ExecServerCommunication()
        {
            mLastPing = null;
            using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            Log?.Invoke($"ServerPipe: {PipeName} waiting for connection");
            server.WaitForConnection();

            Log?.Invoke($"ServerPipe: {PipeName} connected");
            if (Exit) return;

            Connected = true;
            mLastPing = DateTime.Now;

            try
            {
                while (!Exit && server.IsConnected)
                {
                    var message = ReadNextMessage(server);
                    if (message != null)
                    {
                        mLastPing = DateTime.Now;
                        if (message is ClientHostComData ping && ping.Command == ClientHostCommand.Ping)
                            continue;
                        Callback?.Invoke(message);
                    }

                    if (mLastPing.HasValue && (DateTime.Now - mLastPing.Value).TotalSeconds > 5) break;
                }
            }
            finally
            {
                Connected = false;
            }
        }

        private object? ReadNextMessage(NamedPipeServerStream server)
        {
            try
            {
                return PipeProtocol.ReadMessage(server);
            }
            catch (Exception error)
            {
                Log?.Invoke("Failed ReadNextMessage. Reason: " + error.Message);
                return null;
            }
        }

        public void Close()
        {
            try { Exit = true; }
            catch (Exception error) { Log?.Invoke($"CloseError {error}"); }
        }

        public void Dispose()
        {
            Log?.Invoke($"Dispose: {PipeName}");
            Close();
        }
    }
}
