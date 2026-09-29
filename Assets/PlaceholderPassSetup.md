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

## Final models, animations, and boss balance
- Player CokeyRun, Jump, and Cheer use the new bulletmonkey(anims) clips. Copied clips normalize the exported rig scale and bone translation units to the existing player skeleton; keep the original model import intact.
- Q plays a manual cheer with sound and the monkey picture. Island 3 miniboss, Imptron, and Evil Kabu victories trigger a separate Cheer Camera shot, then return to the gameplay camera. Cheer Camera is assigned on PlayerCheer in MainScene and DungeonLevel. The player already uses bulletmonekyAvatar.
- Evil Kabu uses the new model and Run, punch, throw, and jump+stomp clips. Attacks independently roll 33% rock, 34% punch, and 33% stomp. One bonus-ammo imp is thrown after each six normal attacks.
- Boss health remains 10,000. Windup .85s, recovery .8s; rock/punch/stomp damage is 30/40/45. The existing bellyDamage and bellyRadius Inspector fields now configure the stomp.
- DungeonEncounter has a Boulder Prefab slot; when empty, the existing sphere rock is used. Imp Prefab is separate.
- HealingBananaPickup uses the normal banaan model, spins/bounces with loot, heals 20 HP without exceeding maximum, and drops from enemies with 15% probability. Full-health players leave it available for later.
- 18 focused integration checks passed. Final visual inspection verified the corrected cheer pose, monkey image, dedicated camera, and return to gameplay.

## Final boss merge and boat fix
- BossController from the classmate's Boss test scene remains the shared attack controller. Its warning shapes, randomized timing, target calculation, ground queries, and independent rain loop are reused by the fusion boss and the test scene.
- Giant Slam: 30 impact damage, then three walking supply imps launch from the impact. Each guarantees 30 ammo on death.
- Imp Scattershot: six spinning imp projectiles, each marked with a landing circle, explode for 20 damage within radius 3. They do not become walking enemies.
- Forward Push: line warning, 5 damage, short collision-aware backward shove.
- Passive rain: 3-5 explosive imps per burst, randomized 7-12 second delay, launch from above the boss, separate from the main attack timer. All settings remain editable on BossController.
- Island 3's miniboss spawns an imp every 15 seconds while alive. Each gives 30 ammo. Reinforcements count toward remaining wave enemies.
- The regular boat trigger overlaps the drowning volume. A valid boat touch now takes priority, including when water processes the overlap first; escaped players cannot subsequently trigger the death scene.
- 18 integration checks passed, including real 15-second timing, imp/ammo counts, projectile spin and cleanup, push damage/displacement, and water-first boat contact reaching Ending 1.

## Custom final-boss VFX and death cinematic
- BossController on the fusion boss prefab exposes Slam Impact Vfx, Imp Explosion Vfx, Imp Landing Vfx, Imp Trail Vfx, and Custom Vfx Lifetime. Existing per-attack Timer End Effect, passive effect, indicator material/color, and projectile model slots remain available.
- BossFusionEncounter exposes Imp Spawn Vfx, Merge Vfx Prefab, Reveal Vfx, Death Burst Vfx, Death Explosion Vfx, lifetimes, death effect scales, and explosion sound. Empty effect slots retain procedural fog. A custom merge prefab replaces the procedural growing cloud.
- Death sequence: camera pans to the boss; small bursts and shake build up; large explosion and a brief warm flash remove the boss; camera returns, then the player cheer begins. The exit opens after the explosion. Player damage is blocked during the cinematic.
- Optional Death Camera Shot sets an authored position/rotation; otherwise the shot is computed facing the boss from the player's side. Timing, shake, flash intensity, and explosion sizes are Inspector fields.
- Existing vfx_Explosion is assigned as the default death burst and explosion. Eleven Play Mode checks passed for ordering, invulnerability, visual retention/removal, flash cleanup, gate opening, and camera/control restoration.
