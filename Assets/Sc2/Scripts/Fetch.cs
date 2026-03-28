

/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using UnityEngine.Networking;
using GLTFast;
using TMPro;

[System.Serializable]
public class ButtonModelPair
{
    public Button button;
    public string documentName;
    public Transform parent; 
}

public class Fetch : MonoBehaviour
{
    FirebaseFirestore db;

    [SerializeField] private List<ButtonModelPair> buttonModels;
    [SerializeField] private TextMeshProUGUI downloadText;

    private bool isDownloading = false;
    private GameObject currentModel;

    //downloaded ?
    private Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        foreach (var pair in buttonModels)
        {
            ButtonModelPair localPair = pair;

            localPair.button.onClick.AddListener(() =>
            {
                LoadFromDocument(localPair.documentName, localPair.parent);
            });
        }
    }

    void LoadFromDocument(string docName, Transform parent)
    {
        if (isDownloading)
        {
            Debug.Log("Already downloading...");
            return;
        }

        // check if downloaded
        if (modelCache.ContainsKey(docName))
        {
            Debug.Log("Loaded from cache: " + docName);

            if (currentModel != null)
                currentModel.SetActive(false);

            currentModel = modelCache[docName];

            Transform targetParent = parent != null ? parent : this.transform;
            currentModel.transform.SetParent(targetParent);
            currentModel.SetActive(true);

            return;
        }

        // if not downloaded -> download
        isDownloading = true;

        downloadText.gameObject.SetActive(true);
        downloadText.text = "Downloading...";

        db.Collection("retrieve").Document(docName)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result.Exists)
                {
                    UrlData data = task.Result.ConvertTo<UrlData>();

                    StartCoroutine(DownloadModel(data.gitURL, docName, parent));
                }
                else
                {
                    Debug.LogError("Document not found: " + docName);
                    downloadText.text = "Error!";
                    isDownloading = false;
                }
            });
    }

    IEnumerator DownloadModel(string url, string docName, Transform parent)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SendWebRequest();

        float fakeProgress = 0f;

        while (!request.isDone)
        {
            fakeProgress += Time.deltaTime * 0.3f;
            int percent = Mathf.Clamp(Mathf.RoundToInt(fakeProgress * 100), 0, 90);

            downloadText.text = "Downloading... " + percent + "%";

            yield return null;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            downloadText.text = "Downloading... 100%";

            byte[] data = request.downloadHandler.data;

            LoadModel(data, docName, parent);
        }
        else
        {
            Debug.LogError("Download failed: " + request.error);
            downloadText.text = "Download Failed";
            isDownloading = false;
        }

        yield return new WaitForSeconds(0.3f);

        downloadText.gameObject.SetActive(false);
    }

    async void LoadModel(byte[] data, string docName, Transform parent)
    {
        var gltf = new GltfImport();

        bool success = await gltf.LoadGltfBinary(data);

        if (success)
        {
            if (currentModel != null)
                currentModel.SetActive(false);

            GameObject modelParent = new GameObject(docName);

            Transform targetParent = parent != null ? parent : this.transform;

            modelParent.transform.SetParent(targetParent);
            modelParent.transform.localPosition = Vector3.zero;
            modelParent.transform.localRotation = Quaternion.identity;
            modelParent.transform.localScale = Vector3.one;

            await gltf.InstantiateMainSceneAsync(modelParent.transform);

           //cache
            modelCache[docName] = modelParent;

            currentModel = modelParent;

            Debug.Log("Model downloaded & cached: " + docName);
        }
        else
        {
            Debug.LogError("Failed to load model");
        }

        isDownloading = false;
    }
} */ //Version 4 with multiple models capacity

