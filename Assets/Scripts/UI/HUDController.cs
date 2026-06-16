using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RhythmGame
{
    /// <summary>
    /// HUD 控制器 — 显示分数、连击、最大连击、判定弹出文字
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("分数显示")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI maxComboText;

        [Header("判定弹出")]
        public TextMeshProUGUI judgmentPopup;  // 显示 Perfect/Good/Normal/Miss
        public float popupDuration = 0.6f;      // 弹出持续时间
        public float popupFloatDistance = 40f;  // 向上浮动距离

        [Header("颜色配置")]
        public Color perfectColor = new Color(1f, 0.85f, 0f);    // 金色
        public Color goodColor = new Color(0f, 0.9f, 0.3f);      // 绿色
        public Color normalColor = new Color(0.6f, 0.6f, 0.6f);  // 灰色
        public Color missColor = new Color(0.9f, 0.2f, 0.2f);    // 红色

        [Header("连击特效")]
        public float comboPunchScale = 1.2f;    // 连击变大
        public float comboPunchDuration = 0.1f; // 变大持续时间

        private Coroutine popupCoroutine;
        private Coroutine comboPunchCoroutine;
        private Vector3 comboOriginalScale;

        private void Start()
        {
            if (scoreText == null || comboText == null)
            {
                Debug.LogWarning("[HUDController] UI 文本引用缺失，请在场景中绑定。");
            }

            if (comboText != null)
                comboOriginalScale = comboText.transform.localScale;

            // 订阅分数事件
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged.AddListener(UpdateScore);
                ScoreManager.Instance.OnComboChanged.AddListener(UpdateCombo);
                ScoreManager.Instance.OnMaxComboChanged.AddListener(UpdateMaxCombo);
                ScoreManager.Instance.OnJudgment.AddListener(ShowJudgmentPopup);
            }
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged.RemoveListener(UpdateScore);
                ScoreManager.Instance.OnComboChanged.RemoveListener(UpdateCombo);
                ScoreManager.Instance.OnMaxComboChanged.RemoveListener(UpdateMaxCombo);
                ScoreManager.Instance.OnJudgment.RemoveListener(ShowJudgmentPopup);
            }
        }

        #region UI 更新回调

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"{score:N0}";
        }

        private void UpdateCombo(int combo)
        {
            if (comboText != null)
            {
                comboText.text = combo > 1 ? $"{combo} Combo" : "";
                // 连击数字变色
                if (combo >= 50)
                    comboText.color = perfectColor;
                else if (combo >= 20)
                    comboText.color = goodColor;
                else
                    comboText.color = Color.white;
            }

            // 连击变大动画
            if (combo > 0 && comboPunchCoroutine == null)
                comboPunchCoroutine = StartCoroutine(ComboPunchEffect());
        }

        private void UpdateMaxCombo(int maxCombo)
        {
            if (maxComboText != null)
                maxComboText.text = $"Max: {maxCombo}";
        }

        private void ShowJudgmentPopup(JudgmentType judgment, int combo)
        {
            if (judgmentPopup == null) return;

            // 设置文字和颜色
            switch (judgment)
            {
                case JudgmentType.Perfect:
                    judgmentPopup.text = "Perfect!";
                    judgmentPopup.color = perfectColor;
                    break;
                case JudgmentType.Good:
                    judgmentPopup.text = "Good";
                    judgmentPopup.color = goodColor;
                    break;
                case JudgmentType.Normal:
                    judgmentPopup.text = "";
                    judgmentPopup.color = normalColor;
                    break;
                case JudgmentType.Miss:
                    judgmentPopup.text = "Miss";
                    judgmentPopup.color = missColor;
                    break;
            }

            // 显示并启动弹出动画
            if (!string.IsNullOrEmpty(judgmentPopup.text))
            {
                if (popupCoroutine != null)
                    StopCoroutine(popupCoroutine);
                popupCoroutine = StartCoroutine(PopupAnimation());
            }
        }

        #endregion

        #region 动画协程

        private IEnumerator PopupAnimation()
        {
            judgmentPopup.gameObject.SetActive(true);
            judgmentPopup.alpha = 1f;

            Vector3 startPos = judgmentPopup.rectTransform.anchoredPosition;
            Vector3 endPos = startPos + Vector3.up * popupFloatDistance;

            float elapsed = 0f;
            while (elapsed < popupDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popupDuration;
                judgmentPopup.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                judgmentPopup.alpha = 1f - t;
                yield return null;
            }

            judgmentPopup.gameObject.SetActive(false);
            judgmentPopup.rectTransform.anchoredPosition = startPos;
            popupCoroutine = null;
        }

        private IEnumerator ComboPunchEffect()
        {
            comboText.transform.localScale = comboOriginalScale * comboPunchScale;
            yield return new WaitForSeconds(comboPunchDuration);
            comboText.transform.localScale = comboOriginalScale;
            comboPunchCoroutine = null;
        }

        #endregion
    }
}
