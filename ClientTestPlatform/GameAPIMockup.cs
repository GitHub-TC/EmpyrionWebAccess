using Eleon.Modding;
using System;

namespace ClientTestPlatform
{
    internal class GameAPIMockup : ModGameAPI
    {
        public void Console_Write(string txt)
        {
            System.Console.WriteLine($"CW:{txt}");
        }

        public ulong Game_GetTickTime()
        {
            return (ulong)DateTime.Now.Ticks;
        }

        public bool Game_Request(CmdId reqId, ushort seqNr, object data)
        {
            Console.WriteLine($"Game_Request: CmdId:{reqId} seqNr:{seqNr} data:{data}");
            return true;
        }
    }
}