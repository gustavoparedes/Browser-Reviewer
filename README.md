# Browser-Reviewer
Browser Reviewer is a portable forensic tool for analyzing user activity in Firefox and Chrome-based browsers. It extracts and displays browsing history, downloads, bookmarks, and autofill data. The tool allows analysts to tag, comment, and export detailed reports in PDF or HTML format.

It requires no installation and can be executed directly from a USB drive or over a network share — ideal for forensic workflows with minimal footprint on the target system.

![imagen](https://github.com/user-attachments/assets/3395cf20-1b7f-472b-8dee-7622d6876262)


## 🚀 Getting Started

### 🔍 Extracting Browser Artifacts

To begin analyzing browser activity:

1. Go to **File > New**
2. Choose a **file name** for the new project (a SQLite database will be created)
3. Click **Scan Web Activity**

Browser Reviewer will scan the selected path for supported browser artifacts from **Firefox** and **Chrome/Chromium-based browsers**, including:

- 🕓 Browsing history  
- ⬇️ Download history  
- 🔖 Bookmarks  
- 🧠 Autofill form data  

Once processed, the data will appear in the main table, where you can filter, search, tag, and comment on individual entries.

- Use the **Label Manager** to create and assign custom tags to records.
- Set the **UTC offset** at the top of the interface to adjust all timestamps to the correct time zone.
- Quickly review user behavior by sorting records chronologically and observing the **Potential Activity** field.
- Use the search bar to perform simple keyword filtering or **advanced regular expression (RegExp)** searches across all fields.