/*using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using UnityEngine.Networking;
using GLTFast;
using TMPro;

[System.Serializable]
public class ButtonModelPair
{
    public Button button;
    public string documentName;
    public Transform parent;
}

public class Fetch : MonoBehaviour
{
    FirebaseFirestore db;

    [Header("Model Buttons")]
    [SerializeField] private List<ButtonModelPair> buttonModels;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI downloadText;
    [SerializeField] private GameObject qualityPanel;

    [Header("Quality Buttons")]
    [SerializeField] private Button lowButton;
    [SerializeField] private Button highButton;

    private bool isDownloading = false;
    private bool hasSelectedQuality = false;

    private GameObject currentModel;

    private Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();

    public enum ModelQuality
    {
        Low,
        High
    }

    private ModelQuality selectedQuality;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // Show panel at start
        qualityPanel.SetActive(true);

        // Disable model buttons initially
        foreach (var pair in buttonModels)
        {
            pair.button.interactable = false;
        }

        // Assign button click listeners
        foreach (var pair in buttonModels)
        {
            ButtonModelPair localPair = pair;

            localPair.button.onClick.AddListener(() =>
            {
                LoadFromDocument(localPair.documentName, localPair.parent);
            });
        }
    }

    // ---------- QUALITY PANEL ----------

    public void OpenQualityPanel()
    {
        qualityPanel.SetActive(true);
        UpdateQualityUI();
    }

    public void CloseQualityPanel()
    {
        qualityPanel.SetActive(false);
    }

    public void SelectLowQuality()
    {
        selectedQuality = ModelQuality.Low;
        hasSelectedQuality = true;

        EnableModelButtons();
        UpdateQualityUI();
        qualityPanel.SetActive(false);
    }

    public void SelectHighQuality()
    {
        selectedQuality = ModelQuality.High;
        hasSelectedQuality = true;

        EnableModelButtons();
        UpdateQualityUI();
        qualityPanel.SetActive(false);
    }

    void EnableModelButtons()
    {
        foreach (var pair in buttonModels)
        {
            pair.button.interactable = true;
        }
    }

    void UpdateQualityUI()
    {
        Color selectedColor = Color.green;
        Color normalColor = Color.white;

        lowButton.image.color = selectedQuality == ModelQuality.Low ? selectedColor : normalColor;
        highButton.image.color = selectedQuality == ModelQuality.High ? selectedColor : normalColor;
    }

    // ---------- MODEL LOADING ----------

    void LoadFromDocument(string docName, Transform parent)
    {
        if (!hasSelectedQuality)
        {
            Debug.Log("Select quality first");
            return;
        }

        if (isDownloading)
        {
            Debug.Log("Already downloading");
            return;
        }

        string cacheKey = docName + "_" + selectedQuality;

        // Check cache
        if (modelCache.ContainsKey(cacheKey))
        {
            if (currentModel != null)
                currentModel.SetActive(false);

            currentModel = modelCache[cacheKey];

            Transform targetParent = parent != null ? parent : this.transform;
            currentModel.transform.SetParent(targetParent);
            currentModel.SetActive(true);

            return;
        }

        // Not cached → download
        isDownloading = true;

        downloadText.gameObject.SetActive(true);
        downloadText.text = "Downloading...";

        db.Collection("retrieve").Document(docName)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result.Exists)
                {
                    UrlData data = task.Result.ConvertTo<UrlData>();

                    string finalURL = selectedQuality == ModelQuality.Low
                        ? data.gitURL
                        : data.highURL;

                    StartCoroutine(DownloadModel(finalURL, docName, parent));
                }
                else
                {
                    Debug.LogError("Document not found: " + docName);
                    downloadText.text = "Error";
                    isDownloading = false;
                }
            });
    }

    IEnumerator DownloadModel(string url, string docName, Transform parent)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SendWebRequest();

        float fakeProgress = 0f;

        while (!request.isDone)
        {
            fakeProgress += Time.deltaTime * 0.3f;
            int percent = Mathf.Clamp(Mathf.RoundToInt(fakeProgress * 100), 0, 90);

            downloadText.text = "Downloading... " + percent + "%";

            yield return null;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            downloadText.text = "Downloading... 100%";

            byte[] data = request.downloadHandler.data;

            LoadModel(data, docName, parent);
        }
        else
        {
            Debug.LogError("Download failed: " + request.error);
            downloadText.text = "Download Failed";
            isDownloading = false;
        }

        yield return new WaitForSeconds(0.3f);
        downloadText.gameObject.SetActive(false);
    }

    async void LoadModel(byte[] data, string docName, Transform parent)
    {
        var gltf = new GltfImport();

        bool success = await gltf.LoadGltfBinary(data);

        if (success)
        {
            if (currentModel != null)
                currentModel.SetActive(false);

            GameObject modelParent = new GameObject(docName);

            Transform targetParent = parent != null ? parent : this.transform;

            modelParent.transform.SetParent(targetParent);
            modelParent.transform.localPosition = Vector3.zero;
            modelParent.transform.localRotation = Quaternion.identity;
            modelParent.transform.localScale = Vector3.one;

            await gltf.InstantiateMainSceneAsync(modelParent.transform);

            string cacheKey = docName + "_" + selectedQuality;
            modelCache[cacheKey] = modelParent;

            currentModel = modelParent;

            Debug.Log("Model loaded: " + cacheKey);
        }
        else
        {
            Debug.LogError("Failed to load model");
        }

        isDownloading = false;
    }
}*/
//Version 5 with one by one quality download


