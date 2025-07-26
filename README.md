# Real Radio

> An immersive radio mod with positional audio that supports both live internet radio and YtDlp.

**Note: Requires alternate (mono) branch on Steam.**

## Features

-   Play remote audio streams (from urls). Supports all file formats that Media Foundation supports.
-   Youtube and [thousands of other sites](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md) are also supported using [yt-dlp](https://github.com/yt-dlp/yt-dlp/) with playback synced between all players. No manual setup is required for yt-dlp to work. All required files are downloaded automatically.
-   Create new radio stations in the game using an app on your in-game phone.
-   Fully compatible and constantly tested in multiplayer both as host and client.
-   A radio station can be played through multiple audio sources using the same source stream.
-   Placeable radio object that you can put in your properties. This uses custom placement logic which allows placing the radio object on almost any surface.
-   Placeable speakers can be connected to a radio object for improved audio quality and simulated stereo sound when set up correctly.
-   All vehicles have a built in radio. Players can change radio station by holding the reload button and selecting a station in the radial menu. Audio effects are changed depending on if the player is inside the vehicle or not. When inside the vehicle the audio is not spatialized and has no audio filters. When the player is not in the vehicle the audio is spatialized and a low pass filter is applied to simulate the audio being muffled from inside the car. NPC cars also have a chance to play a random radio station when driven.
-   Residential buildings where NPCs live have a chance (50% atm) to play music when the building has NPCs inside. The time when music starts and stops in a day is randomized at the end of each day. This logic only runs on the server but the playing radio station is synced to all clients.
-   A json file based api to add custom radio stations through modding. [see example below](#example-of-custom-radio-station)
-   Fetches currently playing song and displays it in various places. Supports both YtDlp type radio stations and (a limited amount of) internet radio stations out of the box. Can be extended to support more internet stations through modding (no documentation on this yet).

## Known issues
- Player onboarding is not very good. For now here is some tips on how the mod works:
    - You can buy the radio and wall speaker from Dan's hardware store.
    - You can hold E on some placeable objects to get more options. For example on the wall speaker you can adjust the mount's angle.
    - While in a vehicle you can hold the weapon reload button to select and play a radio station.
- Item selection in the radial menu can be very sensitive with small mouse movements.
- Joining a server for the first time plays global music. This might clash with any radios playing in the vicinity. This also applies when you're wanted by the police. The game music does fade out though if you're driving a vehicle with a radio station playing for example.
- The game will freeze temporarily if audio buffering isn't fast enough or if the audio stream dies for whatever reason (such as internet connection issues). Normally the game uses around 50-200 kb/s depending on how many internet streams are active at the same time. YtDlp songs are cached and are only downloaded once.

If you find any bugs or have any suggestions, please [open an issue](https://github.com/Skippeh/Schedule1RealRadioMod/issues/new) on GitHub or contact me (@skipcast) in the [unofficial Schedule I modding discord server](https://discord.gg/nKRBZNQjCq).

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

## Attributions
- Power button icon: https://www.freepik.com/icon/power-button_1073786
- Gear icon: https://www.freepik.com/icon/cog_799803
- Arrow icon: https://www.freepik.com/icon/arrow_15795547
- CD icon: https://www.flaticon.com/free-icons/cd
- Music icon: https://www.flaticon.com/free-icons/music
- Bass icon: https://www.flaticon.com/free-icons/bass
- Earphone icon: https://www.flaticon.com/free-icons/earphone
- Red polygonal background pattern: https://www.freepik.com/free-vector/coloured-polygonal-background-design_913229.htm
- Plus icon: https://www.flaticon.com/free-icons/plus
- Trash icon: https://www.flaticon.com/free-icons/trash-can
- Playlist add icon: https://www.flaticon.com/free-icons/add-to-playlist
- Download icon: https://www.flaticon.com/free-icons/download
- X icon: X by Royyan Wijaya from https://thenounproject.com/browse/icons/term/x/ (CC BY 3.0)
- Text icon: text by Gregor Cresnar https://thenounproject.com/browse/icons/term/text/ (CC BY 3.0)
- Rotate icon: Rotate by Chondon Backla from https://thenounproject.com/browse/icons/term/rotate/ (CC BY 3.0)
- Music icon (app icon): Music by Dwi Budiyanto from https://thenounproject.com/browse/icons/term/music/ (CC BY 3.0)
