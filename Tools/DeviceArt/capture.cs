var result=ANIMOL.Presentation.Editor.DeviceArtBuilder.Capture();
System.IO.File.WriteAllText("Docs/Validation/DeviceArt/capture_result.txt",result);
return result;
