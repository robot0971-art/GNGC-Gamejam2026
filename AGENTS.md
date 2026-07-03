# Gamejam2026 Agent Notes

## Project Direction

This is a 48-hour 2D gamejam project.

Core fantasy:
- The player is in a sniper/inspection viewpoint.
- Five entrants are shown each wave.
- Some entrants are humans and some are AI.
- The player must identify and shoot only AI.
- The mood is closer to an immigration/security checkpoint than a free-movement shooter.

Do not build a landing page, meta UI, or heavy framework around the game. Keep the first playable screen focused on the actual game flow.

## Current Game Flow

The intended wave flow is:

1. Game starts.
2. Sniper cutscene plays.
3. Five entrants are previewed one by one.
4. Sniper mode starts.
5. Timer starts.
6. AI count determines bullet count.
7. Player shoots suspected AI.
8. Wave resolves when bullets are exhausted or all AI are removed.

Important current behavior:
- Timer is currently test-oriented around 10 seconds.
- When timer expires and bullets remain, one unshot human is marked as hit and life decreases.
- Timer expiration should not stop shooting. Remaining bullets must still be usable on AI.
- Human hit and timeout penalties should not double-penalize again at wave resolve.

## Character Rules

Characters are single-sprite entrants.

Because there are no walk cycles or death animations, animation should rely on short impact illusions:
- Small hit flash.
- Slight pushback.
- Tiny rotation.
- Minor squash.
- AI disappear/glitch/fade.
- Human hit color feedback.

Do not force large falling/lying animations for single-sprite characters unless new art supports it.

Character art is loaded from `Assets/Charactor-art`.

Naming convention:
- Files starting with `ai` or `ai_` are AI.
- Files containing `-ai-` are also AI, such as `round1-ai-1`.
- Other character sprites such as `cha_` are treated as humans.
- AI art can be organized under `Round1` through `Round5`.
- By default, stage number selects the AI round pool.
  - Stage 1 uses `Round1`.
  - Stage 2 uses `Round2`.
  - Stage 5 uses `Round5`.
- `StageConfig.artRound` is optional. Leave it `0` for automatic stage-number matching, or set it manually to override.

Random wave composition:
- A wave has 5 entrants by default.
- AI count is random from 1 to 3 for testing.
- Bullets granted equals the AI count.
- A human and AI with the same numeric pair id must not appear together in the same wave.
  - Example: `cha_01` and `round1-ai-1` cannot appear together.
  - The same applies to `02`, `03`, and the rest.
  - This rule remains the same as rounds increase.

Character size:
- Random sprites can have different bounds/PPU, so they may appear as giants or tiny characters if using raw sprite size.
- `EntrantSlot` normalizes sprite height based on the original slot's starting visible height.
- Keep this behavior unless all final art is normalized manually.

## UI Rules

Timer UI:
- `Timer UI` object contains a TMP text child named `Time` or `TIme`.
- Timer is hidden during cutscene/preview and shown during judging.

Heart UI:
- Root should be named `Heart UI`, or assigned directly to `HudView.heartObject`.
- Children are treated as heart icons in sibling order.
- With max mistakes 3:
  - 0 mistakes: 3 hearts.
  - 1 mistake: 2 hearts.
  - 2 mistakes: 1 heart.
  - 3 mistakes: game over.

Bullet UI:
- Use text, not icons.
- Root should be named `Bullet UI`, or assigned directly to `HudView.bulletObject`.
- Put a TMP text child under `Bullet UI`.
- Display format is `current/max`, for example `3/3`, `2/3`, `0/3`.

Do not add code that automatically creates Heart UI or Bullet UI objects at runtime. These should be built in the scene or wired through the Inspector.

## Sniper Mode

Main sniper objects:
- `Player Sniper` is the sniper reticle/visual group.
- `Sniper Aim (1)` is the aim reference point.
- `Sniper Mask` is the black scope mask.

Current sniper behavior:
- Right-click toggle is disabled for normal play.
- The game enters persistent sniper mode after intro/preview.
- `Player Sniper` follows target selection.
- Horizontal mouse position selects among visible entrants.
- `Sniper Aim (1)` remains the visual aiming reference.

Shooting:
- Left click should fire in sniper mode.
- Shooting should use the current sniper target reliably.
- Be careful with target override state. If an entrant is already shot, clear or refresh the override target so the next AI can still be hit.

## Code Architecture

The project follows a practical gamejam version of:
- Singleton for central systems where useful.
- Observer/events for gameplay notifications.
- SOLID principles where they improve speed and safety.

Avoid over-engineering. Prefer focused classes with clear responsibilities.

Key classes:
- `GameManager`
  - Owns main game flow.
  - Handles stage/wave state transitions.
  - Starts preview, sniper mode, timer, and wave resolve.

- `EntrantDatabase`
  - Builds random wave data.
  - Auto-fills humans/AIs in editor from `Assets/Charactor-art` when arrays are empty.

- `WavePresenter`
  - Owns entrant slots presentation.
  - Shows preview at center.
  - Shows all entrants.
  - Counts remaining AI and shot humans.

- `EntrantSlot`
  - Owns one entrant's sprite, collider, hit state, and hit/death feedback.
  - Normalizes sprite visual height.

- `ShootingController`
  - Owns left-click shooting.
  - Tracks bullets.
  - Emits shot events.
  - Supports sniper target override.

- `HudView`
  - Owns UI display only.
  - Handles timer text, heart icons, bullet text, status text, and clear panel.

- `CountdownTimer`
  - Owns countdown coroutine behavior.
  - Keeps timer loop out of `GameManager`.

- `WaveResultEvaluator`
  - Owns success/failure evaluation.

- `WaveResult`
  - Data object for remaining AI and shot human count.

## Design Constraints

No ScriptableObjects are planned for the gamejam version.

Prefer Inspector references when the object exists in the scene.

Avoid repeated runtime `FindObjectsByType` in per-frame gameplay. Occasional setup/resolve-time search is acceptable for this project size.

Avoid automatic scene-object creation unless explicitly requested. Scene UI should be created by the developer in Unity.

Use existing naming conventions and scene objects before adding new systems.

## Known Unity/Editor Issues

Unity AI Assistant service/network errors can appear in the console. These are unrelated to gameplay code.

`SerializedObjectNotCreatableException: Object at index 0 is null` usually means the Inspector is looking at a null/destroyed selection. Try:
- Deselect all.
- Click empty Hierarchy space.
- Reopen Inspector.
- Reset layout if it keeps happening.

The project is not currently a git repository, so changes cannot be audited through git history unless git is initialized later.

## Verification

After code changes, run:

```powershell
dotnet build Assembly-CSharp.csproj
```

Expected result should be:
- 0 warnings.
- 0 errors.

For Unity-specific scene behavior, prefer checking in Play Mode after compilation.
