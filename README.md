# quest64-crowd-control
This is a Crowd Control effect pack for the game Quest 64. Crowd Control is a system which allows viewers on twitch.tv to purchase effects that alter the streamer's game in some way.
## Setup
Several steps are required to setup your stream to use this effect pack.
1. First, install Crowd Control using the instructions [here](https://crowdcontrol.live/setup). Both the twitch extension and desktop app are needed.
2. Download the effect pack "Quest64.ccpak" [here](https://github.com/StrangeQwarq/quest64-crowd-control/raw/main/Quest64.ccpak).
3. You need to acquire a ROM of the US version of Quest 64. ROMs can't be provided here.
4. When you have a ROM, you'll need to install the Bizhawk emulator. In the Crowd Control desktop app, open the ROM Setup screen and click "Install" next to "Bizhawk Path". This will install the appropriate version of the emulator.
5. Go to the Game Selection screen. Choose the option "Crowd Control Custom Game Pak (Beta)" from the list of games. A button labeled "Load Pak" will appear. Click it and choose the file Quest64.ccpak.
6. Below the "Load Pak" button, click "Select ROM" and select your Quest 64 ROM. If the "Select ROM" button isn't visible, you may need to select a different ccpak file (a renamed copy of Quest64.ccpak should be enough) first, then select the original file again. This seems to be a common issue with the Crowd Control app.
7. (Optional) Click the "Edit Menu" button to check all of the effects included in the pack. Each effect has a default price, but the "Adjust All %" button can be used to reduce the cost of all effect by the entered percentage. The cost, name and description of individual effects can also be modified manually. If individal edits don't seem to work, try updating the desktop app.
8. Click "Start Session" when you're ready to begin a Crowd Control stream. After the session starts, a "Launch Bizhawk" button should appear. This will start the emulator and automatically load the appropriate ROM.
9. Enjoy some chaos in Quest 64.

## Effects
The available effects are listed [here](https://github.com/StrangeQwarq/quest64-crowd-control/blob/main/effect%20descriptions.txt). Effect titles are listed on one line, with their default cost and description on the following line. If an effect seems to have no cost, it means it's not an actual effect itself, but just a label used to group multiple effects together.

## Stability
While there are currently no known crashing issues, Quest 64 is particularly sensitive to this sort of memory manipulation. I recommend making a save state periodically (every 20 minutes or so) just in case.

## Thanks
A huge thanks to everyone in the Quest 64 community discord server for helping get this pack to its current state. Special thanks to Mallos31 and Landmine36 for tons of info on memory locations and how the game works, and also BingChang for testing and pointing out potential crashes.