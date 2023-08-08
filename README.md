# About

DockerIRL is a Docker 3D management software to replace your current cheap-whatever-Docker-management-2D-GUI-or-CLI-app you are currently using.

Tested on Windows 11.

# DEMO

[![VIDEO DEMONSTRATION ON YOUTUBE](http://img.youtube.com/vi/zag8veuzlqY/0.jpg)](http://www.youtube.com/watch?v=zag8veuzlqY)

http://www.youtube.com/watch?v=zag8veuzlqY

# Used technologies

This project is mostly a mix of DLLs and Unity packages with a glue code that somewhat holds everything together. Here are the technologies that I used:

## Unity packages

- Newtonsoft Json

- Json.NET Converters of Unity types - used for serializing `Vector3`, `Quaternion`

- Cinemachine

- TextMeshPro

- Input System

- Others...

## DLLs

- Docker.DotNet.dll - at `Assets/DLL`, downloaded from https://www.nuget.org/packages/Docker.DotNet. Used to communicate with Docker Engine using simple C# methods.

- Pty.Net.dll - at `Assets/DLL`, manually built from code that was taken from https://github.com/microsoft/vs-pty.net. Used to open and manage a PTY and IO streams using native OS calls on both Windows & Linux.

- VtNetCore - at `Assets/DLL`, manually built from modified code from https://github.com/darrenstarr/VtNetCore. Used to parse IO (escape sequences, etc) from a PTY. Modifications: Added `OnBell` event.

- ConPTY - at `Assets/DLL/os64`, ~~LITERALLY STOLEN~~ taken from https://github.com/wez/wezterm/blob/main/assets/windows/conhost/conpty.dll. Note that this DLL can be easily built from source: https://github.com/microsoft/terminal#developer-guidance, it requires an incredible amount of laziness to not do that. (TODO: Build & use a DLL)

## Code

- UnityMainThreadDispatcher, at `Assets/Scripts/vendor/UnityMainThreadDispatcher.cs`, source: https://github.com/PimDeWitte/UnityMainThreadDispatcher, used to enqueue tasks to Unity's main thread from other threads.

## Other

- Blender

- Audacity

- freesound.org

# Development

Unity version: `2021.3.19f1`

Copy sample configs:

```bash
cp ./Assets/Sample/keybinds.json keybinds.json
cp ./Assets/Sample/settings.jsonc settings.jsonc
cp ./Assets/Sample/state.json state.json
```

Run this script after building project:

```bash
python ./Utilities/post_build.py
```
