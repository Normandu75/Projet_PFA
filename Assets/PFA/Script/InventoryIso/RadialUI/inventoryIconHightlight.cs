using UnityEngine;
using UnityEngine.UI;

public class InventoryIconHighlight : MonoBehaviour
{
    private Image icon;

    private Vector3 normalScale =
        Vector3.one;

    private Vector3 selectedScale =
        Vector3.one * 1.18f;

    private Color normalColor =
        Color.white;

    private Color selectedColor =
        new Color(
            0.25f,
            0.85f,
            1f,
            1f
        );

    private bool selected;

    private void Awake()
    {
        icon =
            GetComponent<Image>();
    }

    public void SetSelected(
        bool value)
    {
        selected =
            value;

        if (selected)
        {
            icon.color =
                selectedColor;

            transform.localScale =
                selectedScale;
        }
        else
        {
            icon.color =
                normalColor;

            transform.localScale =
                normalScale;
        }
    }
}