## Synopsis

**Tobii Web Socket Server** is a Web Socket Server that wraps the Tobii Interaction SDK and transmits the data through web sockets. It is a fork of the archived and discontinued https://github.com/sradevski/Tobii-EyeX-Web-Socket-Server.

Some new features:
- Reporting the state of the tracker
- Supports HeadPose and EyePosition API
- Supports multiple clients, subscribing to different APIs

## Usage

You can download an executable if you don't want to compile the project by yourself. To do that, simply download a published Release to a location of your choice.
In order to run the server, open the terminal, navigate to the location where the Release folder is located, and run `TobiiSocketServer.exe`, which will start the server on the default port (8887). You can optionally pass a custom port number like `TobiiSocketServer.exe 8886`. Note that the Tobii EyeX Server must be running before using the Tobii Web Socket Server.

If you wish, you could also build the project by yourself. The project is built using Visual Studio 2019 Community.

### WS-API

From your web application call:
```javascript
const ws = new WebSocket("ws://localhost:8886", ["Tobii.Interaction"]);
ws.onmessage = (m) => console.log(JSON.parse(m.data));

ws.send('state');
// yields as message: 
msg = {"type":"state","data":{"userPresence":"Present","userProfileName":"rezreal","eyeTrackingDeviceStatus":"Tracking","gazeTracking":"GazeTracked","screenBounds":{"X":0,"Y":0,"Width":1920,"Height":1080},"displaySize":{"Height":286.875,"Width":509.99999999999994}}}
// updates to one of these properties will yield a message of the same structure.

// subscribes to GazePoint events
ws.send('startGazePoint');
// unsubscribes to GazePoint events
// ws.send('stopGazePoint');

// subscribes to EyePosition events
ws.send('startEyePosition');
// unsubscribes to EyePosition events
// ws.send('stopEyePosition');

// subscribes to HeadPose events
ws.send('startHeadPose');
// unsubscribes to HeadPose events
// ws.send('stopHeadPose');

```
All other events are directly mapped from their C# API equivalent. See https://developer.tobii.com/consumer-eye-trackers-interaction-library-api-reference/ for reference (GazePointData, HeadPoseData, EyePositionData).


## License

This project is under the [MIT Licence](LICENSE)
