using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace EWAExtenderCommunication
{
    public class ClientMessagePipe : IDisposable
    {
        Thread mCommThread;
        Queue<object> mSendCommands = new Queue<object>();
        NamedPipeClientStream mClientPipe;
        public Action<string> log { get; set; }

        public string PipeName { get; }
        public bool Exit { get; private set; }

        public ClientMessagePipe(string aPipeName)
        {
            PipeName = aPipeName;
            mCommThread = new Thread(CommunicationLoop);
            mCommThread.Start();
        }

        public bool Connected { get => mClientPipe != null && mClientPipe.IsConnected; }

        private void CommunicationLoop()
        {
            while (!Exit)
            {
                try
                {
                    log?.Invoke($"Try CommunicationLoop Connect {PipeName} Connected:{mClientPipe?.IsConnected}");
                    using (mClientPipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous))
                    {
                        ConnectOrExit();
                        do
                        {
                            SendMessageViaPipe(WaitForNextMessage());
                        } while (mClientPipe != null && mClientPipe.IsConnected && !Exit);
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception Error)
                {
                    log?.Invoke($"MainError {PipeName} Connected:{mClientPipe?.IsConnected} Reason: {Error.Message}");
                    if (!Exit) Thread.Sleep(10000);
                }
            }
        }

        private object WaitForNextMessage()
        {
            lock (mSendCommands)
            {
                if (mSendCommands.Count == 0) Monitor.Wait(mSendCommands);
                if (mSendCommands.Count > 0) return mSendCommands.Dequeue();
            }

            return null;
        }

        private void SendMessageViaPipe(object SendMessage)
        {
            if (SendMessage == null || !mClientPipe.IsConnected || Exit) return;

            using (var MemBuffer = new MemoryStream())
            {
                try
                {
                    new BinaryFormatter().Serialize(MemBuffer, SendMessage);

                    var Size = MemBuffer.Length;
                    mClientPipe.WriteByte((byte)(Size & 0xff));
                    mClientPipe.WriteByte((byte)(Size >> 8 & 0xff));
                    mClientPipe.WriteByte((byte)(Size >> 16 & 0xff));
                    mClientPipe.WriteByte((byte)(Size >> 24 & 0xff));

                    mClientPipe.Write(MemBuffer.ToArray(), 0, (int)MemBuffer.Length);
                    mClientPipe.Flush();
                }
                catch (SerializationException Error)
                {
                    log?.Invoke("Failed to serialize. Reason: " + Error.Message);
                }
                catch (Exception Error)
                {
                    log?.Invoke($"CommError {PipeName} Connected:{mClientPipe?.IsConnected} Reason: {Error.Message}");
                }
            }
        }

        private void ConnectOrExit()
        {
            do
            {
                try { mClientPipe.Connect(1000); }
                catch (IOException) { Thread.Sleep(1000); }
                catch (TimeoutException) { Thread.Sleep(1000); }
            } while ((mClientPipe == null || !mClientPipe.IsConnected) && !Exit);
        }

        public void SendMessage(object aMessage)
        {
            if (mClientPipe == null || !mClientPipe.IsConnected) return;
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
            catch (Exception Error)
            {
                log?.Invoke($"CloseError: {Error}");
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}