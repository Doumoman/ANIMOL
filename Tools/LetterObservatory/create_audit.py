import pathlib
s=pathlib.Path('Tools/MemoryTheater/audit.py').read_text()
s=s.replace("Docs/Validation/MemoryTheater","Docs/Validation/LetterObservatory/Applied").replace('memory_theater_import_manifest','letter_observatory_import_manifest')
s=s.replace("allowed={'Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs','Assets/ANIMOL/Scripts/Runtime/DeviceVisualView.cs','Assets/ANIMOL/Scripts/Runtime/DeviceArtQaController.cs'}","allowed={'Assets/ANIMOL/Scripts/Editor/ArtAssetBuilder.cs'}")
s=s.replace("len(colors)==16","len(colors)==13").replace('floor_plum.png','floor_navy.png')
pathlib.Path('Tools/LetterObservatory/audit_applied.py').write_text(s)
