using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 2つの継手スロットが力学的・形状的に適合するか検証するクラス。
    /// SlotSystemの接続判定に加え、接合精度スコアを計算してStructuralEvaluatorに渡す。
    /// </summary>
    public class JointCompatibilityChecker : MonoBehaviour
    {
        [Header("継手データ参照")]
        [Tooltip("使用可能な継手タイプデータの一覧（Inspectorから登録）")]
        [SerializeField] private JointTypeData[] jointTypeDatabase;

        [Header("精度評価設定")]
        [Tooltip("スロット位置の許容誤差（メートル）。この値以下なら精密接合と判定。")]
        [SerializeField] private float precisionThreshold = 0.01f;

        [Tooltip("方向の許容誤差（度）。この値以下なら方向一致と判定。")]
        [SerializeField] private float directionThreshold = 5f;

        // 形状種別 → JointTypeDataのルックアップキャッシュ
        private Dictionary<JointShapeType, JointTypeData> _dataByShape = new();

        void Awake()
        {
            foreach (var data in jointTypeDatabase)
                if (data != null) _dataByShape[data.shapeType] = data;
        }

        /// <summary>
        /// 接続済みスロットペアの一覧から各接合の評価を計算して返す。
        /// </summary>
        public List<JointEvaluation> EvaluateAllConnections(List<SlotConnection> connections)
        {
            var results = new List<JointEvaluation>();

            foreach (var conn in connections)
            {
                var eval = EvaluateConnection(conn);
                results.Add(eval);
            }

            return results;
        }

        /// <summary>1つのスロット接続を評価する</summary>
        public JointEvaluation EvaluateConnection(SlotConnection conn)
        {
            var eval = new JointEvaluation
            {
                connection = conn,
                jointShape = conn.JointShape
            };

            // 対応するJointTypeDataを取得
            if (!_dataByShape.TryGetValue(conn.JointShape, out var typeData))
            {
                eval.overallScore = 0f;
                eval.isCompatible = false;
                eval.notes = "継手データが登録されていません";
                return eval;
            }

            eval.jointTypeData = typeData;
            eval.isCompatible = true;

            // ─── 精度評価 ───
            float posError = Vector3.Distance(conn.SlotA.worldPosition, conn.SlotB.worldPosition);
            float posScore = Mathf.Clamp01(1f - posError / precisionThreshold);

            float dirError = Vector3.Angle(conn.SlotA.worldDirection, -conn.SlotB.worldDirection);
            float dirScore = Mathf.Clamp01(1f - dirError / directionThreshold);

            eval.precisionScore = (posScore + dirScore) * 0.5f;

            // ─── 適合スコア（形状×力学方向の組み合わせ） ───
            eval.compatibilityScore = CalcCompatibilityScore(conn, typeData);

            // ─── 総合スコア ───
            eval.overallScore = eval.precisionScore * 0.5f + eval.compatibilityScore * 0.5f;

            eval.notes = eval.overallScore >= 0.8f
                ? "精巧な接合です"
                : eval.overallScore >= 0.5f
                    ? "まずまずの接合です"
                    : "接合が粗いです";

            return eval;
        }

        /// <summary>
        /// スロットの方向と継手の力学特性から適合スコアを計算する。
        /// 例：蟻継ぎは引張方向に配置されると高スコア。
        /// </summary>
        private float CalcCompatibilityScore(SlotConnection conn, JointTypeData typeData)
        {
            // スロットが力を受ける方向を求める（Y=縦方向、XZ=横方向）
            float verticalComponent = Mathf.Abs(Vector3.Dot(conn.SlotA.worldDirection, Vector3.up));
            float horizontalComponent = 1f - verticalComponent;

            float score = conn.JointShape switch
            {
                // ほぞ継ぎ：主に縦（圧縮）方向に強い
                JointShapeType.Mortise =>
                    verticalComponent * typeData.compressionStrength +
                    horizontalComponent * typeData.shearStrength * 0.5f,

                // 蟻継ぎ：横引張方向に強い
                JointShapeType.Dovetail =>
                    horizontalComponent * typeData.tensionStrength +
                    verticalComponent * typeData.compressionStrength * 0.3f,

                // 鎌継ぎ：縦引張と横せん断に強く、しなりも活かす
                JointShapeType.Sickle =>
                    typeData.tensionStrength * 0.4f +
                    typeData.bendingFlexibility * 0.3f +
                    typeData.dampingCoefficient * 0.3f,

                // 相欠き継ぎ：バランス型
                JointShapeType.Splice =>
                    (typeData.compressionStrength + typeData.shearStrength) * 0.5f,

                _ => 0.5f
            };

            return Mathf.Clamp01(score);
        }

        /// <summary>指定形状のJointTypeDataを返す（見つからない場合null）</summary>
        public JointTypeData GetDataByShape(JointShapeType shape)
        {
            return _dataByShape.TryGetValue(shape, out var data) ? data : null;
        }
    }

    // ─────────────────── 評価結果クラス ───────────────────

    /// <summary>1つのスロット接続の評価結果</summary>
    public class JointEvaluation
    {
        public SlotConnection connection;
        public JointShapeType jointShape;
        public JointTypeData jointTypeData;

        /// <summary>適合しているか（形状・役割が正しいか）</summary>
        public bool isCompatible;

        /// <summary>配置精度スコア (0〜1)。位置・方向の正確さ。</summary>
        public float precisionScore;

        /// <summary>適合スコア (0〜1)。力学的に正しい使い方か。</summary>
        public float compatibilityScore;

        /// <summary>総合スコア (0〜1)</summary>
        public float overallScore;

        /// <summary>評価コメント</summary>
        public string notes;
    }
}
