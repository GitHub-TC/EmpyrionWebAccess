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
        public Action LoopPing { get; set; }

        public ClientMessagePipe(string aPipeName)
        {
            PipeName = aPipeName;
            mCommThread = new Thread(CommunicationLoop);
            mCommThread.Start();
        }

        private void CommunicationLoop()
        {
            while (mCommThread.ThreadState != ThreadState.AbortRequested)
            {
                try
                {
                    ConnectClientPipe();

                    try
                    {
                        do
                        {
                            object Msg = GetNextMessage();

                            //log?.Invoke($"SendMessageExec {PipeName}[{mCommThread.ThreadState}] Connected:{mClientPipe?.IsConnected} Msg[{mSendCommands?.Count}]: {SendMessage}");

                            if (Msg != null && mClientPipe != null && mClientPipe.IsConnected)
                            {
                                SendMessageWithPipe(Msg);
                            }

                        } while (mClientPipe != null && mClientPipe.IsConnected);
                    }
                    catch (ThreadAbortException) { }
                    catch
                    {
                        if (mCommThread.ThreadState != ThreadState.AbortRequested) Thread.Sleep(10000);
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception Error)
                {
                    log?.Invoke($"MainError {PipeName}[{mCommThread.ThreadState}] Connected:{mClientPipe?.IsConnected} Reason: {Error.Message}");
                    if(mCommThread.ThreadState != ThreadState.AbortRequested) Thread.Sleep(10000);
                }
            }
        }

        private object GetNextMessage()
        {
            lock (mSendCommands)
            {
                if (mSendCommands.Count == 0) Monitor.Wait(mSendCommands, 1000);
                if (mSendCommands.Count > 0) return mSendCommands.Dequeue();
            }

            return null;
        }

        private void SendMessageWithPipe(object aMessage)
        {
            using (var MemBuffer = new MemoryStream())
            {
                try
                {
                    new BinaryFormatter().Serialize(MemBuffer, aMessage);
                    mClientPipe.Write(MemBuffer.ToArray(), 0, (int)MemBuffer.Length);
                }
                catch (SerializationException Error)
                {
                    log?.Invoke("Failed to serialize. Reason: " + Error.Message);
                    mClientPipe?.Close();
                }
                catch (Exception Error)
                {
                    log?.Invoke($"CommError {PipeName} Connected:{mClientPipe?.IsConnected} Reason: {Error.Message}");
                    mClientPipe?.Close();
                }
            }
        }

        private void ConnectClientPipe()
        {
            if (mClientPipe == null || !mClientPipe.IsConnected)
            {
                do
                {
                    try
                    {
                        log?.Invoke($"Try CommunicationLoop Connect {PipeName} Connected:{mClientPipe?.IsConnected}");
                        LoopPing?.Invoke();
                        if (mClientPipe != null) mClientPipe.Dispose();
                        mClientPipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.WriteThrough);
                        mClientPipe.Connect(1000);
                        log?.Invoke($"CommunicationLoop Connect {PipeName} Connected:{mClientPipe?.IsConnected}");
                    }
                    catch (IOException) { Thread.Sleep(1000); }
                    catch (TimeoutException) { Thread.Sleep(1000); }
                } while (mClientPipe == null || !mClientPipe.IsConnected);
            }
        }

        public void SendMessage(object aMessage)
        {
            try
            {
                //log?.Invoke($"SendMessage {PipeName}[{mCommThread.ThreadState}] Connected:{mClientPipe?.IsConnected} Msg[{mSendCommands?.Count}]: {aMessage}");

                if (mClientPipe == null || !mClientPipe.IsConnected)
                {
                    lock (mSendCommands) { Monitor.PulseAll(mSendCommands); }
                    return;
                }

                lock (mSendCommands)
                {
                    mSendCommands.Enqueue(aMessage);
                    Monitor.PulseAll(mSendCommands);
                }
            }
            catch (Exception Error)
            {
                log?.Invoke($"SendMessageError {PipeName}[{mCommThread.ThreadState}] Connected:{mClientPipe?.IsConnected} Reason: {Error.Message}");
            }
        }

        public void Close()
        {
            try
            {
                mCommThread?.Abort();
                mCommThread = null;
            }
            catch (Exception Error)
            {
                log?.Invoke($"CloseError:mCommThread {Error}");
            }
            try
            {
                lock (mSendCommands) Monitor.PulseAll(mSendCommands);
            }
            catch (Exception Error)
            {
                log?.Invoke($"CloseError:mSendCommands {Error}");
            }
            try
            {
                mClientPipe?.Close(); mClientPipe = null;
            }
            catch (Exception Error)
            {
                log?.Invoke($"CloseError:mClientPipe {Error}");
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}