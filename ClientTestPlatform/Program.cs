using Eleon.Modding;
using EWAExtenderCommunication;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ClientTestPlatform
{
    class Program
    {

        static void Main(string[] args)
        {
            Dictionary<Type, Action<object>> InServerMessageHandler = new Dictionary<Type, Action<object>> {
                { typeof(EmpyrionGameEventData), M => HandleGameEvent               ((EmpyrionGameEventData)M) },
                { typeof(ClientHostComData    ), M => HandleClientHostCommunication ((ClientHostComData)M) }
            };
            var Client = new EWAModClient.EmpyrionModClient();
            var gameAPIMockup = new GameAPIMockup();

            Client.Game_Start(gameAPIMockup);

            while(Client.InServer == null) Thread.Sleep(1000);

            Client.InServer.Callback = Msg => { if (InServerMessageHandler.TryGetValue(Msg.GetType(), out Action<object> Handler)) Handler(Msg); };

            while (Console.ReadKey().KeyChar == ' ')
            {
                Client.Game_Event(Eleon.Modding.CmdId.Event_ChatMessage, 1,
                    new ChatInfo() { playerId=42, type=1, msg = "abc" });

                Client.Game_Event(Eleon.Modding.CmdId.Event_Player_Info, 1, 
                    new PlayerInfo() { playerName = "abc" });
                Client.Game_Event(Eleon.Modding.CmdId.Event_AlliancesAll, 1, 
                    new Eleon.Modding.AlliancesTable() { alliances = new HashSet<int>(new[] { 1, 3, 4 } )  });

                var GSL = new Eleon.Modding.GlobalStructureList();
                var GS = GSL.globalStructures = new Dictionary<string, List<GlobalStructureInfo>>();
                GS.Add("a", new List<GlobalStructureInfo>(
                            new[] { new GlobalStructureInfo() { id = 1, name = "S1" } }
                            ));
                Client.Game_Event(Eleon.Modding.CmdId.Event_GlobalStructure_List, 1, GSL);
                Client.Game_Update();
            }

            Client.Game_Exit();
            Console.WriteLine("finish...");
        }

        private static void HandleClientHostCommunication(ClientHostComData m)
        {
            Console.WriteLine($"ClientHostComData:{m.Command}");
        }

        private static void HandleGameEvent(EmpyrionGameEventData m)
        {
            var obj = m.GetEmpyrionObject();
            Console.WriteLine($"EmpyrionGameEventData:{m.eventId}#{m.seqNr} => {obj}");
        }
    }
}
