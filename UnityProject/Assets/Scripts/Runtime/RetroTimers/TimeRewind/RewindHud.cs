using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace RetroTimers.TimeRewind
{
    public sealed class RewindHud : MonoBehaviour
    {
        [SerializeField] private TimeRewindController controller;

        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool showDebugPanel;
#endif

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<TimeRewindController>();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                showDebugPanel = !showDebugPanel;
            }
        }
#endif

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            EnsureStyles();

            Rect panel = new(16, 16, 460, 180);
            GUI.Box(panel, GUIContent.none);

            float remaining = Mathf.Max(0f, controller.LevelTimeLimitSeconds - controller.IterationTimer);
            GUI.Label(new Rect(28, 28, 390, 26), $"Timer: {remaining:0.0}s", labelStyle);
            GUI.Label(new Rect(28, 58, 390, 26), $"Rewinds: {controller.CompletedRewindCount}", labelStyle);
            GUI.Label(new Rect(28, 88, 390, 26), $"Control delay: {controller.CurrentControlDelay:0.0}s", labelStyle);
            GUI.Label(new Rect(28, 118, 390, 26), controller.StatusMessage, labelStyle);

            if (controller.Status == TimeRewindStatus.Failed)
            {
                if (GUI.Button(new Rect(28, 148, 180, 28), "Restart", buttonStyle))
                {
                    controller.RestartLevel();
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (showDebugPanel)
            {
                DrawDebugPanel();
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void DrawDebugPanel()
        {
            Rect panel = new(16, 208, 460, 130);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(28, 220, 410, 26), $"Debug clone mode: {controller.PlaybackModeLabel}", labelStyle);
            GUI.Label(new Rect(28, 250, 410, 26), $"Clone altered: {(controller.AnyCloneAlteredByControlledPlayer ? "yes" : "no")}", labelStyle);

            if (GUI.Button(new Rect(28, 290, 210, 28), "Toggle clone mode", buttonStyle))
            {
                controller.ToggleClonePlaybackMode();
            }
        }
#endif

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18
            };
        }
    }
}
