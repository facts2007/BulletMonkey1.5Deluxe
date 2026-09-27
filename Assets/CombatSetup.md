# Combat and audio setup

The configured scene is `Assets/MainScene.unity`. The existing Player, EnemyRanged,
and EnemyMelee gameplay prefabs also have the new visuals and animation wiring,
so wave-spawned enemies use them too. Existing movement, combat, shop upgrades,
health bars, and wave logic remain in their original scripts.

## Add your recordings

Select **Game Audio** in MainScene. In its **Game Audio**
component, drag AudioClips into:

- **Shoot**: regular player and ranged-enemy shots.
- **Enemy Explode**: ordinary enemy deaths.
- **Enemy Damaged**: surviving bullet/ordinary hits only. This independent slot starts empty.
- **Enemy Stomp Damaged**: nonlethal stomp impact (existing clang/thud clip).
- **Enemy Stomped**: the final, lethal stomp only.
- **Super Shoot**: a single exaggerated shot, played repeatedly during the stream.
- **Angry Monkey**: a loop played while the Cocey banan speed boost is active.
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

Adjust these values on **Player → Super Shoot Ability**. The circular F dial shows
charge progress and then the remaining super duration. Screen shake and recoil
strengths are on **Player → CamPivot → Main Camera → Shoot Camera Shake**.
Super shooting also pushes the actual aiming camera upward and sideways, with a
capped 18-degree climb, varied kick strength, and gradual recovery. Each super shot
also has independent spread within a 3.5-degree cone. Tune **Super Spread Angle**
on the Gun component. Charging smoothly narrows the camera field of view by up to
5 degrees, then restores it when charging ends or is cancelled.

## Cocey banan

The **Cocey banan Variant** prefab is a working pickup and enemies have a **5%**
drop chance. It gives **2× movement and animation speed for 10 seconds**, a subtle
red screen tint, gentle continuous camera shake, and the Angry Monkey loop. Another pickup refreshes the timer;
it does not stack the multiplier. Pausing freezes the timer and pauses the loop.
Everything returns to normal when the timer expires or the player is disabled.

Change duration, multiplier, and tint opacity on **Player → Speed Boost Ability**.
Change the drop chance on the **Enemy** component of the enemy prefabs. Both rare
drops roll independently, so an enemy can drop both bananas.

## Animation and stomp

- BulletMonkey: idle, walk, normal shooting, super shooting, and empty-ammo pose.
- RoboMonkey: BulletMonkey's idle/walk/shoot bone motion remapped onto its matching
  skeleton; no super ability. The robot FBX's original named clips are static.
- MiniDroid: Imp Walk while moving; holds its pose when stationary.
- A lethal stomp flattens MiniDroid for two seconds. Nonlethal stomps retain the
  original damage behavior; regular bullet deaths still explode.

Controllers and looping clip copies are in `Assets/Animation/Combat`. Imported
source clips are unchanged. Previous player/robot visuals and placeholder meshes
are disabled so their original configuration remains available.

## Verification tools

`Tools → BulletMonkey → Run combat checks (Play mode)` exercises pickup,
charging, rapid fire, stomp deaths, physical landing, settings, Cocey duration,
animation speed, sound routing, recoil, and RoboMonkey state changes. It temporarily
disables movement/enemies and spawns test objects: **stop Play mode after running**
to discard the test setup. Results are written to `Temp/CombatChecks/report.txt`.

The editor-only setup command can rewire MainScene and the three gameplay prefabs.
Do not rerun it after hand-tuning prefab placement or drop chances unless you want
to restore the setup defaults. It keeps existing generated animation controllers.


## Five combat islands

Under **WaveManager**, select **Island1** through **Island5**. These correspond to
**Combat islands 1–5**, not the starting island. The starting island has no wave.

- **Waves**: change the array Size to set the number of waves; set each entry's
  **Enemy Count**. Defaults are two waves per island: 4/6, 6/8, 8/10, 10/12, 12/14.
- **Countdown Seconds**: countdown before each wave (default 3 seconds).
- **Enemy Prefabs**: random mix; both RoboMonkey and MiniDroid are assigned.
- **Spawn Interval** and **Spawn Points**: spawn pacing and positions.
- **Entry trigger** child: a thin box across the entrance. Move/resize its Box
  Collider for layout changes. Only a player entering the unlocked area starts it.
- **Mist wall** child: solid exit gate. All waves must finish before it disappears.
  Clearing an island unlocks the next entry trigger; it does not auto-start it.

Counts belong to spawned enemies on that island. Unrelated kills do not count,
spawning must finish before completion, and cleared islands cannot restart.
The previous starting-island blocker is disabled and kept for reference.

`IslandWaveChecks.Run()` in Play mode exercises all five entry triggers, countdowns,
two-wave progression, gates, unrelated kills, and separate audio routes. Stop Play
mode afterwards to discard its temporary settings. Report: `Temp/CombatChecks/island-report.txt`.

## Drowning / heavenly box

Select **Drowning — heavenly box** in MainScene and assign your MP3 to **Heavenly
Voice** on the **Drowning Sequence** component. The subtitle reads: “You have
drowned my child... May you rest in peace..” You can edit that message, the minimum
reading time (6 seconds), ragdoll delay (1.2 seconds), white fade (7 seconds), and
voice gain in the Inspector. A longer voice clip automatically extends the scene.
The voice follows SFX volume; gameplay music is silent during the heavenly scene.

The water contact volume covers the whole ocean. On contact, controls and the
normal HUD disappear, the player is teleported to the separate room, and a copy of
their visual becomes a 15-body physics ragdoll. The original imported model is
unchanged. After the voice/message and white fade, normal PlayerHealth.Die runs.
After the ascent and whiteout, the game now loads **DeathScene**, with Retry and Back to Main Menu.
The new scene flow is documented in `Assets/ExpansionSetup.md`.
The short cinematic locks pause input; both exit choices are available afterward.

The room, camera, lights, and water volume are children of **Drowning — heavenly
box**. The room lives outside the playable map and starts disabled. Existing wall
placements and island wave settings are untouched by this setup.

`DrowningChecks.Run()` in Play mode tests ocean coverage, real water contact,
teleportation, control lockout, duplicate suppression, ragdoll stability, voice
length, whiteout, death, and the ending UI. Stop Play mode to discard test settings.
Report: `Temp/CombatChecks/drowning-report.txt`.

Choir: assign an MP3/AudioClip to **Heavenly Choir** on Drowning Sequence. It has
its own **Choir Volume** and **Loop Choir** controls, follows the Music slider,
and fades out with the whiteout. It plays alongside Heavenly Voice; it does not
extend the sequence. The monkey gets a glowing halo when ragdoll starts. **Halo
Height** and **Halo Radius** adjust the ring, which follows the head and stays upright.

Ascension: the monkey ragdolls for **2 seconds**, then rises **5 metres** by its
torso over the **7-second white fade**. Limbs remain under physics and the halo
follows the head. Adjust Ragdoll Rest Seconds and Ascent Height on Drowning
Sequence. A longer voice recording finishes over white before the death choices
appear; it does not delay the start of the lift.
