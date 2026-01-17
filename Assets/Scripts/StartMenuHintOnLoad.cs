using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム開始（シーンロード後）に一度だけ、画面中央へ一定時間ヒントを表示する。
/// シーンに配置不要で、どのシーンで再生しても動く。
/// </summary>
public static class StartMenuHintOnLoad
{
    private static bool s_hasShown;

    // Domain Reloadが無効でも毎回再生で出るように、staticをリセット
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_hasShown = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ShowOnceAfterSceneLoad()
    {
        if (s_hasShown)
        {
            return;
        }
        s_hasShown = true;

        var go = new GameObject("StartMenuHint (Auto)");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.DontSave;
        go.AddComponent<Runner>();
    }

    private sealed class Runner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            const string textValue = "Press Esc for Menu";
            const float seconds = 5f;

            GameObject hintCanvas = new GameObject("StartHintCanvas");
            hintCanvas.transform.SetParent(transform, false);

            Canvas canvas = hintCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // Menu(100)より下

            CanvasScaler scaler = hintCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            hintCanvas.AddComponent<GraphicRaycaster>();

            GameObject textObj = new GameObject("StartHintText");
            textObj.transform.SetParent(hintCanvas.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(1200f, 160f);

            Text uiText = textObj.AddComponent<Text>();
            uiText.text = textValue;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiText.font == null)
            {
                uiText.font = Font.CreateDynamicFontFromOSFont("Arial", 80);
            }
            uiText.fontSize = 80;
            uiText.color = Color.white;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.fontStyle = FontStyle.Bold;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(3f, -3f);

            yield return new WaitForSecondsRealtime(seconds);

            if (hintCanvas != null)
            {
                Destroy(hintCanvas);
            }
            Destroy(gameObject);
        }
    }
}

