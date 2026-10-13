from pathlib import Path
s=Path('Tools/LetterObservatory/atlas_qa.cs').read_text(encoding='utf8').replace('ObservatoryAtlasQa','SeaAtlasQa').replace('LetterObservatoryArtBuilder','TwilightSeaArtBuilder').replace('LetterObservatory/Applied','TwilightSea').replace('letter_observatory','twilight_sea')
start=s.index('        var sprites=');end=s.index('        var atlas=',start)
s=s[:start]+'''        var sprites=m.assets.Where(a=>new[]{"05","11","12","15"}.Contains(a.deviceId)).Take(45).Select(a=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+a.path))
            .Concat(m.animations.Where(a=>a.deviceId=="15"||a.id.StartsWith("d11_pilot_boat/row_")).Select(a=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/"+a.frames[a.frames.Length/2].path))).ToArray();
'''+s[end:]
Path('Tools/TwilightSea/atlas_qa.cs').write_text(s,encoding='utf8')
s=Path('Tools/LetterObservatory/finalize.cs').read_text(encoding='utf8').replace('ObservatoryFinalize','SeaFinalize').replace('var validation=LetterObservatoryArtBuilder.Validate();','var validation=TwilightSeaArtBuilder.Validate();LetterObservatoryArtBuilder.Validate();').replace('OpenScene(LetterObservatoryArtBuilder.ScenePath)','OpenScene(TwilightSeaArtBuilder.ScenePath)').replace('LoadAssetAtPath<SceneAsset>(LetterObservatoryArtBuilder.ScenePath)','LoadAssetAtPath<SceneAsset>(TwilightSeaArtBuilder.ScenePath)')
Path('Tools/TwilightSea/finalize.cs').write_text(s,encoding='utf8')
s=Path('Tools/LetterObservatory/finish_capture.cs').read_text(encoding='utf8').replace('ObservatoryCaptureMove','SeaCaptureMove').replace('LetterObservatory','TwilightSea');Path('Tools/TwilightSea/finish_capture.cs').write_text(s,encoding='utf8')
s=Path('Tools/LetterObservatory/audit_applied.py').read_text(encoding='utf8').replace('LetterObservatory/Applied','TwilightSea').replace('letter_observatory','twilight_sea').replace("'Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs'","'Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs','Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs'").replace("['floor_navy.png','floor_slate.png']","['floor_sand.png','floor_dock.png','water_flat.png','water_deep.png']")
s=s.replace("'Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs'","'Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs','Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs'")
Path('Tools/TwilightSea/audit.py').write_text(s,encoding='utf8')
