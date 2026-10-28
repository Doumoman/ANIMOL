#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.SanctuaryStory.Editor
{
    // Text/state preview only. Never creates a scene, Canvas, EventSystem or save record.
    public sealed class SanctuaryStoryPreviewWindow : EditorWindow
    {
        private const string DefaultJson = "Assets/ANIMOL/SanctuaryStory/Data/sanctuary_chapter_v1.json";
        private TextAsset source;
        private SanctuaryFlow flow;
        private SanctuaryChapter chapter;
        private Vector2 scroll;
        private readonly List<string> log = new List<string>();
        private double lastUpdate;
        private float cueElapsed;
        private float revealElapsed;
        private int observedCueToken = -1;
        private bool autoCompleteCues = true;
        private bool paused;
        private string error;

        private static void ValidateSelected()
        {
            var selected = Selection.activeObject as TextAsset;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("성소 데이터 검증", "Project 창에서 sanctuary_chapter_v1.json을 선택하세요.", "확인");
                return;
            }
            try
            {
                var data = JsonUtility.FromJson<SanctuaryChapter>(selected.text);
                new SanctuaryFlow(data); // Includes structural and graph validation.
                int lines = 0;
                foreach (var scene in data.scenes)
                    foreach (var step in scene.steps)
                        if (step.kind == "line") lines++;
                if (data.scenes.Length != 14 || lines != 170)
                    throw new InvalidOperationException("이 성소 작업본은 14장면·170발화여야 합니다. 현재 " + data.scenes.Length + "장면·" + lines + "발화입니다.");
                Debug.Log("[Sanctuary] 데이터 검증 통과: 14장면·170발화. 실제 UI/아트/음향/저장 연결 검증과는 별개입니다.", selected);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Sanctuary] 데이터 검증 실패: " + ex.Message, selected);
            }
        }

        private static void OpenPreview()
        {
            GetWindow<SanctuaryStoryPreviewWindow>("Sanctuary Preview");
        }

        private void OnEnable()
        {
            source = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultJson);
            lastUpdate = EditorApplication.timeSinceStartup;
            EditorApplication.update += TickPreview;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TickPreview;
            if (flow != null) flow.Stop();
            flow = null;
        }

        private void LoadPreview()
        {
            if (flow != null) flow.Stop();
            flow = null;
            error = null;
            log.Clear();
            cueElapsed = revealElapsed = 0;
            observedCueToken = -1;
            paused = false;
            try
            {
                if (source == null) throw new InvalidOperationException("JSON TextAsset을 지정하세요.");
                chapter = JsonUtility.FromJson<SanctuaryChapter>(source.text);
                flow = new SanctuaryFlow(chapter);
                flow.SceneChanged += scene => AddLog("장면 " + scene.id + " · " + scene.title);
                flow.CueRequested += (cue, token) =>
                {
                    cueElapsed = 0;
                    observedCueToken = token;
                    AddLog("큐 " + cue.id + " / " + cue.duration + "초 / token " + token);
                };
                flow.ChoicesPresented += choices => { revealElapsed = 0; AddLog("선택 표시 " + choices.options.Length + "행 / " + choices.mode); };
                flow.HesitationStarted += () => AddLog("망설임: 같은 메뉴로 복귀. 새 루프 아님.");
                flow.PlaybackCompleted += () => AddLog("재생 완료 — 읽기 전용이므로 실제 명칭·보상·저장을 바꾸지 않음.");
                flow.Begin(null, true);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (flow != null) flow.Stop();
                flow = null;
            }
            Repaint();
        }

        private void TickPreview()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = Mathf.Clamp((float)(now - lastUpdate), 0, 0.1f);
            lastUpdate = now;
            if (flow == null || paused) return;
            flow.Tick(delta);
            if (flow.State == SanctuaryFlowState.PreparingChoice)
            {
                revealElapsed += delta;
                if (revealElapsed >= flow.CurrentChoices.revealSeconds) flow.ArmChoices();
            }
            if (flow.State == SanctuaryFlowState.Cue && autoCompleteCues && flow.PendingCue != null)
            {
                int token = flow.PendingCueToken;
                if (token != observedCueToken) { observedCueToken = token; cueElapsed = 0; }
                cueElapsed += delta;
                if (cueElapsed >= flow.PendingCue.duration) flow.CompleteCue(token);
            }
            Repaint();
        }

        private void AddLog(string text)
        {
            log.Add(text);
            if (log.Count > 80) log.RemoveAt(0);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("에디터 전용 대사·선택 상태 검토창입니다. 실제 CG·음향·터치·Unity 플레이 검증을 대신하지 않습니다. 캠페인 상태와 저장에는 접근하지 않습니다.", MessageType.Info);
            source = (TextAsset)EditorGUILayout.ObjectField("장면 JSON", source, typeof(TextAsset), false);
            if (GUILayout.Button("처음부터 미리보기")) LoadPreview();
            autoCompleteCues = EditorGUILayout.Toggle("큐를 시간만큼 대기 후 완료", autoCompleteCues);
            bool nextPaused = EditorGUILayout.Toggle("미리보기 일시정지", paused);
            if (nextPaused != paused)
            {
                paused = nextPaused;
                if (flow != null) flow.SetPaused(paused);
            }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (flow == null) return;
            EditorGUILayout.LabelField("상태", flow.State.ToString());
            if (flow.CurrentScene != null) EditorGUILayout.LabelField("장면", flow.CurrentScene.id + " / loopStage " + flow.CurrentScene.loopStage);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (new EditorGUI.DisabledScope(paused))
            {
                if (flow.State == SanctuaryFlowState.Line && flow.CurrentLine != null)
                {
                    EditorGUILayout.LabelField(flow.CurrentLine.speaker, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(flow.CurrentLine.text, EditorStyles.wordWrappedLabel);
                    if (!string.IsNullOrEmpty(flow.CurrentLine.delivery)) EditorGUILayout.HelpBox("작가 지시: " + flow.CurrentLine.delivery, MessageType.None);
                    if (GUILayout.Button("다음 대사")) flow.Advance();
                }
                else if (flow.State == SanctuaryFlowState.Choice && flow.CurrentChoices != null)
                {
                    var options = flow.CurrentChoices.options;
                    for (int i = 0; i < options.Length; i++)
                        if (GUILayout.Button(options[i].label)) { flow.Choose(i); break; }
                }
                else if (flow.State == SanctuaryFlowState.Cue && flow.PendingCue != null)
                {
                    EditorGUILayout.LabelField(flow.PendingCue.id, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(flow.PendingCue.description, EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("현재 큐 완료 확인")) flow.CompleteCue(flow.PendingCueToken);
                }
                else if (flow.State == SanctuaryFlowState.Completed)
                    EditorGUILayout.HelpBox(chapter.displayNameAfter + " · 읽기 전용 미리보기 완료", MessageType.Info);
                if (flow.State != SanctuaryFlowState.Idle && flow.State != SanctuaryFlowState.Completed && GUILayout.Button("현재 장면 건너뛰기")) flow.SkipScene();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("진행 로그", EditorStyles.boldLabel);
            foreach (string entry in log) EditorGUILayout.LabelField(entry, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
