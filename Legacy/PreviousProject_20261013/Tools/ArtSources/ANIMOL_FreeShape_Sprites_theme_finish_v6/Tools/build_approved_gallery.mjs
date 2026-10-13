import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = async file => JSON.parse(await fs.readFile(path.join(root, file), 'utf8'));
const catalog = await read('Data/style_catalog.json'), approved = await read('Data/approved_design_sources.json');
const lookup = await read('Data/sprite_lookup.json'), registration = await read('SourceArt/THEME_FINISH_NATIVE_REGISTRATION_V6.json');
if (lookup.artVersion !== 6 || registration.artVersion !== 6 || lookup.styles.length !== 20 || registration.styles.length !== 19) throw new Error('Complete theme_finish_v6 registration required.');
const escape = s => String(s).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const uri = async p => `data:image/png;base64,${(await fs.readFile(path.join(root, p))).toString('base64')}`;
const historicalSources = [];
let sections = '';
for (const source of approved.sources) {
  const styles = catalog.styles.filter(s => s.themeId === source.themeId);
  const sourceBytes = await fs.readFile(path.join(root, source.sourceFile));
  historicalSources.push({ themeId: source.themeId, file: source.sourceFile, bytes: sourceBytes.length, sha256: crypto.createHash('sha256').update(sourceBytes).digest('hex') });
  let cards = '';
  for (const style of styles) {
    const exported = lookup.styles.find(s => s.styleId === style.styleId);
    const native = registration.styles.find(s => s.styleId === style.styleId);
    const figures = [];
    for (const [panel, label] of [['material', '재료 반복 면'], ['frame', '완성 외곽 연결'], ['ring', '완성 내부 구멍 연결'], ['motif', '기존 접합 문양']]) {
      figures.push(`<figure><img width="128" height="128" src="${await uri(exported.panels[panel])}" alt="${escape(style.displayName)} ${label}"><figcaption>${label}</figcaption></figure>`);
    }
    const description = style.styleId === 'T01_A' ? 'v5의 옥판 무늬, 흰 하이라이트, 산호색·보라색 그림자와 둥근 모서리를 그대로 유지했습니다.' : native?.materialDescription ?? '테마의 재료 무늬와 일정한 윤곽·띠 명암을 정돈한 v6 출력입니다.';
    const provenance = style.styleId === 'T01_A'
      ? '<a href="Data/hole_native_layout_v5.json">유지한 v5 제작 좌표</a>'
      : `<a href="${escape(native.profile)}">현재 재료·경계 제작 좌표</a>`;
    cards += `<article><h3>${escape(style.styleId)} · ${escape(style.displayName)}${style.styleId === 'T01_A' ? ' · v5 유지' : ' · v6 개정'}</h3><p>${escape(description)} ${provenance}</p><div class="panels">${figures.join('')}</div></article>`;
  }
  sections += `<section id="${source.themeId}"><h2>${escape(styles[0].themeName)}</h2><p><a href="Previews/${source.themeId}_theme_finish_v6_comparison.png">이 지역 4스타일의 실제 v5/v6 셀 조립 비교</a></p><details><summary>기존 clean_v2 승인 원화 · 보존한 디자인 기준</summary><img class="raw" src="${escape(source.sourceFile)}" alt="${source.themeId} 기존 승인 기준 원화"><p>기존 승인 원화의 파일과 바이트를 보존했습니다. 현재 출력은 아래 패널과 실제 아틀라스이며 과거 원화나 새 원화 참고 이미지를 Runtime Sprite로 사용하지 않습니다.</p></details>${cards}</section>`;
}
const html = `<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>ANIMOL 테마별 재료·마감 · theme_finish_v6</title><style>
*{box-sizing:border-box}body{margin:0;background:#121521;color:#ececf2;font:16px/1.55 system-ui,sans-serif}main{max-width:1320px;margin:auto;padding:28px 20px}h1{font-size:27px;margin:0 0 12px}h2{margin:0 0 14px;color:#ffcd75}h3{font-size:17px;margin:0 0 12px}p{max-width:1050px;color:#b8c3d4}nav{display:flex;flex-wrap:wrap;gap:10px;margin:20px 0 32px}a{color:#73eff7}nav a{border:1px solid #566c86;padding:8px 14px;border-radius:7px;text-decoration:none}section{margin-bottom:40px;padding:24px;background:#1b2130;border-radius:12px}article{padding:18px 0;border-top:1px solid #333c57}.panels{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}figure{margin:0;padding:12px;background:#101218;border-radius:8px;text-align:center}figure img{image-rendering:pixelated;display:block;width:100%;height:auto;aspect-ratio:1;object-fit:contain}figcaption{padding-top:10px;font-size:13px;color:#94b0c2}details{margin:16px 0 22px}summary{cursor:pointer;color:#73eff7}.raw{display:block;width:min(100%,1000px);height:auto;margin-top:16px;background:#101218}footer{color:#94b0c2;font-size:13px;padding-bottom:24px}@media(max-width:680px){.panels{grid-template-columns:repeat(2,minmax(0,1fr))}main{padding:18px 12px}section{padding:16px}}
</style><main><h1>ANIMOL · 테마별 재료와 연결 마감 theme_finish_v6</h1><p>아트 버전 6의 실제 소재·외곽·내곽·문양 패널입니다. T01_A는 v5를 유지하고 다른 19스타일의 테마별 재료 디테일과 경계 연결을 다시 정리했습니다. 몸체 명암과 경계 장식을 별도 등록해 같은 색이 잘못 추출되는 경로를 없애고, 상면·측면·하단·구멍 연결을 일정하게 구성합니다. 기존 승인 디자인의 이름과 기준 원화는 보존합니다.</p><nav><a href="DESIGN_GALLERY.html">실제 타일로 자유형 지형 그리기</a><a href="Previews/ANIMOL_theme_finish_v6_overview.png">5테마 20스타일 실제 출력</a>${approved.sources.map(s => `<a href="#${s.themeId}">${s.themeId}</a>`).join('')}</nav>${sections}<footer>이 페이지의 재료와 프레임 패널은 최종 제작 PNG입니다. Runtime 연결 Sprite는 Data/sprite_lookup.json의 아틀라스 rect와 네 전역 위상을 사용합니다. 세부 제작 출처는 SourceArt/THEME_FINISH_NATIVE_REGISTRATION_V6.json, 실제 검사는 Docs/THEME_FINISH_V6_EXPORT_QA_KO.md에 기록합니다. Unity 프로젝트 적용은 이 공간에서 실행하지 않았습니다.</footer></main></html>`;
await fs.writeFile(path.join(root, 'APPROVED_DESIGNS.html'), html);
await fs.writeFile(path.join(root, 'Docs/approved_gallery_build_v6.json'), JSON.stringify({ artVersion: 6, result: 'BUILT', themeCount: approved.sources.length, panelImages: catalog.styles.length * 4, currentNativeStyles: registration.styles.length, preservedStyle: 'T01_A v5', historicalSources, htmlBytes: Buffer.byteLength(html) }, null, 2) + '\n');
console.log(JSON.stringify({ artVersion: 6, approvedThemes: approved.sources.length, panelImages: catalog.styles.length * 4, htmlBytes: Buffer.byteLength(html) }));