/* using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using UnityEngine.Networking;
using GLTFast;
using TMPro;

[System.Serializable]
public class ButtonModelPair
{
    public Button button;
    public string documentName;
    public Transform parent;
}

public class Fetch : MonoBehaviour
{
    FirebaseFirestore db;

    [Header("Model Buttons")]
    [SerializeField] private List<ButtonModelPair> buttonModels;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI downloadText;
    [SerializeField] private GameObject qualityPanel;

    [Header("Quality Buttons")]
    [SerializeField] private Button lowButton;
    [SerializeField] private Button highButton;

    private bool isDownloading = false;
    private bool hasSelectedQuality = false;

    private GameObject currentModel;

    private Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();

    public enum ModelQuality { Low, High }
    private ModelQuality selectedQuality;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        qualityPanel.SetActive(true);

        // Model buttons stay disabled — they activate models from cache after download
        foreach (var pair in buttonModels)
        {
            pair.button.interactable = false;
        }

        // Buttons now just show/hide the cached model for that slot
        foreach (var pair in buttonModels)
        {
            ButtonModelPair localPair = pair;
            localPair.button.onClick.AddListener(() =>
            {
                ShowCachedModel(localPair.documentName, localPair.parent);
            });
        }
    }

    // ---------- QUALITY PANEL ----------

    public void OpenQualityPanel()
    {
        qualityPanel.SetActive(true);
        UpdateQualityUI();
    }

    public void CloseQualityPanel()
    {
        qualityPanel.SetActive(false);
    }

    public void SelectLowQuality()
    {
        if (isDownloading) return;

        selectedQuality = ModelQuality.Low;
        hasSelectedQuality = true;

        UpdateQualityUI();
        qualityPanel.SetActive(false);

        // Batch download all models at Low quality
        StartCoroutine(DownloadAllModels());
    }

    public void SelectHighQuality()
    {
        if (isDownloading) return;

        selectedQuality = ModelQuality.High;
        hasSelectedQuality = true;

        UpdateQualityUI();
        qualityPanel.SetActive(false);

        // Batch download all models at High quality
        StartCoroutine(DownloadAllModels());
    }

    void UpdateQualityUI()
    {
        lowButton.image.color = selectedQuality == ModelQuality.Low ? Color.green : Color.white;
        highButton.image.color = selectedQuality == ModelQuality.High ? Color.green : Color.white;
    }

    // ---------- BATCH DOWNLOAD ----------

    IEnumerator DownloadAllModels()
    {
        isDownloading = true;

        // Disable model buttons while downloading
        foreach (var pair in buttonModels)
            pair.button.interactable = false;

        downloadText.gameObject.SetActive(true);

        int total = buttonModels.Count;
        int completed = 0;

        foreach (var pair in buttonModels)
        {
            string cacheKey = pair.documentName + "_" + selectedQuality;

            // Skip if already cached for this quality
            if (modelCache.ContainsKey(cacheKey))
            {
                completed++;
                downloadText.text = $"Loading... {completed}/{total}";
                continue;
            }

            downloadText.text = $"Fetching {pair.documentName} ({completed + 1}/{total})...";

            // Fetch Firestore URL — wrap in a coroutine-friendly wait
            string resolvedURL = null;
            bool fetchDone = false;
            bool fetchError = false;

            db.Collection("retrieve").Document(pair.documentName)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.Result.Exists)
                    {
                        UrlData data = task.Result.ConvertTo<UrlData>();
                        resolvedURL = selectedQuality == ModelQuality.Low
                            ? data.gitURL
                            : data.highURL;
                    }
                    else
                    {
                        Debug.LogError("Document not found: " + pair.documentName);
                        fetchError = true;
                    }
                    fetchDone = true;
                });

            // Wait for Firestore response
            yield return new WaitUntil(() => fetchDone);

            if (fetchError)
            {
                completed++;
                continue;
            }

            // Download the model bytes
            bool modelDone = false;
            bool modelError = false;

            yield return StartCoroutine(DownloadAndLoadModel(
                resolvedURL,
                pair.documentName,
                pair.parent,
                completed + 1,
                total,
                success =>
                {
                    modelError = !success;
                    modelDone = true;
                }
            ));

            yield return new WaitUntil(() => modelDone);

            completed++;
            downloadText.text = $"Done {completed}/{total}";
        }

        downloadText.text = "All models ready!";
        yield return new WaitForSeconds(1f);
        downloadText.gameObject.SetActive(false);

        // Enable model buttons — everything is cached
        foreach (var pair in buttonModels)
            pair.button.interactable = true;

        isDownloading = false;
    }

    // ---------- DOWNLOAD + LOAD (single model, used in batch) ----------

    IEnumerator DownloadAndLoadModel(string url, string docName, Transform parent, int index, int total, System.Action<bool> onDone)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SendWebRequest();

        float fakeProgress = 0f;

        while (!request.isDone)
        {
            fakeProgress += Time.deltaTime * 0.3f;
            int percent = Mathf.Clamp(Mathf.RoundToInt(fakeProgress * 100), 0, 90);
            downloadText.text = $"Downloading {index}/{total}... {percent}%";
            yield return null;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Download failed: " + request.error);
            downloadText.text = $"Failed: {docName}";
            onDone?.Invoke(false);
            yield break;
        }

        downloadText.text = $"Downloading {index}/{total}... 100%";

        byte[] data = request.downloadHandler.data;

        // LoadModel is async — bridge it with a flag
        bool loadDone = false;
        bool loadOk = false;

        LoadModel(data, docName, parent, result =>
        {
            loadOk = result;
            loadDone = true;
        });

        yield return new WaitUntil(() => loadDone);

        onDone?.Invoke(loadOk);
    }

    async void LoadModel(byte[] data, string docName, Transform parent, System.Action<bool> onDone)
    {
        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data);

        if (success)
        {
            GameObject modelParent = new GameObject(docName);

            Transform targetParent = parent != null ? parent : this.transform;
            modelParent.transform.SetParent(targetParent);
            modelParent.transform.localPosition = Vector3.zero;
            modelParent.transform.localRotation = Quaternion.identity;
            modelParent.transform.localScale = Vector3.one;

            await gltf.InstantiateMainSceneAsync(modelParent.transform);

            // Hide by default; shown when the user taps its button
            modelParent.SetActive(false);

            string cacheKey = docName + "_" + selectedQuality;
            modelCache[cacheKey] = modelParent;

            Debug.Log("Model cached: " + cacheKey);
        }
        else
        {
            Debug.LogError("Failed to load GLTF: " + docName);
        }

        onDone?.Invoke(success);
    }

    // ---------- SHOW CACHED MODEL ----------

    void ShowCachedModel(string docName, Transform parent)
    {
        string cacheKey = docName + "_" + selectedQuality;

        if (!modelCache.ContainsKey(cacheKey))
        {
            Debug.LogWarning("Model not in cache: " + cacheKey);
            return;
        }

        if (currentModel != null)
            currentModel.SetActive(false);

        currentModel = modelCache[cacheKey];

        Transform targetParent = parent != null ? parent : this.transform;
        currentModel.transform.SetParent(targetParent);
        currentModel.SetActive(true);
    }
}*/
//batch download version
/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using UnityEngine.Networking;
using GLTFast;
using TMPro;

