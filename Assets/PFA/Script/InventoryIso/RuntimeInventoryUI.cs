using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RuntimeInventoryUI : MonoBehaviour
{
    [Header("Wheel")]
    [SerializeField]
    private float outerRadius = 300f;

    [SerializeField]
    private float innerRadius = 125f;

    [SerializeField]
    private float slotGap = 3f;

    [Header("Colors")]
    [SerializeField]
    private Color normalColor =
        new Color(0.015f, 0.055f, 0.10f, 0.96f);

    [SerializeField]
    private Color selectedColor =
        new Color(0.05f, 0.55f, 1f, 1f);

    [SerializeField]
    private Color maxColor =
        new Color(1f, 0.72f, 0.05f, 1f);

    [SerializeField]
    private Color centerColor =
        new Color(0.005f, 0.018f, 0.035f, 0.98f);

    private Canvas canvas;

    private GameObject wheelRoot;

    private RectTransform wheelRect;

    private RadialSegment[] segments;

    private Image[] icons;

    private TMP_Text[] amountTexts;

    private TMP_Text[] maxTexts;

    private Image[] maxBorders;

    private TMP_Text centerName;

    private TMP_Text centerDescription;

    private TMP_Text centerAction;

    private GameObject quickSlotRoot;

    private Image quickIcon;

    private TMP_Text quickAmount;

    private int selectedIndex = -1;

    private bool isOpen;

    private const int SlotCount = 8;

    private void Awake()
    {
        CreateCanvas();

        CreateWheel();

        CreateQuickSlot();

        HideWheel();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventory;

            InventoryManager.Instance.OnQuickSlotChanged +=
                OnQuickSlotChanged;
        }

        RefreshInventory();

        RefreshQuickSlot();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -=
                RefreshInventory;

            InventoryManager.Instance.OnQuickSlotChanged -=
                OnQuickSlotChanged;
        }
    }

    private void Update()
    {
        HandleOpenInput();

        if (!isOpen)
            return;

        UpdateMouseSelection();

        UpdateGamepadSelection();

        RefreshSelection();

        HandleEquipInput();
    }

    // =========================================================
    // CANVAS
    // =========================================================

    private void CreateCanvas()
    {
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            return;

        GameObject canvasObject =
            new GameObject("Inventory Canvas");

        canvas =
            canvasObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    // =========================================================
    // WHEEL
    // =========================================================

    private void CreateWheel()
    {
        wheelRoot =
            new GameObject("Radial Inventory");

        wheelRoot.transform.SetParent(
            canvas.transform,
            false
        );

        wheelRect =
            wheelRoot.AddComponent<RectTransform>();

        wheelRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        wheelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        wheelRect.pivot =
            new Vector2(0.5f, 0.5f);

        wheelRect.sizeDelta =
            new Vector2(
                outerRadius * 2.5f,
                outerRadius * 2.5f
            );

        CreateWheelBackground();

        segments =
            new RadialSegment[SlotCount];

        icons =
            new Image[SlotCount];

        amountTexts =
            new TMP_Text[SlotCount];

        maxTexts =
            new TMP_Text[SlotCount];

        maxBorders =
            new Image[SlotCount];

        float anglePerSlot =
            360f / SlotCount;

        for (int i = 0; i < SlotCount; i++)
        {
            CreateSlot(
                i,
                anglePerSlot
            );
        }

        CreateCenter();
    }

    // =========================================================
    // BACKGROUND
    // =========================================================

    private void CreateWheelBackground()
    {
        GameObject background =
            new GameObject(
                "Wheel Background"
            );

        background.transform.SetParent(
            wheelRect,
            false
        );

        Image image =
            background.AddComponent<Image>();

        image.color =
            new Color(
                0.002f,
                0.01f,
                0.02f,
                0.82f
            );

        image.raycastTarget = false;

        RectTransform rect =
            background.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(
                outerRadius * 2.1f,
                outerRadius * 2.1f
            );
    }

    // =========================================================
    // SLOT
    // =========================================================

    private void CreateSlot(
        int index,
        float anglePerSlot)
    {
        GameObject slot =
            new GameObject(
                "Slot " + index
            );

        slot.transform.SetParent(
            wheelRect,
            false
        );

        RectTransform rect =
            slot.AddComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(
                outerRadius * 2f,
                outerRadius * 2f
            );

        RadialSegment segment =
            slot.AddComponent<RadialSegment>();

        float centerAngle =
            90f -
            index * anglePerSlot;

        float startAngle =
            centerAngle -
            anglePerSlot / 2f +
            slotGap;

        float endAngle =
            centerAngle +
            anglePerSlot / 2f -
            slotGap;

        segment.Setup(
            startAngle,
            endAngle,
            innerRadius,
            outerRadius
        );

        segment.color =
            normalColor;

        segment.raycastTarget = false;

        segments[index] =
            segment;

        CreateSlotContent(
            slot.transform,
            index,
            centerAngle
        );
    }

    // =========================================================
    // SLOT CONTENT
    // =========================================================

    private void CreateSlotContent(
        Transform parent,
        int index,
        float angle)
    {
        GameObject content =
            new GameObject(
                "Content"
            );

        content.transform.SetParent(
            parent,
            false
        );

        RectTransform contentRect =
            content.AddComponent<RectTransform>();

        contentRect.sizeDelta =
            new Vector2(
                140f,
                140f
            );

        float radians =
            angle * Mathf.Deg2Rad;

        float radius =
            (innerRadius + outerRadius) / 2f;

        contentRect.anchoredPosition =
            new Vector2(
                Mathf.Cos(radians) * radius,
                Mathf.Sin(radians) * radius
            );

        CreateIcon(
            content.transform,
            index
        );

        CreateAmount(
            content.transform,
            index
        );

        CreateMax(
            content.transform,
            index
        );

        CreateMaxBorder(
            content.transform,
            index
        );
    }

    // =========================================================
    // ICON
    // =========================================================

    private void CreateIcon(
        Transform parent,
        int index)
    {
        GameObject iconObject =
            new GameObject(
                "Item Icon"
            );

        iconObject.transform.SetParent(
            parent,
            false
        );

        Image image =
            iconObject.AddComponent<Image>();

        image.enabled = false;

        image.raycastTarget = false;

        RectTransform rect =
            image.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                70f,
                70f
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                15f
            );

        icons[index] =
            image;
    }

    // =========================================================
    // AMOUNT
    // =========================================================

    private void CreateAmount(
        Transform parent,
        int index)
    {
        GameObject amountObject =
            new GameObject(
                "Amount"
            );

        amountObject.transform.SetParent(
            parent,
            false
        );

        TMP_Text text =
            amountObject.AddComponent<TextMeshProUGUI>();

        text.fontSize = 21;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            Color.white;

        text.raycastTarget = false;

        RectTransform rect =
            text.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                100f,
                35f
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                -35f
            );

        amountTexts[index] =
            text;
    }

    // =========================================================
    // MAX
    // =========================================================

    private void CreateMax(
        Transform parent,
        int index)
    {
        GameObject maxObject =
            new GameObject(
                "MAX"
            );

        maxObject.transform.SetParent(
            parent,
            false
        );

        TMP_Text text =
            maxObject.AddComponent<TextMeshProUGUI>();

        text.text =
            "MAX";

        text.fontSize = 15;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            maxColor;

        text.raycastTarget = false;

        RectTransform rect =
            text.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                100f,
                30f
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                -60f
            );

        text.gameObject.SetActive(false);

        maxTexts[index] =
            text;
    }

    // =========================================================
    // MAX BORDER
    // =========================================================

    private void CreateMaxBorder(
        Transform parent,
        int index)
    {
        GameObject borderObject =
            new GameObject(
                "MAX Border"
            );

        borderObject.transform.SetParent(
            parent,
            false
        );

        Image border =
            borderObject.AddComponent<Image>();

        border.color =
            maxColor;

        border.raycastTarget = false;

        RectTransform rect =
            border.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                115f,
                115f
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                10f
            );

        borderObject.SetActive(false);

        maxBorders[index] =
            border;
    }

    // =========================================================
    // CENTER
    // =========================================================

    private void CreateCenter()
    {
        GameObject center =
            new GameObject(
                "Center"
            );

        center.transform.SetParent(
            wheelRect,
            false
        );

        RectTransform rect =
            center.AddComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                innerRadius * 1.55f,
                innerRadius * 1.55f
            );

        Image image =
            center.AddComponent<Image>();

        image.color =
            centerColor;

        image.raycastTarget = false;

        centerName =
            CreateCenterText(
                center.transform,
                25f,
                new Vector2(
                    0f,
                    35f
                )
            );

        centerDescription =
            CreateCenterText(
                center.transform,
                17f,
                new Vector2(
                    0f,
                    0f
                )
            );

        centerAction =
            CreateCenterText(
                center.transform,
                15f,
                new Vector2(
                    0f,
                    -40f
                )
            );

        centerName.color =
            selectedColor;
    }

    private TMP_Text CreateCenterText(
        Transform parent,
        float fontSize,
        Vector2 position)
    {
        GameObject objectText =
            new GameObject(
                "Text"
            );

        objectText.transform.SetParent(
            parent,
            false
        );

        TMP_Text text =
            objectText.AddComponent<TextMeshProUGUI>();

        text.fontSize =
            fontSize;

        text.fontStyle =
            FontStyles.Bold;

        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget = false;

        RectTransform rect =
            text.GetComponent<RectTransform>();

        rect.sizeDelta =
            new Vector2(
                250f,
                45f
            );

        rect.anchoredPosition =
            position;

        return text;
    }

    // =========================================================
    // QUICK SLOT
    // =========================================================

    private void CreateQuickSlot()
    {
        quickSlotRoot =
            new GameObject(
                "Quick Slot"
            );

        quickSlotRoot.transform.SetParent(
            canvas.transform,
            false
        );

        RectTransform rect =
            quickSlotRoot.AddComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(
                1f,
                0f
            );

        rect.anchorMax =
            new Vector2(
                1f,
                0f
            );

        rect.pivot =
            new Vector2(
                1f,
                0f
            );

        rect.sizeDelta =
            new Vector2(
                140f,
                140f
            );

        rect.anchoredPosition =
            new Vector2(
                -35f,
                35f
            );

        Image background =
            quickSlotRoot.AddComponent<Image>();

        background.color =
            normalColor;

        background.raycastTarget =
            false;

        GameObject iconObject =
            new GameObject(
                "Quick Icon"
            );

        iconObject.transform.SetParent(
            quickSlotRoot.transform,
            false
        );

        quickIcon =
            iconObject.AddComponent<Image>();

        quickIcon.enabled = false;

        quickIcon.raycastTarget =
            false;

        RectTransform iconRect =
            quickIcon.GetComponent<RectTransform>();

        iconRect.sizeDelta =
            new Vector2(
                70f,
                70f
            );

        iconRect.anchoredPosition =
            new Vector2(
                0f,
                20f
            );

        GameObject amountObject =
            new GameObject(
                "Quick Amount"
            );

        amountObject.transform.SetParent(
            quickSlotRoot.transform,
            false
        );

        quickAmount =
            amountObject.AddComponent<TextMeshProUGUI>();

        quickAmount.fontSize =
            21;

        quickAmount.fontStyle =
            FontStyles.Bold;

        quickAmount.alignment =
            TextAlignmentOptions.Center;

        RectTransform amountRect =
            quickAmount.GetComponent<RectTransform>();

        amountRect.sizeDelta =
            new Vector2(
                100f,
                30f
            );

        amountRect.anchoredPosition =
            new Vector2(
                20f,
                -45f
            );

        GameObject keyObject =
            new GameObject(
                "Quick Key"
            );

        keyObject.transform.SetParent(
            quickSlotRoot.transform,
            false
        );

        TMP_Text key =
            keyObject.AddComponent<TextMeshProUGUI>();

        key.text =
            "X";

        key.fontSize =
            16;

        key.alignment =
            TextAlignmentOptions.Center;

        key.color =
            selectedColor;

        RectTransform keyRect =
            key.GetComponent<RectTransform>();

        keyRect.sizeDelta =
            new Vector2(
                40f,
                30f
            );

        keyRect.anchoredPosition =
            new Vector2(
                -48f,
                -48f
            );
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void HandleOpenInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleWheel();
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonNorth.wasPressedThisFrame)
            {
                ToggleWheel();
            }
        }
    }

    private void HandleEquipInput()
    {
        if (Keyboard.current != null &&
            Mouse.current != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                EquipSelected();
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                EquipSelected();
            }
        }
    }

    private void EquipSelected()
    {
        if (selectedIndex < 0)
            return;

        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.SetQuickSlot(
            selectedIndex
        );
    }

    // =========================================================
    // MOUSE
    // =========================================================

    private void UpdateMouseSelection()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouse =
            Mouse.current.position.ReadValue();

        Vector2 center =
            RectTransformUtility.WorldToScreenPoint(
                null,
                wheelRect.position
            );

        Vector2 direction =
            mouse - center;

        if (direction.magnitude < 70f)
            return;

        SelectFromDirection(
            direction
        );
    }

    // =========================================================
    // GAMEPAD
    // =========================================================

    private void UpdateGamepadSelection()
    {
        if (Gamepad.current == null)
            return;

        Vector2 stick =
            Gamepad.current.rightStick.ReadValue();

        if (stick.magnitude < 0.25f)
            return;

        SelectFromDirection(
            stick
        );
    }

    private void SelectFromDirection(
        Vector2 direction)
    {
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        angle =
            90f - angle;

        if (angle < 0)
            angle += 360f;

        float slotAngle =
            360f / SlotCount;

        int index =
            Mathf.FloorToInt(
                (angle + slotAngle / 2f)
                / slotAngle
            );

        index %= SlotCount;

        selectedIndex =
            index;
    }

    // =========================================================
    // SELECTION
    // =========================================================

    private void RefreshSelection()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (i == selectedIndex)
            {
                segments[i].color =
                    selectedColor;
            }
            else
            {
                segments[i].color =
                    normalColor;
            }
        }

        RefreshCenter();
    }

    // =========================================================
    // CENTER
    // =========================================================

    private void RefreshCenter()
    {
        if (selectedIndex < 0)
        {
            centerName.text = "";

            centerDescription.text = "";

            centerAction.text = "";

            return;
        }

        InventorySlotData slot =
            InventoryManager.Instance
                .GetSlot(selectedIndex);

        if (slot == null ||
            slot.IsEmpty)
        {
            centerName.text =
                "EMPTY";

            centerDescription.text =
                "";

            centerAction.text =
                "";

            return;
        }

        centerName.text =
            slot.item.itemName;

        centerDescription.text =
            "+" +
            slot.item.healAmount +
            " HP";

        centerAction.text =
            "A  EQUIP";
    }

    // =========================================================
    // INVENTORY REFRESH
    // =========================================================

    private void RefreshInventory()
    {
        if (InventoryManager.Instance == null)
            return;

        for (int i = 0; i < SlotCount; i++)
        {
            InventorySlotData slot =
                InventoryManager.Instance.GetSlot(i);

            if (slot == null ||
                slot.IsEmpty)
            {
                icons[i].enabled =
                    false;

                amountTexts[i].text =
                    "";

                maxTexts[i]
                    .gameObject
                    .SetActive(false);

                maxBorders[i]
                    .gameObject
                    .SetActive(false);

                continue;
            }

            icons[i].enabled =
                true;

            icons[i].sprite =
                slot.item.icon;

            amountTexts[i].text =
                "x" +
                slot.amount;

            bool isMax =
                slot.IsFull;

            maxTexts[i]
                .gameObject
                .SetActive(isMax);

            maxBorders[i]
                .gameObject
                .SetActive(isMax);

            if (isMax)
            {
                amountTexts[i].color =
                    maxColor;
            }
            else
            {
                amountTexts[i].color =
                    Color.white;
            }
        }

        RefreshQuickSlot();
    }

    // =========================================================
    // QUICK SLOT
    // =========================================================

    private void OnQuickSlotChanged(
        int index)
    {
        RefreshQuickSlot();
    }

    private void RefreshQuickSlot()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryItem item =
            InventoryManager.Instance
                .GetQuickItem();

        int amount =
            InventoryManager.Instance
                .GetQuickItemAmount();

        if (item == null)
        {
            quickIcon.enabled =
                false;

            quickAmount.text =
                "";

            return;
        }

        quickIcon.enabled =
            true;

        quickIcon.sprite =
            item.icon;

        quickAmount.text =
            "x" +
            amount;
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    private void ToggleWheel()
    {
        isOpen =
            !isOpen;

        wheelRoot.SetActive(
            isOpen
        );

        if (isOpen)
        {
            selectedIndex = -1;

            Cursor.visible =
                true;

            Cursor.lockState =
                CursorLockMode.None;

            Time.timeScale =
                0f;

            RefreshInventory();
        }
        else
        {
            Cursor.visible =
                false;

            Cursor.lockState =
                CursorLockMode.Locked;

            Time.timeScale =
                1f;
        }
    }

    private void HideWheel()
    {
        isOpen =
            false;

        wheelRoot.SetActive(
            false
        );
    }
}
