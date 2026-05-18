# Initial Gameplay Setup

## What Was Added

- `GameInput.inputactions`: new Input System actions for Move, Interact, Attack, and Pause.
- `PlayerInputReader`: small input bridge that raises events from `PlayerInput`.
- `TopDownPlayerMotor`: Rigidbody2D movement with collision-friendly `MovePosition`.
- `PlayerAnimationController`: writes simple animator parameters for idle/walk blend trees.
- `FollowCamera2D`: a no-dependency camera follow fallback while Cinemachine is configured.
- `DefaultPlayerMovement.asset`: ScriptableObject movement tuning.
- `Tools > Project Setup > Create Initial Gameplay Scene`: editor tool that builds the first playable scene and saves `Assets/_Project/Prefabs/Player.prefab`.

## Unity Editor Configuration

1. Open Unity Hub.
2. Sign in and activate a Unity license if you have not already.
3. Open `C:\Users\Lars\Documents\nyt_spil`.
4. Wait for Package Manager to finish importing URP, Input System, and Cinemachine.
5. If Unity asks to switch to the new Input System and restart, press **Yes**.
6. Open **Edit > Project Settings > Player > Other Settings**.
7. Set **Active Input Handling** to **Input System Package (New)**.
8. Open **Edit > Project Settings > Editor**.
9. Set **Asset Serialization** to **Force Text**.
10. Set **Version Control Mode** to **Visible Meta Files**.
11. Open **Tools > Project Setup > Create Initial Gameplay Scene**.
12. Open `Assets/_Project/Scenes/Gameplay.unity`.
13. Press **Play**.
14. Move with **WASD**, **Arrow Keys**, or a gamepad left stick.

## Player Inspector Setup

The scene builder creates the Player GameObject with:

- `Rigidbody2D`
  - Body Type: Dynamic
  - Gravity Scale: 0
  - Freeze Rotation: enabled
  - Interpolate: Interpolate
  - Collision Detection: Continuous
- `CapsuleCollider2D`
- `PlayerInput`
  - Actions: `Assets/_Project/Input/GameInput.inputactions`
  - Default Map: `Player`
  - Behavior: Send Messages
- `PlayerInputReader`
- `TopDownPlayerMotor`
  - Movement Settings: `DefaultPlayerMovement`
- `PlayerAnimationController`

## Basic Animation Setup

Create an Animator Controller later at:

`Assets/_Project/Animations/Player.controller`

Add these parameters exactly:

- `MoveX` float
- `MoveY` float
- `FacingX` float
- `FacingY` float
- `Speed` float

Recommended first states:

- `Idle_Down`
- `Walk_Down`
- `Walk_Up`
- `Walk_Left`
- `Walk_Right`

Use `Speed > 0.01` for walking transitions and `FacingX/FacingY` to choose idle direction.

## Camera Setup

The generated scene uses `FollowCamera2D` immediately. For Cinemachine:

1. Open **GameObject > Cinemachine > 2D Camera**.
2. Assign the Player Transform as the follow target.
3. Keep the Main Camera in the scene with the Cinemachine Brain that Unity adds.
4. Disable or remove `FollowCamera2D` from Main Camera after Cinemachine works.

## Collision Setup

Use layers:

- `Player`
- `Solid`
- `Interactable`

For Tilemaps:

1. Select your collision Tilemap.
2. Add `TilemapCollider2D`.
3. Add `CompositeCollider2D`.
4. Add `Rigidbody2D`.
5. Set the Tilemap Rigidbody2D Body Type to **Static**.
6. On `TilemapCollider2D`, enable **Used By Composite**.
7. Put the Tilemap on the `Solid` layer.

Common bug: if the player slides through walls, confirm the walls have Collider2D components and the player has both Rigidbody2D and Collider2D.
