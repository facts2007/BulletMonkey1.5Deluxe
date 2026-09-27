# Island polish and Imptron introduction

Scene changes are saved in MainScene. Your wall, shop spawn, rescue-center and
boss-island markers keep their placed positions.

## Inspector controls

- **WaveManager / Island1–5 → Wave Area**: Island Number, Health Increase Per
  Island (0.15), Damage Increase Per Island (0.10), Shop Spawn, Rescue Center and
  Water Bucket Prefab. Scaling adds a fraction of base stats, not compound growth.
  Island 5 therefore has 1.6× base HP and 1.4× base damage. The island 3 giant
  retains triple base HP and its shot interval is now 1 second instead of 2.
- **Game Audio → Enemy Noscope Hit**: your special impact sound. Enemy Noscope
  remains the launch sound. Ordinary hits do not use the new clip.
- **WaveManager / Boss island → Boss Fusion Encounter**: Fusion Roar (SFX volume)
  and Boss Theme (looping music volume) are separate optional clip slots. Assign
  your recordings here. Empty slots stay silent.
- **Imp fusion cutscene camera**: its saved position/rotation frames the incoming
  imps. Pan Seconds and Reveal Seconds control the camera transitions and reveal.
- **Boss Prefab**: currently ImptronPlaceholder, a 3x enlarged passive cylinder with 750 HP
  and a health bar. Replace this reference with your future Imptron prefab.
- **Imp Spawns / Merge Point / Final Mist Door**: references to your markers and
  the extra mist wall near the boat, now named **Last mist door**.

Island 5 completion only unlocks the encounter. Walking into the box trigger on
Boss island starts the camera cutscene. 120 running visual imps from five angles converge, shrink
into the merge point and feed a growing fog cloud. The cylinder appears, the roar
plays, and the boss theme starts. The camera pans back and restores controls.
Player damage and drowning are blocked during this short introduction. Defeating
the cylinder opens Last mist door and stops the theme. The cylinder has no attacks.

Each completed island produces one physical bucket at its rescue center, and
Kabu moves to its Shopspawn. Intermediate waves no longer award buckets. Bucket
pickup adds one charge; E at burning Kabu spends it to extinguish him.

## Performance changes

Ranged enemies update path destinations at most four times per second. Active-wave
checks use a registry instead of scene searches every frame. Wave counters update
only when their value changes. Settled enemy health bars stop writing transforms.
Spawn fog shares eight bounded particle emitters, and fusion imps have no combat,
healthbar, damage or loot scripts. Mist walls use larger soft clouds with a
120-particle cap. The pixel-presenting camera no longer renders shadows or requests
depth/color copies. Main directional shadow-map resolution is 2048 instead of 4096.

These reduce specific costs; actual FPS depends on your machine and scene view.

## Verification

Run `IslandPolishChecks.Run()` only in MainScene Play mode. It changes temporary
runtime values and visits menu scenes. Stop Play afterward to restore the scene.
The report is written to `Temp/CombatChecks/island-polish-report.txt`.

Island 3: assign the **Miniboss Theme** clip on its Wave Area component. This pauses
normal gameplay music during its boss wave and resumes it at the same playback
position on defeat. Pause/resume also preserves the boss track. Empty slots leave
normal music alone. Retro cameras now render at 400 vertical pixels (40% smaller
pixels than the previous 240-line view). The small [T] unstuck? hint sits above ammo.

FusionExpansionChecks.Run() verifies the 120-imp sequence, growing fog, 3x boss,
400-line rendering and miniboss music continuity in MainScene Play mode.


## Merge sound, pixel health and cheers

Boss island / Boss Fusion Encounter now has **Merging Sound** and **Merging Gain**.
The clip loops while imps arrive and fuse, then stops before Fusion Roar and the
boss theme. It uses SFX volume and stops if the encounter is disabled.

Game Audio has **Player Cheer**, **Multi Kill Window** (2 seconds),
**Cheer Cooldown** (4 seconds), and **Cheer Gain**. Two lethal player hits inside
the window trigger one cheer. Another cheer needs a fresh pair and an expired
cooldown; a playing clip is never restarted. Fusion runners do not count as kills.

Player health is drawn with a pixel heart, HP label, black outline and segmented
red fill. Enemy and boss health show only the outlined segmented bar. The original
health/damage scripts still control values; PixelHealthGraphic reads their current
and maximum HP, so upgrades and damage stay synchronized.

## Loot, cheer image, stored Cokey bananas and fourth upgrades

- Cokey pickups add to **Speed Boost Ability / Stored Bananas**. Press R to consume
  one for the usual ten-second double-speed effect. Using another refreshes the
  timer. The right-hand white-banana icon and count stay visible while inactive.
  Pausing, shopping, rescue and the fusion cinematic cannot spend a banana.
- **Game Audio / Cheer Popup** points to your Monkey cheer image. It pops beside
  the player, wobbles and fades with the multi-kill cheer. You can change its
  duration on Cheer Popup. The image can still appear if no audio clip is assigned.
- Enemy scrap, ammo and both special bananas scatter in an arc, tumble, bounce,
  and settle into hovering/spinning pickups. They cannot be collected mid-flight.
  **Parts.prefab** uses your ScrapMetal model. For the future ammo model assign
  **Ammo.prefab / Loot Motion / Visual Prefab**. The existing visual is the fallback.
  Spawn height, scatter radius, flight duration and spin speed are editable.
- ShopManager now exposes Level 4 cost and increase fields for health, fire rate
  and max ammo. Defaults: 200 scrap each, +35 HP / +5 fire rate / +25 ammo.
  Your first three prices and effects are unchanged. Purchases stop at four tiers.
- Shop, pause, settings and menu buttons use matching pixel frames. Existing
  videos and menu artwork remain. Shop rows show current tier and scrap price.

`LootShopChecks.Run()` in MainScene Play mode verifies storage/use, all fourth
upgrades, animated drops, single-credit collection, the cheer popup and shop opening.
It uses temporary values; stop Play afterward. Report: Temp/CombatChecks/loot-shop-report.txt.
