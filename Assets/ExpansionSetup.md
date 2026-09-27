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

After trying to move against an obstacle for 1.5 seconds, press **T** for the warning,
then **T again within 4 seconds** to confirm. Costs **10 HP**; requires more than
10 HP. No cost for a rejected request. The monkey ragdolls, spins through a short
helicopter flight, and lands at the nearest accessible island's rescue center.
Locked islands are excluded so this cannot bypass mist gates. Controls and damage
are locked during the flight. Rescue-center children are under WaveManager/Island1–5,
plus a starting-island center. Adjust them if you move the map.

## Ranged enemy trick shot and boss

**Ranged Enemy** has Noscope Chance (10%), Noscope Damage Multiplier (2), jump
height, and duration controls. A selected shot performs a jumping 360 and fires
one stronger projectile. **Game Audio → Enemy Noscope** accepts its separate SFX.

Island3's final Waves entry is a boss wave: one **GiantRoboMonkey** prefab, 2.5×
size and 3× normal ranged health. Existing normal waves remain before it. Edit
WaveArea's waves/boss prefab or the giant prefab to tune the fight.

## Kabu, buckets and shop music

Every wave start ignites Kabu and locks the shop, including countdowns. Each cleared
wave awards **one bucket charge** to Player Bucket Inventory. There is a **12-second
intermission** between waves (WaveArea → Intermission Seconds), followed by the
normal countdown. During a break, press **E** near Kabu to spend a bucket and put
out his fire, then **E again** to open the shop. The shop pauses the intermission.
A new wave reignites him. After all island waves, the shop remains available until
a later island starts. Buckets currently use inventory charges; a physical bucket
prefab is not required yet.

Kabu uses the imported Fire and Idle clips plus flame particles. Assign the shop
soundtrack under **Game Audio → Shop Music**. It loops while shopping, follows the
Music volume slider, and resumes gameplay music when closed. Upgrade prices were
preserved from the existing scene.

## Retro presentation

**Retro Camera** renders at 240 vertical pixels with point filtering, 5-bit color
steps and light dithering. Adjust Vertical Resolution or disable that component
for a cleaner picture. Menus, health text, shops, pause/settings and new prompts
use a chunky font; buttons use dark panels and gold borders. Existing artwork is
preserved. Both gameplay and heavenly cameras have the filter.

## Verification

`ExpansionChecks.Run()` in Play mode exercises rescue confirmation/cost/landing,
trick-shot damage and animation, boss spawning, shop lock/buckets, pixel rendering,
and real menu/death/boat/victory/ascension transitions. It uses temporary settings
and changes scenes: stop Play mode afterward. Report is in
`Temp/CombatChecks/expansion-report.txt`. Do not rerun ExpansionSetup.Configure
on a hand-edited scene; it is a one-time migration helper.

## Menu, death and victory themes
Each of MainMenu, DeathScene and VictoryScene has **Scene Music - assign theme here**.
Assign that scene's MP3 to **Theme**. **Theme Volume** adjusts its relative loudness;
the saved Music slider applies too. Themes loop by default, and stop when leaving
the scene. Empty slots are silent. Video audio remains as authored.
