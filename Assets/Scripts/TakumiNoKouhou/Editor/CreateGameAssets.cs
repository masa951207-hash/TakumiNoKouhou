using UnityEngine;
using UnityEditor;
using System.IO;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// 匠の工法で使用する全ScriptableObjectを一括生成するEditorスクリプト。
    /// Unity メニュー → 匠の工法 → 全アセット生成 から実行する。
    /// </summary>
    public static class CreateGameAssets
    {
        private const string JointDir  = "Assets/TakumiNoKouhou/ScriptableObjects/Joints";
        private const string PieceDir  = "Assets/TakumiNoKouhou/ScriptableObjects/Pieces";
        private const string StageDir  = "Assets/TakumiNoKouhou/ScriptableObjects/Stages";

        [MenuItem("匠の工法/全アセット生成", priority = 1)]
        public static void CreateAll()
        {
            EnsureDirs();
            CreateJointTypes();
            CreateWoodPieces();
            AssetDatabase.SaveAssets(); // ピースアセットをディスクに書き込んでからステージで参照する
            CreateStages();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[匠の工法] 全ScriptableObject生成完了");
            EditorUtility.DisplayDialog("生成完了",
                "継手データ×4\n木材ピース×8\nステージデータ×3\nを生成しました。", "OK");
        }

        // ─────────────────── 継手データ ───────────────────

        private static void CreateJointTypes()
        {
            // ほぞ継ぎ
            var mortise = CreateOrLoad<JointTypeData>($"{JointDir}/JointData_Mortise.asset");
            mortise.jointName       = "ほぞ継ぎ";
            mortise.description     = "四角い突起（ほぞ）を穴（ほぞ穴）に差し込む基本の継手。\n柱と梁の接合に広く使われ、圧縮力に優れる。";
            mortise.shapeType       = JointShapeType.Mortise;
            mortise.compressionStrength  = 0.85f;
            mortise.tensionStrength      = 0.40f;
            mortise.shearStrength        = 0.55f;
            mortise.bendingFlexibility   = 0.20f;
            mortise.dampingCoefficient   = 0.30f;
            mortise.requiredThickness    = 0.5f;
            mortise.difficulty           = 1;
            mortise.baseColor            = new Color(0.75f, 0.55f, 0.30f);
            EditorUtility.SetDirty(mortise);

            // 蟻継ぎ
            var dovetail = CreateOrLoad<JointTypeData>($"{JointDir}/JointData_Dovetail.asset");
            dovetail.jointName      = "蟻継ぎ";
            dovetail.description    = "台形（蟻形）の突起を使った継手。\n引張方向の力に強く、横からの抜けを防ぐ。";
            dovetail.shapeType      = JointShapeType.Dovetail;
            dovetail.compressionStrength = 0.60f;
            dovetail.tensionStrength     = 0.80f;
            dovetail.shearStrength       = 0.65f;
            dovetail.bendingFlexibility  = 0.25f;
            dovetail.dampingCoefficient  = 0.35f;
            dovetail.requiredThickness   = 0.6f;
            dovetail.difficulty          = 2;
            dovetail.baseColor           = new Color(0.65f, 0.45f, 0.25f);
            EditorUtility.SetDirty(dovetail);

            // 鎌継ぎ
            var sickle = CreateOrLoad<JointTypeData>($"{JointDir}/JointData_Sickle.asset");
            sickle.jointName       = "鎌継ぎ";
            sickle.description     = "鎌（かま）の形をした複雑な継手。\n引張とせん断を同時に受け持ち、しなりで地震力を分散する。";
            sickle.shapeType       = JointShapeType.Sickle;
            sickle.compressionStrength  = 0.65f;
            sickle.tensionStrength      = 0.75f;
            sickle.shearStrength        = 0.70f;
            sickle.bendingFlexibility   = 0.70f;
            sickle.dampingCoefficient   = 0.65f;
            sickle.requiredThickness    = 0.7f;
            sickle.difficulty           = 3;
            sickle.baseColor            = new Color(0.55f, 0.38f, 0.20f);
            EditorUtility.SetDirty(sickle);

            // 相欠き継ぎ
            var splice = CreateOrLoad<JointTypeData>($"{JointDir}/JointData_Splice.asset");
            splice.jointName       = "相欠き継ぎ";
            splice.description     = "互いに半分ずつ欠いて噛み合わせる簡易な継手。\n加工しやすく、まず力の流れを学ぶ入門継手。";
            splice.shapeType       = JointShapeType.Splice;
            splice.compressionStrength  = 0.55f;
            splice.tensionStrength      = 0.45f;
            splice.shearStrength        = 0.50f;
            splice.bendingFlexibility   = 0.40f;
            splice.dampingCoefficient   = 0.35f;
            splice.requiredThickness    = 0.4f;
            splice.difficulty           = 1;
            splice.baseColor            = new Color(0.80f, 0.62f, 0.38f);
            EditorUtility.SetDirty(splice);
        }

        // ─────────────────── 木材ピースデータ ───────────────────

        private static void CreateWoodPieces()
        {
            // 杉・桁（横架材）
            var sugiKeta = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Sugi_Keta.asset");
            sugiKeta.pieceName       = "杉・桁";
            sugiKeta.description     = "屋根や床を支える横方向の主要架構材。";
            sugiKeta.species         = WoodSpecies.Sugi;
            sugiKeta.length          = 3f;
            sugiKeta.width           = 0.4f;
            sugiKeta.height          = 0.6f;
            sugiKeta.density         = 380f;
            sugiKeta.youngsModulus   = 7500f;
            sugiKeta.bendingStrength = 35f;
            sugiKeta.compressionStrength = 25f;
            sugiKeta.slots           = BuildKetaSlots();
            EditorUtility.SetDirty(sugiKeta);

            // 杉・柱
            var sugiHashira = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Sugi_Hashira.asset");
            sugiHashira.pieceName       = "杉・柱";
            sugiHashira.description     = "鉛直荷重を地盤に伝える垂直材。";
            sugiHashira.species         = WoodSpecies.Sugi;
            sugiHashira.length          = 1f;
            sugiHashira.width           = 0.4f;
            sugiHashira.height          = 3f;
            sugiHashira.density         = 380f;
            sugiHashira.youngsModulus   = 7500f;
            sugiHashira.bendingStrength = 35f;
            sugiHashira.compressionStrength = 28f;
            sugiHashira.slots           = BuildHashiraSlots();
            EditorUtility.SetDirty(sugiHashira);

            // 檜・桁
            var hinokiKeta = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Hinoki_Keta.asset");
            hinokiKeta.pieceName       = "檜・桁";
            hinokiKeta.description     = "耐久性の高い檜の横架材。高強度で香り高い。";
            hinokiKeta.species         = WoodSpecies.Hinoki;
            hinokiKeta.length          = 3f;
            hinokiKeta.width           = 0.4f;
            hinokiKeta.height          = 0.6f;
            hinokiKeta.density         = 450f;
            hinokiKeta.youngsModulus   = 9500f;
            hinokiKeta.bendingStrength = 45f;
            hinokiKeta.compressionStrength = 32f;
            hinokiKeta.slots           = BuildKetaSlots();
            EditorUtility.SetDirty(hinokiKeta);

            // 檜・柱
            var hinokiHashira = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Hinoki_Hashira.asset");
            hinokiHashira.pieceName       = "檜・柱";
            hinokiHashira.description     = "神社仏閣にも使われる高品質な柱材。";
            hinokiHashira.species         = WoodSpecies.Hinoki;
            hinokiHashira.length          = 1f;
            hinokiHashira.width           = 0.4f;
            hinokiHashira.height          = 3f;
            hinokiHashira.density         = 450f;
            hinokiHashira.youngsModulus   = 9500f;
            hinokiHashira.bendingStrength = 45f;
            hinokiHashira.compressionStrength = 36f;
            hinokiHashira.slots           = BuildHashiraSlots();
            EditorUtility.SetDirty(hinokiHashira);

            // 松・貫（横つなぎ材）
            var matsuNuki = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Matsu_Nuki.asset");
            matsuNuki.pieceName       = "松・貫";
            matsuNuki.description     = "柱を横方向に貫いて固める薄い板状の材。剛性を高める。";
            matsuNuki.species         = WoodSpecies.Matsu;
            matsuNuki.length          = 4f;
            matsuNuki.width           = 0.15f;
            matsuNuki.height          = 0.3f;
            matsuNuki.density         = 550f;
            matsuNuki.youngsModulus   = 11000f;
            matsuNuki.bendingStrength = 50f;
            matsuNuki.compressionStrength = 38f;
            matsuNuki.slots           = BuildNukiSlots();
            EditorUtility.SetDirty(matsuNuki);

            // 欅・梁（大型断面材）
            var keyakiHari = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Keyaki_Hari.asset");
            keyakiHari.pieceName       = "欅・梁";
            keyakiHari.description     = "長スパンを渡す欅の大梁。非常に高強度。";
            keyakiHari.species         = WoodSpecies.Keyaki;
            keyakiHari.length          = 4f;
            keyakiHari.width           = 0.5f;
            keyakiHari.height          = 0.8f;
            keyakiHari.density         = 700f;
            keyakiHari.youngsModulus   = 13000f;
            keyakiHari.bendingStrength = 65f;
            keyakiHari.compressionStrength = 50f;
            keyakiHari.slots           = BuildKetaSlots();
            EditorUtility.SetDirty(keyakiHari);

            // 杉・筋交い（斜め耐震材）
            var sugiSujikaui = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Sugi_Sujikaui.asset");
            sugiSujikaui.pieceName       = "杉・筋交い";
            sugiSujikaui.description     = "柱と梁の間に斜めに入れる耐震材。\n横揺れに対して強く、地震力を斜め圧縮で受ける。";
            sugiSujikaui.species         = WoodSpecies.Sugi;
            sugiSujikaui.length          = 2f;
            sugiSujikaui.width           = 0.3f;
            sugiSujikaui.height          = 0.3f;
            sugiSujikaui.density         = 380f;
            sugiSujikaui.youngsModulus   = 7500f;
            sugiSujikaui.bendingStrength = 30f;
            sugiSujikaui.compressionStrength = 35f;
            sugiSujikaui.slots           = BuildSujikauiSlots();
            EditorUtility.SetDirty(sugiSujikaui);

            // 松・母屋（屋根中間横架材）
            var matsuMoya = CreateOrLoad<WoodPieceData>($"{PieceDir}/Piece_Matsu_Moya.asset");
            matsuMoya.pieceName       = "松・母屋";
            matsuMoya.description     = "屋根の中間を支える横架材（桁と棟木の間）。\n垂木を受け、屋根荷重を小屋束に伝える。";
            matsuMoya.species         = WoodSpecies.Matsu;
            matsuMoya.length          = 2f;
            matsuMoya.width           = 0.4f;
            matsuMoya.height          = 0.4f;
            matsuMoya.density         = 550f;
            matsuMoya.youngsModulus   = 11000f;
            matsuMoya.bendingStrength = 48f;
            matsuMoya.compressionStrength = 36f;
            matsuMoya.slots           = BuildKetaSlots();
            EditorUtility.SetDirty(matsuMoya);
        }

        // ─────────────────── スロット定義ヘルパー ───────────────────

        private static JointSlotDefinition[] BuildKetaSlots()
        {
            return new JointSlotDefinition[]
            {
                new JointSlotDefinition
                {
                    slotId = "left",
                    role = SlotRole.Mortise,
                    direction = SlotDirection.NegX,
                    acceptedShape = JointShapeType.Mortise,
                    localPosition = new Vector3(-1.5f, 0f, 0f),
                    depth = 0.2f, slotWidth = 0.15f, isRequired = true
                },
                new JointSlotDefinition
                {
                    slotId = "right",
                    role = SlotRole.Mortise,
                    direction = SlotDirection.PosX,
                    acceptedShape = JointShapeType.Mortise,
                    localPosition = new Vector3(1.5f, 0f, 0f),
                    depth = 0.2f, slotWidth = 0.15f, isRequired = true
                }
            };
        }

        private static JointSlotDefinition[] BuildHashiraSlots()
        {
            return new JointSlotDefinition[]
            {
                new JointSlotDefinition
                {
                    slotId = "top",
                    role = SlotRole.Tenon,
                    direction = SlotDirection.PosY,
                    acceptedShape = JointShapeType.Mortise,
                    localPosition = new Vector3(0f, 1.5f, 0f),
                    depth = 0.2f, slotWidth = 0.15f, isRequired = true
                },
                new JointSlotDefinition
                {
                    slotId = "bottom",
                    role = SlotRole.Tenon,
                    direction = SlotDirection.NegY,
                    acceptedShape = JointShapeType.Mortise,
                    localPosition = new Vector3(0f, -1.5f, 0f),
                    depth = 0.2f, slotWidth = 0.15f, isRequired = false
                }
            };
        }

        private static JointSlotDefinition[] BuildSujikauiSlots()
        {
            return new JointSlotDefinition[]
            {
                new JointSlotDefinition
                {
                    slotId = "bottom_left",
                    role = SlotRole.Either,
                    direction = SlotDirection.NegX,
                    acceptedShape = JointShapeType.Mortise,
                    localPosition = new Vector3(-1f, -1f, 0f),
                    depth = 0.15f, slotWidth = 0.12f, isRequired = true
                },
                new JointSlotDefinition
                {
                    slotId = "top_right",
                    role = SlotRole.Either,
                    direction = SlotDirection.PosX,
                    acceptedShape = JointShapeType.Mortise,
                    localPosition = new Vector3(1f, 1f, 0f),
                    depth = 0.15f, slotWidth = 0.12f, isRequired = true
                }
            };
        }

        private static JointSlotDefinition[] BuildNukiSlots()
        {
            return new JointSlotDefinition[]
            {
                new JointSlotDefinition
                {
                    slotId = "left",
                    role = SlotRole.Either,
                    direction = SlotDirection.NegX,
                    acceptedShape = JointShapeType.Splice,
                    localPosition = new Vector3(-2f, 0f, 0f),
                    depth = 0.1f, slotWidth = 0.12f, isRequired = false
                },
                new JointSlotDefinition
                {
                    slotId = "right",
                    role = SlotRole.Either,
                    direction = SlotDirection.PosX,
                    acceptedShape = JointShapeType.Splice,
                    localPosition = new Vector3(2f, 0f, 0f),
                    depth = 0.1f, slotWidth = 0.12f, isRequired = false
                }
            };
        }

        // ─────────────────── ステージデータ ───────────────────

        private static void CreateStages()
        {
            // ピースアセット参照を取得（CreateWoodPieces後なので必ず存在する）
            var sugiHashira   = AssetDatabase.LoadAssetAtPath<WoodPieceData>($"{PieceDir}/Piece_Sugi_Hashira.asset");
            var sugiKeta      = AssetDatabase.LoadAssetAtPath<WoodPieceData>($"{PieceDir}/Piece_Sugi_Keta.asset");
            var hinokiHashira = AssetDatabase.LoadAssetAtPath<WoodPieceData>($"{PieceDir}/Piece_Hinoki_Hashira.asset");
            var hinokiKeta    = AssetDatabase.LoadAssetAtPath<WoodPieceData>($"{PieceDir}/Piece_Hinoki_Keta.asset");
            var matsuNuki     = AssetDatabase.LoadAssetAtPath<WoodPieceData>($"{PieceDir}/Piece_Matsu_Nuki.asset");
            var keyakiHari    = AssetDatabase.LoadAssetAtPath<WoodPieceData>($"{PieceDir}/Piece_Keyaki_Hari.asset");

            // ── ステージ1：ほぞ継ぎ入門 ──
            var stage1 = CreateOrLoad<PuzzleStageData>($"{StageDir}/Stage_001.asset");
            stage1.stageNumber     = 1;
            stage1.stageName       = "柱と梁の初仕事";
            stage1.stageDescription =
                "「まずは基本のほぞ継ぎから。\n柱の上にほぞを立て、桁のほぞ穴に差し込め。\n圧縮の力はそこに流れる。」";
            stage1.unlockAfterStage = 0;
            stage1.gridSize         = new Vector2Int(4, 4);
            stage1.verticalLoad     = 15f;
            stage1.seismicIntensity = 0.2f;
            stage1.seismicDuration  = 15f;
            stage1.minimumPassGrade = StructuralGrade.C;
            stage1.clearMessage     =
                "「見事。ほぞが圧縮力をしっかり受け止めた。\n柱と梁が一体になった瞬間が分かったか？」";
            stage1.failMessage      =
                "「ほぞとほぞ穴の向きが合っていない。\n圧縮力が流れる方向をもう一度確かめよ。」";
            stage1.hints = new[]
            {
                "柱のほぞ（突起）を桁のほぞ穴（穴）に向かって差し込もう",
                "力は上から下へ流れる。柱は鉛直、桁は水平に配置する",
                "赤く光る部分が圧縮を受けている箇所だ"
            };
            // ── 手持ちピース（グリッド外のステージングエリアに生成される） ──
            stage1.availablePieces = new WoodPieceInventoryEntry[]
            {
                new WoodPieceInventoryEntry { pieceData = sugiHashira, count = 2 },
                new WoodPieceInventoryEntry { pieceData = sugiKeta,    count = 1 }
            };
            stage1.anchorPieces = new AnchorPieceEntry[0];
            EditorUtility.SetDirty(stage1);

            // ── ステージ2：蟻継ぎの引張 ──
            var stage2 = CreateOrLoad<PuzzleStageData>($"{StageDir}/Stage_002.asset");
            stage2.stageNumber      = 2;
            stage2.stageName        = "引張と蟻の形";
            stage2.stageDescription =
                "「地震は横から揺らす。\n引張力が生まれる場所に蟻継ぎを使え。\n台形の形が抜けを防ぐのだ。」";
            stage2.unlockAfterStage = 1;
            stage2.requiredGrade    = StructuralGrade.C;
            stage2.gridSize         = new Vector2Int(5, 4);
            stage2.verticalLoad     = 20f;
            stage2.seismicIntensity = 0.35f;
            stage2.seismicDuration  = 20f;
            stage2.minimumPassGrade = StructuralGrade.C;
            stage2.clearMessage     =
                "「蟻形の力を理解したか。\n引張に逆らわず、形で受け止める。それが木の知恵だ。」";
            stage2.failMessage      =
                "「引張が生じる部位に蟻継ぎを置いていない。\n青く光る引張部分を確認せよ。」";
            stage2.hints = new[]
            {
                "横揺れが加わると、梁の端部に引張力が発生する",
                "蟻継ぎは青（引張）の場所に配置すると効果的",
                "力の流れを表示して、どこに引張が集中しているか確認しよう"
            };
            stage2.availablePieces = new WoodPieceInventoryEntry[]
            {
                new WoodPieceInventoryEntry { pieceData = hinokiHashira, count = 2 },
                new WoodPieceInventoryEntry { pieceData = hinokiKeta,    count = 1 },
                new WoodPieceInventoryEntry { pieceData = matsuNuki,     count = 1 }
            };
            stage2.anchorPieces = new AnchorPieceEntry[0];
            EditorUtility.SetDirty(stage2);

            // ── ステージ3：鎌継ぎのしなり ──
            var stage3 = CreateOrLoad<PuzzleStageData>($"{StageDir}/Stage_003.asset");
            stage3.stageNumber      = 3;
            stage3.stageName        = "鎌のしなり、共振を断て";
            stage3.stageDescription =
                "「強い地震は共振を生む。\n剛すぎる構造は破れる。\n鎌継ぎのしなりで揺れを吸収せよ。」";
            stage3.unlockAfterStage = 2;
            stage3.requiredGrade    = StructuralGrade.C;
            stage3.gridSize         = new Vector2Int(6, 5);
            stage3.verticalLoad     = 25f;
            stage3.seismicIntensity = 0.5f;
            stage3.seismicDuration  = 25f;
            stage3.minimumPassGrade = StructuralGrade.B;
            stage3.clearMessage     =
                "「見事な木組みだ。\n鎌のしなりが共振を断ち、揺れを逃がした。\nこれが匠の工法だ。」";
            stage3.failMessage      =
                "「共振が起きた。固有振動数を下げるには\n鎌継ぎでしなりを加えることだ。\n共振リスク表示を確認せよ。」";
            stage3.hints = new[]
            {
                "共振リスクが赤い場合、鎌継ぎを追加してダンピングを上げよう",
                "鎌継ぎは長材の接合に使う。しなり係数が高い",
                "固有振動数が地震周波数（1Hz付近）から離れると安全になる",
                "ほぞ+蟻+鎌を組み合わせた構造が理想的"
            };
            stage3.availablePieces = new WoodPieceInventoryEntry[]
            {
                new WoodPieceInventoryEntry { pieceData = sugiHashira, count = 2 },
                new WoodPieceInventoryEntry { pieceData = keyakiHari,  count = 1 },
                new WoodPieceInventoryEntry { pieceData = matsuNuki,   count = 2 }
            };
            stage3.anchorPieces = new AnchorPieceEntry[0];
            EditorUtility.SetDirty(stage3);
        }

        // ─────────────────── ユーティリティ ───────────────────

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDirs()
        {
            foreach (var dir in new[] { JointDir, PieceDir, StageDir })
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    var parts = dir.Split('/');
                    string current = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string next = current + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(next))
                            AssetDatabase.CreateFolder(current, parts[i]);
                        current = next;
                    }
                }
            }
        }
    }
}
