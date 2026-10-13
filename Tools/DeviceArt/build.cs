var result=ANIMOL.Presentation.Editor.DeviceArtBuilder.Build();
System.IO.File.WriteAllText("Docs/Validation/DeviceArt/build_result.txt",result);
return result;