[System.Serializable]
public class ButtonModelPair
{
    public Button button;
    public string documentName;
    public Transform parent;
}

public class Fetch : MonoBehaviour
{
    FirebaseFirestore db;

    [Header("Model Buttons")]
    [SerializeField] private List<ButtonModelPair> buttonModels;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI downloadText;
    [SerializeField] private GameObject qualityPanel;

    [Header("Quality Buttons")]
    [SerializeField] private Button lowButton;
    [SerializeField] private Button highButton;

    private bool isDownloading = false;
    private bool hasSelectedQuality = false;

    private GameObject currentModel;

    private Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();

    // Local storage folder inside Application.persistentDataPath
    private string LocalStorageFolder => Path.Combine(Application.persistentDataPath, "CachedModels");

    public enum ModelQuality { Low, High }
    private ModelQuality selectedQuality;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // Clear cache in Unity Editor to ensure fresh downloads
#if UNITY_EDITOR
        ClearLocalStorage();
#endif

        // Make sure local folder exists on device
        if (!Directory.Exists(LocalStorageFolder))
            Directory.CreateDirectory(LocalStorageFolder);

        qualityPanel.SetActive(true);

        foreach (var pair in buttonModels)
            pair.button.interactable = false;

        foreach (var pair in buttonModels)
        {
            ButtonModelPair localPair = pair;
            localPair.button.onClick.AddListener(() =>
            {
                ShowCachedModel(localPair.documentName, localPair.parent);
            });
        }
    }
    // ---------- QUALITY PANEL ----------

    public void OpenQualityPanel()
    {
        qualityPanel.SetActive(true);
        UpdateQualityUI();
    }

    public void CloseQualityPanel()
    {
        qualityPanel.SetActive(false);
    }

    public void SelectLowQuality()
    {
        if (isDownloading) return;

        selectedQuality = ModelQuality.Low;
        hasSelectedQuality = true;

        UpdateQualityUI();
        qualityPanel.SetActive(false);

        StartCoroutine(LoadAllModels());
    }

    public void SelectHighQuality()
    {
        if (isDownloading) return;

        selectedQuality = ModelQuality.High;
        hasSelectedQuality = true;

        UpdateQualityUI();
        qualityPanel.SetActive(false);

        StartCoroutine(LoadAllModels());
    }

    void UpdateQualityUI()
    {
        lowButton.image.color = selectedQuality == ModelQuality.Low ? Color.green : Color.white;
        highButton.image.color = selectedQuality == ModelQuality.High ? Color.green : Color.white;
    }

    // ---------- LOCAL STORAGE HELPERS ----------

    string GetLocalFilePath(string docName, ModelQuality quality)
    {
        string fileName = docName + "_" + quality + ".glb";
        return Path.Combine(LocalStorageFolder, fileName);
    }

    bool IsModelSavedLocally(string docName, ModelQuality quality)
    {
        return File.Exists(GetLocalFilePath(docName, quality));
    }

    void SaveModelLocally(string docName, ModelQuality quality, byte[] data)
    {
        string path = GetLocalFilePath(docName, quality);
        File.WriteAllBytes(path, data);
        Debug.Log($"Saved to device: {path}");
    }

    byte[] LoadModelFromDisk(string docName, ModelQuality quality)
    {
        string path = GetLocalFilePath(docName, quality);
        return File.ReadAllBytes(path);
    }

    // ---------- MAIN LOAD LOOP ----------
    // Priority order:
    //   1. Runtime memory cache  → instant
    //   2. Saved on device disk  → load from file, no network
    //   3. Not on device         → download, save, then load

    IEnumerator LoadAllModels()
    {
        isDownloading = true;

        foreach (var pair in buttonModels)
            pair.button.interactable = false;

        downloadText.gameObject.SetActive(true);

        int total = buttonModels.Count;
        int completed = 0;

        foreach (var pair in buttonModels)
        {
            string cacheKey = pair.documentName + "_" + selectedQuality;

            // w Already in runtime memory
            if (modelCache.ContainsKey(cacheKey))
            {
                completed++;
                downloadText.text = $"Ready {completed}/{total}";
                continue;
            }

            //  Saved on device — load from disk
            if (IsModelSavedLocally(pair.documentName, selectedQuality))
            {
                downloadText.text = $"Loading from device {completed + 1}/{total}...";

                byte[] localData = LoadModelFromDisk(pair.documentName, selectedQuality);

                bool loadDone = false;
                bool loadOk = false;

                LoadModel(localData, pair.documentName, pair.parent, result =>
                {
                    loadOk = result;
                    loadDone = true;
                });

                yield return new WaitUntil(() => loadDone);

                completed++;
                downloadText.text = $"Loaded {completed}/{total}";
                continue;
            }

            //  Not on device — fetch Firestore URL and download
            downloadText.text = $"Fetching {pair.documentName} ({completed + 1}/{total})...";

            string resolvedURL = null;
            bool fetchDone = false;
            bool fetchError = false;

            db.Collection("retrieve").Document(pair.documentName)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.Result.Exists)
                    {
                        UrlData data = task.Result.ConvertTo<UrlData>();
                        resolvedURL = selectedQuality == ModelQuality.Low
                            ? data.gitURL
                            : data.highURL;
                    }
                    else
                    {
                        Debug.LogError("Document not found: " + pair.documentName);
                        fetchError = true;
                    }
                    fetchDone = true;
                });

            yield return new WaitUntil(() => fetchDone);

            if (fetchError)
            {
                completed++;
                continue;
            }

            bool modelDone = false;
            bool modelError = false;

            yield return StartCoroutine(DownloadAndLoadModel(
                resolvedURL,
                pair.documentName,
                pair.parent,
                completed + 1,
                total,
                success =>
                {
                    modelError = !success;
                    modelDone = true;
                }
            ));

            yield return new WaitUntil(() => modelDone);

            completed++;
            downloadText.text = $"Done {completed}/{total}";
        }

        downloadText.text = "All models ready!";
        yield return new WaitForSeconds(1f);
        downloadText.gameObject.SetActive(false);

        foreach (var pair in buttonModels)
            pair.button.interactable = true;

        isDownloading = false;
    }

    // ---------- DOWNLOAD - SAVE TO DISK - LOAD ----------

    IEnumerator DownloadAndLoadModel(string url, string docName, Transform parent, int index, int total, System.Action<bool> onDone)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SendWebRequest();

        float fakeProgress = 0f;

        while (!request.isDone)
        {
            fakeProgress += Time.deltaTime * 0.3f;
            int percent = Mathf.Clamp(Mathf.RoundToInt(fakeProgress * 100), 0, 90);
            downloadText.text = $"Downloading {index}/{total}... {percent}%";
            yield return null;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Download failed: " + request.error);
            downloadText.text = $"Failed: {docName}";
            onDone?.Invoke(false);
            yield break;
        }

        downloadText.text = $"Downloading {index}/{total}... 100%";

        byte[] data = request.downloadHandler.data;

        //  Save to device so next app launch skips download entirely
        SaveModelLocally(docName, selectedQuality, data);

        bool loadDone = false;
        bool loadOk = false;

        LoadModel(data, docName, parent, result =>
        {
            loadOk = result;
            loadDone = true;
        });

        yield return new WaitUntil(() => loadDone);

        onDone?.Invoke(loadOk);
    }

    // ---------- GLTF PARSE + INSTANTIATE ----------

    async void LoadModel(byte[] data, string docName, Transform parent, System.Action<bool> onDone)
    {
        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data);

        if (success)
        {
            GameObject modelParent = new GameObject(docName);

            Transform targetParent = parent != null ? parent : this.transform;
            modelParent.transform.SetParent(targetParent);
            modelParent.transform.localPosition = Vector3.zero;
            modelParent.transform.localRotation = Quaternion.identity;
            modelParent.transform.localScale = Vector3.one;

            await gltf.InstantiateMainSceneAsync(modelParent.transform);

            // Hidden by default; button tap reveals it
            modelParent.SetActive(false);

            string cacheKey = docName + "_" + selectedQuality;
            modelCache[cacheKey] = modelParent;

            Debug.Log("Model ready: " + cacheKey);
        }
        else
        {
            Debug.LogError("Failed to parse GLTF: " + docName);
        }

        onDone?.Invoke(success);
    }

    // ---------- SHOW MODEL ----------

    void ShowCachedModel(string docName, Transform parent)
    {
        string cacheKey = docName + "_" + selectedQuality;

        if (!modelCache.ContainsKey(cacheKey))
        {
            Debug.LogWarning("Model not in cache: " + cacheKey);
            return;
        }

        if (currentModel != null)
            currentModel.SetActive(false);

        currentModel = modelCache[cacheKey];

        Transform targetParent = parent != null ? parent : this.transform;
        currentModel.transform.SetParent(targetParent);
        currentModel.SetActive(true);
    }

    // ----------  CLEAR LOCAL STORAGE ----------


    public void ClearLocalStorage()
    {
        if (Directory.Exists(LocalStorageFolder))
        {
            Directory.Delete(LocalStorageFolder, recursive: true);
            Directory.CreateDirectory(LocalStorageFolder);
            modelCache.Clear();
            Debug.Log("Local model cache cleared.");
        }
    }
}*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.Rendering;

