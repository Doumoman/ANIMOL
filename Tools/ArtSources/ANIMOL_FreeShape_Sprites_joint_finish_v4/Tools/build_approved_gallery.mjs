import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const catalog = JSON.parse(await fs.readFile(path.join(root, 'Data/style_catalog.json'), 'utf8'));
const approved = JSON.parse(await fs.readFile(path.join(root, 'Data/approved_design_sources.json'), 'utf8'));
const restoration = JSON.parse(await fs.readFile(path.join(root, 'SourceArt/BRICK_RESTORE_GENERATION.json'), 'utf8'));
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
  const revisedSource = source.themeId === 'T01'
    ? `<details open><summary>T01_A 줄눈 연결 정리 · 명암과 둥근 모서리 유지 제작 원본</summary><img class="raw" src="${escape(restoration.generatedOutput)}" alt="이번 복원 요청에 따른 T01_A 수정 출력"><p>이번 사용자 요청에 따라 수정한 출력입니다. 새 원본의 사용자 미관 승인을 완료했다는 뜻은 아닙니다. 아래 T01_A 패널과 실제 셀은 이 복원 자료를 사용합니다. <a href="SourceArt/BRICK_RESTORE_GENERATION.json">제작 출처와 참조 기록</a></p></details>`
    : '';
  sections += `<section id="${source.themeId}"><h2>${escape(styles[0].themeName)}</h2>${revisedSource}<details><summary>기존 clean_v2 승인 원화 · 기준 자료</summary><img class="raw" src="${escape(source.sourceFile)}" alt="${source.themeId} 기존 승인 기준 원화"><p>기존 승인 원화의 바이트는 보존합니다. T01_A는 이번 복원 요청에 따라 별도로 수정했고 다른 19스타일은 v3 픽셀을 유지합니다. 최종 패널과 셀은 공용 16색 출력입니다.</p></details>${cards}</section>`;
}
const html = `<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>ANIMOL 기준·복원 디자인 · joint_finish_v4</title><style>
*{box-sizing:border-box}body{margin:0;background:#121521;color:#ececf2;font:16px/1.55 system-ui,sans-serif}main{max-width:1320px;margin:auto;padding:28px 20px}h1{font-size:27px;margin:0 0 12px}h2{margin:0 0 14px;color:#ffcd75}h3{font-size:17px;margin:0 0 14px}p{max-width:850px;color:#b8c3d4}nav{display:flex;flex-wrap:wrap;gap:10px;margin:20px 0 32px}a{color:#73eff7}nav a{border:1px solid #566c86;padding:8px 14px;border-radius:7px;text-decoration:none}section{margin-bottom:40px;padding:24px;background:#1b2130;border-radius:12px}article{padding:18px 0;border-top:1px solid #333c57}.panels{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}figure{margin:0;padding:12px;background:#101218;border-radius:8px;text-align:center}figure img{image-rendering:pixelated;display:block;width:100%;height:auto;aspect-ratio:1;object-fit:contain}figcaption{padding-top:10px;font-size:13px;color:#94b0c2}details{margin:16px 0 22px}summary{cursor:pointer;color:#73eff7}.raw{display:block;width:min(100%,1000px);height:auto;margin-top:16px;background:#101218}footer{color:#94b0c2;font-size:13px;padding-bottom:24px}@media(max-width:680px){.panels{grid-template-columns:repeat(2,minmax(0,1fr))}main{padding:18px 12px}section{padding:16px}}
</style><main><h1>ANIMOL · 기준·복원 디자인 joint_finish_v4</h1><p>아트 버전 4의 소재·외곽·내곽·문양 패널입니다. T01_A는 이번 사용자 요청에 따라 벽돌 구조와 끝마감을 복원한 수정 출력이고, 다른 19스타일은 v3 픽셀을 유지합니다. 기존 승인 원화는 기준 자료로 보존하며 실제 32px 셀 조합은 자유형 갤러리에서 확인합니다.</p><nav><a href="DESIGN_GALLERY.html">실제 타일로 자유형 지형 그리기</a><a href="Previews/T01_A_joint_finish_v4_comparison.png">T01_A v3/v4 셀·지형 비교</a>${approved.sources.map(s => `<a href="#${s.themeId}">${s.themeId}</a>`).join('')}</nav>${sections}<footer>외곽·내곽 패널과 복원 원본은 디자인 확인 자료입니다. Runtime 연결 Sprite의 정확한 매핑은 Data/sprite_lookup.json을 사용합니다. Unity 프로젝트 적용 결과는 이 패키지에서 실행하지 않았습니다.</footer></main></html>`;
await fs.writeFile(path.join(root, 'APPROVED_DESIGNS.html'), html);
console.log(JSON.stringify({approvedThemes: approved.sources.length, panelImages: catalog.styles.length * 4, htmlBytes: Buffer.byteLength(html)}));
