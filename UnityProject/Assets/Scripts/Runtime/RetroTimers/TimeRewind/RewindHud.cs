using UnityEngine;

namespace RetroTimers.TimeRewind
{
    public sealed class RewindHud : MonoBehaviour
    {
        [SerializeField] private TimeRewindController controller;

        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<TimeRewindController>();
            }
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            EnsureStyles();

            Rect panel = new(16, 16, 460, 210);
            GUI.Box(panel, GUIContent.none);

            float remaining = Mathf.Max(0f, controller.LevelTimeLimitSeconds - controller.IterationTimer);
            GUI.Label(new Rect(28, 28, 390, 26), $"Timer: {remaining:0.0}s", labelStyle);
            GUI.Label(new Rect(28, 58, 390, 26), $"Rewinds: {controller.CompletedRewindCount}", labelStyle);
            GUI.Label(new Rect(28, 88, 390, 26), $"Control delay: {controller.CurrentControlDelay:0.0}s", labelStyle);
            GUI.Label(new Rect(28, 118, 390, 26), controller.StatusMessage, labelStyle);
            GUI.Label(new Rect(28, 148, 410, 26), $"Clone mode: {controller.PlaybackModeLabel}", labelStyle);

            if (GUI.Button(new Rect(28, 178, 210, 28), "Toggle clone mode", buttonStyle))
            {
                controller.ToggleClonePlaybackMode();
            }

            if (controller.Status == TimeRewindStatus.Failed)
            {
                if (GUI.Button(new Rect(254, 178, 180, 28), "Restart", buttonStyle))
                {
                    controller.RestartLevel();
                }
            }
        }

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
