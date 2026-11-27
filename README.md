# Between a Rock and a Hard Place

Two kingdoms share a battlefield with a resource mountain in the way. Players take turns aiming artillery, mining ore and buying an upgrade. When its ore runs out, the mountain wakes up, gets to its feet and runs away. The kingdoms then fire across the opening until one wins.

## Open and play

Use **Unity 6000.6.3f1** with the packages pinned in `Packages/manifest.json` (URP 17.6.0). This is the Unity 6.6 patch installed on this workstation. Open `Assets/_Project/Scenes/Main.unity`, press Play, and click the Game view.

| Control | Action |
| --- | --- |
| Up / Down | Change barrel angle |
| Left / Right | Change shot power |
| Space | Fire; the turn passes after the shot resolves |
| U | Spend 20 ore on the existing damage upgrade |
| R | Restart the match |

Blue and Red share one keyboard. Shooting and purchases pause while a shell is active or the mountain is leaving. Both sides keep their ore, loot and upgrades when mining becomes combat.

For a quick demonstration with the current layout, **55 degrees / roughly 6.25 power for Blue or 6 for Red** reaches the mountain. After it leaves, **55 degrees / roughly 12.25 power** reaches the opposing kingdom. These are starting examples; aiming is part of the game.

## Current foundation

- The team's scene, houses, terrain, loot and reward popup are merged from `Scene-and-Mountain-Update`. The script work was already merged into `main`.
- The mountain uses saved, editable Unity Tilemaps made from the existing Cainos terrain sprites. Stone, the solid backing layer, ore seams, faces and feet are separate maps.
- Its escape has a wake-up, crouch, rise, forward lean and accelerating alternating steps. Movement starts from its placed position and clears the camera. Colliders switch off on depletion; combat resumes once after the run.
- The kingdoms keep the team's houses, with matching tilemap ramparts, banners and visible cannons on the actual aiming pivots. Collision follows the mountain, rampart and terrain tiles.
- Seven original sound effects cover firing, stone impacts, kingdom impacts, wake-up, footsteps, upgrades and victory. Adjust `Game Audio` volume on `GameManager` in the Inspector.
- A match ends when a kingdom reaches zero health. The HUD announces the winner and R restarts. A dedicated game-over screen remains to be made.

The old mountain animation clip/controller remain as reference assets; the playable mountain uses `MountainEscape`. The original team layout is preserved in `TeamLayout.unity`.

## Editing art and sounds

Expand `Mountain > Mountain Art` in Main. Paint `Mountain Stone` for the surface and `Mountain Core` behind it to avoid gaps between rock sprites. Keep the root at the feet and the body, faces and legs under this hierarchy. `MountainEscape` has their Inspector references.

Each turret has `Kingdom Tilemaps`; `Cannon Tilemaps` is under its barrel pivot. The new tile assets in `Assets/_Project/Art/Tiles` reference the existing sprites.

**Prototype > Rebuild Mountain and Kingdom Tilemaps** replaces these authored mountain, rampart, banner and cannon groups and saves Main. It also replaces manual paint changes inside those groups, so use it deliberately. Other scene content is preserved.

Effects are imported WAV assets in `Assets/_Project/Audio/Sfx`. Regenerate them with `python scripts/generate_sfx.py`, then use **Prototype > Connect Basic Sound Effects**. The generator uses only the Python standard library and no downloaded samples.

## Build and verification

Use **Prototype > Build Windows Prototype**. The development build is `Builds/Windows/BetweenARock.exe`; distribute the entire Windows folder, including its Data folder and Unity DLLs. Builds and verification output are ignored by Git.

The opt-in `-foundationSmokeTest` argument runs real projectiles through mining, an upgrade purchase, the escape, kingdom damage, a win and restart. It also checks animation continuity, forward travel, foot height and the one-turn handoff. It exits with code 0 on success and writes `Logs/FoundationCheck/result.txt` and screenshots. Normal play does not install the check.

For an Editor check, launch Unity with `-batchmode -projectPath "<repository path>" -executeMethod FoundationValidation.Run -foundationSmokeTest -logFile "Logs/foundation-play.log"`. Omit `-quit`: the check exits Unity when finished.

## Final showcase guide: aim for 4 minutes

The supplied assignment requires **3-5 minutes**, due **Thursday, December 3, 2026 at 10:00 a.m.** Record a standalone build or the eventual Itch build; visibly recording Unity Editor Play mode loses points.

| Time | Focus | Evidence to record |
| --- | --- | --- |
| 0:00-0:25 | Hook and premise | Show both kingdoms and the sleeping mountain. Explain the mine, upgrade, then fight loop. Include the start menu once made. |
| 0:25-1:15 | Artillery and local multiplayer | Change angle/power, show a projectile arc, impact and the turn passing to the other kingdom. |
| 1:15-2:00 | Resource economy | Show ore increasing, a loot popup and buying an upgrade. Explain its effect using the HUD. Show a talent-tree choice here if it is added. |
| 2:00-2:40 | Triggered cutscene | Show the final mining hit, wake-up, feet emerging and run. Let the footsteps be heard. Show play resuming after it clears. |
| 2:40-3:30 | Kingdom combat and feedback | Land hits, show health falling and demonstrate the two chosen core juicy interactions. |
| 3:30-4:00 | Completion and credits | Show a win/lose screen, restart and credits. Finish with a strong gameplay moment if time permits. |

Demonstrate each feature working. The rubric explicitly calls for gameplay, two core juicy interactions, three selected optional features, menus and a win/lose state. Keep results readable even with a trailer-like edit. Submit an MP4 or public/unlisted YouTube link, and stay below five minutes.

## Before the final recording

1. **Required screens:** make a functioning start screen, credits screen and dedicated win/lose screen. Winner text alone does not demonstrate the full screen requirement.
2. **Juice next:** the escape and sound foundation are in place. Add readable mining-hit and kingdom-hit feedback, such as ore/health UI response plus impact animation. Large explosion effects are deferred. Decide which two interactions you will demonstrate against the rubric.
3. **Choose three optional features:** local competitive multiplayer and the triggered mountain cutscene have working demonstrations. Health/damage exists; strengthen its health UI feedback before presenting that as the third feature. WebGL hosted on Itch is another option once built and uploaded. Random loot rolls alone should not be presented as procedural level generation.
4. **Economy follow-up:** a small ore-funded talent tree could give upgrades more depth. Start with clear cannon-damage versus fortification choices, with visible costs/effects. It is a proposed next step. Extra weapons, wind and army units remain brainstorms.
5. **Release:** upload a tested build to Itch.io or Game Jolt with its name, screenshot, icon and credits. Verify the imported art packs' creator/license details and credit all teammates. The supplied team requirement calls for each member to contribute both content and code.

## Attribution

The concept and pitch came from the team. Terrain sprites and supporting asset scripts come from the imported **Cainos / Pixel Art Platformer - Village Props** pack; its original source headers are preserved. Existing house sprites came from the team's **Medieval - Free** import; confirm and retain its original creator/license details for publication. New tilemap compositions reuse these installed assets.

The new sounds are original procedural effects from `scripts/generate_sfx.py`. OpenAI Codex assisted with implementation, tilemap compositions, animation, sound integration, verification and documentation. The existing reward-popup source also records Claude assistance. No third-party gameplay code was copied for this pass.
