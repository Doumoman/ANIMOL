"""Handle the user's added campaign methods without introducing them into the commit."""
import json
import subprocess
from prepare_topdown_cutover import ROOT, JOURNAL

def git(*args, data=None):
    return subprocess.run(['git', *args], cwd=ROOT, input=data, check=True, stdout=subprocess.PIPE).stdout

def stage(name, content):
    blob = git('hash-object', '-w', '--stdin', data=content).decode().strip()
    git('update-index', '--add', '--cacheinfo', '100644,' + blob + ',' + name)

name = 'Assets/ANIMOL/Scripts/UI/CampaignUiPresenter.cs'
head = git('show', 'HEAD:' + name).decode('utf-8')
old = '        private void LaunchDevelopmentRun() => SceneManager.LoadScene("Gameplay");'
assert old in head, 'Baseline campaign launcher differs; review before staging'
new = '''        private void LaunchDevelopmentRun()
        {
            // Archived scene entry: Legacy/Platformer_20261013/Assets/ANIMOL/Scenes/.
#if ANIMOL_LEGACY_MAP_EDITOR
            SceneManager.LoadScene("Gameplay");
#else
            ShowToast("탑뷰 퍼즐 준비 중입니다.");
#endif
        }'''
stage(name, head.replace(old, new).encode('utf-8'))
stage('ProjectSettings/ANIMOLNamedArtReferences.json', (JOURNAL / 'namedart-head-transformed.json').read_bytes().replace(b'\r\n', b'\n'))
(JOURNAL / 'staging-failures.json').write_text('[]', encoding='utf-8')
print(json.dumps({'campaign_baseline_launcher_guarded': True, 'user_added_methods_still_uncommitted': True}))
