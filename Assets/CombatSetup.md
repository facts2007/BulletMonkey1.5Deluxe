# Combat and audio setup

The configured scene is `Assets/MainScene.unity`. The existing Player, EnemyRanged,
and EnemyMelee gameplay prefabs also have the new visuals and animation wiring,
so wave-spawned enemies use them too. Existing movement, combat, shop upgrades,
health bars, and wave logic remain in their original scripts.

## Add your recordings

Select **Game Audio - assign sound clips here** in MainScene. In its **Game Audio**
component, drag AudioClips into:

- **Shoot**: regular player and ranged-enemy shots.
- **Enemy Explode**: ordinary enemy deaths.
- **Enemy Stomped**: stomp impacts and lethal stomp squashes.
- **Super Shoot**: a single exaggerated shot, played repeatedly during the stream.
- **Main Game Music**: looping gameplay soundtrack.
- **Pause Menu Music**: looping pause/settings soundtrack.

Empty slots are silent. Super shots fall back to the regular Shoot clip if the
Super Shoot slot is empty. Shoot Gain and Super Shoot Gain let you balance the two.
The two volume sliders are under **Escape → Settings** and persist between runs.
Gameplay music pauses in the pause menu and resumes from the same position.

## Super banana

Enemies have a **1%** super banana drop chance. Change **Super Banana Drop Chance**
on the Enemy component of the enemy prefabs to tune it (0.01 = 1%).

One banana can be stored at a time. The HUD shows **Hold F to charge**. Hold for
**1.5 seconds**, then the player shoots continuously at **40 shots/second** for
**5 seconds**, using the super animation and stronger screen shake. The stream
does not consume ordinary ammo. Releasing F before the charge finishes keeps the
banana. Once charged, the full stream runs without needing to keep F held.

Adjust these values on **Player → Super Shoot Ability**. Screen shake strengths
are on **Player → CamPivot → Main Camera → Shoot Camera Shake**.

## Animation and stomp

- BulletMonkey: idle, walk, normal shooting, super shooting, and empty-ammo pose.
- RoboMonkey: its own matching idle, walk, and shooting clips; no super ability.
- MiniDroid: Imp Walk while moving; holds its pose when stationary.
- A lethal stomp flattens MiniDroid for two seconds. Nonlethal stomps retain the
  original damage behavior; regular bullet deaths still explode.

Controllers and looping clip copies are in `Assets/Animation/Combat`. Imported
source clips are unchanged. Previous player/robot visuals and placeholder meshes
are disabled so their original configuration remains available.

## Verification tools

`Tools → BulletMonkey → Run combat checks (Play mode)` exercises pickup,
charging, rapid fire, stomp deaths, physical landing, and settings. It temporarily
disables movement/enemies and spawns test objects: **stop Play mode after running**
to discard the test setup. Results are written to `Temp/CombatChecks/report.txt`.

The editor-only setup command can rewire MainScene and the three gameplay prefabs.
Do not rerun it after hand-tuning prefab placement or drop chances unless you want
to restore the setup defaults. It keeps existing generated animation controllers.
