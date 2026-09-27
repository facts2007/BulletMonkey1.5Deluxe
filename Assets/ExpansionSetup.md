# Scene flow and retro gameplay additions

Open `Assets/MainMenu.unity` to test the complete game. Build settings (and the
Windows profile, which uses the global scene list) include MainMenu, MainScene,
DeathScene and VictoryScene. Existing scene artwork and video are retained.

- Start Game → MainScene. Space/Enter/Escape skips the intro; a failed video no
  longer hides menu buttons forever.
- Player death → DeathScene. Heavenly drowning/ascension finishes its white fade
  and voice, then goes to the same DeathScene.
- Touching the boat → VictoryScene. Retry and Back to Menu use the actual scene
  names and restore time/cursor state, including when leaving a paused game.

## Unstuck

From any position, press **T** for the warning,
then **T again within 4 seconds** to confirm. Costs **10 HP**; requires more than
10 HP. No cost for a rejected request. The monkey ragdolls for 0.8 seconds, tilts sideways, then spins through a short
helicopter flight, and lands at the nearest accessible island's rescue center.
Locked islands are excluded so this cannot bypass mist gates. Controls and damage
are locked during the flight. Rescue-center children are under WaveManager/Island1–5,
plus a starting-island center. Adjust them if you move the map.

## Ranged enemy trick shot and boss

**Ranged Enemy** has Noscope Chance (10%), Noscope Damage Multiplier (2), jump
height, and duration controls. A selected shot performs a jumping 360 and fires
one stronger projectile. **Game Audio → Enemy Noscope** is the firing SFX;
**Enemy Noscope Hit** plays only when that projectile actually damages the player.

Island3's final Waves entry is a boss wave: one **GiantRoboMonkey** prefab, 2.5×
size and 3× normal ranged health. Existing normal waves remain before it. Edit
WaveArea's waves/boss prefab or the giant prefab to tune the fight.

## Kabu, buckets and shop music

Every wave start ignites Kabu and locks the shop, including countdowns. There is a
12-second intermission between waves. After the **last wave on an island**, Kabu
teleports to that island's **Shopspawn**, and one physical **WaterBucket** appears
at its **Rescue center**. Pick it up, press **E** near Kabu to extinguish him, then
**E again** to shop. Intermediate waves give no bucket. A new wave reignites him.
Any spare bucket can still be used during a break. The shop pauses gameplay.
Kabu uses the imported Fire and Idle clips plus flame particles. Assign the shop
soundtrack under **Game Audio → Shop Music**. It loops while shopping, follows the
Music volume slider, and resumes gameplay music when closed. Upgrade prices were
preserved from the existing scene.

## Retro presentation

**Retro Camera** renders at 400 vertical pixels with point filtering, 5-bit color
steps and light dithering. Adjust Vertical Resolution or disable that component
for a cleaner picture. Menus, health text, shops, pause/settings and new prompts
use a chunky font; buttons use dark panels and gold borders. Existing artwork is
preserved. Both gameplay and heavenly cameras have the filter.

## Verification

`IslandPolishChecks.Run()` in MainScene Play mode checks the updated rescue,
scaling, noscope impact audio, island rewards, fusion cutscene, final door and all
three scene themes. It uses temporary settings and changes scenes; stop Play
mode afterward. Report: `Temp/CombatChecks/island-polish-report.txt`.
`ExpansionChecks` also covers the earlier scene-flow routes. Do not rerun ExpansionSetup on
a hand-edited scene; it is a one-time migration helper.
## Menu, death and victory themes
Each of MainMenu, DeathScene and VictoryScene has a **Scene Music** object.
Assign that scene's MP3 to **Theme**, or directly to **Audio Source → AudioClip**. **Theme Volume** adjusts its relative loudness;
the saved Music slider applies too. Themes loop by default, and stop when leaving
the scene. Empty slots are silent. Video audio remains as authored.

