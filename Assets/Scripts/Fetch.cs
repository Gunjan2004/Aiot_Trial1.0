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