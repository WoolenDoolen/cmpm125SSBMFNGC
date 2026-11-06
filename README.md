# Between a Rock and a Hard Place

A new Unity game project. Game design and gameplay have not been implemented yet.

## Editor version

Use **Unity 6.6, 6000.6.8f1** (revision `237b6593792b`), the latest production release verified on November 5, 2026. The exact version is tracked in `ProjectSettings/ProjectVersion.txt`.

- [Release notes and downloads](https://unity.com/releases/editor/whats-new/6000.6.8f1)
- [Install this version with Unity Hub](unityhub://6000.6.8f1/237b6593792b)

## Open the project

1. Install Unity Hub and the editor version above. Add build support for your target platform when needed.
2. Clone this repository.
3. In Unity Hub, choose **Projects > Add > Add project from disk** and select the repository root, which contains `Assets`, `Packages`, and `ProjectSettings`.
4. Open `Assets/_Project/Scenes/Main.unity` and enter Play mode. It contains only the default camera and directional light; there is no gameplay yet.

The starter uses Unity's built-in render pipeline. No game framework, third-party assets, or rendering packages have been added.

## WSL workflow

Use WSL for Git and shell commands; run the Windows Unity Editor against the same checkout:

```bash
git clone https://github.com/WoolenDoolen/cmpm125SSBMFNGC.git
cd cmpm125SSBMFNGC
./scripts/open-unity.sh
```

The launcher reads the pinned editor version and looks in the standard Unity Hub install directories. For a custom installation, set `UNITY_EDITOR_PATH` to the executable's WSL or Linux path. Arguments after the script are forwarded to Unity.

For example, run an import check without opening an editor window:

```bash
mkdir -p Logs
./scripts/open-unity.sh -batchmode -nographics -quit -logFile -
```

## Project layout

| Path | Purpose |
| --- | --- |
| `Assets/_Project/Scenes` | Game scenes; `Main` is enabled in the build scene list. |
| `Assets/_Project/Scripts` | Future gameplay code. |
| `Assets/_Project/Art` | Future art assets. |
| `Assets/_Project/Audio` | Future music and sound effects. |
| `Assets/_Project/Materials` | Future materials. |
| `Assets/_Project/Prefabs` | Future reusable game objects. |
| `Assets/_Project/Settings` | Future game configuration assets. |
| `Packages` | Unity package manifest and dependency lockfile. |
| `ProjectSettings` | Shared editor, player, and build settings. |

Commit assets with their `.meta` files to preserve references. Text serialization and visible metadata are enabled. Generated caches, local editor settings, IDE files, logs, and build outputs are ignored by Git.

The previous Phaser/Vite application and its GitHub Pages workflow have been removed. Earlier work remains available in Git history.
