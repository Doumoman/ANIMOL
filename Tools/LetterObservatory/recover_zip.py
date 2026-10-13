import pathlib,struct,zlib,json
source=pathlib.Path('MCP_INBOX/ANIMOL_LETTER_OBSERVATORY_V01.zip');data=source.read_bytes();root=pathlib.Path('C:/Users/user/AppData/Local/Temp/animol-letter-observatory');root.mkdir(exist_ok=True)
offset=0;files=[];errors=[]
while data[offset:offset+4]==b'PK\x03\x04':
    if offset+30>len(data):errors.append('Incomplete local header');break
    _,version,flags,method,t,d,crc,compressed,size,namelen,extra=struct.unpack_from('<IHHHHHIIIHH',data,offset)
    name=data[offset+30:offset+30+namelen].decode('utf8');start=offset+30+namelen+extra
    if compressed==0 and size>0 and method==8:
        decoder=zlib.decompressobj(-15);partial=decoder.decompress(data[start:])
        errors.append(f'Unfinished entry: {name}; pending CRC/size header; expected {size} bytes, recovered {len(partial)}, deflate EOF={decoder.eof}')
        break
    if flags&8:errors.append('Descriptor entry requires further recovery: '+name);break
    if start+compressed>len(data):errors.append('Truncated entry: '+name);break
    payload=data[start:start+compressed]
    try:raw=zlib.decompress(payload,-15) if method==8 else payload if method==0 else None
    except zlib.error as e:errors.append(name+': '+str(e));break
    if raw is None or len(raw)!=size or zlib.crc32(raw)!=crc:errors.append('CRC/size mismatch: '+name);break
    relative=pathlib.PurePosixPath(name);assert not relative.is_absolute() and '..' not in relative.parts
    target=root/name
    if not name.endswith('/'):target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);files.append(name)
    offset=start+compressed
result={'sourceBytes':len(data),'centralDirectoryMissing':data.rfind(b'PK\x05\x06')<0,'verifiedRecoveredFiles':len(files),'offset':offset,'remainingBytes':len(data)-offset,'errors':errors,'lastCompleteEntry':files[-1] if files else None}
pathlib.Path('Docs/Validation/LetterObservatory/zip_recovery.json').write_text(json.dumps(result,indent=2));print(result)
