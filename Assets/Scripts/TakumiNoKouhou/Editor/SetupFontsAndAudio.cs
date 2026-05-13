using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// 日本語TMPフォントの設定とSE/BGMのプロシージャル生成を行うEditorスクリプト。
    /// FirstRunSetup から SetupAll() を呼ぶ。単独実行: 匠の工法 → フォント＆オーディオ生成。
    /// </summary>
    public static class SetupFontsAndAudio
    {
        private const string AudioDir   = "Assets/Audio";
        private const string SeDir      = "Assets/Audio/SE";
        private const string BgmDir     = "Assets/Audio/BGM";
        private const string FontsDir   = "Assets/Fonts";
        private const int    SampleRate = 44100;

        [MenuItem("匠の工法/フォント＆オーディオ生成", priority = 5)]
        public static void SetupAll()
        {
            SetupJapaneseFont();
            EnsureAudioDirs();
            GenerateAllSe();
            GenerateAllBgm();
            AssetDatabase.Refresh();
            Debug.Log("[匠の工法] フォント・オーディオ生成完了");
            EditorUtility.DisplayDialog("完了", "フォント設定とSE/BGMの生成が完了しました。", "OK");
        }

        // ═══════════════════════════════════════════════════════════
        //  日本語フォント設定
        // ═══════════════════════════════════════════════════════════

        public static void SetupJapaneseFont()
        {
            // TMP Essentials がインポートされているか確認
            if (TMP_Settings.instance == null)
            {
                Debug.LogWarning("[フォント] TMP Essentials がインポートされていません。\n" +
                    "Window → TextMeshPro → Import TMP Essential Resources を実行してから再度セットアップを行ってください。");
                EditorUtility.DisplayDialog(
                    "TMP Essentials が必要です",
                    "Window → TextMeshPro → Import TMP Essential Resources を実行してから\n「匠の工法 → 初回セットアップを再実行」を行ってください。",
                    "OK");
                return;
            }

            // Windows フォントを検索（優先順）
            string[] candidates = {
                @"C:\Windows\Fonts\YuGothB.ttc",
                @"C:\Windows\Fonts\meiryo.ttc",
                @"C:\Windows\Fonts\msgothic.ttc",
            };

            string srcFontPath = null;
            foreach (var c in candidates)
                if (File.Exists(c)) { srcFontPath = c; break; }

            if (srcFontPath == null)
            {
                Debug.LogWarning("[フォント] 日本語フォントが見つかりません。手動でフォントをインポートしてください。");
                return;
            }

            if (!AssetDatabase.IsValidFolder(FontsDir))
                AssetDatabase.CreateFolder("Assets", "Fonts");

            var fontFileName = Path.GetFileName(srcFontPath);
            var destFontPath = $"{FontsDir}/{fontFileName}";
            var destFullPath = Path.GetFullPath(destFontPath);

            if (!File.Exists(destFullPath))
            {
                File.Copy(srcFontPath, destFullPath);
                AssetDatabase.Refresh();
            }

            // Font インポーターで face index 0 を指定
            var importer = AssetImporter.GetAtPath(destFontPath) as TrueTypeFontImporter;
            if (importer != null)
            {
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.SaveAndReimport();
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(destFontPath);
            if (font == null)
            {
                Debug.LogWarning($"[フォント] Font のロード失敗: {destFontPath}");
                return;
            }

            const string fontAssetPath = FontsDir + "/TMP_Japanese.asset";

            // 既存の壊れたアセットを削除してから再生成
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath) != null)
                AssetDatabase.DeleteAsset(fontAssetPath);

            var fa = TMP_FontAsset.CreateFontAsset(font);
            if (fa == null)
            {
                Debug.LogWarning("[フォント] TMP_FontAsset の生成に失敗しました");
                return;
            }

            // メインアセットとして保存し、マテリアル・アトラスをサブアセットに追加
            AssetDatabase.CreateAsset(fa, fontAssetPath);
            if (fa.material != null)
                AssetDatabase.AddObjectToAsset(fa.material, fontAssetPath);
            if (fa.atlasTextures != null)
                foreach (var tex in fa.atlasTextures)
                    if (tex != null) AssetDatabase.AddObjectToAsset(tex, fontAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(fontAssetPath);
            Debug.Log($"[フォント] TMP Font Asset 生成: {fontAssetPath}");

            // TMP_Settings のデフォルトフォントとして登録
            ApplyAsDefaultTmpFont(fa);
        }

        private static void ApplyAsDefaultTmpFont(TMP_FontAsset fontAsset)
        {
            var settings = TMP_Settings.instance;
            if (settings == null) { Debug.LogWarning("[フォント] TMP_Settings が見つかりません"); return; }

            var so   = new SerializedObject(settings);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop == null)
            {
                // フォールバック: fallback に追加
                var fallbackProp = so.FindProperty("m_fallbackFontAssets");
                if (fallbackProp != null)
                {
                    fallbackProp.arraySize++;
                    fallbackProp.GetArrayElementAtIndex(fallbackProp.arraySize - 1).objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                }
                return;
            }
            prop.objectReferenceValue = fontAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            Debug.Log("[フォント] TMP デフォルトフォントを日本語フォントに設定しました");
        }

        // ═══════════════════════════════════════════════════════════
        //  SE 生成
        // ═══════════════════════════════════════════════════════════

        private static void GenerateAllSe()
        {
            // 木材つかむ: 短い木質クリック
            SaveWav(SeDir + "/SE_Pickup.wav",       GenPickup());
            // 配置成功: 厚みのある木槌音
            SaveWav(SeDir + "/SE_PlaceSuccess.wav", GenPlaceSuccess());
            // 配置不可: 短いバズ
            SaveWav(SeDir + "/SE_PlaceFail.wav",    GenPlaceFail());
            // 継手接合: 上昇チャイム
            SaveWav(SeDir + "/SE_JointConnect.wav", GenJointConnect());
            // 地震テスト開始: 低音ランブル
            SaveWav(SeDir + "/SE_SeismicStart.wav", GenSeismicStart());
            // クリア: 3音アルペジオ
            SaveWav(SeDir + "/SE_Clear.wav",        GenClear());
            // 失敗: 下降音
            SaveWav(SeDir + "/SE_Fail.wav",         GenFail());
            // UIボタン: 軽いクリック
            SaveWav(SeDir + "/SE_ButtonClick.wav",  GenButtonClick());

            Debug.Log("[オーディオ] SE 8本 生成完了");
        }

        private static float[] GenPickup()
        {
            int n = MsToSamples(80);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-35f * t);
                d[i] = Mathf.Sin(2f * Mathf.PI * 420f * t) * env * 0.7f
                     + Noise() * env * 0.25f;
            }
            return Normalize(d);
        }

        private static float[] GenPlaceSuccess()
        {
            int n = MsToSamples(130);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-22f * t);
                // 低音 + 高調波
                d[i] = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.6f * env
                     + Mathf.Sin(2f * Mathf.PI * 360f * t) * 0.3f * env
                     + Noise() * 0.2f * env;
            }
            return Normalize(d);
        }

        private static float[] GenPlaceFail()
        {
            int n = MsToSamples(160);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-18f * t);
                float fm = Mathf.Sin(2f * Mathf.PI * 8f * t); // FM mod
                d[i] = Mathf.Sin(2f * Mathf.PI * (220f + 30f * fm) * t) * env * 0.7f;
            }
            return Normalize(d);
        }

        private static float[] GenJointConnect()
        {
            int n = MsToSamples(280);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float totalDur = n / (float)SampleRate;
                float freqSweep = Mathf.Lerp(620f, 980f, t / totalDur);
                float env = Mathf.Exp(-8f * t) * Mathf.Min(1f, t * 80f);
                d[i] = Mathf.Sin(2f * Mathf.PI * freqSweep * t) * env * 0.75f
                     + Mathf.Sin(2f * Mathf.PI * freqSweep * 2f * t) * env * 0.2f;
            }
            return Normalize(d);
        }

        private static float[] GenSeismicStart()
        {
            int n = MsToSamples(650);
            var d = new float[n];
            var rng = new System.Random(42);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float rise = Mathf.Clamp01(t / 0.3f);
                float freq = 60f + 20f * rise;
                d[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * rise * 0.5f
                     + (float)(rng.NextDouble() * 2.0 - 1.0) * rise * 0.3f;
            }
            return Normalize(d);
        }

        private static float[] GenClear()
        {
            // ド・ミ・ソ アルペジオ（C5=523Hz, E5=659Hz, G5=784Hz）
            float[] noteFreqs = { 523.25f, 659.26f, 783.99f };
            float noteDur = 0.25f;
            int noteLen = MsToSamples((int)(noteDur * 1000));
            int tailLen = MsToSamples(200);
            int total = noteLen * 3 + tailLen;
            var d = new float[total];

            for (int ni = 0; ni < noteFreqs.Length; ni++)
            {
                float freq = noteFreqs[ni];
                int offset = ni * noteLen;
                for (int i = 0; i < noteLen + tailLen; i++)
                {
                    int idx = offset + i;
                    if (idx >= total) break;
                    float t = (float)i / SampleRate;
                    float env = Mathf.Exp(-5f * t) * Mathf.Min(1f, t * 60f);
                    d[idx] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.45f
                            + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * env * 0.15f;
                }
            }
            return Normalize(d);
        }

        private static float[] GenFail()
        {
            int n = MsToSamples(420);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float totalDur = n / (float)SampleRate;
                float freqSweep = Mathf.Lerp(380f, 80f, t / totalDur);
                float env = Mathf.Exp(-8f * t) * Mathf.Min(1f, t * 40f);
                d[i] = Mathf.Sin(2f * Mathf.PI * freqSweep * t) * env * 0.7f
                     + Noise() * env * 0.15f;
            }
            return Normalize(d);
        }

        private static float[] GenButtonClick()
        {
            int n = MsToSamples(55);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-70f * t);
                d[i] = Mathf.Sin(2f * Mathf.PI * 1100f * t) * env * 0.6f;
            }
            return Normalize(d);
        }

        // ═══════════════════════════════════════════════════════════
        //  BGM 生成（都節音階ベースのプロシージャル和風BGM）
        // ═══════════════════════════════════════════════════════════

        private static void GenerateAllBgm()
        {
            // 都節音階: E, F, A, B, D (0, 1, 5, 7, 10 半音)
            // タイトル BGM: 72BPM ゆったり 8小節
            int[] titleMelody = {
                0, -1, 5, -1,  7,  5,  1, 0,
                0, -1, 5,  7, 10,  7,  5, -1,
                5, -1, 7, -1,  5,  1,  0, -1,
                0,  1, 5,  7,  5,  0, -1, -1,
            };
            float titleBaseHz = FreqFromNote(4, 4); // E4 = 329.63Hz
            SaveWav(BgmDir + "/BGM_Title.wav",    GenBgm(titleMelody, titleBaseHz, 72f, 0.25f));

            // ゲームプレイ BGM: 90BPM 少しテンション感 8小節
            int[] gameMelody = {
                5, -1,  7,  5,  1,  0, -1, -1,
                5,  7, 10,  7,  5, -1, 5,  -1,
                7, -1, 10,  7,  5,  7,  5,  1,
                0,  1,  5, -1,  0, -1, -1, -1,
            };
            float gameBaseHz = FreqFromNote(3, 4); // B3 = 246.94Hz
            SaveWav(BgmDir + "/BGM_Gameplay.wav", GenBgm(gameMelody, gameBaseHz, 92f, 0.22f));

            Debug.Log("[オーディオ] BGM 2本 生成完了");
        }

        /// <summary>
        /// メロディ配列（半音オフセット、-1=休符）とドローンから BGM バッファを生成する。
        /// </summary>
        private static float[] GenBgm(int[] melody, float baseHz, float bpm, float melVol)
        {
            float beatSec   = 60f / bpm;
            int   beatLen   = (int)(SampleRate * beatSec);
            int   totalLen  = beatLen * melody.Length;
            var   d         = new float[totalLen];

            // ── ドローン（低音連続） ──
            float droneHz = baseHz * 0.5f;
            for (int i = 0; i < totalLen; i++)
            {
                float t = (float)i / SampleRate;
                d[i]  = Mathf.Sin(2f * Mathf.PI * droneHz * t) * 0.12f;
                d[i] += Mathf.Sin(2f * Mathf.PI * droneHz * 2f * t) * 0.06f;
            }

            // ── メロディノート ──
            for (int ni = 0; ni < melody.Length; ni++)
            {
                if (melody[ni] < 0) continue; // 休符

                float noteHz  = baseHz * Mathf.Pow(2f, melody[ni] / 12f);
                float attackS = 0.015f;
                float holdS   = beatSec * 0.72f;
                int   noteStart = ni * beatLen;
                int   noteEnd   = noteStart + (int)(SampleRate * (holdS + 0.15f));
                if (noteEnd > totalLen) noteEnd = totalLen;

                for (int i = noteStart; i < noteEnd; i++)
                {
                    float t   = (float)(i - noteStart) / SampleRate;
                    float atk = Mathf.Min(1f, t / attackS);
                    float env = atk * Mathf.Exp(-4.5f * Mathf.Max(0f, t - attackS - holdS));
                    float sample = Mathf.Sin(2f * Mathf.PI * noteHz * t)
                                 + 0.28f * Mathf.Sin(2f * Mathf.PI * noteHz * 2f * t)
                                 + 0.10f * Mathf.Sin(2f * Mathf.PI * noteHz * 3f * t);
                    d[i] += sample * env * melVol;
                }
            }

            return Normalize(d, 0.88f);
        }

        // ═══════════════════════════════════════════════════════════
        //  WAV 書き込み
        // ═══════════════════════════════════════════════════════════

        private static void SaveWav(string assetPath, float[] samples)
        {
            var fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            using var fs = new FileStream(fullPath, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            int numSamples = samples.Length;
            int dataSize   = numSamples * 2; // 16-bit mono

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);         // PCM
            bw.Write((short)1);         // mono
            bw.Write(SampleRate);
            bw.Write(SampleRate * 2);   // byte rate
            bw.Write((short)2);         // block align
            bw.Write((short)16);        // bits per sample
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            foreach (float s in samples)
                bw.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
        }

        // ═══════════════════════════════════════════════════════════
        //  ユーティリティ
        // ═══════════════════════════════════════════════════════════

        private static void EnsureAudioDirs()
        {
            foreach (var dir in new[] { AudioDir, SeDir, BgmDir })
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    var parts   = dir.Split('/');
                    string cur  = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string next = cur + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(next))
                            AssetDatabase.CreateFolder(cur, parts[i]);
                        cur = next;
                    }
                }
            }
        }

        private static int MsToSamples(int ms) => SampleRate * ms / 1000;

        private static readonly System.Random _rng = new System.Random(12345);
        private static float Noise() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        private static float FreqFromNote(int semitone, int octave)
        {
            // E = 4 semitones above C; A440 base
            // C4=261.63Hz
            float c4 = 261.63f;
            int   totalSemitones = (octave - 4) * 12 + semitone;
            return c4 * Mathf.Pow(2f, totalSemitones / 12f);
        }

        private static float[] Normalize(float[] d, float target = 0.92f)
        {
            float maxVal = 0f;
            foreach (float s in d) if (Mathf.Abs(s) > maxVal) maxVal = Mathf.Abs(s);
            if (maxVal < 0.001f) return d;
            float scale = target / maxVal;
            for (int i = 0; i < d.Length; i++) d[i] *= scale;
            return d;
        }

        /// <summary>生成したオーディオクリップを GameAudioManager の SerializeField に自動接続する</summary>
        public static void WireAudioToManager(GameObject audioManagerGO)
        {
            if (audioManagerGO == null) return;

            var mgr = audioManagerGO.GetOrAddComponent<GameAudioManager>();
            var so  = new SerializedObject(mgr);

            void Set(string prop, string clipPath)
            {
                var p = so.FindProperty(prop);
                if (p == null) return;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null) p.objectReferenceValue = clip;
            }

            Set("titleBgm",      BgmDir + "/BGM_Title.wav");
            Set("gameplayBgm",   BgmDir + "/BGM_Gameplay.wav");
            Set("sePickup",      SeDir  + "/SE_Pickup.wav");
            Set("sePlaceSuccess",SeDir  + "/SE_PlaceSuccess.wav");
            Set("sePlaceFail",   SeDir  + "/SE_PlaceFail.wav");
            Set("seJointConnect",SeDir  + "/SE_JointConnect.wav");
            Set("seSeismicStart",SeDir  + "/SE_SeismicStart.wav");
            Set("seClear",       SeDir  + "/SE_Clear.wav");
            Set("seFail",        SeDir  + "/SE_Fail.wav");
            Set("seButtonClick", SeDir  + "/SE_ButtonClick.wav");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mgr);
        }
    }
}
