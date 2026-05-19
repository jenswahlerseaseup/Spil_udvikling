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
- Interact: E eller gamepad south button
- Run / skub bilen: hold Shift eller hold hoejre under saebekasse-run
- Jump: Space eller gamepad south button
- Inventory: Tab eller gamepad north button
- Pause: Escape eller gamepad start

## Saebekassebil Prototype

1. Find dele rundt paa gaarden: traebraedder, hjul, aksel og lejer.
2. Gaa til **Soapbox Garage** ved skuret for at se bilens status.
3. Gaa til **Soapbox Start Ramp** ved sydvejen og tryk **E**.
4. I side-view run: hold hoejre for fart. Maalet er at komme saa langt som muligt.

## Hvis Noget Ikke Virker

- Tjek at Unity ikke viser compiler errors nederst i Console.
- Tjek at scenen hedder `Gameplay`.
- Tjek at Player har `Rigidbody2D`, `CapsuleCollider2D`, `PlayerInput`, `PlayerInputReader`, `TopDownPlayerMotor`.
- Tjek at `PlayerInput` bruger `Assets/_Project/Input/GameInput.inputactions`.
