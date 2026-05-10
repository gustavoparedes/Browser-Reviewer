# Browser-Reviewer
---
Browser Reviewer is a portable forensic tool for analyzing user activity in Firefox-based and Chrome-based browsers for Windows platforms. It extracts and displays:
**Browsing history, downloads, bookmarks, autofill data, cookies, cache, sessions, extensions, saved logins metadata, Local Storage, Session Storage, and IndexedDB**. 
The tool allows analysts to tag, comment, and export reports in PDF.

It requires no installation and can be executed directly from a USB drive ideal for forensic workflows with minimal footprint on the target system.

**Download compiled version** [here](https://github.com/gustavoparedes/Browser-Reviewer/releases/download/v1.1/BrowserReviewer-v1.1.rar).


## What’s New (v1.1 — 2026-05-09)
- **URLhaus intelligence**: Browser Reviewer adds URLhaus-based detection for known malware-distribution URLs and exposes those hits as URLhaus / Malware Distribution during review.
- **Artifact categories**: Category/facet navigation is expanded beyond history, making it easier to drill into bookmarks, autofill, cookies, cache, sessions, extensions, saved logins metadata, Local Storage, Session Storage, and IndexedDB.
- **Clearer media grouping**: Image, audio, and video file groups use more consistent media classification and icons across downloads and cache.


## What’s New (v1.0 — 2026-04-29)

- **Major artifact coverage expansion**: Browser Reviewer now extracts and reviews **cookies, cache, sessions, extensions, saved logins metadata, Local Storage, Session Storage, and IndexedDB**, in addition to history, downloads, bookmarks, and autofill.
- **Improved browser identification**: Better distinction between **Firefox-like** and **Chrome/Chromium-based** browsers, including known embedded containers such as **Outlook, Visual Studio, Windows Search, OneDrive , OpenAI Codex , Steam Embedded Chromium, DeepL**, and others.
- **Stronger artifact traceability**: The `File` field now more consistently points to the real source artifact, reducing ambiguity during forensic review.
- **Hardened Firefox extraction**: Improved handling of locked Firefox files, temporary files, profile artifacts, and Firefox-specific autofill data.
- **Cleaner CLI workflow**: GUI and CLI extraction now share the same core parser path, reducing behavioral differences between interactive and headless runs.
- **Improved reports**: PDF and HTML reports were redesigned for clearer review, with better structure, filtering, sorting, long-URL handling, labels, comments, and artifact context.

---

## Features

- Extracts and visualizes browser artifacts from **Firefox-like** and **Chrome/Chromium-based** browsers, including embedded WebView2/Chromium containers.
- Supports a broad set of artifacts: **history, downloads, bookmarks, autofill, cookies, cache, sessions, extensions, saved logins metadata, Local Storage, Session Storage, and IndexedDB**.
- Identifies known browser/application containers such as **Firefox, Chrome, Edge, Brave, OneDrive, OpenAI Codex , Steam**, and other Chromium-based applications.
- Preserves artifact source traceability through the `File` field, helping analysts understand where each record came from.
- Provides **Label Manager** and **Comments** to tag, annotate, and organize findings.
- Powerful search with simple text or **RegExp**, plus time-range filtering and time zone offset control.
- Interactive review interface with sortable/filterable grids and artifact-focused navigation.
- Export options:
  - **PDF detail report** for selected/reviewed records.
  - **HTML table export** with sorting, filtering, searchable column filters, and long-URL handling.
  - **HTML label report** grouped by artifact and browser/container.
  - **Timeline HTML report** for visual review of browser activity over time.
- **Command-line execution (CLI mode)** for headless or automated evidence processing.


---

## Changelog

### v1.0 — 2026-04-29
- Expanded artifact support to include cookies, cache, sessions, extensions, saved logins metadata, Local Storage, Session Storage, and IndexedDB.
- Improved Firefox-like and Chrome/Chromium-based browser identification, including embedded WebView2 and Chromium containers.
- Added recognition for known containers such as OneDrive WView2, OpenAI Codex WView2, Steam Embedded Chromium, DeepL, and others.
- Improved source artifact traceability through more consistent `File` values.
- Hardened Firefox extraction, including locked-file fallback and cleaner handling of temporary files.
- Aligned GUI and CLI extraction around the same core parser path.
- Improved PDF, HTML, label, and timeline reports for clearer forensic review.


### v0.2 — 2025-10-01
- Improved scaling for different resolutions.
- Fixed *SQL logic error* when clicking categories with an active time-range search.
- Better PDF export (layout, pagination, summary).
- New **Export to HTML** and **interactive HTML Reports** (filter & sort).

### v0.1 — 2025-07-02
- Initial public release with Firefox/Chrome artifacts, labels, comments, PDF export.




<img width="1911" height="1038" alt="imagen" src="https://github.com/user-attachments/assets/39ebe53f-9480-4995-bdc9-9551542466af" />



## 🚀 Getting Started

### 🔍 Extracting Browser Artifacts

To begin analyzing browser activity:
---
1. Open / Create a project.
   
<img width="601" height="464" alt="imagen" src="https://github.com/user-attachments/assets/bb697a20-ebb2-4702-8381-29ebd826dde1" />


---
2. Select folder to scan.

<img width="938" height="570" alt="imagen" src="https://github.com/user-attachments/assets/13ed49c7-815f-402f-b576-2f03d479ee1b" />


---
   
3. Wait for processing
<img width="1761" height="855" alt="imagen" src="https://github.com/user-attachments/assets/45d6c6fb-2cc2-42ae-b602-909388fdadf4" />





Browser Reviewer will scan the selected path for supported browser artifacts from **Firefox** and **Chrome/Chromium-based browsers**, including:

- 🕓 Browsing history  
- ⬇️ Download history  
- 🔖 Bookmarks  
- 🧠 Autofill form data  
- 🍪 Cookies
- 🗂️ Cache records
- 🪟 Sessions / restored tabs
- 🧩 Extensions
- 🔐 Saved logins metadata
- 💾 Local Storage / Session Storage / IndexedDB

  ---

Once processed, the data will appear in the main table, where you can filter, search, tag, and comment on individual entries.

- Use the **Label Manager** to create and assign custom tags to records.

<img width="1912" height="1033" alt="imagen" src="https://github.com/user-attachments/assets/5eab5397-de88-4b93-8151-fb59f49b4764" />



- Set the **UTC offset** at the top of the interface to adjust all timestamps to the correct time zone.


![imagen](https://github.com/user-attachments/assets/ca1c4145-2d7f-4a24-b35f-04b0cd240264)

- Quickly review user behavior by sorting records chronologically and observing the **Potential Activity** field in **Full time line web activity**.

<img width="1903" height="1034" alt="imagen" src="https://github.com/user-attachments/assets/4b7599a8-aee5-41d1-8ff6-c042fcecaf21" />



  and applying filters as needed.

  By Artifact type:

<img width="1908" height="1035" alt="imagen" src="https://github.com/user-attachments/assets/d318b988-3688-4395-97a3-e99554ac31f1" />

  

or by potential activity, for example.

<img width="1911" height="1036" alt="imagen" src="https://github.com/user-attachments/assets/34235194-1741-4cc6-92a5-c25d27c35480" />




- Use the search bar to perform simple keyword filtering 

<img width="1912" height="1035" alt="imagen" src="https://github.com/user-attachments/assets/bc07e0ed-c417-4fd2-ada8-bd97ee0f9262" />



or **advanced regular expression (RegExp)** searches.

<img width="1913" height="1033" alt="imagen" src="https://github.com/user-attachments/assets/403d4b2c-1682-47e2-b73c-c14b2cf340e2" />


- Visualize and explore data from browser artifacts such as browsing history, downloads, bookmarks, and autofill entries. Browsing history is automatically categorized and tagged based on potential user activity, helping to identify relevant patterns and behaviors.

  History

<img width="1909" height="1039" alt="imagen" src="https://github.com/user-attachments/assets/f662591f-33fa-47b3-9360-a17a5beebd96" />


  Downloads

<img width="1912" height="1035" alt="imagen" src="https://github.com/user-attachments/assets/02f9bd1f-c7f0-44e8-84fa-8bde048d880c" />



  Bookmarks

<img width="1916" height="1037" alt="imagen" src="https://github.com/user-attachments/assets/9e88e198-41fb-45f7-a24d-bb32668800c2" />


  Autofill

<img width="1913" height="1036" alt="imagen" src="https://github.com/user-attachments/assets/9d6ebf9e-e23b-4272-a53a-27315b2148f8" />


  Cookies

<img width="1908" height="1035" alt="imagen" src="https://github.com/user-attachments/assets/0f739d19-254e-4be1-af19-fe044a187f8f" />

  Cache

<img width="1917" height="1034" alt="imagen" src="https://github.com/user-attachments/assets/45140330-8e79-44e1-85a9-0c2a3aa9113e" />


Sessions

<img width="1917" height="1039" alt="imagen" src="https://github.com/user-attachments/assets/a0f5ffa7-99f0-42d4-b050-cc6be7fb1338" />


Extensions

<img width="1917" height="1038" alt="imagen" src="https://github.com/user-attachments/assets/1868defb-a65b-4f87-bcbe-5b6d4d854363" />



Saved login metadata

<img width="1919" height="1034" alt="imagen" src="https://github.com/user-attachments/assets/ce2f04ec-3207-4793-aa56-3f1b5cbc0afa" />


Local storage

<img width="1916" height="1037" alt="imagen" src="https://github.com/user-attachments/assets/787105c5-6df4-4197-abc1-3b4fba9aa794" />


Session storage

<img width="1918" height="1039" alt="imagen" src="https://github.com/user-attachments/assets/5beb7636-9437-4118-83f7-ba905fcf8173" />


IndexedDB

<img width="1918" height="1037" alt="imagen" src="https://github.com/user-attachments/assets/a107f6b5-4f14-4d3a-a5a5-65e9252a004f" />






  

- Define and apply labels and comments to annotate findings of interest during the review.
  
<img width="1531" height="954" alt="imagen" src="https://github.com/user-attachments/assets/cd997b70-3d73-4478-a76e-856846692064" />


- Export results as PDF

<img width="945" height="915" alt="imagen" src="https://github.com/user-attachments/assets/74e9b105-7ec0-4eaf-a43d-0bb74d4c5659" />

- Or interactive HTML

  <img width="1911" height="1028" alt="imagen" src="https://github.com/user-attachments/assets/e595da07-41d4-4512-a8d2-ca3b803a2840" />

- And label based reports

  <img width="1911" height="1038" alt="imagen" src="https://github.com/user-attachments/assets/b52c0103-44fe-4ce3-8889-5fba1de1a4a9" />

  ---

  <img width="1895" height="936" alt="imagen" src="https://github.com/user-attachments/assets/7cfe710e-5700-4b17-89c1-8dbb1996de38" />



---

## Command-Line Usage

Browser Reviewer can also run from the command line for headless or automated evidence processing.

```powershell
br.exe -h
```

```text
Browser Reviewer v1.1 - CLI

Usage:
  br.exe <BaseNameOrPath(.bre)> <RootDirectoryToScan>
  br.exe --cli --out <BaseNameOrPath(.bre)> --scan <RootDirectoryToScan> [--overwrite]

Parameters:
  <BaseNameOrPath(.bre)>   Name or full path of the .bre database file to create.
                           If no extension is provided, .bre will be added automatically.
  <RootDirectoryToScan>    Root folder where browser artifacts will be searched.
  --overwrite              Replace an existing .bre file instead of stopping.

Extracted web artifacts:
  History, downloads, bookmarks, autofill, cookies, cache, sessions, extensions,
  saved logins metadata, local storage, session storage, and IndexedDB.

Examples:
  br.exe MyCase "D:\Evidence\UserProfile"
  br.exe "C:\Cases\Case123.bre" "E:\Mounts\Image01"
  br.exe --cli --out "C:\Cases\Case123.bre" --scan "E:\Mounts\Image01" --overwrite

Help flags:
  /?   -?   -h   --help
```


---




  
