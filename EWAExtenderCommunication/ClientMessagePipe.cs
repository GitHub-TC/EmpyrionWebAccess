#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace EWAExtenderCommunication
{
    public class ClientMessagePipe : IDisposable
    {
        Thread mCommThread;
        Queue<object> mSendCommands = new Queue<object>();

        public Action<string>? Log { get; set; }
        public string PipeName { get; }
        public bool Exit { get; private set; }

        public ClientMessagePipe(string aPipeName)
        {
            PipeName = aPipeName;
            mCommThread = new Thread(CommunicationLoop) { IsBackground = true };
            mCommThread.Start();
        }

        private void CommunicationLoop()
        {
            var shownErrors = new List<string>();
            while (!Exit)
            {
                try
                {
                    Log?.Invoke($"Try CommunicationLoop Connect {PipeName}");
                    using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    pipe.Connect(5000);

                    Log?.Invoke($"CommunicationLoop Connected {PipeName}");

                    new Thread(() =>
                    {
                        while (!Exit)
                        {
                            Thread.Sleep(500);
                            SendMessage(new ClientHostComData() { Command = ClientHostCommand.Ping });
                        }
                    }) { IsBackground = true }.Start();

                    var pipeError = false;
                    do
                    {
                        pipeError = SendMessageViaPipe(pipe, WaitForNextMessage());
                    } while (!pipeError && !Exit && pipe.IsConnected);
                }
                catch (ThreadAbortException) { return; }
                catch (TimeoutException) { /* retry */ }
                catch (Exception error)
                {
                    if (!shownErrors.Contains(error.Message))
                    {
                        shownErrors.Add(error.Message);
                        Log?.Invoke($"MainError {PipeName} Reason: {error}");
                    }
                    if (!Exit) Thread.Sleep(1000);
                    lock (mSendCommands) mSendCommands.Clear();
                }
            }
        }

        private object? WaitForNextMessage()
        {
            lock (mSendCommands)
            {
                if (mSendCommands.Count == 0) Monitor.Wait(mSendCommands);
                if (mSendCommands.Count > 0) return mSendCommands.Dequeue();
            }
            return null;
        }

        private bool SendMessageViaPipe(NamedPipeClientStream pipe, object? message)
        {
            if (message == null || Exit) return false;
            try
            {
                PipeProtocol.WriteMessage(pipe, message);
                return false;
            }
            catch (Exception error)
            {
                Log?.Invoke($"CommError {PipeName} Reason: {error.Message}");
                return true;
            }
        }

        public void SendMessage(object aMessage)
        {
            lock (mSendCommands)
            {
                mSendCommands.Enqueue(aMessage);
                Monitor.PulseAll(mSendCommands);
            }
        }

        public void Close()
        {
            try
            {
                Exit = true;
                lock (mSendCommands) Monitor.PulseAll(mSendCommands);
            }
            catch (Exception error)
            {
                Log?.Invoke($"CloseError: {error}");
            }
        }

        public void Dispose() => Close();
    }
}
