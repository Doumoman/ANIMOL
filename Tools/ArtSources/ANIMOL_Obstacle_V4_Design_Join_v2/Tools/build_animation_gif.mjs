import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const run=args=>{const r=spawnSync('ffmpeg',args,{cwd:root,stdio:'inherit'});if(r.error)throw r.error;if(r.status!==0)throw Error('FFmpeg failed');};
const source='Validation/animation_frames/board_%03d.png',palette='Validation/animation_palette.png';
run(['-hide_banner','-loglevel','error','-y','-framerate','100/3','-i',source,'-vf','palettegen=max_colors=16:reserve_transparent=0','-frames:v','1',palette]);
run(['-hide_banner','-loglevel','error','-y','-framerate','100/3','-i',source,'-i',palette,'-lavfi','paletteuse=dither=none','-loop','0','Art/Preview_45_Animated.gif']);
