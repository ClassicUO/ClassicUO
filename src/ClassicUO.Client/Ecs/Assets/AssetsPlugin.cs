using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;

readonly struct AssetsPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var loadAssetsFn = LoadAssets;
        scheduler.OnStartup(loadAssetsFn, ThreadingMode.Single);
    }

    unsafe void LoadAssets
    (
        State<GameState> state,
        Res<GraphicsDevice> device,
        Res<GameContext> gameCtx,
        Res<Settings> settings,
        SchedulerState schedState
    )
    {
        Fonts.Initialize(device);

        var fileManager = new UOFileManager(gameCtx.Value.ClientVersion, settings.Value.UltimaOnlineDirectory);
        fileManager.Load(false, settings.Value.Language);

        schedState.AddResource(fileManager);
        schedState.AddResource(new AssetsServer(fileManager, device));
        schedState.AddResource(new UltimaBatcher2D(device));
        schedState.AddResource(new MultiCache(fileManager.Multis));

        gameCtx.Value.Protocol = ClientFlags.CF_T2A;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_200)
            gameCtx.Value.Protocol |= ClientFlags.CF_RE;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_300)
            gameCtx.Value.Protocol |= ClientFlags.CF_TD;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_308)
            gameCtx.Value.Protocol |= ClientFlags.CF_LBR;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_308Z)
            gameCtx.Value.Protocol |= ClientFlags.CF_AOS;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_405A)
            gameCtx.Value.Protocol |= ClientFlags.CF_SE;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_60144)
            gameCtx.Value.Protocol |= ClientFlags.CF_SA;


        const int TEXTURE_WIDTH = 512;
        const int TEXTURE_HEIGHT = 1024;
        const int LIGHTS_TEXTURE_WIDTH = 32;
        const int LIGHTS_TEXTURE_HEIGHT = 63;

        var hueSamplers = new Texture2D[2];
        hueSamplers[0] = new Texture2D(device, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        hueSamplers[1] = new Texture2D(device, LIGHTS_TEXTURE_WIDTH, LIGHTS_TEXTURE_HEIGHT);

        var buffer = new uint[Math.Max(
            LIGHTS_TEXTURE_WIDTH * LIGHTS_TEXTURE_HEIGHT,
            TEXTURE_WIDTH * TEXTURE_HEIGHT
        )];

        fileManager.Hues.CreateShaderColors(buffer);
        fixed (uint* ptr = buffer)
        {
            hueSamplers[0].SetDataPointerEXT(
                0,
                null,
                (IntPtr)ptr,
                TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint)
            );

            LightColors.CreateLightTextures(buffer, LIGHTS_TEXTURE_HEIGHT);
            hueSamplers[1].SetDataPointerEXT(
                0,
                null,
                (IntPtr)ptr,
                LIGHTS_TEXTURE_WIDTH * LIGHTS_TEXTURE_HEIGHT * sizeof(uint)
            );
        }

        device.Value.Textures[1] = hueSamplers[0];
        device.Value.Textures[2] = hueSamplers[1];

        state.Set(GameState.LoginScreen);
    }
}
