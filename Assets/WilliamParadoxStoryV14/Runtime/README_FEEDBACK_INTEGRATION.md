# Optional feedback and host suspension

## Button feedback

Place `StoryButtonFeedback` on the existing Selectable/Button object. Assign `runner` and a separate cosmetic child `visual`, or call `Configure(runner, visual)`. Keep the Button's root RectTransform and raycast Graphic as the stable hitbox. The component refuses to animate the root or an unrelated transform; it never changes Selectable colors or click listeners.

Pointer down or submit scales the visual to 97% of its authored local scale in 45 ms and returns it in 90 ms on release. A quick tap finishes both phases. Pointer exit returns the visual; no hover effect exists. Timings use unscaled delta. Disabled/non-interactable controls, runner pause, focus loss, application pause and reduced motion restore the exact authored scale without replay on resume. `ResetFeedback()` is available for immediate host-side cancellation. This component plays no sound or vibration.

## Explicit gameplay adapter

`StoryGameplayBridge` only requests suspension through inspector-assigned MonoBehaviours implementing:

```csharp
public interface IStoryPauseSink
{
    void SetStorySuspended(bool suspended);
}
```

The actual project adapter must gate swipe/gameplay input and abilities, and hold its own cost-window, turn and puzzle clocks while this story-specific suspension is true. It must preserve the current timer values. It must combine story suspension with other owners such as an existing pause menu instead of blindly unpausing them. No particular game timer is auto-discovered or guessed here.

Assign `runner` and `sinks` before enabling the bridge. `OnEnable` subscribes to node/completion events and requests suspension while a live story is pending or active. End/completion, `OnDisable`, or `ReleaseStory()` balance that request. `BeginStory()`/`SetStoryActive(true)` acquire it again. Host gameplay gates may explicitly release the bridge; a later story node reacquires it. If adapters are changed at runtime, call `RefreshAdapters()`; removed adapters receive a balancing false request. Duplicate references receive only one request.

`IsSuspended` describes the requested state, not a guarantee that a game is connected. Check `HasAdapters`, `ConnectedAdapterCount`, and `ConfigurationWarning`. Empty or invalid adapters produce a clear warning stating that gameplay input, abilities and timers are not connected. Standalone preview scenes need no real gameplay adapter.

The bridge never sets `Time.timeScale`, grants rewards, consumes a turn, invokes puzzle completion, or emits plot events. Actual project integration still needs the explicit adapter and Play/device verification.

## Optional audio

Assign `StoryAudioFeedback.source` explicitly to a dedicated non-looping 2D SFX AudioSource; never assign the music source. The theater package supplies temporary curtain, door, and eraser WAV files and maps them to five nodes. Map any further `nodeSounds` entries by `nodeId` with `AudioClip[] clips` and `volume`; the first assigned clip is chosen deterministically. Set `advanceClip`/`advanceVolume` only if accepted taps should have sound.

Runner hooks are `PlayNode(nodeId, restoring)` after presenting a node and `PlayAdvance()` only after input acceptance. Call `ResetSession()` only for a deliberate new story/restart, not every scene, pause, or save restore. `StopFeedback()` cancels transient audio. Restored, muted, paused and previously seen nodes are remembered and never queued to replay on resume. Accepted tap sound has a 150 ms/same-frame guard; one dedicated voice means a node cue replaces a same-frame tap instead of stacking. Focus loss, app/runner pause and disable stop transient audio. Background music is not managed by this component.

Haptic feedback is absent/off. No `Handheld.Vibrate`, camera shake or duplicate tween is added.

## Verification status

`RuntimeFeedbackBridgeTests` covers stable hitboxes/colors, quick-tap restoration, reduced motion, pause/disable/focus cancellation, balanced and deduplicated sink requests, adapter removal, reentrant release, and restore/mute node suppression. The project integration run used Unity 6000.3.8f1 and passed the story EditMode suite (51/51); see `Validation/EchoTheater/INTEGRATION_RESULT.md` for the separate PlayMode and device verification scope.
