# Arishna_Trial1

A Unity 6 AR app for Android that lets users view 3D models in augmented reality. Models are downloaded from GitHub via Firebase Firestore and cached locally on the device.

---

## 📱 Features

- AR plane detection and model placement using ARFoundation
- Low / High quality model selection before downloading
- All models download at once when quality is selected
- Models are cached on device — no re-download on next launch
- Automatic re-download if the URL in Firebase changes
- URP material fix applied at runtime to prevent pink materials on Android
- Switch between multiple models instantly after download

---

## 🛠️ Tech Stack

- **Unity 6** — Universal Render Pipeline (URP)
- **ARFoundation + ARCore** — AR plane detection
- **Firebase Firestore** — stores model download URLs
- **GitHub** — hosts AssetBundle files
- **AssetBundles** — platform-compiled model packages
- **Git LFS** — handles large Firebase plugin files

---

## ⚙️ How the Fetch System Works

### Firebase Structure

Each model has a document in the `retrieve` Firestore collection:

```
retrieve/
└── Model1/
    ├── gitURL:  "https://raw.githubusercontent.com/.../model1_low.bundle"
    └── highURL: "https://raw.githubusercontent.com/.../model1_high.bundle"
```

### Load Priority

When the user selects a quality, for each model the script checks in this order:

1. **Already in memory** → show instantly, skip everything
2. **Saved on device + URL unchanged** → load from local storage, no network needed
3. **URL changed or file missing** → download fresh, save to device, update URL registry

### URL Change Detection

A `url_registry.json` file is saved on the device alongside the bundles. It tracks which URL was used to download each model. If the URL in Firebase changes, the old bundle is deleted and a fresh one is downloaded automatically.

### Local Storage Location

```
Android: /data/data/<app>/files/CachedBundles/
Editor:  C:/Users/.../AppData/LocalLow/<company>/<app>/CachedBundles/
```

---

## 🚀 Setup & Running

### Requirements

- Unity 6 with URP, ARFoundation, ARCore XR Plugin
- Firebase Firestore configured with `google-services.json` in `Assets/`
- Android Build Support in Unity Hub
- Git LFS installed

### Clone

```bash
git clone https://github.com/Gunjan2004/Arishna_Test1.0.git
```

### Build AssetBundles

1. Import models into Unity and create Prefabs
2. Assign an **AssetBundle name** at the bottom of each Prefab's Inspector
3. Click **Tools → Build AssetBundles Android** in the top menu
4. Upload the generated `.bundle` files from `Assets/AssetBundles/Android/` to GitHub
5. Update Firebase Firestore URLs to point to the raw GitHub links

### Build to Android

1. **File → Build Settings → Android → Switch Platform**
2. Player Settings → Scripting Backend: `IL2CPP`, Architecture: `ARM64`
3. Connect device → **Build and Run**

---

## 🔧 Inspector Setup

On the `Fetch` component in your scene, assign:

| Field | What to set |
|---|---|
| Button Models | One entry per model — Button, Document Name, Parent Transform |
| Download Text | TMP text shown during download |
| Quality Panel | The quality selection panel GameObject |
| Low / High Button | The two quality selection buttons |

`Document Name` must match the Firestore document name exactly.

---

## 🐛 Common Issues

| Problem | Fix |
|---|---|
| Pink materials on Android | Handled automatically by `FixMaterials()` at runtime |
| Model not loading | Check document name in Inspector matches Firestore exactly |
| Corrupt bundle on disk | Press **C** in Editor play mode to clear cache, or call `ClearLocalStorage()` |
| AR planes not detected | Use in good lighting, move device slowly over a flat surface |

---

## 🔧 Editor Notes

- Cache is **auto-cleared every Play** in the Editor so you always get fresh downloads while testing
- To clear cache from a UI button wire it to `ClearLocalStorage()` on the Fetch component
