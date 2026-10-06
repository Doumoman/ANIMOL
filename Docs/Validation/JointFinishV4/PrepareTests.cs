// Keep unrelated MCP TCP client timeout logs out of synchronous Unity tests.
var running=MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost.IsRunning;
UnityEditor.SessionState.SetBool("JointFinishV4.McpWasRunning",running);
if(running)MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost.Stop();
// The legacy window refuses binding while the Scene editor owns the bridge.
ANIMOL.Editor.TerrainEditorSceneEditor.Close();
return new{mcpWasRunning=running,sceneEditorActive=ANIMOL.Editor.TerrainEditorSceneEditor.Active};
