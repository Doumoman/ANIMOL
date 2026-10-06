(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const names = ['월궁', '구름고래 목장', '별가루 도서관', '시간유리 온실', '오로라 수정광산'];
  const meta = { rabbit: { frameWidth: 64, frameHeight: 96, footY: 90 }, floorY: 526 };
  const visible = $('view'), ctx = visible.getContext('2d', { alpha: false });
  const output = document.createElement('canvas'); output.width = 352; output.height = 704;
  const renderers = {}, images = {};
  let ready = false, screen = 'main', variant = 'corrected', paused = false;
  let started = performance.now(), held = 0, campaignImage;
  const clock = () => paused ? held : held + (performance.now() - started) / 1000;
  function pause(value) {
    if (value === paused) return;
    if (value) held = clock(); else started = performance.now();
    paused = value; $('pause').textContent = value ? '재생' : '일시 정지';
  }
  function fit() {
    const detail = screen === 'main' && $('detail').checked;
    const height = detail ? 250 : 704, zoom = Number($('zoom').value);
    visible.width = 352; visible.height = height;
    visible.style.width = (352 * zoom) + 'px'; visible.style.height = (height * zoom) + 'px';
    ctx.imageSmoothingEnabled = false;
    $('dimensions').textContent = '352 × ' + height + ' 원본 도트 · ' + zoom + '배 최근접 표시';
  }
  function setScreen(value) {
    screen = value;
    $('main-tab').setAttribute('aria-pressed', String(value === 'main'));
    $('campaign-tab').setAttribute('aria-pressed', String(value === 'campaign'));
    for (const id of ['variant', 'theme', 'near', 'rabbit', 'detail', 'pause', 'restart']) $(id).disabled = value === 'campaign';
    $('screen-caption').textContent = value === 'main'
      ? '기존 형태를 유지한 배경 레이어 비교. 같은 토끼 시트를 공통으로 사용합니다.'
      : '밝은 카드 배킹을 복원한 캠페인 디자인 참고 합성. 진행도·상태는 연결되지 않았습니다.';
    $('back').hidden = value !== 'campaign'; fit();
  }
  function draw() {
    if (!ready) { requestAnimationFrame(draw); return; }
    const t = clock(), selection = Number($('theme').value);
    const theme = selection < 0 ? Math.floor(t / 5) % 5 : selection;
    ctx.imageSmoothingEnabled = false;
    if (screen === 'campaign') {
      ctx.drawImage(campaignImage, 0, 0);
      $('state').textContent = '캠페인 UI · 디자인 참고용\n진행도·서버 미연결 · 실제 Unity 적용/실행 미확인';
    } else if ($('detail').checked) {
      ctx.fillStyle = '#41a6f6'; ctx.fillRect(0, 0, 352, 250);
      ctx.drawImage(images[variant][theme].platform, 0, 454, 352, 250, 0, 0, 352, 250);
      $('state').textContent = names[theme] + ' · 다리 PNG만 정지 표시\n윤곽·기둥·난간 구멍 확인용 · 청색은 투명 영역 배킹';
    } else {
      const state = renderers[variant].render(t, selection, { hideNear: !$('near').checked, hideRabbit: !$('rabbit').checked });
      ctx.drawImage(output, 0, 0);
      $('state').textContent = names[state.theme] + ' · ' + (paused ? '일시 정지' : '재생 중')
        + '\n발판 고정 · 5초 테마 전환 · 0.36초 도트 전환\n서비스 호출·저장 없음 · 실제 Unity 적용/실행 미확인';
    }
    requestAnimationFrame(draw);
  }
  async function init() {
    for (const key of ['original', 'previous', 'corrected']) {
      const data = Object.assign({}, window.PREVIEW_DATA.backgrounds[key], window.PREVIEW_DATA.rabbit);
      images[key] = await AnimolBackgrounds.loadEmbedded(data);
      renderers[key] = AnimolBackgrounds.create(images[key], meta, output);
    }
    campaignImage = await new Promise((resolve, reject) => {
      const im = new Image(); im.onload = () => resolve(im);
      im.onerror = () => reject(new Error('캠페인 참고 이미지 로드 실패'));
      im.src = window.PREVIEW_DATA.campaign;
    });
    ready = true; $('loading').hidden = true;
  }
  $('main-tab').onclick = () => setScreen('main');
  $('campaign-tab').onclick = () => setScreen('campaign');
  $('back').onclick = () => setScreen('main');
  $('variant').onchange = () => { variant = $('variant').value; };
  $('zoom').onchange = fit;
  $('pause').onclick = () => pause(!paused);
  $('restart').onclick = () => { held = 0; started = performance.now(); };
  $('detail').onchange = () => { if ($('detail').checked) pause(true); fit(); };
  setScreen('main'); draw();
  init().catch(error => { $('loading').textContent = '미리보기 로드 오류: ' + error.message; });
  window.AnimolCorrectionPreview = { meta, clock, setScreen, get ready() { return ready; },
    get sourceHashes() { return window.PREVIEW_DATA.inputHashes; } };
})();