[System.Serializable]
public class ButtonModelPair
{
    public Button button;
    public string documentName;
    public Transform parent;
}

public class Fetch : MonoBehaviour
{
    FirebaseFirestore db;

    [Header("Model Buttons")]
    [SerializeField] private List<ButtonModelPair> buttonModels;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI downloadText;
    [SerializeField] private GameObject qualityPanel;

    [Header("Quality Buttons")]
    [SerializeField] private Button lowButton;
    [SerializeField] private Button highButton;

    private bool isDownloading = false;
    private bool hasSelectedQuality = false;

    private GameObject currentModel;

    private Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();
    private Dictionary<string, AssetBundle> bundleCache = new Dictionary<string, AssetBundle>();

    private string LocalStorageFolder => Path.Combine(Application.persistentDataPath, "CachedBundles");
    private string URLRegistryPath => Path.Combine(Application.persistentDataPath, "url_registry.json");

    private Dictionary<string, string> urlRegistry = new Dictionary<string, string>();

    public enum ModelQuality { Low, High }
    private ModelQuality selectedQuality;

    void Start()
    {
        // Auto-clear cache when playing in Unity Editor so changes always fetch fresh
#if UNITY_EDITOR
        ClearLocalStorage();
        Debug.Log("Editor mode: Cache cleared automatically on play");
#endif

        db = FirebaseFirestore.DefaultInstance;

        if (!Directory.Exists(LocalStorageFolder))
            Directory.CreateDirectory(LocalStorageFolder);

        LoadURLRegistry();

        qualityPanel.SetActive(true);

        foreach (var pair in buttonModels)
            pair.button.interactable = false;

        foreach (var pair in buttonModels)
        {
            ButtonModelPair localPair = pair;
            localPair.button.onClick.AddListener(() =>
            {
                ShowCachedModel(localPair.documentName, localPair.parent);
            });
        }
    }

