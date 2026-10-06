import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const catalog = JSON.parse(await fs.readFile(path.join(root, 'Data/style_catalog.json'), 'utf8'));
const approved = JSON.parse(await fs.readFile(path.join(root, 'Data/approved_design_sources.json'), 'utf8'));
const escape = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const uri = async p => `data:image/png;base64,${(await fs.readFile(path.join(root, p))).toString('base64')}`;
let sections = '';
for (const source of approved.sources) {
  const styles = catalog.styles.filter(s => s.themeId === source.themeId);
  let cards = '';
  for (const style of styles) {
    const figures = [];
    for (const [panel, label] of [['material', '재료 원본 면'], ['frame', '완성 외곽 프레임'], ['ring', '완성 안쪽 프레임'], ['motif', '접합 문양']]) {
      figures.push(`<figure><img width="128" height="128" src="${await uri(`Art/${style.styleId}/${panel}.png`)}" alt="${escape(style.displayName)} ${label}"><figcaption>${label}</figcaption></figure>`);
    }
    cards += `<article><h3>${escape(style.styleId)} · ${escape(style.displayName)}</h3><div class="panels">${figures.join('')}</div></article>`;
  }
  sections += `<section id="${source.themeId}"><h2>${escape(styles[0].themeName)}</h2><details><summary>승인한 원화 그대로 보기</summary><img class="raw" src="${escape(source.sourceFile)}" alt="${source.themeId} 승인 원화"><p>승인 원화는 제작 참고이며, 아래 추출 패널과 최종 셀 아트는 공용 16색으로 변환됩니다.</p></details>${cards}</section>`;
}
const html = `<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>ANIMOL 승인 디자인 · clean_v2</title><style>
*{box-sizing:border-box}body{margin:0;background:#121521;color:#ececf2;font:16px/1.55 system-ui,sans-serif}main{max-width:1320px;margin:auto;padding:28px 20px}h1{font-size:27px;margin:0 0 12px}h2{margin:0 0 14px;color:#ffcd75}h3{font-size:17px;margin:0 0 14px}p{max-width:850px;color:#b8c3d4}nav{display:flex;flex-wrap:wrap;gap:10px;margin:20px 0 32px}a{color:#73eff7}nav a{border:1px solid #566c86;padding:8px 14px;border-radius:7px;text-decoration:none}section{margin-bottom:40px;padding:24px;background:#1b2130;border-radius:12px}article{padding:18px 0;border-top:1px solid #333c57}.panels{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}figure{margin:0;padding:12px;background:#101218;border-radius:8px;text-align:center}figure img{image-rendering:pixelated;display:block;width:100%;height:auto;aspect-ratio:1;object-fit:contain}figcaption{padding-top:10px;font-size:13px;color:#94b0c2}details{margin:16px 0 22px}summary{cursor:pointer;color:#73eff7}.raw{display:block;width:min(100%,1000px);height:auto;margin-top:16px;background:#101218}footer{color:#94b0c2;font-size:13px;padding-bottom:24px}@media(max-width:680px){.panels{grid-template-columns:repeat(2,minmax(0,1fr))}main{padding:18px 12px}section{padding:16px}}
</style><main><h1>ANIMOL · 승인 디자인 clean_v2</h1><p>사용자가 선택한 다섯 원화의 재료 면, 외곽 프레임, 안쪽 프레임과 접합 문양입니다. 원화의 디자인을 보존한 추출 패널을 확인하고, 실제 32px 연결 셀의 조합은 자유형 갤러리에서 확인할 수 있습니다.</p><nav><a href="DESIGN_GALLERY.html">실제 타일로 자유형 지형 그리기</a>${approved.sources.map(s => `<a href="#${s.themeId}">${s.themeId}</a>`).join('')}</nav>${sections}<footer>외곽·내곽 추출 패널은 완성 디자인 참고입니다. Runtime 연결 Sprite의 정확한 매핑은 Data/sprite_lookup.json을 사용합니다. Unity 프로젝트 적용 결과는 이 패키지에서 실행하지 않았습니다.</footer></main></html>`;
await fs.writeFile(path.join(root, 'APPROVED_DESIGNS.html'), html);
console.log(JSON.stringify({approvedThemes: approved.sources.length, panelImages: catalog.styles.length * 4, htmlBytes: Buffer.byteLength(html)}));
