# Between a Rock and a Hard Place

Early Unity starting layout for the team pitch. The current scene tests the basic interaction: aim a turret, shoot the mountain for ore, try a damage upgrade, then shoot across the field when the mountain clears.

## Open it

Use **Unity 6000.6.3f1** with **Universal Render Pipeline (URP) 17.6.0**. Open `Assets/_Project/Scenes/Main.unity`, press Play, and click the Game view. The ground, two tank blocks, barrels, and mountain are saved in the scene and can be moved in the editor.

| Control | Action |
| --- | --- |
| Up / Down | Change barrel angle |
| Left / Right | Change shot power |
| Space | Fire and pass the turn after impact |
| U | Spend 20 ore on one test damage upgrade |
| R | Reload the scene |

Blue and Red share the keyboard and alternate turns. The small default Unity GUI is a temporary readout for testing.

The team's original terrain layout from commit `334e992` is preserved in `Assets/_Project/Scenes/TeamLayout.unity`, with its hills, mountain, ground, and colliders. Its sprite materials use URP. `Main.unity` is the playable mechanics test scene.

## What currently works

- Aiming and gravity-driven projectiles with collision detection.
- Turn switching after a hit or miss.
- Mountain hits give the firing side 20 ore. Four hits empty the test mountain.
- One upgrade per side changes direct-hit damage from 20 to 30.
- The empty mountain slides right and its collider switches off.
- After it clears, direct hits reduce the other tank's health.

The mountain movement is a placeholder for the pitched walking sequence. Combat currently tests health subtraction; there is no match-end flow. Army units, shields, weapon selection, terrain changes, art, animation, sound, menus, and game balancing still need work.

## Video highlights (about 1-2 minutes)

1. Show `Main` in Unity's Scene view. Point out the two faction placeholders, central resource mountain, and flat collision ground.
2. Enter Play. Change the angle with Up/Down and power with Left/Right, then fire with Space. Point out the curved projectile motion and turn change.
3. Hit the mountain and show the ore count increasing. The starting **40 degrees / 14 power** works from either side. Press U on a later turn once that side has 20 ore, and show damage changing from 20 to 30.
4. After four mountain hits, show the temporary slide-out and explain that this tests the transition into open combat.
5. Set about **40 degrees / 14.5 power**, fire across the field, and show the opponent's health dropping. Close by describing the next work: mountain walking animation, army units/equipment, and the rest of the combat loop.

Describe this as a working first pass at the pitch's core mechanics. You do not need to play a finished match or show a victory screen.

## Backup and local build

The previous complete prototype is preserved beside this repository in `complete prototype backup`, including its Unity source and Windows build. The active repository contains the smaller starting-layout version.

Use **Prototype > Build Windows Prototype** to rebuild `Builds/Windows/BetweenARock.exe`. The optional development-build argument `-prototypeSmokeTest` runs a short check of the current mechanics. Build outputs and local checks are ignored by Git.

## AI assistance

OpenAI Codex assisted with implementing this prototype, its documentation, and local checks. The game concept and pitch came from the team.
