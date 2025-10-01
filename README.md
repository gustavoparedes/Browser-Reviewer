# Browser-Reviewer
Browser Reviewer is a portable forensic tool for analyzing user activity in Firefox and Chrome-based browsers for Windows platforms. It extracts and displays browsing history, downloads, bookmarks, and autofill data. The tool allows analysts to tag, comment, and export reports in PDF.

It requires no installation and can be executed directly from a USB drive or over a network share — ideal for forensic workflows with minimal footprint on the target system.

Download compiled version [here](https://github.com/gustavoparedes/Browser-Reviewer/releases/download/v0.1/Browser.Reviewer.v0.1.rar).

## What’s New (v0.2 — 2025-10-01)

- **Better display scaling**: Improved support for different screen resolutions and high-DPI displays. UI elements, fonts and grids scale more consistently across laptops, external monitors and projectors.
- **Time-range + Categories fix**: Resolved the *SQL logic error* that could occur when clicking on categories while a **time-range search** was active.
- **PDF exporting, improved**: More reliable layout, better table pagination, and clearer metadata on the first page.
- **New: Export to HTML**: Export the current view as a standalone **HTML Report** with built-in **filtering** and **sorting** (works offline in any modern browser).
- **HTML Reports**: Interactive headers for sorting, a search box for quick filtering, and a compact summary (rows, visible columns, sort, filters, export time, and time zone).

---

## Features

- Extracts and visualizes browser artifacts from **Firefox** and **Chrome/Chromium-based** browsers (history, downloads, bookmarks, autofill).
- **Label Manager** to tag items and **Comments** to annotate findings.
- Powerful search: simple text or **RegExp**.
- **Time zone** offset control (consistent timestamps across cases).
- **High-DPI aware** UI and better scaling on different screen resolutions. ← *new in v0.2*
- Export options:
  - **PDF** (improved pagination & layout). ← *v0.2 improvements*
  - **HTML Report** with client-side **filter/sort**. ← *new in v0.2*

---

## Exporting

### PDF
Use **File → Export → PDF** to generate a paginated report of the current grid.  

### HTML
Use **File → Export → HTML** to generate a standalone HTML file:
- **Sortable columns** (click column headers)
- **Quick filter** (search box)
- **Self-contained** (no server required; opens in any modern browser)
- **Summary block** (rows exported, visible columns, active sort/filter, export timestamp, time zone)

---

## Changelog

### v0.2 — 2025-10-01
- Improved scaling for different resolutions / high-DPI displays.
- Fixed *SQL logic error* when clicking categories with an active time-range search.
- Better PDF export (layout, pagination, summary).
- New **Export to HTML** and **interactive HTML Reports** (filter & sort).

### v0.1 — 2025-07-02
- Initial public release with Firefox/Chrome artifacts, labels, comments, PDF export.


![imagen](https://github.com/user-attachments/assets/3395cf20-1b7f-472b-8dee-7622d6876262)


## 🚀 Getting Started

### 🔍 Extracting Browser Artifacts

To begin analyzing browser activity:

1. Go to **File > New**
   
![imagen](https://github.com/user-attachments/assets/eb79f8d9-1d95-4f0b-b28f-633a715c24ae)

2. Choose a **file name** for the new project.

![imagen](https://github.com/user-attachments/assets/fcfb1526-7f6a-468c-9989-cf1422c3ea86)


   
3. Click **"Scan Web Activity"** and select the directory where browser artifacts should be searched. Make sure you have read access to the selected path.

![imagen](https://github.com/user-attachments/assets/4c17b8bb-eb37-40a5-97f7-5934bf92fc69)




Browser Reviewer will scan the selected path for supported browser artifacts from **Firefox** and **Chrome/Chromium-based browsers**, including:

- 🕓 Browsing history  
- ⬇️ Download history  
- 🔖 Bookmarks  
- 🧠 Autofill form data  

Once processed, the data will appear in the main table, where you can filter, search, tag, and comment on individual entries.

- Use the **Label Manager** to create and assign custom tags to records.

![imagen](https://github.com/user-attachments/assets/fd3b890a-2476-4573-8547-fb9d6ace97d1)


- Set the **UTC offset** at the top of the interface to adjust all timestamps to the correct time zone.


![imagen](https://github.com/user-attachments/assets/ca1c4145-2d7f-4a24-b35f-04b0cd240264)

- Quickly review user behavior by sorting records chronologically and observing the **Potential Activity** field.

![imagen](https://github.com/user-attachments/assets/3309b8db-8385-4085-9be8-3e58b2c8cee6)


  and applying filters as needed.

  By category:

![imagen](https://github.com/user-attachments/assets/db3c666d-f886-4513-a7b7-ee7c3810532a)
  

or by potential activity, for example.

![imagen](https://github.com/user-attachments/assets/0dca8f1e-cafb-44ce-85e0-ae70f752f57b)



- Use the search bar to perform simple keyword filtering 

![imagen](https://github.com/user-attachments/assets/435882ed-ab08-4838-ab7b-82d35d2861f6)


or **advanced regular expression (RegExp)** searches.

![imagen](https://github.com/user-attachments/assets/53f3262e-ddb3-4749-a170-571f27a29823)

- Visualize and explore data from browser artifacts such as browsing history, downloads, bookmarks, and autofill entries. Browsing history is automatically categorized and tagged based on potential user activity, helping to identify relevant patterns and behaviors.

  History

![imagen](https://github.com/user-attachments/assets/5444bbc6-4ccd-452f-81bc-d386b8edcafa)

  Downloads

![imagen](https://github.com/user-attachments/assets/14a269fd-c90c-4153-9a3a-394f9b6ea897)

  Bookmarks

![imagen](https://github.com/user-attachments/assets/753dae17-b663-4420-a3df-64dc937cfa08)

  

- Define and apply labels and comments to annotate findings of interest during the review.
  
![imagen](https://github.com/user-attachments/assets/a7db0671-292d-4a96-b1c5-e45d53c787fe)

- And export results as PDF

![imagen](https://github.com/user-attachments/assets/f9eafe3d-cbe2-4a74-adaa-494d6bd06ca3)






  
