using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class TowerSelectionMenu : MonoBehaviour
{
    private const float TowerIconSize = 64f;

    private sealed class MenuEntry
    {
        public PlaceableObjectDefinition Definition;
        public Image Background;
    }

    private readonly List<MenuEntry> menuEntries = new List<MenuEntry>();

    private GameManager gameManager;
    private GameObject canvasObject;
    [SerializeField] private GameObject TowerSelectionUI;
    [SerializeField] private GameObject TowerButtonPrefab;
    private GameObject createdEventSystem;

    internal void Initialize(GameManager owningGameManager)
    {
        if (gameManager != null)
        {
            return;
        }

        if (owningGameManager == null)
        {
            Debug.LogError("Tower selection menu requires a GameManager.", this);
            return;
        }

        if (canvasObject == null)
        {
            canvasObject = FindAnyObjectByType<Canvas>().gameObject;
        }

        gameManager = owningGameManager;
        EnsureEventSystem();
        BuildMenu();
    }

    public void RefreshSelection()
    {
        if (gameManager == null)
        {
            return;
        }

        PlaceableObjectDefinition selected = gameManager.SelectedPlaceableObject;
        foreach (MenuEntry entry in menuEntries)
        {
            entry.Background.color = entry.Definition == selected
                ? new Color(0.22f, 0.48f, 0.72f, 1f)
                : new Color(0.18f, 0.2f, 0.24f, 1f);
        }
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            createdEventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem = createdEventSystem.GetComponent<EventSystem>();
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        else if (eventSystem.GetComponent<BaseInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    private void BuildMenu()
    {
        RectTransform content = TowerSelectionUI.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);

        IReadOnlyList<PlaceableObjectDefinition> definitions = gameManager.PlaceableObjects;
        int validDefinitionCount = 0;
        if (definitions != null)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                PlaceableObjectDefinition definition = definitions[i];
                if (definition == null)
                {
                    Debug.LogWarning(
                        $"Placeable object list entry {i} is unassigned and will not appear in the tower menu.",
                        gameManager);
                    continue;
                }

                CreateTowerButton(definition, content);
                validDefinitionCount++;
            }
        }

        if (validDefinitionCount == 0)
        {
            CreateEmptyState(content);
        }

        RefreshSelection();
    }

    private void CreateTowerButton(
        PlaceableObjectDefinition definition,
        RectTransform content)
    {
        GameObject buttonObject = Instantiate(TowerButtonPrefab, content);
        buttonObject.name = $"Tower Button - {definition.ObjectName}";

        Image background = buttonObject.GetComponent<Image>();

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (!gameManager.SelectPlaceableObject(definition))
            {
                Debug.LogError(
                    $"Could not select placeable object '{definition.ObjectName}'.",
                    gameManager);
            }
        });
        GameObject iconObject = buttonObject.transform.Find("Icon").gameObject;
        //LayoutElement iconLayoutElement = iconObject.GetComponent<LayoutElement>();
        //iconLayoutElement.preferredWidth = TowerIconSize;
        //iconLayoutElement.preferredHeight = TowerIconSize;
        //iconLayoutElement.minWidth = TowerIconSize;
        //iconLayoutElement.minHeight = TowerIconSize;

        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = definition.Icon;
        icon.enabled = definition.Icon != null;

        GameObject labelObject = buttonObject.transform.Find("Label").gameObject;
        //LayoutElement labelLayoutElement = labelObject.GetComponent<LayoutElement>();
        //labelLayoutElement.flexibleWidth = 1f;
        //labelLayoutElement.preferredHeight = TowerIconSize;

        Text label = labelObject.GetComponentInChildren<Text>();
        label.text = $"{definition.ObjectName}\nCost: {definition.Cost}";

        menuEntries.Add(new MenuEntry
        {
            Definition = definition,
            Background = background
        });
    }

    private static void CreateEmptyState(RectTransform content)
    {
        RectTransform emptyStateTransform = CreateRectTransform("Empty State", content);
        LayoutElement layoutElement = emptyStateTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 320f;
        layoutElement.preferredHeight = 92f;

        Text emptyState = emptyStateTransform.gameObject.AddComponent<Text>();
        emptyState.material = Graphic.defaultGraphicMaterial;
        emptyState.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        emptyState.fontSize = 20;
        emptyState.alignment = TextAnchor.MiddleLeft;
        emptyState.color = Color.white;
        emptyState.text = "No towers available";
    }

    private static RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        if (createdEventSystem != null)
        {
            Destroy(createdEventSystem);
        }
    }
}
