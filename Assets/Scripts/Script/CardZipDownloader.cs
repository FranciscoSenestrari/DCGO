using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class CardZipDownloader : MonoBehaviour
{
    private static CardZipDownloader _instance;
    public static CardZipDownloader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CardZipDownloader");
                _instance = go.AddComponent<CardZipDownloader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Cloudflare R2 Bucket Base URL")]
    public string R2CardZipUrl = "https://pub-52468b82169d40afb8ff3d4a7bacf05f.r2.dev";

    public bool IsDownloading { get; private set; } = false;

    private void Awake()
    {
        SetLandscapeOrientation();
    }

    /// <summary>
    /// Enforces landscape orientation with automatic rotation between left and right on Android and mobile platforms.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void SetLandscapeOrientation()
    {
#if UNITY_ANDROID || UNITY_IOS
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
#endif
    }

    /// <summary>
    /// Main entry point: checks and downloads both card images and default decks if local folders are empty.
    /// </summary>
    public IEnumerator CheckAndDownloadAssets(LoadingObject loadingObject = null)
    {
        SetLandscapeOrientation();

        IsDownloading = true;

        // 1. Download card images if Card directory is empty
        yield return CheckAndDownloadCards(loadingObject);

        // 2. Download default decks if Decks directory is empty
        yield return CheckAndDownloadDecks(loadingObject);

        IsDownloading = false;
    }

    /// <summary>
    /// Legacy compatibility alias for CheckAndDownloadAssets.
    /// </summary>
    public IEnumerator CheckAndDownloadCardZip(LoadingObject loadingObject = null)
    {
        yield return CheckAndDownloadAssets(loadingObject);
    }

    /// <summary>
    /// Checks if the local Card directory is empty or missing. If empty, downloads card.zip from Cloudflare R2 and extracts images.
    /// </summary>
    public IEnumerator CheckAndDownloadCards(LoadingObject loadingObject = null)
    {
        string texturesDir = StreamingAssetsUtility.GetStreamingAssetPath("Textures", false);
        string cardDir = Path.Combine(texturesDir, "Card").Replace("\\", "/");

        if (!Directory.Exists(cardDir))
        {
            Directory.CreateDirectory(cardDir);
        }

        string[] existingFiles = Directory.GetFiles(cardDir);
        if (existingFiles.Length > 0)
        {
            // Cards already exist locally; skip download
            yield break;
        }

        if (string.IsNullOrEmpty(R2CardZipUrl) || R2CardZipUrl.Contains("your-r2-bucket"))
        {
            Debug.LogWarning("[CardZipDownloader] R2CardZipUrl is not configured with a valid URL. Skipping download.");
            yield break;
        }

        string baseUrl = R2CardZipUrl.Trim();
        string downloadUrl = baseUrl;
        if (!downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            downloadUrl = downloadUrl.TrimEnd('/') + "/card.zip";
        }

        if (loadingObject != null)
        {
            yield return loadingObject.StartLoading("Downloading card assets (0%)");
        }

        string tempZipPath = Path.Combine(Application.persistentDataPath, "card.zip").Replace("\\", "/");

        using (UnityWebRequest webReq = UnityWebRequest.Get(downloadUrl))
        {
            webReq.downloadHandler = new DownloadHandlerFile(tempZipPath);
            UnityWebRequestAsyncOperation op = webReq.SendWebRequest();

            while (!op.isDone)
            {
                float progress = op.progress;
                ulong downloadedBytes = webReq.downloadedBytes;
                float downloadedMB = downloadedBytes / (1024f * 1024f);

                string progressText = $"Downloading Card Assets...\n{progress * 100f:F1}% ({downloadedMB:F1} MB)";
                if (loadingObject != null && loadingObject.LoadingText != null)
                {
                    loadingObject.LoadingText.text = progressText;
                }

                yield return null;
            }

            if (webReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CardZipDownloader] Cards download error: {webReq.error}");
                if (loadingObject != null && loadingObject.LoadingText != null)
                {
                    loadingObject.LoadingText.text = $"Cards Download Error: {webReq.error}";
                }
                yield return new WaitForSeconds(2.5f);
                if (loadingObject != null)
                {
                    yield return loadingObject.EndLoading();
                }
                yield break;
            }
        }

        if (loadingObject != null && loadingObject.LoadingText != null)
        {
            loadingObject.LoadingText.text = "Extracting card assets...";
        }

        bool extractSuccess = false;
        string extractError = "";

        Task extractTask = Task.Run(() =>
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(tempZipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;

                        string destinationPath = Path.Combine(cardDir, entry.Name).Replace("\\", "/");
                        entry.ExtractToFile(destinationPath, true);
                    }
                }
                extractSuccess = true;
            }
            catch (Exception ex)
            {
                extractError = ex.Message;
                Debug.LogError($"[CardZipDownloader] Zip extraction error: {ex}");
            }
            finally
            {
                if (File.Exists(tempZipPath))
                {
                    try { File.Delete(tempZipPath); } catch { }
                }
            }
        });

        while (!extractTask.IsCompleted)
        {
            yield return null;
        }

        if (!extractSuccess)
        {
            if (loadingObject != null && loadingObject.LoadingText != null)
            {
                loadingObject.LoadingText.text = $"Extraction Error: {extractError}";
            }
            yield return new WaitForSeconds(2.5f);
        }

        if (loadingObject != null)
        {
            yield return loadingObject.EndLoading();
        }
    }

    /// <summary>
    /// Checks if local Decks directory is empty. If empty, downloads individual deck .txt files from R2 bucket decks/ folder.
    /// </summary>
    public IEnumerator CheckAndDownloadDecks(LoadingObject loadingObject = null)
    {
        string deckDir = StreamingAssetsUtility.GetStreamingAssetPath("Decks", false);

        if (!Directory.Exists(deckDir))
        {
            Directory.CreateDirectory(deckDir);
        }

        string[] existingFiles = Directory.GetFiles(deckDir, "*.txt");
        if (existingFiles.Length > 0)
        {
            // Decks already exist locally; skip download
            yield break;
        }

        string baseUrl = R2CardZipUrl.Trim();
        if (baseUrl.EndsWith("/card.zip", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl.Substring(0, baseUrl.Length - "/card.zip".Length);
        }
        else if (baseUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = Path.GetDirectoryName(baseUrl).Replace("\\", "/");
        }
        baseUrl = baseUrl.TrimEnd('/');

        string decksFolderUrl = baseUrl + "/decks/";

        if (loadingObject != null && loadingObject.LoadingText != null)
        {
            loadingObject.LoadingText.text = "Fetching default decks list...";
        }

        List<string> deckFileNames = new List<string>();

        // Strategy 1: Check for list.txt or index.txt in decks/
        string listUrl = decksFolderUrl + "list.txt";
        using (UnityWebRequest listReq = UnityWebRequest.Get(listUrl))
        {
            yield return listReq.SendWebRequest();
            if (listReq.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(listReq.downloadHandler.text))
            {
                string[] lines = listReq.downloadHandler.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string clean = line.Trim();
                    if (!string.IsNullOrEmpty(clean) && !deckFileNames.Contains(clean))
                    {
                        deckFileNames.Add(clean);
                    }
                }
            }
        }

        // Strategy 2: If no list.txt, attempt to query the decks/ folder directory for HTML/XML links
        if (deckFileNames.Count == 0)
        {
            using (UnityWebRequest folderReq = UnityWebRequest.Get(decksFolderUrl))
            {
                yield return folderReq.SendWebRequest();
                if (folderReq.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(folderReq.downloadHandler.text))
                {
                    string body = folderReq.downloadHandler.text;

                    MatchCollection matches = Regex.Matches(
                        body, @"(?:<Key>decks/|href=[""]?)([^""<>\s]+\.txt)[""]?", RegexOptions.IgnoreCase);

                    foreach (Match match in matches)
                    {
                        if (match.Success && match.Groups.Count > 1)
                        {
                            string fileName = Path.GetFileName(match.Groups[1].Value.Trim());
                            if (!string.IsNullOrEmpty(fileName) && fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && !fileName.Equals("list.txt", StringComparison.OrdinalIgnoreCase) && !deckFileNames.Contains(fileName))
                            {
                                deckFileNames.Add(fileName);
                            }
                        }
                    }
                }
            }
        }

        if (deckFileNames.Count == 0)
        {
            Debug.LogWarning("[CardZipDownloader] Could not resolve individual deck files from R2 /decks/ folder.");
            if (loadingObject != null)
            {
                yield return loadingObject.EndLoading();
            }
            yield break;
        }

        // Download each individual deck .txt file into Decks folder
        int downloadedCount = 0;
        for (int i = 0; i < deckFileNames.Count; i++)
        {
            string fileName = deckFileNames[i];
            string fileUrl = decksFolderUrl + fileName;
            string targetPath = Path.Combine(deckDir, fileName).Replace("\\", "/");

            if (loadingObject != null && loadingObject.LoadingText != null)
            {
                loadingObject.LoadingText.text = $"Downloading Default Decks...\n({i + 1} / {deckFileNames.Count}): {fileName}";
            }

            using (UnityWebRequest deckReq = UnityWebRequest.Get(fileUrl))
            {
                deckReq.downloadHandler = new DownloadHandlerFile(targetPath);
                yield return deckReq.SendWebRequest();

                if (deckReq.result == UnityWebRequest.Result.Success)
                {
                    downloadedCount++;
                }
                else
                {
                    Debug.LogWarning($"[CardZipDownloader] Failed to download deck file {fileName} from {fileUrl}: {deckReq.error}");
                }
            }
        }

        Debug.Log($"[CardZipDownloader] Successfully downloaded {downloadedCount} deck files to {deckDir}.");

        if (downloadedCount > 0 && ContinuousController.instance != null)
        {
            ContinuousController.instance.LoadDeckLists();
        }

        if (loadingObject != null)
        {
            yield return loadingObject.EndLoading();
        }
    }
}
