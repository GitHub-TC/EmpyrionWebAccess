using System;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using SharedMemory;

namespace EWAExtenderCommunication
{
    public class ClientMessagePipe : IDisposable
    {
        Thread mCommThread;
        Queue<object> mSendCommands = new Queue<object>();
        CircularBuffer mClient;

        public Action<string> log { get; set; }

        public string PipeName { get; }
        public bool Exit { get; private set; }

        public ClientMessagePipe(string aPipeName)
        {
            PipeName = aPipeName;
            mCommThread = new Thread(CommunicationLoop);
            mCommThread.Start();
        }

        private void CommunicationLoop()
        {
            var ShownErrors = new List<string>();
            while (!Exit)
            {
                try
                {
                    log?.Invoke($"Try CommunicationLoop Connect {PipeName}");
                    using (mClient = new CircularBuffer(PipeName, 4, 10 * 1024 * 1024))
                    {
                        if (!Exit)
                        {
                            new Thread(() =>
                            {
                                while (!Exit)
                                {
                                    Thread.Sleep(500);
                                    SendMessage(new ClientHostComData() { Command = ClientHostCommand.Ping });
                                }
                            }).Start();

                            var PipeError = false;
                            do
                            {
                                PipeError = SendMessageViaPipe(WaitForNextMessage());
                            } while (!PipeError && !Exit);
                        }
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception Error)
                {
                    if (!ShownErrors.Contains(Error.Message))
                    {
                        ShownErrors.Add(Error.Message);
                        log?.Invoke($"MainError {PipeName} Reason: {Error}");
                    }

                    if (!Exit) Thread.Sleep(1000);
                    lock (mSendCommands) mSendCommands.Clear();
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

        private bool SendMessageViaPipe(object SendMessage)
        {
            if (SendMessage == null || Exit) return false;

            try
            {
                using (var memStream = new MemoryStream())
                {
                    new BinaryFormatter().Serialize(memStream, SendMessage);
                    mClient.Write(memStream.ToArray());
                }
            }
            catch (SerializationException Error)
            {
                log?.Invoke("Failed to serialize. Reason: " + Error.Message);
            }
            catch (Exception Error)
            {
                log?.Invoke($"CommError {PipeName} Reason: {Error.Message}");
                return true;
            }

            return false;
        }

        public void SendMessage(object aMessage)
        {
            if (mClient == null) return;
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