using OnixRuntime.Api;
using OnixRuntime.Api.Rendering;
using OnixRuntime.Plugin;
using RakNetAgain;
using OnixRuntime.Api.Maths;
using OnixRuntime.Api.Inputs;

namespace RakNetOnix;
public class RakNetOnix : OnixPluginBase {
    public static RakNetOnix Instance { get; private set; } = null!;
    public static RakNetOnixConfig Config { get; private set; } = null!;

    public RakServer? server;
    public Task? listenerTask;
    private bool running = false;
    private bool hovering = false;

    public RakNetOnix(OnixPluginInitInfo initInfo) : base(initInfo) {
        Instance = this;
        // If you can clean up what the plugin leaves behind manually, please do not unload the plugin when disabling.
        base.DisablingShouldUnloadPlugin = false;
    }

    protected override void OnLoaded() {
        Console.WriteLine($"Plugin {CurrentPluginManifest.Name} loaded!");
        Config = new RakNetOnixConfig(PluginDisplayModule);
        Onix.Client.Notify(title: "rak: Loaded", type: ClientNotificationType.Banner, duration: .1f);
    }

    protected override void OnEnabled() {
        server = new RakServer(25565) {
            GameVersion = "1.21.82",
            ProtocolVersion = 800,
            MaxConnections = 20,
        };

        // listenerTask = server!.StartListener();

        // Onix.Client.NotifyBanner(title: "Server started");

        Onix.Events.Rendering.RenderScreen += (renderer, b, c, d, e) => {
            var mousepos = Onix.Gui.MousePosition;

            renderer.FillCircle(new Vec2(20, 40), running ? ColorF.Green : ColorF.Red, 10);
            if (
                10 < mousepos.X && mousepos.X < 30 &&
                30 < mousepos.Y && mousepos.Y < 50
            ) {
                renderer.DrawCircle(new Vec2(20, 40), ColorF.Aqua, 10, 3);
                hovering = true;
            } else {
                hovering = false;
            }
        };

        Onix.Events.Input.Input += (key, isdown) => {
            if (!isdown) return false;

            if (key.Value == InputKey.Type.LMB) {
                if (!hovering) return false;

                if (running) {
                    Onix.Client.Notify(title: "Stopping", type: ClientNotificationType.Tray, duration: .1f);
                    running = false;
                    server.StopListener();
                    listenerTask = null;
                } else {
                    Onix.Client.Notify(title: "Starting", type: ClientNotificationType.Tray, duration: .1f);
                    running = true;
                    listenerTask = server.StartListener();
                }

                return true;
            }

            return false;
        };
    }

    protected override void OnDisabled() {
        // server?.Close();
        server = null;
    }

    protected override void OnUnloaded() {
        // Ensure every task or thread is stopped when this function returns.
        // You can give them base.PluginEjectionCancellationToken which will be cancelled when this function returns. 
        Console.WriteLine($"Plugin {CurrentPluginManifest.Name} unloaded!");
    }
}

/*
Unhandled SocketException occured in RakNetOnix
Only one usage of each socket address (protocol/network address/port) is normally permitted.
at Sustem.Net.50ckets.50cket.Llpdate5tatusfifter50cketErrorfindThrowExceptionC50cketError
error, aoolean disconnectünrailure, String callerName>ä
*/
