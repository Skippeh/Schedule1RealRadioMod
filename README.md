# Real Radio

> An immersive radio mod for Schedule I. Supports both BepInEx and MelonLoader.

**Note: Requires alternate (mono) branch on Steam.**

## Existing features

-   Play remote audio streams (from urls). Supports all file formats that Media Foundation supports.
-   Youtube and [thousands of other sites](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md) are also supported using [yt-dlp](https://github.com/yt-dlp/yt-dlp/) with playback synced between all players. No manual setup is required for yt-dlp to work. All required files are downloaded automatically.
-   Create new radio stations in the game using an app on your in-game phone.
-   Fully compatible and constantly tested in multiplayer both as host and client.
-   A radio station can be played through multiple audio sources using the same source stream.
-   Placeable radio object that you can put in your properties. This uses custom placement logic which allows placing the radio object on almost any surface.
-   Placeable speakers can be connected to a radio object for improved audio quality and simulated stereo sound when set up correctly.
-   All vehicles have a built in radio. Players can change radio station by holding the reload button and selecting a station in the radial menu. Audio effects are changed depending on if the player is inside the vehicle or not. When inside the vehicle the audio is not spatialized and has no audio filters. When the player is not in the vehicle the audio is spatialized and a low pass filter is applied to simulate the audio being muffled from inside the car. NPC cars also have a chance to play a random radio station when driven.
-   Residential buildings where NPCs live have a chance (50% atm) to play music when the building has NPCs inside. The time when music starts and stops in a day is randomized at the end of each day. This logic only runs on the server but the playing radio station is synced to all clients
-   A json file based api to add custom radio stations through modding. [see example below](#example-of-custom-radio-station)
-   Fetches currently playing song and displays it in various places (only shown when selecting radio station in vehicles at the moment). Supports all radio stations types including (a limited amount of) internet radio stations out of the box. Can be extended to support more internet stations through modding (no documentation on this yet).

### Example of custom radio station

```json5
{
    "id": "gtasa-radiox",
    "name": "Radio X",
    "type": "YtDlp", // YtDlp or InternetRadio

    // 'urls' is used for YtDlp type
    // otherwise 'url' should be used if type is InternetRadio.
    "urls": [
        "https://www.youtube.com/watch?v=xczq2sbRoHg",
        "https://www.youtube.com/watch?v=sqLAvhBwoQg",
        "https://www.youtube.com/watch?v=xkZFkL8D4qM",
        "https://www.youtube.com/watch?v=0wrzPb-d6tY",
        "https://www.youtube.com/watch?v=4rDb12j6kGs",
        "https://www.youtube.com/watch?v=-yLg4_1iqfo",
        "https://www.youtube.com/watch?v=Ig0z_nspfRo",
        "https://www.youtube.com/watch?v=wBmv2NYbYCA",
        "https://www.youtube.com/watch?v=0OGUCeHcNNY",
        "https://www.youtube.com/watch?v=iuJwtB1HrCw",
        "https://www.youtube.com/watch?v=Wte5IiOXpYk",
        "https://www.youtube.com/watch?v=pTdyyt42U0Q",
        "https://www.youtube.com/watch?v=S7MDxta8srw",
        "https://www.youtube.com/watch?v=SG8UCbG0WIA",
        "https://www.youtube.com/watch?v=noQrdNaL6Mo",
    ],
}
```

If there's a png file with the same name as the json file it will be used at the station's icon. [There are more fields that can be found here](https://github.com/Skippeh/Schedule1RealRadioMod/blob/main/RealRadio/Components/API/Data/RadioStation.cs).

Radio stations go into the following folder: `Schedule I\RealRadio\Stations\`. Subfolders within this folder can also be used.

## Compiling

When compiling the project, you have to specify which mod loader you're compiling for. You can do this by setting the `BEPINEX` or `MELONLOADER` environment variables or by providing a property value in the `dotnet build` command.

### BepInEx

```
dotnet build /p:BEPINEX=1
```

### MelonLoader

```
dotnet build /p:MELONLOADER=1
```

You also need to create a `DevVars.props` file in the root directory. There is an example file included in the repository that you can use.
