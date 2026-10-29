using UnityEngine;
using UnityEngine.UI;

namespace DuckSoup.Features.GameFeel.UI
{
    // Poner UNA vez en el Canvas de cada escena. Incluye botones inactivos.
    [DefaultExecutionOrder(-500)]
    public sealed class ButtonFeelInstaller : MonoBehaviour
    {
        private void Awake() => Install();

        [ContextMenu("Install button feel")]
        public void Install()
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<ButtonPressFeel>() == null)
                    button.gameObject.AddComponent<ButtonPressFeel>();
            }
        }
    }
}
