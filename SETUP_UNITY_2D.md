# Unity 6 LTS 2D Setup Checklist

## First Unity Hub Run

1. Open **Unity Hub** from the Start menu.
2. Press the profile icon in the top-left.
3. Press **Sign in**.
4. Sign in with your Unity account in the browser.
5. Back in Unity Hub, press the profile icon again.
6. Press **Manage licenses**.
7. Press **Add license**.
8. Choose **Get a free personal license** unless you already have Pro/Enterprise.
9. Accept the terms and press **Done**.

Common mistake: opening Unity Editor before this license step makes batch mode and project creation fail with "No valid Unity Editor license found."

## Confirm Editor Modules

1. In Unity Hub, press **Installs** in the left sidebar.
2. Find **6000.3.15f1**.
3. Press the gear icon or **Manage**.
4. Press **Add modules**.
5. Confirm **Windows Build Support (IL2CPP)** is checked or already installed.
6. Confirm **Documentation** is checked or use the manual documentation zip in `Downloads/Unity6000.3.15f1`.

Common mistake: choosing only Windows Build Support without IL2CPP can block release builds when Scripting Backend is set to IL2CPP.

## Open This Project

1. In Unity Hub, press **Projects**.
2. Press **Add**.
3. Press **Add project from disk**.
4. Select `C:\Users\Lars\Documents\nyt_spil`.
5. Press **Open**.
6. Let Unity import packages. This can take several minutes the first time.
7. When prompted to enable the new Input System and restart, press **Yes**.
8. Open `Assets/_Project/Scenes/TestScene.unity`.
9. Press **Play**.
10. Press WASD or arrow keys and confirm the movement vector changes.

## Recommended Unity Editor Settings

Use these after the project opens:

- **Edit > Project Settings > Editor**
  - Asset Serialization: **Force Text**
  - Version Control Mode: **Visible Meta Files**
- **Edit > Project Settings > Player > Other Settings**
  - Active Input Handling: **Input System Package (New)**
  - Color Space: **Gamma** for simple pixel art projects, or **Linear** if using lighting-heavy URP effects.
- **Edit > Project Settings > Quality**
  - Anti Aliasing: **Disabled** for crisp pixel art.
  - VSync Count: **Every V Blank** while developing.
- **Edit > Project Settings > Graphics**
  - Use a URP asset for the default render pipeline.
  - For pixel art, use a 2D Renderer and Pixel Perfect Camera.
- Sprite import defaults:
  - Texture Type: **Sprite (2D and UI)**
  - Filter Mode: **Point (no filter)**
  - Compression: **None**
  - Pixels Per Unit: choose one project-wide value, commonly **16**, **32**, or **100**.

## Recommended VS Code Settings

Already added in `.vscode/settings.json`:

- C# Dev Kit and Unity extension support.
- Format on save.
- Search excludes for Unity generated folders.
- Library/Temp/Obj hidden from the file explorer.

In Unity, press **Edit > Preferences > External Tools**, then:

1. Set **External Script Editor** to **Visual Studio Code**.
2. Check **Embedded packages**, **Local packages**, **Registry packages**, and **Git packages**.
3. Press **Regenerate project files**.

## GitHub Desktop Sign In

1. Open **GitHub Desktop** from the Start menu.
2. Press **Sign in to GitHub.com**.
3. Sign in in the browser.
4. Back in GitHub Desktop, press **File > Add local repository**.
5. Choose `C:\Users\Lars\Documents\nyt_spil`.
6. Press **Add repository**.
7. Commit the starter files.

Common mistake: do not commit `Library`, `Temp`, `Obj`, `Logs`, or builds. The `.gitignore` already excludes them.

## Offline Documentation Finish

The documentation zip is downloaded here:

`C:\Users\Lars\Downloads\Unity6000.3.15f1\UnityDocumentation.zip`

To install it manually:

1. Right-click **Windows PowerShell**.
2. Press **Run as administrator**.
3. Run:

```powershell
Expand-Archive -LiteralPath "$env:USERPROFILE\Downloads\Unity6000.3.15f1\UnityDocumentation.zip" -DestinationPath "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Data" -Force
```

## Download Locations

- Unity Hub: https://unity.com/download
- Unity release archive: https://unity.com/releases/editor/archive
- Visual Studio Community: https://visualstudio.microsoft.com/vs/community/
- Git for Windows: https://git-scm.com/download/win
- GitHub Desktop: https://desktop.github.com/
- VS Code: https://code.visualstudio.com/
- Aseprite: https://www.aseprite.org/download/
- LibreSprite: https://github.com/LibreSprite/LibreSprite/releases
- Audacity: https://www.audacityteam.org/download/windows/
- Blender: https://www.blender.org/download/
