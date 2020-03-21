using System;
using System.Collections.Immutable;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tobii.Interaction;
using Tobii.Interaction.Framework;

namespace TobiiSocketServer
{

    struct State
    {
        public UserPresence userPresence { get; set; }
        public  String userProfileName { get; set; }

        public  EyeTrackingDeviceStatus eyeTrackingDeviceStatus { get; set; }

        public GazeTracking gazeTracking { get; set; }

        public Rectangle screenBounds { get; set; }

        public Size displaySize { get; set; }

    }

    public class SocketServer
    {
        private readonly WebSocketServer server;
        private readonly int port;
        private readonly string address;
        private readonly Host host;

        private ImmutableHashSet<IWebSocketConnection> allClients = ImmutableHashSet.Create<IWebSocketConnection>();
        private ImmutableHashSet<IWebSocketConnection> headPoseClients = ImmutableHashSet.Create<IWebSocketConnection>();
        private ImmutableHashSet<IWebSocketConnection> gazePointDataClients = ImmutableHashSet.Create<IWebSocketConnection>();
        private ImmutableHashSet<IWebSocketConnection> eyePositionClients = ImmutableHashSet.Create<IWebSocketConnection>();

        private HeadPoseStream headPoseStream;
        private GazePointDataStream gazePointDataStream;
        private EyePositionStream eyePositionStream;
        private State state = new State();


        public SocketServer(int port, string address, Host host)
        {
            this.port = port;
            this.address = address;
            this.server = new WebSocketServer($"ws://{this.address}:{this.port}");
            this.server.RestartAfterListenError = true;
            this.server.ListenerSocket.NoDelay = true;
            this.server.SupportedSubProtocols = new[] { "Tobii.Interaction" };


            this.host = host;
            this.host.States.CreateUserPresenceObserver().Changed += (e, userPresence) =>
            {
                this.state.userPresence = userPresence.Value;
                publishStateUpdateToAll();
            };

            this.host.States.CreateUserProfileNameObserver().Changed += (e, userProfileName) =>
            {
                this.state.userProfileName = userProfileName.Value;
                publishStateUpdateToAll();
            };

            this.host.States.CreateEyeTrackingDeviceStatusObserver().Changed += (e, eyeTrackingDeviceStatus) =>
            {
                this.state.eyeTrackingDeviceStatus = eyeTrackingDeviceStatus.Value;
                publishStateUpdateToAll();
            };

            this.host.States.CreateGazeTrackingObserver().Changed += (e, gazeTracking) =>
            {
                this.state.gazeTracking = gazeTracking.Value;
                publishStateUpdateToAll();

            };

            this.host.States.CreateScreenBoundsObserver().Changed += (e, screenBounds) =>
            {
                this.state.screenBounds = screenBounds.Value;
                publishStateUpdateToAll();
            };


            this.host.States.CreateDisplaySizeObserver().Changed += (e, displaySize) =>
            {
                this.state.displaySize = displaySize.Value;
                publishStateUpdateToAll();
            };


        }

        private void publishStateUpdateToAll()
        {
            foreach (var client in allClients)
            {
                publishStateUpdate(client);
            }
        }

        private void publishStateUpdate(IWebSocketConnection client)
        {
            client.Send(JsonConvert.SerializeObject(new { type = "state", data = this.state }, new StringEnumConverter()));
        }

        public void start()
        {

            this.server.Start(socket =>
            {

                socket.OnOpen = () =>
                {
                    FleckLog.Info($"{socket.ConnectionInfo.Id} (from {socket.ConnectionInfo.Host}) has connected. Negotiated protocol: {socket.ConnectionInfo.NegotiatedSubProtocol}");
                    allClients = allClients.Add(socket);
                    publishStateUpdate(socket);
                };

                socket.OnClose = () =>
                {
                    FleckLog.Info($"{socket.ConnectionInfo.Id} has disconnected");
                    allClients = allClients.Remove(socket);
                    headPoseClients = headPoseClients.Remove(socket);
                    gazePointDataClients = gazePointDataClients.Remove(socket);
                    eyePositionClients = eyePositionClients.Remove(socket);
                };

                socket.OnError = (err) =>
                {
                    FleckLog.Warn($"{socket.ConnectionInfo.Id} had error: {err}");
                };

                socket.OnMessage = message =>
                {
                    handleEyeNavMessage(socket, message);
                    FleckLog.Info($"{socket.ConnectionInfo.Id} sent message: {message}");
                };
            });
        }

        private void handleEyeNavMessage(IWebSocketConnection socket, String message)
        {

            switch (message)
            {
                case "state":
                    publishStateUpdate(socket);
                    break;
                case "startGazePoint":
                    if (!gazePointDataClients.Contains(socket))
                    {
                        gazePointDataClients = gazePointDataClients.Add(socket);
                    }
                    startGaze();
                    break;
                case "startEyePosition":
                    if (!eyePositionClients.Contains(socket))
                    {
                        eyePositionClients = eyePositionClients.Add(socket);
                    }
                    startEyeTracker();
                    break;
                case "startHeadPose":
                    if (!headPoseClients.Contains(socket))
                    {
                        headPoseClients = headPoseClients.Add(socket);
                    }
                    startHead();
                    break;

                case "stopGazePoint":
                    gazePointDataClients = gazePointDataClients.Remove(socket);
                    if (gazePointDataClients.IsEmpty)
                    {
                        this.gazePointDataStream.IsEnabled = false;
                    }
                    break;
                case "stopEyePosition":
                    eyePositionClients = eyePositionClients.Remove(socket);
                    if (eyePositionClients.IsEmpty)
                    {
                        this.eyePositionStream.IsEnabled = false;
                    }
                    break;
                case "stopHeadPose":
                    headPoseClients = headPoseClients.Remove(socket);
                    if (headPoseClients.IsEmpty)
                    {
                        this.headPoseStream.IsEnabled = false;
                    }
                    break;
            }
        }

        private void startGaze()
        {
            if (this.gazePointDataStream == null)
            {
                this.gazePointDataStream = this.host.Streams.CreateGazePointDataStream();
                this.gazePointDataStream.Next += (sender, e) =>
                {

                    foreach (IWebSocketConnection client in this.gazePointDataClients)
                    {
                        client.Send(JsonConvert.SerializeObject(new { type = "gazePoint", data = e.Data }, new StringEnumConverter()));
                    }
                };

            }
            else
            {
                this.gazePointDataStream.IsEnabled = true;
            }

        }

        private void startHead()
        {
            if (this.headPoseStream == null)
            {
                this.headPoseStream = this.host.Streams.CreateHeadPoseStream();
                this.headPoseStream.Next += (sender, e) =>
                {

                    foreach (IWebSocketConnection client in this.headPoseClients)
                    {
                        client.Send(JsonConvert.SerializeObject(new { type = "headPose", data = e.Data }, new StringEnumConverter()));
                    }
                };
            }
            else
            {
                this.headPoseStream.IsEnabled = true;
            }

        }

        private void startEyeTracker()
        {
            if (this.eyePositionStream == null)
            {
                this.eyePositionStream = this.host.Streams.CreateEyePositionStream();
                this.eyePositionStream.Next += (sender, e) =>
                {
                    foreach (IWebSocketConnection client in this.eyePositionClients)
                    {
                        client.Send(JsonConvert.SerializeObject(new { type = "eyePosition", data = e.Data }, new StringEnumConverter()));
                    }
                };
            }
            else
            {
                this.eyePositionStream.IsEnabled = true;
            }
        }

    }
}
