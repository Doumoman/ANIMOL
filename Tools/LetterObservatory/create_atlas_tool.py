import pathlib
s=pathlib.Path('Tools/MemoryTheater/atlas_qa.cs').read_text()
s=s.replace('TheaterAtlasQa','ObservatoryAtlasQa').replace('MemoryTheaterArtBuilder','LetterObservatoryArtBuilder').replace('MemoryTheater','LetterObservatory/Applied').replace('memory_theater','letter_observatory')
s=s.replace('new[]{"08","09","18","23"}','new[]{"03","10","16","21"}').replace('a.id.StartsWith("d08_")||a.id=="foundation/curtain_idle"','a.deviceId=="21"||a.id.StartsWith("d10_optics/mirror_")')
pathlib.Path('Tools/LetterObservatory/atlas_qa.cs').write_text(s)
