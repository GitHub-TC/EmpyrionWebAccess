using AutoMapper;
using Eleon.Modding;
using EmpyrionModWebHost.Configuration;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionModWebHost.Services;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace EmpyrionModWebHost.Controllers
{

    public class GameplayManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class GameplayController : ControllerBase
    {
        public GameplayManager GameplayManager { get; }

        public GameplayController()
        {
            GameplayManager = Program.GetManager<GameplayManager>();
        }

        [HttpGet("GetAllPlayfieldNames")]
        public IActionResult GetAllPlayfieldNames()
        {
            return Ok(
                Directory.EnumerateDirectories(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Templates"))
                .Select(D => Path.GetFileName(D))
                );
        }

        public class WarpToData
        {
            public string Playfield { get; set; }
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float PosZ { get; set; }
            public float RotX { get; set; }
            public float RotY { get; set; }
            public float RotZ { get; set; }
        }

        [HttpPost("PlayerWarpTo/{aEntityId}")]
        public async System.Threading.Tasks.Task<IActionResult> PlayerWarpToAsync(int aEntityId, [FromBody]WarpToData aWarpToData)
        {
            var playerInfo = await TaskWait.For(5, GameplayManager.Request_Player_Info(new Id(aEntityId)));

            var pos = new PVector3(aWarpToData.PosX, aWarpToData.PosY, aWarpToData.PosZ);
            var rot = new PVector3(aWarpToData.RotX, aWarpToData.RotY, aWarpToData.RotZ);

            await TaskWait.For(5, playerInfo.playfield == aWarpToData.Playfield
                ? GameplayManager.Request_Entity_Teleport         (new IdPositionRotation(aEntityId, pos, rot))
                : GameplayManager.Request_Player_ChangePlayerfield(new IdPlayfieldPositionRotation(aEntityId, aWarpToData.Playfield, pos, rot))
            );

            return Ok();
        }
    }
}
