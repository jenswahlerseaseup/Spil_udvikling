# Build And Play

## Første Gang

1. Åbn projektet i Unity.
2. Vent til Unity er færdig med at importere og compile.
3. Hvis `Gameplay.unity` ikke åbner automatisk, åbn:
   `Assets/_Project/Scenes/Gameplay.unity`
4. Tryk **Play** i Unity for at teste.

## Lav En Spilfil

1. I Unity-menuen, vælg **Tools > Build > Build Windows Demo**.
2. Vent til buildet er færdigt.
3. Dobbeltklik på `PLAY_NYT_SPIL.cmd` i projektmappen.

Buildet bliver lavet her:

`Builds/Windows/NytSpil.exe`

## Controls

- Move: WASD, piletaster eller gamepad left stick
- Attack: Space, left mouse eller gamepad right trigger
- Interact: E eller gamepad south button
- Pause: Escape eller gamepad start

## Hvis Noget Ikke Virker

- Tjek at Unity ikke viser compiler errors nederst i Console.
- Tjek at scenen hedder `Gameplay`.
- Tjek at Player har `Rigidbody2D`, `CapsuleCollider2D`, `PlayerInput`, `PlayerInputReader`, `TopDownPlayerMotor`.
- Tjek at `PlayerInput` bruger `Assets/_Project/Input/GameInput.inputactions`.
