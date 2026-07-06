using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class WorldList : UdonSharpBehaviour
{
    [Space(-8)]
    [Header("Remote World List | Prefab by Kitto Dev")]
    [Space(-8)]
    [Header("Updated 7/6/2026 | Version 1.2 QOL Update")]

    [Header("Remote Config")]
    [Tooltip("Raw Text URL (e.g., https://pastebin.com/raw/abc123) to pull the portal list from. Format each line as a single world ID (e.g., wrld_abc123). Sites like Pastebin, or GitHub Gists are confirmed to work and is whitelisted by VRChat.")]
    [SerializeField] private VRCUrl worldListUrl;

    [Header("Portals Per Page")]
    [Tooltip("Assign portals in page order")]
    [SerializeField] private VRC_PortalMarker[] portals;

    [Header("Page Navigation Buttons")]
    [Tooltip("Button that moves to the previous page")]
    [SerializeField] private Selectable previousPageButton;
    [Tooltip("Button that moves to the next page")]
    [SerializeField] private Selectable nextPageButton;

    [Header("Page Number Display (Optional)")]
    [Tooltip("TextMeshPro or Text component to show current page number")]
    [SerializeField] private MaskableGraphic currentPageText;
    [Tooltip("TextMeshPro or Text component to show total pages")]
    [SerializeField] private MaskableGraphic totalPagesText;

    private string[] portalLines;
    private int currentPage = 0;
    private int portalsPerPage;
    private System.Type displayType;

    void Start()
    {
        LoadList();
        UpdatePageNavigationButtons();

        if (currentPageText == null)
        {
            return;
        }

        displayType = currentPageText.GetType();

        if (displayType != typeof(Text) &&
            displayType != typeof(TextMeshPro) &&
            displayType != typeof(TextMeshProUGUI))
        {
            Debug.LogError("[WorldList] Unsupported text component type! Use Text, TextMeshPro, or TextMeshProUGUI.");
        }
    }

    private void LoadList()
    {
        portalsPerPage = portals.Length;
        VRCStringDownloader.LoadUrl(worldListUrl, (IUdonEventReceiver)this);
    }

    private void SetText(MaskableGraphic textComponent, string value)
    {
        if (textComponent == null)
        {
            return;
        }

        var componentType = textComponent.GetType();
        if (componentType == typeof(Text))
        {
            ((Text)textComponent).text = value;
            return;
        }

        if (componentType == typeof(TextMeshProUGUI))
        {
            ((TextMeshProUGUI)textComponent).text = value;
            return;
        }

        if (componentType == typeof(TextMeshPro))
        {
            ((TextMeshPro)textComponent).text = value;
        }
    }

    void OnEnable()
    {
        LoadList();
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        string raw = result.Result;
        portalLines = raw.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        currentPage = 0;
        UpdatePage();
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.LogError("[WorldList] Error loading World ID list: " + result.Error);
    }

    public void NextPage()
    {
        int maxPages = Mathf.CeilToInt((float)portalLines.Length / portalsPerPage);
        if (currentPage < maxPages - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    private void UpdatePageNavigationButtons()
    {
        int maxPages = portalsPerPage > 0 && portalLines != null
            ? Mathf.CeilToInt((float)portalLines.Length / portalsPerPage)
            : 0;

        bool canGoPrevious = maxPages > 1 && currentPage > 0;
        bool canGoNext = maxPages > 1 && currentPage < maxPages - 1;

        if (previousPageButton != null)
        {
            previousPageButton.interactable = canGoPrevious;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = canGoNext;
        }
    }

    private void UpdatePage()
    {
        int maxPages = portalsPerPage > 0 && portalLines != null
            ? Mathf.CeilToInt((float)portalLines.Length / portalsPerPage)
            : 0;

        int displayPage = maxPages > 0 ? currentPage + 1 : 0;
        UpdatePageNavigationButtons();
        SetText(currentPageText, displayPage.ToString());
        SetText(totalPagesText, maxPages.ToString());

        int startIndex = currentPage * portalsPerPage;

        for (int i = 0; i < portalsPerPage; i++)
        {
            int index = startIndex + i;
            VRC_PortalMarker portal = portals[i];

            if (index < portalLines.Length)
            {
                string worldId = portalLines[index].Trim();
                portal.roomId = worldId;
                portal.gameObject.SetActive(true);
            }
            else
            {
                portal.gameObject.SetActive(false);
            }
        }
    }
}