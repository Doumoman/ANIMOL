# V6 native Editor probes

These are reviewable Pipeline `run_script` probes, outside Assets and never installed as gameplay components. They ran in a temporary copy of the current working project (including its existing M7 owners), with Unity 6000.3.8f1 GUI mode. Output JSON and PNG files are next to this directory.

Use a fresh verification copy and the existing DEV-OVG04-JOIN fixture. Run open, place, move, play, runtime, game-capture, physics, return, verify-return, capture in that order. Allow Editor frames between calls. Source paths and output paths target the task's Windows workspace. SceneView event coordinates include its 26-point native toolbar; actual duringSceneGui coordinates do not.

The physics probe drives the existing player's Rigidbody through real trigger contacts while suppressing locomotion for deterministic positioning. It does not create a movement, bubble, checkpoint, or result owner. Captures use CaptureEditorWindow on the Unity window's framebuffer and never read desktop pixels. Screenshots taken inside a coroutine are deferred to Editor delayCall after the frame has painted.

The separate NUnit EditMode/PlayMode suites validate the current original working project. GUI probes validate the native menu, canvas gesture handling, actual toolbar Local Play, portrait UI, physical checkpoint/exit state and same-window return. They are automated Editor events, not physical mouse or device touches.
