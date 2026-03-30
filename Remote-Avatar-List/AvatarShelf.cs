using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using VRC.Udon.Common.Interfaces;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AvatarShelf : UdonSharpBehaviour
{
    [Space(-8)]
    [Header("Remote Avatar List | Prefab by Kitto Dev")]
    [Space(-8)]
    [Header("Updated 3/30/2026 | Version 1.2 Page Number Update")]

    [Header("Remote Config")]
    [Tooltip("Raw Pastebin URL (e.g., https://pastebin.com/raw/abc123) to pull avatar list from. Format each line as: 'AvatarID, AvatarName, CreatorName'")]
    [SerializeField] private VRCUrl avatarListUrl;

    [Header("Avatar Pedestals Per Page")]
    [Tooltip("Assign pedestals in page order")]
    [SerializeField] private VRC_AvatarPedestal[] pedestals;

    [Header("TextMeshPro Labels")]
    [Tooltip("Assign TextMeshPro (non-UI) components to each pedestal. Ensure to assign them in the exact same order in the Pedestal array in order to properly label each pedestal.")]
    [SerializeField] private TextMeshPro[] pedestalLabels;

    [Header("Special Formatting")]
    [Tooltip("This formats a creator's name in a special color, useful for highlighting your own creations.")]
    [SerializeField] private string creatorName = "Kitto Dev";

    [Tooltip("Hex color code for creator name label (e.g. #C40DF8)")]
    [SerializeField] private string creatorHexColor = "#C40DF8";

    [Tooltip("If true, all labels will be hidden regardless of creator")]
    [SerializeField] private bool hideLabels = false;

    [Header("Page Number Display (Optional)")]
    [Tooltip("TextMeshPro or Text component to show current page number")]
    [SerializeField] private MaskableGraphic currentPageText;
    [Tooltip("TextMeshPro or Text component to show total pages")]
    [SerializeField] private MaskableGraphic totalPagesText;

    private string[] avatarLines;
    private int currentPage = 0;
    private int avatarsPerPage;
    private System.Type displayType;

    void Start()
    {
        LoadList();

        if (currentPageText == null)
        {
            return;
        }

        displayType = currentPageText.GetType();

        if (displayType != typeof(Text) &&
            displayType != typeof(TextMeshPro) &&
            displayType != typeof(TextMeshProUGUI))
        {
            Debug.LogError("[AvatarShelf] Unsupported text component type! Use Text, TextMeshPro, or TextMeshProUGUI.");
        }
    }

    private void LoadList()
    {
        avatarsPerPage = pedestals.Length;
        VRCStringDownloader.LoadUrl(avatarListUrl, (IUdonEventReceiver)this);
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
        avatarLines = raw.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        currentPage = 0; // Reset to first page on new load
        UpdatePage();
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.LogError("[AvatarShelf] Error loading avatar list: " + result.Error);
    }

    public void NextPage()
    {
        int maxPages = Mathf.CeilToInt((float)avatarLines.Length / avatarsPerPage);
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

    private void UpdatePage()
    {
        int maxPages = avatarsPerPage > 0 && avatarLines != null
            ? Mathf.CeilToInt((float)avatarLines.Length / avatarsPerPage)
            : 0;

        int displayPage = maxPages > 0 ? currentPage + 1 : 0;
        SetText(currentPageText, displayPage.ToString());
        SetText(totalPagesText, maxPages.ToString());

        int startIndex = currentPage * avatarsPerPage;

        for (int i = 0; i < avatarsPerPage; i++)
        {
            int index = startIndex + i;
            VRC_AvatarPedestal pedestal = pedestals[i];
            TextMeshPro label = (pedestalLabels != null && i < pedestalLabels.Length) ? pedestalLabels[i] : null;

            if (index < avatarLines.Length)
            {
                string[] parts = avatarLines[index].Split(',');
                if (parts.Length >= 3)
                {
                    string id = parts[0].Trim();
                    string name = parts[1].Trim();
                    string creator = parts[2].Trim();

                    pedestal.blueprintId = id;
                    pedestal.gameObject.SetActive(true);

                    if (label != null)
                    {
                        if (hideLabels)
                        {
                            label.text = "";
                        }
                        else if (creator == creatorName)
                        {
                            label.text = $"<color={creatorHexColor}>{name}</color>";
                        }
                        else
                        {
                            label.text = $"{name} by {creator}";
                        }
                    }
                }
                else
                {
                    pedestal.gameObject.SetActive(false);
                    if (label != null) label.text = "";
                }
            }
            else
            {
                pedestal.gameObject.SetActive(false);
                if (label != null) label.text = "";
            }
        }
    }
}