# Final placeholder pass

## Gameplay
- Player shots reach 300 units (previously 100); bullet lifetime is 6 seconds (previously 2).
- Shop ammo defaults to 50 rounds for 25 scrap. Full ammo costs nothing; purchases cap at maximum ammo. Adjust ShopManager in MainScene.
- All four tiers of health, fire rate, and maximum ammo unlock the secret dungeon button. Repeatable ammo purchases do not count.
- Tutorial opens once per new game; X or Escape closes it. Pause > Tutorial reopens it. Death retries do not repeat it.
- Wave intermissions show a numeric countdown.
- Empty ammo still uses the normal idle animation. BulletMonkey.controller has Jump and CokeyRun replacement slots using existing idle/walk clips until final animations arrive.
- Cokey doubles movement and jump/fall simulation speed while preserving jump height. Shooting cadence and shooting animation speed remain unchanged.

## DungeonLevel
The scene is included in build settings. It contains a roofless arena, a large evil Kabu cylinder, a key, an opening door, and an outside victory boat.

Select the object with DungeonEncounter to configure boss health (2000), attack damage, windup/recovery times, indicator sizes, optional Boss Theme, and Heavenly Door Music. The existing heaven-choir clip is assigned to the door sequence.

The EvilKabuPlaceholder controller contains Idle, RockThrow, MegaPunch, BellyBonk, and Defeated placeholder states. Replace the placeholder visual and clips when final artwork is ready; retain the boss health root and encounter references.

Rock throw marks a landing circle, mega punch marks a rectangle, and belly bonk marks a landing circle. Defeating the boss drops a key. Collecting it allows the door camera sequence and opens the path to the boat, which loads VictoryScene.

Purchased maximum health, ammo capacity, firing rate, scrap, and stored Cokey bananas carry into the dungeon. Entry refills health/ammo. Death retries the dungeon with the entry loadout.

## Verification
33 Play Mode integration checks passed, covering tutorial/pause, range, ammo, animations, boost timing, countdown, twelve-upgrade unlock, all attacks, key/door cinematic, dungeon retry, and victory routing.

The editor-only PlaceholderPassChecks runner starts from MainScene in Play Mode. Its report is written to Temp/CombatChecks/placeholder-pass-report.txt. PlaceholderPassSetup is a setup helper, not a runtime dependency; do not rerun scene creation over the finished scene.

## Boss and scrap update
Scrap HUD is right-aligned inside the screen with room for six digits. Moneycube grants 500 scrap once on touch. Evil Kabu now has 10,000 HP; Run, Jump, and ThrowImp animation states are available. After each three normal attacks, he throws one active melee imp; it guarantees 75 ammo on death (DungeonEncounter.impAmmoDrop).