    // ---------- QUALITY PANEL ----------

    public void OpenQualityPanel()
    {
        qualityPanel.SetActive(true);
        UpdateQualityUI();
    }

    public void CloseQualityPanel()
    {
        qualityPanel.SetActive(false);
    }

    public void SelectLowQuality()
    {
        if (isDownloading) return;
        selectedQuality = ModelQuality.Low;
        hasSelectedQuality = true;
        UpdateQualityUI();
        qualityPanel.SetActive(false);
        StartCoroutine(LoadAllModels());
    }

    public void SelectHighQuality()
    {
        if (isDownloading) return;
        selectedQuality = ModelQuality.High;
        hasSelectedQuality = true;
        UpdateQualityUI();
        qualityPanel.SetActive(false);
        StartCoroutine(LoadAllModels());
    }

    void UpdateQualityUI()
    {
        lowButton.image.color = selectedQuality == ModelQuality.Low ? Color.green : Color.white;
        highButton.image.color = selectedQuality == ModelQuality.High ? Color.green : Color.white;
    }

    // ---------- URL REGISTRY ----------

    [System.Serializable]
    private class URLRegistryData
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();
    }

    void LoadURLRegistry()
    {
        urlRegistry.Clear();
        if (!File.Exists(URLRegistryPath)) return;

        try
        {
            string json = File.ReadAllText(URLRegistryPath);
            URLRegistryData data = JsonUtility.FromJson<URLRegistryData>(json);

            for (int i = 0; i < data.keys.Count; i++)
                urlRegistry[data.keys[i]] = data.values[i];

            Debug.Log($"URL registry loaded — {urlRegistry.Count} entries");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to load URL registry: " + e.Message);
        }
    }

    void SaveURLRegistry()
    {
        try
        {
            URLRegistryData data = new URLRegistryData();
            foreach (var kvp in urlRegistry)
            {
                data.keys.Add(kvp.Key);
                data.values.Add(kvp.Value);
            }
            File.WriteAllText(URLRegistryPath, JsonUtility.ToJson(data, true));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to save URL registry: " + e.Message);
        }
    }

    bool IsURLUnchanged(string docName, ModelQuality quality, string currentURL)
    {
        string key = docName + "_" + quality;
        return urlRegistry.ContainsKey(key) && urlRegistry[key] == currentURL;
    }

    void RegisterURL(string docName, ModelQuality quality, string url)
    {
        urlRegistry[docName + "_" + quality] = url;
        SaveURLRegistry();
    }

    // ---------- LOCAL STORAGE ----------

    string GetLocalFilePath(string docName, ModelQuality quality)
        => Path.Combine(LocalStorageFolder, docName + "_" + quality + ".bundle");

    bool IsBundleSavedLocally(string docName, ModelQuality quality)
        => File.Exists(GetLocalFilePath(docName, quality));

    void SaveBundleLocally(string docName, ModelQuality quality, byte[] data)
    {
        File.WriteAllBytes(GetLocalFilePath(docName, quality), data);
        Debug.Log($"Bundle saved: {GetLocalFilePath(docName, quality)}");
    }

    void DeleteLocalBundle(string docName, ModelQuality quality)
    {
        string path = GetLocalFilePath(docName, quality);
        if (File.Exists(path)) File.Delete(path);
        Debug.Log($"Deleted old bundle: {docName}_{quality}");
    }

    // ---------- MAIN LOAD LOOP ----------

    IEnumerator LoadAllModels()
    {
        isDownloading = true;

        foreach (var pair in buttonModels)
            pair.button.interactable = false;

        downloadText.gameObject.SetActive(true);

        int total = buttonModels.Count;
        int completed = 0;

        foreach (var pair in buttonModels)
        {
            string cacheKey = pair.documentName + "_" + selectedQuality;

            //  Already in runtime memory — skip
            if (modelCache.ContainsKey(cacheKey))
            {
                completed++;
                downloadText.text = $"Ready {completed}/{total}";
                continue;
            }

            //  Always fetch Firebase URL first to detect changes
            downloadText.text = $"Checking {pair.documentName} ({completed + 1}/{total})...";

            string resolvedURL = null;
            bool fetchDone = false;
            bool fetchError = false;

            db.Collection("retrieve").Document(pair.documentName)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.Result.Exists)
                    {
                        UrlData data = task.Result.ConvertTo<UrlData>();
                        resolvedURL = selectedQuality == ModelQuality.Low
                            ? data.gitURL
                            : data.highURL;
                    }
                    else
                    {
                        Debug.LogError("Document not found: " + pair.documentName);
                        fetchError = true;
                    }
                    fetchDone = true;
                });

            yield return new WaitUntil(() => fetchDone);

            if (fetchError) { completed++; continue; }

            // Bundle on disk AND URL unchanged → load from disk
            if (IsBundleSavedLocally(pair.documentName, selectedQuality) &&
                IsURLUnchanged(pair.documentName, selectedQuality, resolvedURL))
            {
                downloadText.text = $"Loading from device ({completed + 1}/{total})...";

                bool loadDone = false;
                bool loadOk = false;

                yield return StartCoroutine(LoadBundleFromDisk(
                    pair.documentName,
                    pair.parent,
                    result => { loadOk = result; loadDone = true; }
                ));

                yield return new WaitUntil(() => loadDone);

                completed++;
                downloadText.text = $"Loaded {completed}/{total}";
                continue;
            }

            //  URL changed or file missing → delete old, re-download
            if (IsBundleSavedLocally(pair.documentName, selectedQuality))
            {
                Debug.Log($"URL changed for {pair.documentName} — re-downloading");
                DeleteLocalBundle(pair.documentName, selectedQuality);

                if (bundleCache.ContainsKey(cacheKey))
                {
                    bundleCache[cacheKey].Unload(true);
                    bundleCache.Remove(cacheKey);
                }

                if (modelCache.ContainsKey(cacheKey))
                {
                    Destroy(modelCache[cacheKey]);
                    modelCache.Remove(cacheKey);
                }
            }

            bool modelDone = false;
            bool modelError = false;

            yield return StartCoroutine(DownloadAndLoadBundle(
                resolvedURL,
                pair.documentName,
                pair.parent,
                completed + 1,
                total,
                success => { modelError = !success; modelDone = true; }
            ));

            yield return new WaitUntil(() => modelDone);

            completed++;
            downloadText.text = $"Done {completed}/{total}";
        }

        downloadText.text = "All models ready!";
        yield return new WaitForSeconds(1f);
        downloadText.gameObject.SetActive(false);

        foreach (var pair in buttonModels)
            pair.button.interactable = true;

        isDownloading = false;
    }

    // ---------- LOAD FROM DISK ----------

    IEnumerator LoadBundleFromDisk(string docName, Transform parent, System.Action<bool> onDone)
    {
        string path = GetLocalFilePath(docName, selectedQuality);
        string cacheKey = docName + "_" + selectedQuality;

        var bundleRequest = AssetBundle.LoadFromFileAsync(path);
        yield return bundleRequest;

        AssetBundle bundle = bundleRequest.assetBundle;

        if (bundle == null)
        {
            Debug.LogError("Failed to load bundle from disk: " + docName);
            onDone?.Invoke(false);
            yield break;
        }

        // Auto-find first asset inside bundle
        string[] names = bundle.GetAllAssetNames();
        var assetRequest = bundle.LoadAssetAsync<GameObject>(names[0]);
        yield return assetRequest;

        GameObject prefab = assetRequest.asset as GameObject;

        if (prefab == null)
        {
            Debug.LogError("Failed to load prefab from bundle: " + docName);
            bundle.Unload(false);
            onDone?.Invoke(false);
            yield break;
        }

        Transform targetParent = parent != null ? parent : this.transform;

        //  Instantiate with NO transform overrides — uses prefab's saved values
        GameObject instance = Instantiate(prefab, targetParent);

        instance.SetActive(false);

        FixMaterials(instance);

        bundleCache[cacheKey] = bundle;
        modelCache[cacheKey] = instance;

        Debug.Log("Bundle loaded: " + cacheKey);
        onDone?.Invoke(true);
    }

    // ---------- DOWNLOAD → SAVE → LOAD ----------

    IEnumerator DownloadAndLoadBundle(string url, string docName, Transform parent,
        int index, int total, System.Action<bool> onDone)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SendWebRequest();

        float fakeProgress = 0f;

        while (!request.isDone)
        {
            fakeProgress += Time.deltaTime * 0.3f;
            int percent = Mathf.Clamp(Mathf.RoundToInt(fakeProgress * 100), 0, 90);
            downloadText.text = $"Downloading {index}/{total}... {percent}%";
            yield return null;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Download failed: " + request.error);
            downloadText.text = $"Failed: {docName}";
            onDone?.Invoke(false);
            yield break;
        }

        downloadText.text = $"Downloading {index}/{total}... 100%";

        byte[] data = request.downloadHandler.data;

        //  Save bundle to device
        SaveBundleLocally(docName, selectedQuality, data);

        //  Register URL for future change detection
        RegisterURL(docName, selectedQuality, url);

        bool loadDone = false;
        bool loadOk = false;

        yield return StartCoroutine(LoadBundleFromDisk(
            docName,
            parent,
            result => { loadOk = result; loadDone = true; }
        ));

        yield return new WaitUntil(() => loadDone);

        onDone?.Invoke(loadOk);
    }

    // ---------- SHOW MODEL ----------

    void ShowCachedModel(string docName, Transform parent)
    {
        string key = docName + "_" + selectedQuality;

        if (!modelCache.ContainsKey(key))
        {
            Debug.LogWarning("Model not in cache: " + key);
            return;
        }

        if (currentModel != null)
            currentModel.SetActive(false);

        currentModel = modelCache[key];

        // ✅ Just re-parent and show — no transform overrides at all
        currentModel.transform.SetParent(parent != null ? parent : this.transform);
        currentModel.SetActive(true);
    }

    // ---------- URP MATERIAL FIX ----------

    void FixMaterials(GameObject root)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("URP Lit shader not found!");
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (var renderer in renderers)
        {
            var mats = renderer.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                var oldMat = mats[i];
                if (oldMat == null) continue;

                var newMat = new Material(shader);

                if (oldMat.mainTexture != null)
                    newMat.SetTexture("_BaseMap", oldMat.mainTexture);

                if (oldMat.HasProperty("_Color"))
                    newMat.SetColor("_BaseColor", oldMat.color);

                if (oldMat.HasProperty("_BumpMap"))
                {
                    newMat.SetTexture("_BumpMap", oldMat.GetTexture("_BumpMap"));
                    newMat.EnableKeyword("_NORMALMAP");
                }

                if (oldMat.HasProperty("_MetallicGlossMap"))
                    newMat.SetTexture("_MetallicGlossMap", oldMat.GetTexture("_MetallicGlossMap"));

                newMat.SetFloat("_Surface", 0);
                newMat.renderQueue = (int)RenderQueue.Geometry;

                mats[i] = newMat;
            }

            renderer.materials = mats;
        }
    }

    // ---------- CLEAR LOCAL STORAGE ----------

    public void ClearLocalStorage()
    {
        foreach (var bundle in bundleCache.Values)
            bundle.Unload(true);
        bundleCache.Clear();

        foreach (var model in modelCache.Values)
            Destroy(model);
        modelCache.Clear();

        if (Directory.Exists(LocalStorageFolder))
        {
            Directory.Delete(LocalStorageFolder, recursive: true);
            Directory.CreateDirectory(LocalStorageFolder);
        }

        if (File.Exists(URLRegistryPath))
            File.Delete(URLRegistryPath);

        urlRegistry.Clear();

        Debug.Log("Local storage cleared.");
    }

    void OnDestroy()
    {
        foreach (var bundle in bundleCache.Values)
            bundle.Unload(false);
    }
}