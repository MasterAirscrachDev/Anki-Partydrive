# Abilities — summary

This file summarises each ability implemented under Assets/Scripts/Abilities.

- `AbilityController` : attaches an ability instance to an owner `CarController`. Provides `Setup(owner)` and `ReportDamage(amount)` so abilities can credit damage back to the owner for stats.

- `GlobalAbilitySystem` : central factory/spawner for abilities and visuals (missiles, EMP, trails, orbital laser, crasher boost, grappler, lightning, recharger, traffic cone, disabled visual). Contains convenient `SpawnX()` methods used by gameplay logic.

- `AbilityMissile` : flying projectile (seeking or fixed target). Uses `speedCurve`/`handlingCurve`, switches head material when seeking. Explodes on ground contact (y < 0) and deals ~25 energy damage to nearby cars (reports to owner). Plays missile SFX and spawns explosion particle.

- `AbilityEMP` : area EMP effect. Grows/fades by curves, activates after `activationDelay`. On activation it drains 40 energy from affected cars and applies a -20 speed modifier for ~3s (skips owner). Reports energy-drain as damage to ability owner.

- `AbilityRecharger` : follows the user and periodically restores energy. Configurable `energyPerTick`, `tickInterval`, and `totalDuration`. Destroys itself when duration ends or user is gone.

- `AbilityTrailEmitter` : attached to a car to periodically spawn trail prefabs while moving. Each spawned trail uses `AbilityHazardZone` so vehicles that drive through the trail take energy changes or speed modifiers.

- `AbilityHazardZone` : generic zone placed by trails or hazards. On first contact with a car (per zone) it either consumes energy (negative `energyChange`) or grants energy, and can apply a speed modifier (absolute or multiplier) for a configurable duration. If `energyChange < 0` the owner records the damage dealt.

- `AbilityOrbitalLazer` : heavy single-target orbital strike. Targets a car visual transform (usually first place), plays charge animation, waits `damageDelay` (default 1s) then deals large energy damage (500 in code), plays SFX and destroys itself.

- `AbilityLightningPower` : spawns one or more lightning cloud objects that move above target cars and then strike. Applies a large slow (code uses -60, true) for 4s, plays a lightning SFX for the first strike, shows a brief line renderer strike and then disperses.

- `AbilityCrasherBoost` : projectile that homes to a target car. On hit it grants a large immediate boost to the target (`boostAmount`, applied as a speed modifier non-mult), then after `boostDuration` applies a strong slow (configured `slowPercent`) for `slowDuration`. Spawns slow particle and self-destructs after effects.

- `AbilityGrappler` : attaches user to a target car, applies `GrapplerBoost` (+20% mult) to the user and `GrapplerSlow` (-20% mult) to the target for 8s. Optionally renders a connecting line. Removes effects early if the user overtakes the target.

- `AbilityCone` (Traffic Cone) : placed obstacle that checks a small cube area behind the cone for collisions. On hit it deals a modest energy hit (damage ~15), applies a slowdown (configured `slowPercent`) and spawns an animated cone visual; then destroys itself after a short life.

- `AbilityDisabled` : visual effect used while a car is in a disabled state. Follows the user's visual transform for `duration` and then self-destructs.

Notes:
- The `Ability` enum (in `AbilityController.cs`) contains additional categories/tier names (Missle1..3, MissleSeeking1..3, Overdrive, BurstShield, etc.). Some enum entries are represented by different prefab variants or are not implemented as separate classes in this folder (they are handled by `GlobalAbilitySystem` or by ability prefab parameters).
- Many numeric values (damage, durations, modifiers) are serialized fields on prefabs/scripts and can be tuned in the Unity inspector.



## Overdrive Ability
Fallback (will be used for cars that dont have a specific overdrive implemented).
- +400 speed for 8 seconds -300 to all opponents for 8 seconds
- Gives 100% energy after 8 seconds

Big Bang Ability (Black hole)
- for 8 seconds All Enemies recive a preportional slow based on their distance to the user (max 80% slow)

Thermo Ability (Heatsink)
- for 8 seconds, user gains a 200 speed boost and energy recharge.

IceWave & x52Ice (Frozen Fronter)
- Summon a wave of ice that quickly travels forward, dealing a 3s stun to all opponents it hits. the wave tavels for 8s or a full lap, whichever comes first.

Spektrix Ability (Disco Oblivion)
- Summons disco balls all over the track (roughly 4 per segment) that last for 8 seconds. If an opponent hits a disco ball, they are slowed by 100 for 2 seconds and the disco ball is destroyed, if the user hits a disco ball, they are boosted by 100 for 2 seconds and the disco ball is destroyed.